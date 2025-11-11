using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 위치에서 플레이어를 조준해 파이어볼을 연달아 발사하는 핸들러
/// - 핸들러 프리팹 방식 (SkillHandlerInterface)
/// - 각 발사 시점의 플레이어 위치를 기준으로 방향 재계산
/// - 인스펙터에서 발사 수/간격 조절
/// </summary>
public class FiaballTargetedBurstShotHandler : MonoBehaviour, SkillHandlerInterface
{
	[Header("Fiaball 프리팹")]
	[Tooltip("명시하면 이 프리팹을 사용. 비어있으면 스킬 데이터의 skillEffectPrefab 사용")]
	[SerializeField] private GameObject fiaballPrefab;

	[Header("버스트 설정")]
	[Tooltip("한 번 사용 시 연달아 발사할 탄 수")]
	[SerializeField] private int burstCount = 5;
	[Tooltip("연달아 발사할 때의 간격(초)")]
	[SerializeField] private float interShotInterval = 0.15f;

	[Header("프로젝타일 설정")]
	[Tooltip("ISkillProjectile에 설정할 관통 여부")]
	[SerializeField] private bool isPiercing = false;

	[Header("샷 패턴")]
	[Tooltip("체크 시 한 번에 세 갈래로 발사합니다")]
	[SerializeField] private bool useTripleShot = false;
	[Tooltip("세 갈래 발사 시 좌우 벌어짐 각도(도 단위)")]
	[SerializeField] private float tripleSpreadDegrees = 18f;

	[Header("사전 경고(텔레그래프)")]
	[Tooltip("각 탄을 발사하기 전에 방향 경고바를 표시합니다")]
	[SerializeField] private bool showTelegraphPerShot = true;
	[Tooltip("경고바 표시 시간(초). 이 시간이 지난 뒤 실제 발사")]
	[SerializeField] private float telegraphDuration = 0.25f;
	[Tooltip("경고바 길이(월드 단위)")]
	[SerializeField] private float telegraphLength = 3f;
	[Tooltip("경고바 두께(월드 단위)")]
	[SerializeField] private float telegraphThickness = 0.12f;
	[Tooltip("경고바 색상")]
	[SerializeField] private Color telegraphColor = new Color(1f, 0f, 0f, 1f);
	[Tooltip("경고바 스프라이트 프리팹(선택). 비우면 LineRenderer 사용. X=길이, Y=두께 기준")]
	[SerializeField] private GameObject telegraphBarPrefab;

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
		// 플레이어 찾기
		Transform player = null;
		if (PlayerController.Instance != null) player = PlayerController.Instance.transform;
		if (player == null)
		{
			var go = GameObject.FindWithTag("Player");
			if (go != null) player = go.transform;
		}
		if (player == null)
		{
			if (debugLogs) Debug.LogWarning("[FiaballTargetedBurstShotHandler] Player를 찾지 못했습니다.");
			Cleanup();
			yield break;
		}

		GameObject projPrefab = fiaballPrefab != null ? fiaballPrefab : (skillData != null ? skillData.skillEffectPrefab : null);
		if (projPrefab == null)
		{
			Debug.LogError("[FiaballTargetedBurstShotHandler] Fiaball 프리팹이 설정되지 않았습니다.");
			Cleanup();
			yield break;
		}

		int shots = Mathf.Max(1, burstCount);
		float speed = skillData != null ? skillData.projectileSpeed : 10f;
		int damage = skillData != null ? Mathf.RoundToInt(skillData.damage) : 10;

