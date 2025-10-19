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



public enum CourageState
{
    Despair, anxiety, doubt, courage, determination, confidence
}

public enum PlayerSkillRank
{
    D, C, B, A, S
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