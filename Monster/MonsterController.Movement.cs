using UnityEngine;

/// <summary>
/// MonsterController - Movement 파트
/// NavMeshPlus 기반 2D 경로 찾기 시스템
/// </summary>
public partial class MonsterController : EntityController
{
    #region Movement System - NavMeshPlus 2D 이동
    
    /// <summary>
    /// 메인 이동 처리 (Update에서 호출)
    /// </summary>
    private void ProcessMovement()
    {
        // ⭐ NavMeshAgent가 비활성화되어 있으면 이동 처리 안함 (죽은 상태 등)
        if (navAgent == null || !navAgent.enabled || !navAgent.isOnNavMesh) return;
        if (target == null) return;
        if (!isMovementEnabled) return;
        if (monsterData == null) return;
        if (!monsterData.canMove) return;
        
        // ⭐ 스킬 사용 중 이동 잠금 체크
        if (IsMovementLocked)
        {
            StopMovement();
            return;
        }
        
        float distance = GetDistanceToTarget();
        bool isInRange = distance <= runtimeDetectionRange; // ⭐ 런타임 변수 사용
        
        // 인식 범위 체크
        if (!isInRange && monsterData.stopWhenOutOfRange)
        {
            StopMovement();
            return;
        }
        
        // 정지 거리 이내면 멈춤
        if (IsAtStopDistance())
        {
            StopMovement();
            return;
        }
        
        // 타겟 추적
        MoveTowardsTarget();
        
        // ⭐ 2D: Z축 고정 (NavMeshPlus는 XY 평면에서 작동)
        Fix2DPosition();
    }
    
    /// <summary>
    /// 타겟 방향으로 이동 (NavMeshAgent 사용)
    /// </summary>
    private void MoveTowardsTarget()
    {
        if (navAgent == null || target == null) return;
        
        // NavMeshAgent에 목적지 설정 (2D: Z축 0으로 고정)
        Vector3 targetPos = target.position;
        targetPos.z = 0f;
        navAgent.SetDestination(targetPos);
        
        // isStopped를 false로 (이동 재개)
        if (navAgent.isStopped)
        {
            navAgent.isStopped = false;
        }
        
        // 선택: 이동 방향으로 스프라이트 반전
        if (navAgent.velocity.sqrMagnitude > 0.01f)
        {
            FlipSprite(navAgent.velocity.x);
        }
    }
    
