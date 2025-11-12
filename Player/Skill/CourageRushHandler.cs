using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.LuisPedroFonseca.ProCamera2D;

/// <summary>
/// 용기의 질주 스킬 핸들러
/// 대쉬 후 도착 지점에 AOE 데미지 + 둔화 효과
/// 다른 스킬 적중 시 재사용 가능
/// </summary>
public class CourageRushHandler : MonoBehaviour, SkillHandlerInterface
{
	#region 기본 설정
	
	[Header("기본 데이터")]
	private PlayerSkillData skillData;
	private GameObject owner;
	private PlayerController playerController;
	private SkillSlot skillSlot; // 쿨타임 강제 초기화를 위한 SkillSlot 참조
	
	#endregion
	
	#region 대쉬 설정
	
	[Header("대쉬 거리")]
	[Tooltip("1차 대쉬 거리 (기본)")]
	[SerializeField] private float firstDashDistance = 8f;
	
	[Tooltip("2차 대쉬 거리 (재사용)")]
	[SerializeField] private float secondDashDistance = 4f;
	
	[Tooltip("대쉬 속도")]
	[SerializeField] private float dashSpeed = 30f;
	
	[Tooltip("대쉬 중 무적 여부")]
	[SerializeField] private bool invincibleDuringDash = true;
	
	[Header("충돌 감지")]
	[Tooltip("충돌을 감지할 레이어 (벽, 장애물 등)")]
	[SerializeField] private LayerMask obstacleLayerMask = -1;
	
	#endregion
	
	#region AOE 설정
	
	[Header("AOE 범위 및 데미지")]
	[Tooltip("AOE 반경")]
	[SerializeField] private float aoeRadius = 2f;
	
	[Tooltip("기본 데미지 퍼센테이지 (1차)")]
	[SerializeField] private float baseDamagePercent = 80f;
	
	[Tooltip("재사용 시 데미지 배율")]
	[SerializeField] private float rechargeDamageMultiplier = 1.3f;
	
	[Tooltip("AOE 지속 시간")]
	[SerializeField] private float aoeDuration = 2f;
	
	#endregion
	
	#region 상태 효과
	
	[Header("상태 효과")]
	[Tooltip("둔화 효과 (기본)")]
	[SerializeField] private StatusEffectDataSO slowEffect;
	
	[Tooltip("기절 효과 (A랭크 이상)")]
	[SerializeField] private StatusEffectDataSO stunEffect;
	
	#endregion
	
	#region 재사용 메커니즘
	
	[Header("재사용 시스템")]
	[Tooltip("재사용 가능 횟수 (기본: 1회)")]
	[SerializeField] private int maxRechargeCount = 1;
	
	// 런타임 상태
	private int currentRechargeCount = 0;
	private bool isRechargeAvailable = false;
	
	#endregion
	
	#region S랭크 시스템
	
	[Header("S랭크(확신) 효과")]
	[Tooltip("S랭크 쿨타임 (매우 짧음)")]
	[SerializeField] private float sRankCooldown = 0.5f;
	
	[Tooltip("S랭크 기본 지속시간")]
	[SerializeField] private float sRankBaseDuration = 15f;
	
	[Tooltip("재사용 시 시간 회복량")]
	[SerializeField] private float sRankTimeRecovery = 2f;
	
	[Tooltip("다른 스킬 정신력 감소율")]
	[SerializeField] private float sRankMentalCostReduction = 0.5f;
	
	[Tooltip("다른 스킬 쿨타임 감소율")]
	[SerializeField] private float sRankCooldownReduction = 0.5f;
	
	// S랭크 상태
	private bool isInSRankMode = false;
	private float sRankRemainingTime = 0f;
	
	#endregion
	
	#region 돌진 중 충돌
	
	[Header("돌진 중 충돌 시스템")]
	[Tooltip("돌진 중 적 충돌 감지 반경")]
	[SerializeField] private float collisionDetectionRadius = 0.8f;
	
	[Tooltip("돌진 중 충돌 시 데미지 퍼센테이지")]
	[SerializeField] private float collisionDamagePercent = 50f;
	
	[Tooltip("재사용 활성화에 필요한 적 충돌 수")]
	[SerializeField] private int requiredCollisionCount = 2;
	
	[Tooltip("적 충돌 시 효과음")]
	[SerializeField] private AudioClip enemyCollisionSound;
	
	[Tooltip("적 충돌 시 이펙트")]
	[SerializeField] private GameObject enemyCollisionEffectPrefab;
	
	// 돌진 중 충돌한 적 목록 (중복 방지)
	private HashSet<EntityController> hitEnemiesDuringDash = new HashSet<EntityController>();
	
	#endregion
	
	#region 궤적 폭발
	
	[Header("궤적 폭발 시스템")]
	[Tooltip("궤적 폭발까지 대기 시간 (초)")]
	[SerializeField] private float trailExplosionDelay = 1.5f;
	
	[Tooltip("궤적 폭발 데미지 퍼센테이지")]
	[SerializeField] private float trailExplosionDamagePercent = 40f;
	
