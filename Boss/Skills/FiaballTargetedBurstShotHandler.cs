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

			GameObject obj = Instantiate(projPrefab, spawnPos, Quaternion.identity);
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
				Vector2 rbPos = spawnPos;
				Vector2 targetNow = (player != null) ? (Vector2)player.position : rbPos;
				Vector2 dir = (targetNow - rbPos).normalized;
				rb.linearVelocity = dir * Mathf.Max(0.01f, speed);
			}

			if (interShotInterval > 0f && i < shots - 1)
			{
				yield return new WaitForSeconds(interShotInterval);
			}
		}

		Cleanup();
	}
}


