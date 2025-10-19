using UnityEngine;

/// <summary>
/// 몬스터 데이터 ScriptableObject
/// EntityDataSO를 상속받아 몬스터 특화 데이터 정의
/// </summary>
[CreateAssetMenu(fileName = "MonsterData", menuName = "Entity/Monster Data")]
public class MonsterDataSO : EntityDataSO
{
    [Header("AI 설정")]
    [Tooltip("플레이어 인식 거리")]
    public float detectionRange = 10f;
    
    [Tooltip("타겟에게 접근할 최소 거리 (이 거리까지만 다가감)")]
    public float stopDistance = 1.5f;
    
    [Tooltip("인식 범위를 벗어나면 추적 중단 여부")]
    public bool stopWhenOutOfRange = true;
    
    [Header("이동 설정")]
    [Tooltip("이동 가능 여부")]
    public bool canMove = true;
    
    [Header("몸박 데미지")]
    [Tooltip("몸박 데미지 여부")]
    public bool hasBodyDamage = false;
    
    [Tooltip("몸박 데미지 수치")]
    public int bodyDamageAmount = 5;
    
    [Header("보상 설정")]
    [Tooltip("처치 시 경험치")]
    public int experienceReward = 10;
    
    [Tooltip("처치 시 골드")]
    public int goldReward = 5;
    
    [Tooltip("처치 시 용기 점수 (플레이어가 얻는 용기)")]
    public int courageReward = 10;
    
    [Tooltip("아이템 드랍 확률 (0~100)")]
    [Range(0, 100)]
    public float dropChance = 30f;
    
    // TODO: 아이템 드랍 테이블은 나중에 별도 시스템으로 확장
    // [Tooltip("드랍 아이템 리스트")]
    // public ItemDropTable dropTable;
    
    [Header("죽음 설정")]
    [Tooltip("죽음 후 오브젝트 파괴까지 대기 시간 (초) - 애니메이션/이펙트 시간")]
    public float deathDestroyDelay = 2f;
    
    [Tooltip("죽음 시 페이드아웃 효과 사용 여부")]
    public bool useFadeOutOnDeath = false;
    
    [Tooltip("페이드아웃 지속 시간 (초)")]
    public float fadeOutDuration = 1f;
    
    // OnValidate - entityType 자동 설정
    private void OnValidate()
    {
        entityType = EntityType.Monster;
    }
}