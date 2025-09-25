using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[AddComponentMenu("Game/Health/Health Base")]
[HelpURL("https://docs.unity3d.com/ScriptReference/TooltipAttribute.html")]
public abstract class HealthBase : MonoBehaviour
{
   
    [SerializeField, Tooltip("최대 HP 값 (1 이상). SetMax로 런타임 변경 가능")] 
    protected int maxHp = 100;
    
    
    [SerializeField, Tooltip("현재 HP 값. 0~Max 범위로 자동 보정됩니다")] 
    protected int currentHp = 100;

    
    [Tooltip("HP나 최대 HP가 변경될 때 호출됩니다. 인자: (현재 HP, 최대 HP)")]
    public UnityEvent<int,int> onChanged;
    
    
    [Tooltip("현재 HP가 0이 되었을 때 호출됩니다 (사망)")]
    public UnityEvent onDied;
    
    // 스크린샷: On Damaged (Int32)
    [Tooltip("데미지를 받았을 때 호출됩니다. 인자: 실제로 감소한 HP 양")]
    public UnityEvent<int> onDamaged; // amount
    
    
    [Tooltip("HP가 회복되었을 때 호출됩니다. 인자: 실제로 증가한 HP 양")]
    public UnityEvent<int> onHealed;  // amount

    
    public int Max => maxHp;
    public int Current => currentHp;
    public bool IsDead => currentHp <= 0;

    protected virtual void Awake()
    {
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        onChanged?.Invoke(currentHp, maxHp);
    }

    public virtual void SetMax(int value, bool fill = true)
    {
        maxHp = Mathf.Max(1, value);
        if (fill) currentHp = maxHp;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        onChanged?.Invoke(currentHp, maxHp);
    }

    public virtual void Damage(int amount)
    {
        if (IsDead) return;
        int prev = currentHp;
        currentHp = Mathf.Max(0, currentHp - Mathf.Max(0, amount));
        int delta = prev - currentHp;
        if (delta > 0) onDamaged?.Invoke(delta);
        onChanged?.Invoke(currentHp, maxHp);
        if (currentHp == 0) onDied?.Invoke();
    }

    public virtual void Heal(int amount)
    {
        if (IsDead) return;
        int prev = currentHp;
        currentHp = Mathf.Min(maxHp, currentHp + Mathf.Max(0, amount));
        int delta = currentHp - prev;
        if (delta > 0) onHealed?.Invoke(delta);
        onChanged?.Invoke(currentHp, maxHp);
    }

    public virtual void Revive(int hp)
    {
        currentHp = Mathf.Clamp(hp, 1, maxHp);
        onChanged?.Invoke(currentHp, maxHp);
    }
}