using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 신념질주 스킬 핸들러
/// 수리검을 발사하여 표식을 남기고, 재사용 시 표식된 적의 뒤로 돌진합니다
/// </summary>
public class FaithDashHandler : MonoBehaviour, SkillHandlerInterface
{
	#region 표식 UI 추적
	
	/// <summary>
	/// 표식된 적과 UI를 매핑하는 딕셔너리
	/// </summary>
	private Dictionary<EntityController, GameObject> markedEnemyUIs = new Dictionary<EntityController, GameObject>();
	
	#endregion
	
	#region 기본 설정
	
	[Header("기본 데이터")]
	[Tooltip("스킬 데이터 (Setup에서 자동 설정)")]
	private PlayerSkillData skillData;
	
	[Tooltip("스킬 소유자 GameObject")]
	private GameObject owner;
	
	[Tooltip("플레이어 컨트롤러")]
	private PlayerController playerController;
	
	[Tooltip("스킬 슬롯 (쿨타임 제어용)")]
	private SkillSlot skillSlot;
	
	#endregion
	
	#region 수리검 설정
	
	[Header("수리검 설정")]
	[Tooltip("수리검 프리팹")]
	[SerializeField] private GameObject shurikenPrefab;
	
	[Tooltip("수리검 발사 속도")]
	[SerializeField] private float shurikenSpeed = 15f;
	
	[Tooltip("수리검 데미지 퍼센트")]
	[SerializeField] private float shurikenDamagePercent = 30f;
	
	[Tooltip("표식 상태효과")]
	[SerializeField] private StatusEffectDataSO markEffect;
	
	[Tooltip("S랭크 시 수리검 관통 여부")]
	[SerializeField] private bool sRankPiercing = true;
	
	#endregion
	
	#region 돌진 설정
	
	[Header("돌진 설정")]
	[Tooltip("적 뒤 오프셋 거리 (적 바로 뒤에 얼마나 떨어져서 정지할지)")]
	[SerializeField] private float behindTargetOffset = 0.5f;
	
	[Tooltip("방향 돌진 거리 (타겟 없을 때)")]
	[SerializeField] private float dashDistance = 4f;
	
	[Tooltip("돌진 속도")]
	[SerializeField] private float dashSpeed = 25f;
	
	[Tooltip("돌진 중 데미지 퍼센트")]
	[SerializeField] private float dashDamagePercent = 100f;
	
	[Tooltip("돌진 중 무적 여부")]
	[SerializeField] private bool invincibleDuringDash = true;
	
	[Tooltip("돌진 중 충돌 감지 반경")]
	[SerializeField] private float dashCollisionRadius = 1f;
	
	#endregion
	
	#region 재사용 메커니즘
	
	[Header("재사용 메커니즘")]
	[Tooltip("기본 재사용 횟수 (랭크별)")]
	[SerializeField] private int[] baseRechargeCountByRank = new int[] { 1, 1, 2, 2, 3 };
	
	[Tooltip("표식 적 처치 시 추가 재사용 횟수")]
	[SerializeField] private int bonusRechargeOnKill = 1;
	
	[Tooltip("표식 적 처치 시 추가 시간 (초)")]
	[SerializeField] private float bonusTimeOnKill = 3f;
	
	[Tooltip("최대 재사용 횟수")]
	[SerializeField] private int maxRechargeCount = 5;
	
	[Tooltip("스킬 지속 시간 (재사용 가능 시간)")]
	[SerializeField] private float skillDuration = 10f;
	
	[Tooltip("최대 스킬 지속 시간")]
	[SerializeField] private float maxSkillDuration = 30f;
	
	// 재사용 상태
	private bool isInRechargeMode = false;
	private int currentRechargeCount = 0;
	private float remainingTime = 0f;
	
	// 외부 접근용 프로퍼티
	public bool IsInRechargeMode => isInRechargeMode;
	public int CurrentRechargeCount => currentRechargeCount;
	public float RemainingTime => remainingTime;
	
	#endregion
	
	#region S랭크 설정
	
	[Header("S랭크 특수 효과")]
	[Tooltip("S랭크 추가 데미지 배율")]
	[SerializeField] private float sRankDamageMultiplier = 1.8f;
	
	[Tooltip("S랭크 몬스터 처치 시 추가 시간 (초)")]
	[SerializeField] private float sRankTimeOnKill = 5f;
	
