using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SkillController : MonoBehaviour
{
    [Header("입력 액션")]
    [SerializeField] private InputActionAsset inputActions;
    
    [Header("스킬 슬롯")]
    [SerializeField] private SkillSlot zSlot;
    [SerializeField] private SkillSlot xSlot;
    [SerializeField] private SkillSlot cSlot;
    [SerializeField] private SkillSlot vSlot;
    
    [Header("스킬 로드아웃 (우선 적용)")]
    [SerializeField] private SkillLoadout loadout;

    [Header("기본 스킬 (로드아웃 없을 때 사용)")]
    [SerializeField] private PlayerSkillData defaultZSkill;  // 기본 Z 스킬
    [SerializeField] private PlayerSkillData defaultXSkill;  // 기본 X 스킬
    [SerializeField] private PlayerSkillData defaultCSkill;  // 기본 C 스킬
    [SerializeField] private PlayerSkillData defaultVSkill;  // 기본 V 스킬
    
    private InputAction skillZAction;
    private InputAction skillXAction;
    private InputAction skillCAction;
    private InputAction skillVAction;
    
    private PlayerController playerController;
    
    #region 정신력 소모 배율 시스템
    
    /// <summary>
    /// 각 슬롯별 정신력 소모 배율 (기본값 1.0)
    /// </summary>
    private Dictionary<SkillSlotType, float> mentalCostMultipliers = new Dictionary<SkillSlotType, float>();
    
    /// <summary>
    /// 각 슬롯별 정신력 감소 가중치 (SO 데이터에서 로드)
    /// </summary>
    private Dictionary<SkillSlotType, float> mentalCostWeights = new Dictionary<SkillSlotType, float>();
    
    /// <summary>
    /// 각 슬롯별 정신력 감소 최소치 (SO 데이터에서 로드)
    /// </summary>
    private Dictionary<SkillSlotType, float> mentalCostMinimums = new Dictionary<SkillSlotType, float>();
    
    #endregion
    
    private void Awake()
    {
        // Input Actions 초기화
        skillZAction = inputActions.FindAction("SkillZ");
        skillXAction = inputActions.FindAction("SkillX");
        skillCAction = inputActions.FindAction("SkillC");
        skillVAction = inputActions.FindAction("SkillV");
        
        // PlayerController 찾기 (자기 자신 또는 부모에서)
        playerController = GetComponent<PlayerController>();
        if (playerController == null)
        {
            playerController = GetComponentInParent<PlayerController>();
        }
        
        if (playerController == null)
        {
            playerController = PlayerController.Instance; // 싱글톤 사용
        }
    }
    
    private void Start()
    {
        EnsureSlot(ref zSlot, "Z Slot", SkillSlotType.Z);
        EnsureSlot(ref xSlot, "X Slot", SkillSlotType.X);
        EnsureSlot(ref cSlot, "C Slot", SkillSlotType.C);
        EnsureSlot(ref vSlot, "V Slot", SkillSlotType.V);

        // 정신력 소모 배율 시스템 초기화
        InitializeMentalCostSystem();

        // 로드아웃이 있으면 우선 적용, 없으면 기존 기본 스킬 사용
        if (loadout != null)
        {
            if (loadout.zSkill != null) zSlot.SetSkillData(loadout.zSkill);
            if (loadout.xSkill != null) xSlot.SetSkillData(loadout.xSkill);
            if (loadout.cSkill != null) cSlot.SetSkillData(loadout.cSkill);
            if (loadout.vSkill != null) vSlot.SetSkillData(loadout.vSkill);
        }
        else
        {
            if (defaultZSkill != null) zSlot.SetSkillData(defaultZSkill);
            if (defaultXSkill != null) xSlot.SetSkillData(defaultXSkill);
            if (defaultCSkill != null) cSlot.SetSkillData(defaultCSkill);
            if (defaultVSkill != null) vSlot.SetSkillData(defaultVSkill);
        }
    }
    
    /// <summary>
    /// SkillController의 모든 슬롯 가져오기 (배열 형태)
    /// </summary>
    public SkillSlot[] GetAllSlots()
    {
        return new SkillSlot[] { zSlot, xSlot, cSlot, vSlot };
    }
    
    private void OnEnable()
    {
        // 이벤트 등록
        skillZAction.performed += OnSkillZPerformed;
        skillXAction.performed += OnSkillXPerformed;
        skillCAction.performed += OnSkillCPerformed;
        skillVAction.performed += OnSkillVPerformed;
        
        // Input Actions 활성화
        inputActions.Enable();
    }
    
    private void OnDisable()
    {
        // 이벤트 해제
        skillZAction.performed -= OnSkillZPerformed;
        skillXAction.performed -= OnSkillXPerformed;
        skillCAction.performed -= OnSkillCPerformed;
        skillVAction.performed -= OnSkillVPerformed;
        
        // Input Actions 비활성화
        inputActions.Disable();
    }
    
    // 스킬 사용 이벤트들 (현재 스킬 랭크 전달)
    // 다이얼로그 중에는 스킬 발동을 차단합니다
    private void OnSkillZPerformed(InputAction.CallbackContext context)
    {
        // DialogueInputHandler.IsDialogueActive가 true이면 입력을 무시 (다이얼로그 중)
        if (DialogueInputHandler.IsDialogueActive)
        {
            return;
        }
        
        SkillRank rank = playerController != null ? playerController.currentSkillRank : SkillRank.D;
        zSlot?.ExecuteSkill(rank);
    }
    
    private void OnSkillXPerformed(InputAction.CallbackContext context)
    {
        // DialogueInputHandler.IsDialogueActive가 true이면 입력을 무시 (다이얼로그 중)
        if (DialogueInputHandler.IsDialogueActive)
        {
            return;
        }
        
        SkillRank rank = playerController != null ? playerController.currentSkillRank : SkillRank.D;
        xSlot?.ExecuteSkill(rank);
    }
    
    private void OnSkillCPerformed(InputAction.CallbackContext context)
    {
        // DialogueInputHandler.IsDialogueActive가 true이면 입력을 무시 (다이얼로그 중)
        if (DialogueInputHandler.IsDialogueActive)
        {
            return;
        }
        
        SkillRank rank = playerController != null ? playerController.currentSkillRank : SkillRank.D;
        cSlot?.ExecuteSkill(rank);
    }
    
    private void OnSkillVPerformed(InputAction.CallbackContext context)
    {
        // DialogueInputHandler.IsDialogueActive가 true이면 입력을 무시 (다이얼로그 중)
        if (DialogueInputHandler.IsDialogueActive)
        {
            return;
        }
        
        SkillRank rank = playerController != null ? playerController.currentSkillRank : SkillRank.D;
        vSlot?.ExecuteSkill(rank);
    }
    
    // 슬롯 보장: 없으면 생성해서 붙임
    private void EnsureSlot(ref SkillSlot slot, string name, SkillSlotType slotType)
    {
        if (slot != null)
        {
            // 이미 있는 슬롯의 타입 설정
            slot.SetSlotType(slotType);
            return;
        }
        
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        slot = go.AddComponent<SkillSlot>();
        
        // 생성한 슬롯의 타입 설정
        slot.SetSlotType(slotType);
    }
    
    /// <summary>
    /// 스킬 슬롯 가져오기 (UI 접근용)
    /// </summary>
    public SkillSlot GetSkillSlot(SkillSlotType slotType)
    {
        switch (slotType)
        {
            case SkillSlotType.Z: return zSlot;
            case SkillSlotType.X: return xSlot;
            case SkillSlotType.C: return cSlot;
            case SkillSlotType.V: return vSlot;
            default: return null;
        }
    }

    // 런타임 스킬 변경 API
    public void ChangeZSkill(PlayerSkillData newSkill) => zSlot?.SetSkillData(newSkill);
    public void ChangeXSkill(PlayerSkillData newSkill) => xSlot?.SetSkillData(newSkill);
    public void ChangeCSkill(PlayerSkillData newSkill) => cSlot?.SetSkillData(newSkill);
    public void ChangeVSkill(PlayerSkillData newSkill) => vSlot?.SetSkillData(newSkill);
    
    #region 정신력 소모 배율 시스템 관리
    
    /// <summary>
    /// 정신력 소모 배율 시스템 초기화
    /// </summary>
    private void InitializeMentalCostSystem()
    {
        // 모든 슬롯의 배율을 1.0으로 초기화
        mentalCostMultipliers[SkillSlotType.Z] = 1.0f;
        mentalCostMultipliers[SkillSlotType.X] = 1.0f;
        mentalCostMultipliers[SkillSlotType.C] = 1.0f;
        mentalCostMultipliers[SkillSlotType.V] = 1.0f;
        
        // 가중치는 나중에 SetSkillData에서 업데이트됨
        mentalCostWeights[SkillSlotType.Z] = 1.0f;
        mentalCostWeights[SkillSlotType.X] = 1.0f;
        mentalCostWeights[SkillSlotType.C] = 1.0f;
        mentalCostWeights[SkillSlotType.V] = 1.0f;
        
        // 최소치도 초기화
        mentalCostMinimums[SkillSlotType.Z] = 0.0f;
        mentalCostMinimums[SkillSlotType.X] = 0.0f;
        mentalCostMinimums[SkillSlotType.C] = 0.0f;
        mentalCostMinimums[SkillSlotType.V] = 0.0f;
    }
    
    /// <summary>
    /// 스킬 데이터 설정 시 가중치 및 최소치 업데이트
    /// </summary>
    /// <param name="slotType">슬롯 타입</param>
    /// <param name="skillData">스킬 데이터</param>
    public void UpdateSkillWeight(SkillSlotType slotType, PlayerSkillData skillData)
    {
        if (skillData == null) return;
        
        // 가중치 업데이트
        if (!mentalCostWeights.ContainsKey(slotType))
        {
            mentalCostWeights[slotType] = 1.0f;
        }
        mentalCostWeights[slotType] = skillData.mentalCostReductionWeight;
        
        // 최소치 업데이트
        if (!mentalCostMinimums.ContainsKey(slotType))
        {
            mentalCostMinimums[slotType] = 0.0f;
        }
        mentalCostMinimums[slotType] = skillData.mentalCostReductionMinimum;
        
        Debug.Log($"[SkillController] {slotType} 슬롯 업데이트 - 가중치: {mentalCostWeights[slotType]}, 최소치: {mentalCostMinimums[slotType]}");
    }
    
    /// <summary>
    /// 슬롯의 정신력 소모 배율 가져오기
    /// </summary>
    /// <param name="slotType">슬롯 타입</param>
    /// <returns>정신력 소모 배율 (기본값 1.0)</returns>
    public float GetMentalCostMultiplier(SkillSlotType slotType)
    {
        if (!mentalCostMultipliers.ContainsKey(slotType))
        {
            mentalCostMultipliers[slotType] = 1.0f;
        }
        return mentalCostMultipliers[slotType];
    }
    
    /// <summary>
    /// 스킬 사용 후 다른 스킬들의 배율 감소 처리
    /// </summary>
    /// <param name="usedSlotType">사용한 슬롯 타입</param>
    /// <param name="reductionAmount">감소량 (사용한 스킬의 otherSkillMentalCostReduction, 1.0이면 감소 없음)</param>
    public void OnSkillUsed(SkillSlotType usedSlotType, float reductionAmount)
    {
        Debug.Log($"[SkillController] ===== {usedSlotType} 스킬 사용 - 배율 시스템 업데이트 시작 =====");
        
        // 사용한 스킬의 배율을 1.0으로 리셋
        float previousMultiplier = mentalCostMultipliers.ContainsKey(usedSlotType) ? mentalCostMultipliers[usedSlotType] : 1.0f;
        if (!mentalCostMultipliers.ContainsKey(usedSlotType))
        {
            mentalCostMultipliers[usedSlotType] = 1.0f;
        }
        mentalCostMultipliers[usedSlotType] = 1.0f;
        
        Debug.Log($"[SkillController] {usedSlotType} 스킬 배율 리셋: {previousMultiplier:F3} → 1.000");
        Debug.Log($"[SkillController] 사용한 스킬의 감소량: {reductionAmount:F3} (1.0 = 감소 없음)");
        
        // 감소량이 1.0 미만일 때만 다른 스킬들의 배율 감소 적용
        if (reductionAmount < 1.0f)
        {
            Debug.Log($"[SkillController] 감소량이 1.0 미만이므로 다른 스킬들의 배율 감소 적용 시작...");
            
            // ⭐ Dictionary를 순회하는 도중 수정을 방지하기 위해 키 목록을 먼저 복사
            List<SkillSlotType> slotKeys = new List<SkillSlotType>(mentalCostMultipliers.Keys);
            
            // 사용한 스킬 제외한 다른 슬롯들에 감소 적용
            foreach (var slotKey in slotKeys)
            {
                if (slotKey == usedSlotType) continue; // 사용한 스킬은 제외
                
                // 현재 배율 × 감소량 × 대상 스킬의 가중치
                float currentMultiplier = mentalCostMultipliers[slotKey];
                float weight = mentalCostWeights.ContainsKey(slotKey) ? mentalCostWeights[slotKey] : 1.0f;
                float minimum = mentalCostMinimums.ContainsKey(slotKey) ? mentalCostMinimums[slotKey] : 0.0f;
                float calculatedMultiplier = currentMultiplier * reductionAmount * weight;
                
                // ⭐ 최소치 적용 (계산된 배율이 최소치보다 작으면 최소치로 고정)
                float newMultiplier = Mathf.Max(calculatedMultiplier, minimum);
                
                // ⭐ 디버그: 계산 과정 상세 로그
                bool isClamped = (newMultiplier > calculatedMultiplier);
                Debug.Log($"[SkillController] {slotKey} 스킬 배율 계산:\n" +
                    $"  - 기존 배율: {currentMultiplier:F3}\n" +
                    $"  - 감소량: {reductionAmount:F3} ({usedSlotType} 스킬 사용으로 인한 감소)\n" +
                    $"  - 가중치: {weight:F3} ({slotKey} 스킬의 감소 가중치)\n" +
                    $"  - 계산식: {currentMultiplier:F3} × {reductionAmount:F3} × {weight:F3} = {calculatedMultiplier:F3}\n" +
                    $"  - 최소치: {minimum:F3} {(minimum > 0f ? $"(최소치 적용: {isClamped})" : "(제한 없음)")}\n" +
                    $"  - 최종 결과: {currentMultiplier:F3} → {newMultiplier:F3} (변화량: {newMultiplier - currentMultiplier:F3}){(isClamped ? " [최소치로 고정됨]" : "")}");
                
                // 순회 중이 아닌 상태에서 수정
                mentalCostMultipliers[slotKey] = newMultiplier;
            }
        }
        else
        {
            Debug.Log($"[SkillController] 감소량이 1.0이므로 다른 스킬들의 배율 감소 없음");
        }
        
        // ⭐ 전체 배율 상태 출력
        Debug.Log($"[SkillController] 현재 모든 슬롯 배율 상태:\n" +
            $"  - Z: {GetMentalCostMultiplier(SkillSlotType.Z):F3}\n" +
            $"  - X: {GetMentalCostMultiplier(SkillSlotType.X):F3}\n" +
            $"  - C: {GetMentalCostMultiplier(SkillSlotType.C):F3}\n" +
            $"  - V: {GetMentalCostMultiplier(SkillSlotType.V):F3}");
        
        Debug.Log($"[SkillController] ===== {usedSlotType} 스킬 사용 - 배율 시스템 업데이트 완료 =====");
    }
    
    /// <summary>
    /// 특정 슬롯의 배율만 리셋 (정신력 소모가 없는 경우)
    /// </summary>
    /// <param name="slotType">리셋할 슬롯 타입</param>
    public void ResetMultiplierForSlot(SkillSlotType slotType)
    {
        if (!mentalCostMultipliers.ContainsKey(slotType))
        {
            mentalCostMultipliers[slotType] = 1.0f;
        }
        mentalCostMultipliers[slotType] = 1.0f;
        
        Debug.Log($"[SkillController] {slotType} 슬롯 배율 리셋: 1.0");
    }
    
    #endregion
}

// ⭐ SkillHandlerInterface는 Core/SkillHandlerInterface.cs로 이동됨