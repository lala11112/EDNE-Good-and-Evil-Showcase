using UnityEngine;

/// <summary>
/// C랭크 심작박동 아이템 데이터
/// 연속 처치 3회 시 다음 공격의 데미지가 증가합니다.
/// </summary>
[CreateAssetMenu(fileName = "New Heartbeat Item Data", menuName = "Item/Heartbeat Item Data", order = 4)]
public class HeartbeatItemDataSO : ItemDataSO
{
    [Header("심작박동 설정")]
    [Tooltip("기본 받는 피해 증가량 (%)")]
    [SerializeField] private float baseDamageIncrease = 10f; // +10%
    
    [Tooltip("연속 처치 필요 횟수")]
    [SerializeField] private int requiredKillCount = 3;
    
    [Tooltip("다음 공격 데미지 증가량 (%)")]
    [SerializeField] private float nextAttackDamageBonus = 150f; // +150%
    
    #region 프로퍼티
    
    /// <summary>기본 받는 피해 증가량 (%)</summary>
    public float BaseDamageIncrease => baseDamageIncrease;
    
    /// <summary>연속 처치 필요 횟수</summary>
    public int RequiredKillCount => requiredKillCount;
    
    /// <summary>다음 공격 데미지 증가량 (%)</summary>
    public float NextAttackDamageBonus => nextAttackDamageBonus;
    
    #endregion
}

