using UnityEngine;

/// <summary>
/// 멀티킬 기록 구조체
/// 용기 시스템에서 멀티킬 판정을 위해 사용됩니다.
/// </summary>
[System.Serializable]
public struct MultiKillRecord
{
    public float time;
    public int killCount;
    public SkillSlotType skillSlotType;

    public MultiKillRecord(float time, int killCount, SkillSlotType skillSlotType)
    {
        this.time = time;
        this.killCount = killCount;
        this.skillSlotType = skillSlotType;
    }
}

