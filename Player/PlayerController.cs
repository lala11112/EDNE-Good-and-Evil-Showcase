using System;
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
		
		// ⭐ 싱글톤 정리
		if (Instance == this)
		{
			Instance = null;
		}
	}

	protected override void Update()
	{
		base.Update(); // EntityController의 상태효과 업데이트

		// 정신력 자동 회복
		UpdateMentalPowerRegeneration(Time.deltaTime);

		// 이동 입력 처리
		UpdateMovementInput();

		// 애니메이터 파라미터 업데이트
		UpdateAnimatorParameters();


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
		
		Debug.Log("[PlayerController] Rigidbody2D가 Dynamic 모드로 설정되었습니다. (즉시 반응 + 물리 충돌)");
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
		float regenAmount = playerData.mentalPowerRegenRate * deltaTime;
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

		courageScore += amount;

		// 용기 단계 재계산
		UpdateCourageState();

		// 단계 변경 이벤트
		if (previousState != currentCourageState)
		{
			OnCourageStateChanged(previousState, currentCourageState);
		}

		// 절망 상태 체크
		CheckDespairState();
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

		// 점수에 따라 단계 결정 (높은 단계부터 체크)
		if (courageScore >= playerData.confidenceStepRequirement)
			newState = CourageState.confidence;
		else if (courageScore >= playerData.determinationStepRequirement)
			newState = CourageState.determination;
		else if (courageScore >= playerData.courageStepRequirement)
			newState = CourageState.courage;
		else if (courageScore >= playerData.doubtStepRequirement)
			newState = CourageState.doubt;
		else if (courageScore >= playerData.anxietyStepRequirement)
			newState = CourageState.anxiety;
		else
			newState = CourageState.Despair;

		currentCourageState = newState;

		// 용기 단계에 따라 스킬 랭크 결정
		currentSkillRank = GetSkillRankFromCourageState(currentCourageState);
	}

	/// <summary>
	/// 용기 단계를 스킬 랭크로 변환
	/// </summary>
	private SkillRank GetSkillRankFromCourageState(CourageState state)
	{
		switch (state)
		{
			case CourageState.Despair:
				return SkillRank.D; // 절망 상태에서는 최하 랭크
			case CourageState.anxiety:
				return SkillRank.D;
			case CourageState.doubt:
				return SkillRank.C;
			case CourageState.courage:
				return SkillRank.B;
			case CourageState.determination:
				return SkillRank.A;
			case CourageState.confidence:
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
		Debug.Log($"[PlayerController] 용기 단계 변경: {previousState} → {newState} (스킬 랭크: {currentSkillRank})");
		
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

	/// <summary>
	private void UpdateAnimatorParameters()
	{
		if (animator == null) return;

		bool isMoving = moveInput.sqrMagnitude > 0.01f;
		Vector2 dir = isMoving ? moveInput.normalized : GetDirectionVector(lastMoveDirection);

		animator.SetBool("isMove", isMoving);
		animator.SetFloat("InputX", dir.x);
		animator.SetFloat("InputY", dir.y);
	}

	/// </summary>
	private void ProcessMovement()
	{
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
	/// 스킬 사용 가능 여부 체크 (오버라이드)
	/// EntityController의 기본 체크에 정신력 체크 추가
	/// </summary>
	public override bool CanUseSkill()
	{
		// 기본 체크 (생존, 상태효과)
		if (!base.CanUseSkill()) return false;

		// 정신력 체크는 개별 스킬에서 수행
		// 여기서는 기본 조건만 체크
		return true;
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

		Debug.Log("[PlayerController] 플레이어 사망 (체력 0)");

		// 플레이어 전용 사망 처리
		OnPlayerDeath();
	}

	/// <summary>
	/// 플레이어 사망 시 추가 처리
	/// </summary>
	protected virtual void OnPlayerDeath()
	{
		Debug.Log("[PlayerController] 플레이어 사망 이벤트 발생");
		
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
		
		// 플레이어 사망 이벤트 발생 (GameOverUI가 구독 중)
		OnPlayerDied?.Invoke();
	}
	
	/// <summary>
	/// 플레이어 부활
	/// 게임 오버 UI의 "계속하기" 버튼에서 호출합니다.
	/// </summary>
	public void Revive()
	{
		Debug.Log("[PlayerController] 플레이어 부활");
		
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

