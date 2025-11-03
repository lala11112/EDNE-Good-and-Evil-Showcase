using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// 적의 죽음을 감지하고 이벤트를 발생시키는 시스템
/// 적의 컴포넌트에 추가하여 사용
/// </summary>
public class EnemyDeathEventSystem : MonoBehaviour
{
    #region Events - 이벤트 정의
    
    /// <summary>
    /// 적이 죽었을 때 발생하는 이벤트
    /// </summary>
    public static event Action<GameObject, EnemyDeathEventSystem> OnEnemyDeath;
    
    /// <summary>
    /// 특정 적이 죽었을 때 발생하는 이벤트 (인스턴스별)
    /// </summary>
    public event Action<GameObject> OnThisEnemyDeath;
    
    #endregion
    
    #region Inspector Fields - 인스펙터 필드
    
    [Header("죽음 감지 설정")]
    [SerializeField] private bool enableDeathDetection = true;
    [SerializeField] private float deathCheckInterval = 0.1f;
    
    [Header("프리팹 실행 설정")]
    [SerializeField] private List<GameObject> deathPrefabs = new List<GameObject>();
    [SerializeField] private bool executePrefabsOnDeath = true;
    [SerializeField] private float prefabExecutionDelay = 0f; // 죽은 후 몇 초 뒤에 실행할지
    
    #endregion
    
    #region Private Variables - 프라이빗 변수
    
    private bool isDead = false;
    private float lastHealthCheck = 0f;
    private EntityController entityController;
    private MonsterController monsterController;
    private Vector3 deathPosition; // 죽은 위치 저장
    
    #endregion
    
    #region Properties - 프로퍼티
    
    /// <summary>
    /// 이 적이 죽었는지 여부
    /// </summary>
    public bool IsDead => isDead;
    
    /// <summary>
    /// 등록된 죽음 프리팹 개수
    /// </summary>
    public int DeathPrefabCount => deathPrefabs.Count;
    
    #endregion
    
    #region Unity Lifecycle - 유니티 생명주기
    
    private void Awake()
    {
        // EntityController 컴포넌트 찾기
        entityController = GetComponent<EntityController>();
        if (entityController == null)
        {
            Debug.LogError($"[EnemyDeathEventSystem] {gameObject.name}에 EntityController가 없습니다!");
            enabled = false;
            return;
        }
        
        // MonsterController 컴포넌트 찾기 (분열 몬스터 등록용)
        monsterController = GetComponent<MonsterController>();
    }
    
    private void Start()
    {
        // 초기 상태 설정
        isDead = false;
    }
    
    private void Update()
    {
        if (!enableDeathDetection || isDead) return;
        
        // 주기적으로 체력 확인
        if (Time.time - lastHealthCheck >= deathCheckInterval)
        {
            CheckForDeath();
            lastHealthCheck = Time.time;
        }
    }
    
    private void OnDestroy()
    {
        // 정리 작업 (필요시)
    }
    
    #endregion
    
    #region Death Detection - 죽음 감지
    
    /// <summary>
    /// 죽음 상태 확인
    /// </summary>
    private void CheckForDeath()
    {
        if (entityController != null && !entityController.IsAlive && !isDead)
        {
            TriggerDeath();
        }
    }
    
    /// <summary>
    /// 죽음 이벤트 발생 및 처리
    /// </summary>
    private void TriggerDeath()
    {
        if (isDead) return; // 중복 실행 방지
        
        isDead = true;
        
        // 죽은 위치 저장
        deathPosition = transform.position;
        
        Debug.Log($"[EnemyDeathEventSystem] {gameObject.name}의 죽음이 감지되었습니다!");
        
        // 전역 이벤트 발생
        OnEnemyDeath?.Invoke(gameObject, this);
        
        // 인스턴스 이벤트 발생
        OnThisEnemyDeath?.Invoke(gameObject);
        
        // 등록된 프리팹 실행
        if (executePrefabsOnDeath)
        {
            if (prefabExecutionDelay > 0f)
            {
                StartCoroutine(ExecuteDeathPrefabsWithDelay());
            }
            else
            {
                ExecuteDeathPrefabs();
            }
        }
    }
    
