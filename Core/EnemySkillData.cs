using UnityEngine;

/// <summary>
/// 기존 EnemySkillData (하위 호환용)
/// 새로운 프로젝트에서는 MonsterSkillData 사용 권장
/// </summary>
[CreateAssetMenu(menuName = "Game/Enemy Skill Data (Legacy)", fileName = "New_Enemy_Skill_Data")] 
public class EnemySkillData : ScriptableObject
{
    [Header("스킬 능력치")]
    public string skillName = "Basic Attack";
    public float cooldownTime = 1f;       
    public float damage = 10f;            
    public float projectileSpeed = 15f;   
    
    [Header("실행 프리팹")]
    public GameObject skillEffectPrefab; 
}