		for (int i = 0; i < shots; i++)
		{
			// 스폰 위치: 핸들러(또는 오너) 위치
			Vector3 spawnPos = transform.position;
			if (owner != null) spawnPos = owner.transform.position;

			// 현재 타겟 방향 "스냅샷(고정)" 계산: 텔레그래프 시작 시점의 위치/방향을 저장
			Vector2 rbPos = spawnPos;
			Vector2 lockedTargetPos = (player != null) ? (Vector2)player.position : rbPos;
			Vector2 lockedAimDir = (lockedTargetPos - rbPos);
			if (lockedAimDir.sqrMagnitude < 0.0001f) lockedAimDir = Vector2.right; else lockedAimDir.Normalize();

			// 경고바 표시 후 발사
			if (showTelegraphPerShot && telegraphDuration > 0f)
			{
				if (useTripleShot && tripleSpreadDegrees > 0.01f)
				{
					yield return StartCoroutine(ShowTelegraphFan(spawnPos, lockedAimDir, tripleSpreadDegrees, telegraphDuration));
				}
				else
				{
					yield return StartCoroutine(ShowTelegraph(spawnPos, lockedAimDir, telegraphDuration));
				}
			}

			if (useTripleShot && tripleSpreadDegrees > 0.01f)
			{
				Vector2 left = Rotate(lockedAimDir, -tripleSpreadDegrees);
				Vector2 right = Rotate(lockedAimDir, tripleSpreadDegrees);
				SpawnProjectile(projPrefab, spawnPos, left, speed, damage);
				SpawnProjectile(projPrefab, spawnPos, lockedAimDir, speed, damage);
				SpawnProjectile(projPrefab, spawnPos, right, speed, damage);
			}
			else
			{
				SpawnProjectile(projPrefab, spawnPos, lockedAimDir, speed, damage);
			}

			if (interShotInterval > 0f && i < shots - 1)
			{
				yield return new WaitForSeconds(interShotInterval);
			}
		}