	[Tooltip("궤적 폭발 범위")]
	[SerializeField] private float trailExplosionRadius = 1.5f;
	
	[Tooltip("궤적 폭발 이펙트")]
	[SerializeField] private GameObject trailExplosionEffectPrefab;
	
	[Tooltip("궤적 폭발 효과음")]
	[SerializeField] private AudioClip trailExplosionSound;
	
	#endregion
	
	#region 이펙트
	
	[Header("비주얼 이펙트")]
	[Tooltip("대쉬 궤적 이펙트 (바닥에 남는 궤적)")]
	[SerializeField] private GameObject dashTrailPrefab;
	
	[Tooltip("바람을 가르는 이펙트 (플레이어와 함께 돌진)")]
	[SerializeField] private GameObject windSlashEffectPrefab;
	
	[Tooltip("AOE 이펙트")]
	[SerializeField] private GameObject aoeEffectPrefab;
	
	[Tooltip("재사용 가능 표시 이펙트")]
	[SerializeField] private GameObject rechargeReadyEffectPrefab;
	
	[Header("잔상 효과")]
	[Tooltip("잔상 생성 간격 (초)")]
	[SerializeField] private float afterimageInterval = 0.1f;
	
	[Tooltip("잔상 지속 시간 (초)")]
	[SerializeField] private float afterimageDuration = 0.5f;
	
	[Tooltip("잔상 시작 알파값 (0~1)")]
	[SerializeField] private float afterimageStartAlpha = 0.7f;
	
	[Tooltip("잔상 색상 틴트")]
	[SerializeField] private Color afterimageColor = new Color(0.5f, 0.8f, 1f, 1f);
	
	private float lastAfterimageTime = 0f;
	
	[Header("열 왜곡 효과")]
	[Tooltip("열 왜곡 이펙트 프리팹 (AOE 범위에서 배경 왜곡)")]
	[SerializeField] private GameObject heatDistortionPrefab;
	
	[Header("사운드 이펙트")]
	[Tooltip("대쉬 시작 효과음")]
	[SerializeField] private AudioClip dashStartSound;
	
	[Tooltip("AOE 착지 효과음")]
	[SerializeField] private AudioClip aoeImpactSound;
	
	[Tooltip("AOE 착지 효과음 딜레이 (초)")]
	[SerializeField] private float aoeImpactSoundDelay = 0f;
	
	[Header("카메라 쉐이크")]
	[Tooltip("ProCamera2DShake 컴포넌트 (카메라에 부착됨)")]
	[SerializeField] private ProCamera2DShake camShake;
	
	[Tooltip("카메라 쉐이크 강도 (좌우, 상하)")]
	[SerializeField] private Vector2 shakeStrength = new Vector2(0.6f, 0.6f);
	
	[Tooltip("카메라 쉐이크 지속 시간 (초)")]
	[SerializeField] private float shakeDuration = 0.2f;
	
	[Tooltip("카메라 쉐이크 진동 횟수")]
	[SerializeField] private int shakeVibrato = 30;
	
	[Tooltip("카메라 쉐이크 무작위성 (0~1)")]
	[SerializeField] private float shakeRandomness = 0.5f;
	
	// 생성된 이펙트 추적 (정리용)
	private List<GameObject> activeEffects = new List<GameObject>();
	
	#endregion
	
	#region SkillHandlerInterface 구현
	
	public void Setup(BaseSkillData data, GameObject owner)
	{
		this.skillData = data as PlayerSkillData;
		this.owner = owner;
		
		if (owner != null)
		{
			playerController = owner.GetComponent<PlayerController>();
			if (playerController == null)
			{
				playerController = owner.GetComponentInParent<PlayerController>();
			}
		}
		
		if (playerController == null)
		{
			Debug.LogError("[CourageRushHandler] PlayerController를 찾을 수 없습니다!");
		}
		
		// SkillSlot 찾기 (쿨타임 강제 초기화를 위해)
		skillSlot = GetComponentInParent<SkillSlot>();
		if (skillSlot == null)
		{
			Debug.LogWarning("[CourageRushHandler] SkillSlot을 찾을 수 없습니다. 쿨타임 강제 초기화 기능이 작동하지 않을 수 있습니다.");
		}
		
		// ProCamera2DShake 컴포넌트 찾기 (Inspector에서 할당되지 않은 경우)
		if (camShake == null)
		{
			camShake = FindObjectOfType<ProCamera2DShake>();
			if (camShake == null)
			{
				Debug.LogWarning("[CourageRushHandler] ProCamera2DShake 컴포넌트를 찾을 수 없습니다. 카메라 쉐이크가 작동하지 않습니다.");
			}
		}
	}
	
	/// <summary>
	/// 스킬 슬롯 타입 가져오기 (용기 시스템용)
	/// </summary>
	public SkillSlotType? GetSkillSlotType()
	{
		return skillSlot != null ? skillSlot.SlotType : null;
	}
	
