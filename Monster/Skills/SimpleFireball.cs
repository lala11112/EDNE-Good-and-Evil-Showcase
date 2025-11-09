using UnityEngine;

/// <summary>
/// 간단한 파이어볼 투사체
/// ISkillProjectile 구현으로 MonsterController와 호환
/// </summary>
public class SimpleFireball : MonoBehaviour, ISkillProjectile
{
    [Header("설정")]
    [SerializeField] private float lifetime = 5f; // 자동 파괴 시간
    
    [Header("완벽한 회피 시스템")]
    [Tooltip("완벽한 회피 판정 거리 (PlayerDataSO에서 가져옴, Inspector에서 설정 불필요)")]
    [SerializeField] private float perfectDodgeDistance = 1.5f; // 기본값 (SO 데이터가 없을 때 사용)
    
    private float damage;
    private float speed;
    private EntityType attackerType;
    private MonoBehaviour ownerController;
    private bool isPiercing; // 관통 여부
    
    // ⭐ 완벽한 회피 시스템 변수
    private PlayerController playerController;
    private bool wasNearPlayer = false; // 플레이어가 탄막 근처에 있었는지 여부
    private bool hasHitPlayer = false; // 플레이어에게 맞았는지 여부
    
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
    /// 몬스터 스킬은 플레이어 스킬이 아니므로 null 반환
    /// </summary>
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
        
        // ⭐ 플레이어 컨트롤러 참조 가져오기
        if (PlayerController.Instance != null)
        {
            playerController = PlayerController.Instance;
            
            // ⭐ PlayerDataSO에서 완벽한 회피 판정 거리 가져오기
            if (playerController.PlayerData != null)
            {
                perfectDodgeDistance = playerController.PlayerData.perfectDodgeDistance;
                Debug.Log($"[SimpleFireball] 완벽한 회피 판정 거리: {perfectDodgeDistance}");
            }
        }
    }
    
    private void Update()
    {
        // ⭐ 완벽한 회피 시스템: 플레이어와의 거리 체크
        if (playerController != null && !hasHitPlayer)
        {
            CheckPerfectDodgeDistance();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // EntityController가 있는 대상과 충돌 시
        EntityController target = collision.GetComponent<EntityController>();
        if (target != null)
        {
            // ⭐ 플레이어와 충돌했는지 확인
            PlayerController hitPlayer = target.GetComponent<PlayerController>();
            if (hitPlayer != null)
            {
                hasHitPlayer = true; // 플레이어에게 맞았으므로 완벽한 회피 불가
                wasNearPlayer = false; // 회피 판정 초기화
            }
            
            // 데미지 적용 시도 (팀킬 방지 포함)
            bool damageApplied = target.ApplyDamage((int)damage, attackerType);
            
            if (damageApplied)
            {
                // ✅ 적에게 데미지를 줬음 -> 관통 여부에 따라 파괴 결정
                Debug.Log($"[SimpleFireball] {target.name}에게 {damage} 데미지!");
                
                if (!isPiercing)
                {
                    // 비관통형: 적에게 맞았으므로 파괴
                    Debug.Log($"[SimpleFireball] 비관통형 - 충돌로 인해 파괴됨");
                    Destroy(gameObject);
                }
                else
                {
                    // 관통형: 적을 뚫고 계속 날아감
                    Debug.Log($"[SimpleFireball] 관통형 - 파괴되지 않고 계속 날아감");
                }
            }
            else
            {
                // ❌ 아군이라 데미지가 안 들어감 -> 무조건 통과 (파괴 안함)
                Debug.Log($"[SimpleFireball] {target.name}은 아군이라 통과!");
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D collision)
    {
        // ⭐ 완벽한 회피 판정: 플레이어가 탄막을 피했는지 확인
        if (playerController != null && !hasHitPlayer)
        {
            PlayerController exitPlayer = collision.GetComponent<PlayerController>();
            if (exitPlayer != null && wasNearPlayer)
            {
                // 플레이어가 탄막 근처에 있었고, 충돌하지 않고 지나갔으므로 완벽한 회피!
                OnPerfectDodge();
            }
        }
    }
    
    #endregion
    
    #region 완벽한 회피 시스템
    
    /// <summary>
    /// 완벽한 회피 판정 거리 체크
    /// 플레이어가 탄막 근처에 접근했는지 확인 (SO 데이터의 판정 거리만 사용)
    /// </summary>
    private void CheckPerfectDodgeDistance()
    {
        if (playerController == null) return;
        
        // 플레이어와의 거리 계산
        float distanceToPlayer = Vector2.Distance(transform.position, playerController.transform.position);
        
        // ⭐ 완벽한 회피 판정 거리 체크 (SO 데이터의 값만 사용, 속도 기반 계산 제거)
        float dodgeJudgmentDistance = perfectDodgeDistance;
        
        // 플레이어가 탄막 근처에 있는지 확인
        if (distanceToPlayer <= dodgeJudgmentDistance)
        {
            if (!wasNearPlayer)
            {
                // 플레이어가 탄막 근처에 접근함
                wasNearPlayer = true;
#if UNITY_EDITOR
                Debug.Log($"[SimpleFireball] ⭐ 플레이어가 탄막 근처에 접근! (거리: {distanceToPlayer:F2}, 판정 거리: {dodgeJudgmentDistance:F2})");
#endif
            }
        }
        else
        {
            // 플레이어가 탄막 근처에서 벗어남 (회피 판정 초기화)
            if (wasNearPlayer)
            {
                wasNearPlayer = false;
            }
        }
    }
    
    /// <summary>
    /// 완벽한 회피 처리
    /// 플레이어가 탄막을 아슬아슬하게 피했을 때 호출
    /// </summary>
    private void OnPerfectDodge()
    {
        if (playerController == null) return;
        
        // PlayerController의 완벽한 회피 메서드 호출
        playerController.OnPerfectDodge();
        
#if UNITY_EDITOR
        Debug.Log($"[SimpleFireball] ⭐⭐⭐ 완벽한 회피! 플레이어가 탄막을 아슬아슬하게 피했습니다!");
#endif
        
        // 회피 판정 초기화 (중복 호출 방지)
        wasNearPlayer = false;
    }
    
    #endregion
}

