using UnityEngine;

/// <summary>
/// A랭크 확신의 사슬 아이템 데이터
/// 몬스터 연속 처치 시 공격력이 증가합니다.
/// </summary>
[CreateAssetMenu(fileName = "New Chain Item Data", menuName = "Item/Chain Item Data", order = 2)]
public class ChainItemDataSO : ItemDataSO
{
    [Header("확신의 사슬 설정")]
    [Tooltip("처치당 공격력 증가량 (%)")]
    [SerializeField] private float attackPowerPerKill = 10f; // 10%
    
    [Tooltip("최대 스택 수")]
    [SerializeField] private int maxStack = 5;
    
    [Tooltip("연속 처치 유지 시간 (초)")]
    [SerializeField] private float killChainDuration = 5f;
    
    #region 프로퍼티
    
    /// <summary>처치당 공격력 증가량 (%)</summary>
    public float AttackPowerPerKill => attackPowerPerKill;
    
    /// <summary>최대 스택 수</summary>
    public int MaxStack => maxStack;
    
    /// <summary>연속 처치 유지 시간 (초)</summary>
    public float KillChainDuration => killChainDuration;
    
    #endregion
}

