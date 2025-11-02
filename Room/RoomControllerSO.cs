using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 방 컨트롤러 설정 데이터 (ScriptableObject)
/// 방의 웨이브, 스폰, 몬스터 설정을 관리합니다.
/// </summary>
[CreateAssetMenu(fileName = "RoomControllerData", menuName = "Room/RoomControllerSO")]
public class RoomControllerSO : ScriptableObject
{
    [Header("방 기본 정보")]
    [Tooltip("방 이름")]
    public string roomName = "Default Room";
    
    [Tooltip("방 설명")]
    [TextArea(3, 5)]
    public string roomDescription = "기본 방입니다.";

    [Header("몬스터 스폰 설정")]
    [Tooltip("몬스터 스폰 전 위협 표시 대기 시간")]
    [Range(0.5f, 5f)]
    public float spawnWarningTime = 2f;
    
    [Tooltip("몬스터 스폰 이펙트")]
    public GameObject spawnEffect;
    
    [Tooltip("몬스터 스폰 예고 사운드 (이펙트와 함께 재생)")]
    public AudioClip spawnWarningSound;
    
    [Tooltip("몬스터 스폰 사운드 (실제 스폰 시 재생)")]
    public AudioClip spawnSound;
    
    [Tooltip("몬스터 스폰 시 랜덤 위치 오프셋")]
    [Range(0f, 2f)]
    public float spawnRandomOffset = 0.5f;

    [Header("웨이브 설정")]
    [Tooltip("웨이브 데이터 리스트")]
    public List<WaveData> waveDataList = new List<WaveData>();

    [Header("방 클리어 설정")]
    [Tooltip("방 클리어 시 보상")]
    public List<RewardData> clearRewards = new List<RewardData>();
    
    [Tooltip("방 클리어 시 다음 방으로 이동 가능")]
    public bool allowNextRoomOnClear = true;

    [Header("몬스터 추적 설정")]
    [Tooltip("몬스터가 방을 벗어났을 때 처리 방식")]
    public MonsterEscapeAction escapeAction = MonsterEscapeAction.TeleportBack;
    
    [Tooltip("몬스터가 방을 벗어난 거리 임계값")]
    [Range(5f, 50f)]
    public float escapeDistance = 10f;
    
    [Tooltip("몬스터가 끼었을 때 처리 방식")]
    public MonsterStuckAction stuckAction = MonsterStuckAction.TeleportToSpawner;

    [Header("디버그 설정")]
    [Tooltip("디버그 로그 출력")]
    public bool enableDebugLog = true;
}

/// <summary>
/// 웨이브 데이터
/// </summary>
[System.Serializable]
public class WaveData
{
    [Header("웨이브 정보")]
    [Tooltip("웨이브 번호")]
    public int waveNumber = 1;
    
    [Tooltip("웨이브 이름")]
    public string waveName = "Wave 1";

    [Header("몬스터 스폰 설정")]
    [Tooltip("이 웨이브에서 스폰될 몬스터 데이터")]
    public List<MonsterSpawnData> monsterSpawnData = new List<MonsterSpawnData>();
    
    [Tooltip("웨이브 시작 시 대기 시간")]
    [Range(0f, 5f)]
    public float waveStartDelay = 1f;
    
    [Tooltip("몬스터 스폰 간격")]
    [Range(0.1f, 3f)]
    public float spawnInterval = 0.5f;

    [Header("웨이브 완료 조건")]
    [Tooltip("이 웨이브가 마지막 웨이브인지")]
    public bool isLastWave = false;
    
    [Tooltip("웨이브 완료 시 특별 이펙트")]
    public GameObject waveCompleteEffect;
}

/// <summary>
/// 몬스터 스폰 데이터
/// </summary>
[System.Serializable]
public class MonsterSpawnData
{
    [Header("몬스터 정보")]
    [Tooltip("스폰될 몬스터 프리팹")]
    public GameObject monsterPrefab;
    
    [Tooltip("스폰될 몬스터 수")]
    [Range(1, 20)]
    public int spawnCount = 1;
    
    [Tooltip("몬스터 스폰 가중치 (랜덤 스폰 시)")]
    [Range(1, 100)]
    public int spawnWeight = 50;
    
    [Tooltip("특정 스포너에서만 스폰할지 여부")]
    public bool useSpecificSpawner = false;
    
    [Tooltip("사용할 스포너 인덱스 (useSpecificSpawner가 true일 때)")]
    public int spawnerIndex = 0;
}

/// <summary>
/// 보상 데이터
/// </summary>
[System.Serializable]
public class RewardData
{
    [Tooltip("보상 타입")]
    public RewardType rewardType = RewardType.Experience;
    
    [Tooltip("보상 수량")]
    public int rewardAmount = 100;
    
    [Tooltip("보상 아이템 (아이템 타입일 때)")]
    public GameObject rewardItem;
}

/// <summary>
/// 몬스터 탈출 시 처리 방식
/// </summary>
public enum MonsterEscapeAction
{
    TeleportBack,       // 스포너로 텔레포트
    Kill,              // 즉시 처치
    Ignore,            // 무시
    ResetPosition       // 원래 위치로 리셋 
}

/// <summary>
/// 몬스터 끼임 시 처리 방식
/// </summary>
public enum MonsterStuckAction
{
    TeleportToSpawner, // 스포너로 텔레포트
    Kill,              // 즉시 처치
    Ignore,            // 무시
    ResetPosition      // 원래 위치로 리셋
}

/// <summary>
/// 보상 타입
/// </summary>
public enum RewardType
{
    Experience,        // 경험치
    Gold,              // 골드
    Item,              // 아이템
    SkillPoint         // 스킬 포인트
}
