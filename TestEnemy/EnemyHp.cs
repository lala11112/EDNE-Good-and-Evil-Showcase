using UnityEngine;

public class EnemyHp : HealthBase
{
    // HP 감소
    public void Decrease(int amount)
    {
        Damage(amount);
    }

    // HP 증가
    public void Increase(int amount)
    {
        Heal(amount);
    }
}