    #endregion
    
    
    #region Prefab Execution - 프리팹 실행
    
    /// <summary>
    /// 등록된 죽음 프리팹들 실행
    /// </summary>
    private void ExecuteDeathPrefabs()
    {
        if (deathPrefabs.Count == 0)
        {
            Debug.Log($"[EnemyDeathEventSystem] {gameObject.name}에 등록된 죽음 프리팹이 없습니다.");
            return;
        }
        
        for (int i = 0; i < deathPrefabs.Count; i++)
        {
            if (deathPrefabs[i] != null)
            {
                ExecutePrefab(deathPrefabs[i]);
            }
        }
    }
    
    /// <summary>
    /// 지연 후 죽음 프리팹들 실행
    /// </summary>
    private System.Collections.IEnumerator ExecuteDeathPrefabsWithDelay()
    {
        yield return new WaitForSeconds(prefabExecutionDelay);
        
        // 몬스터가 파괴되었을 수 있으므로 저장된 위치에서 실행
        ExecuteDeathPrefabsAtPosition(deathPosition);
    }
    
    /// <summary>
    /// 저장된 위치에서 죽음 프리팹들 실행
    /// </summary>
    private void ExecuteDeathPrefabsAtPosition(Vector3 position)
    {
        if (deathPrefabs.Count == 0)
        {
            Debug.Log($"[EnemyDeathEventSystem] {gameObject.name}에 등록된 죽음 프리팹이 없습니다.");
            return;
        }
        
        for (int i = 0; i < deathPrefabs.Count; i++)
        {
            if (deathPrefabs[i] != null)
            {
                ExecutePrefabAtPosition(deathPrefabs[i], position);
            }
        }
    }
    
    /// <summary>
    /// 개별 프리팹 실행
    /// </summary>
    private void ExecutePrefab(GameObject prefab)
    {
        ExecutePrefabAtPosition(prefab, transform.position);
    }
    
    /// <summary>
    /// 지정된 위치에서 개별 프리팹 실행
    /// </summary>
    private void ExecutePrefabAtPosition(GameObject prefab, Vector3 position)
    {
        try
        {
            Debug.Log($"[EnemyDeathEventSystem] 프리팹 실행 시작 - {prefab.name} at {position}");
            
            // 프리팹을 지정된 위치에 인스턴스화
            GameObject instance = Instantiate(prefab, position, Quaternion.identity);
            Debug.Log($"[EnemyDeathEventSystem] 인스턴스 생성 완료 - {instance.name}");
            
            // IEnemyDeathScript를 구현한 모든 컴포넌트 찾아서 실행
            IEnemyDeathScript[] deathScripts = instance.GetComponentsInChildren<IEnemyDeathScript>();
            Debug.Log($"[EnemyDeathEventSystem] IEnemyDeathScript 컴포넌트 {deathScripts.Length}개 발견");
            
            foreach (var deathScript in deathScripts)
            {
                Debug.Log($"[EnemyDeathEventSystem] IEnemyDeathScript 실행: {deathScript.GetType().Name}");
                deathScript.OnEnemyDeath(gameObject);
            }
            
            // 분열 몬스터 처리: 생성된 몬스터를 RoomController에 자동 등록
            Debug.Log($"[EnemyDeathEventSystem] RegisterSpawnedMonsters 호출 전");
            RegisterSpawnedMonsters(instance);
            Debug.Log($"[EnemyDeathEventSystem] RegisterSpawnedMonsters 호출 후");
            
            Debug.Log($"[EnemyDeathEventSystem] {prefab.name} 프리팹이 {position} 위치에서 실행되었습니다.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnemyDeathEventSystem] {prefab.name} 프리팹 실행 중 오류 발생: {e.Message}\n{e.StackTrace}");
        }
    }
    
