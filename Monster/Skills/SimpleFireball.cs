using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 간단한 파이어볼 투사체
/// ISkillProjectile 구현으로 MonsterController와 호환
/// </summary>
public class SimpleFireball : MonoBehaviour, ISkillProjectile
{
    [Header("설정")]
    [SerializeField] private float lifetime = 5f; // 자동 파괴 시간
    
    private float damage;
    private float speed;
    private EntityType attackerType;
    private MonoBehaviour ownerController;
    private bool isPiercing; // 관통 여부
    
    /// <summary>
    /// 이미 데미지를 준 대상들 (중복 히트 방지)
    /// </summary>
    private HashSet<EntityController> hitTargets = new HashSet<EntityController>();
    
    #region 완벽한 회피 시스템 (거리 기반 + 최근접점 추적)
    
    /// <summary>
    /// 플레이어와의 최소 거리 (아슬아슬함 판정용)
    /// </summary>
    private float minDistanceToPlayer = float.MaxValue;
    
    /// <summary>
    /// 회피 범위에 들어온 적이 있는지 여부
    /// </summary>
    private bool hasBeenInDodgeRange = false;
    
    /// <summary>
    /// 완벽한 회피 판정을 이미 했는지 여부 (중복 방지)
    /// </summary>
    private bool hasTriggeredPerfectDodge = false;
    
    /// <summary>
    /// 플레이어와 충돌했는지 여부 (충돌 시 완벽한 회피 판정 안함)
    /// </summary>
    private bool hasHitPlayer = false;
    
    #endregion
    
    #region ISkillProjectile 구현
    
    public void SetSkillData(float damage, float speed)
    {
        this.damage = damage;
        this.speed = speed;
        
        Debug.Log($"[SimpleFireball] 데미지: {damage}, 속도: {speed}");
    }
    
    public void SetAttackerType(EntityType type)
    {
        this.attackerType = type;
        
        Debug.Log($"[SimpleFireball] 공격자 타입: {type}");
    }
    
    public void SetOwnerController(MonoBehaviour owner)
    {
        this.ownerController = owner;
    }
    
    public void SetPiercing(bool isPiercing)
    {
        this.isPiercing = isPiercing;
        
        Debug.Log($"[SimpleFireball] 관통 여부: {(isPiercing ? "관통형" : "충돌 시 파괴")}");
    }
    
