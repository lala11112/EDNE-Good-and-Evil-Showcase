using UnityEngine;

/// <summary>
/// S랭크 운명의 주사위 아이템 데이터
/// 방마다 랜덤한 효과를 부여합니다.
/// </summary>
[CreateAssetMenu(fileName = "New Dice Item Data", menuName = "Item/Dice Item Data", order = 1)]
public class DiceItemDataSO : ItemDataSO
{
    [Header("운명의 주사위 설정")]
    [Tooltip("공격력 변경 범위 (최소, 최대)")]
    [SerializeField] private Vector2 attackPowerRange = new Vector2(-30f, 70f); // -30% ~ +70%
    
    [Tooltip("이동속도 변경 범위 (최소, 최대)")]
    [SerializeField] private Vector2 moveSpeedRange = new Vector2(-20f, 30f); // -20% ~ +30%
    
    [Tooltip("정신력회복속도 변경 범위 (최소, 최대)")]
    [SerializeField] private Vector2 mentalRecoverySpeedRange = new Vector2(-20f, 20f); // -20% ~ +20%
    
    #region 프로퍼티
    
    /// <summary>공격력 변경 범위</summary>
    public Vector2 AttackPowerRange => attackPowerRange;
    
    /// <summary>이동속도 변경 범위</summary>
    public Vector2 MoveSpeedRange => moveSpeedRange;
    
    /// <summary>정신력회복속도 변경 범위</summary>
    public Vector2 MentalRecoverySpeedRange => mentalRecoverySpeedRange;
    
    #endregion
}