    /// <summary>
    /// 생성된 몬스터들을 RoomController에 등록
    /// (분열 몬스터 등)
    /// </summary>
    private void RegisterSpawnedMonsters(GameObject spawnedObject)
    {
        Debug.Log($"[EnemyDeathEventSystem] RegisterSpawnedMonsters 호출됨 - 스폰된 오브젝트: {spawnedObject?.name ?? "null"}");
        
        // 부모 몬스터의 RoomController 가져오기
        if (monsterController == null)
        {
            Debug.LogWarning($"[EnemyDeathEventSystem] monsterController가 null입니다!");
            return;
        }
        
        RoomController roomController = monsterController.GetRoomController();
        if (roomController == null)
        {
            Debug.LogWarning($"[EnemyDeathEventSystem] {gameObject.name}의 RoomController를 찾을 수 없습니다.");
            return;
        }
        
        Debug.Log($"[EnemyDeathEventSystem] RoomController 찾음: {roomController.name}");
        
        // 생성된 오브젝트와 자식들에서 MonsterController 찾기
        MonsterController[] spawnedMonsters = spawnedObject.GetComponentsInChildren<MonsterController>();
        
        Debug.Log($"[EnemyDeathEventSystem] 분열 몬스터 {spawnedMonsters.Length}마리 발견 (오브젝트: {spawnedObject.name})");
        
        foreach (var spawnedMonster in spawnedMonsters)
        {
            if (spawnedMonster != null)
            {
                // RoomController 설정
                spawnedMonster.SetRoomController(roomController);
                
                // RoomController에 등록
                roomController.RegisterMonster(spawnedMonster);
                
                // ⭐ 몬스터 죽음 이벤트 구독 (중요!)
                spawnedMonster.OnMonsterDied += roomController.UnregisterMonster;
                Debug.Log($"[EnemyDeathEventSystem] 분열 몬스터 {spawnedMonster.name}의 죽음 이벤트 구독 완료 (구독자 수: {spawnedMonster.OnMonsterDied?.GetInvocationList()?.Length ?? 0})");
                
                Debug.Log($"[EnemyDeathEventSystem] 분열 몬스터 {spawnedMonster.name}를 RoomController에 등록했습니다. 현재 총 몬스터 수: {roomController.GetCurrentMonsterCount()}마리");
            }
        }
    }
    
    #endregion
    
    #region Public Methods - 퍼블릭 메서드
    
    /// <summary>
    /// 죽음 프리팹 추가
    /// </summary>
    public void AddDeathPrefab(GameObject prefab)
    {
        if (prefab != null && !deathPrefabs.Contains(prefab))
        {
            deathPrefabs.Add(prefab);
            Debug.Log($"[EnemyDeathEventSystem] {prefab.name} 프리팹이 추가되었습니다.");
        }
    }
    
    /// <summary>
    /// 죽음 프리팹 제거
    /// </summary>
    public void RemoveDeathPrefab(GameObject prefab)
    {
        if (deathPrefabs.Remove(prefab))
        {
            Debug.Log($"[EnemyDeathEventSystem] {prefab.name} 프리팹이 제거되었습니다.");
        }
    }
    
    /// <summary>
    /// 모든 죽음 프리팹 제거
    /// </summary>
    public void ClearDeathPrefabs()
    {
        deathPrefabs.Clear();
        Debug.Log($"[EnemyDeathEventSystem] 모든 죽음 프리팹이 제거되었습니다.");
    }
    
    /// <summary>
    /// 죽음 감지 수동 트리거 (테스트용)
    /// </summary>
    [ContextMenu("수동 죽음 트리거")]
    public void ManualDeathTrigger()
    {
        if (!isDead)
        {
            TriggerDeath();
        }
    }
    
    #endregion
}