    /// <summary>
    /// 스킬 슬롯 타입 가져오기 (ISkillProjectile 인터페이스 구현)
    /// 몬스터 스킬이므로 플레이어 스킬 슬롯 타입은 없음 (null 반환)
    /// </summary>
    /// <returns>null (몬스터 스킬은 플레이어 스킬 슬롯 타입 없음)</returns>
    public SkillSlotType? GetSkillSlotType()
    {
        return null; // 몬스터 스킬은 플레이어 스킬 슬롯 타입이 없음
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Start()
    {
        // 일정 시간 후 자동 파괴
        Destroy(gameObject, lifetime);
    }
    
    private void Update()
    {
        // 완벽한 회피 시스템: 거리 추적
        UpdatePerfectDodgeTracking();
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // EntityController가 있는 대상과 충돌 시
        EntityController target = collision.GetComponent<EntityController>();
        if (target == null) return;
        
        // 중복 히트 방지: 이미 데미지를 준 대상은 무시
        if (hitTargets.Contains(target))
        {
            Debug.Log($"[SimpleFireball] {target.name}은 이미 맞은 적 - 무시 (중복 히트 방지)");
            return;
        }
        
        // 플레이어와 충돌했는지 확인 (완벽한 회피 판정 방지용)
        if (target.EntityType == EntityType.Player)
        {
            hasHitPlayer = true;
        }
        
        // 데미지 적용 시도 (팀킬 방지 포함, 자기 자신을 damageSource로 전달)
        bool damageApplied = target.ApplyDamage((int)damage, attackerType, this);
        
        if (damageApplied)
        {
            // 적에게 데미지를 줬음 -> 히트 목록에 추가 및 관통 여부에 따라 파괴 결정
            hitTargets.Add(target);
            Debug.Log($"[SimpleFireball] {target.name}에게 {damage} 데미지!");
            
            if (!isPiercing)
            {
                // 비관통형: 적에게 맞았으므로 파괴
                Debug.Log($"[SimpleFireball] 비관통형 - 충돌로 인해 파괴됨");
                Destroy(gameObject);
            }
            else
            {
                // 관통형: 적을 뚫고 계속 날아감 (다음 적도 맞출 수 있음)
                Debug.Log($"[SimpleFireball] 관통형 - 파괴되지 않고 계속 날아감");
            }
        }
        else
        {
            // 아군이라 데미지가 안 들어감 -> 무조건 통과 (파괴 안함)
            Debug.Log($"[SimpleFireball] {target.name}은 아군이라 통과!");
        }
    }
    
    private void OnDestroy()
    {
        // 탄환이 파괴될 때 완벽한 회피 판정 (충돌하지 않았을 때만)
        if (hasBeenInDodgeRange && !hasTriggeredPerfectDodge && !hasHitPlayer)
        {
            CheckPerfectDodge();
        }
    }
    
    #endregion
    
    #region 완벽한 회피 시스템
    
    /// <summary>
    /// 완벽한 회피 추적 업데이트 (매 프레임 호출)
    /// 거리 기반 + 최근접점 추적 방식
    /// </summary>
    private void UpdatePerfectDodgeTracking()
    {
        // 플레이어가 없거나 이미 충돌했거나 이미 판정했으면 스킵
        PlayerController player = PlayerController.Instance;
        if (player == null || hasHitPlayer || hasTriggeredPerfectDodge) return;
        
        // PlayerDataSO 가져오기 (설정값 사용)
        PlayerDataSO playerData = player.PlayerData;
        if (playerData == null) return;
        
        // 플레이어와의 거리 계산
        Vector2 playerPosition = player.transform.position;
        Vector2 fireballPosition = transform.position;
        float distance = Vector2.Distance(fireballPosition, playerPosition);
        
        // 최소 거리 추적 (아슬아슬함 판정용)
        if (distance < minDistanceToPlayer)
        {
            minDistanceToPlayer = distance;
        }
        
        // 회피 범위 체크 (플레이어 히트박스 반지름 + 여유)
        float dodgeRange = playerData.perfectDodgeRange;
        if (distance < dodgeRange)
        {
            // 회피 범위에 들어왔음을 표시
            hasBeenInDodgeRange = true;
        }
        
        // 회피 범위에 들어왔다가 안전 거리 이상 멀어졌을 때 판정
        if (hasBeenInDodgeRange)
        {
            float safeDistance = playerData.perfectDodgeSafeDistance;
            if (distance > safeDistance)
            {
                // 안전하게 지나갔으므로 완벽한 회피 판정
                CheckPerfectDodge();
            }
        }
    }
    
    /// <summary>
    /// 완벽한 회피 판정
    /// 조건: 회피 범위에 들어왔고, 최소 거리가 임계값 이내, 실제 충돌 없음
    /// </summary>
    private void CheckPerfectDodge()
    {
        // 이미 판정했거나 충돌했으면 스킵
        if (hasTriggeredPerfectDodge || hasHitPlayer) return;
        
        // 플레이어가 없으면 스킵
        PlayerController player = PlayerController.Instance;
        if (player == null) return;
        
        PlayerDataSO playerData = player.PlayerData;
        if (playerData == null) return;
        
        // 완벽한 회피 조건 체크
        // 1. 회피 범위에 들어왔음
        // 2. 최소 거리가 임계값 이내 (아슬아슬하게 지나감)
        // 3. 실제 충돌 없음
        if (hasBeenInDodgeRange && 
            minDistanceToPlayer < playerData.perfectDodgeRange &&
            !hasHitPlayer)
        {
            // 완벽한 회피 판정!
            player.OnPerfectDodge();
            hasTriggeredPerfectDodge = true;
            
            Debug.Log($"[SimpleFireball] 완벽한 회피! 최소 거리: {minDistanceToPlayer:F2}, 판정 범위: {playerData.perfectDodgeRange}");
        }
    }
    
    #endregion
}

