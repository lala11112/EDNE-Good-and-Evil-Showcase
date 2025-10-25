using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 방 컨트롤러
/// 방의 웨이브, 몬스터 스폰, 클리어 조건을 관리합니다.
/// </summary>
public class RoomController : MonoBehaviour
{
    [Header("방 설정")]
    [Tooltip("방 컨트롤러 데이터 (ScriptableObject)")]
    [SerializeField] private RoomControllerSO roomData;
    
    [Tooltip("방 콜라이더 (타일맵 콜라이더)")]
    [SerializeField] private Collider2D roomCollider;
    
    [Tooltip("방과 연결된 문들")]
    [SerializeField] private List<DoorController> connectedDoors = new List<DoorController>();

    [Header("몬스터 스포너")]
    [Tooltip("방의 몬스터 스포너들")]
    [SerializeField] private List<MonsterSpawner> monsterSpawners = new List<MonsterSpawner>();

    [Header("현재 상태")]
    [Tooltip("현재 웨이브 번호")]
    [SerializeField] private int currentWave = 0;
    
    [Tooltip("현재 방에 있는 몬스터들")]
    [SerializeField] private List<MonsterController> currentMonsters = new List<MonsterController>();
    
    [Tooltip("방 클리어 여부")]
    [SerializeField] private bool isRoomCleared = false;
    
    [Tooltip("방 활성화 여부")]
    [SerializeField] private bool isRoomActive = false;

    [Tooltip("방 들어가봤는지 여부")]
    [SerializeField] private bool isRoomFirstEntered= false;

    // 웨이브 관련 변수
    private bool isWaveInProgress = false;
    private float nextWaveTimer = 0f;
    private int remainingMonstersForNextWave = 0;
    private Coroutine waveCoroutine;
    
    // 디버그 관련 변수
    private float debugTimer = 0f;
    private const float DEBUG_INTERVAL = 10f; // 10초마다

    // 이벤트
    public System.Action<RoomController> OnRoomEntered;
    public System.Action<RoomController> OnRoomCleared;
    public System.Action<RoomController, int> OnWaveStarted;
    public System.Action<RoomController, int> OnWaveCompleted;

    /// <summary>
    /// 방 초기화
    /// </summary>
    private void Awake()
    {
        InitializeRoom();
    }

    /// <summary>
    /// 매 프레임 업데이트
    /// </summary>
    private void Update()
    {
        
        // 방이 활성화되어 있을 때만 디버그 타이머 실행
        if (isRoomActive)
        {
            debugTimer += Time.deltaTime;
            
            // 10초마다 몬스터 수 표시
            if (debugTimer >= DEBUG_INTERVAL)
            {
                debugTimer = 0f;
                DebugMonsterCount();
            }
        }
    }

    /// <summary>
    /// 방 초기화
    /// </summary>
    private void InitializeRoom()
    {
        if (roomData == null)
        {
            Debug.LogError($"[RoomController] {gameObject.name}에 RoomControllerSO가 설정되지 않았습니다!");
            return;
        }

        // 방 콜라이더 설정
        if (roomCollider == null)
        {
            roomCollider = GetComponent<Collider2D>();
        }

        // 몬스터 스포너 자동 찾기
        if (monsterSpawners.Count == 0)
        {
            FindMonsterSpawners();
        }

        // 초기 설정
        isRoomActive = false;
        isRoomCleared = false;
        currentWave = 0;

        Debug.Log($"[RoomController] {roomData.roomName} 초기화 완료");
    }

    /// <summary>
    /// 몬스터 스포너 자동 찾기
    /// </summary>
    private void FindMonsterSpawners()
    {
        MonsterSpawner[] spawners = GetComponentsInChildren<MonsterSpawner>();
        monsterSpawners.AddRange(spawners);
        
        // 각 스포너에 RoomController 설정
        foreach (var spawner in spawners)
        {
            if (spawner != null)
            {
                spawner.SetRoomController(this);
                Debug.Log($"[RoomController] 스포너 {spawner.spawnerName}에 RoomController 설정 완료");
            }
        }
        
        Debug.Log($"[RoomController] {monsterSpawners.Count}개의 몬스터 스포너를 찾았습니다.");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 방에 한번이라도 들어가면 EnterRoom은 다시 실행되지않게
        if(isRoomFirstEntered) return;
        PlayerController playerController = other.GetComponent<PlayerController>();
        if(playerController != null){
            EnterRoom();
        }
    }

