using UnityEngine;

/// <summary>
/// 몬스터 파이어볼 스킬 핸들러
/// SkillHandlerInterface를 구현하여 MonsterController에서 사용 가능
/// </summary>
public class MonsterFireballHandler : MonoBehaviour, SkillHandlerInterface
{
    [Header("스킬 설정")]
    [SerializeField] private GameObject fireballPrefab; // 파이어볼 투사체 프리팹
	[Tooltip("Reinforced Application: 한 방향으로 3연발 탄막 발사")]
	[SerializeField] private bool reinforcedApplication = false;
	[Tooltip("3연발 간격 (월드 유닛, 진행 방향의 수직 방향으로 좌우 오프셋)")]
	[SerializeField] private float multiShotSpacing = 0.25f;
    
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
        
        Debug.Log($"[MonsterFireballHandler] Setup 완료 - Damage: {skillData?.damage}");
    }
    
    /// <summary>
    /// 스킬 사용 (랭크는 몬스터에서 무시)
    /// </summary>
    public void Use(SkillRank rank)
    {
        if (skillData == null || ownerController == null)
        {
            Debug.LogError("[MonsterFireballHandler] 스킬 데이터 또는 소유자가 없습니다!");
            Cleanup();
            return;
        }
        
        if (target == null)
        {
            Debug.LogWarning("[MonsterFireballHandler] 타겟이 없습니다!");
            Cleanup();
            return;
        }
        
		// 파이어볼 발사
		ShootFireball();
        
        // 스킬 종료 (즉시 발사 후 종료)
        Cleanup();
    }
    
    /// <summary>
    /// 스킬 정리 (종료 시 호출)
    /// </summary>
    public void Cleanup()
    {
        Debug.Log("[MonsterFireballHandler] Cleanup 호출됨");
        Destroy(gameObject);
    }
    
    #endregion
    
    #region 스킬 로직
    
    /// <summary>
    /// 파이어볼 발사
    /// </summary>
	private void ShootFireball()
    {
        // 투사체 프리팹 확인
        GameObject projectilePrefab = fireballPrefab != null ? fireballPrefab : skillData.skillEffectPrefab;
        
        if (projectilePrefab == null)
        {
            Debug.LogError("[MonsterFireballHandler] 파이어볼 프리팹이 없습니다!");
            return;
        }
        
        // 발사 위치 (몬스터 위치)
        Vector3 spawnPos = ownerController.transform.position;
        
		Vector2 direction = (target.position - spawnPos).normalized;
		if (direction.sqrMagnitude < 0.000001f) direction = Vector2.right;

		if (!reinforcedApplication)
		{
			SpawnAndFire(projectilePrefab, spawnPos, direction);
			Debug.Log($"[MonsterFireballHandler] 파이어볼 발사! Damage: {skillData.damage}, Speed: {skillData.projectileSpeed}");
		}
		else
		{
			// 3연발: 진행 방향의 수직 벡터로 좌/중/우 오프셋
			Vector2 perp = new Vector2(-direction.y, direction.x); // 좌측 수직
			Vector3 leftPos = spawnPos + (Vector3)(perp * -multiShotSpacing);
			Vector3 midPos = spawnPos;
			Vector3 rightPos = spawnPos + (Vector3)(perp * multiShotSpacing);

			SpawnAndFire(projectilePrefab, leftPos, direction);
			SpawnAndFire(projectilePrefab, midPos, direction);
			SpawnAndFire(projectilePrefab, rightPos, direction);

			Debug.Log($"[MonsterFireballHandler] Reinforced 3연발 발사! Damage: {skillData.damage}, Spacing: {multiShotSpacing}");
		}
    }

	private void SpawnAndFire(GameObject projectilePrefab, Vector3 spawnPos, Vector2 direction)
	{
		GameObject fireball = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
		ISkillProjectile projScript = fireball.GetComponent<ISkillProjectile>();
		if (projScript != null)
		{
			projScript.SetSkillData(skillData.damage, skillData.projectileSpeed);
			projScript.SetAttackerType(EntityType.Monster);
			projScript.SetOwnerController(ownerController);
		}
		Rigidbody2D rb = fireball.GetComponent<Rigidbody2D>();
		if (rb != null)
		{
			rb.linearVelocity = direction.normalized * skillData.projectileSpeed;
		}
	}
    
    #endregion
}

