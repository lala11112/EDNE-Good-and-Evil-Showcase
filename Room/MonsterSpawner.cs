using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 몬스터 스포너
/// 방에서 몬스터를 스폰하는 지점을 관리합니다.
/// </summary>
public class MonsterSpawner : MonoBehaviour
{
    [Header("스포너 기본 설정")]
    [Tooltip("스포너 이름")]
    public string spawnerName = "Spawner";
    
    [Tooltip("스포너 인덱스 (RoomController에서 참조)")]
    public int spawnerIndex = 0;
    
    [Tooltip("스포너 활성화 여부")]
    public bool isActive = true;

    [Header("스폰 설정")]
    [Tooltip("스폰 반경")]
    [Range(0.5f, 5f)]
    public float spawnRadius = 1f;
    
    [Tooltip("스폰 높이 오프셋")]
    [Range(0f, 2f)]
    public float spawnHeightOffset = 0f;
    
    [Tooltip("스폰 시 랜덤 회전 적용")]
    public bool randomRotation = true;

    [Header("시각적 표시")]
    [Tooltip("스포너 표시 이펙트")]
    public GameObject spawnerIndicator;
    
    [Tooltip("스폰 대기 중 표시 이펙트")]
    public GameObject warningEffect;
    
    [Tooltip("스포너 표시 색상")]
    public Color spawnerColor = Color.red;

    [Header("디버그")]
    [Tooltip("스포너 표시")]
    public bool showGizmos = true;
    
    [Tooltip("스포너 인덱스 표시")]
    public bool showSpawnerIndex = true;

    [Header("연결된 RoomController")]
    [Tooltip("이 스포너가 속한 RoomController")]
    [SerializeField] private RoomController connectedRoomController;

    // 컴포넌트
    private AudioSource audioSource;

    /// <summary>
    /// 대기 중인 스폰 요청 목록
    /// </summary>
    private readonly List<PendingSpawn> pendingSpawns = new List<PendingSpawn>();

    /// <summary>
    /// 스폰 대기 데이터
    /// </summary>
    private class PendingSpawn
    {
        public MonsterSpawnData spawnData;
        public Vector3 spawnPosition;
        public float spawnTime;
        public GameObject effectInstance;
    }

    /// <summary>
    /// 스포너 초기화
    /// </summary>
    private void Awake()
    {
        // AudioSource 컴포넌트 자동 할당 또는 추가
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D 사운드
        }
        
        // 스포너 표시 이펙트 활성화
        if (spawnerIndicator != null)
        {
            spawnerIndicator.SetActive(true);
        }
        
