using UnityEngine;

/// <summary>
/// 산탄총 스킬 핸들러
/// SkillHandlerInterface를 구현하여 MonsterController에서 사용 가능
/// 플레이어 방향으로 여러 발의 산탄을 부채꼴로 발사합니다
/// </summary>
public class ShotgunHandler : MonoBehaviour, SkillHandlerInterface
{
    #region Inspector Fields - 인스펙터 설정
    
    [Header("산탄 투사체 설정")]
    [Tooltip("산탄 투사체 프리팹 (ISkillProjectile 구현 필요)")]
    [SerializeField] private GameObject pelletPrefab;
    
    [Header("산탄 발사 설정")]
    [Tooltip("한 번에 발사할 산탄 개수")]
    [SerializeField] private int pelletCount = 8;
    
    [Tooltip("산탄 확산 각도 (도 단위, 예: 30도면 좌우 15도씩)")]
    [SerializeField] private float spreadAngle = 30f;
    
    [Tooltip("각 산탄의 데미지 배율 (스킬 데이터 damage 기준, 예: 0.5면 절반)")]
    [SerializeField] private float damageMultiplier = 0.5f;
    
    [Tooltip("산탄 발사 효과음")]
    [SerializeField] private AudioClip shotgunFireSound;
    
    #endregion
    
    #region Private Fields - 내부 필드
    
    private MonsterSkillData skillData;
    private GameObject ownerObject;
    private MonsterController ownerController;
    private Transform target;
    
    #endregion
    
    #region SkillHandlerInterface 구현
    
    /// <summary>
    /// 스킬 핸들러 초기 설정
    /// MonsterController에서 스킬 사용 전 호출됩니다
    /// </summary>
    /// <param name="data">몬스터 스킬 데이터 SO</param>
    /// <param name="owner">스킬 소유자 (몬스터) GameObject</param>
    public void Setup(BaseSkillData data, GameObject owner)
    {
        // 스킬 데이터를 MonsterSkillData로 캐스팅
        skillData = data as MonsterSkillData;
        ownerObject = owner;
        ownerController = owner.GetComponent<MonsterController>();
        
        // 타겟(플레이어) 참조 가져오기
        if (ownerController != null)
        {
            target = ownerController.Target;
        }
        
        Debug.Log($"[ShotgunHandler] Setup 완료 - Damage: {skillData?.damage}, Pellets: {pelletCount}");
    }
    
    /// <summary>
    /// 스킬 사용 (산탄총 발사)
    /// </summary>
    /// <param name="rank">스킬 랭크 (몬스터는 사용하지 않음)</param>
    public void Use(SkillRank rank)
    {
        // 필수 데이터 검증
        if (skillData == null || ownerController == null)
        {
            Debug.LogError("[ShotgunHandler] 스킬 데이터 또는 소유자가 없습니다!");
            Cleanup();
            return;
        }
        
        if (target == null)
        {
            Debug.LogWarning("[ShotgunHandler] 타겟이 없습니다!");
            Cleanup();
            return;
        }
        
        // 산탄 발사
        FireShotgun();
        
        // 효과음 재생
        if (shotgunFireSound != null && ownerController != null)
        {
            AudioSource.PlayClipAtPoint(shotgunFireSound, ownerController.transform.position);
        }
        
        // 스킬 즉시 종료 (투사체는 독립적으로 날아감)
        Cleanup();
    }
    
    /// <summary>
    /// 스킬 정리 (종료 시 호출)
    /// 핸들러 오브젝트를 파괴하여 메모리 정리
    /// </summary>
    public void Cleanup()
    {
        Debug.Log("[ShotgunHandler] Cleanup 호출됨 - 핸들러 파괴");
        Destroy(gameObject);
    }
    
    #endregion
    
    #region Private Methods - 스킬 로직
    
