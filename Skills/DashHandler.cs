using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// 대시 스킬 핸들러
/// 몬스터가 플레이어를 향해 매우 빠르게 돌진하여 충돌 시(거리 판정) 데미지를 줍니다
/// 대시 중에는 모든 이동 로직을 핸들러가 직접 관리합니다
/// </summary>
public class DashHandler : MonoBehaviour, SkillHandlerInterface
{
    [Header("대시 설정")]
    [Tooltip("대시 속도 (매우 빠른 속도)")]
    [SerializeField] private float dashSpeed = 15f;
    
    [Tooltip("대시 지속 시간 (초)")]
    [SerializeField] private float dashDuration = 1.0f;
    
    [Tooltip("대시 데미지")]
    [SerializeField] private float dashDamage = 30f;

    [Header("충돌 판정")]
    [Tooltip("플레이어와의 충돌 판정 반경 (거리 기반)")]
    [SerializeField] private float hitRadius = 0.6f;

    private MonsterSkillData skillData;
    private GameObject ownerObject;
    private MonsterController ownerController;
    private Transform target;
    private NavMeshAgent agent;
    private float originalSpeed;
    private float originalStoppingDistance;
    private bool originalAutoBraking;
    private bool isDashing = false;
    private bool hasHitTarget = false;
    private Rigidbody2D rb2d;
    private float dashEndTime;

    #region SkillHandlerInterface 구현
    
    /// <summary>
    /// 스킬 핸들러 초기 설정
    /// </summary>
    public void Setup(BaseSkillData data, GameObject owner)
    {
        skillData = data as MonsterSkillData;
        ownerObject = owner;
        ownerController = owner.GetComponent<MonsterController>();
        agent = owner.GetComponent<NavMeshAgent>();
        
        if (ownerController != null)
        {
            target = ownerController.Target;
        }
        
        if (agent != null)
        {
            originalSpeed = agent.speed;
            originalStoppingDistance = agent.stoppingDistance;
            originalAutoBraking = agent.autoBraking;
        }
        
        // 2D 물리 이벤트(다른 스킬과의 호환)를 위해 Rigidbody2D 보장(대시에 필수는 아님)
        rb2d = owner.GetComponent<Rigidbody2D>();
        if (rb2d == null)
        {
            rb2d = owner.AddComponent<Rigidbody2D>();
        }
        rb2d.bodyType = RigidbodyType2D.Kinematic;
        rb2d.simulated = true;
        rb2d.gravityScale = 0f;
        
        Debug.Log($"[DashHandler] Setup 완료 - 원래 속도: {originalSpeed}");
    }
    
    /// <summary>
    /// 스킬 사용
    /// </summary>
    public void Use(SkillRank rank)
    {
        if (skillData == null || ownerController == null || target == null)
        {
            Debug.LogError("[DashHandler] 스킬 데이터, 소유자 또는 타겟이 없습니다!");
            Cleanup();
            return;
        }
        
        if (agent == null)
        {
            Debug.LogError("[DashHandler] NavMeshAgent가 없습니다!");
            Cleanup();
            return;
        }
        
        // 대시 시작
        StartCoroutine(DashSequence());
    }
    
    /// <summary>
    /// 스킬 정리
    /// </summary>
    public void Cleanup()
    {
        Debug.Log("[DashHandler] Cleanup");
        Destroy(gameObject);
    }
    
    #endregion
    
    #region 대시 로직
    