	public void Use(SkillRank rank)
	{
		if (skillData == null || playerController == null)
		{
			Debug.LogError("[CourageRushHandler] 스킬 데이터 또는 플레이어 컨트롤러가 없습니다!");
			return;
		}
		
		// ⭐ 정신력 소모 체크는 SkillSlot에서 처리됨
		
		// S랭크 모드 체크
		if (rank == SkillRank.S && !isInSRankMode)
		{
			StartSRankMode();
		}
		
		// 대쉬 실행
		StartCoroutine(ExecuteDash(rank));
	}
	
	public void Cleanup()
	{
		StopSRankMode();
		StopAllCoroutines();
		
		// 생성된 모든 이펙트 즉시 제거
		CleanupAllEffects();
		
		skillData = null;
		owner = null;
		playerController = null;
		skillSlot = null;
	}
	
	/// <summary>
	/// 생성된 모든 이펙트 정리
	/// </summary>
	private void CleanupAllEffects()
	{
		foreach (var effect in activeEffects)
		{
			if (effect != null)
			{
				Destroy(effect);
			}
		}
		activeEffects.Clear();
	}
	
	/// <summary>
	/// 이미 파괴된(null) 이펙트를 리스트에서 제거
	/// </summary>
	private void RemoveNullEffects()
	{
		activeEffects.RemoveAll(effect => effect == null);
	}
	
	/// <summary>
	/// 컴포넌트 비활성화 시 이펙트 정리
	/// </summary>
	private void OnDisable()
	{
		CleanupAllEffects();
	}
	
	/// <summary>
	/// 컴포넌트 파괴 시 이펙트 정리
	/// </summary>
	private void OnDestroy()
	{
		CleanupAllEffects();
	}
	
	#endregion
	
	#region 대쉬 실행
	