    /// <summary>
    /// 산탄총 발사 - 여러 발의 산탄을 부채꼴로 발사
    /// </summary>
    private void FireShotgun()
    {
        // 투사체 프리팹 확인 (핸들러에 설정된 것 우선, 없으면 SO에서 가져오기)
        GameObject projectilePrefab = pelletPrefab != null ? pelletPrefab : skillData.skillEffectPrefab;
        
        if (projectilePrefab == null)
        {
            Debug.LogError("[ShotgunHandler] 산탄 투사체 프리팹이 없습니다!");
            return;
        }
        
        // 발사 위치 (몬스터 위치)
        Vector3 spawnPos = ownerController.transform.position;
        
        // 타겟 방향 계산 (플레이어를 향하는 방향)
        Vector2 baseDirection = (target.position - spawnPos).normalized;
        
        // 방향 벡터가 0이면 기본 방향(오른쪽) 사용
        if (baseDirection.sqrMagnitude < 0.000001f)
        {
            baseDirection = Vector2.right;
        }
        
        // 기본 방향의 각도 계산 (라디안 -> 도)
        float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;
        
        // 각 산탄의 데미지 계산 (스킬 데이터의 damage * 배율)
        float pelletDamage = skillData.damage * damageMultiplier;
        
        // 산탄 발사 (pelletCount 개수만큼 부채꼴로 분산)
        for (int i = 0; i < pelletCount; i++)
        {
            // 각도 오프셋 계산
            // 예: pelletCount=8, spreadAngle=30이면
            // -15, -10.71, -6.43, -2.14, +2.14, +6.43, +10.71, +15도로 분산
            float angleOffset = Mathf.Lerp(-spreadAngle / 2f, spreadAngle / 2f, i / (float)(pelletCount - 1));
            
            // 최종 발사 각도 = 기본 각도 + 오프셋
            float finalAngle = baseAngle + angleOffset;
            
            // 각도를 방향 벡터로 변환
            Vector2 direction = new Vector2(
                Mathf.Cos(finalAngle * Mathf.Deg2Rad),
                Mathf.Sin(finalAngle * Mathf.Deg2Rad)
            ).normalized;
            
            // 산탄 생성 및 발사
            SpawnAndFirePellet(projectilePrefab, spawnPos, direction, pelletDamage);
        }
        
        Debug.Log($"[ShotgunHandler] 산탄 {pelletCount}발 발사! " +
                  $"(각 탄환 데미지: {pelletDamage:F1}, 확산각: {spreadAngle}도)");
    }
    
    /// <summary>
    /// 개별 산탄 생성 및 발사
    /// </summary>
    /// <param name="projectilePrefab">투사체 프리팹</param>
    /// <param name="spawnPos">생성 위치</param>
    /// <param name="direction">발사 방향 (정규화된 벡터)</param>
    /// <param name="damage">이 탄환의 데미지</param>
    private void SpawnAndFirePellet(GameObject projectilePrefab, Vector3 spawnPos, Vector2 direction, float damage)
    {
        // 투사체 생성
        GameObject pellet = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        
        // ISkillProjectile 인터페이스 구현 확인
        ISkillProjectile projScript = pellet.GetComponent<ISkillProjectile>();
        if (projScript != null)
        {
            // 투사체 데이터 설정
            projScript.SetSkillData(damage, skillData.projectileSpeed);
            
            // 공격자 타입 설정 (팀킬 방지)
            projScript.SetAttackerType(EntityType.Monster);
            
            // 소유자 컨트롤러 설정
            projScript.SetOwnerController(ownerController);
            
            // 관통 여부는 기본값 사용 (SimpleFireball은 false)
            projScript.SetPiercing(false);
        }
        else
        {
            Debug.LogWarning($"[ShotgunHandler] {projectilePrefab.name}에 ISkillProjectile이 구현되지 않았습니다!");
        }
        
        // Rigidbody2D로 발사
        Rigidbody2D rb = pellet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // 방향 * 속도로 발사
            rb.linearVelocity = direction * skillData.projectileSpeed;
        }
        else
        {
            Debug.LogWarning($"[ShotgunHandler] {projectilePrefab.name}에 Rigidbody2D가 없습니다!");
        }
    }
    
    #endregion
}

