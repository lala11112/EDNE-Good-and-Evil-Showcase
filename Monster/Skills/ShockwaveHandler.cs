using UnityEngine;
using System.Collections;

/// <summary>
/// 충격파 스킬 핸들러
/// 타겟 앞에 충격파를 생성하고 경고 → 발동 시퀀스를 관리합니다
/// </summary>
public class ShockwaveHandler : MonoBehaviour, SkillHandlerInterface
{
    [Header("충격파 설정")]
    [Tooltip("충격파 프리팹 (직사각형 스프라이트)")]
    [SerializeField] private GameObject shockwavePrefab;
    
    [Header("생성 위치 설정")]
    [Tooltip("충격파가 생성될 거리 (몬스터 앞)")]
    [SerializeField] private float spawnDistance = 2f;
    
    [Tooltip("충격파 크기 (너비, 높이)")]
    [SerializeField] private Vector2 shockwaveSize = new Vector2(3f, 1.5f);
    
    private MonsterSkillData skillData;
    private GameObject ownerObject;
    private MonsterController ownerController;
    private Transform target;
    
    #region SkillHandlerInterface 구현
    
    /// <summary>
    /// 스킬 핸들러 초기 설정
    /// </summary>
    public void Setup(BaseSkillData data, GameObject owner)
    {
        skillData = data as MonsterSkillData;
        ownerObject = owner;
        ownerController = owner.GetComponent<MonsterController>();
        
        if (ownerController != null)
        {
            target = ownerController.Target;
        }
        
        Debug.Log($"[ShockwaveHandler] Setup 완료");
    }
    
    /// <summary>
    /// 스킬 사용
    /// </summary>
    public void Use(SkillRank rank)
    {
        if (skillData == null || ownerController == null)
        {
            Debug.LogError("[ShockwaveHandler] 스킬 데이터 또는 소유자가 없습니다!");
            Cleanup();
            return;
        }
        
        if (target == null)
        {
            Debug.LogWarning("[ShockwaveHandler] 타겟이 없습니다!");
            Cleanup();
            return;
        }
        
        // 충격파 생성 및 시퀀스 시작
        StartCoroutine(ShockwaveSequence());
    }
    
    /// <summary>
    /// 스킬 정리
    /// </summary>
    public void Cleanup()
    {
        Debug.Log("[ShockwaveHandler] Cleanup");
        Destroy(gameObject);
    }
    
    #endregion
    
    #region 충격파 로직
    
    /// <summary>
    /// 충격파 시퀀스: 생성 → 경고 → 발동
    /// </summary>
    private IEnumerator ShockwaveSequence()
    {
        // ⭐ 0. 몬스터 이동 완전 잠금 및 즉시 정지
        Debug.Log("[ShockwaveHandler] 몬스터 이동 잠금 및 즉시 정지");
        float totalDuration = CalculateTotalDuration();
        ownerController.LockMovement(totalDuration);
        
        // ⭐ NavMeshAgent를 강제로 즉시 정지 (관성 제거)
        UnityEngine.AI.NavMeshAgent agent = ownerController.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            Debug.Log("[ShockwaveHandler] NavMeshAgent 강제 정지 완료");
        }
        
        // 한 프레임 대기 (NavMeshAgent가 완전히 멈출 시간 제공)
        yield return null;
        
        // 1. 충격파 생성 위치/회전 계산 (항상 동일 스케일, 일정 거리 앞에 스폰)
        Vector3 directionToTarget = (target.position - ownerController.transform.position).normalized;
        Vector3 spawnPosition = ownerController.transform.position + directionToTarget * spawnDistance;
        float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);

        // 2. 충격파 생성
        GameObject shockwavePrefabToUse = shockwavePrefab != null ? shockwavePrefab : skillData.skillEffectPrefab;
        
        if (shockwavePrefabToUse == null)
        {
            Debug.LogError("[ShockwaveHandler] 충격파 프리팹이 없습니다!");
            Cleanup();
            yield break;
        }
        
        GameObject shockwaveObj = Instantiate(shockwavePrefabToUse, spawnPosition, rotation);

        // 3. 충격파 컴포넌트 설정 (항상 동일 스케일 적용)
        Shockwave shockwave = shockwaveObj.GetComponent<Shockwave>();
        if (shockwave != null)
        {
            shockwave.Initialize(skillData.damage, EntityType.Monster, shockwaveSize);
        }
        else
        {
            Debug.LogError("[ShockwaveHandler] Shockwave 컴포넌트가 없습니다!");
            Destroy(shockwaveObj);
            Cleanup();
            yield break;
        }
        
        // 5. 충격파가 끝날 때까지 대기 (경고 → 발동 → 종료)
        yield return new WaitUntil(() => shockwaveObj == null);
        
        // 6. 핸들러 정리 (LockMovement는 시간이 지나면 자동 해제됨)
        Debug.Log("[ShockwaveHandler] 충격파 완료");
        Cleanup();
    }
    
    /// <summary>
    /// 충격파의 전체 지속 시간 계산
    /// </summary>
    private float CalculateTotalDuration()
    {
        // Shockwave 프리팹에서 설정 가져오기
        GameObject prefabToUse = shockwavePrefab != null ? shockwavePrefab : skillData.skillEffectPrefab;
        
        if (prefabToUse != null)
        {
            Shockwave shockwaveComponent = prefabToUse.GetComponent<Shockwave>();
            if (shockwaveComponent != null)
            {
                // Shockwave의 설정값을 통해 계산
                // 기본값: 경고 1.0초 + 발동 0.3초 = 1.3초
                // 여유있게 1.5초로 설정
                return 1.5f;
            }
        }
        
        // 기본값: 1.5초
        return 1.5f;
    }
    
    #endregion
}

