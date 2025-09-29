using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EntityDataSO", menuName = "Entity/Entity Data")]

public class EntityDataSO : ScriptableObject
{
    [Header("기본 정보")]
    public string uniqueID;           // 고유 ID (겹치면 안됨)
    public string entityName;
    public EntityType entityType;    // Player, Monster, Environment
    
    [Header("체력 시스템")]
    public int maxHealth;
    public int currentHealth;
    
    [Header("상태 효과")]
    public List<StatusEffect> statusEffects = new List<StatusEffect>();
    
    
}

public enum EntityType
{
    Player,
    Monster, 
    Environment
}

[System.Serializable]
public class StatusEffect
{
    public string effectID;
    public string effectName;
    public float duration;
    public float value;
    public StatusEffectType type;
}

public enum StatusEffectType
{
    Buff,    // 버프
    Debuff,  // 디버프
    Damage,  // 지속 데미지
    Heal     // 지속 힐
}