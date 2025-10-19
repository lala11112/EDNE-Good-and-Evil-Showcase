using UnityEngine;

/// <summary>
/// 몬스터 스킬 데이터 ScriptableObject
/// BaseSkillData를 상속받아 몬스터 전용 필드 추가
/// </summary>
[CreateAssetMenu(fileName = "MonsterSkillData", menuName = "Skills/Monster Skill Data")]
public class MonsterSkillData : BaseSkillData
{
    [Header("몬스터 전용 - 데미지")]
    [Tooltip("직접 데미지 값 (고정 데미지, 퍼센테이지 아님)")]
    public float damage = 50f;
    
    // 추가 몬스터 전용 필드 (나중에 필요 시 추가)
    // 예: public float aggroRange; // 어그로 범위
    // 예: public bool isInterruptible; // 스킬 중단 가능 여부
    // 예: public float castTime; // 시전 시간
}

