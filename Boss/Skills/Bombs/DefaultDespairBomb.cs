using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 기본 절망 폭탄 프리팹 스크립트.
/// - 자체적으로 채워진 원(텔레그래프) 표시 → 폭발 색 전환 → 범위 내 데미지 적용
/// - 핸들러(DespairBombSkillHandler)로부터 전달되는 컨텍스트 값이 > 0 이면 우선 사용, 아니면 프리팹 기본값 사용
/// </summary>
public class DefaultDespairBomb : MonoBehaviour, IDespairBomb
{
	[Header("기본 파라미터 (프리팹 기본값)")]
	[Tooltip("폭발 데미지(기본값). 컨텍스트에서 값이 오면 덮어씁니다")]
	[SerializeField] private int defaultDamage = 10;
	[Tooltip("폭발 반경(유닛, 기본값). 컨텍스트 radius가 0이면 이 값 사용")]
	[SerializeField] private float defaultRadius = 2.0f;
	[Tooltip("경고(텔레그래프) 표시 시간(초, 기본값)")]
	[SerializeField] private float defaultWarningDuration = 0.8f;
	[Tooltip("낙하 연출 대기 시간(초, 기본값). 실제 이동은 선택")]
	[SerializeField] private float defaultFallDuration = 0.2f;
	[Tooltip("폭발 색 유지 시간(초, 기본값)")]
	[SerializeField] private float defaultActiveHoldDuration = 0.15f;
	[Tooltip("경고(대기) 단계 색 (반투명 권장)")]
	[SerializeField] private Color defaultWarningColor = new Color(1f, 0.25f, 0.25f, 0.35f);
	[Tooltip("폭발(피해 적용) 단계 색")]
	[SerializeField] private Color defaultActiveColor = new Color(1f, 0.0f, 0.0f, 0.9f);
	[Tooltip("피해를 적용할 레이어 마스크(기본값)")]
	[SerializeField] private LayerMask defaultDamageLayerMask;

	[Header("몬스터 소환")]
	[Tooltip("폭발 범위 가운데에서 소환할 몬스터 프리팹")]
	[SerializeField] private GameObject monsterPrefab;
	[Tooltip("몬스터 소환 시 Y축 오프셋 (지면 위로 올리기)")]
	[SerializeField] private float monsterSpawnYOffset = 0f;

	[Header("렌더/정렬")]
	[Tooltip("채워진 원 텍스처 해상도(픽셀). 클수록 가장자리 품질↑")]
	[SerializeField] private int filledTextureSize = 128;
	[Tooltip("텔레그래프 SpriteRenderer의 정렬 레이어 이름")]
	[SerializeField] private string sortingLayerName = "";
	[Tooltip("텔레그래프 SpriteRenderer의 정렬 순서")]
	[SerializeField] private int sortingOrder = 0;

	private DespairBombContext ctx;
	private bool ctxSet = false;

	public void Setup(DespairBombContext ctx)
	{
		this.ctx = ctx;
		ctxSet = true;
	}

	public void ArmAt(Vector2 targetPosition, System.Action onCompleted)
	{
		if (!ctxSet)
		{
			Debug.LogWarning("[DefaultDespairBomb] Setup() 없이 ArmAt 호출됨. 기본값으로 진행합니다.");
		}
		StartCoroutine(CoRun(targetPosition, onCompleted));
	}