	/// <summary>
	/// 대쉬 실행
	/// </summary>
	private IEnumerator ExecuteDash(SkillRank rank)
	{
		// 0. 대쉬 애니메이션 재생 (Attack2)
		if (playerController != null)
		{
			playerController.PlayAttackAnimation(2);
			Debug.Log("[CourageRushHandler] 대쉬 애니메이션 재생 (Attack2)");
		}
		
		// 0-1. 카메라 쉐이크 (대쉬 시작 시)
		if (camShake != null)
		{
			camShake.Shake(shakeDuration, shakeStrength, shakeVibrato, shakeRandomness);
		}
		
		// 0-1. 충돌한 적 목록 초기화
		hitEnemiesDuringDash.Clear();
		
		// 0-2. 잔상 시간 초기화
		lastAfterimageTime = 0f;
		
		// 0-3. 대쉬 시작 효과음 재생
		PlayDashStartSound();
		
		// 1. Rigidbody2D 가져오기 및 물리 상태 초기화
		Rigidbody2D rb = playerController.GetComponent<Rigidbody2D>();
		Vector2 originalVelocity = Vector2.zero;
		
		if (rb != null)
		{
			originalVelocity = rb.linearVelocity;
			rb.linearVelocity = Vector2.zero; // 대쉬 시작 전 속도 초기화
		}
		
		// 1. 대쉬 거리 결정
		float dashDistance = isRechargeAvailable ? secondDashDistance : firstDashDistance;
		
		// 2. 대쉬 방향
		Vector2 dashDirection = playerController.GetLastDirectionVector();
		
		// 3. 대쉬 시작 위치
		Vector3 startPos = playerController.transform.position;
		Vector3 targetPos = startPos + (Vector3)(dashDirection * dashDistance);
		
		// 4. 플레이어의 콜라이더 크기 가져오기 (충돌 체크용)
		Collider2D playerCollider = playerController.GetComponent<Collider2D>();
		float colliderRadius = 0.5f; // 기본값
		if (playerCollider != null)
		{
			if (playerCollider is CircleCollider2D circleCollider)
			{
				colliderRadius = circleCollider.radius * playerController.transform.localScale.x;
			}
			else if (playerCollider is CapsuleCollider2D capsuleCollider)
			{
				colliderRadius = Mathf.Max(capsuleCollider.size.x, capsuleCollider.size.y) * 0.5f * playerController.transform.localScale.x;
			}
			else if (playerCollider is BoxCollider2D boxCollider)
			{
				colliderRadius = Mathf.Max(boxCollider.size.x, boxCollider.size.y) * 0.5f * playerController.transform.localScale.x;
			}
		}
		
		// 5. 장애물 체크 (벽, 장애물만 감지, 트리거 제외)
		// CircleCast를 사용하여 플레이어 크기를 고려한 충돌 체크
		RaycastHit2D hit = Physics2D.CircleCast(
			startPos, 
			colliderRadius * 0.95f, // 약간 작게 하여 정밀도 향상
			dashDirection, 
			dashDistance, 
			obstacleLayerMask, // 설정한 레이어만 감지
			-Mathf.Infinity, 
			Mathf.Infinity
		);
		
		// 장애물 감지 시 해당 지점까지만 이동
		if (hit.collider != null)
		{
			// 벽 직전에 멈춤 (충분한 여유 공간 확보하여 끼임 방지)
			float safeDistance = Mathf.Max(0, hit.distance - colliderRadius * 0.5f);
			targetPos = startPos + (Vector3)(dashDirection * safeDistance);
			
			Debug.Log($"[CourageRush] 장애물 감지! 거리: {hit.distance}, 안전 거리: {safeDistance}");
		}
		
		// 6. 무적 설정
		if (invincibleDuringDash)
		{
			playerController.SetInvincible(true);
		}
		
		// 7. 대쉬 이동 (매 프레임마다 충돌 체크)
		float totalDistance = Vector2.Distance(startPos, targetPos);
		
		// 목표 지점이 시작 지점과 거의 같다면 (벽에 막혔다면) 대쉬하지 않음
		if (totalDistance < 0.1f)
		{
			if (invincibleDuringDash)
			{
				playerController.SetInvincible(false);
			}
			
			// 물리 상태 복원
			if (rb != null)
			{
				rb.linearVelocity = Vector2.zero;
			}
			
			Debug.Log("[CourageRush] 대쉬 거리가 너무 짧아 취소됨");
			yield break;
		}
		
		float dashDuration = totalDistance / dashSpeed;
		float elapsedTime = 0f;
		Vector3 currentPos = startPos;
		
		// 대쉬 궤적 이펙트 생성 (바닥에 남는 궤적)
		GameObject trail = null;
		if (dashTrailPrefab != null)
		{
			// 돌진 방향을 바라보도록 회전 설정
			float angle = Mathf.Atan2(dashDirection.y, dashDirection.x) * Mathf.Rad2Deg;
			Quaternion rotation = Quaternion.Euler(0, 0, angle);
			
			trail = Instantiate(dashTrailPrefab, playerController.transform.position, rotation);
			trail.transform.SetParent(playerController.transform);
			
			// 이펙트 추적 리스트에 추가
			activeEffects.Add(trail);
		}
		
		// 바람을 가르는 이펙트 생성 (플레이어와 함께 돌진)
		GameObject windEffect = null;
		if (windSlashEffectPrefab != null)
		{
			// 돌진 방향을 바라보도록 회전 설정
			float angle = Mathf.Atan2(dashDirection.y, dashDirection.x) * Mathf.Rad2Deg;
			Quaternion rotation = Quaternion.Euler(0, 0, angle);
			
			windEffect = Instantiate(windSlashEffectPrefab, playerController.transform.position, rotation);
			windEffect.transform.SetParent(playerController.transform);
			
			// 이펙트 추적 리스트에 추가
			activeEffects.Add(windEffect);
			
			Debug.Log($"[CourageRush] 바람 이펙트 생성 (각도: {angle}도)");
		}
		
		// 궤적 폭발을 위한 경로 저장
		List<Vector3> dashPath = new List<Vector3>();
		dashPath.Add(startPos);
		
		while (elapsedTime < dashDuration)
		{
			elapsedTime += Time.deltaTime;
			float t = Mathf.Clamp01(elapsedTime / dashDuration);
			Vector3 nextPos = Vector3.Lerp(startPos, targetPos, t);
			
			// 다음 위치로 이동 가능한지 체크
			Vector2 moveDirection = (nextPos - currentPos).normalized;
			float moveDistance = Vector3.Distance(currentPos, nextPos);
			
			if (moveDistance > 0.001f) // 이동 거리가 있을 때만 체크
			{
				// 장애물 레이어만 체크 (트리거 자동 무시)
				RaycastHit2D moveHit = Physics2D.CircleCast(
					currentPos, 
					colliderRadius * 0.9f, // 약간 작게 체크
					moveDirection, 
					moveDistance,
					obstacleLayerMask // 장애물 레이어만
				);
				
				// 장애물에 부딪혔다면 해당 위치에서 멈춤
				if (moveHit.collider != null)
				{
					// 충돌 지점 직전까지만 이동 (충분한 여유 공간 확보)
					float safeDistance = Mathf.Max(0, moveHit.distance - colliderRadius * 0.6f);
					if (safeDistance > 0.001f)
					{
						nextPos = currentPos + (Vector3)(moveDirection * safeDistance);
						playerController.transform.position = nextPos;
					}
					
					Debug.Log($"[CourageRush] 대쉬 중 장애물 충돌! 정지");
					break; // 대쉬 중단
				}
			}
			
			currentPos = nextPos;
			playerController.transform.position = currentPos;
			
			// 경로 저장 (일정 간격마다)
			if (Vector3.Distance(dashPath[dashPath.Count - 1], currentPos) > 0.5f)
			{
				dashPath.Add(currentPos);
			}
			
			// 돌진 중 적 충돌 감지
			CheckEnemyCollisionDuringDash(currentPos, rank);
			
			// ⭐ 잔상 생성 (일정 간격마다)
			if (Time.time - lastAfterimageTime >= afterimageInterval)
			{
				CreateAfterimage();
				lastAfterimageTime = Time.time;
			}
			
			yield return null;
		}
		
		// 최종 위치 확정 및 안전성 검증
		Vector3 finalPosition = playerController.transform.position;
		
		// 최종 위치에서 주변 장애물 체크 (벽에 끼이지 않았는지 확인)
		Collider2D[] overlapCheck = Physics2D.OverlapCircleAll(finalPosition, colliderRadius * 0.8f, obstacleLayerMask);
		if (overlapCheck.Length > 0)
		{
			// 벽에 끼였다면 안전한 위치로 이동 (시작 위치 방향으로 약간 후퇴)
			Vector2 escapeDirection = (startPos - finalPosition).normalized;
			finalPosition = finalPosition + (Vector3)(escapeDirection * colliderRadius);
			playerController.transform.position = finalPosition;
		}
		
		// 8. 무적 해제
		if (invincibleDuringDash)
		{
			playerController.SetInvincible(false);
		}
		
		// 9. 대쉬 궤적 이펙트 정리
		if (trail != null)
		{
			trail.transform.SetParent(null);
			Destroy(trail, 2f);
		}
		
		// 9-1. 바람 이펙트 정리
		if (windEffect != null)
		{
			windEffect.transform.SetParent(null);
			Destroy(windEffect, 0.5f); // 짧은 시간 후 제거
		}
		
		// 10. 물리 상태 복원
		if (rb != null)
		{
			rb.linearVelocity = Vector2.zero; // 대쉬 후 속도 초기화
		}
		
		// 10-1. 최종 경로 위치 추가
		if (dashPath.Count == 0 || Vector3.Distance(dashPath[dashPath.Count - 1], finalPosition) > 0.1f)
		{
			dashPath.Add(finalPosition);
		}
		
		// 10-2. 궤적 폭발 코루틴 시작
		if (dashPath.Count > 1)
		{
			StartCoroutine(TrailExplosionCoroutine(dashPath, rank));
		}
		
		// 11. 도착 지점 AOE 효과 (실제 도착한 위치에서 발동)
		ApplyAOEEffect(finalPosition, rank);
		
		// 12. 돌진 중 충돌한 적이 2명 이상이면 즉시 재사용 가능
		if (hitEnemiesDuringDash.Count >= requiredCollisionCount)
		{
			isRechargeAvailable = true;
			Debug.Log($"[CourageRush] {hitEnemiesDuringDash.Count}명의 적과 충돌! 즉시 재사용 가능!");
			
			// ⭐ 쿨타임 강제 초기화 (즉시 재사용 가능하게 만듦)
			if (skillSlot != null)
			{
				skillSlot.ResetCooldown();
			}
			else
			{
				Debug.LogWarning("[CourageRush] SkillSlot이 없어서 쿨타임을 초기화할 수 없습니다!");
			}
			
			// 재사용 가능 이펙트 표시
			if (rechargeReadyEffectPrefab != null && playerController != null)
			{
				GameObject effect = Instantiate(rechargeReadyEffectPrefab, playerController.transform.position, Quaternion.identity);
				effect.transform.SetParent(playerController.transform);
				
				// 이펙트 추적 리스트에 추가
				activeEffects.Add(effect);
				
				Destroy(effect, 2f);
			}
		}
		
		// 13. 재사용 상태 업데이트
		if (isRechargeAvailable && hitEnemiesDuringDash.Count < requiredCollisionCount)
		{
			// 재사용 소모 (충돌로 인한 재사용이 아닌 경우에만)
			isRechargeAvailable = false;
			currentRechargeCount--;
			
			// S랭크 모드 시간 회복
			if (isInSRankMode)
			{
				sRankRemainingTime += sRankTimeRecovery;
			}
		}
		else if (!isRechargeAvailable && hitEnemiesDuringDash.Count < requiredCollisionCount)
		{
			// 1차 사용 후 재사용 준비
			currentRechargeCount = maxRechargeCount;
		}
		
		// 14. 충돌한 적 목록 초기화 (다음 대쉬를 위해)
		// hitEnemiesDuringDash.Clear(); // 다음 대쉬 시작 시 초기화하므로 여기서는 불필요
		
		// 15. 이미 파괴된 이펙트를 리스트에서 제거 (메모리 관리)
		RemoveNullEffects();
	}
	