    /// <summary>
    /// 대시 시퀀스: 매우 빠른 속도로 돌진 → 거리 판정으로 데미지 적용
    /// </summary>
    private IEnumerator DashSequence()
    {
        // 1. 대시 시작
        Debug.Log("[DashHandler] 대시 시작 - 매우 빠른 속도로 돌진!");
        
        // 2. 몬스터의 일반 이동 로직 완전 비활성화
        ownerController.DisableMovement();
		
		// 에이전트 사용 가능 여부 선 확인
		if (agent == null || !agent.enabled || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
		{
			Debug.LogWarning("[DashHandler] NavMeshAgent가 비활성화되었거나 NavMesh 위에 있지 않아 대시를 시작할 수 없습니다.");
			Cleanup();
			yield break;
		}
        
        // 3. NavMeshAgent를 대시 모드로 설정
        agent.speed = dashSpeed;
        agent.stoppingDistance = 0f; // 끝까지 붙도록
        agent.autoBraking = false;   // 감속 비활성화
        agent.isStopped = false;
        
        isDashing = true;
        hasHitTarget = false;
        dashEndTime = Time.time + dashDuration;
        
        Debug.Log($"[DashHandler] 대시 속도 설정: {originalSpeed} → {dashSpeed}");
        
        // 4. 대시 지속 시간만큼 플레이어를 향해 돌진 + 거리 판정
        float elapsedTime = 0f;
		while (elapsedTime < dashDuration && !hasHitTarget)
        {
			// 주기적 안전 점검: 소유자가 죽었거나 에이전트가 더 이상 유효하지 않으면 중단
			if (ownerController == null || !ownerController.IsAlive)
			{
				Debug.Log("[DashHandler] 소유자가 사망하여 대시를 중단합니다.");
				break;
			}
			if (agent == null || !agent.enabled || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
			{
				Debug.LogWarning("[DashHandler] NavMeshAgent가 더 이상 유효하지 않아 대시를 중단합니다.");
				break;
			}
			
            if (target != null)
            {
                // 매 프레임 플레이어 위치로 목적지 갱신 (유도탄처럼)
				agent.SetDestination(target.position);

                // 대시 시간 창 내부에서만 판정
                if (Time.time <= dashEndTime)
                {
                    // 거리 기반 충돌 판정
                    float sqrDist = (ownerController.transform.position - target.position).sqrMagnitude;
                    if (sqrDist <= hitRadius * hitRadius)
                    {
                        hasHitTarget = true;
                        var targetEntity = target.GetComponentInParent<EntityController>();
                        if (targetEntity != null)
                        {
                            bool applied = targetEntity.ApplyDamage((int)dashDamage, EntityType.Monster);
                            Debug.Log(applied
                                ? $"[DashHandler] 거리 판정 충돌! {target.name}에게 {dashDamage} 데미지 적용"
                                : $"[DashHandler] 거리 판정 충돌! 그러나 데미지 적용 실패 (팀/무적/가드 등)");
                        }
                        break;
                    }
                }
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
		// 5. 대시 완료 후 즉시 정지 (에이전트가 유효한 경우에만)
		if (agent != null && agent.enabled && agent.isActiveAndEnabled && agent.isOnNavMesh)
		{
			agent.isStopped = true;
		}
		else
		{
			Debug.LogWarning("[DashHandler] NavMeshAgent가 유효하지 않아 정지 호출을 생략합니다.");
		}
		
		// 6. 충돌 결과 로그
        if (!hasHitTarget)
        {
            Debug.Log($"[DashHandler] 대시 완료 - 플레이어와 충돌하지 않음");
        }
        
		// 7. 원래 속성 복구 및 일반 이동 로직 재활성화 (에이전트 유효 시)
		if (agent != null && agent.enabled && agent.isActiveAndEnabled)
		{
			agent.speed = originalSpeed;
			agent.stoppingDistance = originalStoppingDistance;
			agent.autoBraking = originalAutoBraking;
		}
		else
		{
			Debug.LogWarning("[DashHandler] NavMeshAgent가 유효하지 않아 속성 복구를 생략합니다.");
		}
        isDashing = false;
        ownerController.EnableMovement();
        
        Debug.Log($"[DashHandler] 대시 완료 - 원래 속도로 복구 및 이동 재개");
        
        // 8. 핸들러 정리
        Cleanup();
    }
    
    #endregion
}