	[Tooltip("S랭크 전체 적 표식 범위 (0이면 무한)")]
	[SerializeField] private float sRankMarkRange = 0f;
	
	// S랭크 상태
	private bool isInSRankMode = false;
	
	#endregion
	
	#region 타겟 선택 설정
	
	[Header("타겟 선택")]
	[Tooltip("타겟 탐지 범위")]
	[SerializeField] private float detectionRange = 20f;
	
	[Tooltip("방향 일치 판정 각도 (도)")]
	[SerializeField] private float directionThreshold = 45f;
	
	#endregion
	
	#region 비주얼 이펙트
	
	[Header("비주얼 이펙트")]
	[Tooltip("돌진 궤적 이펙트")]
	[SerializeField] private GameObject dashTrailPrefab;
	
	[Tooltip("표식 처치 이펙트")]
	[SerializeField] private GameObject markKillEffectPrefab;
	
	[Tooltip("재사용 준비 이펙트 (플레이어에 부착)")]
	[SerializeField] private GameObject rechargeReadyEffectPrefab;
	
	[Tooltip("표식 UI 프리팹 (적 위에 표시)")]
	[SerializeField] private GameObject markUIPrefab;
	
	[Tooltip("표식 UI 오프셋 (적 위에서 얼마나 위에 표시할지)")]
	[SerializeField] private Vector3 markUIOffset = new Vector3(0f, 1f, 0f);
	
	private GameObject activeRechargeEffect;
	
	#endregion
	
	#region 코루틴 참조
	
	private Coroutine rechargeCoroutine;
	private Coroutine dashCoroutine;
	
	#endregion
	
	#region SkillHandlerInterface 구현
	
	/// <summary>
	/// 스킬 초기화
	/// </summary>
	public void Setup(BaseSkillData data, GameObject owner)
	{
		this.skillData = data as PlayerSkillData;
		this.owner = owner;
		
		// PlayerController 찾기 (owner 자신 또는 부모에서)
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
			Debug.LogError("[FaithDashHandler] PlayerController를 찾을 수 없습니다!");
		}
		