	#endregion
	
	#region AOE 효과
	
	/// <summary>
	/// 도착 지점 AOE 효과 적용
	/// </summary>
	private void ApplyAOEEffect(Vector3 position, SkillRank rank)
	{
		// 0. AOE 착지 효과음 재생 (딜레이 적용)
		if (aoeImpactSoundDelay > 0f)
		{
			StartCoroutine(PlayAOEImpactSoundDelayed(aoeImpactSoundDelay));
		}
		else
		{
			PlayAOEImpactSound();
		}
		
		// 1. 데미지 계산
		float damage = CalculateDamage(rank);
		
		// 2. 범위 내 적 탐지
		Collider2D[] colliders = Physics2D.OverlapCircleAll(position, aoeRadius);
		
		foreach (var collider in colliders)
		{
			EntityController entity = collider.GetComponent<EntityController>();
			if (entity == null || !entity.IsAlive) continue;
			if (entity.EntityType != EntityType.Monster) continue;
			
			// 데미지 적용
			int finalDamage = Mathf.RoundToInt(damage);
			entity.ApplyDamage(finalDamage, EntityType.Player, this);
			
			// 둔화 효과 적용
			if (slowEffect != null)
			{
				entity.ApplyStatusEffect(slowEffect);
			}
			
			// A랭크 이상: 기절 효과
			if (rank >= SkillRank.A && stunEffect != null)
			{
				entity.ApplyStatusEffect(stunEffect);
			}
		}
		
		// 3. AOE 이펙트 생성
		if (aoeEffectPrefab != null)
		{
			GameObject aoeEffect = Instantiate(aoeEffectPrefab, position, Quaternion.identity);
			
			// 크기 조정
			aoeEffect.transform.localScale = Vector3.one * (aoeRadius * 2f);
			
			// 이펙트 추적 리스트에 추가
			activeEffects.Add(aoeEffect);
			
			Destroy(aoeEffect, aoeDuration);
		}
		
		// 4. 열 왜곡 효과 생성
		if (heatDistortionPrefab != null)
		{
			GameObject distortion = Instantiate(heatDistortionPrefab, position, Quaternion.identity);
			
			// 크기 조정 (AOE 범위에 맞춤)
			distortion.transform.localScale = Vector3.one * (aoeRadius * 2f);
			
			// 이펙트 추적 리스트에 추가
			activeEffects.Add(distortion);
			
			// 열 왜곡 효과는 AOE보다 조금 더 오래 유지
			Destroy(distortion, aoeDuration + 1f);
			
			Debug.Log("[CourageRush] 열 왜곡 효과 생성");
		}
	}
	