    /// <summary>
    /// 방 진입 처리
    /// </summary>
    public void EnterRoom()
    {
        if (isRoomActive) return;

        isRoomFirstEntered = true;
        isRoomActive = true;
        isRoomCleared = false;
        currentWave = 0;

        // 문 닫기
        CloseDoors();

        // 첫 번째 웨이브 시작
        StartCoroutine(StartFirstWave());

        OnRoomEntered?.Invoke(this);
        Debug.Log($"[RoomController] {roomData.roomName}에 진입했습니다.");
    }

    /// <summary>
    /// 첫 번째 웨이브 시작
    /// </summary>
    private IEnumerator StartFirstWave()
    {
        yield return new WaitForSeconds(roomData.spawnWarningTime);
        StartWave(0);
    }

    /// <summary>
    /// 웨이브 시작
    /// </summary>
    private void StartWave(int waveIndex)
    {
        Debug.Log("웨이브 인덱스"+waveIndex);
        if (waveIndex >= roomData.waveDataList.Count)
        {
            Debug.LogError($"[RoomController] 웨이브 인덱스 {waveIndex}가 범위를 벗어났습니다.");
            return;
        }

        currentWave = waveIndex;
        WaveData waveData = roomData.waveDataList[waveIndex];
        isWaveInProgress = true;

        Debug.Log($"[RoomController] 웨이브 {waveData.waveNumber} 시작: {waveData.waveName}");
        OnWaveStarted?.Invoke(this, currentWave);

        // 웨이브 시작 코루틴 시작
        StartCoroutine(StartWaveCoroutine(waveData));
    }

    /// <summary>
    /// 웨이브 시작 코루틴
    /// </summary>
    private IEnumerator StartWaveCoroutine(WaveData waveData)
    {
        // 웨이브 시작 대기
        yield return new WaitForSeconds(waveData.waveStartDelay);

        // 몬스터 스폰
        yield return StartCoroutine(SpawnWaveMonsters(waveData));
    }

    /// <summary>
    /// 웨이브 몬스터 스폰
    /// </summary>
    private IEnumerator SpawnWaveMonsters(WaveData waveData)
    {
        foreach (var monsterData in waveData.monsterSpawnData)
        {
            // 스포너 선택
            MonsterSpawner selectedSpawner = SelectSpawner(monsterData);
            if (selectedSpawner == null)
            {
                Debug.LogWarning($"[RoomController] 사용 가능한 스포너가 없습니다.");
                continue;
            }

            // 몬스터 스폰 해당 기능은 몬스터 스폰카운트가 많아도 적용되지않을텐데 이부분 나중에 수정할예정입니다.
            for (int i = 0; i < monsterData.spawnCount; i++)
            {
                selectedSpawner.StartSpawning(monsterData, roomData.spawnWarningTime);
                yield return new WaitForSeconds(waveData.spawnInterval);
            }
        }

        // 웨이브 완료 대기
        StartCoroutine(WaitForWaveCompletion(waveData));
    }

    /// <summary>
    /// 스포너 선택
    /// </summary>
    private MonsterSpawner SelectSpawner(MonsterSpawnData monsterData)
    {
        if (monsterData.useSpecificSpawner)
        {
            // 특정 스포너 사용
            if (monsterData.spawnerIndex < monsterSpawners.Count)
            {
                return monsterSpawners[monsterData.spawnerIndex];
            }
        }
        else
        {
            // 활성화된 스포너 중 랜덤 선택
            List<MonsterSpawner> activeSpawners = new List<MonsterSpawner>();
            foreach (var spawner in monsterSpawners)
            {
                if (spawner.isActive && !spawner.IsSpawning())
                {
                    activeSpawners.Add(spawner);
                }
            }

            if (activeSpawners.Count > 0)
            {
                return activeSpawners[Random.Range(0, activeSpawners.Count)];
            }
        }

        return null;
    }