		// SkillSlot 찾기 (쿨타임 제어용)
		skillSlot = GetComponentInParent<SkillSlot>();
	}
	
	/// <summary>
	/// 스킬 사용
	/// </summary>
	public void Use(SkillRank rank)
	{
		if (playerController == null) return;
		
		// 재사용 모드가 아니면 초기 발동 (수리검 발사)
		if (!isInRechargeMode)
		{
			ExecuteInitialShot(rank);
		}
		// 재사용 모드면 돌진 실행
		else
		{
			ExecuteDash(rank);
		}
	}
	
	/// <summary>
	/// 스킬 정리
	/// </summary>
	public void Cleanup()
	{
		StopRechargeMode();
		
		if (activeRechargeEffect != null)
		{
			Destroy(activeRechargeEffect);
		}
		
		// 모든 표식 UI 제거
		ClearAllMarkUIs();
	}
	
	#endregion
	
	#region Unity Lifecycle
	
	private void Update()
	{
		// 표식 UI 위치 업데이트 및 상태 확인
		UpdateMarkUIs();
	}
	
	#endregion
	
	#region 초기 발동 (수리검 발사)
	
	/// <summary>
	/// 초기 발동: 수리검 발사 및 재사용 모드 시작
	/// </summary>
	private void ExecuteInitialShot(SkillRank rank)
	{
		// ⭐ 이전 표식 모두 제거 (새 수리검 발사 시)
		RemoveAllMarksFromEnemies();
		
		// S랭크 모드 확인
		isInSRankMode = (rank == SkillRank.S);
		
		// 플레이어가 바라보는 방향 가져오기
		Vector2 direction = playerController.GetLastDirectionVector();
		
		// 수리검 발사
		FireShuriken(direction, rank);
		
		// S랭크면 모든 적에게 표식 적용
		if (isInSRankMode && markEffect != null)
		{
			ApplyMarkToAllEnemies();
		}
		
		// 재사용 모드 시작
		int rechargeCount = GetBaseRechargeCount(rank);
		StartRechargeMode(rechargeCount, skillDuration);
	}
	
	/// <summary>
	/// 수리검 발사
	/// </summary>
	private void FireShuriken(Vector2 direction, SkillRank rank)
	{
		if (shurikenPrefab == null)
		{
			Debug.LogWarning("[FaithDashHandler] 수리검 프리팹이 설정되지 않았습니다!");
			return;
		}
		
		// 데미지 계산
		float baseDamage = playerController.CalculateSkillDamage(shurikenDamagePercent);
		float damageMultiplier = skillData.GetDamageMultiplier(rank);
		if (isInSRankMode)
		{
			damageMultiplier *= sRankDamageMultiplier;
		}
		float finalDamage = baseDamage * damageMultiplier;
		
		// 수리검 생성
		Vector3 spawnPos = playerController.transform.position;
		float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
		Quaternion rotation = Quaternion.Euler(0, 0, angle);
		
		GameObject shurikenObj = Instantiate(shurikenPrefab, spawnPos, rotation);
		ShurikenProjectile shuriken = shurikenObj.GetComponent<ShurikenProjectile>();
		
		if (shuriken != null)
		{
			// 프로젝타일 데이터 설정
			shuriken.SetSkillData(finalDamage, shurikenSpeed);
			shuriken.SetAttackerType(EntityType.Player);
			shuriken.SetOwnerController(playerController);
			shuriken.SetMarkEffect(markEffect);
			
			// S랭크면 관통 적용
			if (isInSRankMode)
			{
				shuriken.SetPiercing(sRankPiercing);
			}
		}
	}
	
	/// <summary>
	/// S랭크 시 모든 적에게 표식 적용
	/// </summary>
	private void ApplyMarkToAllEnemies()
	{
		Collider2D[] enemies;
		
		// 범위 지정이 있으면 범위 내, 없으면 전체
		if (sRankMarkRange > 0)
		{
			// 범위 내 모든 Collider 탐색
			enemies = Physics2D.OverlapCircleAll(
				playerController.transform.position,
				sRankMarkRange
			);
		}
		else
		{
			// 전체 적 탐색
			enemies = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
				.OfType<EntityController>()
				.Where(e => e.EntityType != EntityType.Player && e.IsAlive)
				.Select(e => e.GetComponent<Collider2D>())
				.Where(c => c != null)
				.ToArray();
		}
		
		foreach (Collider2D enemyCollider in enemies)
		{
			EntityController enemy = enemyCollider.GetComponent<EntityController>();
			// EntityController가 있고, 플레이어가 아니고, 살아있는 적에게만 표식 적용
			if (enemy != null && enemy.EntityType != EntityType.Player && enemy.IsAlive)
			{
				enemy.ApplyStatusEffect(markEffect);
			}
		}
	}
	
	/// <summary>
	/// 랭크별 기본 재사용 횟수 가져오기
	/// </summary>
	private int GetBaseRechargeCount(SkillRank rank)
	{
		int index = (int)rank;
		if (index >= 0 && index < baseRechargeCountByRank.Length)
		{
			return baseRechargeCountByRank[index];
		}
		return 1;
	}
	
	#endregion
	
	#region 재사용 모드
	
	/// <summary>
	/// 재사용 모드 시작
	/// </summary>
	private void StartRechargeMode(int count, float duration)
	{
		isInRechargeMode = true;
		currentRechargeCount = Mathf.Min(count, maxRechargeCount);
		remainingTime = Mathf.Min(duration, maxSkillDuration);
		
		// 재사용 준비 이펙트 생성
		if (rechargeReadyEffectPrefab != null && activeRechargeEffect == null)
		{
			activeRechargeEffect = Instantiate(
				rechargeReadyEffectPrefab,
				playerController.transform
			);
		}
		
		// 재사용 타이머 시작
		if (rechargeCoroutine != null)
		{
			StopCoroutine(rechargeCoroutine);
		}
		rechargeCoroutine = StartCoroutine(RechargeTimer());
	}
	
	/// <summary>
	/// 재사용 모드 종료
	/// </summary>
	private void StopRechargeMode()
	{
		isInRechargeMode = false;
		isInSRankMode = false;
		currentRechargeCount = 0;
		remainingTime = 0f;
		
		if (rechargeCoroutine != null)
		{
			StopCoroutine(rechargeCoroutine);
			rechargeCoroutine = null;
		}
		
		if (activeRechargeEffect != null)
		{
			Destroy(activeRechargeEffect);
			activeRechargeEffect = null;
		}
		
		// ⭐ 재사용 모드 종료 시 쿨타임 시작
		if (skillSlot != null)
		{
			skillSlot.StartCooldownFromHandler();
		}
	}
	
	/// <summary>
	/// 재사용 타이머
	/// </summary>
	private IEnumerator RechargeTimer()
	{
		while (remainingTime > 0f)
		{
			remainingTime -= Time.deltaTime;
			yield return null;
		}
		
		// 시간 종료 시 재사용 모드 종료
		StopRechargeMode();
	}
	
	/// <summary>
	/// 재사용 횟수 추가
	/// </summary>
	private void AddRechargeCount(int amount)
	{
		currentRechargeCount = Mathf.Min(currentRechargeCount + amount, maxRechargeCount);
	}
	
	/// <summary>
	/// 재사용 시간 추가
	/// </summary>
	private void AddRechargeTime(float amount)
	{
		remainingTime = Mathf.Min(remainingTime + amount, maxSkillDuration);
	}
	
	#endregion
	
	#region 돌진 실행
	
	/// <summary>
	/// 돌진 실행
	/// </summary>
	private void ExecuteDash(SkillRank rank)
	{
		// 재사용 횟수 확인
		if (currentRechargeCount <= 0)
		{
			Debug.Log("[FaithDashHandler] 재사용 횟수가 없습니다!");
			return;
		}
		
		// 타겟 선택
		EntityController target = SelectDashTarget();
		
		// 돌진 목표 위치 계산
		Vector3 dashTargetPos;
		if (target != null)
		{
			// 타겟의 바로 뒤로 이동 (플레이어→적 방향)
			Vector2 directionToTarget = (target.transform.position - playerController.transform.position).normalized;
			dashTargetPos = target.transform.position + (Vector3)directionToTarget * behindTargetOffset;
		}
		else
		{
			// 바라보는 방향으로 직선 돌진
			Vector2 direction = playerController.GetLastDirectionVector();
			dashTargetPos = playerController.transform.position + (Vector3)direction * dashDistance;
		}
		
		// 돌진 시작
		if (dashCoroutine != null)
		{
			StopCoroutine(dashCoroutine);
		}
		dashCoroutine = StartCoroutine(DashCoroutine(dashTargetPos, rank));
		
		// 재사용 횟수 감소
		currentRechargeCount--;
		
		// 재사용 횟수가 0이 되면 모드 종료
		if (currentRechargeCount <= 0)
		{
			StopRechargeMode();
		}
	}
	
	/// <summary>
	/// 돌진 타겟 선택
	/// </summary>
	private EntityController SelectDashTarget()
	{
		// 모든 살아있는 적 탐색 (모든 Collider 검색)
		Collider2D[] allColliders = Physics2D.OverlapCircleAll(
			playerController.transform.position,
			detectionRange
		);
		
		List<EntityController> enemies = new List<EntityController>();
		foreach (Collider2D col in allColliders)
		{
			EntityController enemy = col.GetComponent<EntityController>();
			// EntityController가 있고, 플레이어가 아니고, 살아있는 적만 추가
			if (enemy != null && enemy.EntityType != EntityType.Player && enemy.IsAlive)
			{
				enemies.Add(enemy);
			}
		}
		
		if (enemies.Count == 0) return null;
		
		// 표식이 있는 적만 필터링
		List<EntityController> markedEnemies = enemies
			.Where(e => HasMark(e))
			.ToList();
		
		// 표식이 있는 적이 없으면 null 반환 (방향으로만 돌진)
		if (markedEnemies.Count == 0) return null;
		
		// 플레이어가 바라보는 방향
		Vector2 playerDirection = playerController.GetLastDirectionVector();
		
		// 우선순위 1: 바라보는 방향 + 표식 있음 + 체력 가장 낮음
		List<EntityController> markedInDirection = markedEnemies
			.Where(e => IsInDirection(e.transform.position, playerDirection))
			.OrderBy(e => e.CurrentHealth)
			.ToList();
		
		if (markedInDirection.Count > 0)
		{
			return markedInDirection[0];
		}
		
		// 우선순위 2: 표식 있음 + 체력 가장 낮음 (방향 무관)
		return markedEnemies.OrderBy(e => e.CurrentHealth).First();
	}
	
	/// <summary>
	/// 적이 표식을 가지고 있는지 확인
	/// </summary>
	private bool HasMark(EntityController enemy)
	{
		if (markEffect == null) return false;
		
		foreach (var effect in enemy.ActiveStatusEffects)
		{
			if (effect.Effect.effectID == markEffect.effectID)
			{
				return true;
			}
		}
		return false;
	}
	
	/// <summary>
	/// 타겟이 플레이어가 바라보는 방향에 있는지 확인
	/// </summary>
	private bool IsInDirection(Vector3 targetPos, Vector2 direction)
	{
		Vector2 toTarget = (targetPos - playerController.transform.position).normalized;
		float angle = Vector2.Angle(direction, toTarget);
		return angle <= directionThreshold;
	}
	
	/// <summary>
	/// 돌진 코루틴
	/// </summary>
	private IEnumerator DashCoroutine(Vector3 targetPos, SkillRank rank)
	{
		// 무적 활성화
		bool wasInvincible = playerController.IsInvincible;
		if (invincibleDuringDash && !wasInvincible)
		{
			playerController.SetInvincible(true);
		}
		
		// 데미지 계산
		float baseDamage = playerController.CalculateSkillDamage(dashDamagePercent);
		float damageMultiplier = skillData.GetDamageMultiplier(rank);
		if (isInSRankMode)
		{
			damageMultiplier *= sRankDamageMultiplier;
		}
		float finalDamage = baseDamage * damageMultiplier;
		
		// 이미 데미지를 입힌 적 추적
		HashSet<EntityController> hitEnemies = new HashSet<EntityController>();
		
		// 돌진 궤적 이펙트 생성
		GameObject trail = null;
		if (dashTrailPrefab != null)
		{
			trail = Instantiate(dashTrailPrefab, playerController.transform.position, Quaternion.identity);
			trail.transform.SetParent(playerController.transform);
		}
		
		// 돌진 시작
		Vector3 startPos = playerController.transform.position;
		float distance = Vector3.Distance(startPos, targetPos);
		float duration = distance / dashSpeed;
		float elapsed = 0f;
		
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / duration;
			
			// 이동
			playerController.transform.position = Vector3.Lerp(startPos, targetPos, t);
			
			// 충돌 감지 (모든 Collider 탐색)
			Collider2D[] hits = Physics2D.OverlapCircleAll(
				playerController.transform.position,
				dashCollisionRadius
			);
			
			foreach (Collider2D hit in hits)
			{
				// EntityController가 있는 대상만 처리
				EntityController enemy = hit.GetComponent<EntityController>();
				if (enemy == null) continue;
				
				// 플레이어 자신은 무시
				if (enemy.EntityType == EntityType.Player) continue;
				
				// 죽은 적이나 이미 히트한 적은 무시
				if (!enemy.IsAlive || hitEnemies.Contains(enemy)) continue;
				
				// 표식 확인 (데미지 전에 체크)
				bool hadMark = HasMark(enemy);
				
				// 데미지 적용
				enemy.ApplyDamage((int)finalDamage, EntityType.Player);
				hitEnemies.Add(enemy);
				
				// 적이 죽었는지 확인
				if (!enemy.IsAlive)
				{
					// 죽은 적은 OnEnemyKilled에서 보너스 처리 후 표식 자동 제거됨 (Update에서)
					OnEnemyKilled(enemy);
				}
				else if (hadMark && markEffect != null)
				{
					// 살아있는 적의 표식만 제거 (돌진으로 히트하면 표식 소모)
					enemy.RemoveStatusEffect(markEffect);
				}
			}
			
			yield return null;
		}
		
		// 최종 위치 설정
		playerController.transform.position = targetPos;
		
		// 무적 해제
		if (invincibleDuringDash && !wasInvincible)
		{
			playerController.SetInvincible(false);
		}
		
		// 궤적 이펙트 제거
		if (trail != null)
		{
			trail.transform.SetParent(null);
			Destroy(trail, 2f);
		}
		
		dashCoroutine = null;
	}
	
	#endregion
	
	#region 적 처치 보상
	
	/// <summary>
	/// 적 처치 시 호출
	/// </summary>
	private void OnEnemyKilled(EntityController enemy)
	{
		// 표식이 있는 적을 처치한 경우
		if (HasMark(enemy))
		{
			// 재사용 횟수 추가
			AddRechargeCount(bonusRechargeOnKill);
			
			// 시간 추가
			AddRechargeTime(bonusTimeOnKill);
			
			// 표식 처치 이펙트
			if (markKillEffectPrefab != null)
			{
				Instantiate(markKillEffectPrefab, enemy.transform.position, Quaternion.identity);
			}
		}
		
		// S랭크 모드면 무조건 시간 추가
		if (isInSRankMode)
		{
			AddRechargeTime(sRankTimeOnKill);
		}
	}
	
	#endregion
	
	#region 표식 관리
	
	/// <summary>
	/// 모든 적에게서 표식 제거
	/// </summary>
	private void RemoveAllMarksFromEnemies()
	{
		if (markEffect == null) return;
		
		// 씬의 모든 적 탐색
		EntityController[] allEntities = FindObjectsByType<EntityController>(FindObjectsSortMode.None);
		
		foreach (EntityController entity in allEntities)
		{
			// 플레이어나 죽은 적은 무시
			if (entity.EntityType == EntityType.Player || !entity.IsAlive) continue;
			
			// 표식이 있으면 제거
			if (HasMark(entity))
			{
				entity.RemoveStatusEffect(markEffect);
			}
		}
	}
	
	#endregion
	
	#region 표식 UI 관리
	
	/// <summary>
	/// 표식 UI 위치 업데이트 및 상태 확인
	/// </summary>
	private void UpdateMarkUIs()
	{
		if (markEffect == null || markUIPrefab == null) return;
		
		// 씬의 모든 적 탐색
		EntityController[] allEntities = FindObjectsByType<EntityController>(FindObjectsSortMode.None);
		
		foreach (EntityController entity in allEntities)
		{
			// 플레이어나 죽은 적은 무시
			if (entity.EntityType == EntityType.Player || !entity.IsAlive)
			{
				// 죽은 적의 UI 제거
				RemoveMarkUI(entity);
				continue;
			}
			
			// 표식 확인
			bool hasMarkEffect = HasMark(entity);
			
			if (hasMarkEffect)
			{
				// 표식이 있으면 UI 생성 또는 위치 업데이트
				if (!markedEnemyUIs.ContainsKey(entity))
				{
					CreateMarkUI(entity);
				}
				else
				{
					UpdateMarkUIPosition(entity);
				}
			}
			else
			{
				// 표식이 없으면 UI 제거
				RemoveMarkUI(entity);
			}
		}
	}
	
	/// <summary>
	/// 표식 UI 생성
	/// </summary>
	private void CreateMarkUI(EntityController enemy)
	{
		if (markUIPrefab == null || enemy == null) return;
		
		GameObject markUI = Instantiate(markUIPrefab);
		markUI.transform.position = enemy.transform.position + markUIOffset;
		markedEnemyUIs[enemy] = markUI;
	}
	
	/// <summary>
	/// 표식 UI 위치 업데이트
	/// </summary>
	private void UpdateMarkUIPosition(EntityController enemy)
	{
		if (!markedEnemyUIs.ContainsKey(enemy)) return;
		
		GameObject markUI = markedEnemyUIs[enemy];
		if (markUI != null && enemy != null)
		{
			markUI.transform.position = enemy.transform.position + markUIOffset;
		}
	}
	
	/// <summary>
	/// 표식 UI 제거
	/// </summary>
	private void RemoveMarkUI(EntityController enemy)
	{
		if (!markedEnemyUIs.ContainsKey(enemy)) return;
		
		GameObject markUI = markedEnemyUIs[enemy];
		if (markUI != null)
		{
			Destroy(markUI);
		}
		markedEnemyUIs.Remove(enemy);
	}
	
	/// <summary>
	/// 모든 표식 UI 제거
	/// </summary>
	private void ClearAllMarkUIs()
	{
		foreach (var kvp in markedEnemyUIs)
		{
			if (kvp.Value != null)
			{
				Destroy(kvp.Value);
			}
		}
		markedEnemyUIs.Clear();
	}
	
	#endregion
	
	#region 디버그
	
#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		if (playerController == null) return;
		
		// 탐지 범위
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(playerController.transform.position, detectionRange);
		
		// 돌진 충돌 범위
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(playerController.transform.position, dashCollisionRadius);
	}
#endif
	
	#endregion
}

