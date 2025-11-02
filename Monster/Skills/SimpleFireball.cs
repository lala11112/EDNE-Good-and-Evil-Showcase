using UnityEngine;

/// <summary>
/// 간단한 파이어볼 투사체
/// ISkillProjectile 구현으로 MonsterController와 호환
/// </summary>
public class SimpleFireball : MonoBehaviour, ISkillProjectile
{
    [Header("설정")]
    [SerializeField] private float lifetime = 5f; // 자동 파괴 시간
    
    private float damage;
    private float speed;
    private EntityType attackerType;
    private MonoBehaviour ownerController;
    private bool isPiercing; // 관통 여부
    
    #region ISkillProjectile 구현
    
    public void SetSkillData(float damage, float speed)
    {
        this.damage = damage;
        this.speed = speed;
        
        Debug.Log($"[SimpleFireball] 데미지: {damage}, 속도: {speed}");
    }
    
    public void SetAttackerType(EntityType type)
    {
        this.attackerType = type;
        
        Debug.Log($"[SimpleFireball] 공격자 타입: {type}");
    }
    
    public void SetOwnerController(MonoBehaviour owner)
    {
        this.ownerController = owner;
    }
    
    public void SetPiercing(bool isPiercing)
    {
        this.isPiercing = isPiercing;
        
        Debug.Log($"[SimpleFireball] 관통 여부: {(isPiercing ? "관통형" : "충돌 시 파괴")}");
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Start()
    {
        // 일정 시간 후 자동 파괴
        Destroy(gameObject, lifetime);
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // EntityController가 있는 대상과 충돌 시
        EntityController target = collision.GetComponent<EntityController>();
        if (target != null)
        {
            // 데미지 적용 시도 (팀킬 방지 포함)
            bool damageApplied = target.ApplyDamage((int)damage, attackerType);
            
            if (damageApplied)
            {
                // ✅ 적에게 데미지를 줬음 -> 관통 여부에 따라 파괴 결정
                Debug.Log($"[SimpleFireball] {target.name}에게 {damage} 데미지!");
                
                if (!isPiercing)
                {
                    // 비관통형: 적에게 맞았으므로 파괴
                    Debug.Log($"[SimpleFireball] 비관통형 - 충돌로 인해 파괴됨");
                    Destroy(gameObject);
                }
                else
                {
                    // 관통형: 적을 뚫고 계속 날아감
                    Debug.Log($"[SimpleFireball] 관통형 - 파괴되지 않고 계속 날아감");
                }
            }
            else
            {
                // ❌ 아군이라 데미지가 안 들어감 -> 무조건 통과 (파괴 안함)
                Debug.Log($"[SimpleFireball] {target.name}은 아군이라 통과!");
            }
        }
    }
    
    #endregion
}

