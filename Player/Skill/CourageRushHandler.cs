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
		// 1. 대쉬 거리 결정
		float dashDistance = isRechargeAvailable ? secondDashDistance : firstDashDistance;
		
		// 2. 대쉬 방향
		Vector2 dashDirection = playerController.GetLastDirectionVector();
		
		// 3. 대쉬 시작 위치
		Vector3 startPos = playerController.transform.position;
		Vector3 targetPos = startPos + (Vector3)(dashDirection * dashDistance);
		
		// 4. 장애물 체크 (벽을 뚫지 않도록)
		RaycastHit2D hit = Physics2D.Raycast(startPos, dashDirection, dashDistance, LayerMask.GetMask("Wall"));
		if (hit.collider != null)
		{
			targetPos = hit.point - dashDirection * 0.5f; // 벽 직전에 멈춤
		}
		
		// 5. 무적 설정
		if (invincibleDuringDash)
		{
			playerController.SetInvincible(true);
		}
		
		// 6. 대쉬 이동
		float dashDuration = Vector2.Distance(startPos, targetPos) / dashSpeed;
		float elapsedTime = 0f;
		
		while (elapsedTime < dashDuration)
		{
			elapsedTime += Time.deltaTime;
			float t = elapsedTime / dashDuration;
			playerController.transform.position = Vector3.Lerp(startPos, targetPos, t);
			yield return null;
		}
		
		playerController.transform.position = targetPos;
		
		// 7. 무적 해제
		if (invincibleDuringDash)
		{
			playerController.SetInvincible(false);
		}
		
		// 8. 도착 지점 AOE 효과
		ApplyAOEEffect(targetPos, rank);
		
		// 9. 재사용 상태 업데이트
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
}

