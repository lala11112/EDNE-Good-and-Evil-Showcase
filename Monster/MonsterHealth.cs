using UnityEngine;

/// <summary>
/// 몬스터의 체력을 관리하는 컴포넌트
/// MonsterDataComponent를 참조하여 스크립터블 오브젝트의 데이터를 사용합니다.
/// </summary>
[RequireComponent(typeof(MonsterDataComponent))]
public class MonsterHealth : MonoBehaviour
{
    private MonsterDataComponent dataComponent;
    private Animator animator;

    private void Awake()
    {
        dataComponent = GetComponent<MonsterDataComponent>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // 게임 시작 시 체력 초기화
        if (dataComponent?.Data != null)
        {
            dataComponent.Data.Initialize();
        }
    }

    /// <summary>
    /// 데미지를 받습니다.
    /// </summary>
    /// <param name="damage">받을 데미지 양</param>
    public void TakeDamage(int damage)
    {
        if (dataComponent?.Data == null) return;

        bool isDead = dataComponent.Data.TakeDamage(damage);

        // 피격 애니메이션
        if (animator != null)
        {
            animator.SetTrigger("OnDamage");
        }

        Debug.Log($"{dataComponent.Data.monsterName} HP: {dataComponent.Data.currentHealth}/{dataComponent.Data.maxHealth}");

        // 사망 처리
        if (isDe
        {
            Die();
        }
    }

    /// <summary>
    /// 체력을 회복합니다.
    /// </summary>
    /// <param name="amount">회복할 체력 양</param>
    public void Heal(int amount)
    {
        if (dataComponent?.Data != null)
        {
            dataComponent.Data.Heal(amount);
        }
    }

    /// <summary>
    /// 몬스터 사망 처리
    /// </summary>
    private void Die()
    {
        Debug.Log($"{dataComponent.Data.monsterName} 사망!");
        
        // 사망 애니메이션
        if (animator != null)
        {
            animator.SetTrigger("OnDeath");
        }

        // 일정 시간 후 삭제 (애니메이션 재생 시간 고려)
        Destroy(gameObject, 1f);
    }

    /// <summary>
    /// 현재 체력 비율 반환 (UI용)
    /// </summary>
    public float GetHealthPercentage()
    {
        return dataComponent?.Data?.GetHealthPercentage() ?? 0f;
    }
}
