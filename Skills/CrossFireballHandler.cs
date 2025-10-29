using UnityEngine;
using System.Collections;

/// <summary>
/// 몬스터 중심에서 앞/뒤/좌/우로 파이어볼을 동시에 발사하는 핸들러
/// MonsterFireballHandler와 동일한 API를 따르며 4발을 스폰합니다.
/// </summary>
public class CrossFireballHandler : MonoBehaviour, SkillHandlerInterface
{
	[Header("스킬 설정")]
	[Tooltip("발사할 투사체 프리팹 (비우면 SkillData.skillEffectPrefab 사용)")]
	[SerializeField] private GameObject fireballPrefab; // 파이어볼 투사체 프리팹
	[Tooltip("본체에서 투사체가 생성될 초기 거리")]
	[Min(0f)] [SerializeField] private float spawnOffset = 0.8f; // 본체로부터 약간 떨어진 위치에서 스폰
	[Tooltip("투사체를 진행 방향으로 회전시킬지")]
	[SerializeField] private bool rotateProjectileToDirection = true; // 진행방향으로 회전

	[Header("발사 패턴")]
	[Tooltip("켜면 X(대각선) 4방향, 끄면 +(상하좌우) 4방향")]
	[SerializeField] private bool useDiagonalPattern = false; // 체크 시 대각선(X) 발사, 아니면 십자가(+)

	[Header("경고 바 설정")]
	[Tooltip("경고 바로 사용할 프리팹 (비우면 임시 막대 생성)")]
	[SerializeField] private GameObject warningBarPrefab; // 선택: 없으면 임시 스프라이트 생성
	[Tooltip("발사 전에 경고 바가 유지되는 시간(초)")]
	[Min(0f)] [SerializeField] private float warningDuration = 1.0f; // 발사 전 경고 시간
	[Tooltip("경고 바의 길이(X), 두께(Y)")]
	[SerializeField] private Vector2 warningBarSize = new Vector2(3.5f, 0.35f);
	[Tooltip("경고 바 색/투명도")]
	[SerializeField] private Color warningColor = new Color(1f, 0f, 0f, 0.7f);
	[Tooltip("경고 바를 본체에서 추가로 띄우는 거리")]
	[Min(0f)] [SerializeField] private float warningExtraOffset = 0.6f; // 경고 바를 본체에서 더 띄우는 거리

	private MonsterSkillData skillData;
	private MonsterController ownerController;

	public void Setup(BaseSkillData data, GameObject owner)
	{
		skillData = data as MonsterSkillData;
		ownerController = owner != null ? owner.GetComponent<MonsterController>() : null;
		Debug.Log("[CrossFireballHandler] Setup 완료");
	}

	public void Use(SkillRank rank)
	{
		if (skillData == null || ownerController == null)
		{
			Debug.LogError("[CrossFireballHandler] 스킬 데이터 또는 소유자가 없습니다!");
			Cleanup();
			return;
		}

		StartCoroutine(Sequence());
	}

	public void Cleanup()
	{
		Destroy(gameObject);
	}

	private IEnumerator Sequence()
	{
		Vector3 origin = ownerController.transform.position;
		Vector3[] dirs = GetDirections();

		// 경고바가 표시되는 동안 이동을 잠그고, NavMeshAgent 관성을 즉시 제거
		if (ownerController != null)
		{
			// 경고 시간 동안만 잠금
			ownerController.LockMovement(warningDuration);

			// NavMeshAgent 강제 정지 (관성 제거)
			UnityEngine.AI.NavMeshAgent agent = ownerController.GetComponent<UnityEngine.AI.NavMeshAgent>();
			if (agent != null)
			{
				agent.isStopped = true;
				agent.velocity = Vector3.zero;
				Debug.Log("[CrossFireballHandler] NavMeshAgent 강제 정지 완료");
			}
		}

		// 1) 경고 바 생성
		GameObject[] warnings = new GameObject[4];
		for (int i = 0; i < 4; i++)
		{
			warnings[i] = CreateWarningBar(origin, dirs[i]);
		}

		// 2) 경고 시간 대기
		yield return new WaitForSeconds(warningDuration);

		// 3) 경고 제거
		for (int i = 0; i < 4; i++)
		{
			if (warnings[i] != null) Destroy(warnings[i]);
		}

		// 4) 발사
		FireProjectiles(origin, dirs);

		// 5) 종료
		Cleanup();
	}

	private GameObject CreateWarningBar(Vector3 origin, Vector3 dir)
	{
		GameObject bar;
		if (warningBarPrefab != null)
		{
			bar = Instantiate(warningBarPrefab);
		}
		else
		{
			bar = new GameObject("WarningBar");
			var sr = bar.AddComponent<SpriteRenderer>();
			// 1x1 텍스처로 임시 막대
			Texture2D texture = new Texture2D(1, 1);
			texture.SetPixel(0, 0, Color.white);
			texture.Apply();
			sr.sprite = Sprite.Create(texture, new Rect(0,0,1,1), new Vector2(0.5f,0.5f), 1f);
		}

		// 위치/회전/크기/색
		float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
		bar.transform.position = origin + dir.normalized * (spawnOffset + warningExtraOffset);
		bar.transform.rotation = Quaternion.Euler(0,0,angle);
		bar.transform.localScale = new Vector3(warningBarSize.x, warningBarSize.y, 1f);
		var sr2 = bar.GetComponent<SpriteRenderer>();
		if (sr2 != null) sr2.color = warningColor;
		return bar;
	}

	private void FireProjectiles(Vector3 origin, Vector3[] dirs)
	{
		GameObject prefab = fireballPrefab != null ? fireballPrefab : skillData.skillEffectPrefab;
		if (prefab == null)
		{
			Debug.LogError("[CrossFireballHandler] 파이어볼 프리팹이 없습니다!");
			return;
		}
		for (int i = 0; i < dirs.Length; i++)
		{
			Vector3 dir = dirs[i].normalized;
			Vector3 spawnPos = origin + dir * spawnOffset;
			Quaternion rot = Quaternion.identity;
			if (rotateProjectileToDirection)
			{
				float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
				rot = Quaternion.Euler(0, 0, angle);
			}
			GameObject proj = Instantiate(prefab, spawnPos, rot);
			ISkillProjectile projScript = proj.GetComponent<ISkillProjectile>();
			if (projScript != null)
			{
				projScript.SetSkillData(skillData.damage, skillData.projectileSpeed);
				projScript.SetAttackerType(EntityType.Monster);
				projScript.SetOwnerController(ownerController);
			}
			Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
			if (rb != null)
			{
				rb.linearVelocity = (Vector2)(dir * skillData.projectileSpeed);
			}
		}
	}

	private Vector3[] GetDirections()
	{
		if (!useDiagonalPattern)
		{
			return new Vector3[]
			{
				ownerController.transform.up,      // 앞
				-ownerController.transform.up,     // 뒤
				ownerController.transform.right,   // 우
				-ownerController.transform.right   // 좌
			};
		}
		else
		{
			Vector3 up = ownerController.transform.up.normalized;
			Vector3 right = ownerController.transform.right.normalized;
			Vector3 ur = (up + right).normalized;
			Vector3 ul = (up - right).normalized;
			Vector3 dr = (-up + right).normalized;
			Vector3 dl = (-up - right).normalized;
			return new Vector3[] { ur, ul, dr, dl };
		}
	}
}


