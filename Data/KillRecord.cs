using UnityEngine;

/// <summary>
/// 처치 기록 구조체
/// 용기 시스템에서 처치 기록을 추적하기 위해 사용됩니다.
/// </summary>
[System.Serializable]
public struct KillRecord
{
    public float time;
    public SkillSlotType skillSlotType;

    public KillRecord(float time, SkillSlotType skillSlotType)
    {
        this.time = time;
        this.skillSlotType = skillSlotType;
    }
}

