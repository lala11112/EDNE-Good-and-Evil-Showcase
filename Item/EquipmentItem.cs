using UnityEngine;

/// <summary>
/// 장비 아이템
/// 지속 효과를 적용하고 ItemController에 등록됩니다.
/// 효과 수치는 ItemDataSO에서 관리하며, ItemController에서 효과를 적용합니다.
/// </summary>
public class EquipmentItem : ItemBase
{
    /// <summary>
    /// 아이템 획득 시 처리
    /// ItemController에 아이템 데이터를 전달하여 등록합니다.
    /// </summary>
    protected override void OnItemAcquired()
    {
        if (itemData == null)
        {
            Debug.LogError("[EquipmentItem] itemData가 null입니다!");
            return;
        }
        
        // ItemController에 아이템 등록
        if (ItemController.Instance != null)
        {
            ItemController.Instance.RegisterItem(itemData);
            Debug.Log($"[EquipmentItem] {itemData.itemName} 획득! ItemController에 등록되었습니다.");
        }
        else
        {
            Debug.LogError("[EquipmentItem] ItemController.Instance를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 아이템 삭제 시 처리
    /// 아이템 제거는 나중에 다른 곳에서 처리합니다.
    /// </summary>
    protected override void OnItemDestroyed()
    {
        // 아이템 제거는 나중에 다른 곳에서 처리
        // 현재는 등록만 하고 제거는 하지 않음
    }
}

