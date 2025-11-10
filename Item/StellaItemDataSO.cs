using UnityEngine;

/// <summary>
/// B랭크 스텔라 프로토콜 아이템 데이터
/// 피격 시 아이템이 파괴되며 공격력과 받는 피해가 증가합니다.
/// </summary>
[CreateAssetMenu(fileName = "New Stella Item Data", menuName = "Item/Stella Item Data", order = 3)]
public class StellaItemDataSO : ItemDataSO
{
    [Header("스텔라 프로토콜 설정")]
    [Tooltip("공격력 증가량 (%)")]
    [SerializeField] private float attackPowerBonus = 100f; // +100%
    
    [Tooltip("받는 피해 증가량 (%)")]
    [SerializeField] private float damageTakenBonus = 100f; // +100%
    
    #region 프로퍼티
    
    /// <summary>공격력 증가량 (%)</summary>
    public float AttackPowerBonus => attackPowerBonus;
    
    /// <summary>받는 피해 증가량 (%)</summary>
    public float DamageTakenBonus => damageTakenBonus;
    
    #endregion
}

