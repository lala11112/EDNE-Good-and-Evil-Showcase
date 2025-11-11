using UnityEngine;
using System.Collections;

/// <summary>
/// 빗발치는 믿음 스킬의 잔상 컨트롤러 (리워크)
/// [설치 모드] 비활성 상태로 맵에 설치됨
/// [폭발 모드] 활성화되면 가까운 적을 향해 돌진 공격
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AfterimageController : MonoBehaviour
{
	#region 기본 설정
	
	[Header("기본 설정")]
	[Tooltip("잔상 데미지")]
	private float damage;
	
	[Tooltip("S랭크 모드 여부")]
	private bool isSRankMode = false;
	
	[Tooltip("S랭크 폭발 범위")]
	[SerializeField] private float sRankExplosionRadius = 3f;
	
	[Tooltip("S랭크 폭발 데미지 배율")]
	[SerializeField] private float sRankExplosionMultiplier = 2f;
	
	[Tooltip("활성화 여부 (폭발 모드 실행 여부)")]
	private bool isActivated = false;
	
	[Tooltip("스킬 슬롯 타입 (용기 시스템용)")]
	private SkillSlotType? skillSlotType = null;
	
	#endregion
	
	#region 폭발 모드 (돌진 공격)
	
	[Header("폭발 모드 설정")]
	[Tooltip("돌진 대상 (가장 가까운 적)")]
	private EntityController targetEnemy;
	
	[Tooltip("돌진 속도")]
	private float rushSpeed = 20f;
	
	[Tooltip("돌진 완료 여부")]
	private bool hasReachedTarget = false;
	
	[Header("Attack2 애니메이션 연출")]
	[Tooltip("공격 애니메이션 활성화 여부")]
	private bool isAttacking = false;
	
	[Tooltip("공격 애니메이션 지속 시간 (초)")]
	[SerializeField] private float attackDuration = 0.5f;
	
	[Tooltip("공격 경과 시간")]
	private float attackElapsedTime = 0f;
	
	#endregion
	
	#region 비주얼
	
	[Header("비주얼")]
	[Tooltip("스프라이트 렌더러")]
	[SerializeField] private SpriteRenderer spriteRenderer;
	
	[Tooltip("애니메이터 (attack2 애니메이션 재생용)")]
	[SerializeField] private Animator animator;
	
	[Tooltip("페이드아웃 시간 (소멸 전)")]
	[SerializeField] private float fadeOutDuration = 0.5f;
	
	[Tooltip("초기 알파값")]
	[SerializeField] private float initialAlpha = 0.5f;
	
	[Tooltip("참격 이펙트 프리팹 (적 위치에 생성)")]
	[SerializeField] private GameObject slashHitEffectPrefab;
	
	[Tooltip("폭발 이펙트 프리팹 (S랭크)")]
	[SerializeField] private GameObject explosionEffectPrefab;
	
	[Header("사운드")]
	[Tooltip("돌진 시작 효과음")]
	[SerializeField] private AudioClip rushStartSound;
	
	[Tooltip("참격 효과음")]
	[SerializeField] private AudioClip slashSound;
	
	private AudioSource audioSource;
	
	#endregion
	
	#region 충돌 처리
	
	private Collider2D col;
	private bool isTriggered = false;
	
	#endregion
	
	#region Unity Lifecycle
	
	private void Awake()
	{
		col = GetComponent<Collider2D>();
		if (col != null)
		{
			col.isTrigger = true;
		}
		
		if (spriteRenderer == null)
		{
			spriteRenderer = GetComponent<SpriteRenderer>();
		}
		
		if (animator == null)
		{
			animator = GetComponent<Animator>();
		}
		
		// AudioSource 자동 할당 또는 추가
		audioSource = GetComponent<AudioSource>();
		if (audioSource == null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.playOnAwake = false;
			audioSource.spatialBlend = 0f; // 2D 사운드
		}
	}
	
	private void OnEnable()
	{
		isTriggered = false;
		isActivated = false;
		hasReachedTarget = false;
		targetEnemy = null;
		isAttacking = false;
		attackElapsedTime = 0f;
		
		// 초기 알파 설정
		if (spriteRenderer != null)
		{
			Color color = spriteRenderer.color;
			color.a = initialAlpha;
			spriteRenderer.color = color;
		}
	}
	
	private void Update()
	{
		// 활성화되지 않았으면 대기 상태 (설치 모드)
		if (!isActivated) return;
		
		// 이미 트리거되었으면 무시
		if (isTriggered) return;
		
		// ⭐ Attack2 애니메이션 연출 중
		if (isAttacking)
		{
			attackElapsedTime += Time.deltaTime;
			
			// 공격 애니메이션 완료
			if (attackElapsedTime >= attackDuration)
			{
				// 데미지 적용 및 파괴
				ExecuteAttack();
			}
			
			return;
		}
		
		// 돌진 대상이 없거나 죽었으면 그냥 사라짐
		if (targetEnemy == null || !targetEnemy.IsAlive)
		{
			Destroy(gameObject);
			return;
		}
		
		// 적을 향해 돌진
		Vector3 direction = (targetEnemy.transform.position - transform.position).normalized;
		transform.position += direction * rushSpeed * Time.deltaTime;
		
		// 적과의 거리 체크
		float distance = Vector3.Distance(transform.position, targetEnemy.transform.position);
		if (distance < 0.5f && !hasReachedTarget)
		{
			hasReachedTarget = true;
			StartAttackAnimation();
		}
	}
	
	#endregion
	
	#region 설정
	
	/// <summary>
	/// 잔상 초기화 (비활성 상태로 설치)
	/// </summary>
	public void SetupAsInactive(float damage, bool isSRankMode, Sprite playerSprite = null, SkillSlotType? skillSlotType = null, RuntimeAnimatorController animatorController = null, AudioClip rushSound = null, AudioClip attackSound = null)
	{
		this.damage = damage;
		this.isSRankMode = isSRankMode;
		this.isActivated = false;
		this.isTriggered = false;
		this.hasReachedTarget = false;
		this.targetEnemy = null;
		this.skillSlotType = skillSlotType;
		
		// 효과음 설정
		this.rushStartSound = rushSound;
		this.slashSound = attackSound;
		
		// 플레이어 스프라이트 복사
		if (spriteRenderer != null && playerSprite != null)
		{
			spriteRenderer.sprite = playerSprite;
			Color color = spriteRenderer.color;
			color.a = initialAlpha;
			spriteRenderer.color = color;
		}
		
		// 플레이어 애니메이터 컨트롤러 복사
		if (animator != null && animatorController != null)
		{
			animator.runtimeAnimatorController = animatorController;
			Debug.Log("[AfterimageController] 애니메이터 컨트롤러 복사 완료");
		}
		
		// 콜라이더 비활성화 (설치 모드에서는 충돌 안 함)
		if (col != null)
		{
			col.enabled = false;
		}
	}
	
	/// <summary>
	/// 폭발 모드 활성화: 일정 거리 내 가장 가까운 적을 찾아 돌진
	/// </summary>
	public void ActivateAndAttack(float attackRange, float rushSpeed)
	{
		this.rushSpeed = rushSpeed;
		this.isActivated = true;
		
		// 콜라이더 활성화 (돌진 중 충돌 감지)
		if (col != null)
		{
			col.enabled = true;
		}
		
		// 일정 거리 내 가장 가까운 적 찾기
		targetEnemy = FindNearestEnemy(attackRange);
		
		if (targetEnemy == null)
		{
			// 공격할 적이 없으면 그냥 사라짐
			Debug.Log($"[AfterimageController] 공격 범위 내 적 없음. 잔상 제거.");
			Destroy(gameObject, 0.5f);
			return;
		}
		
		// ⭐ 돌진 시작 효과음 재생
		if (rushStartSound != null && audioSource != null)
		{
			audioSource.PlayOneShot(rushStartSound);
			Debug.Log($"[AfterimageController] 🔊 돌진 효과음 재생!");
		}
		
		Debug.Log($"[AfterimageController] 💥 활성화! 대상: {targetEnemy.name}");
	}
	
	/// <summary>
	/// 스킬 슬롯 타입 가져오기 (용기 시스템용)
	/// </summary>
	public SkillSlotType? GetSkillSlotType()
	{
		return skillSlotType;
	}
	
	/// <summary>
	/// 일정 거리 내 가장 가까운 적 찾기
	/// </summary>
	private EntityController FindNearestEnemy(float range)
	{
		Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
		
		EntityController nearestEnemy = null;
		float nearestDistance = float.MaxValue;
		
		foreach (Collider2D hit in hits)
		{
			EntityController entity = hit.GetComponent<EntityController>();
			if (entity == null) continue;
			if (entity.EntityType == EntityType.Player) continue;
			if (!entity.IsAlive) continue;
			
			float distance = Vector3.Distance(transform.position, entity.transform.position);
			if (distance < nearestDistance)
			{
				nearestDistance = distance;
				nearestEnemy = entity;
			}
		}
		
		return nearestEnemy;
	}
	
	#endregion
	
	#region 충돌 감지
	
	private void OnTriggerEnter2D(Collider2D other)
	{
		// 활성화되지 않았으면 충돌 무시
		if (!isActivated) return;
		
		if (isTriggered) return;
		
		// EntityController 확인
		EntityController entity = other.GetComponent<EntityController>();
		if (entity == null) return;
		
		// 플레이어는 무시
		if (entity.EntityType == EntityType.Player) return;
		
		// 죽은 적은 무시
		if (!entity.IsAlive) return;
		
		// 잔상 공격 (돌진 중 충돌 - 바로 attack2 애니메이션 시작)
		StartAttackAnimation();
	}
	
	#endregion
	
	#region 잔상 공격
	
	/// <summary>
	/// Attack2 애니메이션 시작
	/// </summary>
	private void StartAttackAnimation()
	{
		if (isTriggered || isAttacking) return;
		
		isAttacking = true;
		attackElapsedTime = 0f;
		
		// ⭐ 참격 효과음 재생
		if (slashSound != null && audioSource != null)
		{
			audioSource.PlayOneShot(slashSound);
			Debug.Log($"[AfterimageController] 🔊 참격 효과음 재생!");
		}
		
		// Attack2 애니메이션 트리거
		if (animator != null)
		{
			animator.SetTrigger("Attack2");
			Debug.Log($"[AfterimageController] ⚔️ Attack2 애니메이션 시작!");
		}
	}
	
	/// <summary>
	/// Attack2 애니메이션 완료 후 데미지 적용
	/// </summary>
	private void ExecuteAttack()
	{
		if (isTriggered) return;
		isTriggered = true;
		
		if (isSRankMode)
		{
			// S랭크: 원형 범위 폭발
			ExecuteExplosion();
		}
		else
		{
			// 일반: 단일 타겟 데미지
			if (targetEnemy != null && targetEnemy.IsAlive)
			{
				// ⭐ 적 위치에 참격 이펙트 생성
				SpawnSlashHitEffect(targetEnemy.transform.position);
				
				// ⭐ 자기 자신을 데미지 소스로 전달 (용기 시스템용)
				targetEnemy.ApplyDamage((int)damage, EntityType.Player, this);
				Debug.Log($"[AfterimageController] ⚔️ Attack2 완료! {targetEnemy.name}에게 {(int)damage} 데미지!");
			}
		}
		
		// 잔상 파괴
		Destroy(gameObject);
	}
	
	/// <summary>
	/// 적 위치에 참격 이펙트 생성
	/// </summary>
	private void SpawnSlashHitEffect(Vector3 position)
	{
		if (slashHitEffectPrefab == null) return;
		
		GameObject effect = Instantiate(slashHitEffectPrefab, position, Quaternion.identity);
		Destroy(effect, 1f); // 1초 후 자동 파괴
		
		Debug.Log($"[AfterimageController] 💥 참격 이펙트 생성!");
	}
	
	/// <summary>
	/// S랭크 폭발 실행
	/// </summary>
	private void ExecuteExplosion()
	{
		// 폭발 범위 내 모든 적 탐색
		Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, sRankExplosionRadius);
		
		float explosionDamage = damage * sRankExplosionMultiplier;
		
		foreach (Collider2D hit in hits)
		{
			EntityController entity = hit.GetComponent<EntityController>();
			if (entity != null && entity.EntityType != EntityType.Player && entity.IsAlive)
			{
				// ⭐ 적 위치에 참격 이펙트 생성
				SpawnSlashHitEffect(entity.transform.position);
				
				// ⭐ 자기 자신을 데미지 소스로 전달 (용기 시스템용)
				entity.ApplyDamage((int)explosionDamage, EntityType.Player, this);
			}
		}
		
		// 폭발 이펙트 생성
		if (explosionEffectPrefab != null)
		{
			Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
		}
	}
	
	#endregion
	
	#region 디버그
	
#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		if (isSRankMode)
		{
			// S랭크 폭발 범위 표시
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(transform.position, sRankExplosionRadius);
		}
	}
#endif
	
	#endregion
}

