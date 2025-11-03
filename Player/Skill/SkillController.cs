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
    private void OnSkillZPerformed(InputAction.CallbackContext context)
    {
        SkillRank rank = playerController != null ? playerController.currentSkillRank : SkillRank.D;
        zSlot?.ExecuteSkill(rank);
    }
    
    private void OnSkillXPerformed(InputAction.CallbackContext context)
    {
        SkillRank rank = playerController != null ? playerController.currentSkillRank : SkillRank.D;
        xSlot?.ExecuteSkill(rank);
    }
    
    private void OnSkillCPerformed(InputAction.CallbackContext context)
    {
        SkillRank rank = playerController != null ? playerController.currentSkillRank : SkillRank.D;
        cSlot?.ExecuteSkill(rank);
    }
    
    private void OnSkillVPerformed(InputAction.CallbackContext context)
    {
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
}

// 스킬 슬롯 타입 열거형 (새로 추가)
public enum SkillSlotType
{
    Z, X, C, V
}


public enum SkillRank { D, C, B, A, S }

// ⭐ SkillHandlerInterface는 Core/SkillHandlerInterface.cs로 이동됨