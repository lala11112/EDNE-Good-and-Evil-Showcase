using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 몬스터 컨트롤러 - EntityController 상속
/// Partial Class로 기능별 파일 분리
/// - MonsterController.cs: 메인 (초기화, 라이프사이클)
/// - MonsterController.Movement.cs: NavMeshPlus 2D 이동
/// - MonsterController.Skill.cs: 스킬 시스템
/// - MonsterController.Debug.cs: 디버그 시각화
/// 
/// ⚠️ 2D 프로젝트 전용 - NavMeshPlus 사용 (2D NavMesh)
/// </summary>
public partial class MonsterController : EntityController
{
    #region Inspector Fields - 데이터 및 설정
    [Header("몬스터 데이터")]
    [SerializeField] protected MonsterDataSO monsterData;
    
    [Header("타겟 설정")]
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindPlayer = true;
    #endregion
    
    #region Component References - 컴포넌트 참조
    private NavMeshAgent navAgent;  // NavMeshPlus 2D 경로 찾기
    private Animator animator;      // 애니메이션 컨트롤러
    private SpriteRenderer spriteRenderer;  // 스프라이트 렌더러 (좌우 반전용)
    private RoomController roomController;  // 이 몬스터가 속한 방 컨트롤러
    #endregion
    
    #region Runtime Variables - 런타임 상태
    private bool isMovementEnabled = true;
    private float movementLockTimer = 0f; // 이동 잠금 타이머
    
    // ⭐ SO 값을 복사한 런타임 변수들 (SO 직접 수정 방지)
    private float runtimeDetectionRange;
    private float runtimeStopDistance;
    private float runtimeMoveSpeed;
    #endregion
    
    #region Properties - 외부 접근용
    /// <summary>
    /// 현재 타겟 Transform
    /// </summary>
    public Transform Target => target;
    
    /// <summary>
    /// 몬스터 데이터 SO
    /// </summary>
    public MonsterDataSO Data => monsterData;
    
    /// <summary>
    /// NavMeshAgent 컴포넌트 (NavMeshPlus 2D)
    /// </summary>
    public NavMeshAgent NavAgent => navAgent;
    
    /// <summary>
    /// Animator 컴포넌트
    /// </summary>
    public Animator Animator => animator;
    
    /// <summary>
    /// SpriteRenderer 컴포넌트 (스프라이트 좌우 반전)
    /// </summary>
    public SpriteRenderer SpriteRenderer => spriteRenderer;
    
    /// <summary>
    /// 이동이 잠겨있는지 여부 (스킬 사용 중 등)
    /// </summary>
    public bool IsMovementLocked => movementLockTimer > 0f;
    
    /// <summary>
    /// 이 몬스터가 속한 RoomController
    /// </summary>
    public RoomController RoomController => roomController;
    #endregion
    
    #region Unity Lifecycle - 생명주기
    
    protected override void Awake()
    {
        base.Awake(); // EntityController 초기화
        SetupComponents();
        InitializeMonster(); // ⭐ Awake로 이동 (OnEnable보다 먼저 실행되어야 함)
    }
    
    private void Start()
    {
        // ⭐ 플레이어 찾기 (모든 Awake 후에 실행되므로 안전)
        FindPlayer();
        
        // 스킬 초기화
        InitializeSkills(); // Skill 파일의 메서드
    }
    
    protected override void Update()
    {
        // ⭐ 부모의 상태효과 업데이트 호출
        base.Update();
        
        if (!isAlive) return;
        
        // 이동 잠금 타이머 감소
        if (movementLockTimer > 0f)
        {
            movementLockTimer -= Time.deltaTime;
            if (movementLockTimer <= 0f)
            {
                movementLockTimer = 0f;
                Debug.Log($"[MonsterController] {gameObject.name} 이동 잠금 해제");
            }
        }
        
        // 메인 업데이트 루프
        ProcessMovement();  // Movement 파일의 메서드
        ProcessSkills();    // Skill 파일의 메서드
    }
    
    private void OnDestroy()
    {
        CleanupSkills(); // Skill 파일의 메서드
    }
    
    #endregion
    
    #region Initialization - 초기화
    
    /// <summary>
    /// 필요한 컴포넌트 확인 및 추가
    /// </summary>
    private void SetupComponents()
    {
        // NavMeshAgent 확인/추가
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent == null)
        {
            navAgent = gameObject.AddComponent<NavMeshAgent>();
            Debug.Log($"[MonsterController] {gameObject.name}에 NavMeshAgent 추가됨");
        }
        
        // NavMeshPlus 2D 설정
        navAgent.updateRotation = false;  // 2D에서는 회전 수동 관리
        navAgent.updateUpAxis = false;    // 2D에서는 Up 축 업데이트 안 함
    
        // 2D: Z축 고정 강제 (NavMesh가 XY 평면에 있어야 함)
        // NavMeshAgent가 높이(elevation)를 무시하도록 설정
        navAgent.baseOffset = 0f;  // 높이 오프셋 0
        
        // 2D: Transform Z축 강제 고정
        Vector3 pos = transform.position;
        pos.z = 0f;
        transform.position = pos;
        
