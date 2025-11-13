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
    [Tooltip("대시 속도")]
    [SerializeField] private float dashSpeed = 15f;
    
    [Tooltip("대시 가속도 (즉시 최고속에 도달하도록 충분히 크게 권장)")]
    [SerializeField] private float dashAcceleration = 500f;
    
    
    
    [Tooltip("대시 데미지")]
    [SerializeField] private float dashDamage = 30f;

    [Header("충돌 판정")]
    [Tooltip("플레이어와의 충돌 판정 반경 (거리 기반)")]
    [SerializeField] private float hitRadius = 0.6f;

    [Header("벽 충돌 감지")]
    [Tooltip("벽 충돌 감지 여부 (활성화하면 벽을 통과하지 않음)")]
    [SerializeField] private bool enableWallCollision = true;
    [Tooltip("벽으로 인식할 레이어 마스크 (예: Wall, Obstacle 등)")]
    [SerializeField] private LayerMask wallLayer = -1;
    [Tooltip("벽 감지용 충돌 반경 (몬스터 크기보다 약간 크게 설정)")]
    [SerializeField] private float wallCheckRadius = 0.5f;
    [Tooltip("벽 감지 거리 (한 프레임에 이동할 거리보다 약간 크게)")]
    [SerializeField] private float wallCheckDistance = 0.2f;

    [Header("대시 거리")]
    [Tooltip("고정 대시 거리 사용 여부 (켜면 대시 거리를 명확히 지정)")]
    [SerializeField] private bool useFixedDashDistance = false;
    [Tooltip("고정 대시 거리 (유닛)")]
    [SerializeField] private float dashDistance = 6f;
    [Tooltip("고정 거리 사용 시, 거리/속도 기반으로 자동 지속시간 계산")]
    [SerializeField] private bool autoDurationFromDistance = true;

    [Header("대시 경고 표시")]
    [Tooltip("대시 전 경고 표시 여부")]
    [SerializeField] private bool enableDashWarning = true;
    [Tooltip("대시 전 경고 표시 지속 시간 (초)")]
    [SerializeField] private float dashWarningDuration = 0.6f;
    [Tooltip("대시 경고 색상 (반투명)")]
    [SerializeField] private Color dashWarningColor = new Color(1f, 0.5f, 0f, 0.3f);

    private MonsterSkillData skillData;
    private GameObject ownerObject;
    private MonsterController ownerController;
    private Transform target;
    private NavMeshAgent agent;
    private float originalSpeed;
    private float originalStoppingDistance;
    private bool originalAutoBraking;
    private float originalAcceleration;
    private bool originalAgentUpdatePosition;
    private bool originalAgentUpdateRotation;
    
    private bool isDashing = false;
    private bool hasHitTarget = false;
    private Rigidbody2D rb2d;

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
            originalAcceleration = agent.acceleration;
            originalAgentUpdatePosition = agent.updatePosition;
            originalAgentUpdateRotation = agent.updateRotation;
            
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
        
		// ⭐ 경고창 전에 완전 정지 (관성 제거) 및 한 프레임 대기
		if (agent.isOnNavMesh)
		{
			agent.isStopped = true;
			agent.velocity = Vector3.zero;
		}
        yield return null;

        // 3. 대시 목표 위치 고정 및 경고 표시 (경고 표시 순간의 플레이어 위치 고정)
        Vector3 dashStart = ownerController.transform.position;
        Vector3 warningLockedTargetPosition;
        Vector3 plannedDashEnd;
        GameObject dashTelegraphObj = null;
        if (enableDashWarning && dashWarningDuration > 0f)
        {
            // 경고창이 뜨는 바로 그 순간의 플레이어 위치를 잠금
            warningLockedTargetPosition = target.position;
            Vector3 dashDirection = (warningLockedTargetPosition - dashStart).normalized;
            plannedDashEnd = useFixedDashDistance ? dashStart + dashDirection * dashDistance : warningLockedTargetPosition;

            dashTelegraphObj = CreateDashTelegraph(dashStart, plannedDashEnd);
            yield return new WaitForSeconds(dashWarningDuration);
            if (dashTelegraphObj != null)
            {
                Destroy(dashTelegraphObj);
            }
        }
        else
        {
            // 경고가 없으면 대쉬 직전에 위치를 잠금
            warningLockedTargetPosition = target.position;
            Vector3 dashDirection = (warningLockedTargetPosition - dashStart).normalized;
            plannedDashEnd = useFixedDashDistance ? dashStart + dashDirection * dashDistance : warningLockedTargetPosition;
        }
        
        // 4. NavMeshAgent를 대시 모드로 설정 (직선 이동은 직접 제어하므로 에이전트 이동은 비활성화)
		agent.speed = dashSpeed;
		agent.acceleration = dashAcceleration; // 가속도입니다.
		agent.stoppingDistance = 0f; // 끝까지 붙도록
		agent.autoBraking = false;   // 감속 비활성화
		if (agent.isOnNavMesh)
		{
			agent.isStopped = true;      // 에이전트 자체 이동 정지
		}
		agent.updatePosition = false; // 위치 업데이트는 직접 처리
		agent.updateRotation = false; // 회전 업데이트 비활성화 (방향 틀지 않음)
        
        isDashing = true;
        hasHitTarget = false;
        float startTime = Time.time;
        float plannedLength = Vector3.Distance(dashStart, plannedDashEnd);
        float safetyDuration = Mathf.Max(1f, (dashSpeed > 0.001f ? plannedLength / dashSpeed : 1f) * 1.5f);
        
        Debug.Log($"[DashHandler] 대시 속도 설정: {originalSpeed} → {dashSpeed}");
        
        // 4. 대시 지속 시간만큼 플레이어를 향해 돌진 + 거리 판정
        float elapsedTime = 0f;
		while ((Time.time - startTime) < safetyDuration && !hasHitTarget)
        {
			// 주기적 안전 점검: 소유자가 죽었거나 에이전트가 더 이상 유효하지 않으면 중단
			if (ownerController == null || !ownerController.IsAlive)
			{
				Debug.Log("[DashHandler] 소유자가 사망하여 대시를 중단합니다.");
				break;
			}

			// 직선 이동 (방향 불변)
			Vector3 current = ownerController.transform.position;
			float step = dashSpeed * Time.fixedDeltaTime;
			Vector3 nextPos = Vector3.MoveTowards(current, plannedDashEnd, step);

			// 벽 충돌 감지: 이동하기 전에 벽이 있는지 체크
			if (enableWallCollision && CheckWallCollision(current, nextPos))
			{
				Debug.Log("[DashHandler] 벽에 부딪혀 대시를 중단합니다.");
				break;
			}

			// 이동 실행
			if (rb2d != null)
			{
				rb2d.MovePosition(new Vector2(nextPos.x, nextPos.y));
			}
			else
			{
				ownerController.transform.position = nextPos;
			}

			// 목표 지점 도달 여부 체크
			bool reachedDestination = (nextPos - plannedDashEnd).sqrMagnitude <= (0.05f * 0.05f);
			if (reachedDestination)
			{
				break;
			}
			
			if (target != null)
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

			elapsedTime += Time.fixedDeltaTime;
			yield return new WaitForFixedUpdate();
        }
        
		// 5. 대시 완료 후 에이전트 동기화 및 정지
		if (agent != null && agent.enabled && agent.isActiveAndEnabled)
		{
			// 현재 Transform 위치로 에이전트를 동기화하여 스냅백 방지
			if (agent.isOnNavMesh)
			{
				agent.Warp(ownerController.transform.position);
				agent.isStopped = true;
			}
			else
			{
				// NavMesh 밖이면 근처 유효 지점으로 워프 시도
				NavMeshHit hit;
				if (NavMesh.SamplePosition(ownerController.transform.position, out hit, 1.5f, NavMesh.AllAreas))
				{
					agent.Warp(hit.position);
					agent.isStopped = true;
				}
				else
				{
					// 근처에 NavMesh가 없으면 에이전트를 비활성화 (컨트롤러 이동 유지)
					agent.enabled = false;
					Debug.LogWarning("[DashHandler] 주변에 NavMesh가 없어 에이전트를 비활성화했습니다.");
				}
			}
			// 업데이트 플래그 복구는 마지막에
			agent.updatePosition = originalAgentUpdatePosition;
			agent.updateRotation = originalAgentUpdateRotation;
		}
		else
		{
			Debug.LogWarning("[DashHandler] NavMeshAgent가 유효하지 않아 동기화를 생략합니다.");
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
            agent.acceleration = originalAcceleration;
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

    #region 벽 충돌 감지
    
    /// <summary>
    /// 현재 위치에서 다음 위치로 이동할 때 벽과 충돌하는지 확인합니다.
    /// CircleCast를 사용하여 몬스터의 크기를 고려한 충돌 감지를 수행합니다.
    /// </summary>
    /// <param name="currentPos">현재 위치</param>
    /// <param name="nextPos">이동할 위치</param>
    /// <returns>벽과 충돌하면 true, 아니면 false</returns>
    private bool CheckWallCollision(Vector3 currentPos, Vector3 nextPos)
    {
        // 이동 방향과 거리 계산
        Vector2 direction = (nextPos - currentPos).normalized;
        float distance = Vector3.Distance(currentPos, nextPos);
        
        // 이동 거리가 너무 작으면 체크하지 않음
        if (distance < 0.001f)
        {
            return false;
        }
        
        // CircleCast로 경로상의 벽 감지
        // 시작점에서 wallCheckRadius 반경의 원을 이동 방향으로 쏘아 벽을 감지합니다
        RaycastHit2D hit = Physics2D.CircleCast(
            currentPos, 
            wallCheckRadius, 
            direction, 
            distance + wallCheckDistance, 
            wallLayer
        );
        
        if (hit.collider != null)
        {
            Debug.Log($"[DashHandler] 벽 감지됨: {hit.collider.name} (거리: {hit.distance:F2})");
            return true;
        }
        
        return false;
    }
    
    #endregion
    
    #region 시각적 효과
    
    /// <summary>
    /// 대시 경로를 표시하는 임시 직사각형 스프라이트 생성
    /// </summary>
    private GameObject CreateDashTelegraph(Vector3 start, Vector3 end)
    {
        float length = Vector3.Distance(start, end);
        if (length <= 0.01f)
        {
            return null;
        }
        
        GameObject telegraph = new GameObject("DashTelegraph");
        var spriteRenderer = telegraph.AddComponent<SpriteRenderer>();
        
        // 기본 1x1 흰색 텍스처 스프라이트 생성
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        spriteRenderer.sprite = Sprite.Create(
            texture,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f),
            1f
        );
        spriteRenderer.color = dashWarningColor;
        
        // 위치/회전/스케일 설정 (폭은 판정 반경 기준)
        Vector3 mid = (start + end) * 0.5f;
        telegraph.transform.position = new Vector3(mid.x, mid.y, 0f);
        Vector3 dir = (end - start).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        telegraph.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        float width = hitRadius * 2f;
        telegraph.transform.localScale = new Vector3(length, width, 1f);
        
        // 안전장치: 4초 후 자동 제거
        Destroy(telegraph, 4f);
        
        return telegraph;
    }
    
    #endregion
}
