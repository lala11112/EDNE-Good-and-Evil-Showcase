using UnityEngine;

/// <summary>
/// 스킬 기본 데이터 ScriptableObject
/// 플레이어와 몬스터 스킬의 공통 데이터
/// 이 클래스를 상속받아 PlayerSkillData, MonsterSkillData 생성
/// </summary>
public abstract class BaseSkillData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("스킬 이름")]
    public string skillName;
    
    [Tooltip("스킬 설명")]
    [TextArea(2, 3)]
    public string description;
    
    [Tooltip("스킬 아이콘 (UI용)")]
    public Sprite skillIcon;
    
    [Header("쿨타임")]
    [Tooltip("스킬 재사용 대기 시간 (초)")]
    public float cooldown;
    
    [Header("핸들러 프리팹")]
    [Tooltip("스킬 실행 로직이 담긴 프리팹 (SkillHandlerInterface 구현 필요)")]
    public GameObject handlerPrefab;
    
    [Header("기본 성능")]
    [Tooltip("투사체 속도 (투사체 스킬인 경우)")]
    public float projectileSpeed = 10f;
    
    [Header("이펙트 (하위 호환용)")]
    [Tooltip("스킬 이펙트 프리팹 (투사체, AOE 등) - handlerPrefab 없을 때 사용")]
    public GameObject skillEffectPrefab;
}