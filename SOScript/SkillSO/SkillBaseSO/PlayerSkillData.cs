using UnityEngine;

/// <summary>
/// 플레이어 스킬 데이터 ScriptableObject
/// BaseSkillData를 상속받아 플레이어 전용 필드 추가
/// </summary>
[CreateAssetMenu(fileName = "PlayerSkillData", menuName = "Skills/Player Skill Data")]
public class PlayerSkillData : BaseSkillData
{
    [Header("플레이어 전용 - 스킬 타입")]
    [Tooltip("스킬 타입 (Courage, Justice, Temperance, Wisdom)")]
    public SkillType skillType;
    
    [Header("플레이어 전용 - 정신력")]
    [Tooltip("스킬 사용 시 소모되는 정신력")]
    public int mentalCost = 0;
    
    [Header("플레이어 전용 - 데미지")]
    [Tooltip("데미지 퍼센테이지 (공격력의 % 적용, 100 = 공격력 100%)")]
    public float damagePercent = 100f;
    
    [Header("플레이어 전용 - 랭크별 배율")]
    [Tooltip("랭크별 데미지 배율 [D, C, B, A, S]")]
    public float[] damageMultiplierByRank = new float[] { 1f, 1.2f, 1.5f, 2f, 3f };
    
    [Tooltip("랭크별 범위 배율 [D, C, B, A, S]")]
    public float[] rangeMultiplierByRank = new float[] { 1f, 1.1f, 1.3f, 1.5f, 2f };
    
    /// <summary>
    /// 랭크에 따른 데미지 배율 가져오기
    /// </summary>
    public float GetDamageMultiplier(SkillRank rank)
    {
        int index = (int)rank;
        if (damageMultiplierByRank != null && index >= 0 && index < damageMultiplierByRank.Length)
        {
            return damageMultiplierByRank[index];
        }
        return 1f;
    }
    
    /// <summary>
    /// 랭크에 따른 범위 배율 가져오기
    /// </summary>
    public float GetRangeMultiplier(SkillRank rank)
    {
        int index = (int)rank;
        if (rangeMultiplierByRank != null && index >= 0 && index < rangeMultiplierByRank.Length)
        {
            return rangeMultiplierByRank[index];
        }
        return 1f;
    }
}

