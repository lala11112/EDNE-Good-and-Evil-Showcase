using UnityEngine;

public class PlayerHp : HealthBase
{
    
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
