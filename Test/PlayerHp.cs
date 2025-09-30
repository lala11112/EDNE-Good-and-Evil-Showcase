using UnityEngine;

public class PlayerHp : HealthBase
{
    
   public void PlayerDecrease(int amount)
    {
        Damage(amount);
    }

    // HP 증가
    public void PlayerIncrease(int amount)
    {
        Heal(amount);
    }
}
