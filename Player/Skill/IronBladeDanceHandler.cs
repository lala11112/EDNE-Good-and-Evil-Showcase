using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 거철의 검무 스킬 핸들러
/// 전방 부채꼴 범위 공격으로 적에게 데미지를 입히고 탄환을 무력화합니다
/// 스트라이크 존에 적을 맞추면 추가 데미지와 스턴을 적용합니다
/// </summary>
public class IronBladeDanceHandler : MonoBehaviour, SkillHandlerInterface
{
	#region 기본 설정
	
	[Header("기본 데이터")]
	[Tooltip("스킬 데이터 (Setup에서 자동 설정)")]
	private PlayerSkillData skillData;
	
	[Tooltip("스킬 소유자 GameObject")]
	private GameObject owner;
	
	[Tooltip("플레이어 컨트롤러")]
	private PlayerController playerController;
	
	#endregion
	
	#region 범위 설정
	
	[Header("부채꼴 범위 설정")]
	[Tooltip("부채꼴 각도 (170도)")]
	[SerializeField] private float fanAngle = 170f;
	
	[Tooltip("기본 범위 반경")]
	[SerializeField] private float baseRange = 3f;
	
	[Tooltip("스트라이크 존 두께 (범위 끝에서부터)")]
	[SerializeField] private float strikeZoneThickness = 0.5f;
	
	#endregion
	
	#region 데미지 설정
	
	[Header("데미지 설정")]
	[Tooltip("기본 데미지 퍼센테이지")]
	[SerializeField] private float baseDamagePercent = 150f;
	
	[Tooltip("스트라이크 존 데미지 배율")]
	[SerializeField] private float strikeZoneDamageMultiplier = 2.5f;
	
	#endregion
	
	#region 스턴 설정
	
	[Header("스턴 효과")]
	[Tooltip("스턴 상태효과 데이터 (스트라이크 존 적중 시)")]
	[SerializeField] private StatusEffectDataSO stunEffect;
	
	#endregion
	
	#region S랭크 특수 메커니즘
	
	[Header("S랭크(확신) 특수 효과")]
	[Tooltip("S랭크 이동속도 증가 배율")]
	[SerializeField] private float sRankMoveSpeedMultiplier = 1.3f;
	
	[Tooltip("S랭크 추가 데미지 배율")]
	[SerializeField] private float sRankDamageBonus = 1.5f;
	
	[Tooltip("S랭크 스트라이크 존 범위 증가 배율")]
	[SerializeField] private float sRankStrikeZoneMultiplier = 1.5f;
	
	[Tooltip("연속 적중 시 추가 데미지 증가량 (%)")]
	[SerializeField] private float comboBonus = 10f;
	
	[Tooltip("연속 적중 시 사용시간 증가량 (초)")]
	[SerializeField] private float comboTimeBonus = 2f;
	
	[Tooltip("최대 연속 적중 횟수")]
	[SerializeField] private int maxComboCount = 5;
	
	// S랭크 상태 변수
	private bool isInSRankMode = false;
	private int comboCount = 0;
	private float sRankRemainingTime = 0f;
	private float sRankBaseDuration = 10f;
	
	#endregion
	
	#region 이펙트
	
	[Header("비주얼 이펙트")]
	[Tooltip("검무 이펙트 프리팹 (선택 사항)")]
	[SerializeField] private GameObject bladeDanceEffectPrefab;
	
	[Tooltip("스트라이크 존 이펙트 프리팹 (선택 사항)")]
	[SerializeField] private GameObject strikeZoneEffectPrefab;
	
	[Tooltip("이펙트 지속 시간")]
	[SerializeField] private float effectDuration = 0.5f;
	
	[Header("파티클 이펙트")]
	[Tooltip("일반 범위 파티클 프리팹 (선택 사항)")]
	[SerializeField] private GameObject normalAreaParticlePrefab;
	
	[Tooltip("스트라이크 존 파티클 프리팹 (선택 사항)")]
	[SerializeField] private GameObject strikeZoneParticlePrefab;
	
	[Tooltip("범위 경계선 파티클 프리팹 (선택 사항)")]
	[SerializeField] private GameObject edgeParticlePrefab;
	
	[Tooltip("파티클 생성 개수 (범위 전체)")]
	[SerializeField] private int particleSpawnCount = 20;
	
