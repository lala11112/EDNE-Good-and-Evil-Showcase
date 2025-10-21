using UnityEngine;

public class EnemyHp : HealthBase
{
    public Animator animator;

    // HP 감소
    public void EnemyDecrease(int amount)
    {
        Damage(amount);

        // 애니메이션 트리거 실행
        if (animator != null)
            animator.SetTrigger("OnDamage");
            Debug.Log("EnemyHp 감소");
            
    }

    // HP 증가
    public void EnemyIncrease(int amount)
    {
        Heal(amount);
    }
}