		Cleanup();
	}

	private void SpawnProjectile(GameObject prefab, Vector3 spawnPos, Vector2 dir, float speed, int damage)
	{
		GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
		var projectile = obj.GetComponent<ISkillProjectile>();
		if (projectile != null)
		{
			projectile.SetSkillData(damage, speed);
			projectile.SetAttackerType(EntityType.Monster);
			projectile.SetPiercing(isPiercing);
		}

		var rb = obj.GetComponent<Rigidbody2D>();
		if (rb != null)
		{
			rb.linearVelocity = dir.normalized * Mathf.Max(0.01f, speed);
		}
	}

	private static Vector2 Rotate(Vector2 v, float degrees)
	{
		float rad = degrees * Mathf.Deg2Rad;
		float cos = Mathf.Cos(rad);
		float sin = Mathf.Sin(rad);
		return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
	}

	private IEnumerator ShowTelegraph(Vector3 start, Vector2 dir, float duration)
	{
		Vector3 end = start + (Vector3)(dir * Mathf.Max(0.01f, telegraphLength));
		if (telegraphBarPrefab != null)
		{
			Vector3 mid = (start + end) * 0.5f;
			float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
			var go = Instantiate(telegraphBarPrefab, mid, Quaternion.Euler(0f, 0f, angle));
			// 스케일(프리팹 기준 X=길이, Y=두께 가정)
			var tr = go.transform;
			Vector3 s = tr.localScale;
			s.x = telegraphLength;
			s.y = telegraphThickness;
			tr.localScale = s;
			// 색
			var sr = go.GetComponentInChildren<SpriteRenderer>();
			if (sr != null) sr.color = telegraphColor;
			yield return new WaitForSeconds(duration);
			if (go != null) Destroy(go);
		}
		else
		{
			var go = new GameObject("FiaballTelegraph");
			var lr = go.AddComponent<LineRenderer>();
			lr.positionCount = 2;
			lr.useWorldSpace = true;
			lr.startWidth = telegraphThickness;
			lr.endWidth = telegraphThickness;
			lr.material = new Material(Shader.Find("Sprites/Default"));
			lr.startColor = telegraphColor;
			lr.endColor = telegraphColor;
			lr.sortingOrder = 3000;
			lr.SetPosition(0, start);
			lr.SetPosition(1, end);
			yield return new WaitForSeconds(duration);
			if (go != null) Destroy(go);
		}
	}

	private IEnumerator ShowTelegraphFan(Vector3 start, Vector2 centerDir, float spreadDeg, float duration)
	{
		Vector2 leftDir = Rotate(centerDir.normalized, -spreadDeg);
		Vector2 rightDir = Rotate(centerDir.normalized, spreadDeg);

		// 프리팹 존재 시 프리팹 3개, 아니면 LineRenderer 3개를 만들어 동시에 표시
		GameObject leftGo = null, centerGo = null, rightGo = null;
		if (telegraphBarPrefab != null)
		{
			// Center
			{
				Vector3 end = start + (Vector3)(centerDir.normalized * Mathf.Max(0.01f, telegraphLength));
				Vector3 mid = (start + end) * 0.5f;
				float angle = Mathf.Atan2(centerDir.y, centerDir.x) * Mathf.Rad2Deg;
				centerGo = Instantiate(telegraphBarPrefab, mid, Quaternion.Euler(0f, 0f, angle));
				var tr = centerGo.transform;
				Vector3 s = tr.localScale;
				s.x = telegraphLength;
				s.y = telegraphThickness;
				tr.localScale = s;
				var sr = centerGo.GetComponentInChildren<SpriteRenderer>();
				if (sr != null) sr.color = telegraphColor;
			}
			// Left
			{
				Vector3 end = start + (Vector3)(leftDir * Mathf.Max(0.01f, telegraphLength));
				Vector3 mid = (start + end) * 0.5f;
				float angle = Mathf.Atan2(leftDir.y, leftDir.x) * Mathf.Rad2Deg;
				leftGo = Instantiate(telegraphBarPrefab, mid, Quaternion.Euler(0f, 0f, angle));
				var tr = leftGo.transform;
				Vector3 s = tr.localScale;
				s.x = telegraphLength;
				s.y = telegraphThickness;
				tr.localScale = s;
				var sr = leftGo.GetComponentInChildren<SpriteRenderer>();
				if (sr != null) sr.color = telegraphColor;
			}
			// Right
			{
				Vector3 end = start + (Vector3)(rightDir * Mathf.Max(0.01f, telegraphLength));
				Vector3 mid = (start + end) * 0.5f;
				float angle = Mathf.Atan2(rightDir.y, rightDir.x) * Mathf.Rad2Deg;
				rightGo = Instantiate(telegraphBarPrefab, mid, Quaternion.Euler(0f, 0f, angle));
				var tr = rightGo.transform;
				Vector3 s = tr.localScale;
				s.x = telegraphLength;
				s.y = telegraphThickness;
				tr.localScale = s;
				var sr = rightGo.GetComponentInChildren<SpriteRenderer>();
				if (sr != null) sr.color = telegraphColor;
			}

			yield return new WaitForSeconds(duration);

			if (leftGo != null) Destroy(leftGo);
			if (centerGo != null) Destroy(centerGo);
			if (rightGo != null) Destroy(rightGo);
		}
		else
		{
			var left = new GameObject("FiaballTelegraphLeft");
			var center = new GameObject("FiaballTelegraphCenter");
			var right = new GameObject("FiaballTelegraphRight");

			LineRenderer lrL = left.AddComponent<LineRenderer>();
			LineRenderer lrC = center.AddComponent<LineRenderer>();
			LineRenderer lrR = right.AddComponent<LineRenderer>();

			foreach (var lr in new[] { lrL, lrC, lrR })
			{
				lr.positionCount = 2;
				lr.useWorldSpace = true;
				lr.startWidth = telegraphThickness;
				lr.endWidth = telegraphThickness;
				lr.material = new Material(Shader.Find("Sprites/Default"));
				lr.startColor = telegraphColor;
				lr.endColor = telegraphColor;
				lr.sortingOrder = 3000;
			}

			lrC.SetPosition(0, start);
			lrC.SetPosition(1, start + (Vector3)(centerDir.normalized * Mathf.Max(0.01f, telegraphLength)));

			lrL.SetPosition(0, start);
			lrL.SetPosition(1, start + (Vector3)(leftDir * Mathf.Max(0.01f, telegraphLength)));

			lrR.SetPosition(0, start);
			lrR.SetPosition(1, start + (Vector3)(rightDir * Mathf.Max(0.01f, telegraphLength)));

			yield return new WaitForSeconds(duration);

			if (left != null) Destroy(left);
			if (center != null) Destroy(center);
			if (right != null) Destroy(right);
		}
	}
}


