using UnityEngine;

// Unity 에디터에서 에셋을 생성할 수 있게 메뉴 등록
[CreateAssetMenu(menuName = "Game/Enemy Skill Data", fileName = "New_Enemy_Skill_Data")] 
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