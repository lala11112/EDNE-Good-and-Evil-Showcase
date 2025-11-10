using UnityEngine;

public enum EntityType
{
    Player,
    Monster,
    Environment,
    Interactable
}

public enum StatusEffectType
{
    Damage,      // DOT (독, 화상)
    Heal,        // HOT (재생)
    Slow,        // 둔화
    Stun,        // 기절
    Buff,        // 버프
    Debuff       // 디버프
}



/// <summary>
/// 용기 상태 (Courage State)
/// </summary>
public enum CourageState
{
    Despair,      // 절망 (0 이하)
    Anxiety,      // 불안
    Doubt,        // 의심
    Courage,      // 용기
    Determination,// 결의
    Confidence    // 자신감
}

/// <summary>
/// 스킬 랭크 (Skill Rank)
/// </summary>
public enum SkillRank
{
    D,  // 최하 랭크
    C,
    B,
    A,
    S   // 최상 랭크
}

/// <summary>
/// 스킬 슬롯 타입 (Skill Slot Type)
/// </summary>
public enum SkillSlotType
{
    Z,  // 스킬 Z (Skill1)
    X,  // 스킬 X (Skill2)
    C,  // 스킬 C (Skill3)
    V   // 스킬 V (Skill4, 궁극기)
}

/// <summary>
/// 데미지 소스 타입 (Damage Source Type)
/// </summary>
public enum DamageSourceType
{
    Projectile,   // 탄막
    Contact,      // 접촉
    Environment   // 환경
}


public enum SkillType
{
    Courage,    // 용기 (Z)
    Justice,    // 정의 (X)
    Temperance, // 절제 (C)
    Wisdom      // 지혜 (V)
}

/// <summary>
/// 이동 방향 (8방향)
/// </summary>
public enum MoveDirection
{
    None,       // 정지
    Up,         // 위
    Down,       // 아래
    Left,       // 왼쪽
    Right,      // 오른쪽
    UpLeft,     // 왼쪽 위 (대각선)
    UpRight,    // 오른쪽 위 (대각선)
    DownLeft,   // 왼쪽 아래 (대각선)
    DownRight   // 오른쪽 아래 (대각선)
}

/// <summary>
/// 아이템 타입 (Item Type)
/// </summary>
public enum ItemType
{
    Consumable, // 소비 아이템 (즉시 효과 적용, 인벤토리 등록 안 함)
    Equipment   // 장비 아이템 (지속 효과 적용, 인벤토리에 등록)
}