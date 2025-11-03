using UnityEngine;

/// <summary>
/// 적이 죽었을 때 즉시 십자가(상하좌우) 방향으로 분열 스폰
/// EnemyDeathEventSystem의 Death Prefabs 리스트에 이 스크립트가 붙은 프리팹을 등록해서 사용
/// </summary>
public class SplitOnDeathScript : MonoBehaviour, IEnemyDeathScript
{
    [Header("분열 스폰 설정")]
    [SerializeField] private GameObject horizontalEnemyPrefab; // 좌/우에 스폰할 프리팹
    [SerializeField] private GameObject verticalEnemyPrefab;   // 상/하에 스폰할 프리팹
    [SerializeField] private float spawnDistance = 1.5f;       // 중심으로부터 거리

    [Header("추가 설정")]
    [SerializeField] private bool alignZToZero = true;         // 2D용 Z=0 강제
    [SerializeField] private bool setTargetToPlayer = true;    // 스폰 시 플레이어를 바로 타겟으로 설정

    /// <summary>
    /// EnemyDeathEventSystem이 호출
    /// </summary>
    public void OnEnemyDeath(GameObject deadEnemy)
    {
        Vector3 center = deadEnemy != null ? deadEnemy.transform.position : transform.position;
        if (alignZToZero) center.z = 0f;

        // 부모 몬스터의 RoomController 가져오기
        RoomController roomController = null;
        if (deadEnemy != null)
        {
            var deadMonsterController = deadEnemy.GetComponent<MonsterController>();
            if (deadMonsterController != null)
            {
                roomController = deadMonsterController.GetRoomController();
                Debug.Log($"[SplitOnDeathScript] 부모 몬스터 {deadEnemy.name}의 RoomController 찾음: {roomController?.name ?? "null"}");
            }
        }

        // 좌/우 스폰
        SpawnOne(horizontalEnemyPrefab, center + Vector3.left * spawnDistance, roomController);
        SpawnOne(horizontalEnemyPrefab, center + Vector3.right * spawnDistance, roomController);

        // 상/하 스폰
        SpawnOne(verticalEnemyPrefab, center + Vector3.up * spawnDistance, roomController);
        SpawnOne(verticalEnemyPrefab, center + Vector3.down * spawnDistance, roomController);

        // 작업 끝났으면 자신 정리 (이 프리팹은 로직 전용)
        Destroy(gameObject);
    }

    private void SpawnOne(GameObject prefab, Vector3 position, RoomController roomController)
    {
        if (prefab == null) return;

        if (alignZToZero) position.z = 0f;
        GameObject spawned = Instantiate(prefab, position, Quaternion.identity);
        
        Debug.Log($"[SplitOnDeathScript] 분열 몬스터 생성: {spawned.name} at {position}");

        var mc = spawned.GetComponent<MonsterController>();
        if (mc != null)
        {
            // ⭐ RoomController 설정 및 등록 (중요!)
            if (roomController != null)
            {
                // RoomController 설정
                mc.SetRoomController(roomController);
                
                // RoomController에 등록
                roomController.RegisterMonster(mc);
                
                // ⭐ 몬스터 죽음 이벤트 구독 (중요!)
                mc.OnMonsterDied += roomController.UnregisterMonster;
                Debug.Log($"[SplitOnDeathScript] 분열 몬스터 {mc.name} 등록 완료 (구독자 수: {mc.OnMonsterDied?.GetInvocationList()?.Length ?? 0}, 현재 총 몬스터 수: {roomController.GetCurrentMonsterCount()}마리)");
            }
            else
            {
                Debug.LogWarning($"[SplitOnDeathScript] RoomController가 없어서 {mc.name}을 등록할 수 없습니다!");
            }
            
            // 플레이어 타겟 설정
            if (setTargetToPlayer && PlayerController.Instance != null)
            {
                // MonsterController는 Start에서 자동 타겟 찾도록 설계되어 있을 수 있으나, 바로 지정하면 반응이 더 빠름
                // 안전하게 Transform만 설정 (노출된 API가 없다면 직접 필드 접근은 하지 않음)
                // Target 설정용 공개 메서드가 없다면, Start에서 autoFindPlayer에 의존하도록 둡니다.
            }
        }
        else
        {
            Debug.LogWarning($"[SplitOnDeathScript] {spawned.name}에 MonsterController가 없습니다!");
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 에디터에서 예상 스폰 위치 미리보기
        Gizmos.color = Color.cyan;
        Vector3 c = transform.position;
        if (alignZToZero) c.z = 0f;
        Gizmos.DrawWireSphere(c + Vector3.left * spawnDistance, 0.15f);
        Gizmos.DrawWireSphere(c + Vector3.right * spawnDistance, 0.15f);
        Gizmos.DrawWireSphere(c + Vector3.up * spawnDistance, 0.15f);
        Gizmos.DrawWireSphere(c + Vector3.down * spawnDistance, 0.15f);
    }
}