	#endregion
	
	#region 돌진 중 충돌 처리
	
	/// <summary>
	/// 돌진 중 적 충돌 감지 및 처리
	/// </summary>
	private void CheckEnemyCollisionDuringDash(Vector3 currentPosition, SkillRank rank)
	{
		// 현재 위치에서 반경 내 적 탐지
		Collider2D[] colliders = Physics2D.OverlapCircleAll(currentPosition, collisionDetectionRadius);
		
		foreach (var collider in colliders)
		{
			EntityController entity = collider.GetComponent<EntityController>();
			if (entity == null || !entity.IsAlive) continue;
			if (entity.EntityType != EntityType.Monster) continue;
			
			// 이미 충돌한 적은 무시 (중복 방지)
			if (hitEnemiesDuringDash.Contains(entity))
			{
				continue;
			}
			
			// 충돌 목록에 추가
			hitEnemiesDuringDash.Add(entity);
			
			// 데미지 계산 및 적용
			float damage = CalculateCollisionDamage(rank);
			int finalDamage = Mathf.RoundToInt(damage);
			entity.ApplyDamage(finalDamage, EntityType.Player, this);
			
			Debug.Log($"[CourageRush] 돌진 중 적 충돌! {entity.name}에게 {finalDamage} 데미지 (총 {hitEnemiesDuringDash.Count}명 충돌)");
			
			// 충돌 이펙트 생성
			if (enemyCollisionEffectPrefab != null)
			{
				GameObject effect = Instantiate(enemyCollisionEffectPrefab, entity.transform.position, Quaternion.identity);
				
				// 이펙트 추적 리스트에 추가
				activeEffects.Add(effect);
				
				Destroy(effect, 1f);
			}
			
			// 충돌 효과음 재생
			PlayEnemyCollisionSound();
		}
	}
	
	/// <summary>
	/// 돌진 중 충돌 데미지 계산
	/// </summary>
	private float CalculateCollisionDamage(SkillRank rank)
	{
		// 1. 기본 데미지
		float damage = playerController.CalculateSkillDamage(collisionDamagePercent);
		
		// 2. 랭크 배율
		float rankMultiplier = skillData.GetDamageMultiplier(rank);
		damage *= rankMultiplier;
		
		return damage;
	}
	
	#endregion
	
	#region 궤적 폭발
	