    /// <summary>
    /// 이동 멈춤 (NavMesh)
    /// </summary>
    private void StopMovement()
    {
        // ⭐ NavMeshAgent가 활성화되어 있고 NavMesh에 있을 때만 정지 명령
        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh && !navAgent.isStopped)
        {
            navAgent.isStopped = true;
        }
    }
    
    /// <summary>
    /// 2D 프로젝트를 위한 Z축 고정
    /// ⚠️ NavMeshPlus 설정이 올바르면 Z축 drift가 발생하지 않음
    /// 경고 발생 시에만 디버깅용으로 활성화
    /// </summary>
    private void Fix2DPosition()
    {
        // ⭐ 성능 최적화: Z축 체크를 매 프레임이 아닌 조건부로만 실행
        #if UNITY_EDITOR || DEBUG
        Vector3 pos = transform.position;
        if (Mathf.Abs(pos.z) > 0.01f)  // Z축이 0이 아니면 고정 및 경고
        {
            pos.z = 0f;
            transform.position = pos;
            Debug.LogWarning($"[MonsterController] {gameObject.name} Z축 drift 감지 및 수정 (NavMeshPlus 설정 확인 필요)");
        }
        #endif
        
        // ⭐ 릴리즈 빌드에서는 아예 실행 안함 (성능 최적화)
    }
    
    /// <summary>
    /// 스프라이트 좌우 반전 (이동 방향에 따라)
    /// ⭐ SpriteRenderer.flipX 사용 (권장)
    /// </summary>
    private void FlipSprite(float directionX)
    {
        if (Mathf.Abs(directionX) < 0.01f) return; // 방향이 없으면 무시
        if (spriteRenderer == null) return; // SpriteRenderer 없으면 무시
        
        // ⭐ SpriteRenderer.flipX 사용 (Transform.localScale보다 성능/안정성 우수)
        if (directionX < 0)  // 왼쪽으로 이동
        {
            spriteRenderer.flipX = true;
        }
        else if (directionX > 0)  // 오른쪽으로 이동
        {
            spriteRenderer.flipX = false;
        }
    }
    
    #endregion
    
    #region Movement Control - 이동 제어 (스킬에서 호출)
    
    /// <summary>
    /// 이동을 일정 시간 동안 잠급니다 (스킬 사용 중)
    /// 스킬에서 이동 제어가 필요할 때 호출
    /// </summary>
    /// <param name="duration">잠금 시간 (초)</param>
    public void LockMovement(float duration)
    {
        movementLockTimer = Mathf.Max(movementLockTimer, duration); // 기존 타이머보다 긴 시간만 적용
        Debug.Log($"[MonsterController] {gameObject.name} 이동 잠금 ({duration}초)");
    }
    
    /// <summary>
    /// 이동 잠금을 즉시 해제합니다
    /// </summary>
    public void UnlockMovement()
    {
        movementLockTimer = 0f;
        Debug.Log($"[MonsterController] {gameObject.name} 이동 잠금 해제");
    }
    
    /// <summary>
    /// 이동 활성화 (영구적 활성화)
    /// 일시적 잠금은 LockMovement 사용
    /// </summary>
    public void EnableMovement()
    {
        if (isMovementEnabled) return;
        isMovementEnabled = true;
        
        // ⭐ NavMeshAgent가 활성화되어 있을 때만 재개
        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = false;
        }
        
        Debug.Log($"[MonsterController] {gameObject.name} 이동 영구 활성화");
    }
    
    /// <summary>
    /// 이동 비활성화 (영구적 비활성화)
    /// ⚠️ 일시적 잠금은 LockMovement 사용
    /// </summary>
    public void DisableMovement()
    {
        if (!isMovementEnabled) return;
        isMovementEnabled = false;
        
        StopMovement();
        
        Debug.Log($"[MonsterController] {gameObject.name} 이동 영구 비활성화");
    }
    
    /// <summary>
    /// 이동 토글
    /// </summary>
    public void ToggleMovement()
    {
        isMovementEnabled = !isMovementEnabled;
        Debug.Log($"[MonsterController] {gameObject.name} 이동 토글: {isMovementEnabled}");
    }
    
    /// <summary>
    /// 이동 활성화 상태 확인 (영구적 플래그)
    /// </summary>
    public bool IsMovementEnabled()
    {
        return isMovementEnabled;
    }
    
    /// <summary>
    /// 현재 이동 중인지 확인 (NavMesh 기반)
    /// </summary>
    public bool IsMoving()
    {
        // ⭐ NavMeshAgent가 null이거나 비활성화 상태면 false 반환
        if (navAgent == null || !navAgent.enabled || !navAgent.isOnNavMesh) 
            return false;
        
        return !navAgent.isStopped && navAgent.velocity.sqrMagnitude > 0.01f;
    }
    
    #endregion
    
    #region Target & Detection System - 타겟 시스템
    
    /// <summary>
    /// 타겟까지 거리 계산 (2D)
    /// </summary>
    public float GetDistanceToTarget()
    {
        if (target == null) return float.MaxValue;
        return Vector2.Distance(transform.position, target.position);
    }
    
    /// <summary>
    /// 타겟이 인식 범위 내인지 확인
    /// </summary>
    public bool IsTargetInRange()
    {
        if (target == null) return false;
        float distance = GetDistanceToTarget();
        return distance <= runtimeDetectionRange; // ⭐ 런타임 변수 사용
    }
    
    /// <summary>
    /// 타겟이 정지 거리 내인지 확인
    /// </summary>
    public bool IsAtStopDistance()
    {
        if (target == null) return false;
        float distance = GetDistanceToTarget();
        return distance <= runtimeStopDistance; // ⭐ 런타임 변수 사용
    }
    
    /// <summary>
    /// 타겟 설정
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        Debug.Log($"[MonsterController] {gameObject.name}의 타겟이 {(newTarget != null ? newTarget.name : "null")}로 변경됨");
    }
    
    /// <summary>
    /// 인식 범위 설정 (런타임 전용)
    /// </summary>
    public void SetDetectionRange(float range)
    {
        runtimeDetectionRange = range; // ⭐ 런타임 변수만 수정 (SO 수정 안함)
        Debug.Log($"[MonsterController] {gameObject.name}의 인식 거리가 {range}로 변경됨");
    }
    
    /// <summary>
    /// 인식 범위 반환 (런타임 값)
    /// </summary>
    public float GetDetectionRange()
    {
        return runtimeDetectionRange; // ⭐ 런타임 변수 반환
    }
    
    /// <summary>
    /// 이동 속도 설정 (런타임 전용)
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        runtimeMoveSpeed = speed;
        if (navAgent != null)
        {
            navAgent.speed = runtimeMoveSpeed;
        }
        Debug.Log($"[MonsterController] {gameObject.name}의 이동 속도가 {speed}로 변경됨");
    }
    
    /// <summary>
    /// 정지 거리 설정 (런타임 전용)
    /// </summary>
    public void SetStopDistance(float distance)
    {
        runtimeStopDistance = distance;
        if (navAgent != null)
        {
            navAgent.stoppingDistance = runtimeStopDistance;
        }
        Debug.Log($"[MonsterController] {gameObject.name}의 정지 거리가 {distance}로 변경됨");
    }
    
    /// <summary>
    /// 현재 이동 속도 반환 (상태효과 포함)
    /// </summary>
    public float GetCurrentMoveSpeed()
    {
        // ⭐ NavMeshAgent가 활성화되어 있을 때만 속도 반환
        if (navAgent == null || !navAgent.enabled) return 0f;
        return navAgent.speed;
    }
    
    #endregion
}