        // ⭐ Animator 컴포넌트 참조 (선택적)
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning($"[MonsterController] {gameObject.name}에 Animator가 없습니다. 애니메이션을 사용하려면 추가하세요.");
        }
        
        // ⭐ SpriteRenderer 컴포넌트 참조 (필수 - 스프라이트 반전용)
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[MonsterController] {gameObject.name}에 SpriteRenderer가 없습니다. 스프라이트 반전이 작동하지 않습니다.");
        }
    }
    
    /// <summary>
    /// 몬스터 데이터로 초기화
    /// </summary>
    private void InitializeMonster()
    {
        if (monsterData != null)
        {
            // EntityController에 데이터 설정
            entityData = monsterData;
            
            // EntityController 초기화 (체력, 타입 등)
            Initialize();
            
            // ⭐ SO 값을 런타임 변수로 복사 (SO 직접 수정 방지)
            runtimeDetectionRange = monsterData.detectionRange;
            runtimeStopDistance = monsterData.stopDistance;
            runtimeMoveSpeed = monsterData.moveSpeed;
            
            // NavMeshAgent 설정 (런타임 변수 사용)
            navAgent.speed = runtimeMoveSpeed;
            navAgent.stoppingDistance = runtimeStopDistance;
            
            Debug.Log($"[MonsterController] {gameObject.name} 초기화 완료 " +
                      $"(HP: {currentHealth}/{MaxHealth}, Speed: {navAgent.speed}, DetectRange: {runtimeDetectionRange})");
        }
        else
        {
            Debug.LogError($"[MonsterController] {gameObject.name}에 MonsterDataSO가 설정되지 않았습니다!");
        }
    }
    
    /// <summary>
    /// 플레이어 타겟 설정 (Start에서 호출)
    ///  Awake가 아닌 Start에서 호출해야 PlayerController.Instance가 보장됨
    /// </summary>
    private void FindPlayer()
    {
        // ⭐ 자동으로 플레이어 찾기 (싱글톤 사용)
        if (autoFindPlayer && target == null)
        {
            if (PlayerController.Instance != null)
            {
                target = PlayerController.Instance.transform;
                Debug.Log($"[MonsterController] {gameObject.name}가 플레이어를 타겟으로 설정");
            }
            else
            {
                Debug.LogWarning($"[MonsterController] {gameObject.name}가 PlayerController.Instance를 찾지 못했습니다!");
            }
        }
    }
    
    #endregion
    
    #region EntityController Overrides - 부모 오버라이드
    
    /// <summary>
    /// 데미지를 받았을 때 (EntityController 오버라이드)
    /// </summary>
    protected override void OnDamaged(int damageAmount, object damageSource)
    {
        base.OnDamaged(damageAmount, damageSource);
        
        // 몬스터 피격 반응
        // TODO: 나중에 구현
        // PlayHitAnimation();
        // PlayHitSound();
        // ShowDamagePopup(damageAmount);
        
        Debug.Log($"[MonsterController] {gameObject.name}가 {damageAmount} 데미지를 받음 (남은 HP: {currentHealth}/{MaxHealth})");
    }
    
    /// <summary>
    /// 죽음 처리 (EntityController 오버라이드)
    /// </summary>
    protected override void Die()
    {
        Debug.Log("다이");
        base.Die();
        
        // NavMesh 이동 정지
        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.enabled = false;
        }
        
        // ⭐ 죽음 이벤트 발생 (EnemyDeathEventSystem이 있다면 자동으로 감지됨)
        Debug.Log($"[MonsterController] {gameObject.name}의 죽음이 EnemyDeathEventSystem에 의해 감지됩니다.");
        
        // 보상 처리 (나중에 구현)
        if (monsterData != null)
        {
            Debug.Log($"[MonsterController] {gameObject.name} 보상: 경험치 {monsterData.experienceReward}, 골드 {monsterData.goldReward}");
            // TODO: 실제 보상 지급 로직 구현
            // GameManager.Instance.AddExperience(monsterData.experienceReward);
            // GameManager.Instance.AddGold(monsterData.goldReward);
            // DropItem(monsterData.dropChance);
        }
        
        Debug.Log($"[MonsterController] {gameObject.name} 사망");
        
        // 죽음 이벤트 발생
        Debug.Log($"[MonsterController] {gameObject.name} 죽음 이벤트 발생 - 구독자 수: {(OnMonsterDied?.GetInvocationList()?.Length ?? 0)}");
        OnMonsterDied?.Invoke(this);
        
        // TODO: 죽음 애니메이션, 이펙트 등
        // PlayDeathAnimation();
        // PlayDeathSound();
        
        // TODO: 페이드아웃 효과 (나중에 구현)
        // if (monsterData.useFadeOutOnDeath)
        // {
        //     StartCoroutine(FadeOutAndDestroy(monsterData.fadeOutDuration));
        // }
        
        // ⭐ 몬스터 오브젝트 파괴 (SO 데이터 기반)
        float destroyDelay = monsterData != null ? monsterData.deathDestroyDelay : 2f;
        Destroy(gameObject, destroyDelay);
    }

    // 이벤트
    public System.Action<MonsterController> OnMonsterDied;
    
    #endregion
    
    #region Room Controller - 방 관리
    
    /// <summary>
    /// RoomController 설정 (MonsterSpawner에서 호출)
    /// </summary>
    public void SetRoomController(RoomController controller)
    {
        roomController = controller;
        Debug.Log($"[MonsterController] {gameObject.name}에 RoomController 설정: {controller?.name ?? "null"}");
    }
    
    /// <summary>
    /// RoomController 가져오기 (분열 몬스터 등록용)
    /// </summary>
    public RoomController GetRoomController()
    {
        return roomController;
    }
    
    #endregion
}

