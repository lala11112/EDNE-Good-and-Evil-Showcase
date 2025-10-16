using UnityEngine;

/// <summary>
/// 적의 죽음에 반응하는 스크립트들이 구현해야 하는 인터페이스
/// </summary>
public interface IEnemyDeathScript
{
    /// <summary>
    /// 적이 죽었을 때 호출되는 메서드
    /// </summary>
    /// <param name="deadEnemy">죽은 적의 GameObject</param>
    void OnEnemyDeath(GameObject deadEnemy);
}
