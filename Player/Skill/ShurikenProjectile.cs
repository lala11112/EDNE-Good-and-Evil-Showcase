using UnityEngine;
using System.Collections;

/// <summary>
/// 신념질주 스킬용 수리검 프로젝타일
/// 적에게 낮은 데미지를 주고 표식을 남깁니다
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ShurikenProjectile : MonoBehaviour, ISkillProjectile
{
	#region 컴포넌트
	
	[Tooltip("Rigidbody2D 컴포넌트")]
	private Rigidbody2D rb;
	
	[Tooltip("Collider2D 컴포넌트")]
	private Collider2D col;
	
	[Tooltip("스프라이트 렌더러 (회전 연출용)")]
	[SerializeField] private SpriteRenderer spriteRenderer;
	
	#endregion
	
	#region 스킬 데이터
	
	[Header("스킬 데이터")]
	[Tooltip("데미지 (Setup에서 설정)")]
	private float damage;
	
	[Tooltip("비행 속도")]
	private float speed;
	
	[Tooltip("공격자 타입 (Player/Monster)")]
	private EntityType attackerType;
	
	[Tooltip("스킬 소유자 (PlayerController)")]
	private MonoBehaviour ownerController;
	
	[Tooltip("관통 여부")]
	private bool isPiercing = false;
	
	[Tooltip("스킬 슬롯 타입 (용기 시스템용)")]
	private SkillSlotType? skillSlotType = null;
	
	#endregion
	
	#region 표식 설정
	
	[Header("표식 효과")]
	[Tooltip("적에게 적용할 표식 상태효과")]
	[SerializeField] private StatusEffectDataSO markEffect;
	
	#endregion
	
	#region 비주얼
	
	[Header("비주얼 설정")]
	[Tooltip("수리검 회전 속도 (도/초)")]
	[SerializeField] private float rotationSpeed = 720f;
	
	[Tooltip("최대 생존 시간 (벽에 박히지 않았을 때)")]
	[SerializeField] private float maxLifetime = 5f;
	
	#endregion
	
	#region 프로젝타일 설정
	
	[Header("프로젝타일 설정")]
	[Tooltip("충돌 레이어 (적 레이어)")]
	[SerializeField] private LayerMask targetLayer;
	
	[Tooltip("벽/장애물 레이어 (충돌 시 파괴)")]
	[SerializeField] private LayerMask obstacleLayer;
	
	#endregion
	
	#region Unity Lifecycle
	
	private void Awake()
	{
		// 컴포넌트 자동 할당
		rb = GetComponent<Rigidbody2D>();
		col = GetComponent<Collider2D>();
		
		if (spriteRenderer == null)
		{
			spriteRenderer = GetComponent<SpriteRenderer>();
		}
		
		// Rigidbody 설정
		if (rb != null)
		{
			rb.gravityScale = 0f;
			rb.bodyType = RigidbodyType2D.Kinematic;
		}
		
		// Collider를 Trigger로 설정
		if (col != null)
		{
			col.isTrigger = true;
		}
	}
	
	private void OnEnable()
	{
		// 최대 생존 시간 후 자동 파괴
		StartCoroutine(LifetimeTimer());
	}
	
	private void Update()
	{
		// 수리검 회전 연출
		if (spriteRenderer != null)
		{
			transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
		}
	}
	
	#endregion
	
	#region ISkillProjectile 구현
	
	/// <summary>
	/// 스킬 데이터 설정
	/// </summary>
	public void SetSkillData(float damage, float speed)
	{
		this.damage = damage;
		this.speed = speed;
		
		// 발사 시작
		if (rb != null)
		{
			rb.linearVelocity = transform.right * speed;
		}
	}
	
	/// <summary>
	/// 공격자 타입 설정
	/// </summary>
	public void SetAttackerType(EntityType attackerType)
	{
		this.attackerType = attackerType;
	}
	
	/// <summary>
	/// 스킬 소유자 설정
	/// </summary>
	public void SetOwnerController(MonoBehaviour owner)
	{
		this.ownerController = owner;
	}
	
	/// <summary>
	/// 관통 여부 설정
	/// </summary>
	public void SetPiercing(bool isPiercing)
	{
		this.isPiercing = isPiercing;
	}
	
	/// <summary>
	/// 스킬 슬롯 타입 설정 (용기 시스템용)
	/// </summary>
	public void SetSkillSlotType(SkillSlotType slotType)
	{
		this.skillSlotType = slotType;
	}
	
	/// <summary>
	/// 스킬 슬롯 타입 가져오기 (ISkillProjectile 인터페이스 구현)
	/// </summary>
	public SkillSlotType? GetSkillSlotType()
	{
		return skillSlotType;
	}
	
	#endregion
	
	#region 표식 설정
	
	/// <summary>
	/// 표식 효과 설정 (외부에서 호출)
	/// </summary>
	public void SetMarkEffect(StatusEffectDataSO mark)
	{
		this.markEffect = mark;
	}
	
	#endregion
	
	#region 충돌 처리
	
	private void OnTriggerEnter2D(Collider2D other)
	{
		// 벽이나 장애물 레이어에 충돌 시 파괴
		if (obstacleLayer != 0 && ((1 << other.gameObject.layer) & obstacleLayer) != 0)
		{
			DestroyProjectile();
			return;
		}
		
		// 적 감지
		EntityController target = other.GetComponent<EntityController>();
		if (target == null) return;
		
		// 팀킬 방지: 같은 타입이면 무시
		if (target.EntityType == attackerType) return;
		
		// 이미 죽은 적은 무시
		if (!target.IsAlive) return;
		
		// 데미지 적용 (프로젝타일 자체를 데미지 소스로 전달)
		target.ApplyDamage((int)damage, attackerType, this);
		
		// 표식 적용
		if (markEffect != null)
		{
			target.ApplyStatusEffect(markEffect);
		}
		
		// 관통이 아니면 파괴
		if (!isPiercing)
		{
			DestroyProjectile();
		}
	}
	
	#endregion
	
	#region 생명 주기
	
	/// <summary>
	/// 최대 생존 시간 타이머
	/// </summary>
	private IEnumerator LifetimeTimer()
	{
		yield return new WaitForSeconds(maxLifetime);
		DestroyProjectile();
	}
	
	/// <summary>
	/// 프로젝타일 파괴
	/// </summary>
	private void DestroyProjectile()
	{
		// TODO: 파괴 이펙트 생성
		Destroy(gameObject);
	}
	
	#endregion
}