	private IEnumerator CoRun(Vector2 center, System.Action onCompleted)
	{
		int damage = ctxSet && ctx.damage > 0 ? ctx.damage : defaultDamage;
		float radius = ctxSet && ctx.radius > 0f ? ctx.radius : defaultRadius;
		float warn = ctxSet && ctx.warningDuration > 0f ? ctx.warningDuration : defaultWarningDuration;
		float fall = ctxSet && ctx.fallDuration > 0f ? ctx.fallDuration : defaultFallDuration;
		float hold = ctxSet && ctx.activeHoldDuration > 0f ? ctx.activeHoldDuration : defaultActiveHoldDuration;
		Color warnC = ctxSet && ctx.warningColor.a > 0f ? ctx.warningColor : defaultWarningColor;
		Color activeC = ctxSet && ctx.activeColor.a > 0f ? ctx.activeColor : defaultActiveColor;
		LayerMask dmgMask = ctxSet && ctx.damageLayerMask.value != 0 ? ctx.damageLayerMask : defaultDamageLayerMask;
		EntityType attacker = ctxSet ? ctx.attackerType : EntityType.Monster;

		GameObject telegraph = new GameObject("DespairBomb.Telegraph");
		telegraph.transform.position = new Vector3(center.x, center.y, 0f);
		var sr = telegraph.AddComponent<SpriteRenderer>();
		if (!string.IsNullOrEmpty(sortingLayerName)) sr.sortingLayerName = sortingLayerName;
		sr.sortingOrder = sortingOrder;
		sr.sprite = CreateFilledCircleSprite(Mathf.Max(16, filledTextureSize));
		sr.color = warnC;
		AutoScaleCircularSprite(sr.transform, radius, sr.sprite);

		if (warn > 0f) yield return new WaitForSeconds(warn);
		if (fall > 0f) yield return new WaitForSeconds(fall);

		sr.color = activeC;
		ApplyDamage(center, radius, damage, dmgMask, attacker);
		
		// 폭발 범위 가운데에서 몬스터 소환
		SpawnMonster(center);

		if (hold > 0f) yield return new WaitForSeconds(hold);

		onCompleted?.Invoke();
		Destroy(telegraph);
		Destroy(gameObject);
	}

	private void ApplyDamage(Vector2 center, float radius, int damage, LayerMask mask, EntityType attacker)
	{
		Collider2D[] hits = Physics2D.OverlapCircleAll(center, Mathf.Max(0.01f, radius), mask);
		if (hits == null || hits.Length == 0) return;
		HashSet<EntityController> damaged = new HashSet<EntityController>();
		for (int i = 0; i < hits.Length; i++)
		{
			var h = hits[i];
			if (h == null) continue;
			var entity = h.GetComponentInParent<EntityController>();
			if (entity == null) continue;
			if (damaged.Contains(entity)) continue;
			bool applied = entity.ApplyDamage(damage, attacker, this);
			if (applied) damaged.Add(entity);
		}
	}

	private static Sprite CreateFilledCircleSprite(int size)
	{
		int s = Mathf.Max(16, size);
		var tex = new Texture2D(s, s, TextureFormat.ARGB32, false);
		tex.wrapMode = TextureWrapMode.Clamp;
		tex.filterMode = FilterMode.Bilinear;
		Color32 transparent = new Color32(255, 255, 255, 0);
		Color32 opaque = new Color32(255, 255, 255, 255);
		int cx = s / 2;
		int cy = s / 2;
		float r = (s - 2) * 0.5f;
		float r2 = r * r;
		var pixels = new Color32[s * s];
		for (int y = 0; y < s; y++)
		{
			int yoff = y * s;
			for (int x = 0; x < s; x++)
			{
				float dx = x - cx + 0.5f;
				float dy = y - cy + 0.5f;
				pixels[yoff + x] = (dx * dx + dy * dy <= r2) ? opaque : transparent;
			}
		}
		tex.SetPixels32(pixels);
		tex.Apply(false, false);
		return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
	}

	private void SpawnMonster(Vector2 center)
	{
		if (monsterPrefab == null) return;

		Vector3 spawnPos = new Vector3(center.x, center.y + monsterSpawnYOffset, 0f);
		GameObject monster = Instantiate(monsterPrefab, spawnPos, Quaternion.identity);
		
		Debug.Log($"[DefaultDespairBomb] 폭발 중심에서 몬스터 소환: {monster.name} at {spawnPos}");
	}

	private static void AutoScaleCircularSprite(Transform tr, float targetRadius, Sprite sprite)
	{
		if (sprite == null) return;
		float diameter = Mathf.Max(0.01f, targetRadius * 2f);
		float currentDiameter = sprite.bounds.size.x;
		if (currentDiameter <= 0.0001f) return;
		float scale = diameter / currentDiameter;
		Vector3 s = tr.localScale;
		s.x = scale;
		s.y = scale;
		tr.localScale = s;
	}
}