	/// <summary>
	/// 궤적 폭발 코루틴
	/// 대쉬 경로를 따라 일정 시간 후 폭발합니다.
	/// </summary>
	private IEnumerator TrailExplosionCoroutine(List<Vector3> path, SkillRank rank)
	{
		// 폭발 대기
		yield return new WaitForSeconds(trailExplosionDelay);
		
		Debug.Log($"[CourageRush] 궤적 폭발 시작! 경로 포인트: {path.Count}개");
		
		// 각 경로 포인트에서 폭발
		foreach (var position in path)
		{
			// 폭발 데미지 계산
			float explosionDamage = CalculateTrailExplosionDamage(rank);
			
			// 범위 내 적 탐지
			Collider2D[] colliders = Physics2D.OverlapCircleAll(position, trailExplosionRadius);
			
			foreach (var collider in colliders)
			{
				EntityController entity = collider.GetComponent<EntityController>();
				if (entity == null || !entity.IsAlive) continue;
				if (entity.EntityType != EntityType.Monster) continue;
				
				// 데미지 적용
				int finalDamage = Mathf.RoundToInt(explosionDamage);
				entity.ApplyDamage(finalDamage, EntityType.Player, this);
				
				Debug.Log($"[CourageRush] 궤적 폭발! {entity.name}에게 {finalDamage} 데미지");
			}
			
			// 폭발 이펙트 생성
			if (trailExplosionEffectPrefab != null)
			{
				GameObject effect = Instantiate(trailExplosionEffectPrefab, position, Quaternion.identity);
				effect.transform.localScale = Vector3.one * (trailExplosionRadius * 2f);
				
				// 이펙트 추적 리스트에 추가
				activeEffects.Add(effect);
				
				Destroy(effect, 1.5f);
			}
			
			// 폭발 효과음 재생 (첫 번째 폭발만)
			if (position == path[0])
			{
				PlayTrailExplosionSound();
			}
			
			// 각 폭발 사이 짧은 딜레이 (연쇄 폭발 느낌)
			yield return new WaitForSeconds(0.05f);
		}
		
		Debug.Log($"[CourageRush] 궤적 폭발 완료!");
	}
	
	/// <summary>
	/// 궤적 폭발 데미지 계산
	/// </summary>
	private float CalculateTrailExplosionDamage(SkillRank rank)
	{
		// 1. 기본 데미지
		float damage = playerController.CalculateSkillDamage(trailExplosionDamagePercent);
		
		// 2. 랭크 배율
		float rankMultiplier = skillData.GetDamageMultiplier(rank);
		damage *= rankMultiplier;
		
		return damage;
	}
	
	#endregion
	
	#region 재사용 시스템
	
	/// <summary>
	/// 다른 스킬 적중 시 호출 (재사용 활성화)
	/// </summary>
	public void OnOtherSkillHit()
	{
		if (currentRechargeCount > 0)
		{
			isRechargeAvailable = true;
			
			// 재사용 가능 이펙트
			if (rechargeReadyEffectPrefab != null && playerController != null)
			{
				GameObject effect = Instantiate(rechargeReadyEffectPrefab, playerController.transform.position, Quaternion.identity);
				effect.transform.SetParent(playerController.transform);
				
				// 이펙트 추적 리스트에 추가
				activeEffects.Add(effect);
				
				Destroy(effect, 2f);
			}
		}
	}
	
	/// <summary>
	/// 재사용 가능 여부
	/// </summary>
	public bool IsRechargeAvailable => isRechargeAvailable;
	
	#endregion
	
	#region 계산
	
	/// <summary>
	/// 데미지 계산
	/// </summary>
	private float CalculateDamage(SkillRank rank)
	{
		// 1. 기본 데미지
		float damage = playerController.CalculateSkillDamage(baseDamagePercent);
		
		// 2. 랭크 배율
		float rankMultiplier = skillData.GetDamageMultiplier(rank);
		damage *= rankMultiplier;
		
		// 3. 재사용 시 데미지 증가
		if (isRechargeAvailable)
		{
			damage *= rechargeDamageMultiplier;
		}
		
		return damage;
	}
	
	/// <summary>
	/// 정신력 소모 계산 (SkillSlot에서 호출)
	/// </summary>
	public int CalculateMentalCost(SkillRank rank)
	{
		int cost = skillData.mentalCost;
		
		// S랭크 모드: 정신력 감소
		if (isInSRankMode)
		{
			cost = Mathf.RoundToInt(cost * sRankMentalCostReduction);
		}
		
		// 랭크별 소모 감소 (A랭크 이상)
		if (rank >= SkillRank.A)
		{
			cost = Mathf.RoundToInt(cost * 0.8f);
		}
		
		return cost;
	}
	
	#endregion
	
	#region S랭크 모드
	
	/// <summary>
	/// S랭크 모드 시작
	/// </summary>
	private void StartSRankMode()
	{
		isInSRankMode = true;
		sRankRemainingTime = sRankBaseDuration;
		
		StartCoroutine(SRankModeTimer());
	}
	
	/// <summary>
	/// S랭크 모드 타이머
	/// </summary>
	private IEnumerator SRankModeTimer()
	{
		while (isInSRankMode && sRankRemainingTime > 0f)
		{
			sRankRemainingTime -= Time.deltaTime;
			yield return null;
		}
		
		StopSRankMode();
	}
	
	/// <summary>
	/// S랭크 모드 종료
	/// </summary>
	private void StopSRankMode()
	{
		if (!isInSRankMode) return;
		
		isInSRankMode = false;
		sRankRemainingTime = 0f;
	}
	
	/// <summary>
	/// S랭크 모드 활성 여부
	/// </summary>
	public bool IsInSRankMode => isInSRankMode;
	
