using UnityEngine;

/// <summary>
/// 몬스터의 데이터를 관리하는 컴포넌트
/// MonsterData ScriptableObject를 참조하여 몬스터의 스탯을 제공합니다.
/// </summary>
public class MonsterDataComponent : MonoBehaviour
{
    [SerializeField] private MonsterData monsterData;

    public MonsterData Data => monsterData;

    /// <summary>
    /// 몬스터 데이터를 설정합니다.
    /// </summary>
    /// <param name="data">설정할 MonsterData ScriptableObject</param>
    public void SetMonsterData(MonsterData data)
    {
        monsterData = data;
    }
}
