using UnityEngine;
using System.Collections;

/// <summary>
/// 몬스터의 체력을 관리하는 컴포넌트
/// MonsterDataComponent를 참조하여 스크립터블 오브젝트의 데이터를 사용합니다.
/// IDamageable 인터페이스를 구현하여 데미지를 받을 수 있습니다.
/// </summary>
[RequireComponent(typeof(MonsterDataComponent))]
public class MonsterHealth : MonoBehaviour, IDamageable
{
    private MonsterDataComponent dataComponent;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    
    [Header("피격 효과 설정")]
    [Tooltip("피격 시 변경될 색상")]
    [SerializeField] private Color hitColor = Color.red;
    
    [Tooltip("피격 효과 지속 시간(초)")]
    [SerializeField] private float hitEffectDuration = 0.2f;

    // IDamageable 인터페이스 구현
    public bool IsAlive => dataComponent?.Data != null && dataComponent.Data.currentHealth > 0;
    public Transform Transform => transform;

    private void Awake()
    {
        dataComponent = GetComponent<MonsterDataComponent>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
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

        // 피격 색상 효과
        StartCoroutine(HitEffect());

        Debug.Log($"{dataComponent.Data.monsterName} HP: {dataComponent.Data.currentHealth}/{dataComponent.Data.maxHealth}");

        // 사망 처리
        if (isDead)
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

    /// <summary>
    /// 피격 시 색상 변경 효과
    /// </summary>
    private IEnumerator HitEffect()
    {
        if (spriteRenderer == null) yield break;

        // 원래 색상 저장
        Color originalColor = spriteRenderer.color;

        // 피격 색상으로 변경
        spriteRenderer.color = hitColor;

        // 지정된 시간만큼 대기
        yield return new WaitForSeconds(hitEffectDuration);

        // 원래 색상으로 복원
        spriteRenderer.color = originalColor;
    }
}
