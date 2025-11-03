using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
	
	#region 이펙트
	
	[Header("비주얼 이펙트")]
	[Tooltip("대쉬 궤적 이펙트")]
	[SerializeField] private GameObject dashTrailPrefab;
	
	[Tooltip("AOE 이펙트")]
	[SerializeField] private GameObject aoeEffectPrefab;
	
	[Tooltip("재사용 가능 표시 이펙트")]
	[SerializeField] private GameObject rechargeReadyEffectPrefab;
	
	[Header("사운드 이펙트")]
	[Tooltip("대쉬 시작 효과음")]
	[SerializeField] private AudioClip dashStartSound;
	
	[Tooltip("AOE 착지 효과음")]
	[SerializeField] private AudioClip aoeImpactSound;
	
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
	}
	
	public void Use(SkillRank rank)
	{
		if (skillData == null || playerController == null)
		{
			Debug.LogError("[CourageRushHandler] 스킬 데이터 또는 플레이어 컨트롤러가 없습니다!");
			return;
		}
		
		// 정신력 소모 체크
		int mentalCost = CalculateMentalCost(rank);
		if (!playerController.TryConsumeMentalPower(mentalCost))
		{
			return;
		}
		
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
		
		skillData = null;
		owner = null;
		playerController = null;
	}
	
	#endregion
	
	#region 대쉬 실행
	
	/// <summary>
	/// 대쉬 실행
	/// </summary>
	private IEnumerator ExecuteDash(SkillRank rank)
	{
		// 0. 대쉬 시작 효과음 재생
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
			
			Debug.LogWarning("[CourageRush] 벽 끼임 감지! 안전한 위치로 이동");
		}
		
		// 8. 무적 해제
		if (invincibleDuringDash)
		{
			playerController.SetInvincible(false);
		}
		
		// 9. 물리 상태 복원
		if (rb != null)
		{
			rb.linearVelocity = Vector2.zero; // 대쉬 후 속도 초기화
		}
		
		// 10. 도착 지점 AOE 효과 (실제 도착한 위치에서 발동)
		ApplyAOEEffect(finalPosition, rank);
		
		// 11. 재사용 상태 업데이트
		if (isRechargeAvailable)
		{
			// 재사용 소모
			isRechargeAvailable = false;
			currentRechargeCount--;
			
			// S랭크 모드 시간 회복
			if (isInSRankMode)
			{
				sRankRemainingTime += sRankTimeRecovery;
			}
		}
		else
		{
			// 1차 사용 후 재사용 준비
			currentRechargeCount = maxRechargeCount;
		}
	}
	
	#endregion
	
	#region AOE 효과
	
	/// <summary>
	/// 도착 지점 AOE 효과 적용
	/// </summary>
	private void ApplyAOEEffect(Vector3 position, SkillRank rank)
	{
		// 0. AOE 착지 효과음 재생
		PlayAOEImpactSound();
		
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
			
			Destroy(aoeEffect, aoeDuration);
		}
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
	/// 정신력 소모 계산
	/// </summary>
	private int CalculateMentalCost(SkillRank rank)
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
	
	#endregion
}

