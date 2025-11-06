using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 절망폭탄 스킬 핸들러: 플레이어 위치를 스냅샷하여 해당 지점에 폭탄을 투하.
/// - 프리팹에 IDespairBomb 구현이 있으면 그 컴포넌트를 통해 동작
/// - 없으면 핸들러가 기본(데미지만) 폭탄 동작을 수행 (채워진 원 경고 -> 폭발)
/// </summary>
public class DespairBombSkillHandler : MonoBehaviour, SkillHandlerInterface
{
	[Header("폭탄 프리팹")]
	[Tooltip("여기에 폭탄 프리팹을 할당. 프리팹에 IDespairBomb 구현이 있으면 해당 로직 사용")]
	[SerializeField] private GameObject bombPrefab;

	[Header("타겟/스냅샷")]
	[Tooltip("발동 시점의 플레이어 위치를 스냅샷합니다 (끄면 보스 앞쪽으로 오프셋)")]
	[SerializeField] private bool snapshotPlayerPosition = true;
	[Tooltip("스냅샷 실패 시 보스의 우측(앞) 방향으로 이 값만큼 오프셋")]
	[SerializeField] private float fallbackForwardOffset = 0.5f;

	[Header("폴백(데미지만) 설정 - 폭탄 컴포넌트가 없을 때")]
	[Tooltip("폴백 폭탄의 폭발 반경(유닛). 프리팹에 IDespairBomb가 없을 때만 사용")]
	[SerializeField] private float fallbackRadius = 2.0f;
	[Tooltip("경고(텔레그래프) 표시 시간(초)")]
	[SerializeField] private float fallbackWarningDuration = 0.8f;
	[Tooltip("낙하 연출 대기 시간(초). 실제 이동은 없음")]
	[SerializeField] private float fallbackFallDuration = 0.2f;
	[Tooltip("폭발 색으로 전환 후 유지 시간(초)")]
	[SerializeField] private float fallbackActiveHoldDuration = 0.15f;
	[Tooltip("경고(대기) 단계의 색 (반투명 권장)")]
	[SerializeField] private Color fallbackWarningColor = new Color(1f, 0.25f, 0.25f, 0.35f);
	[Tooltip("폭발(피해 적용) 단계의 색")]
	[SerializeField] private Color fallbackActiveColor = new Color(1f, 0.0f, 0.0f, 0.9f);
	[Tooltip("채워진 원 텍스처 해상도(픽셀). 클수록 가장자리 품질↑")]
	[SerializeField] private int filledTextureSize = 128;
	[Tooltip("텔레그래프 SpriteRenderer의 정렬 레이어 이름")]
	[SerializeField] private string sortingLayerName = "";
	[Tooltip("텔레그래프 SpriteRenderer의 정렬 순서")]
	[SerializeField] private int sortingOrder = 0;
	[Tooltip("폭발 데미지를 적용할 대상 레이어 마스크")]
	[SerializeField] private LayerMask damageLayerMask;

	[Header("디버그")]
	[SerializeField] private bool debugLogs = false;

	private MonsterSkillData skillData;
	private GameObject owner;

	public void Setup(BaseSkillData skill, GameObject owner)
	{
		this.owner = owner;
		this.skillData = skill as MonsterSkillData;
	}

	public void Use(SkillRank rank)
	{
		StartCoroutine(CoUse());
	}

	public void Cleanup()
	{
		if (this != null && gameObject != null)
		{
			Destroy(gameObject);
		}
	}

