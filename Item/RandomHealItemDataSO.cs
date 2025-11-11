using UnityEngine;

/// <summary>
/// 랜덤 회복 아이템 데이터
/// HP를 일정 범위 내에서 랜덤으로 회복시킵니다.
/// </summary>
[CreateAssetMenu(fileName = "New Random Heal Item Data", menuName = "Item/Random Heal Item Data", order = 5)]
public class RandomHealItemDataSO : ItemDataSO
{
    [Header("랜덤 회복 설정")]
    [Tooltip("최소 회복 비율 (%) - 최대 체력의 몇 %를 최소로 회복할지")]
    [Range(0f, 100f)]
    [SerializeField] private float minHealPercent = 10f; // 10%
    
    [Tooltip("최대 회복 비율 (%) - 최대 체력의 몇 %를 최대로 회복할지")]
    [Range(0f, 100f)]
    [SerializeField] private float maxHealPercent = 100f; // 100%
    
    #region 프로퍼티
    
    /// <summary>최소 회복 비율 (%)</summary>
    public float MinHealPercent => minHealPercent;
    
    /// <summary>최대 회복 비율 (%)</summary>
    public float MaxHealPercent => maxHealPercent;
    
    #endregion
}

