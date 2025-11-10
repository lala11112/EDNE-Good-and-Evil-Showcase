using UnityEngine;

/// <summary>
/// 장비 아이템
/// 지속 효과를 적용하고 인벤토리에 등록됩니다.
/// </summary>
public class EquipmentItem : ItemBase
{
    [Header("장비 아이템 설정")]
    [Tooltip("공격력 증가량 (인스펙터에서 설정)")]
    [SerializeField] private float attackPowerBonus = 0f;
    
    [Tooltip("이동 속도 증가량 (인스펙터에서 설정)")]
    [SerializeField] private float moveSpeedBonus = 0f;
    
    [Tooltip("체력 증가량 (인스펙터에서 설정)")]
    [SerializeField] private int maxHealthBonus = 0;
    
    [Tooltip("정신력 증가량 (인스펙터에서 설정)")]
    [SerializeField] private int maxMentalPowerBonus = 0;
    
    /// <summary>
    /// 아이템 획득 시 처리
    /// 장비 아이템은 인벤토리에 등록되고 지속 효과를 적용합니다.
    /// </summary>
    protected override void OnItemAcquired()
    {
        if (playerController == null) return;
        
        // TODO: 인벤토리에 등록 (나중에 InventoryController 구현 시)
        // InventoryController.Instance.AddItem(this);
        
        // 지속 효과 적용 (나중에 이벤트 시스템으로 처리)
        // 현재는 로그만 출력
        Debug.Log($"[EquipmentItem] {itemData.itemName} 획득!");
        Debug.Log($"[EquipmentItem] 효과: 공격력 +{attackPowerBonus}, 이동속도 +{moveSpeedBonus}, 체력 +{maxHealthBonus}, 정신력 +{maxMentalPowerBonus}");
        
        // TODO: 이벤트 발생하여 PlayerController에 효과 적용
        // ItemEvents.InvokeItemAcquired(this);
    }
    
    /// <summary>
    /// 아이템 삭제 시 처리
    /// 장비 아이템의 효과를 제거합니다.
    /// </summary>
    protected override void OnItemDestroyed()
    {
        // TODO: 인벤토리에서 제거 (나중에 InventoryController 구현 시)
        // InventoryController.Instance.RemoveItem(this);
        
        // 효과 제거 (나중에 이벤트 시스템으로 처리)
        Debug.Log($"[EquipmentItem] {itemData.itemName} 효과 제거");
        
        // TODO: 이벤트 발생하여 PlayerController에 효과 제거
        // ItemEvents.InvokeItemRemoved(this);
    }
    
    #region 프로퍼티 (효과 데이터 접근용)
    
    /// <summary>공격력 증가량</summary>
    public float AttackPowerBonus => attackPowerBonus;
    
    /// <summary>이동 속도 증가량</summary>
    public float MoveSpeedBonus => moveSpeedBonus;
    
    /// <summary>체력 증가량</summary>
    public int MaxHealthBonus => maxHealthBonus;
    
    /// <summary>정신력 증가량</summary>
    public int MaxMentalPowerBonus => maxMentalPowerBonus;
    
    #endregion
}

