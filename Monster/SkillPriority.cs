using UnityEngine;

/// <summary>
/// 몬스터 스킬 우선순위 설정 클래스
/// Inspector에서 스킬을 배열로 설정할 수 있습니다.
/// </summary>
[System.Serializable]
public class SkillPriority
{
    [Header("스킬 설정")]
    [Tooltip("사용할 몬스터 스킬 데이터 (MonsterSkillData SO)")]
    public MonsterSkillData skill;
    
    [Header("우선순위 설정")]
    [Tooltip("낮을수록 우선순위가 높습니다 (1이 가장 높음)")]
    public int priority = 1;
    
    [Header("사용 거리 설정")]
    [Tooltip("이 스킬을 사용할 최소 거리")]
    public float minRange = 0f;
    
    [Tooltip("이 스킬을 사용할 최대 거리")]
    public float maxRange = 10f;
    
    [Header("투사체 설정")]
    [Tooltip("관통형 투사체 여부 (true = 적 관통, false = 충돌 시 파괴)")]
    public bool isPiercing = false;
    
    [Header("이동 제어 설정")]
    [Tooltip("스킬 사용 중 이동 잠금 여부 (true = 가만히 서서 사용, false = 이동하면서 사용 가능)")]
    public bool lockMovementDuringSkill = false;
    
    [Tooltip("이동 잠금 지속 시간 (초) - lockMovementDuringSkill이 true일 때만 적용")]
    public float movementLockDuration = 1f;
}