    /// <summary>
    /// 웨이브 완료 대기
    /// </summary>
    private IEnumerator WaitForWaveCompletion(WaveData waveData)
    {
        // 몬스터 처치 대기
        while (currentMonsters.Count > roomData.remainingMonstersForNextWave)
        {
            yield return null;
        }

        // 다음 웨이브 카운트 시작
        StartCoroutine(NextWaveCountdown(waveData));
    }

    /// <summary>
    /// 다음 웨이브 카운트다운
    /// </summary>
    private IEnumerator NextWaveCountdown(WaveData currentWaveData)
    {
        nextWaveTimer = roomData.nextWaveDelay;
        remainingMonstersForNextWave = roomData.remainingMonstersForNextWave;

        Debug.Log($"[RoomController] 다음 웨이브까지 {nextWaveTimer}초 대기 시작");

        while (nextWaveTimer > 0f)
        {
            nextWaveTimer -= Time.deltaTime;

            // 몬스터 처치 시 카운트 감소
            if (currentMonsters.Count < remainingMonstersForNextWave)
            {
                nextWaveTimer -= roomData.killCountReduction;
                remainingMonstersForNextWave = currentMonsters.Count;
            }

            yield return null;
        }

        // 웨이브 완료
        CompleteWave(currentWaveData);
    }

    /// <summary>
    /// 웨이브 완료
    /// </summary>
    private void CompleteWave(WaveData waveData)
    {
        isWaveInProgress = false;
        OnWaveCompleted?.Invoke(this, currentWave);

        Debug.Log($"[RoomController] 웨이브 {waveData.waveNumber} 완료");

        // 마지막 웨이브인지 확인
        if (waveData.isLastWave)
        {
            StartCoroutine(WaitForRoomClear());
        }
        else
        {
            // 다음 웨이브 시작
            StartWave(currentWave + 1);
        }
    }

    /// <summary>
    /// 방 클리어 대기
    /// </summary>
    private IEnumerator WaitForRoomClear()
    {
        // 모든 몬스터 처치 대기
        while (currentMonsters.Count > 0)
        {
            yield return null;
        }

        // 방 클리어
        ClearRoom();
    }

    /// <summary>
    /// 방 클리어
    /// </summary>
    private void ClearRoom()
    {
        isRoomCleared = true;
        isRoomActive = false;

        // 문 열기
        OpenDoors();

        // 보상 지급
        GiveRewards();

        OnRoomCleared?.Invoke(this);
        Debug.Log($"[RoomController] {roomData.roomName} 클리어!");
    }

    /// <summary>
    /// 몬스터 등록
    /// </summary>
    public void RegisterMonster(MonsterController monster)
    {
        if (monster == null)
        {
            Debug.LogError($"[RoomController] null 몬스터를 등록하려고 시도했습니다.");
            return;
        }

        if (!currentMonsters.Contains(monster))
        {
            currentMonsters.Add(monster);
            Debug.Log($"[RoomController] 몬스터 등록: {monster.name} (총 {currentMonsters.Count}마리)");
            
            // 등록된 몬스터 목록 출력
            Debug.Log($"[RoomController] 현재 등록된 몬스터 목록:");
            for (int i = 0; i < currentMonsters.Count; i++)
            {
                Debug.Log($"  {i + 1}. {currentMonsters[i]?.name ?? "null"}");
            }
        }
        else
        {
            Debug.LogWarning($"[RoomController] 몬스터 {monster.name}이 이미 등록되어 있습니다. (현재 {currentMonsters.Count}마리)");
        }
    }

