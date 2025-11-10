using UnityEngine;

/// <summary>
/// 산탄총 투사체 (산탄)
/// ISkillProjectile 구현으로 MonsterController의 스킬 시스템과 호환
/// SimpleFireball을 기반으로 제작되었습니다
/// </summary>
public class ShotgunPellet : MonoBehaviour, ISkillProjectile
{
    #region Inspector Fields - 인스펙터 설정
    
    [Header("투사체 설정")]
    [Tooltip("자동 파괴 시간 (초) - 일정 시간 후 화면에서 사라짐")]
    [SerializeField] private float lifetime = 3f;
    
    [Tooltip("충돌 시 생성할 파괴 이펙트 (옵션)")]
    [SerializeField] private GameObject hitEffectPrefab;
    
    #endregion
    
    #region Private Fields - 내부 필드
    
    private float damage;              // 데미지
    private float speed;               // 속도
    private EntityType attackerType;   // 공격자 타입 (팀킬 방지용)
    private MonoBehaviour ownerController; // 소유자 컨트롤러
    private bool isPiercing;           // 관통 여부 (산탄은 기본적으로 비관통)
    private bool canCollide = false;   // 충돌 가능 여부 (생성 직후 짧은 시간 동안 false)
    
    #endregion
    
    #region ISkillProjectile 구현
    
    /// <summary>
    /// 스킬 데이터 설정 (데미지, 속도)
    /// </summary>
    /// <param name="damage">투사체 데미지</param>
    /// <param name="speed">투사체 속도</param>
    public void SetSkillData(float damage, float speed)
    {
        this.damage = damage;
        this.speed = speed;
        
        Debug.Log($"[ShotgunPellet] 데미지: {damage}, 속도: {speed}");
    }
    
    /// <summary>
    /// 공격자 타입 설정 (팀킬 방지용)
    /// </summary>
    /// <param name="type">공격자의 EntityType (Monster, Player 등)</param>
    public void SetAttackerType(EntityType type)
    {
        this.attackerType = type;
        
        Debug.Log($"[ShotgunPellet] 공격자 타입: {type}");
    }
    
    /// <summary>
    /// 소유자 컨트롤러 설정
    /// </summary>
    /// <param name="owner">스킬을 발사한 컨트롤러</param>
    public void SetOwnerController(MonoBehaviour owner)
    {
        this.ownerController = owner;
    }
    
    /// <summary>
    /// 관통 여부 설정
    /// </summary>
    /// <param name="isPiercing">true면 적을 관통, false면 충돌 시 파괴</param>
    public void SetPiercing(bool isPiercing)
    {
        this.isPiercing = isPiercing;
        
        Debug.Log($"[ShotgunPellet] 관통 여부: {(isPiercing ? "관통형" : "충돌 시 파괴")}");
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
    
    #region Unity Lifecycle - Unity 생명주기
    
    /// <summary>
    /// 시작 시 호출 - 자동 파괴 타이머 설정 및 충돌 활성화
    /// </summary>
    private void Start()
    {
        // 일정 시간 후 자동 파괴 (화면 밖으로 나가도 메모리 누수 방지)
        Destroy(gameObject, lifetime);
        
        // 생성 직후 0.1초 후 충돌 활성화 (몬스터 자신이나 벽과의 즉시 충돌 방지)
        Invoke(nameof(EnableCollision), 0.1f);
    }
    
    /// <summary>
    /// 충돌 활성화 (생성 직후 짧은 딜레이 후 호출)
    /// </summary>
    private void EnableCollision()
    {
        canCollide = true;
        Debug.Log($"[ShotgunPellet] 충돌 활성화됨");
    }
    
    /// <summary>
    /// 트리거 충돌 처리 (2D Collider2D 전용)
    /// 적에게 데미지를 주고, 관통 여부에 따라 파괴 여부 결정
    /// </summary>
    /// <param name="collision">충돌한 Collider2D</param>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 충돌이 활성화되지 않았으면 무시 (생성 직후 0.1초 동안)
        if (!canCollide)
        {
            return;
        }
        
        // EntityController가 있는 대상과 충돌했는지 확인
        EntityController target = collision.GetComponent<EntityController>();
        
        if (target != null)
        {
            // 소유자(발사한 몬스터) 자신과의 충돌은 무시
            if (ownerController != null && target.gameObject == ownerController.gameObject)
            {
                return;
            }
            
            // 데미지 적용 시도 (팀킬 방지 로직 포함)
            // EntityController의 ApplyDamage가 자동으로 같은 팀이면 false 반환
            bool damageApplied = target.ApplyDamage((int)damage, attackerType);
            
            if (damageApplied)
            {
                // ✅ 적에게 데미지를 성공적으로 줬음
                Debug.Log($"[ShotgunPellet] {target.name}에게 {damage} 데미지!");
                
                // 파괴 이펙트 생성 (옵션)
                if (hitEffectPrefab != null)
                {
                    Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
                }
                
                // 관통 여부에 따라 파괴 결정
                if (!isPiercing)
                {
                    // 비관통형: 적에게 맞았으므로 파괴
                    Debug.Log($"[ShotgunPellet] 비관통형 - 충돌로 인해 파괴됨");
                    Destroy(gameObject);
                }
                else
                {
                    // 관통형: 적을 뚫고 계속 날아감
                    Debug.Log($"[ShotgunPellet] 관통형 - 적을 관통하여 계속 날아감");
                }
            }
            else
            {
                // ❌ 아군이라 데미지가 안 들어감 -> 무조건 통과
                Debug.Log($"[ShotgunPellet] {target.name}은 아군이라 통과!");
            }
        }
        else
        {
            // EntityController가 없는 오브젝트와 충돌 (벽, 장애물 등)
            // 벽에 부딪히면 파괴
            
            // 벽이나 장애물 레이어 확인 (필요시 특정 레이어만 충돌 처리 가능)
            // 예: if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
            
            Debug.Log($"[ShotgunPellet] {collision.name}(벽/장애물)과 충돌 - 파괴됨");
            
            // 파괴 이펙트 생성 (옵션)
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }
            
            Destroy(gameObject);
        }
    }
    
    #endregion
}

