using UnityEngine;

/// <summary>
/// 스킬 사용 기록 구조체
/// 용기 시스템에서 스킬 연계 판정을 위해 사용됩니다.
/// </summary>
[System.Serializable]
public struct SkillUseRecord
{
    public float time;
    public SkillSlotType skillSlotType;

    public SkillUseRecord(float time, SkillSlotType skillSlotType)
    {
        this.time = time;
        this.skillSlotType = skillSlotType;
    }
}

