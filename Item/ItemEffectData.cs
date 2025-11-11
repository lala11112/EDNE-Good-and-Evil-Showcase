using UnityEngine;

/// <summary>
/// 아이템 효과 데이터 구조체
/// 각 아이템의 효과 정보를 저장합니다.
/// </summary>

/// <summary>
/// S랭크 운명의 주사위 효과 데이터
/// </summary>
[System.Serializable]
public struct DiceEffectData
{
    /// <summary>공격력 변경량 (%)</summary>
    public float attackPowerModifier;
    
    /// <summary>이동속도 변경량 (%)</summary>
    public float moveSpeedModifier;
    
    /// <summary>정신력회복속도 변경량 (%)</summary>
    public float mentalRecoverySpeedModifier;
    
    /// <summary>
    /// 효과 데이터 생성
    /// </summary>
    public DiceEffectData(float attackPower, float moveSpeed, float mentalRecovery)
    {
        attackPowerModifier = attackPower;
        moveSpeedModifier = moveSpeed;
        mentalRecoverySpeedModifier = mentalRecovery;
    }
}

/// <summary>
/// A랭크 확신의 사슬 효과 데이터
/// </summary>
[System.Serializable]
public struct ChainEffectData
{
    /// <summary>현재 스택 수 (0~5)</summary>
    public int currentStack;
    
    /// <summary>연속 처치 타이머 (초)</summary>
    public float killChainTimer;
    
    /// <summary>
    /// 효과 데이터 생성
    /// </summary>
    public ChainEffectData(int stack, float timer)
    {
        currentStack = stack;
        killChainTimer = timer;
    }
}

/// <summary>
/// B랭크 스텔라 프로토콜 효과 데이터
/// </summary>
[System.Serializable]
public struct StellaEffectData
{
    /// <summary>활성화 여부 (피격 시 false로 변경되어 아이템 파괴)</summary>
    public bool isActive;
    
    /// <summary>
    /// 효과 데이터 생성
    /// </summary>
    public StellaEffectData(bool active)
    {
        isActive = active;
    }
}

/// <summary>
/// C랭크 심작박동 효과 데이터
/// </summary>
[System.Serializable]
public struct HeartbeatEffectData
{
    /// <summary>연속 처치 카운트 (0~3)</summary>
    public int killCount;
    
    /// <summary>다음 공격 데미지 증가 플래그</summary>
    public bool hasNextAttackBonus;
    
    /// <summary>
    /// 효과 데이터 생성
    /// </summary>
    public HeartbeatEffectData(int count, bool hasBonus)
    {
        killCount = count;
        hasNextAttackBonus = hasBonus;
    }
}