    /// <summary>
    /// 몬스터 제거
    /// </summary>
    public void UnregisterMonster(MonsterController monster)
    {
        Debug.Log($"[RoomController] UnregisterMonster 호출됨 - 몬스터: {monster?.name ?? "null"}");
        
        if (monster == null)
        {
            Debug.LogWarning($"[RoomController] 제거하려는 몬스터가 null입니다.");
            return;
        }
        
        if (currentMonsters.Contains(monster))
        {
            currentMonsters.Remove(monster);
            Debug.Log($"[RoomController] 몬스터 제거 성공: {monster.name} (남은 {currentMonsters.Count}마리)");
            
            // 제거 후 몬스터 목록 출력
            Debug.Log($"[RoomController] 제거 후 등록된 몬스터 목록:");
            for (int i = 0; i < currentMonsters.Count; i++)
            {
                Debug.Log($"  {i + 1}. {currentMonsters[i]?.name ?? "null"}");
            }
        }
        else
        {
            Debug.LogWarning($"[RoomController] 몬스터 {monster.name}이 등록 목록에 없습니다. (현재 등록된 몬스터 수: {currentMonsters.Count})");
            
            // 현재 등록된 몬스터 목록 출력
            Debug.Log($"[RoomController] 현재 등록된 몬스터 목록:");
            for (int i = 0; i < currentMonsters.Count; i++)
            {
                Debug.Log($"  {i + 1}. {currentMonsters[i]?.name ?? "null"}");
            }
        }
    }

    /// <summary>
    /// 문 닫기
    /// </summary>
    private void CloseDoors()
    {
        foreach (var door in connectedDoors)
        {
            if (door != null)
            {
                door.CloseDoor();
            }
        }
    }

    /// <summary>
    /// 문 열기
    /// </summary>
    private void OpenDoors()
    {
        foreach (var door in connectedDoors)
        {
            if (door != null)
            {
                door.OpenDoor();
            }
        }
    }

    /// <summary>
    /// 보상 지급
    /// </summary>
    private void GiveRewards()
    {
        foreach (var reward in roomData.clearRewards)
        {
            // TODO: 보상 시스템 구현
            Debug.Log($"[RoomController] 보상 지급: {reward.rewardType} x{reward.rewardAmount}");
        }
    }

    /// <summary>
    /// 방이 활성화되어 있는지 확인
    /// </summary>
    public bool IsRoomActive()
    {
        return isRoomActive;
    }

    /// <summary>
    /// 방이 클리어되었는지 확인
    /// </summary>
    public bool IsRoomCleared()
    {
        return isRoomCleared;
    }

    /// <summary>
    /// 현재 웨이브 번호
    /// </summary>
    public int GetCurrentWave()
    {
        return currentWave;
    }

    /// <summary>
    /// 현재 몬스터 수
    /// </summary>
    public int GetCurrentMonsterCount()
    {
        return currentMonsters.Count;
    }

    /// <summary>
    /// 몬스터 수 디버그 출력
    /// </summary>
    private void DebugMonsterCount()
    {
        string roomName = roomData != null ? roomData.roomName : "Unknown Room";
        string status = isRoomCleared ? "클리어됨" : (isRoomActive ? "진행중" : "비활성화");
        
        Debug.Log($"[RoomController] {roomName} - 상태: {status}, 현재 웨이브: {currentWave}, 남은 몬스터: {currentMonsters.Count}마리");
        
        // null 체크 및 정리
        int nullCount = 0;
        for (int i = currentMonsters.Count - 1; i >= 0; i--)
        {
            if (currentMonsters[i] == null)
            {
                currentMonsters.RemoveAt(i);
                nullCount++;
            }
        }
        
        if (nullCount > 0)
        {
            Debug.LogWarning($"[RoomController] {nullCount}개의 null 몬스터를 목록에서 제거했습니다. (현재 {currentMonsters.Count}마리)");
        }
        
        // 각 몬스터의 상세 정보도 표시
        if (currentMonsters.Count > 0)
        {
            Debug.Log($"[RoomController] 현재 등록된 몬스터 목록:");
            for (int i = 0; i < currentMonsters.Count; i++)
            {
                if (currentMonsters[i] != null)
                {
                    Debug.Log($"  {i + 1}. {currentMonsters[i].name} (HP: {currentMonsters[i].CurrentHealth}/{currentMonsters[i].MaxHealth})");
                }
                else
                {
                    Debug.LogError($"  {i + 1}. NULL 몬스터 발견!");
                }
            }
        }
        else
        {
            Debug.Log($"[RoomController] 등록된 몬스터가 없습니다.");
        }
    }
}