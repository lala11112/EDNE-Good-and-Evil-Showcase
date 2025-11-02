using UnityEngine;
using System.Collections;

/// <summary>
/// 빗발치는 믿음 스킬의 잔상 컨트롤러
/// 적이 밟으면 데미지를 주고 사라집니다
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
	
	#endregion
	
	#region 생명 주기
	
	[Header("생명 주기")]
	[Tooltip("잔상 지속 시간")]
	private float lifetime;
	
	private float remainingLifetime;
	
	#endregion
	
	#region 비주얼
	
	[Header("비주얼")]
	[Tooltip("스프라이트 렌더러")]
	[SerializeField] private SpriteRenderer spriteRenderer;
	
	[Tooltip("페이드아웃 시간 (소멸 전)")]
	[SerializeField] private float fadeOutDuration = 0.5f;
	
	[Tooltip("초기 알파값")]
	[SerializeField] private float initialAlpha = 0.5f;
	
	[Tooltip("폭발 이펙트 프리팹 (S랭크)")]
	[SerializeField] private GameObject explosionEffectPrefab;
	
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
	}
	
	private void OnEnable()
	{
		isTriggered = false;
		remainingLifetime = lifetime;
		
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
		if (isTriggered) return;
		
		remainingLifetime -= Time.deltaTime;
		
		// 페이드아웃 시작
		if (remainingLifetime <= fadeOutDuration && spriteRenderer != null)
		{
			float alpha = Mathf.Lerp(0f, initialAlpha, remainingLifetime / fadeOutDuration);
			Color color = spriteRenderer.color;
			color.a = alpha;
			spriteRenderer.color = color;
		}
		
		// 수명 종료
		if (remainingLifetime <= 0f)
		{
			Destroy(gameObject);
		}
	}
	
	#endregion
	
	#region 설정
	
	/// <summary>
	/// 잔상 초기화
	/// </summary>
	public void Setup(float damage, float lifetime, bool isSRankMode, Sprite playerSprite = null)
	{
		this.damage = damage;
		this.lifetime = lifetime;
		this.remainingLifetime = lifetime;
		this.isSRankMode = isSRankMode;
		
		// 플레이어 스프라이트 복사
		if (spriteRenderer != null && playerSprite != null)
		{
			spriteRenderer.sprite = playerSprite;
			Color color = spriteRenderer.color;
			color.a = initialAlpha;
			spriteRenderer.color = color;
		}
	}
	
	#endregion
	
	#region 충돌 감지
	
	private void OnTriggerEnter2D(Collider2D other)
	{
		if (isTriggered) return;
		
		// EntityController 확인
		EntityController entity = other.GetComponent<EntityController>();
		if (entity == null) return;
		
		// 플레이어는 무시
		if (entity.EntityType == EntityType.Player) return;
		
		// 죽은 적은 무시
		if (!entity.IsAlive) return;
		
		// 잔상 발동
		TriggerAfterimage(entity);
	}
	
	#endregion
	
	#region 잔상 발동
	
	/// <summary>
	/// 잔상 발동 (적이 밟음)
	/// </summary>
	private void TriggerAfterimage(EntityController triggeringEnemy)
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
			triggeringEnemy.ApplyDamage((int)damage, EntityType.Player);
		}
		
		// 잔상 파괴
		Destroy(gameObject);
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
				entity.ApplyDamage((int)explosionDamage, EntityType.Player);
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

