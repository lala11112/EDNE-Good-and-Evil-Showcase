using UnityEngine;

/// <summary>
/// 소비 아이템
/// 즉시 효과를 적용하고 인벤토리에 등록되지 않습니다.
/// </summary>
public class ConsumableItem : ItemBase
{
    [Header("소비 아이템 설정")]
    [Tooltip("즉시 적용할 효과 (인스펙터에서 설정)")]
    [SerializeField] private int healAmount = 0;
    
    [Tooltip("즉시 적용할 정신력 회복량 (인스펙터에서 설정)")]
    [SerializeField] private int mentalPowerRestoreAmount = 0;
    
    /// <summary>
    /// 아이템 획득 시 처리
    /// 소비 아이템은 즉시 효과를 적용하고 사라집니다.
    /// </summary>
    protected override void OnItemAcquired()
    {
        if (playerController == null) return;
        
        // 체력 회복
        if (healAmount > 0)
        {
            playerController.Heal(healAmount);
            Debug.Log($"[ConsumableItem] {itemData.itemName} - 체력 {healAmount} 회복");
        }
        
        // 정신력 회복
        if (mentalPowerRestoreAmount > 0)
        {
            playerController.RestoreMentalPower(mentalPowerRestoreAmount);
            Debug.Log($"[ConsumableItem] {itemData.itemName} - 정신력 {mentalPowerRestoreAmount} 회복");
        }
        
        Debug.Log($"[ConsumableItem] {itemData.itemName} 획득 및 사용 완료!");
    }
    
    /// <summary>
    /// 아이템 삭제 시 처리
    /// 소비 아이템은 즉시 사용되므로 효과 제거가 필요 없습니다.
    /// </summary>
    protected override void OnItemDestroyed()
    {
        // 소비 아이템은 즉시 사용되므로 효과 제거 불필요
    }
}

