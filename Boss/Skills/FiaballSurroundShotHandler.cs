using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 현재 위치 주변에 일정 간격으로 Fiaball 탄을 배치해 잠시 대기 후 플레이어 쪽으로 발사
/// - 핸들러 프리팹 방식 (SkillHandlerInterface)
/// - 발사까지 대기 시간 동안 탄은 정지 상태
/// - 발사 시점의 플레이어 위치를 기준으로 방향 계산
/// </summary>
public class FiaballSurroundShotHandler : MonoBehaviour, SkillHandlerInterface
{
	[Header("Fiaball 프리팹")]
	[Tooltip("명시하면 이 프리팹을 사용. 비어있으면 스킬 데이터의 skillEffectPrefab 사용")]
	[SerializeField] private GameObject fiaballPrefab;

	[Header("배치 설정")]
	[Tooltip("플레이어 주변에 배치할 탄 개수")] 
	[SerializeField] private int projectileCount = 8;
	[Tooltip("배치 반지름 (플레이어 중심에서의 거리)")] 
	[SerializeField] private float radius = 3f;
	[Tooltip("배치 각도 오프셋 (도)")] 
	[SerializeField] private float angleOffsetDeg = 0f;

	[Header("랜덤 배치(혼합)")]
	[Tooltip("true면 원을 기준으로 난수 각도/반지름에 스폰합니다 (순차 X)")]
	[SerializeField] private bool useRandomScatter = false;
	[Tooltip("랜덤 배치 시 순차적으로 하나씩 스폰합니다 (켜면 동시에 여러 개 생성 방지)")]
	[SerializeField] private bool randomScatterSequential = true;
	[Tooltip("랜덤 배치 시 최소 반지름")] 
	[SerializeField] private float minRadius = 2.5f;
	[Tooltip("랜덤 배치 시 최대 반지름")] 
	[SerializeField] private float maxRadius = 4.0f;
	[Tooltip("랜덤 스폰 지연 최대값(초). 0~이 값 사이에서 개별 지연")]
	[SerializeField] private float spawnJitterMax = 0.4f;

	[Header("타이밍")]
	[Tooltip("각 탄환이 발사되기 전에 대기할 시간(초) - 각 탄환별 개별 적용")] 
	[SerializeField] private float waitBeforeFire = 2f;
	[Tooltip("탄환을 하나씩 스폰할 때의 간격(초)")]
	[SerializeField] private float interSpawnDelay = 0.1f;

