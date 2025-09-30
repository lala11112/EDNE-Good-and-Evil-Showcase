using UnityEngine;

public class SlashSkillHandler : MonoBehaviour, SkillHandlerInterface
{
	private BaseSkillData data;	// SO에서 전달된 스킬 데이터
	private GameObject owner;	// 스킬 소유자(플레이어 등)

	// [AOE 설정]
	// - aoePrefab: 선택 사항(비주얼/이펙트용). CircleCollider2D(Trigger)가 붙어있으면 사용, 없으면 자동 추가.
	// - aoeRadius: 원형 범위 반경
	// - aoeDuration: 유지 시간(초)
	// - spawnLocalOffset: 오너 기준 생성 위치 보정(x: 오른쪽, y: 위)
	// - attachToOwner: true면 지속 시간 동안 오너를 따라다님
	[SerializeField] private GameObject aoePrefab;
	[SerializeField] private float aoeRadius = 1.5f;
	[SerializeField] private float aoeDuration = 0.5f;
	[SerializeField] private Vector2 spawnLocalOffset = Vector2.zero;
	[SerializeField] private bool attachToOwner = false;

	public void Setup(BaseSkillData data, GameObject owner)
	{
		this.data = data;
		this.owner = owner;
	}

	public void Use(SkillRank rank)
	{
		// 1) 생성 위치 계산(2D)
		var ownerTf = owner != null ? owner.transform : transform;
		Vector3 spawnPos = ownerTf.position + ownerTf.TransformVector(new Vector3(spawnLocalOffset.x, spawnLocalOffset.y, 0f));

		// 2) AOE 생성(프리팹이 있으면 사용, 없으면 런타임 생성)
		GameObject aoeGO = null;

		if (aoePrefab != null)
		{
			aoeGO = Instantiate(aoePrefab, spawnPos, Quaternion.identity);

			// CircleCollider2D 보장 및 반경 설정
			if (!aoeGO.TryGetComponent<CircleCollider2D>(out var cc2d))
			{
				cc2d = aoeGO.AddComponent<CircleCollider2D>();
				cc2d.isTrigger = true;
			}
			cc2d.radius = aoeRadius;
		}
		else
		{
			aoeGO = new GameObject("Slash_AOE");
			aoeGO.transform.position = spawnPos;

			var cc2d = aoeGO.AddComponent<CircleCollider2D>();
			cc2d.isTrigger = true;
			cc2d.radius = aoeRadius;

			// 필요 시 간단한 비주얼을 붙이고 싶다면 SpriteRenderer를 추가하세요.
			// var sr = aoeGO.AddComponent<SpriteRenderer>(); ...
		}

		// 3) 데미지 처리 스크립트 추가
		if (aoeGO != null)
		{
			var damageHandler = aoeGO.AddComponent<SkillDamageHandler>();
			damageHandler.Setup(data, owner, rank);
		}

		// 4) 오너를 따라가게 할지 여부
		if (attachToOwner && aoeGO != null)
		{
			aoeGO.transform.SetParent(ownerTf, true);
		}

		// 5) 일정 시간 후 제거
		Destroy(aoeGO, aoeDuration);
	}
}

// 스킬 데미지 처리 컴포넌트
public class SkillDamageHandler : MonoBehaviour
{
	private BaseSkillData skillData;
	private GameObject skillOwner;
	private SkillRank skillRank;

	public void Setup(BaseSkillData data, GameObject owner, SkillRank rank)
	{
		skillData = data;
		skillOwner = owner;
		skillRank = rank;
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		// 자신이거나 스킬 소유자와 충돌한 경우 무시
		if (other.gameObject == skillOwner || other.gameObject == gameObject)
			return;

		// 데미지를 받을 수 있는 객체인지 확인
		var damageable = other.GetComponent<IDamageable>();
		if (damageable == null || !damageable.IsAlive)
			return;

		// 데미지 계산
		int damage = GetDamage();
		
		// 객체에게 데미지 적용
		damageable.TakeDamage(damage);
		
		Debug.Log($"[{skillData.skillName}] {other.name}에게 {damage} 데미지!");
	}

	private int GetDamage()
	{
		// Test01ZSkillData 타입인지 확인
		if (skillData is Test01ZSkillData testSkill)
		{
			return testSkill.attackDamage;
		}
		
		// Test02ZSkillData 타입인지 확인
		if (skillData is Test02ZSkillData testSkill2)
		{
			return testSkill2.attackDamage;
		}
		
		// 기본값 반환
		return 50;
	}
}
