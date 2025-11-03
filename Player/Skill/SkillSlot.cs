using UnityEngine;


public class SkillSlot : MonoBehaviour
{
    [Header("슬롯 타입")]
    [Tooltip("이 슬롯의 타입 (Z, X, C, V)")]
    [SerializeField] private SkillSlotType slotType;
    
    /// <summary>
    /// 슬롯 타입 설정 (SkillController에서 자동 설정)
    /// </summary>
    public void SetSlotType(SkillSlotType type)
    {
        slotType = type;
    }
    
    [Header("현재 스킬")]
    [SerializeField] private PlayerSkillData currentSkillData;  // 현재 장착된 스킬
    
    [Header("상태")]
    [SerializeField] private bool isOnCooldown;
    [SerializeField] private float currentCooldown;
    [SerializeField] private float totalCooldown; // 전체 쿨타임 (UI 퍼센트 계산용)
    
    // 현재 스킬 데이터 접근용 프로퍼티
    public PlayerSkillData CurrentSkillData => currentSkillData;
    public float CurrentCooldown => currentCooldown;
    public float TotalCooldown => totalCooldown;
    public bool IsOnCooldown => isOnCooldown;
    public SkillSlotType SlotType => slotType;

    private SkillHandlerInterface handler;        // 캐시
    private GameObject handlerGO;         // 인스턴스
    [SerializeField] private GameObject owner; // 플레이어 오브젝트 참조(없으면 transform.root 등)

    private void Awake()
    {
        // owner가 설정되지 않았으면 자동으로 찾기
        if (owner == null)
        {
            // 부모에서 PlayerController 찾기
            PlayerController playerController = GetComponentInParent<PlayerController>();
            if (playerController != null)
            {
                owner = playerController.gameObject;
            }
            else
            {
                Debug.LogWarning("[SkillSlot] PlayerController를 찾을 수 없습니다. owner를 수동으로 설정해주세요.");
            }
        }
    }

    private void Update()
    {
        // 쿨타임 관리
        if (isOnCooldown && currentSkillData != null)
        {
            float previousCooldown = currentCooldown;
            currentCooldown -= Time.deltaTime;
            
            // 쿨타임 업데이트 이벤트 (매 프레임)
            SkillUIEvents.InvokeCooldownUpdated(slotType, currentCooldown, totalCooldown);
            
            if (currentCooldown <= 0)
            {
                isOnCooldown = false;
                currentCooldown = 0f;
                
                // 쿨타임 종료 이벤트
                SkillUIEvents.InvokeCooldownEnded(slotType);
            }
        }
    }
    
    // 스킬 데이터 설정 (변경 시에만 호출)
    public void SetSkillData(PlayerSkillData newSkillData)
    {
        if (newSkillData == null)
        {
            Debug.LogWarning("스킬 데이터가 null입니다!");
            return;
        }
        
        // 스킬이 바뀌지 않았으면 무시
        if (currentSkillData == newSkillData && handler != null)
        {
            Debug.Log($"{currentSkillData.skillName} 스킬이 이미 장착되어 있습니다.");
            return;
        }
        
        // ⭐ 기존 핸들러 정리 (스킬 변경 시에만)
        if (handler != null)
        {
            handler.Cleanup(); // 상태 초기화
            Destroy(handlerGO); // 오브젝트 파괴
            handler = null;
            handlerGO = null;
        }
        
        currentSkillData = newSkillData;
        
        // 스킬 변경 시 쿨타임 초기화
        isOnCooldown = false;
        currentCooldown = 0f;
        totalCooldown = newSkillData != null ? newSkillData.cooldown : 0f;

        // ⭐ 새 핸들러 생성 및 Setup (스킬 변경 시에만)
        if (currentSkillData != null && currentSkillData.handlerPrefab != null)
        {
            handlerGO = Instantiate(currentSkillData.handlerPrefab, transform);
            handler = handlerGO.GetComponent<SkillHandlerInterface>();
            if (handler == null)
            {
                Debug.LogError("핸들러 프리팹에 SkillHandlerInterface가 없습니다.");
                return;
            }
            var ownerGO = owner != null ? owner : gameObject;
            handler.Setup(currentSkillData, ownerGO);
        }
        
        // 스킬 슬롯 변경 이벤트 발생
        SkillUIEvents.InvokeSkillSlotChanged(slotType, currentSkillData);
        
        Debug.Log($"{currentSkillData.skillName} 스킬이 슬롯에 장착되었습니다!");
    }
    
    // 스킬 실행 (저장된 데이터 사용)
    public void ExecuteSkill(SkillRank rank = SkillRank.D)
    {
        if (currentSkillData == null)
        {
            SkillUIEvents.InvokeSkillUseFailed(slotType, "스킬 없음");
            Debug.LogWarning("장착된 스킬이 없습니다!");
            return;
        }
        
        if (handler == null)
        {
            SkillUIEvents.InvokeSkillUseFailed(slotType, "핸들러 없음");
            Debug.LogWarning("핸들러가 없습니다.");
            return;
        }

        // ⭐ FaithDashHandler 특수 처리
        FaithDashHandler faithDashHandler = handler as FaithDashHandler;
        bool isFaithDash = (faithDashHandler != null);
        bool isFaithDashRecharging = (isFaithDash && faithDashHandler.IsInRechargeMode);
        
        Debug.Log($"[SkillSlot] ExecuteSkill - isFaithDash: {isFaithDash}, isRecharging: {isFaithDashRecharging}, isOnCooldown: {isOnCooldown}");
        
        // 쿨타임 체크 (FaithDash 재사용 모드일 때는 쿨타임 무시)
        if (isOnCooldown && !isFaithDashRecharging)
        {
            SkillUIEvents.InvokeSkillUseFailed(slotType, $"쿨타임 ({currentCooldown:F1}초)");
            Debug.Log($"{currentSkillData.skillName} 스킬이 쿨타임 중입니다! ({currentCooldown:F1}초 남음)");
            return;
        }

        // 스킬 사용 이벤트
        SkillUIEvents.InvokeSkillUsed(slotType, currentSkillData);
        
        handler.Use(rank);
        
        // ⭐ 쿨타임 시작 - FaithDash는 절대 여기서 쿨타임 시작 안 함 (재사용 모드 종료 시 시작)
        if (!isFaithDash)
        {
            StartCooldown();
        }
        else
        {
            Debug.Log($"[SkillSlot] {currentSkillData.skillName} - FaithDash 스킬이므로 쿨타임 시작 안 함");
        }
    }
    
    private void StartCooldown()
    {
        isOnCooldown = true;
        currentCooldown = currentSkillData.cooldown;
        totalCooldown = currentSkillData.cooldown;
        
        // 쿨타임 시작 이벤트
        SkillUIEvents.InvokeCooldownStarted(slotType, totalCooldown);
        
        Debug.Log($"{currentSkillData.skillName} 쿨타임 시작: {currentCooldown}초");
    }
    
    /// <summary>
    /// 외부(핸들러)에서 쿨타임 시작 요청
    /// 신념질주 같은 재사용 스킬이 모드 종료 시 호출
    /// </summary>
    public void StartCooldownFromHandler()
    {
        StartCooldown();
    }
}