        // RoomController 자동 찾기 (Awake에서 시도)
        if (connectedRoomController == null)
        {
            connectedRoomController = GetComponentInParent<RoomController>();
            if (connectedRoomController != null)
            {
                Debug.Log($"[MonsterSpawner] {spawnerName}에서 RoomController 자동 찾기 성공: {connectedRoomController.name}");
            }
        }
    }

    /// <summary>
    /// 스포너 업데이트 (대기 중인 스폰 처리)
    /// </summary>
    private void Update()
    {
        if (pendingSpawns.Count == 0)
        {
            return;
        }

        float currentTime = Time.time;

        for (int i = pendingSpawns.Count - 1; i >= 0; i--)
        {
            PendingSpawn pending = pendingSpawns[i];

            if (currentTime >= pending.spawnTime)
            {
                ExecuteSpawn(pending);
                pendingSpawns.RemoveAt(i);
            }
        }

        // 대기 중인 스폰이 모두 처리되었다면 경고 이펙트 비활성화
        if (pendingSpawns.Count == 0 && warningEffect != null && warningEffect.activeSelf)
        {
            warningEffect.SetActive(false);
        }
    }

    /// <summary>
    /// 몬스터 스폰 시작
    /// </summary>
    /// <param name="spawnData">스폰할 몬스터 데이터</param>
    /// <param name="warningTime">위협 표시 시간</param>
    public void StartSpawning(MonsterSpawnData spawnData, float warningTime)
    {
        if (!isActive)
        {
            Debug.LogWarning($"[MonsterSpawner] 스포너 {spawnerName}이 비활성화되어 있습니다.");
            return;
        }

        // RoomController가 연결되어 있는지 확인
        if (connectedRoomController == null)
        {
            Debug.LogWarning($"[MonsterSpawner] {spawnerName}에 RoomController가 연결되지 않았습니다. 자동으로 찾는 중...");
            connectedRoomController = GetComponentInParent<RoomController>();
            if (connectedRoomController == null)
            {
                Debug.LogError($"[MonsterSpawner] {spawnerName}에서 RoomController를 찾을 수 없습니다! 스폰을 중단합니다.");
                return;
            }
            else
            {
                Debug.Log($"[MonsterSpawner] {spawnerName}에서 RoomController 자동 찾기 성공: {connectedRoomController.name}");
            }
        }
        
        // 위협 표시 이펙트 활성화
        if (warningEffect != null)
        {
            warningEffect.SetActive(true);
        }
        
        // 스폰 위치 미리 계산
        Vector3 spawnPosition = GetSpawnPosition();

        // 스폰 예고 이펙트 생성
        GameObject effectInstance = CreateSpawnEffect(spawnPosition);

        // 스폰 정보를 대기열에 추가
        float spawnTime = Time.time + warningTime;
        pendingSpawns.Add(new PendingSpawn
        {
            spawnData = spawnData,
            spawnPosition = spawnPosition,
            spawnTime = spawnTime,
            effectInstance = effectInstance
        });

        PlaySpawnWarningSound();

        Debug.Log($"[MonsterSpawner] {spawnerName}에서 {spawnData.monsterPrefab.name} 스폰 시작 (대기시간: {warningTime}초)");
    }

    /// <summary>
    /// 몬스터 스폰 실행 (대기 중인 스폰 데이터를 기반으로)
    /// </summary>
    private void ExecuteSpawn(PendingSpawn pending)
    {
        if (pending.spawnData == null || pending.spawnData.monsterPrefab == null)
        {
            Debug.LogError($"[MonsterSpawner] {spawnerName}에서 스폰할 몬스터 데이터가 없습니다.");
            SafeDestroyEffect(pending.effectInstance);
            return;
        }

        // 몬스터 인스턴스 생성
        GameObject monsterInstance = Instantiate(pending.spawnData.monsterPrefab, pending.spawnPosition, GetSpawnRotation());
        
        // 몬스터를 RoomController에 등록
        var monsterController = monsterInstance.GetComponent<MonsterController>();
        if (monsterController != null)
        {
            Debug.Log($"[MonsterSpawner] {spawnerName}에서 몬스터 {monsterController.name} 스폰됨");
            
            // 연결된 RoomController 사용 (StartSpawning에서 이미 확인됨)
            if (connectedRoomController != null)
            {
                Debug.Log($"[MonsterSpawner] 연결된 RoomController 사용: {connectedRoomController.name}");
                
                // 몬스터에 RoomController 설정 (분열 몬스터 등록용)
                monsterController.SetRoomController(connectedRoomController);
                
                // 몬스터 등록
                connectedRoomController.RegisterMonster(monsterController);
                
                // 몬스터 죽음 이벤트 구독
                monsterController.OnMonsterDied += connectedRoomController.UnregisterMonster;
                Debug.Log($"[MonsterSpawner] {monsterController.name}의 죽음 이벤트 구독 완료");
            }
            else
            {
                Debug.LogError($"[MonsterSpawner] {spawnerName}에 연결된 RoomController가 없습니다! 이는 StartSpawning에서 확인되어야 합니다.");
            }
        }
        else
        {
            Debug.LogError($"[MonsterSpawner] {spawnerName}에서 MonsterController를 찾을 수 없습니다!");
        }

        // 스폰 사운드 재생
        PlaySpawnSound();

        // 스폰 이펙트 제거
        SafeDestroyEffect(pending.effectInstance);

        Debug.Log($"[MonsterSpawner] {spawnerName}에서 {pending.spawnData.monsterPrefab.name} 스폰 완료");
    }

    /// <summary>
    /// 스폰 위치 계산
    /// </summary>
    private Vector3 GetSpawnPosition()
    {
        Vector3 basePosition = transform.position + Vector3.up * spawnHeightOffset;
        
        // 스폰 반경 내 랜덤 위치 (2D 기준: X/Y 평면 사용, Z는 고정)
        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = basePosition + new Vector3(randomOffset.x, randomOffset.y, 0f);
        spawnPosition.z = transform.position.z; // Z 값 고정 (렌더 순서 안정화)
        
        return spawnPosition;
    }

    /// <summary>
    /// 스폰 회전값 계산
    /// </summary>
    private Quaternion GetSpawnRotation()
    {
        if (randomRotation)
        {
            return Quaternion.Euler(0f, 0f, 0f);
        }
        else
        {
            return transform.rotation;
        }
    }

    /// <summary>
    /// 스폰 이펙트 생성 (성공 시 이펙트 인스턴스를 반환)
    /// </summary>
    private GameObject CreateSpawnEffect(Vector3 position)
    {
        if (connectedRoomController == null)
        {
            Debug.LogWarning($"[MonsterSpawner] {spawnerName}에 연결된 RoomController가 없습니다.");
            return null;
        }

        RoomControllerSO roomData = connectedRoomController.GetRoomData();
        if (roomData == null)
        {
            Debug.LogWarning($"[MonsterSpawner] {spawnerName}에서 RoomControllerSO 데이터를 가져올 수 없습니다.");
            return null;
        }

        if (roomData.spawnEffect == null)
        {
            Debug.LogWarning($"[MonsterSpawner] {spawnerName}의 RoomControllerSO에 spawnEffect가 설정되지 않았습니다.");
            return null;
        }

        GameObject effectInstance = Instantiate(roomData.spawnEffect, position, Quaternion.identity);
        Debug.Log($"[MonsterSpawner] {spawnerName}에서 스폰 예고 이펙트 생성 at {position}");
        return effectInstance;
    }

    /// <summary>
    /// 이펙트 안전 제거
    /// </summary>
    private void SafeDestroyEffect(GameObject effectInstance)
    {
        if (effectInstance != null)
        {
            Destroy(effectInstance);
        }
    }
    
    /// <summary>
    /// 스폰 예고 효과음 재생 (스폰 전에 재생)
    /// </summary>
    private void PlaySpawnWarningSound()
    {
        // RoomControllerSO 데이터 가져오기
        if (connectedRoomController != null)
        {
            RoomControllerSO roomData = connectedRoomController.GetRoomData();
            
            if (roomData != null)
            {
                // 스폰 예고 효과음 재생
                if (roomData.spawnWarningSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(roomData.spawnWarningSound);
                    Debug.Log($"[MonsterSpawner] {spawnerName}에서 스폰 예고 효과음 재생: {roomData.spawnWarningSound.name}");
                }
            }
        }
    }
    
    /// <summary>
    /// 스폰 사운드 재생 (실제 스폰 시 재생)
    /// </summary>
    private void PlaySpawnSound()
    {
        // RoomControllerSO 데이터 가져오기
        if (connectedRoomController != null)
        {
            RoomControllerSO roomData = connectedRoomController.GetRoomData();
            
            if (roomData != null)
            {
                // 스폰 사운드 재생
                if (roomData.spawnSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(roomData.spawnSound);
                    Debug.Log($"[MonsterSpawner] {spawnerName}에서 스폰 사운드 재생: {roomData.spawnSound.name}");
                }
                else if (roomData.spawnSound == null)
                {
                    Debug.LogWarning($"[MonsterSpawner] {spawnerName}의 RoomControllerSO에 spawnSound가 설정되지 않았습니다.");
                }
            }
        }
    }
    
    /// <summary>
    /// 모든 활성 스폰 이펙트 즉시 제거
    /// </summary>
    private void RemoveAllSpawnEffects()
    {
        foreach (PendingSpawn pending in pendingSpawns)
        {
            SafeDestroyEffect(pending.effectInstance);
        }
        pendingSpawns.Clear();
        Debug.Log($"[MonsterSpawner] {spawnerName}에서 모든 스폰 이펙트 즉시 제거 완료");
    }

    /// <summary>
    /// 스포너 비활성화
    /// </summary>
    public void DeactivateSpawner()
    {
        isActive = false;
        
        // 모든 스폰 이펙트 즉시 제거
        RemoveAllSpawnEffects();
        
        if (warningEffect != null)
        {
            warningEffect.SetActive(false);
        }
    }

    /// <summary>
    /// 스포너 활성화
    /// </summary>
    public void ActivateSpawner()
    {
        isActive = true;
    }

    /// <summary>
    /// 현재 스폰 중인지 확인
    /// </summary>
    public bool IsSpawning()
    {
        return pendingSpawns.Count > 0;
    }

    /// <summary>
    /// RoomController 설정
    /// </summary>
    public void SetRoomController(RoomController roomController)
    {
        connectedRoomController = roomController;
        Debug.Log($"[MonsterSpawner] {spawnerName}에 RoomController 설정: {roomController?.name ?? "null"}");
    }

    /// <summary>
    /// 스포너 정보 가져오기
    /// </summary>
    public SpawnerInfo GetSpawnerInfo()
    {
        return new SpawnerInfo
        {
            spawnerName = this.spawnerName,
            spawnerIndex = this.spawnerIndex,
            spawnPosition = transform.position,
            isActive = this.isActive
        };
    }

    /// <summary>
    /// 기즈모 그리기
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // 스포너 위치 표시
        Gizmos.color = spawnerColor;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * spawnHeightOffset, spawnRadius);
        
        // 스포너 방향 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
        
        // 스포너 인덱스 표시
        if (showSpawnerIndex)
        {
            DrawSpawnerIndex();
        }
    }

    /// <summary>
    /// 기즈모 선택 시 그리기
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // 선택된 스포너 강조 표시
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * spawnHeightOffset, spawnRadius + 0.5f);
    }

    /// <summary>
    /// 스포너 인덱스 그리기
    /// </summary>
    private void DrawSpawnerIndex()
    {
        // 스포너 인덱스 텍스트 표시
        Vector3 labelPosition = transform.position -Vector3.right * 2.5f + Vector3.up * 1.0f;   
        
        // 인덱스 번호 표시
        string indexText = $"Spawner {spawnerIndex}";
        
        // Unity의 Handles를 사용하여 텍스트 표시 (크고 검정색)
        #if UNITY_EDITOR
        // 새로운 스타일 생성
        var labelStyle = new GUIStyle();
        labelStyle.fontSize = 32;  // 글씨 크기 증가
        labelStyle.normal.textColor = Color.red;  // 검정색
        labelStyle.fontStyle = FontStyle.Bold;  // 굵은 글씨
        
        // 스타일 적용하여 텍스트 표시
        UnityEditor.Handles.Label(labelPosition, indexText, labelStyle);
        #endif
    }
}

/// <summary>
/// 스포너 정보 구조체
/// </summary>
[System.Serializable]
public struct SpawnerInfo
{
    public string spawnerName;
    public int spawnerIndex;
    public Vector3 spawnPosition;
    public bool isActive;
}