	private IEnumerator CoUse()
	{
		Vector2 center = ResolveTargetPosition();

		// 1) 폭탄 프리팹 경로
		if (bombPrefab != null)
		{
			GameObject bomb = Instantiate(bombPrefab, center, Quaternion.identity);
			var bombComp = bomb.GetComponent<IDespairBomb>();
			if (bombComp != null)
			{
				var ctx = new DespairBombContext
				{
					damage = skillData != null ? Mathf.RoundToInt(skillData.damage) : 0,
					radius = 0f, // 0 => 폭탄 프리팹 기본값 사용
					warningDuration = 0f, // 0 => 프리팹 기본값 사용
					fallDuration = 0f,
					activeHoldDuration = 0f,
					warningColor = new Color(0f, 0f, 0f, 0f),
					activeColor = new Color(0f, 0f, 0f, 0f),
					damageLayerMask = damageLayerMask,
					attackerType = EntityType.Monster,
					owner = owner
				};
				bombComp.Setup(ctx);
				bombComp.ArmAt(center, () => { if (debugLogs) Debug.Log("[DespairBomb] completed"); Cleanup(); });
				yield break;
			}
			else if (debugLogs)
			{
				Debug.LogWarning("[DespairBombSkillHandler] 프리팹에 IDespairBomb 구현이 없어 폴백으로 동작합니다.");
			}
		}

		// 2) 폴백: 채워진 원 경고 -> 폭발 -> 데미지
		yield return RunFallbackBomb(center);
		Cleanup();
	}

	private Vector2 ResolveTargetPosition()
	{
		if (snapshotPlayerPosition)
		{
			var player = PlayerController.Instance != null ? PlayerController.Instance.transform : null;
			if (player != null) return player.position;
			var tagged = GameObject.FindGameObjectWithTag("Player");
			if (tagged != null) return tagged.transform.position;
		}
		if (owner != null)
		{
			Vector2 pos = owner.transform.position;
			Vector2 forward = owner.transform.right;
			return pos + forward * fallbackForwardOffset;
		}
		return transform.position;
	}

	private IEnumerator RunFallbackBomb(Vector2 center)
	{
		float radius = Mathf.Max(0.01f, fallbackRadius);
		int damage = skillData != null ? Mathf.RoundToInt(skillData.damage) : 0;
		float warn = Mathf.Max(0f, fallbackWarningDuration);
		float fall = Mathf.Max(0f, fallbackFallDuration);
		float hold = Mathf.Max(0f, fallbackActiveHoldDuration);

		GameObject telegraph = new GameObject("DespairBomb.Telegraph");
		telegraph.transform.position = new Vector3(center.x, center.y, 0f);
		var sr = telegraph.AddComponent<SpriteRenderer>();
		if (!string.IsNullOrEmpty(sortingLayerName)) sr.sortingLayerName = sortingLayerName;
		sr.sortingOrder = sortingOrder;
		sr.sprite = CreateFilledCircleSprite(Mathf.Max(16, filledTextureSize));
		sr.color = fallbackWarningColor;
		AutoScaleCircularSprite(sr.transform, radius, sr.sprite);

		if (warn > 0f) yield return new WaitForSeconds(warn);
		if (fall > 0f) yield return new WaitForSeconds(fall);

		sr.color = fallbackActiveColor;
		ApplyDamage(center, radius, damage);

		if (hold > 0f) yield return new WaitForSeconds(hold);

		Destroy(telegraph);
	}

	private void ApplyDamage(Vector2 center, float radius, int damage)
	{
		Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, damageLayerMask);
		if (hits == null || hits.Length == 0) return;
		HashSet<EntityController> damaged = new HashSet<EntityController>();
		for (int i = 0; i < hits.Length; i++)
		{
			var h = hits[i];
			if (h == null) continue;
			var entity = h.GetComponentInParent<EntityController>();
			if (entity == null) continue;
			if (damaged.Contains(entity)) continue;
			bool applied = entity.ApplyDamage(damage, EntityType.Monster, this);
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

// 폭탄 프리팹이 구현할 수 있는 인터페이스
public interface IDespairBomb
{
	void Setup(DespairBombContext ctx);
	void ArmAt(Vector2 targetPosition, System.Action onCompleted);
}

public struct DespairBombContext
{
	public int damage;
	public float radius;
	public float warningDuration;
	public float fallDuration;
	public float activeHoldDuration;
	public Color warningColor;
	public Color activeColor;
	public LayerMask damageLayerMask;
	public EntityType attackerType;
	public GameObject owner;
}