	/// <summary>
	/// S랭크 쿨타임 감소율 (다른 스킬용)
	/// </summary>
	public float GetSRankCooldownReduction() => isInSRankMode ? sRankCooldownReduction : 1f;
	
	/// <summary>
	/// S랭크 정신력 감소율 (다른 스킬용)
	/// </summary>
	public float GetSRankMentalCostReduction() => isInSRankMode ? sRankMentalCostReduction : 1f;
	
	#endregion
	
	#region 사운드 재생
	
	/// <summary>
	/// 대쉬 시작 효과음 재생
	/// </summary>
	private void PlayDashStartSound()
	{
		if (dashStartSound != null && playerController != null)
		{
			AudioSource audioSource = playerController.GetComponent<AudioSource>();
			if (audioSource == null)
			{
				audioSource = playerController.gameObject.AddComponent<AudioSource>();
				audioSource.playOnAwake = false;
				audioSource.spatialBlend = 0f; // 2D 사운드
			}
			audioSource.PlayOneShot(dashStartSound);
			Debug.Log("[CourageRushHandler] 대쉬 시작 효과음 재생");
		}
	}
	
	/// <summary>
	/// AOE 착지 효과음 재생
	/// </summary>
	private void PlayAOEImpactSound()
	{
		if (aoeImpactSound != null && playerController != null)
		{
			AudioSource audioSource = playerController.GetComponent<AudioSource>();
			if (audioSource == null)
			{
				audioSource = playerController.gameObject.AddComponent<AudioSource>();
				audioSource.playOnAwake = false;
				audioSource.spatialBlend = 0f; // 2D 사운드
			}
			audioSource.PlayOneShot(aoeImpactSound);
			Debug.Log("[CourageRushHandler] AOE 착지 효과음 재생");
		}
	}
	
	/// <summary>
	/// AOE 착지 효과음 딜레이 후 재생
	/// </summary>
	private IEnumerator PlayAOEImpactSoundDelayed(float delay)
	{
		yield return new WaitForSeconds(delay);
		PlayAOEImpactSound();
	}
	
	/// <summary>
	/// 적 충돌 효과음 재생
	/// </summary>
	private void PlayEnemyCollisionSound()
	{
		if (enemyCollisionSound != null && playerController != null)
		{
			AudioSource audioSource = playerController.GetComponent<AudioSource>();
			if (audioSource == null)
			{
				audioSource = playerController.gameObject.AddComponent<AudioSource>();
				audioSource.playOnAwake = false;
				audioSource.spatialBlend = 0f; // 2D 사운드
			}
			audioSource.PlayOneShot(enemyCollisionSound, 0.7f); // 볼륨 약간 낮춤
		}
	}
	
	/// <summary>
	/// 궤적 폭발 효과음 재생
	/// </summary>
	private void PlayTrailExplosionSound()
	{
		if (trailExplosionSound != null && playerController != null)
		{
			AudioSource audioSource = playerController.GetComponent<AudioSource>();
			if (audioSource == null)
			{
				audioSource = playerController.gameObject.AddComponent<AudioSource>();
				audioSource.playOnAwake = false;
				audioSource.spatialBlend = 0f; // 2D 사운드
			}
			audioSource.PlayOneShot(trailExplosionSound);
			Debug.Log("[CourageRushHandler] 궤적 폭발 효과음 재생");
		}
	}
	
	#endregion
	
	#region 잔상 효과
	
	/// <summary>
	/// 플레이어의 잔상 생성
	/// </summary>
	private void CreateAfterimage()
	{
		if (playerController == null) return;
		
		// 플레이어의 SpriteRenderer 찾기
		SpriteRenderer playerSprite = playerController.GetComponentInChildren<SpriteRenderer>();
		if (playerSprite == null) return;
		
		// 잔상 GameObject 생성
		GameObject afterimageObj = new GameObject("Afterimage");
		afterimageObj.transform.position = playerController.transform.position;
		afterimageObj.transform.rotation = playerController.transform.rotation;
		afterimageObj.transform.localScale = playerController.transform.localScale;
		
		// SpriteRenderer 추가 및 설정
		SpriteRenderer afterimageSprite = afterimageObj.AddComponent<SpriteRenderer>();
		afterimageSprite.sprite = playerSprite.sprite;
		afterimageSprite.sortingLayerName = playerSprite.sortingLayerName;
		afterimageSprite.sortingOrder = playerSprite.sortingOrder - 1; // 플레이어보다 뒤에 표시
		afterimageSprite.flipX = playerSprite.flipX;
		afterimageSprite.flipY = playerSprite.flipY;
		
		// 색상 및 알파값 설정
		Color color = afterimageColor;
		color.a = afterimageStartAlpha;
		afterimageSprite.color = color;
		
		// AfterimageEffect 컴포넌트 추가 (페이드 아웃 처리)
		AfterimageEffect afterimageEffect = afterimageObj.AddComponent<AfterimageEffect>();
		afterimageEffect.duration = afterimageDuration;
		afterimageEffect.startAlpha = afterimageStartAlpha;
	}
	
	#endregion
}