	[Tooltip("스트라이크 존 파티클 개수")]
	[SerializeField] private int strikeZoneParticleCount = 15;
	
	[Header("사운드 이펙트")]
	[Tooltip("검무 휘두르기 효과음")]
	[SerializeField] private AudioClip bladeDanceSound;
	
	[Tooltip("스트라이크 존 적중 효과음")]
	[SerializeField] private AudioClip strikeZoneHitSound;
	
	#endregion
	
	#region SkillHandlerInterface 구현
	
	/// <summary>
	/// 스킬 핸들러 초기 설정
	/// </summary>
	public void Setup(BaseSkillData data, GameObject owner)
	{
		this.skillData = data as PlayerSkillData;
		this.owner = owner;
		
		// PlayerController 가져오기
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
			Debug.LogError("[IronBladeDanceHandler] PlayerController를 찾을 수 없습니다!");
		}
		
		Debug.Log("[IronBladeDanceHandler] Setup 완료");
	}
	
	/// <summary>
	/// 스킬 사용
	/// </summary>
	public void Use(SkillRank rank)
	{
		if (skillData == null || playerController == null)
		{
			Debug.LogError("[IronBladeDanceHandler] 스킬 데이터 또는 플레이어 컨트롤러가 없습니다!");
			return;
		}
		
		// 정신력 소모 체크
		if (!playerController.TryConsumeMentalPower(skillData.mentalCost))
		{
			Debug.Log($"[IronBladeDanceHandler] 정신력이 부족합니다! (필요: {skillData.mentalCost})");
			return;
		}
		
		// S랭크 모드 체크 및 시작
		if (rank == SkillRank.S && !isInSRankMode)
		{
			StartSRankMode();
		}
		
		// 스킬 실행
		ExecuteBladeDance(rank);
		
		Debug.Log($"[IronBladeDanceHandler] 거철의 검무 사용 (랭크: {rank})");
	}
	
	/// <summary>
	/// 스킬 정리
	/// </summary>
	public void Cleanup()
	{
		// S랭크 모드 종료
		StopSRankMode();
		
		// 진행 중인 코루틴 정리
		StopAllCoroutines();
		
		// 상태 초기화
		skillData = null;
		owner = null;
		playerController = null;
	}
	
	#endregion
	
	#region 검무 실행
	
	/// <summary>
	/// 거철의 검무 실행
	/// </summary>
	private void ExecuteBladeDance(SkillRank rank)
	{
		// 0. 검무 휘두르기 효과음 재생
		PlayBladeDanceSound();
		
		// 1. 범위 계산 (랭크별)
		float currentRange = CalculateRange(rank);
		float currentStrikeZoneThickness = strikeZoneThickness;
		
		// S랭크에서 스트라이크 존 범위 증가
		if (isInSRankMode)
		{
			currentStrikeZoneThickness *= sRankStrikeZoneMultiplier;
		}
		
		// 2. 데미지 계산 (랭크별)
		float baseDamage = CalculateBaseDamage(rank);
		
		// 3. 플레이어가 바라보는 방향 가져오기 (현재 입력 방향 우선, 없으면 마지막 방향)
		Vector2 attackDirection = GetCurrentAimDirection();
		
		// 4. 이펙트 먼저 생성 (즉시 표시)
		SpawnVisualEffects(attackDirection, currentRange, currentStrikeZoneThickness);
		
		// 5. 부채꼴 범위 내 적 탐지 (이펙트와 동시)
		List<EntityController> hitEnemies = DetectEnemiesInFan(attackDirection, currentRange);
		
		// 6. 탄환 무력화 (이펙트와 동시)
		DestroyProjectilesInFan(attackDirection, currentRange);
		
		// 7. 적에게 데미지 적용 (즉시)
		bool strikeZoneHit = false;
		foreach (var enemy in hitEnemies)
		{
			// 스트라이크 존 체크
			bool isInStrikeZone = CheckStrikeZone(enemy.transform.position, playerController.transform.position, 
				attackDirection, currentRange, currentStrikeZoneThickness);
			
			if (isInStrikeZone)
			{
				// 스트라이크 존 적중 - 강화 데미지 및 스턴
				ApplyStrikeZoneDamage(enemy, baseDamage);
				strikeZoneHit = true;
			}
			else
			{
				// 일반 데미지
				ApplyNormalDamage(enemy, baseDamage);
			}
		}
		
		// 8. S랭크 모드 업데이트
		if (isInSRankMode)
		{
			UpdateSRankMode(strikeZoneHit);
		}
	}
	
	/// <summary>
	/// 현재 조준 방향 가져오기 (플레이어가 바라보는 방향 = 마지막 이동 방향)
	/// </summary>
	private Vector2 GetCurrentAimDirection()
	{
		// 플레이어의 마지막 이동 방향 사용 (애니메이션과 일치)
		// CurrentMoveDirection은 현재 프레임의 입력이고,
		// LastMoveDirection은 플레이어가 실제로 바라보는 방향입니다.
		return playerController.GetLastDirectionVector();
	}
	
	#endregion
	
	#region 범위 및 데미지 계산
	
	/// <summary>
	/// 랭크별 범위 계산
	/// </summary>
	private float CalculateRange(SkillRank rank)
	{
		// BaseSkillData의 rangeMultiplier 사용
		float rangeMultiplier = skillData.GetRangeMultiplier(rank);
		return baseRange * rangeMultiplier;
	}
	
	/// <summary>
	/// 랭크별 기본 데미지 계산
	/// </summary>
	private float CalculateBaseDamage(SkillRank rank)
	{
		// 1. 플레이어 공격력 기반 데미지
		float damage = playerController.CalculateSkillDamage(baseDamagePercent);
		
		// 2. 랭크별 배율 적용
		float rankMultiplier = skillData.GetDamageMultiplier(rank);
		damage *= rankMultiplier;
		
		// 3. S랭크 모드 보너스
		if (isInSRankMode)
		{
			damage *= sRankDamageBonus;
			
			// 콤보 보너스 적용
			float comboMultiplier = 1f + (comboCount * comboBonus / 100f);
			damage *= comboMultiplier;
		}
		
		return damage;
	}
	
	#endregion
	
	#region 범위 판정
	
	/// <summary>
	/// 부채꼴 범위 내 적 탐지
	/// </summary>
	private List<EntityController> DetectEnemiesInFan(Vector2 direction, float range)
	{
		List<EntityController> enemies = new List<EntityController>();
		
		// 원형 범위로 일단 탐지
		Collider2D[] colliders = Physics2D.OverlapCircleAll(playerController.transform.position, range);
		
		foreach (var collider in colliders)
		{
			// EntityController 체크
			EntityController entity = collider.GetComponent<EntityController>();
			if (entity == null || !entity.IsAlive) continue;
			
			// 플레이어 자신 제외
			if (entity == playerController) continue;
			
			// 몬스터만 대상
			if (entity.EntityType != EntityType.Monster) continue;
			
			// 부채꼴 각도 체크
			Vector2 toTarget = (entity.transform.position - playerController.transform.position).normalized;
			float angle = Vector2.Angle(direction, toTarget);
			
			if (angle <= fanAngle / 2f)
			{
				enemies.Add(entity);
			}
		}
		
		return enemies;
	}
	
	/// <summary>
	/// 스트라이크 존 체크 (범위 끝자락)
	/// </summary>
	private bool CheckStrikeZone(Vector3 targetPos, Vector3 playerPos, Vector2 direction, float range, float thickness)
	{
		// 플레이어로부터 타겟까지의 거리
		float distance = Vector2.Distance(playerPos, targetPos);
		
		// 범위 끝자락 (range - thickness ~ range) 사이에 있는지 체크
		float strikeZoneStart = range - thickness;
		float strikeZoneEnd = range;
		
		return distance >= strikeZoneStart && distance <= strikeZoneEnd;
	}
	
	#endregion
	
	#region 탄환 무력화
	
	/// <summary>
	/// 부채꼴 범위 내 탄환 무력화
	/// </summary>
	private void DestroyProjectilesInFan(Vector2 direction, float range)
	{
		// 원형 범위로 탄환 탐지
		Collider2D[] colliders = Physics2D.OverlapCircleAll(playerController.transform.position, range);
		
		int destroyedCount = 0;
		
		foreach (var collider in colliders)
		{
			// ISkillProjectile 인터페이스를 가진 오브젝트만 대상
			ISkillProjectile projectile = collider.GetComponent<ISkillProjectile>();
			if (projectile == null) continue;
			
			// 부채꼴 각도 체크
			Vector2 toProjectile = (collider.transform.position - playerController.transform.position).normalized;
			float angle = Vector2.Angle(direction, toProjectile);
			
			if (angle <= fanAngle / 2f)
			{
				// 탄환 파괴
				Destroy(collider.gameObject);
				destroyedCount++;
			}
		}
		
	}
	
	#endregion
	
	#region 데미지 적용
	
	/// <summary>
	/// 일반 데미지 적용
	/// </summary>
	private void ApplyNormalDamage(EntityController enemy, float baseDamage)
	{
		int damage = Mathf.RoundToInt(baseDamage);
		enemy.ApplyDamage(damage, EntityType.Player, this);
		
	}
	
	/// <summary>
	/// 스트라이크 존 데미지 및 스턴 적용
	/// </summary>
	private void ApplyStrikeZoneDamage(EntityController enemy, float baseDamage)
	{
		// 스트라이크 존 적중 효과음 재생 (첫 적중 시만)
		PlayStrikeZoneHitSound();
		
		// 강화 데미지
		float enhancedDamage = baseDamage * strikeZoneDamageMultiplier;
		int damage = Mathf.RoundToInt(enhancedDamage);
		enemy.ApplyDamage(damage, EntityType.Player, this);
		
		// 스턴 효과 적용
		if (stunEffect != null)
		{
			enemy.ApplyStatusEffect(stunEffect);
		}
	}
	
	#endregion
	
	#region S랭크 모드 관리
	
	/// <summary>
	/// S랭크 모드 시작
	/// </summary>
	private void StartSRankMode()
	{
		isInSRankMode = true;
		comboCount = 0;
		sRankRemainingTime = sRankBaseDuration;
		
		// 코루틴 시작
		StartCoroutine(SRankModeTimer());
	}
	
	/// <summary>
	/// S랭크 모드 업데이트 (스트라이크 존 적중 여부)
	/// </summary>
	private void UpdateSRankMode(bool strikeZoneHit)
	{
		if (!isInSRankMode) return;
		
		if (strikeZoneHit)
		{
			// 스트라이크 존 적중 - 콤보 증가, 시간 증가
			comboCount = Mathf.Min(comboCount + 1, maxComboCount);
			
			// 시간 증가 (콤보가 높을수록 증가량 감소)
			float timeBonus = comboTimeBonus * (1f - (comboCount / (float)maxComboCount) * 0.5f);
			sRankRemainingTime += timeBonus;
		}
		else
		{
			// 스트라이크 존 미적중 - 시간 감소
			sRankRemainingTime -= comboTimeBonus * 0.5f;
		}
	}
	
	/// <summary>
	/// S랭크 모드 종료
	/// </summary>
	private void StopSRankMode()
	{
		if (!isInSRankMode) return;
		
		isInSRankMode = false;
		comboCount = 0;
		sRankRemainingTime = 0f;
	}
	
	/// <summary>
	/// S랭크 모드 타이머 코루틴
	/// </summary>
	private IEnumerator SRankModeTimer()
	{
		while (isInSRankMode && sRankRemainingTime > 0f)
		{
			sRankRemainingTime -= Time.deltaTime;
			yield return null;
		}
		
		// 시간 종료
		StopSRankMode();
	}
	
	#endregion
	
	#region 비주얼 이펙트
	
	/// <summary>
	/// 비주얼 이펙트 생성
	/// </summary>
	private void SpawnVisualEffects(Vector2 direction, float range, float strikeZoneThickness)
	{
		Vector3 playerPos = playerController.transform.position;
		
		// 1. 부채꼴 범위 시각화 생성
		GameObject fanVisualizer = new GameObject("IronBladeDance_FanArea");
		fanVisualizer.transform.position = playerPos;
		
		// 방향에 맞게 회전 (앞이 Y축 양의 방향)
		// Unity 2D 좌표계: 위(0,1)=0도, 왼쪽(-1,0)=90도, 아래(0,-1)=180도, 오른쪽(1,0)=270도
		// Atan2는 시계 방향으로 계산하므로 부호를 반전해야 함
		float angle = -Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
		
		// 음수 각도를 0-360 범위로 정규화
		if (angle < 0) angle += 360f;
		
		fanVisualizer.transform.rotation = Quaternion.Euler(0, 0, angle);
		
		// FanAreaVisualizer 컴포넌트 추가
		FanAreaVisualizer visualizer = fanVisualizer.AddComponent<FanAreaVisualizer>();
		visualizer.SetFanParameters(fanAngle, range, strikeZoneThickness);
		visualizer.StartFadeOut(effectDuration);
		
		// 디버그 컴포넌트 추가 (Scene 뷰에서 방향 확인용)
		#if UNITY_EDITOR
		fanVisualizer.AddComponent<FanAreaVisualizerDebug>();
		#endif
		
		// S랭크 모드일 때 색상 변경
		if (isInSRankMode)
		{
			Color sRankColor = new Color(0.5f, 0f, 1f, 0.4f); // 보라색
			Color sRankStrikeColor = new Color(1f, 0f, 0.5f, 0.6f); // 핑크색
			visualizer.SetColors(sRankColor, sRankStrikeColor);
		}
		
		// 2. 검무 이펙트 생성 (추가 이펙트가 있다면)
		if (bladeDanceEffectPrefab != null)
		{
			Vector3 effectPos = playerPos + (Vector3)(direction * (range / 2f));
			float effectAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
			Quaternion rotation = Quaternion.Euler(0, 0, effectAngle);
			
			GameObject effect = Instantiate(bladeDanceEffectPrefab, effectPos, rotation);
			Destroy(effect, effectDuration);
		}
		
		// 3. 스트라이크 존 이펙트 생성 (추가 이펙트가 있다면)
		if (strikeZoneEffectPrefab != null)
		{
			Vector3 strikePos = playerPos + (Vector3)(direction * (range - strikeZoneThickness / 2f));
			float effectAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
			Quaternion rotation = Quaternion.Euler(0, 0, effectAngle);
			
			GameObject strikeEffect = Instantiate(strikeZoneEffectPrefab, strikePos, rotation);
			Destroy(strikeEffect, effectDuration);
		}
		
		// 4. 일반 범위 파티클 생성
		if (normalAreaParticlePrefab != null)
		{
			SpawnAreaParticles(playerPos, direction, range, strikeZoneThickness, normalAreaParticlePrefab, particleSpawnCount, false);
		}
		
		// 5. 스트라이크 존 파티클 생성
		if (strikeZoneParticlePrefab != null)
		{
			SpawnAreaParticles(playerPos, direction, range, strikeZoneThickness, strikeZoneParticlePrefab, strikeZoneParticleCount, true);
		}
		
		// 6. 경계선 파티클 생성
		if (edgeParticlePrefab != null)
		{
			SpawnEdgeParticles(playerPos, direction, range);
		}
	}
	
	/// <summary>
	/// 범위 내 파티클 생성 (일반 또는 스트라이크 존)
	/// </summary>
	private void SpawnAreaParticles(Vector3 center, Vector2 direction, float range, float strikeZoneThickness, GameObject particlePrefab, int count, bool strikeZoneOnly)
	{
		float halfAngle = fanAngle / 2f;
		
		for (int i = 0; i < count; i++)
		{
			// 랜덤 각도 (부채꼴 내)
			float randomAngle = Random.Range(-halfAngle, halfAngle);
			Vector2 randomDirection = Quaternion.Euler(0, 0, randomAngle) * direction;
			
			// 랜덤 거리
			float minDistance = strikeZoneOnly ? (range - strikeZoneThickness) : 0.5f;
			float maxDistance = strikeZoneOnly ? range : (range - strikeZoneThickness);
			float randomDistance = Random.Range(minDistance, maxDistance);
			
			// 파티클 위치 계산
			Vector3 particlePos = center + (Vector3)(randomDirection * randomDistance);
			
			// 파티클 생성
			GameObject particle = Instantiate(particlePrefab, particlePos, Quaternion.identity);
			Destroy(particle, effectDuration);
		}
	}
	
	/// <summary>
	/// 부채꼴 경계선 파티클 생성
	/// </summary>
	private void SpawnEdgeParticles(Vector3 center, Vector2 direction, float range)
	{
		float halfAngle = fanAngle / 2f;
		int edgeParticleCount = 15;
		
		// 외곽 호(arc) 파티클
		for (int i = 0; i <= edgeParticleCount; i++)
		{
			float t = i / (float)edgeParticleCount;
			float currentAngle = Mathf.Lerp(-halfAngle, halfAngle, t);
			Vector2 edgeDirection = Quaternion.Euler(0, 0, currentAngle) * direction;
			Vector3 edgePos = center + (Vector3)(edgeDirection * range);
			
			GameObject particle = Instantiate(edgeParticlePrefab, edgePos, Quaternion.identity);
			Destroy(particle, effectDuration);
		}
		
		// 왼쪽 경계선 파티클
		Vector2 leftBoundary = Quaternion.Euler(0, 0, -halfAngle) * direction;
		for (int i = 1; i <= 5; i++)
		{
			float t = i / 6f;
			Vector3 linePos = center + (Vector3)(leftBoundary * range * t);
			
			GameObject particle = Instantiate(edgeParticlePrefab, linePos, Quaternion.identity);
			Destroy(particle, effectDuration);
		}
		
		// 오른쪽 경계선 파티클
		Vector2 rightBoundary = Quaternion.Euler(0, 0, halfAngle) * direction;
		for (int i = 1; i <= 5; i++)
		{
			float t = i / 6f;
			Vector3 linePos = center + (Vector3)(rightBoundary * range * t);
			
			GameObject particle = Instantiate(edgeParticlePrefab, linePos, Quaternion.identity);
			Destroy(particle, effectDuration);
		}
	}
	
	#endregion
	
	#region 사운드 재생
	
	/// <summary>
	/// 검무 휘두르기 효과음 재생
	/// </summary>
	private void PlayBladeDanceSound()
	{
		if (bladeDanceSound != null && playerController != null)
		{
			AudioSource audioSource = playerController.GetComponent<AudioSource>();
			if (audioSource == null)
			{
				audioSource = playerController.gameObject.AddComponent<AudioSource>();
				audioSource.playOnAwake = false;
				audioSource.spatialBlend = 0f; // 2D 사운드
			}
			audioSource.PlayOneShot(bladeDanceSound);
			Debug.Log("[IronBladeDanceHandler] 검무 휘두르기 효과음 재생");
		}
	}
	
	/// <summary>
	/// 스트라이크 존 적중 효과음 재생
	/// </summary>
	private void PlayStrikeZoneHitSound()
	{
		if (strikeZoneHitSound != null && playerController != null)
		{
			AudioSource audioSource = playerController.GetComponent<AudioSource>();
			if (audioSource == null)
			{
				audioSource = playerController.gameObject.AddComponent<AudioSource>();
				audioSource.playOnAwake = false;
				audioSource.spatialBlend = 0f; // 2D 사운드
			}
			audioSource.PlayOneShot(strikeZoneHitSound);
			Debug.Log("[IronBladeDanceHandler] 스트라이크 존 적중 효과음 재생");
		}
	}
	
	#endregion
	
	#region Gizmos (디버깅용)
	
	private void OnDrawGizmosSelected()
	{
		if (playerController == null) return;
		
		// 부채꼴 범위 시각화
		Vector2 direction = playerController.GetLastDirectionVector();
		float range = baseRange;
		
		// 부채�ol 그리기
		Gizmos.color = Color.yellow;
		Vector3 playerPos = playerController.transform.position;
		
		// 중심선
		Gizmos.DrawLine(playerPos, playerPos + (Vector3)(direction * range));
		
		// 부채꼴 좌우 경계선
		float halfAngle = fanAngle / 2f;
		Vector2 leftBoundary = Quaternion.Euler(0, 0, -halfAngle) * direction;
		Vector2 rightBoundary = Quaternion.Euler(0, 0, halfAngle) * direction;
		
		Gizmos.DrawLine(playerPos, playerPos + (Vector3)(leftBoundary * range));
		Gizmos.DrawLine(playerPos, playerPos + (Vector3)(rightBoundary * range));
		
		// 스트라이크 존
		Gizmos.color = Color.red;
		float strikeZoneStart = range - strikeZoneThickness;
		Gizmos.DrawWireSphere(playerPos + (Vector3)(direction * strikeZoneStart), 0.2f);
		Gizmos.DrawWireSphere(playerPos + (Vector3)(direction * range), 0.2f);
	}
	
	#endregion
}

