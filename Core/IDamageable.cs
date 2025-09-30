using UnityEngine;

/// <summary>
/// 데미지를 받을 수 있는 모든 객체가 구현해야 하는 인터페이스
/// 몬스터, 플레이어, 구조물 등 다양한 타입의 객체가 데미지를 받을 수 있도록 합니다.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// 객체가 살아있는지 여부를 반환합니다.
    /// </summary>
    bool IsAlive { get; }
    
    /// <summary>
    /// 객체에게 데미지를 입힙니다.
    /// </summary>
    /// <param name="damage">입힐 데미지 양</param>
    void TakeDamage(int damage);
    
    /// <summary>
    /// 데미지를 받을 수 있는 위치를 반환합니다.
    /// </summary>
    Transform Transform { get; }
}
