using UnityEngine;

// Unity 에디터에서 에셋을 생성할 수 있게 메뉴 등록
[CreateAssetMenu(menuName = "Game/Enemy Skill Data", fileName = "New_Enemy_Skill_Data")] 
public class EnemySkillData : ScriptableObject
{
    [Header("스킬 능력치")]
    public string skillName = "Basic Attack";
    public float cooldownTime = 1f;       // 발사 간격 (Manager 제어)
    public float damage = 10f;            // 데미지 (Projectile 제어)
    public float projectileSpeed = 15f;   // 투사체 속도 (Projectile 제어)
    
    [Header("실행 프리팹")]
    // 이 SO에 연결될 실제 투사체 프리팹 (ProjectileSkillEffect 컴포넌트 포함 필수)
    public GameObject skillEffectPrefab; 
}