using UnityEngine;

/// <summary>
/// 처치 기록 (15초간 유지)
/// </summary>
[System.Serializable]
public class KillRecord
{
    /// <summary>
    /// 처치 시간
    /// </summary>
    public float time;

    /// <summary>
    /// 처치에 사용한 스킬 타입
    /// </summary>
    public SkillSlotType skillSlotType;

    public KillRecord(float time, SkillSlotType skillSlotType)
    {
        this.time = time;
        this.skillSlotType = skillSlotType;
    }
}

/// <summary>
/// 스킬 사용 기록 (5초간 유지 - 스킬 연계 판정용)
/// </summary>
[System.Serializable]
public class SkillUseRecord
{
    /// <summary>
    /// 스킬 사용 시간
    /// </summary>
    public float time;

    /// <summary>
    /// 사용한 스킬 타입
    /// </summary>
    public SkillSlotType skillSlotType;

    public SkillUseRecord(float time, SkillSlotType skillSlotType)
    {
        this.time = time;
        this.skillSlotType = skillSlotType;
    }
}

/// <summary>
/// 멀티킬 기록 (3초간 유지)
/// </summary>
[System.Serializable]
public class MultiKillRecord
{
    /// <summary>
    /// 처치 시간
    /// </summary>
    public float time;

    /// <summary>
    /// 동시 처치 수
    /// </summary>
    public int killCount;

    /// <summary>
    /// 사용한 스킬 타입
    /// </summary>
    public SkillSlotType skillSlotType;

    public MultiKillRecord(float time, int killCount, SkillSlotType skillSlotType)
    {
        this.time = time;
        this.killCount = killCount;
        this.skillSlotType = skillSlotType;
    }
}

