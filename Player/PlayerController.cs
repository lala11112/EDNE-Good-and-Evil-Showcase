using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Com.LuisPedroFonseca.ProCamera2D;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : EntityController
{
	#region 싱글톤 패턴
	
	/// <summary>
	/// 플레이어 인스턴스 (씬에 하나만 존재)
	/// </summary>
	public static PlayerController Instance { get; private set; }
	
	#endregion
	
	#region 컴포넌트 참조

	[Header("플레이어 데이터 (ScriptableObject)")]
	[Tooltip("플레이어 전용 SO 데이터 (EntityDataSO도 자동으로 설정됨)")]
	[SerializeField] private PlayerDataSO playerData;

	[Header("스킬 시스템")]
	[Tooltip("스킬 관리 컨트롤러")]
	[SerializeField] private SkillController skillController;

	[Header("애니메이션")]
	[SerializeField] private Animator animator;
	
	[Header("비주얼")]
	[Tooltip("플레이어 스프라이트 렌더러 (깜빡임 효과용)")]
	[SerializeField] private SpriteRenderer spriteRenderer;

	[Header("카메라 쉐이크 (피격)")]
	[Tooltip("피격 데미지에 따라 쉐이크 강도를 계산하는 SO")]
	[SerializeField] private PlayerHitShakeSO hitShake;
	[Tooltip("ProCamera2DShake 컴포넌트 (카메라에 부착)")]
	[SerializeField] private ProCamera2DShake camShake;

	[Header("입력 시스템")]
	[Tooltip("Input Action Asset (Generate C# Class를 사용하지 않는 경우)")]
	[SerializeField] private InputActionAsset inputActions;
	
	private InputAction moveAction;
	
	// 또는 Generated C# Class를 사용하는 경우:
	// private PlayerInputActions playerInput;
	// private InputAction moveAction;

	#endregion

	#region 런타임 상태 - 정신력

	[Header("정신력 시스템")]
	[Tooltip("현재 정신력 (스킬 사용 시 소모)")]
	[SerializeField] private int currentMentalPower;
	
	[Tooltip("최대 정신력")]
	[SerializeField] private int maxMentalPower;
	
	[Tooltip("누적 회복량 (소수점 회복량 저장용, 1 이상이 되면 정수로 변환하여 적용)")]
	private float accumulatedRegen = 0f;

	#endregion

	#region 런타임 상태 - 용기 시스템

	[Header("용기 시스템")]
	[Tooltip("현재 용기 점수 (동적으로 증감)")]
	[SerializeField] private int courageScore;

	[Tooltip("현재 용기 단계")]
	[SerializeField] private CourageState currentCourageState;

	[Tooltip("현재 스킬 랭크 (용기 단계에 따라 자동 결정)")]
	[SerializeField] public SkillRank currentSkillRank;

	// ⭐ 용기 시스템 추적 변수들
	/// <summary>
	/// 최근 처치 기록 (15초간 유지, 용기 획득 판정용)
	/// </summary>
	private List<KillRecord> recentKillRecords = new List<KillRecord>();

	/// <summary>
	/// 최근 사용한 스킬 기록 (5초간 유지, 스킬 연계 판정용)
	/// </summary>
	private List<SkillUseRecord> recentSkillUseRecords = new List<SkillUseRecord>();

	/// <summary>
	/// 최근 동시 처치 기록 (3초간 유지, 멀티킬 판정용)
	/// </summary>
	private List<MultiKillRecord> recentMultiKillRecords = new List<MultiKillRecord>();

	/// <summary>
	/// 마지막 전투 행동 시간 (데미지 입힘/피격)
	/// </summary>
	private float lastCombatActionTime = 0f;

	/// <summary>
	/// 마지막 피격 시간
	/// </summary>
	private float lastHitTime = 0f;

	/// <summary>
	/// 연속 피격 기록 (10초간 유지)
	/// </summary>
	private List<float> recentHitTimes = new List<float>();

	/// <summary>
	/// 현재 방 컨트롤러 (방 활성화 체크용)
	/// </summary>
	private RoomController currentRoomController = null;

	/// <summary>
	/// 전투 상태 (현재 전투 중인지 여부)
	/// </summary>
	private bool isInCombat = false;

	/// <summary>
	/// 전투 종료 예정 시간 (이 시간 이후 전투 종료)
	/// </summary>
	private float combatEndTime = 0f;

	/// <summary>
	/// S랭크 궁극기 버프 활성화 여부
	/// </summary>
	private bool isUltimateBuffActive = false;

	/// <summary>
	/// S랭크 궁극기 버프 종료 시간
	/// </summary>
	private float ultimateBuffEndTime = 0f;

	/// <summary>
	/// 누적 전투 없음 페널티 (소수점 감소량 저장용)
	/// </summary>
	private float accumulatedNoCombatLoss = 0f;

	/// <summary>
	/// 누적 무피해 용기 획득량 (소수점 획득량 저장용)
	/// </summary>
	private float accumulatedNoDamageGain = 0f;

	/// <summary>
	/// 구독한 몬스터 목록 (중복 구독 방지용)
	/// </summary>
	private HashSet<MonsterController> subscribedMonsters = new HashSet<MonsterController>();

	/// <summary>
	/// 방에서 데미지를 입힌 적이 있는지 여부 (무피해 정신력 회복 시작 판정용)
	/// </summary>
	private bool hasDealtDamageInRoom = false;

	/// <summary>
	/// 방에서 무피해 정신력 회복 시작 시간
	/// </summary>
	private float roomNoDamageStartTime = 0f;

	#endregion

	#region 런타임 상태 - 이동

	[Header("이동 상태")]
	[Tooltip("현재 이동 입력 벡터")]
	[SerializeField] private Vector2 moveInput;

	[Tooltip("마지막 이동 방향 (8방향)")]
	[SerializeField] private MoveDirection lastMoveDirection = MoveDirection.Down;

	[Tooltip("현재 이동 방향 (8방향)")]
	[SerializeField] private MoveDirection currentMoveDirection = MoveDirection.None;

	#endregion
	
	#region 런타임 상태 - 피격 무적
	
	// 깜빡임 효과 코루틴 참조
	private Coroutine blinkCoroutine;

	#endregion
	
	#region 런타임 상태 - 애니메이션
	
	[Header("애니메이션 트리거")]
	[Tooltip("현재 재생 중인 공격 애니메이션이 있는지 여부")]
	private bool isPlayingAttackAnimation = false;

	#endregion

	#region 런타임 상태 - 넉백

	[Header("넉백 상태")]
	[Tooltip("넉백 방향 (정규화된 벡터)")]
	[SerializeField] private Vector2 knockbackDirection;

	[Tooltip("넉백 초기 힘")]
	[SerializeField] private float knockbackForce;

	[Tooltip("넉백 지속 시간")]
	[SerializeField] private float knockbackDuration;

	[Tooltip("넉백 경과 시간")]
	[SerializeField] private float knockbackElapsedTime;

	[Tooltip("넉백 중인지 여부")]
	[SerializeField] private bool isKnockedBack;

	#endregion

	#region 이벤트

	/// <summary>
	/// 플레이어 사망 이벤트
	/// 게임 오버 UI 등에서 구독하여 사용합니다.
	/// </summary>
	public static event System.Action OnPlayerDied;
	
	/// <summary>
	/// 플레이어 부활 이벤트
	/// 게임 오버 UI에서 "계속하기" 버튼 클릭 시 사용합니다.
	/// </summary>
	public static event System.Action OnPlayerRevived;

	#endregion

	#region 프로퍼티 (읽기 전용)

	/// <summary>현재 정신력</summary>
	public int CurrentMentalPower => currentMentalPower;

	/// <summary>최대 정신력</summary>
	public int MaxMentalPower => maxMentalPower;

	/// <summary>현재 용기 점수</summary>
	public int CourageScore => courageScore;

	/// <summary>현재 용기 단계</summary>
	public CourageState CurrentCourageState => currentCourageState;

	/// <summary>현재 스킬 랭크</summary>
	public SkillRank CurrentSkillRank => currentSkillRank;

	/// <summary>플레이어 데이터 SO</summary>
	public PlayerDataSO PlayerData => playerData;

	/// <summary>마지막 이동 방향 (8방향)</summary>
	public MoveDirection LastMoveDirection => lastMoveDirection;

	/// <summary>현재 이동 방향 (8방향)</summary>
	public MoveDirection CurrentMoveDirection => currentMoveDirection;

	#endregion

	#region Unity Lifecycle

	/// <summary>
	/// 에디터에서 값이 변경될 때 호출 (자동 설정용)
	/// </summary>
	private void OnValidate()
	{
		// PlayerDataSO를 EntityDataSO에도 자동으로 할당
		if (playerData != null)
		{
			entityData = playerData;
		}
		
		// SpriteRenderer 자동 할당
		if (spriteRenderer == null)
		{
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		}
	}

	protected override void Awake()
	{
		base.Awake();
		
		// 싱글톤 설정
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Debug.LogWarning($"[PlayerController] 씬에 PlayerController가 여러 개 있습니다! {gameObject.name}을 파괴합니다.");
			Destroy(gameObject);
			return;
		}

		// PlayerDataSO를 EntityDataSO에 할당 (런타임 안전성)
		if (playerData != null)
		{
			entityData = playerData;
		}

		// 컴포넌트 자동 할당
		if (skillController == null)
		{
			skillController = GetComponent<SkillController>();
		}
		
		if (spriteRenderer == null)
		{
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		}

		// Rigidbody2D 설정 (Dynamic + 즉시 반응)
		SetupRigidbody2D();

		// Input Actions 초기화
		if (inputActions != null)
		{
			moveAction = inputActions.FindAction("Move");
		}

		// Animator 자동 할당
		if (animator == null)
		{
			animator = GetComponentInChildren<Animator>();
		}

		// 카메라 쉐이크 자동 할당 (없으면 찾기)
		if (camShake == null)
		{
			camShake = FindObjectOfType<ProCamera2DShake>();
		}
	}

	protected override void OnEnable()
	{
		base.OnEnable();

		// Input Actions 활성화
		if (inputActions != null)
		{
			inputActions.Enable();
		}
	}

	private void OnDisable()
	{
		// Input Actions 비활성화
		if (inputActions != null)
		{
			inputActions.Disable();
		}
		
		// ⭐ 개선 #15: 이벤트 구독 해제
		UnsubscribeAllMonsterDeathEvents();
		
		// ⭐ 싱글톤 정리
		if (Instance == this)
		{
			Instance = null;
		}
	}

	private void OnDestroy()
	{
		// ⭐ 개선 #15: 이벤트 구독 해제 (OnDisable이 호출되지 않을 수 있으므로)
		UnsubscribeAllMonsterDeathEvents();
	}

	private void Start()
	{
		// ⭐ 용기 시스템 이벤트 구독
		SubscribeCourageSystemEvents();
		
		// 이미 씬에 있는 몬스터들도 구독 (스폰 전에 생성된 몬스터용)
		MonsterController[] existingMonsters = FindObjectsOfType<MonsterController>();
		foreach (var monster in existingMonsters)
		{
			SubscribeMonsterDeathEvent(monster);
		}
	}

	protected override void Update()
	{
		base.Update(); // EntityController의 상태효과 업데이트

		// 정신력 자동 회복
		UpdateMentalPowerRegeneration(Time.deltaTime);

		// 넉백 타이머 업데이트 (우선 처리)
		UpdateKnockbackTimer(Time.deltaTime);

		// 이동 입력 처리
		UpdateMovementInput();

		// 애니메이터 파라미터 업데이트
		UpdateAnimatorParameters();

		// ⭐ 용기 시스템 업데이트
		UpdateCourageSystem(Time.deltaTime);

		// ⭐ S랭크 궁극기 버프 업데이트
		UpdateUltimateBuff(Time.deltaTime);
	}

	private void FixedUpdate()
	{
		// 물리 기반 이동 처리
		ProcessMovement();
	}

	#endregion

	#region Rigidbody2D 설정

	/// <summary>
	/// Rigidbody2D 설정 (Dynamic + 즉시 반응)
	/// </summary>
	private void SetupRigidbody2D()
	{
		if (rb == null)
		{
			rb = GetComponent<Rigidbody2D>();
		}

		// Dynamic 설정으로 물리 충돌 활성화
		rb.bodyType = RigidbodyType2D.Dynamic;
		
		// 2D 탑뷰 게임 최적화 설정
		rb.gravityScale = 0f;                    // 중력 제거
		rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 정확한 충돌 감지
		rb.freezeRotation = true;               // 회전 고정
		
#if UNITY_EDITOR
		Debug.Log("[PlayerController] Rigidbody2D가 Dynamic 모드로 설정되었습니다. (즉시 반응 + 물리 충돌)");
#endif
	}

	#endregion

	#region 초기화

	/// <summary>
	/// 플레이어 초기화
	/// EntityController의 초기화를 확장합니다
	/// </summary>
	public override void Initialize()
	{
		// EntityDataSO는 이미 OnValidate/Awake에서 자동 설정됨
		
		// 기본 초기화 (체력, 상태효과 등)
		base.Initialize();

		// 플레이어 전용 초기화
		InitializeMentalPower();
		InitializeCourageSystem();
	}

	/// <summary>
	/// 정신력 초기화
	/// </summary>
	private void InitializeMentalPower()
	{
		if (playerData == null) return;

		maxMentalPower = playerData.maxMentalPower;
		currentMentalPower = playerData.initialMentalPower;
		
		// 초기화 시 이벤트 발생
		PlayerStatusEvents.InvokeMentalPowerChanged(currentMentalPower, maxMentalPower);
		PlayerStatusEvents.InvokeHealthChanged(CurrentHealth, MaxHealth);
	}

	/// <summary>
	/// 용기 시스템 초기화
	/// </summary>
	private void InitializeCourageSystem()
	{
		if (playerData == null) return;

		courageScore = playerData.initialCourageScore;
		UpdateCourageState();

		// ⭐ 랭크 시스템 디버그: 초기 랭크 출력
#if UNITY_EDITOR
		Debug.Log($"[PlayerController] ⭐ 용기 시스템 초기화 - 현재 랭크: {currentSkillRank}, 용기 단계: {currentCourageState}, 용기 점수: {courageScore}");
#endif

		// ⭐ 용기 시스템 이벤트 구독
		// 몬스터 처치 이벤트는 Start에서 구독 (MonsterController가 생성된 후)
	}

	/// <summary>
	/// 용기 시스템 이벤트 구독 (Start에서 호출)
	/// </summary>
	private void SubscribeCourageSystemEvents()
	{
		Debug.Log("[PlayerController] 용기 시스템 이벤트 구독 완료 (RoomController.RegisterMonster에서 자동 구독됨)");
	}

	/// <summary>
	/// 몬스터 이벤트 구독 (RoomController.RegisterMonster에서 호출)
	/// </summary>
	public void SubscribeMonsterDeathEvent(MonsterController monster)
	{
		if (monster == null) return;

		// 이미 구독했는지 확인 (중복 구독 방지)
		if (!subscribedMonsters.Contains(monster))
		{
			monster.OnMonsterKilledBySkill += OnMonsterKilled;
			subscribedMonsters.Add(monster);
#if UNITY_EDITOR
			Debug.Log($"[PlayerController] {monster.name}의 죽음 이벤트 구독 완료");
#endif
		}
	}

	/// <summary>
	/// 몬스터 이벤트 구독 해제 (개선 #15: 이벤트 구독 해제 로직 추가)
	/// </summary>
	/// <param name="monster">구독 해제할 몬스터</param>
	public void UnsubscribeMonsterDeathEvent(MonsterController monster)
	{
		if (monster == null) return;

		if (subscribedMonsters.Contains(monster))
		{
			monster.OnMonsterKilledBySkill -= OnMonsterKilled;
			subscribedMonsters.Remove(monster);
#if UNITY_EDITOR
			Debug.Log($"[PlayerController] {monster.name}의 죽음 이벤트 구독 해제 완료");
#endif
		}
	}

	/// <summary>
	/// 모든 몬스터 이벤트 구독 해제 (개선 #15: 이벤트 구독 해제 로직 추가)
	/// </summary>
	private void UnsubscribeAllMonsterDeathEvents()
	{
		foreach (var monster in subscribedMonsters)
		{
			if (monster != null)
			{
				monster.OnMonsterKilledBySkill -= OnMonsterKilled;
			}
		}
		subscribedMonsters.Clear();
#if UNITY_EDITOR
		Debug.Log("[PlayerController] 모든 몬스터 이벤트 구독 해제 완료");
#endif
	}

	#endregion

	#region 정신력 관리

	/// <summary>
	/// 정신력 자동 회복 처리
	/// </summary>
	private void UpdateMentalPowerRegeneration(float deltaTime)
	{
		if (playerData == null) return;
		if (currentMentalPower >= maxMentalPower) 
		{
			// 최대치에 도달하면 누적 회복량 초기화
			accumulatedRegen = 0f;
			return;
		}
		if (!isAlive) return;

		// 회복량 누적 (소수점 단위로 정확하게 계산)
		int previousMentalPower = currentMentalPower;
		float baseRegenAmount = playerData.mentalPowerRegenRate * deltaTime;
		
		// ⭐ S랭크 궁극기 버프: 정신력 회복 속도 배율 적용 (SO 데이터 사용)
		float regenMultiplier = isUltimateBuffActive 
			? playerData.ultimateBuffMentalPowerRegenMultiplier 
			: 1f;
		float regenAmount = baseRegenAmount * regenMultiplier;
		
		accumulatedRegen += regenAmount;
		
		// 누적 회복량이 1 이상이 되면 정수로 변환하여 적용
		if (accumulatedRegen >= 1f)
		{
			int regenToApply = Mathf.FloorToInt(accumulatedRegen); // 소수점 버림
			accumulatedRegen -= regenToApply; // 적용한 만큼 차감
			
			// 정신력 회복 (최대치 초과 방지)
			currentMentalPower = Mathf.Min(maxMentalPower, currentMentalPower + regenToApply);
			
			// 실제로 회복되었을 때만 이벤트 발생
			if (currentMentalPower != previousMentalPower)
			{
				PlayerStatusEvents.InvokeMentalPowerChanged(currentMentalPower, maxMentalPower);
			}
		}
	}

	/// <summary>
	/// 정신력 소모 시도
	/// </summary>
	/// <param name="amount">소모할 정신력</param>
	/// <returns>소모 성공 여부</returns>
	public bool TryConsumeMentalPower(int amount)
	{
		if (amount <= 0) return true; // 0 이하면 소모 불필요
		if (currentMentalPower < amount) return false; // 부족

		currentMentalPower -= amount;
		OnMentalPowerConsumed(amount);
		return true;
	}

	/// <summary>
	/// 정신력 회복
	/// </summary>
	/// <param name="amount">회복량</param>
	public void RestoreMentalPower(int amount)
	{
		if (amount <= 0) return;

		int previousMentalPower = currentMentalPower;
		currentMentalPower = Mathf.Min(maxMentalPower, currentMentalPower + amount);

		int actualRestored = currentMentalPower - previousMentalPower;
		OnMentalPowerRestored(actualRestored);
	}

	/// <summary>
	/// 정신력 소모 시 호출 (오버라이드용)
	/// </summary>
	protected virtual void OnMentalPowerConsumed(int amount)
	{
		// 이벤트 발생 (UI 업데이트용)
		PlayerStatusEvents.InvokeMentalPowerChanged(currentMentalPower, maxMentalPower);
		PlayerStatusEvents.InvokeMentalPowerConsumed(amount);
	}

	/// <summary>
	/// 정신력 회복 시 호출 (오버라이드용)
	/// </summary>
	protected virtual void OnMentalPowerRestored(int amount)
	{
		// 이벤트 발생 (UI 업데이트용)
		PlayerStatusEvents.InvokeMentalPowerChanged(currentMentalPower, maxMentalPower);
		PlayerStatusEvents.InvokeMentalPowerRestored(amount);
	}

	#endregion

	#region 용기 시스템 관리

	/// <summary>
	/// 용기 점수 변경
	/// </summary>
	/// <param name="amount">변경량 (양수: 증가, 음수: 감소)</param>
	public void ChangeCourageScore(int amount)
	{
		if (amount == 0) return;

		int previousScore = courageScore;
		CourageState previousState = currentCourageState;
		SkillRank previousRank = currentSkillRank;

		courageScore += amount;

		// ⭐ 용기 점수 최소값 제한: 0 이하로 떨어지지 않도록 제한
		courageScore = Mathf.Max(0, courageScore);

		// 용기 단계 재계산
		UpdateCourageState();

		// 단계 변경 이벤트
		if (previousState != currentCourageState)
		{
			OnCourageStateChanged(previousState, currentCourageState);
		}

		// ⭐ 랭크 변경 디버그
		if (previousRank != currentSkillRank)
		{
#if UNITY_EDITOR
			Debug.Log($"[PlayerController] ⭐ 용기 점수 변경으로 인한 랭크 변경: {previousScore} → {courageScore} (변화: {(amount > 0 ? "+" : "")}{amount}), 랭크: {previousRank} → {currentSkillRank}");
#endif
		}
	}

	/// <summary>
	/// 용기 점수 직접 설정
	/// </summary>
	public void SetCourageScore(int score)
	{
		courageScore = score;
		UpdateCourageState();
		CheckDespairState();
	}

	/// <summary>
	/// 용기 단계 업데이트
	/// PlayerDataSO의 요구 점수를 기반으로 현재 단계를 계산합니다
	/// </summary>
	private void UpdateCourageState()
	{
		if (playerData == null) return;

		CourageState newState;
		CourageState previousState = currentCourageState;
		SkillRank previousRank = currentSkillRank;

		// 점수에 따라 단계 결정 (높은 단계부터 체크)
		if (courageScore >= playerData.confidenceStepRequirement)
			newState = CourageState.Confidence;
		else if (courageScore >= playerData.determinationStepRequirement)
			newState = CourageState.Determination;
		else if (courageScore >= playerData.courageStepRequirement)
			newState = CourageState.Courage;
		else if (courageScore >= playerData.doubtStepRequirement)
			newState = CourageState.Doubt;
		else if (courageScore >= playerData.anxietyStepRequirement)
			newState = CourageState.Anxiety;
		else
			newState = CourageState.Despair;

		currentCourageState = newState;

		// 용기 단계에 따라 스킬 랭크 결정
		currentSkillRank = GetSkillRankFromCourageState(currentCourageState);

		// ⭐ 랭크 변경 디버그
		if (previousRank != currentSkillRank)
		{
#if UNITY_EDITOR
			Debug.Log($"[PlayerController] ⭐ 스킬 랭크 변경: {previousRank} → {currentSkillRank} (용기 단계: {previousState} → {currentCourageState}, 용기 점수: {courageScore})");
#endif
		}
	}

	/// <summary>
	/// 용기 단계를 스킬 랭크로 변환
	/// </summary>
	private SkillRank GetSkillRankFromCourageState(CourageState state)
	{
		switch (state)
		{
			case CourageState.Despair:
				return SkillRank.D;
			case CourageState.Anxiety:
				return SkillRank.D;
			case CourageState.Doubt:
				return SkillRank.C;
			case CourageState.Courage:
				return SkillRank.B;
			case CourageState.Determination:
				return SkillRank.A;
			case CourageState.Confidence:
				return SkillRank.S;
			default:
				return SkillRank.D;
		}
	}

	/// <summary>
	/// 절망 상태 체크
	/// 용기 점수가 Despair 이하로 떨어지면 게임오버
	/// </summary>
	private void CheckDespairState()
	{
		if (currentCourageState == CourageState.Despair)
		{
			OnDespairReached();
		}
	}

	/// <summary>
	/// 용기 단계 변경 시 호출 (오버라이드용)
	/// </summary>
	protected virtual void OnCourageStateChanged(CourageState previousState, CourageState newState)
	{
#if UNITY_EDITOR
		Debug.Log($"[PlayerController] 용기 단계 변경: {previousState} → {newState} (스킬 랭크: {currentSkillRank}, 용기 점수: {courageScore})");
#endif
		
		// 자식 클래스 또는 이벤트 시스템에서 처리:
		// - UI 업데이트
		// - 효과음/이펙트
		// - 화면 효과 (단계 상승/하락 연출)
	}

	/// <summary>
	/// 절망 상태 도달 시 호출
	/// </summary>
	protected virtual void OnDespairReached()
	{
		Debug.LogWarning("[PlayerController] 절망 상태 도달! (게임오버 - 현재는 테스트 모드)");
		
		// 나중에 구현:
		// - 게임오버 UI
		// - 씬 전환
		// - 데이터 저장
	}

	/// <summary>
	/// 용기 시스템 업데이트 (매 프레임 호출)
	/// </summary>
	private void UpdateCourageSystem(float deltaTime)
	{
		if (playerData == null) return;

		// 방 활성화 상태 확인
		bool isRoomActive = currentRoomController != null && currentRoomController.IsRoomActive();
		
		// 오래된 기록 정리
		CleanupOldRecords();

		// 전투 상태 업데이트 (방 활성화 중일 때만)
		if (isRoomActive)
		{
			UpdateCombatState(deltaTime);
		}

		// 전투 없음 페널티 체크 (방 활성화 중일 때만)
		if (isRoomActive)
		{
			CheckNoCombatPenalty(deltaTime);
		}

		// 무피해 용기 획득 체크 (방 활성화 중일 때만)
		if (isRoomActive)
		{
			CheckNoDamageCourageGain(deltaTime);
		}
	}

	/// <summary>
	/// 오래된 기록 정리
	/// </summary>
	private void CleanupOldRecords()
	{
		if (playerData == null) return;

		float currentTime = Time.time;

		// 처치 기록 정리 (15초)
		recentKillRecords.RemoveAll(record => currentTime - record.time > playerData.killRecordDuration);

		// 스킬 사용 기록 정리 (3초)
		recentSkillUseRecords.RemoveAll(record => currentTime - record.time > playerData.skillUseRecordDuration);

		// 멀티킬 기록 정리 (3초)
		recentMultiKillRecords.RemoveAll(record => currentTime - record.time > playerData.multiKillJudgmentDuration);

		// 연속 피격 기록 정리 (10초)
		recentHitTimes.RemoveAll(time => currentTime - time > playerData.consecutiveHitJudgmentDuration);
	}

	/// <summary>
	/// 전투 없음 페널티 체크
	/// 소수점 단위로 누적하여 정확하게 계산 (정신력 회복과 동일한 방식)
	/// </summary>
	private void CheckNoCombatPenalty(float deltaTime)
	{
		if (playerData == null) return;

		float currentTime = Time.time;
		float timeSinceLastCombat = currentTime - lastCombatActionTime;
		
		if (timeSinceLastCombat > playerData.noCombatJudgmentDuration)
		{
			// 전투 없음 페널티 누적 (소수점 단위로 정확하게 계산)
			float lossAmount = playerData.noCombatCourageLossPerSecond * deltaTime;
			accumulatedNoCombatLoss += lossAmount;
			
			// 누적 감소량이 1 이상이 되면 정수로 변환하여 적용
			if (accumulatedNoCombatLoss >= 1f)
			{
				int lossToApply = Mathf.FloorToInt(accumulatedNoCombatLoss); // 소수점 버림
				accumulatedNoCombatLoss -= lossToApply; // 적용한 만큼 차감
				
				if (lossToApply > 0)
				{
					ChangeCourageScore(-lossToApply);
				}
			}
		}
		else
		{
		// 전투 행동이 있으면 누적 감소량 초기화
		if (accumulatedNoCombatLoss > 0f)
		{
#if UNITY_EDITOR
			Debug.Log($"[PlayerController] 전투 행동 재개 - 누적 전투 없음 페널티 초기화: {accumulatedNoCombatLoss:F4} 용기");
#endif
			accumulatedNoCombatLoss = 0f;
		}
		}
	}

	/// <summary>
	/// 무피해 용기 획득 체크
	/// 소수점 단위로 누적하여 정확하게 계산 (정신력 회복과 동일한 방식)
	/// 전투 모드가 활성화되어 있을 때만 적용 (isInCombat으로 체크)
	/// </summary>
	private void CheckNoDamageCourageGain(float deltaTime)
	{
		if (playerData == null) return;

		float currentTime = Time.time;
		float timeSinceLastHit = currentTime - lastHitTime;
		
		// ⭐ 전투 모드 활성화 체크: 전투 중이 아니면 무피해 보너스 없음
		if (!isInCombat)
		{
			// 전투 중이 아니면 무피해 보너스 없음 (누적 초기화)
			if (accumulatedNoDamageGain > 0f)
			{
				accumulatedNoDamageGain = 0f;
			}
			return;
		}
		
		// 무피해 용기 획득량 계산 (시간에 따라 다른 보너스)
		float gainPerSecond = 0f;
		if (timeSinceLastHit >= playerData.noDamageJudgmentDuration_20)
		{
			// 20초 이상 무피해 유지
			gainPerSecond = playerData.noDamageCourageGainPerSecond_20;
		}
		else if (timeSinceLastHit >= playerData.noDamageJudgmentDuration_10)
		{
			// 10초 이상 무피해 유지
			gainPerSecond = playerData.noDamageCourageGainPerSecond_10;
		}
		else
		{
			// 10초 미만이면 무피해 보너스 없음 (누적 초기화)
			if (accumulatedNoDamageGain > 0f)
			{
				accumulatedNoDamageGain = 0f;
			}
			return;
		}
		
		// 무피해 용기 획득량 누적 (소수점 단위로 정확하게 계산)
		float gainAmount = gainPerSecond * deltaTime;
		accumulatedNoDamageGain += gainAmount;
		
		// 누적 획득량이 1 이상이 되면 정수로 변환하여 적용
		if (accumulatedNoDamageGain >= 1f)
		{
			int baseGainToApply = Mathf.FloorToInt(accumulatedNoDamageGain); // 소수점 버림
			accumulatedNoDamageGain -= baseGainToApply; // 적용한 만큼 차감
			
			if (baseGainToApply > 0)
			{
				// ⭐ S랭크 궁극기 버프: 용기 획득량 배율 적용 (SO 데이터 사용)
				float courageMultiplier = isUltimateBuffActive 
					? playerData.ultimateBuffCourageGainMultiplier 
					: 1f;
				int finalGainToApply = Mathf.RoundToInt(baseGainToApply * courageMultiplier);
				
				ChangeCourageScore(finalGainToApply);
			}
		}
	}

	/// <summary>
	/// 몬스터 처치 시 용기 획득 처리
	/// MonsterController의 OnMonsterKilledBySkill 이벤트에서 호출
	/// </summary>
	public void OnMonsterKilled(MonsterController monster, SkillSlotType? skillSlotType, DamageSourceType sourceType)
	{
		if (playerData == null) return;

		// 방 활성화 상태 확인
		bool isRoomActive = currentRoomController != null && currentRoomController.IsRoomActive();
		if (!isRoomActive)
		{
#if UNITY_EDITOR
			Debug.Log("[PlayerController] 방이 활성화되지 않아 용기 획득 없음");
#endif
			return;
		}

		// 스킬로 처치한 경우만 처리
		if (!skillSlotType.HasValue)
		{
			return;
		}

		SkillSlotType usedSkill = skillSlotType.Value;
		float currentTime = Time.time;

		// ⭐ 버그 #1 수정: 기본 전투 행동 판정을 처치 기록 추가 전에 수행
		// (이전 처치 기록을 정확하게 확인하기 위해)
		int baseCourageGain = CalculateBaseCombatActionCourageGain(usedSkill);

		// 최근 처치 기록 추가 (15초 유지)
		recentKillRecords.Add(new KillRecord(currentTime, usedSkill));

		// 멀티킬 판정 (3초 이내 동시 처치 체크 - 스킬 필터링 없이 모든 처치 수집)
		// ⭐ 버그 #2 수정: 현재 처치를 포함하여 멀티킬 계산
		int multiKillCount = GetRecentKillCountInDuration(currentTime, playerData.multiKillJudgmentDuration);
		
		// 멀티킬 기록 추가
		recentMultiKillRecords.Add(new MultiKillRecord(currentTime, multiKillCount, usedSkill));
		
		// 멀티킬 보너스 계산
		int multiKillBonus = CalculateMultiKillBonus(multiKillCount);
		
		// ⭐ 버그 #2 수정: 멀티킬 수가 5까지 도달하고 보너스를 받은 후 초기화
		// 초기화 후 현재 처치를 다시 추가하여 다음 처치 계산에 영향 없도록 함
		if (multiKillCount >= 5 && multiKillBonus > 0)
		{
			// 멀티킬 수 5까지 도달하고 보너스를 받았으므로 처치 기록 초기화
			recentKillRecords.Clear();
			recentMultiKillRecords.Clear();
			
			// ⭐ 현재 처치를 다시 추가 (다음 처치 계산을 위해)
			recentKillRecords.Add(new KillRecord(currentTime, usedSkill));
			recentMultiKillRecords.Add(new MultiKillRecord(currentTime, 1, usedSkill));
			
#if UNITY_EDITOR
			Debug.Log($"[PlayerController] ⭐ 멀티킬 수 5까지 도달! 보너스 획득 후 멀티킬 기록 초기화 (멀티킬 수: {multiKillCount}, 보너스: +{multiKillBonus})");
#endif
		}
		
		// ⭐ 버그 #3 수정: 스킬 연계 보너스 계산
		// OnSkillUsed가 먼저 호출되지 않았을 수 있으므로, 현재 스킬 사용 기록이 없으면 추가
		// (CalculateSkillComboBonus는 현재 스킬을 포함하지만, recentSkillUseRecords에 없으면 시간 체크가 안 됨)
		EnsureSkillUseRecord(usedSkill, currentTime);
		int comboBonus = CalculateSkillComboBonus(usedSkill);
		
		// 4종 콤보(풀 체인) 완성 시 스킬 사용 기록 초기화
		if (comboBonus == playerData.combo4Bonus)
		{
			recentSkillUseRecords.Clear();
#if UNITY_EDITOR
			Debug.Log("[PlayerController] ⭐ 4종 풀 체인 완성! 스킬 사용 기록 초기화");
#endif
		}

		// 총 용기 획득량
		int totalCourageGain = baseCourageGain + multiKillBonus + comboBonus;
		
		if (totalCourageGain > 0)
		{
			// ⭐ S랭크 궁극기 버프: 용기 획득량 배율 적용 (SO 데이터 사용)
			float courageMultiplier = isUltimateBuffActive 
				? playerData.ultimateBuffCourageGainMultiplier 
				: 1f;
			int finalCourageGain = Mathf.RoundToInt(totalCourageGain * courageMultiplier);
			
			ChangeCourageScore(finalCourageGain);
#if UNITY_EDITOR
			Debug.Log($"[PlayerController] 용기 획득: +{finalCourageGain} " +
			          $"(기본: {baseCourageGain}, 멀티킬: +{multiKillBonus}, 연계: +{comboBonus}, " +
			          $"{usedSkill} 스킬, 멀티킬 수: {multiKillCount}" +
			          (isUltimateBuffActive ? $" [궁극기 버프 {playerData.ultimateBuffCourageGainMultiplier:F1}배 적용!]" : "") + ")");
#endif
		}

		// 전투 행동 시간 업데이트
		lastCombatActionTime = currentTime;
		
		// ⭐ 무피해 정신력 회복 시작: 방에서 몬스터를 한 대 때린 후부터 카운트 시작
		if (!hasDealtDamageInRoom)
		{
			hasDealtDamageInRoom = true;
			roomNoDamageStartTime = currentTime;
			lastHitTime = currentTime; // 무피해 카운트 시작 시점
#if UNITY_EDITOR
			Debug.Log($"[PlayerController] 무피해 정신력 회복 시작 - 방에서 몬스터 첫 처치 ({currentTime:F2}초)");
#endif
		}
		
		// ⭐ 전투 행동 발생: 전투 시간 연장 및 전투 없음 페널티 누적량 초기화
		HandleCombatAction("몬스터 처치로 인한");
	}

	/// <summary>
	/// 기본 전투 행동 용기 획득량 계산
	/// 스킬로 적 처치 시 기본 용기 획득량을 계산합니다.
	/// 같은 스킬 연속 사용 시 페널티가 적용됩니다.
	/// ⭐ 버그 #1 수정: 처치 기록 추가 전에 호출되므로 이전 처치 기록을 정확하게 확인 가능
	/// </summary>
	/// <param name="currentSkill">현재 처치에 사용한 스킬 타입</param>
	/// <returns>용기 획득량 (기본: +15, 페널티: +10)</returns>
	private int CalculateBaseCombatActionCourageGain(SkillSlotType currentSkill)
	{
		if (playerData == null) return 0;

		// 1. 가장 최근에 죽인 스킬과 현재 스킬 비교
		// 같은 스킬 연속 사용 시 페널티 적용
		// ⭐ 버그 #1 수정: 처치 기록 추가 전에 호출되므로 Count - 1이 이전 처치를 가리킴
		if (recentKillRecords.Count > 0)
		{
			SkillSlotType? lastKillSkill = recentKillRecords[recentKillRecords.Count - 1].skillSlotType;
			
			// 같은 스킬 연속 사용 체크
			if (lastKillSkill.HasValue && lastKillSkill.Value == currentSkill)
			{
				return playerData.sameSkillKillCourageGain; // 페널티: +10
			}
		}

		// 2. 15초 배열에서 3개 이상이면서 반 초과가 현재 스킬인지 체크
		// 단조로운 스킬 사용 패턴 감지
		// ⭐ 버그 #1 수정: 처치 기록 추가 전에 호출되므로 현재 스킬을 제외하고 계산
		if (recentKillRecords.Count >= 2) // 현재 처치를 추가하면 3개가 되므로 2개 이상일 때 체크
		{
			int sameSkillCount = 0;
			foreach (var record in recentKillRecords)
			{
				if (record.skillSlotType == currentSkill)
				{
					sameSkillCount++;
				}
			}

			// 현재 스킬을 포함하여 반 초과가 같은 스킬이면 페널티 (다양한 스킬 사용 유도)
			// 현재 처치를 추가하면 Count + 1이 되므로, (sameSkillCount + 1) > (Count + 1) / 2 체크
			// 즉, sameSkillCount + 1 > (Count + 1) / 2
			// sameSkillCount > (Count - 1) / 2
			if (sameSkillCount > (recentKillRecords.Count - 1) / 2)
			{
				return playerData.sameSkillKillCourageGain; // 페널티: +10
			}
		}

		// 3. 정상 용기 획득 (다양한 스킬 사용)
		return playerData.killCourageGain; // 정상: +15
	}

	/// <summary>
	/// 최근 일정 시간 내 처치 수 계산 (멀티킬 판정용)
	/// 스킬 타입 필터링 없이 모든 처치를 카운트 (동시 처치 판정)
	/// </summary>
	private int GetRecentKillCountInDuration(float currentTime, float duration)
	{
		int count = 0;
		foreach (var record in recentKillRecords)
		{
			// 3초 이내 모든 처치 카운트 (스킬 타입 무관)
			if (currentTime - record.time <= duration)
			{
				count++;
			}
		}
		return count;
	}

	/// <summary>
	/// 멀티킬 보너스 계산
	/// 동시 처치 수에 따라 보너스 용기를 제공합니다.
	/// 3초 이내에 여러 적을 처치하면 보너스가 적용됩니다.
	/// </summary>
	/// <param name="killCount">동시 처치 수 (3초 이내)</param>
	/// <returns>멀티킬 보너스 (2마리: +20, 3마리: +30, 4마리: +40, 5마리+: +50)</returns>
	private int CalculateMultiKillBonus(int killCount)
	{
		if (playerData == null) return 0;

		switch (killCount)
		{
			case 2: return playerData.multiKill2Bonus; // +20
			case 3: return playerData.multiKill3Bonus; // +30
			case 4: return playerData.multiKill4Bonus; // +40
			case >= 5: return playerData.multiKill5Bonus; // +50
			default: return 0;
		}
	}

	/// <summary>
	/// 스킬 사용 기록 보장 (버그 #3 수정용)
	/// OnMonsterKilled에서 스킬 연계 보너스를 계산할 때,
	/// OnSkillUsed가 먼저 호출되지 않았을 수 있으므로 현재 스킬 사용 기록을 보장합니다.
	/// </summary>
	/// <param name="skillType">스킬 타입</param>
	/// <param name="currentTime">현재 시간</param>
	private void EnsureSkillUseRecord(SkillSlotType skillType, float currentTime)
	{
		// 최근 사용 기록에 현재 스킬이 없거나, 가장 최근 기록이 현재 스킬과 다르면 추가
		// (중복 방지: 같은 스킬을 연속으로 사용한 경우가 아닌 경우만 추가)
		bool shouldAdd = true;
		
		if (recentSkillUseRecords.Count > 0)
		{
			var lastRecord = recentSkillUseRecords[recentSkillUseRecords.Count - 1];
			// 가장 최근 기록이 현재 스킬과 같고, 시간이 매우 가까우면 중복 추가 방지
			if (lastRecord.skillSlotType == skillType && (currentTime - lastRecord.time) < 0.1f)
			{
				shouldAdd = false;
			}
		}
		
		if (shouldAdd)
		{
			recentSkillUseRecords.Add(new SkillUseRecord(currentTime, skillType));
		}
	}

	/// <summary>
	/// 스킬 연계 보너스 계산
	/// 5초 이내 사용한 서로 다른 스킬 수에 따라 보너스 제공합니다.
	/// 다양한 스킬을 연속으로 사용하면 폭발적인 용기 상승을 제공합니다.
	/// </summary>
	/// <param name="currentSkill">현재 사용한 스킬 타입</param>
	/// <returns>연계 보너스 (2종: +20, 3종: +30, 4종 풀 체인: +50)</returns>
	private int CalculateSkillComboBonus(SkillSlotType currentSkill)
	{
		if (playerData == null) return 0;

		// 최근 5초 이내 사용한 서로 다른 스킬 수 계산
		float currentTime = Time.time;
		HashSet<SkillSlotType> uniqueSkills = new HashSet<SkillSlotType>();
		
		// 현재 스킬 포함
		uniqueSkills.Add(currentSkill);

		// 최근 사용 기록에서 다른 스킬 추가 (5초 이내)
		// 스킬 사용 후 5초 이내에 다른 스킬 사용 = 연계 유지
		foreach (var record in recentSkillUseRecords)
		{
			if (currentTime - record.time <= playerData.skillUseRecordDuration)
			{
				uniqueSkills.Add(record.skillSlotType);
			}
		}

		int comboCount = uniqueSkills.Count;
		
		switch (comboCount)
		{
			case 2: return playerData.combo2Bonus; // +20
			case 3: return playerData.combo3Bonus; // +30
			case 4: return playerData.combo4Bonus; // +50 (풀 체인!)
			default: return 0;
		}
	}

	/// <summary>
	/// 스킬 사용 시 호출 (스킬 연계 판정용)
	/// SkillSlot 또는 SkillController에서 호출
	/// </summary>
	public void OnSkillUsed(SkillSlotType skillType)
	{
		if (playerData == null) return;

		float currentTime = Time.time;
		
		// 최근 사용 기록 추가 (5초 유지 - 스킬 연계 판정용)
		recentSkillUseRecords.Add(new SkillUseRecord(currentTime, skillType));

		// 전투 행동 시간 업데이트
		lastCombatActionTime = currentTime;
		
		// ⭐ 전투 행동 발생: 전투 시간 연장 및 전투 없음 페널티 누적량 초기화
		HandleCombatAction("스킬 사용으로 인한");
	}

	/// <summary>
	/// 궁극기 사용 시 호출
	/// S랭크에서 스킬을 사용했을 때 호출됨 (궁극기 실행 후)
	/// </summary>
	public void OnUltimateSkillUsed()
	{
		if (playerData == null) return;

#if UNITY_EDITOR
		Debug.Log($"[PlayerController] ⭐⭐⭐ 궁극기 사용! S랭크에서 스킬 사용 - 즉시 D랭크로 추락 및 용기 0 리셋");
#endif
		
		// ⭐ S랭크 사용 후 초기화: 용기 0으로 리셋 및 D랭크로 추락
		ResetCourageSystemAfterUltimate();
		
		// S랭크 궁극기 버프 발동 (SO 데이터에서 지속 시간 가져오기)
		float buffDuration = playerData != null ? playerData.ultimateBuffDuration : 15f;
		ActivateUltimateBuff(buffDuration);
	}
	
	/// <summary>
	/// S랭크 사용 후 용기 시스템 초기화
	/// 용기 0으로 리셋하고 D랭크로 추락시킵니다.
	/// </summary>
	private void ResetCourageSystemAfterUltimate()
	{
		// 용기 점수 0으로 리셋
		SetCourageScore(0);
		
		// ⭐ 방 활성화 중일 때만 기록 초기화 (방 클리어 시와 동일)
		bool isRoomActive = currentRoomController != null && currentRoomController.IsRoomActive();
		if (isRoomActive)
		{
			// 기록 초기화 (다음 전투를 위해)
			recentKillRecords.Clear();
			recentSkillUseRecords.Clear();
			recentMultiKillRecords.Clear();
			recentHitTimes.Clear();
			
			// 타이머 초기화
			float currentTime = Time.time;
			lastCombatActionTime = currentTime;
			lastHitTime = currentTime;
			roomNoDamageStartTime = currentTime;
			
			// 누적량 초기화
			accumulatedNoCombatLoss = 0f;
			accumulatedNoDamageGain = 0f;
			
			// 전투 상태 초기화
			isInCombat = false;
			combatEndTime = 0f;
			hasDealtDamageInRoom = false;
			
#if UNITY_EDITOR
			Debug.Log($"[PlayerController] ⭐ S랭크 사용 후 용기 시스템 초기화 완료 (용기: {courageScore}, 랭크: {currentSkillRank})");
#endif
		}
	}


	/// <summary>
	/// 플레이어 피격 처리
	/// 플레이어가 몬스터에게 데미지를 입었을 때 호출됨
	/// OnDamaged()에서 호출되어 실제 데미지가 들어간 경우에만 실행됨
	/// </summary>
	private void OnPlayerHit(object damageSource)
	{
		if (playerData == null) return;

		// 방 활성화 상태 확인 (피격 페널티는 방 활성화 중일 때만 적용)
		bool isRoomActive = currentRoomController != null && currentRoomController.IsRoomActive();
		if (!isRoomActive)
		{
			return;
		}

		float currentTime = Time.time;
		lastHitTime = currentTime;
		lastCombatActionTime = currentTime;

		// ⭐ 피격 시 무피해 정신력 회복 리셋: 무피해 카운트 시작 시간을 현재 시간으로 설정
		if (hasDealtDamageInRoom)
		{
			roomNoDamageStartTime = currentTime;
			accumulatedRegen = 0f; // 누적 회복량 초기화
#if UNITY_EDITOR
			Debug.Log($"[PlayerController] 피격으로 인한 무피해 정신력 회복 리셋 ({currentTime:F2}초)");
#endif
		}

		// ⭐ 플레이어가 몬스터에게 데미지를 입었을 때 전투 시간 연장 (전투 중이 아니면 전투 재개, 전투 중이면 15초 추가)
		// 이 위치가 플레이어가 실제로 데미지를 입은 시점입니다!
		ExtendCombatTime(isHit: true);
		
		// ⭐ 전투 행동 발생: 전투 없음 페널티 누적량 초기화
		ResetNoCombatPenalty("피격으로 인한");

		// ⭐ 피격 시 무피해 용기 획득 누적량 초기화 (피격당했으므로 무피해 보너스 없음)
		if (accumulatedNoDamageGain > 0f)
		{
#if UNITY_EDITOR
			Debug.Log($"[PlayerController] 피격으로 인한 무피해 용기 획득 누적량 초기화: {accumulatedNoDamageGain:F4} 용기");
#endif
			accumulatedNoDamageGain = 0f;
		}

		// 피격 기록 추가 (10초 유지)
		recentHitTimes.Add(currentTime);

		// 데미지 소스 타입 확인
		DamageSourceInfo damageInfo = DamageSourceInfo.FromDamageSource(damageSource);
		
		// 연속 피격 보호 메커니즘
		int courageLoss = CalculateHitCourageLoss(damageInfo.sourceType, recentHitTimes.Count);

		if (courageLoss > 0)
		{
			ChangeCourageScore(-courageLoss);
#if UNITY_EDITOR
			Debug.Log($"[PlayerController] 피격 용기 감소: -{courageLoss} (소스 타입: {damageInfo.sourceType}, 연속 피격 수: {recentHitTimes.Count})");
#endif
		}
	}

	/// <summary>
	/// 피격 용기 감소량 계산 (연속 피격 보호 포함)
	/// </summary>
	private int CalculateHitCourageLoss(DamageSourceType sourceType, int consecutiveHitCount)
	{
		if (playerData == null) return 0;

		int baseLoss = 0;
		
		// 소스 타입에 따른 기본 감소량
		switch (sourceType)
		{
			case DamageSourceType.Projectile:
				baseLoss = playerData.projectileHitCourageLoss;
				break;
			case DamageSourceType.Contact:
				baseLoss = playerData.contactHitCourageLoss;
				break;
			case DamageSourceType.Environment:
				baseLoss = playerData.environmentHitCourageLoss;
				break;
			default:
				baseLoss = playerData.projectileHitCourageLoss; // 기본값 (예상치 못한 타입)
				break;
		}

		// 연속 피격 보호 적용
		if (consecutiveHitCount == 2)
		{
			baseLoss = playerData.consecutiveHit2CourageLoss;
		}
		else if (consecutiveHitCount >= 3)
		{
			baseLoss = playerData.consecutiveHit3PlusCourageLoss;
		}

		return baseLoss;
	}

	/// <summary>
	/// 완벽한 회피 시 용기 획득
	/// 외부에서 호출 (회피 시스템에서)
	/// </summary>
	public void OnPerfectDodge()
	{
		if (playerData == null) return;

		// 방 활성화 상태 확인
		bool isRoomActive = currentRoomController != null && currentRoomController.IsRoomActive();
		if (!isRoomActive)
		{
			return;
		}

		int baseCourageGain = playerData.perfectDodgeCourageGain;
		
			// ⭐ S랭크 궁극기 버프: 용기 획득량 배율 적용 (SO 데이터 사용)
			float courageMultiplier = isUltimateBuffActive 
				? playerData.ultimateBuffCourageGainMultiplier 
				: 1f;
			int finalCourageGain = Mathf.RoundToInt(baseCourageGain * courageMultiplier);
		
		if (finalCourageGain > 0)
		{
			ChangeCourageScore(finalCourageGain);
#if UNITY_EDITOR
			Debug.Log($"[PlayerController] 완벽한 회피 용기 획득: +{finalCourageGain}" +
			          (isUltimateBuffActive ? $" [궁극기 버프 {playerData.ultimateBuffCourageGainMultiplier:F1}배 적용!]" : ""));
#endif
		}

		// 전투 행동 시간 업데이트
		lastCombatActionTime = Time.time;
	}

	/// <summary>
	/// 현재 방 컨트롤러 설정
	/// RoomController의 OnTriggerEnter2D에서 호출
	/// </summary>
	public void SetCurrentRoomController(RoomController roomController)
	{
		// 방 클리어 시 기록 초기화
		if (currentRoomController != null && roomController != currentRoomController)
		{
			OnRoomCleared();
		}

		currentRoomController = roomController;
#if UNITY_EDITOR
		Debug.Log($"[PlayerController] 현재 방 컨트롤러 설정: {roomController?.name ?? "null"}");
#endif
		
		// ⭐ 방 진입 시 타이머 초기화 (무피해 보너스 및 전투 없음 페널티 리셋)
		if (roomController != null && roomController.IsRoomActive())
		{
			float currentTime = Time.time;
			
			// ⭐ 무피해 보너스 타이머 초기화 (방 진입 시점부터 카운트 시작)
			lastHitTime = currentTime;
			
			// ⭐ 전투 없음 페널티 타이머 초기화 (방 진입 시점부터 카운트 시작)
			lastCombatActionTime = currentTime;
			
			// ⭐ 누적량 초기화 (방 진입 시 리셋)
			accumulatedNoCombatLoss = 0f;
			accumulatedNoDamageGain = 0f;
			
			// ⭐ 무피해 정신력 회복 시스템 초기화
			hasDealtDamageInRoom = false;
			roomNoDamageStartTime = 0f;
			
#if UNITY_EDITOR
			Debug.Log($"[PlayerController] ⭐ 방 진입 시 타이머 초기화 완료 (무피해 보너스 및 전투 없음 페널티 리셋)");
#endif
			
			// ⭐ 방 진입 시 전투 시작
			StartCombat();
		}
	}

	/// <summary>
	/// 방 클리어 시 용기 시스템 초기화
	/// </summary>
	private void OnRoomCleared()
	{
		// 기록 초기화
		recentKillRecords.Clear();
		recentSkillUseRecords.Clear();
		recentMultiKillRecords.Clear();
		recentHitTimes.Clear();

		// 타이머 초기화 (현재 시간으로 설정하여 다음 방에서 올바른 타이머 계산)
		float currentTime = Time.time;
		lastCombatActionTime = currentTime;
		lastHitTime = currentTime; // 방 진입 시점 기준으로 무피해 보너스 계산

		// 누적량 초기화
		accumulatedNoCombatLoss = 0f;
		accumulatedNoDamageGain = 0f;
		accumulatedRegen = 0f; // 정신력 회복 누적량 초기화

		// ⭐ 무피해 정신력 회복 시스템 초기화
		hasDealtDamageInRoom = false;
		roomNoDamageStartTime = 0f;

		// 전투 상태 초기화
		isInCombat = false;
		combatEndTime = 0f;

#if UNITY_EDITOR
		Debug.Log("[PlayerController] 방 클리어 - 용기 시스템 기록 초기화 (누적량, 전투 상태, 무피해 정신력 회복 포함)");
#endif
	}

	#endregion

	#region S랭크 궁극기 시스템

	/// <summary>
	/// S랭크 궁극기 버프 발동
	/// </summary>
	/// <param name="duration">버프 지속 시간 (초)</param>
	private void ActivateUltimateBuff(float duration)
	{
		if (playerData == null) return;
		
		float currentTime = Time.time;
		isUltimateBuffActive = true;
		ultimateBuffEndTime = currentTime + duration;
		
#if UNITY_EDITOR
		Debug.Log($"[PlayerController] ⭐⭐⭐ S랭크 궁극기 버프 발동! (지속 시간: {duration}초)\n" +
		          $"  - 용기 획득량 증가: {playerData.ultimateBuffCourageGainMultiplier:F1}배\n" +
		          $"  - 정신력 회복 속도 증가: {playerData.ultimateBuffMentalPowerRegenMultiplier:F1}배\n" +
		          $"  - 이동 속도 증가: {playerData.ultimateBuffMoveSpeedMultiplier:F1}배");
#endif
	}

	/// <summary>
	/// S랭크 궁극기 버프 업데이트
	/// 매 프레임 호출되어 버프 종료 시간을 체크
	/// </summary>
	private void UpdateUltimateBuff(float deltaTime)
	{
		if (!isUltimateBuffActive) return;

		float currentTime = Time.time;
		
		// 버프 종료 시간 체크
		if (currentTime >= ultimateBuffEndTime)
		{
			// 버프 종료
			isUltimateBuffActive = false;
			ultimateBuffEndTime = 0f;
			
#if UNITY_EDITOR
			Debug.Log("[PlayerController] ⭐ S랭크 궁극기 버프 종료");
#endif
		}
	}

	/// <summary>
	/// 전투 행동 처리 (개선 #9: 중복 로직 리팩토링)
	/// 전투 시간 연장 및 전투 없음 페널티 누적량 초기화를 통합 처리합니다.
	/// </summary>
	/// <param name="actionDescription">행동 설명 (로그용)</param>
	private void HandleCombatAction(string actionDescription)
	{
		ExtendCombatTime();
		ResetNoCombatPenalty(actionDescription);
	}

	/// <summary>
	/// 전투 없음 페널티 초기화 (개선 #9: 중복 로직 리팩토링)
	/// </summary>
	/// <param name="actionDescription">행동 설명 (로그용)</param>
	private void ResetNoCombatPenalty(string actionDescription)
	{
		if (accumulatedNoCombatLoss > 0f)
		{
#if UNITY_EDITOR
			Debug.Log($"[PlayerController] {actionDescription} 전투 행동 재개 - 누적 전투 없음 페널티 초기화: {accumulatedNoCombatLoss:F4} 용기");
#endif
			accumulatedNoCombatLoss = 0f;
		}
	}

	#endregion

	#region 전투 시간 시스템

	/// <summary>
	/// 전투 시작
	/// 방 진입 시 호출되어 기본 전투 시간만큼 전투 활성화
	/// </summary>
	private void StartCombat()
	{
		if (playerData == null) return;

		float currentTime = Time.time;
		isInCombat = true;
		combatEndTime = currentTime + playerData.initialCombatDuration;

		float remainingTime = combatEndTime - currentTime;
#if UNITY_EDITOR
		Debug.Log($"[PlayerController] ⚔️ 전투 시작!\n" +
		          $"  - 전투 상태: 전투 중 (isInCombat = true)\n" +
		          $"  - 현재 시간: {currentTime:F2}초\n" +
		          $"  - 전투 종료 예정 시간: {combatEndTime:F2}초\n" +
		          $"  - 남은 전투 시간: {remainingTime:F2}초\n" +
		          $"  - 기본 전투 시간: {playerData.initialCombatDuration}초");
#endif
	}

	/// <summary>
	/// 전투 시간 연장
	/// 전투 행동(공격/스킬 사용/피격) 발생 시 호출되어 전투 시간을 연장
	/// 전투가 종료되어 있으면 전투 재개
	/// </summary>
	/// <param name="isHit">피격으로 인한 호출인지 여부 (피격 시 true, 일반 전투 행동 시 false)</param>
	private void ExtendCombatTime(bool isHit = false)
	{
		if (playerData == null) return;

		float currentTime = Time.time;

		// 전투 중이 아니면 전투 재개
		if (!isInCombat)
		{
			// 전투 종료 후 15초 안에 전투 행동이 발생했으면 전투 재개
			float timeSinceLastCombat = currentTime - lastCombatActionTime;
			if (timeSinceLastCombat <= playerData.combatEndJudgmentDuration && lastCombatActionTime > 0f)
			{
				StartCombat();
				return;
			}
			else
			{
				// 15초 초과면 재개 불가
				return;
			}
		}

		// 전투 중이면 전투 시간 연장
		float newCombatEndTime = currentTime + playerData.combatExtensionDuration;
		float previousEndTime = combatEndTime;

		// 기존 종료 시간과 새로 계산한 시간 중 더 늦은 시간으로 설정
		// (피격 여부와 관계없이 항상 최소한 현재 시간 + 연장 시간은 보장)
		combatEndTime = Mathf.Max(combatEndTime, newCombatEndTime);
		
		float remainingTime = combatEndTime - currentTime;
		
		// 로그 메시지 생성
		string actionType = isHit ? "피격으로 인한" : "";
		string logMessage = combatEndTime > previousEndTime 
			? $"[PlayerController] ⚔️ 전투 중 - {actionType}전투 시간 연장!\n" +
			  $"  - 전투 상태: 전투 중 (isInCombat = true)\n" +
			  $"  - 현재 시간: {currentTime:F2}초\n" +
			  $"  - 이전 종료 시간: {previousEndTime:F2}초\n" +
			  $"  - 새로운 종료 시간: {combatEndTime:F2}초\n" +
			  $"  - 남은 전투 시간: {remainingTime:F2}초\n" +
			  $"  - 연장 시간: {playerData.combatExtensionDuration}초"
			: $"[PlayerController] ⚔️ 전투 중 - {actionType}전투 시간 연장 시도 (기존 종료 시간이 더 늦음)\n" +
			  $"  - 전투 상태: 전투 중 (isInCombat = true)\n" +
			  $"  - 현재 시간: {currentTime:F2}초\n" +
			  $"  - 전투 종료 예정 시간: {combatEndTime:F2}초 (변경 없음, 기존 시간이 더 늦음)\n" +
			  $"  - 남은 전투 시간: {remainingTime:F2}초\n" +
			  $"  - 계산된 새로운 종료 시간: {newCombatEndTime:F2}초 (기존 시간이 더 늦어서 적용 안 됨)";
		
#if UNITY_EDITOR
		Debug.Log(logMessage);
#endif
	}

	/// <summary>
	/// 전투 상태 업데이트
	/// 매 프레임 호출되어 전투 종료 여부를 체크
	/// </summary>
	private void UpdateCombatState(float deltaTime)
	{
		if (playerData == null) return;

		float currentTime = Time.time;

		// 전투 중이고 전투 종료 시간이 지났으면 전투 종료
		if (isInCombat && currentTime >= combatEndTime)
		{
			EndCombat();
		}
		else if (isInCombat)
		{
			// 전투 중일 때 남은 시간 로그 (주기적으로 출력하지 않고, 필요한 경우만)
			// 디버그용: 매 5초마다 한 번씩 출력 (선택사항)
			// float remainingTime = combatEndTime - currentTime;
			// Debug.Log($"[PlayerController] ⚔️ 전투 중 - 남은 시간: {remainingTime:F2}초");
		}
		else
		{
			// 전투 종료 상태
			// Debug.Log($"[PlayerController] ⚔️ 전투 종료 상태 (isInCombat = false)");
		}
	}

	/// <summary>
	/// 전투 종료
	/// 전투 종료 시간이 지나거나 전투 행동이 없을 때 호출
	/// </summary>
	private void EndCombat()
	{
		if (!isInCombat) return;

		float currentTime = Time.time;
		isInCombat = false;
		combatEndTime = 0f;

		// 전투 종료 시 무피해 보너스 누적량 초기화
		if (accumulatedNoDamageGain > 0f)
		{
#if UNITY_EDITOR
			Debug.Log($"[PlayerController] 전투 종료 - 무피해 용기 획득 누적량 초기화: {accumulatedNoDamageGain:F4} 용기");
#endif
			accumulatedNoDamageGain = 0f;
		}

#if UNITY_EDITOR
		Debug.Log($"[PlayerController] ⚔️ 전투 종료!\n" +
		          $"  - 전투 상태: 전투 종료 (isInCombat = false)\n" +
		          $"  - 현재 시간: {currentTime:F2}초\n" +
		          $"  - 전투 종료 시간: {combatEndTime:F2}초\n" +
		          $"  - 다음 전투 행동까지 {playerData.combatEndJudgmentDuration}초 안에 전투 재개 가능");
#endif
	}

	#endregion

	#region 이동 관리

	/// <summary>
	/// 이동 입력 업데이트
	/// </summary>
	private void UpdateMovementInput()
	{
		if (moveAction != null)
		{
			moveInput = moveAction.ReadValue<Vector2>();
		}
	}

	#endregion

	#region 넉백 시스템

	/// <summary>
	/// 넉백 적용
	/// 트랩 등에서 호출하여 플레이어를 넉백시킵니다.
	/// </summary>
	/// <param name="direction">넉백 방향 (정규화하지 않아도 됨)</param>
	/// <param name="force">넉백 힘</param>
	/// <param name="duration">넉백 지속 시간 (초)</param>
	public void ApplyKnockback(Vector2 direction, float force, float duration)
	{
		if (!isAlive) return; // 사망 시 넉백 불가
		if (force <= 0f || duration <= 0f) return; // 유효하지 않은 값 무시

		// 넉백 데이터 설정
		knockbackDirection = direction.normalized; // 방향 정규화
		knockbackForce = force;
		knockbackDuration = duration;
		knockbackElapsedTime = 0f;
		isKnockedBack = true;

#if UNITY_EDITOR
		Debug.Log($"[PlayerController] 넉백 적용 - 방향: {knockbackDirection}, 힘: {force}, 시간: {duration}초");
#endif
	}

	/// <summary>
	/// 넉백 타이머 업데이트
	/// </summary>
	/// <param name="deltaTime">델타 타임</param>
	private void UpdateKnockbackTimer(float deltaTime)
	{
		if (!isKnockedBack) return;

		// 경과 시간 증가
		knockbackElapsedTime += deltaTime;

		// 넉백 시간 종료 체크
		if (knockbackElapsedTime >= knockbackDuration)
		{
			// 넉백 종료
			EndKnockback();
		}
	}

	/// <summary>
	/// 넉백 처리 (FixedUpdate에서 호출)
	/// 감쇠 처리 포함
	/// </summary>
	private void ProcessKnockback()
	{
		if (!isKnockedBack) return;

		// 감쇠 계산 (시간이 지날수록 힘이 감소)
		// 선형 감쇠: 1.0 → 0.0 (시간에 비례)
		float dampingFactor = 1f - (knockbackElapsedTime / knockbackDuration);
		
		// 최소값 보장 (0보다 크게 유지)
		dampingFactor = Mathf.Max(0f, dampingFactor);

		// 현재 넉백 힘 계산 (초기 힘 × 감쇠 계수)
		float currentForce = knockbackForce * dampingFactor;

		// 넉백 velocity 적용
		rb.linearVelocity = knockbackDirection * currentForce;

		// 이동 방향은 None으로 설정 (넉백 중에는 이동 입력 무시)
		currentMoveDirection = MoveDirection.None;
	}

	/// <summary>
	/// 넉백 종료
	/// </summary>
	private void EndKnockback()
	{
		isKnockedBack = false;
		knockbackElapsedTime = 0f;
		knockbackForce = 0f;
		knockbackDuration = 0f;

		// 넉백 종료 시 속도는 0으로 설정 (이동 입력이 없으면 정지)
		// 이동 입력이 있으면 다음 ProcessMovement에서 처리됨
		rb.linearVelocity = Vector2.zero;

#if UNITY_EDITOR
		Debug.Log("[PlayerController] 넉백 종료");
#endif
	}

	/// <summary>
	/// 넉백 중인지 확인
	/// </summary>
	/// <returns>넉백 중 여부</returns>
	public bool IsKnockedBack()
	{
		return isKnockedBack;
	}

	/// <summary>
	/// 애니메이터 파라미터 및 스프라이트 방향 업데이트
	/// </summary>
	private void UpdateAnimatorParameters()
	{
		if (animator == null) return;

		bool isMoving = moveInput.sqrMagnitude > 0.01f;
		Vector2 dir = isMoving ? moveInput.normalized : GetDirectionVector(lastMoveDirection);

		animator.SetBool("isMove", isMoving);
		animator.SetFloat("InputX", dir.x);
		animator.SetFloat("InputY", dir.y);
		
		// ⭐ 스프라이트 방향 전환 (왼쪽/오른쪽)
		UpdateSpriteDirection(dir);
	}
	
	/// <summary>
	/// 스프라이트 방향 전환 (좌우 반전)
	/// </summary>
	/// <param name="direction">현재 이동 방향</param>
	private void UpdateSpriteDirection(Vector2 direction)
	{
		if (spriteRenderer == null) return;
		
		// X 방향이 거의 0이면 방향 전환하지 않음 (위/아래 이동 시)
		if (Mathf.Abs(direction.x) < 0.01f) return;
		
		// 왼쪽으로 이동 시 스프라이트 반전
		if (direction.x < 0)
		{
			spriteRenderer.flipX = true; // 왼쪽 보기
		}
		// 오른쪽으로 이동 시 스프라이트 원래대로
		else if (direction.x > 0)
		{
			spriteRenderer.flipX = false; // 오른쪽 보기 (기본)
		}
	}

	/// <summary>
	/// 물리 기반 이동 처리
	/// </summary>
	private void ProcessMovement()
	{
		// ⭐ 넉백 우선 처리 (이동 입력 체크 전에 처리)
		// 넉백 중에는 넉백 처리만 하고 일반 이동 처리는 하지 않음
		if (isKnockedBack)
		{
			ProcessKnockback();
			return; // 넉백 중에는 일반 이동 처리 건너뛰기
		}

		// 이동 불가 상태 체크
		if (!CanMove())
		{
			// 즉시 정지 (가속도 없음)
			rb.linearVelocity = Vector2.zero;
			currentMoveDirection = MoveDirection.None;
			return;
		}

		// 이동 입력이 없으면 즉시 정지
		if (moveInput.sqrMagnitude < 0.01f)
		{
			// 즉시 정지 (가속도 없음)
			rb.linearVelocity = Vector2.zero;
			currentMoveDirection = MoveDirection.None;
			return;
		}

		// 현재 이동 방향 계산 (8방향)
		currentMoveDirection = GetMoveDirectionFromInput(moveInput);

		// 이동 중이면 마지막 방향 업데이트
		if (currentMoveDirection != MoveDirection.None)
		{
			lastMoveDirection = currentMoveDirection;
		}

		// 이동속도 계산 (기본 속도 × 상태효과 배율)
		float moveSpeed = playerData != null ? playerData.moveSpeed : 5f;
		float speedMultiplier = GetMoveSpeedMultiplier(); // EntityController에서 제공
		float finalSpeed = moveSpeed * speedMultiplier;

		// ⭐ Dynamic Rigidbody2D는 velocity 사용 (물리 충돌 자동 처리)
		// 즉시 속도 설정 (가속도 영향 없음 - 높은 드래그로 제어)
		rb.linearVelocity = moveInput.normalized * finalSpeed;
	}

	/// <summary>
	/// 입력 벡터를 8방향으로 변환
	/// </summary>
	/// <param name="input">입력 벡터</param>
	/// <returns>8방향 중 하나</returns>
	private MoveDirection GetMoveDirectionFromInput(Vector2 input)
	{
		// 입력이 거의 없으면 None
		if (input.sqrMagnitude < 0.01f)
		{
			return MoveDirection.None;
		}

		// 각도 계산 (Atan2는 -180 ~ 180 범위)
		float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;

		// 0 ~ 360 범위로 정규화
		if (angle < 0) angle += 360f;

		// 8방향으로 변환 (각 방향은 45도씩)
		// Right: 337.5~22.5, UpRight: 22.5~67.5, Up: 67.5~112.5, ...
		if (angle >= 337.5f || angle < 22.5f)
			return MoveDirection.Right;
		else if (angle >= 22.5f && angle < 67.5f)
			return MoveDirection.UpRight;
		else if (angle >= 67.5f && angle < 112.5f)
			return MoveDirection.Up;
		else if (angle >= 112.5f && angle < 157.5f)
			return MoveDirection.UpLeft;
		else if (angle >= 157.5f && angle < 202.5f)
			return MoveDirection.Left;
		else if (angle >= 202.5f && angle < 247.5f)
			return MoveDirection.DownLeft;
		else if (angle >= 247.5f && angle < 292.5f)
			return MoveDirection.Down;
		else // 292.5 ~ 337.5
			return MoveDirection.DownRight;
	}

	/// <summary>
	/// 방향을 Vector2로 변환 (스킬 발사 방향 등에 사용)
	/// </summary>
	/// <param name="direction">방향</param>
	/// <returns>정규화된 방향 벡터</returns>
	public Vector2 GetDirectionVector(MoveDirection direction)
	{
		switch (direction)
		{
			case MoveDirection.Up:
				return Vector2.up;
			case MoveDirection.Down:
				return Vector2.down;
			case MoveDirection.Left:
				return Vector2.left;
			case MoveDirection.Right:
				return Vector2.right;
			case MoveDirection.UpLeft:
				return (Vector2.up + Vector2.left).normalized;
			case MoveDirection.UpRight:
				return (Vector2.up + Vector2.right).normalized;
			case MoveDirection.DownLeft:
				return (Vector2.down + Vector2.left).normalized;
			case MoveDirection.DownRight:
				return (Vector2.down + Vector2.right).normalized;
			default:
				return Vector2.down; // 기본값
		}
	}

	/// <summary>
	/// 마지막 이동 방향의 벡터 가져오기
	/// </summary>
	public Vector2 GetLastDirectionVector()
	{
		return GetDirectionVector(lastMoveDirection);
	}

	/// <summary>
	/// 현재 이동속도 가져오기
	/// </summary>
	public float GetCurrentMoveSpeed()
	{
		if (playerData == null) return 0f;
		return playerData.moveSpeed * GetMoveSpeedMultiplier();
	}

	/// <summary>
	/// 이동 중인지 확인
	/// </summary>
	public bool IsMoving()
	{
		return currentMoveDirection != MoveDirection.None;
	}

	#endregion

	#region 스킬 연동

	/// <summary>
	/// 이동 가능 여부 체크 (오버라이드)
	/// EntityController의 기본 체크에 넉백 체크 추가
	/// </summary>
	public override bool CanMove()
	{
		// 기본 체크 (생존, 상태효과)
		if (!base.CanMove()) return false;

		// 넉백 중에는 이동 입력 차단 (넉백 자체는 ProcessKnockback에서 처리)
		if (isKnockedBack) return false;

		return true;
	}

	/// <summary>
	/// 스킬 사용 가능 여부 체크 (오버라이드)
	/// EntityController의 기본 체크에 정신력 체크 추가
	/// </summary>
	public override bool CanUseSkill()
	{
		// 기본 체크 (생존, 상태효과)
		if (!base.CanUseSkill()) return false;

		// 넉백 중에는 스킬 사용 불가
		if (isKnockedBack) return false;

		// 정신력 체크는 개별 스킬에서 수행
		// 여기서는 기본 조건만 체크
		return true;
	}
	
	/// <summary>
	/// 이동 속도 배율 가져오기 (오버라이드)
	/// EntityController의 기본 배율에 S랭크 궁극기 버프 배율 추가
	/// </summary>
	public override float GetMoveSpeedMultiplier()
	{
		// 기본 배율 (상태효과 등)
		float baseMultiplier = base.GetMoveSpeedMultiplier();
		
		// ⭐ S랭크 궁극기 버프: 이동 속도 배율 적용 (SO 데이터 사용)
		if (isUltimateBuffActive && playerData != null)
		{
			baseMultiplier *= playerData.ultimateBuffMoveSpeedMultiplier;
		}
		
		return baseMultiplier;
	}

	/// <summary>
	/// 스킬 사용 시 정신력 소모 처리
	/// SkillSlot 또는 SkillHandler에서 호출
	/// </summary>
	/// <param name="mentalPowerCost">소모할 정신력</param>
	/// <returns>소모 성공 여부</returns>
	public bool TryUseSkillWithMentalPower(int mentalPowerCost)
	{
		if (!CanUseSkill()) return false;
		if (!TryConsumeMentalPower(mentalPowerCost)) return false;

		return true;
	}

	#endregion

	#region 죽음 처리 (오버라이드)

	/// <summary>
	/// 플레이어 피격 처리
	/// EntityController의 OnDamaged()를 오버라이드
	/// </summary>
	protected override void OnDamaged(int damageAmount, object damageSource)
	{
		base.OnDamaged(damageAmount, damageSource);
		
		// 체력 변경 이벤트 발생 (플레이어 전용)
		PlayerStatusEvents.InvokeHealthChanged(CurrentHealth, MaxHealth);
		
		// 카메라 쉐이크: 데미지 크기에 비례
		if (damageAmount > 0 && hitShake != null && camShake != null)
		{
			hitShake.ApplyShake(camShake, damageAmount);
		}
		
		// ⭐ 피격 애니메이션 재생
		if (animator != null && isAlive)
		{
			animator.SetTrigger("Hurt");
		}
		
		// ⭐ 피격 후 무적 시간 부여
		if (playerData != null && playerData.hitInvincibilityDuration > 0f)
		{
			ApplyHitInvincibility();
		}
		
		// ⭐ 피격 시 용기 감소 처리 (실제 데미지가 들어간 경우에만)
		if (damageAmount > 0)
		{
			OnPlayerHit(damageSource);
		}
	}
	
	/// <summary>
	/// 피격 무적 시간 적용 (무적 + 깜빡임 효과)
	/// </summary>
	private void ApplyHitInvincibility()
	{
		if (playerData == null) return;
		
		// 무적 시간 설정
		SetInvincibleForDuration(playerData.hitInvincibilityDuration);
		
		// 깜빡임 효과 시작
		if (playerData.useBlinkEffect && spriteRenderer != null)
		{
			// 기존 깜빡임 코루틴이 있으면 중지
			if (blinkCoroutine != null)
			{
				StopCoroutine(blinkCoroutine);
			}
			
			blinkCoroutine = StartCoroutine(BlinkEffect(playerData.hitInvincibilityDuration));
		}
	}
	
	/// <summary>
	/// 스프라이트 깜빡임 효과 코루틴 (빨간색으로 변경)
	/// </summary>
	private System.Collections.IEnumerator BlinkEffect(float duration)
	{
		if (spriteRenderer == null || playerData == null) yield break;
		
		// 원래 색상 저장
		Color originalColor = spriteRenderer.color;
		
		float elapsed = 0f;
		bool isHitColor = false;
		
		while (elapsed < duration)
		{
			// 깜빡임 주기마다 색상 토글
			yield return new WaitForSeconds(playerData.blinkInterval);
			
			isHitColor = !isHitColor;
			spriteRenderer.color = isHitColor ? playerData.hitColor : originalColor;
			
			elapsed += playerData.blinkInterval;
		}
		
		// 무적 시간 종료 - 원래 색상으로 복구
		spriteRenderer.color = originalColor;
		blinkCoroutine = null;
	}
	
	/// <summary>
	/// 플레이어 회복 처리
	/// EntityController의 OnHealed()를 오버라이드
	/// </summary>
	protected override void OnHealed(int healedAmount)
	{
		base.OnHealed(healedAmount);
		
		// 체력 변경 이벤트 발생 (플레이어 전용)
		PlayerStatusEvents.InvokeHealthChanged(CurrentHealth, MaxHealth);
	}
	
	/// <summary>
	/// 플레이어 사망 처리
	/// EntityController의 Die()를 오버라이드
	/// </summary>
	protected override void Die()
	{
		base.Die(); // 기본 사망 처리

#if UNITY_EDITOR
		Debug.Log("[PlayerController] 플레이어 사망 (체력 0)");
#endif

		// 플레이어 전용 사망 처리
		OnPlayerDeath();
	}

	/// <summary>
	/// 플레이어 사망 시 추가 처리
	/// </summary>
	protected virtual void OnPlayerDeath()
	{
#if UNITY_EDITOR
		Debug.Log("[PlayerController] 플레이어 사망 이벤트 발생");
#endif
		
		// 입력 비활성화
		if (inputActions != null)
		{
			inputActions.Disable();
		}
		
		// 이동 정지
		if (rb != null)
		{
			rb.linearVelocity = Vector2.zero;
		}
		
		// 사망 애니메이션 재생
		if (animator != null)
		{
			animator.SetTrigger("Death");
		}
		
		// ⭐ 사망 이펙트 생성
		SpawnDeathEffect();
		
		// 플레이어 사망 이벤트 발생 (GameOverUI가 구독 중)
		OnPlayerDied?.Invoke();
	}
	
	/// <summary>
	/// 플레이어 사망 이펙트 생성
	/// </summary>
	private void SpawnDeathEffect()
	{
		if (playerData == null) return;
		
		Vector3 deathPosition = transform.position;
		
		// 1. 사망 이펙트 프리팹 생성
		if (playerData.deathEffectPrefab != null)
		{
			GameObject effectObj = Instantiate(playerData.deathEffectPrefab, deathPosition, Quaternion.identity);
			
			// 지속 시간 후 자동 삭제
			Destroy(effectObj, playerData.deathEffectDuration);
			
			Debug.Log($"[PlayerController] 사망 이펙트 생성: {playerData.deathEffectPrefab.name}");
		}
		
		// 2. 사망 사운드 재생
		if (playerData.deathSound != null)
		{
			AudioSource audioSource = GetComponent<AudioSource>();
			if (audioSource == null)
			{
				audioSource = gameObject.AddComponent<AudioSource>();
				audioSource.playOnAwake = false;
				audioSource.spatialBlend = 0f; // 2D 사운드
			}
			audioSource.PlayOneShot(playerData.deathSound);
			
			Debug.Log($"[PlayerController] 사망 사운드 재생: {playerData.deathSound.name}");
		}
		
		// 3. 카메라 쉐이크
		if (playerData.deathCameraShakeStrength > 0f && hitShake != null && camShake != null)
		{
			// 사망 시 강한 쉐이크 (데미지 배율 적용)
			hitShake.ApplyShake(camShake, Mathf.RoundToInt(playerData.deathCameraShakeStrength * 10));
			
			Debug.Log($"[PlayerController] 사망 카메라 쉐이크: 강도 {playerData.deathCameraShakeStrength}");
		}
	}
	
	/// <summary>
	/// 플레이어 부활
	/// 게임 오버 UI의 "계속하기" 버튼에서 호출합니다.
	/// </summary>
	public void Revive()
	{
#if UNITY_EDITOR
		Debug.Log("[PlayerController] 플레이어 부활");
#endif
		
		// 체력 완전 회복 (최대로 설정)
		currentHealth = MaxHealth;
		
		// 정신력 완전 회복 (최대로 설정)
		currentMentalPower = maxMentalPower;
		
		// 생존 상태로 변경
		isAlive = true;
		
		// 입력 활성화
		if (inputActions != null)
		{
			inputActions.Enable();
		}
		
		// 애니메이터 리셋
		if (animator != null)
		{
			animator.SetTrigger("Revive");
		}
		
		// 부활 이벤트 발생
		OnPlayerRevived?.Invoke();
		
		// UI 업데이트 (최대 체력/정신력으로 업데이트)
		PlayerStatusEvents.InvokeHealthChanged(currentHealth, MaxHealth);
		PlayerStatusEvents.InvokeMentalPowerChanged(currentMentalPower, maxMentalPower);
		
		Debug.Log($"[PlayerController] 부활 완료 - 체력: {currentHealth}/{MaxHealth}, 정신력: {currentMentalPower}/{maxMentalPower}");
	}

	#endregion

	#region 저장/로드 (EasySave3) - 나중에 구현

	/// <summary>
	/// 플레이어 데이터 저장
	/// EasySave3 사용 예정
	/// </summary>
	public void SavePlayerData()
	{
		// TODO: EasySave3 구현
		// ES3.Save("PlayerHealth", currentHealth);
		// ES3.Save("PlayerMentalPower", currentMentalPower);
		// ES3.Save("PlayerCourageScore", courageScore);
	}

	/// <summary>
	/// 플레이어 데이터 로드
	/// EasySave3 사용 예정
	/// </summary>
	public void LoadPlayerData()
	{
		// TODO: EasySave3 구현
		// currentHealth = ES3.Load("PlayerHealth", maxHealth);
		// currentMentalPower = ES3.Load("PlayerMentalPower", maxMentalPower);
		// courageScore = ES3.Load("PlayerCourageScore", playerData.initialCourageScore);
		// UpdateCourageState();
	}

	#endregion
	
	#region 애니메이션 제어
	
	/// <summary>
	/// 공격 애니메이션 재생 (스킬 핸들러에서 호출)
	/// </summary>
	/// <param name="attackIndex">공격 애니메이션 인덱스 (1~4)</param>
	public void PlayAttackAnimation(int attackIndex)
	{
		if (animator == null) return;
		if (attackIndex < 1 || attackIndex > 4)
		{
			Debug.LogWarning($"[PlayerController] 잘못된 공격 애니메이션 인덱스: {attackIndex} (1~4만 유효)");
			return;
		}
		
		animator.SetTrigger($"Attack{attackIndex}");
		isPlayingAttackAnimation = true;
		
		Debug.Log($"[PlayerController] Attack{attackIndex} 애니메이션 재생");
	}
	
	/// <summary>
	/// 공격 애니메이션 종료 (애니메이션 이벤트에서 호출)
	/// </summary>
	public void OnAttackAnimationEnd()
	{
		isPlayingAttackAnimation = false;
		Debug.Log("[PlayerController] 공격 애니메이션 종료");
	}
	
	/// <summary>
	/// 현재 공격 애니메이션 재생 중인지 확인
	/// </summary>
	public bool IsPlayingAttackAnimation()
	{
		return isPlayingAttackAnimation;
	}
	
	#endregion

	#region 디버그 및 유틸리티

	/// <summary>
	/// 플레이어 정보를 문자열로 반환 (디버깅용)
	/// </summary>
	public override string GetDebugInfo()
	{
		string baseInfo = base.GetDebugInfo();
		return $"{baseInfo}\n정신력: {currentMentalPower}/{maxMentalPower}, 용기: {courageScore} ({currentCourageState}), 랭크: {currentSkillRank}";
	}
	
	/// <summary>
	/// 스킬 데미지 계산 (공격력 기반 퍼센테이지)
	/// </summary>
	/// <param name="damagePercent">데미지 퍼센테이지 (100 = 공격력 100%)</param>
	/// <returns>최종 계산된 데미지</returns>
	public float CalculateSkillDamage(float damagePercent)
	{
		if (playerData == null) return 0f;
		
		// 공격력 * (퍼센테이지 / 100)
		float finalDamage = playerData.attackPower * (damagePercent / 100f);
		
		return finalDamage;
	}
	
	/// <summary>
	/// 공격력 반환 (외부 접근용)
	/// </summary>
	public float GetAttackPower()
	{
		return playerData != null ? playerData.attackPower : 0f;
	}

	/// <summary>
	/// 테스트용 용기 점수 증가 (임시)
	/// </summary>
	[ContextMenu("테스트: 용기 +50")]
	private void DebugIncreaseCourage()
	{
		ChangeCourageScore(50);
	}

	/// <summary>
	/// 테스트용 용기 점수 감소 (임시)
	/// </summary>
	[ContextMenu("테스트: 용기 -50")]
	private void DebugDecreaseCourage()
	{
		ChangeCourageScore(-50);
	}

	/// <summary>
	/// 테스트용 정신력 회복 (임시)
	/// </summary>
	[ContextMenu("테스트: 정신력 풀 회복")]
	private void DebugRestoreMentalPower()
	{
		RestoreMentalPower(maxMentalPower);
	}

	#endregion
}

