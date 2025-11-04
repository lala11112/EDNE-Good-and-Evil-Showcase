using UnityEngine;

/// <summary>
/// 몬스터의 모든 속성과 데이터를 관리하는 ScriptableObject
/// 이 클래스는 몬스터의 기본 정보, 스탯, 그리고 런타임 상태를 포함합니다.
/// ScriptableObject를 사용하여 데이터와 로직을 분리하고, 메모리 효율성을 높입니다.
/// </summary>
[CreateAssetMenu(fileName = "MonsterData", menuName = "Monster/MonsterData")]
public class MonsterData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("몬스터의 이름입니다. 고유 식별자로 사용됩니다.")]
    public string monsterName;
    
    [Tooltip("몬스터의 스프라이트 이미지입니다.")]
    public Sprite monsterSprite;
    
    [Header("스탯")]
    [Tooltip("몬스터의 최대 체력입니다.")]
    public int maxHealth;
    
    [Tooltip("몬스터의 공격력입니다.")]
    public int attack;
    
    [Tooltip("몬스터의 이동속도입니다.")]
    public float moveSpeed;
    
    /// <summary>
    /// 현재 체력 (런타임에서만 사용)
    /// Inspector에서는 숨겨져 있으며, 게임 실행 중에만 변경됩니다. 
    /// </summary>
    [HideInInspector]
    public int currentHealth;
    
    /// <summary>
    /// 몬스터 데이터를 초기화합니다.
    /// 몬스터가 스폰될 때 호출되어 현재 체력을 최대 체력으로 설정합니다.
    /// </summary>
    public void Initialize()
    {
        currentHealth = maxHealth;
    }
    
    /// <summary>
    /// 몬스터가 데미지를 받을 때 호출되는 메서드
    /// </summary>
    /// <param name="damage">받을 데미지 양</param>
    /// <returns>몬스터가 죽었는지 여부 (true: 죽음, false: 생존)</returns>
    public bool TakeDamage(int damage)
    {
        // 현재 체력에서 데미지만큼 감소시키되, 0 이하로는 내려가지 않도록 제한
        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        // 체력이 0 이하면 몬스터가 죽은 것으로 판단
        return currentHealth <= 0;
    }
    
    /// <summary>
    /// 몬스터의 체력을 회복시킵니다.
    /// </summary>
    /// <param name="amount">회복할 체력 양</param>
    public void Heal(int amount)
    {
        // 최대 체력이 0보다 클 때만 회복
        if (maxHealth > 0)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }
    }
    
    /// <summary>
    /// 현재 체력의 비율을 0~1 사이의 값으로 반환합니다.
    /// UI에서 체력바를 표시할 때 사용됩니다.
    /// </summary>
    /// <returns>현재 체력 / 최대 체력 (0.0 ~ 1.0)</returns>
    public float GetHealthPercentage()
    {
        // 최대 체력이 0이면 0을 반환 (0으로 나누기 방지)
        if (maxHealth <= 0) return 0f;
        
        return (float)currentHealth / maxHealth;
    }
}
