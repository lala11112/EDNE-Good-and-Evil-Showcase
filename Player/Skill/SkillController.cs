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
    
    private void Awake()
    {
        // Input Actions 초기화
        skillZAction = inputActions.FindAction("SkillZ");
        skillXAction = inputActions.FindAction("SkillX");
        skillCAction = inputActions.FindAction("SkillC");
        skillVAction = inputActions.FindAction("SkillV");
    }
    
    private void Start()
    {
        EnsureSlot(ref zSlot, "Z Slot");
        EnsureSlot(ref xSlot, "X Slot");
        EnsureSlot(ref cSlot, "C Slot");
        EnsureSlot(ref vSlot, "V Slot");

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
    
    // 스킬 사용 이벤트들 (간단해짐!)
    private void OnSkillZPerformed(InputAction.CallbackContext context)
    {
        zSlot?.ExecuteSkill();
    }
    
    private void OnSkillXPerformed(InputAction.CallbackContext context)
    {
        xSlot?.ExecuteSkill();
    }
    
    private void OnSkillCPerformed(InputAction.CallbackContext context)
    {
        cSlot?.ExecuteSkill();
    }
    
    private void OnSkillVPerformed(InputAction.CallbackContext context)
    {
        vSlot?.ExecuteSkill();
    }
    
    // 슬롯 보장: 없으면 생성해서 붙임
    private void EnsureSlot(ref SkillSlot slot, string name)
    {
        if (slot != null) return;
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        slot = go.AddComponent<SkillSlot>();
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