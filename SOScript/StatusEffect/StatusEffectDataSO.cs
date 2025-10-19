using UnityEngine;

/// <summary>
/// 상태효과 데이터 ScriptableObject
/// 독, 화상, 둔화, 기절 등 모든 상태효과의 기본 데이터를 담습니다.
/// </summary>
[CreateAssetMenu(fileName = "StatusEffect_", menuName = "StatusEffect/Status Effect Data")]
public class StatusEffectDataSO : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("상태효과 고유 ID (예: poison_lv1, burn_fire)")]
    public string effectID;
    
    [Tooltip("상태효과 표시 이름 (UI용)")]
    public string effectName;
    
    [Tooltip("상태효과 설명")]
    [TextArea(2, 4)]
    public string description;
    
    [Tooltip("상태효과 아이콘 (UI용)")]
    public Sprite icon;

    [Header("효과 타입")]
    [Tooltip("상태효과 종류 (DOT, HOT, 버프, 디버프 등)")]
    public StatusEffectType effectType;
    
    [Header("지속 시간")]
    [Tooltip("효과 지속 시간 (초). 0이면 영구")]
    public float duration;
    
    [Header("틱(Tick) 데이터 - DOT/HOT용")]
    [Tooltip("틱당 데미지/힐량 (effectType이 Damage/Heal일 때)")]
    public float tickValue;
    
    [Tooltip("틱 주기 (초). 0.5초마다 데미지를 준다면 0.5")]
    public float tickInterval = 1f;
    
    [Header("이동속도 변경 - 둔화/가속용")]
    [Tooltip("이동속도 배율 (1 = 100%, 0.5 = 50% 둔화, 1.5 = 150% 가속)")]
    [Range(0f, 3f)]
    public float moveSpeedMultiplier = 1f;
    
    [Header("행동 제약")]
    [Tooltip("이동 불가 여부 (기절, 빙결 등)")]
    public bool preventMovement = false;
    
    [Tooltip("공격 불가 여부")]
    public bool preventAttack = false;
    
    [Tooltip("스킬 사용 불가 여부")]
    public bool preventSkill = false;
    
    [Header("스택 설정")]
    [Tooltip("중복 적용 가능 여부")]
    public bool canStack = false;
    
    [Tooltip("최대 스택 수 (canStack이 true일 때)")]
    public int maxStack = 1;
    
    [Header("비주얼 효과")]
    [Tooltip("효과 적용 시 생성할 이펙트 프리팹")]
    public GameObject effectPrefab;
    
    [Tooltip("효과 적용 시 재생할 사운드")]
    public AudioClip effectSound;
}

