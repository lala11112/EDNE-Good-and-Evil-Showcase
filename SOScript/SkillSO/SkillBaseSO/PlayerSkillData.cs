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
    
    [Tooltip("다른 스킬 사용 시 줄어들 정신력 소모량 (0.1~1.0, 기본값 1.0 = 감소 없음)")]
    [Range(0.1f, 1.0f)]
    public float otherSkillMentalCostReduction = 0.9f;
    
    [Tooltip("정신력 감소 가중치 (다른 스킬 사용 시 자신의 감소에 영향을 미치는 정도, 기본값 1.0)")]
    [Range(0f, 2f)]
    public float mentalCostReductionWeight = 1.0f;
    
    [Tooltip("정신력 감소 최소치 (이 값 이하로는 정신력 배율이 내려가지 않음, 기본값 0.0 = 제한 없음)")]
    [Range(0f, 1f)]
    public float mentalCostReductionMinimum = 0.0f;
    
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