	[Header("프로젝타일 설정")]
	[Tooltip("ISkillProjectile에 설정할 관통 여부")] 
	[SerializeField] private bool isPiercing = false;

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
			if (debugLogs) Debug.LogWarning("[FiaballSurroundShotHandler] Player를 찾지 못했습니다.");
			Cleanup();
			yield break;
		}

		GameObject projPrefab = fiaballPrefab != null ? fiaballPrefab : (skillData != null ? skillData.skillEffectPrefab : null);
		if (projPrefab == null)
		{
			Debug.LogError("[FiaballSurroundShotHandler] Fiaball 프리팹이 설정되지 않았습니다.");
			Cleanup();
			yield break;
		}

		int count = Mathf.Max(1, projectileCount);
		float speed = skillData != null ? skillData.projectileSpeed : 10f;
		int damage = skillData != null ? Mathf.RoundToInt(skillData.damage) : 10;

		int remaining = 0;
		Vector2 center = player.position;
		if (useRandomScatter)
		{
			if (randomScatterSequential || spawnJitterMax <= 0f)
			{
				// 순차적으로 하나씩 랜덤 위치에 스폰
				for (int i = 0; i < count; i++)
				{
					float ang = Random.Range(0f, 360f) + angleOffsetDeg;
					float r = Mathf.Clamp(Random.Range(minRadius, maxRadius), 0.01f, 999f);
					float tRad = ang * Mathf.Deg2Rad;
					Vector2 pos = center + new Vector2(Mathf.Cos(tRad), Mathf.Sin(tRad)) * r;

					GameObject obj = Instantiate(projPrefab, pos, Quaternion.identity);
					var p = obj.GetComponent<ISkillProjectile>();
					if (p != null)
					{
						p.SetSkillData(damage, speed);
						p.SetAttackerType(EntityType.Monster);
						p.SetPiercing(isPiercing);
					}
					var rb = obj.GetComponent<Rigidbody2D>();
					if (rb != null)
					{
						rb.linearVelocity = Vector2.zero;
						remaining++;
						StartCoroutine(CoDelayAndFire(rb, player, speed, waitBeforeFire, () => remaining--));
					}

					if (interSpawnDelay > 0f)
					{
						yield return new WaitForSeconds(interSpawnDelay);
					}
				}
			}
			else
			{
				// 병렬 지연 스폰 (기존 랜덤 지터 방식)
				for (int i = 0; i < count; i++)
				{
					float ang = Random.Range(0f, 360f) + angleOffsetDeg;
					float r = Mathf.Clamp(Random.Range(minRadius, maxRadius), 0.01f, 999f);
					float tRad = ang * Mathf.Deg2Rad;
					Vector2 pos = center + new Vector2(Mathf.Cos(tRad), Mathf.Sin(tRad)) * r;
					float spawnDelay = Random.Range(0f, spawnJitterMax);
					float fireDelay = Mathf.Max(0f, waitBeforeFire);
					remaining++;
					StartCoroutine(CoSpawnAndFire(projPrefab, pos, player, damage, speed, isPiercing, spawnDelay, fireDelay, () => remaining--));
				}
			}
		}
		else
		{
			for (int i = 0; i < count; i++)
			{
				float t = (i / (float)count) * Mathf.PI * 2f + angleOffsetDeg * Mathf.Deg2Rad;
				Vector2 pos = center + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * Mathf.Max(0.01f, radius);
				GameObject obj = Instantiate(projPrefab, pos, Quaternion.identity);

				// ISkillProjectile 세팅 (데미지/속도/타입/관통)
				var p = obj.GetComponent<ISkillProjectile>();
				if (p != null)
				{
					p.SetSkillData(damage, speed);
					p.SetAttackerType(EntityType.Monster);
					p.SetPiercing(isPiercing);
				}

				Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
				if (rb != null)
				{
					rb.linearVelocity = Vector2.zero; // 대기 동안 정지
					remaining++;
					StartCoroutine(CoDelayAndFire(rb, player, speed, waitBeforeFire, () => remaining--));
				}

				// 스폰 간격
				if (interSpawnDelay > 0f)
				{
					yield return new WaitForSeconds(interSpawnDelay);
				}
			}
		}

		// 모든 탄이 발사되기까지 대기
		while (remaining > 0) yield return null;

		// 핸들러 종료 (BossController가 이 오브젝트 파괴를 완료 신호로 인식)
		Cleanup();
	}

	private IEnumerator CoDelayAndFire(Rigidbody2D rb, Transform player, float speed, float delay, System.Action onDone)
	{
		if (rb == null)
		{
			onDone?.Invoke();
			yield break;
		}
		yield return new WaitForSeconds(Mathf.Max(0f, delay));
		// 스폰 후 파괴되었을 수 있음
		if (rb == null)
		{
			onDone?.Invoke();
			yield break;
		}
		Vector2 rbPos = rb.position;
		Vector2 targetNow = (player != null) ? (Vector2)player.position : rbPos;
		Vector2 dir = (targetNow - rbPos).normalized;
		rb.linearVelocity = dir * Mathf.Max(0.01f, speed);
		onDone?.Invoke();
	}

	private IEnumerator CoSpawnAndFire(GameObject prefab, Vector2 pos, Transform player, int damage, float speed, bool piercing, float spawnDelay, float fireDelay, System.Action onDone)
	{
		yield return new WaitForSeconds(Mathf.Max(0f, spawnDelay));
		GameObject obj = Instantiate(prefab, pos, Quaternion.identity);
		var p = obj.GetComponent<ISkillProjectile>();
		if (p != null)
		{
			p.SetSkillData(damage, speed);
			p.SetAttackerType(EntityType.Monster);
			p.SetPiercing(piercing);
		}
		var rb = obj.GetComponent<Rigidbody2D>();
		if (rb != null)
		{
			rb.linearVelocity = Vector2.zero;
			yield return CoDelayAndFire(rb, player, speed, fireDelay, onDone);
		}
		else
		{
			onDone?.Invoke();
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.6f);
		Transform player = PlayerController.Instance != null ? PlayerController.Instance.transform : null;
		Vector3 c = player != null ? player.position : transform.position;
		Gizmos.DrawWireSphere(c, Mathf.Max(0.01f, radius));
	}
}


