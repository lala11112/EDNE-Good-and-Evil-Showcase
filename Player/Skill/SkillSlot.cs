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
    private PlayerController playerController; // 플레이어 컨트롤러 참조
    private SkillController skillController; // 스킬 컨트롤러 참조 (배율 관리용)

    private void Awake()
    {
        // owner가 설정되지 않았으면 자동으로 찾기
        if (owner == null)
        {
            // 부모에서 PlayerController 찾기
            playerController = GetComponentInParent<PlayerController>();
            if (playerController != null)
            {
                owner = playerController.gameObject;
            }
            else
            {
                Debug.LogWarning("[SkillSlot] PlayerController를 찾을 수 없습니다. owner를 수동으로 설정해주세요.");
            }
        }
        else
        {
            // owner가 설정되어 있으면 PlayerController 찾기
            playerController = owner.GetComponent<PlayerController>();
            if (playerController == null)
            {
                playerController = owner.GetComponentInParent<PlayerController>();
            }
        }
        
        // 싱글톤으로도 찾기 시도 (최후의 수단)
        if (playerController == null)
        {
            playerController = PlayerController.Instance;
        }
        
        // SkillController 찾기 (부모에서)
        skillController = GetComponentInParent<SkillController>();
        if (skillController == null && owner != null)
        {
            skillController = owner.GetComponentInParent<SkillController>();
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
        
        // 기존 핸들러 정리 (스킬 변경 시에만)
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

        // 새 핸들러 생성 및 Setup (스킬 변경 시에만)
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
        
        // SkillController에 가중치 업데이트 알림
        if (skillController != null && currentSkillData != null)
        {
            skillController.UpdateSkillWeight(slotType, currentSkillData);
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

        // FaithDashHandler 특수 처리
        FaithDashHandler faithDashHandler = handler as FaithDashHandler;
        bool isFaithDash = (faithDashHandler != null);
        bool isFaithDashRecharging = (isFaithDash && faithDashHandler.IsInRechargeMode);
        
        // RainOfFaithHandler 특수 처리 (자체적으로 쿨타임 관리)
        RainOfFaithHandler rainOfFaithHandler = handler as RainOfFaithHandler;
        bool isRainOfFaith = (rainOfFaithHandler != null);
        
        // 쿨타임 체크 (FaithDash 재사용 모드일 때는 쿨타임 무시)
        if (isOnCooldown && !isFaithDashRecharging)
        {
            SkillUIEvents.InvokeSkillUseFailed(slotType, $"쿨타임 ({currentCooldown:F1}초)");
            Debug.Log($"{currentSkillData.skillName} 스킬이 쿨타임 중입니다! ({currentCooldown:F1}초 남음)");
            return;
        }

        // ⭐ 정신력 소모 체크 (SkillSlot에서 통합 처리)
        // 재사용 모드가 아닐 때만 정신력 소모 체크 (FaithDash 재사용 모드에서는 정신력 소모 안 함)
        if (!isFaithDashRecharging && currentSkillData.mentalCost > 0)
        {
            // 기본 정신력 소모량 계산
            int baseMentalCost = currentSkillData.mentalCost;
            bool isRankBased = false;
            
            // CourageRushHandler처럼 랭크별 정신력 계산이 필요한 경우 처리
            CourageRushHandler courageRushHandler = handler as CourageRushHandler;
            if (courageRushHandler != null)
            {
                // CourageRushHandler의 CalculateMentalCost 메서드 사용 (랭크별 계산)
                baseMentalCost = courageRushHandler.CalculateMentalCost(rank);
                isRankBased = true;
            }
            
            // SkillController로부터 현재 배율 가져오기
            float multiplier = 1.0f;
            if (skillController != null)
            {
                multiplier = skillController.GetMentalCostMultiplier(slotType);
            }
            
            // 실제 정신력 소모량 = 기본 소모량 × 배율 (소수점 반올림)
            int actualMentalCost = Mathf.RoundToInt(baseMentalCost * multiplier);
            
            // ⭐ 디버그: 정신력 소모 전 상태 로그
            int mentalPowerBefore = playerController != null ? playerController.CurrentMentalPower : 0;
            
            Debug.Log($"[SkillSlot] {currentSkillData.skillName} 스킬 정신력 소모 계산:\n" +
                $"  - 기본 소모량: {currentSkillData.mentalCost} (랭크별 계산: {isRankBased}, 계산된 값: {baseMentalCost})\n" +
                $"  - 현재 배율: {multiplier:F3}\n" +
                $"  - 실제 소모량: {baseMentalCost} × {multiplier:F3} = {actualMentalCost}\n" +
                $"  - 소모 전 정신력: {mentalPowerBefore}/{playerController?.MaxMentalPower ?? 0}");
            
            if (playerController != null && !playerController.TryConsumeMentalPower(actualMentalCost))
            {
                SkillUIEvents.InvokeSkillUseFailed(slotType, $"정신력 부족 (필요: {actualMentalCost})");
                Debug.LogWarning($"[SkillSlot] {currentSkillData.skillName} 스킬 사용 실패 - 정신력이 부족합니다! (필요: {actualMentalCost}, 현재: {playerController.CurrentMentalPower}, 배율: {multiplier:F3})");
                return;
            }
            
            // ⭐ 디버그: 정신력 소모 후 상태 로그
            int mentalPowerAfter = playerController != null ? playerController.CurrentMentalPower : 0;
            int actualConsumed = mentalPowerBefore - mentalPowerAfter;
            
            Debug.Log($"[SkillSlot] {currentSkillData.skillName} 스킬 정신력 소모 완료:\n" +
                $"  - 소모량: {actualConsumed} (계산된 값: {actualMentalCost})\n" +
                $"  - 소모 전: {mentalPowerBefore} → 소모 후: {mentalPowerAfter} (차이: -{actualConsumed})\n" +
                $"  - 남은 정신력: {mentalPowerAfter}/{playerController?.MaxMentalPower ?? 0}");
            
            // ⭐ 정신력 소모 후 SkillController에 알림 (다른 스킬들의 배율 감소 처리)
            // 정신력 소모가 있으면 항상 OnSkillUsed 호출 (배율 리셋 및 다른 스킬 감소)
            if (skillController != null)
            {
                float reductionAmount = currentSkillData.otherSkillMentalCostReduction;
                skillController.OnSkillUsed(slotType, reductionAmount);
            }
        }

        // 스킬 사용 이벤트
        SkillUIEvents.InvokeSkillUsed(slotType, currentSkillData);
        
        handler.Use(rank);
        
        // ⭐ 쿨타임 시작
        // - FaithDash: 재사용 모드 종료 시 시작 (자체 관리)
        // - RainOfFaith: 설치/폭발 단계별로 자체 관리
        if (!isFaithDash && !isRainOfFaith)
        {
            StartCooldown();
        }
        else if (isFaithDash)
        {
            Debug.Log($"[SkillSlot] {currentSkillData.skillName} - FaithDash 스킬이므로 쿨타임 자체 관리");
        }
        else if (isRainOfFaith)
        {
            Debug.Log($"[SkillSlot] {currentSkillData.skillName} - RainOfFaith 스킬이므로 쿨타임 자체 관리");
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
    
    /// <summary>
    /// 외부(핸들러)에서 커스텀 쿨타임 시작 요청
    /// 특정 시간을 지정하여 쿨타임 시작
    /// </summary>
    public void StartCooldownFromHandler(float customCooldownTime)
    {
        isOnCooldown = true;
        currentCooldown = customCooldownTime;
        totalCooldown = customCooldownTime;
        
        // 쿨타임 시작 이벤트
        SkillUIEvents.InvokeCooldownStarted(slotType, totalCooldown);
        
        Debug.Log($"{currentSkillData?.skillName ?? "Unknown"} 커스텀 쿨타임 시작: {currentCooldown}초");
    }
    
    /// <summary>
    /// 쿨타임 강제 종료 (즉시 재사용 가능하게 만듦)
    /// 용기의 질주처럼 적 충돌 시 즉시 재사용 가능하게 만들 때 사용
    /// </summary>
    public void ResetCooldown()
    {
        if (isOnCooldown)
        {
            currentCooldown = 0f;
            isOnCooldown = false;
            
            // ⭐ 즉시 재사용 가능 이벤트 발생 (특별한 색상 표시용)
            SkillUIEvents.InvokeCooldownInstantReset(slotType);
            
            Debug.Log($"[SkillSlot] {currentSkillData?.skillName ?? "Unknown"} 쿨타임 강제 초기화! 즉시 재사용 가능!");
        }
    }
}
