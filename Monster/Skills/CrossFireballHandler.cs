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
	[Tooltip("체크 시 십자/대각선 패턴을 스킬 사용시마다 번갈아가며 적용합니다")]
	[SerializeField] private bool useAlternatingPattern = false;
	[Tooltip("교대 모드에서 첫 실행을 대각선(X)으로 시작할지 여부")]
	[SerializeField] private bool startWithDiagonal = false;

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

	[Header("사망 시 즉시 종료")]
	[SerializeField] private bool cancelOnOwnerDeath = true;

	private MonsterSkillData skillData;
	private MonsterController ownerController;

	// 활성 경고바 보관 (사망 즉시 제거용)
	private GameObject[] activeWarnings;
	private bool subscribedToDeathEvent = false;

	// 잠금된 기준값들 (슬라이딩/목표 이동과 무관하게 동일 경로 보장)
	private Vector3 lockedOrigin;
	private Vector3 lockedTargetPosition;
	private Vector3[] lockedDirs;
	private bool currentIsDiagonal = false;

	// 교대 토글(프로젝트 실행 중 공유)
	private static bool s_altInit = false;
	private static bool s_nextDiagonal = false;

	public void Setup(BaseSkillData data, GameObject owner)
	{
		skillData = data as MonsterSkillData;
		ownerController = owner != null ? owner.GetComponent<MonsterController>() : null;
		Debug.Log("[CrossFireballHandler] Setup 완료");

		if (ownerController != null && cancelOnOwnerDeath && !subscribedToDeathEvent)
		{
			ownerController.OnMonsterDied += OnOwnerDied;
			subscribedToDeathEvent = true;
		}
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
		// 패턴 결정: 교대 모드면 토글 사용, 아니면 체크박스 고정
		if (useAlternatingPattern)
		{
			if (!s_altInit)
			{
				s_nextDiagonal = startWithDiagonal;
				s_altInit = true;
			}
			currentIsDiagonal = s_nextDiagonal;
			s_nextDiagonal = !s_nextDiagonal;
		}
		else
		{
			currentIsDiagonal = useDiagonalPattern;
		}

		// 기준값 잠금
		lockedOrigin = ownerController.transform.position;
		lockedTargetPosition = (ownerController != null && ownerController.Target != null)
			? ownerController.Target.position
			: lockedOrigin + ownerController.transform.up; // 타겟 없을 때 앞방향

		// 플레이어 방향을 기준으로 교차/대각선 방향 계산
		lockedDirs = GetLockedDirections(lockedOrigin, lockedTargetPosition, currentIsDiagonal);

		// 경고바가 표시되는 동안 이동 완전 정지 (슬라이딩 방지)
		if (ownerController != null)
		{
			// 일반 이동 로직 비활성화 + 잠금 타이머 (여유 0.2초)
			ownerController.DisableMovement();
			ownerController.LockMovement(Mathf.Max(0f, warningDuration + 0.2f));

			// NavMeshAgent 강제 정지 (관성 제거)
			UnityEngine.AI.NavMeshAgent agent = ownerController.GetComponent<UnityEngine.AI.NavMeshAgent>();
			if (agent != null)
			{
				agent.isStopped = true;
				try { agent.ResetPath(); } catch { }
				Debug.Log("[CrossFireballHandler] NavMeshAgent 강제 정지 완료 (ResetPath)");
			}

			// Rigidbody2D가 있으면 즉시 속도 0
			var rb2d = ownerController.GetComponent<Rigidbody2D>();
			if (rb2d != null)
			{
				rb2d.linearVelocity = Vector2.zero;
				rb2d.angularVelocity = 0f;
			}
		}

		// 1) 경고 바 생성
		activeWarnings = new GameObject[4];
		for (int i = 0; i < 4; i++)
		{
			activeWarnings[i] = CreateWarningBar(lockedOrigin, lockedDirs[i]);
		}

		// 2) 경고 시간 대기
		{
			float elapsed = 0f;
			while (elapsed < warningDuration)
			{
				// 경고 중에는 절대 움직이지 않도록 강제 고정
				if (ownerController != null)
				{
					// 위치 스냅
					ownerController.transform.position = lockedOrigin;

					// NavMeshAgent도 함께 스냅/정지
					var agent = ownerController.GetComponent<UnityEngine.AI.NavMeshAgent>();
					if (agent != null)
					{
						agent.isStopped = true;
						if (agent.isOnNavMesh)
						{
							try { agent.Warp(lockedOrigin); } catch { }
						}
						try { agent.ResetPath(); } catch { }
					}

					// Rigidbody2D가 있으면 속도 0 유지
					var rb2d = ownerController.GetComponent<Rigidbody2D>();
					if (rb2d != null)
					{
						rb2d.linearVelocity = Vector2.zero;
						rb2d.angularVelocity = 0f;
					}
				}

				elapsed += Time.deltaTime;
				yield return null;
			}
		}

		// 3) 경고 제거
		for (int i = 0; i < 4; i++)
		{
			if (activeWarnings != null && activeWarnings[i] != null) Destroy(activeWarnings[i]);
			if (activeWarnings != null) activeWarnings[i] = null;
		}

		// 4) 발사
		FireProjectiles(lockedOrigin, lockedDirs);

		// 5) 종료
		if (ownerController != null)
		{
			ownerController.EnableMovement();
		}
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

	private void OnOwnerDied(MonsterController _)
	{
		if (!cancelOnOwnerDeath) return;

		// 즉시 코루틴 중단
		try { StopAllCoroutines(); } catch { }

		// 경고바 즉시 제거
		if (activeWarnings != null)
		{
			for (int i = 0; i < activeWarnings.Length; i++)
			{
				if (activeWarnings[i] != null) Destroy(activeWarnings[i]);
				activeWarnings[i] = null;
			}
		}

		// 핸들러 정리
		Cleanup();
	}

	private void OnDestroy()
	{
		if (subscribedToDeathEvent && ownerController != null)
		{
			ownerController.OnMonsterDied -= OnOwnerDied;
			subscribedToDeathEvent = false;
		}
	}

	private void OnDisable()
	{
		if (subscribedToDeathEvent && ownerController != null)
		{
			ownerController.OnMonsterDied -= OnOwnerDied;
			subscribedToDeathEvent = false;
		}
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

	// 플레이어 방향을 기준으로 고정된 4방향 계산
	private Vector3[] GetLockedDirections(Vector3 origin, Vector3 targetPos, bool diagonal)
	{
		Vector3 baseDir = (targetPos - origin);
		if (baseDir.sqrMagnitude < 0.0001f)
		{
			baseDir = ownerController != null ? ownerController.transform.up : Vector3.up;
		}
		baseDir.z = 0f;
		baseDir.Normalize();

		// 수직 방향(좌/우)
		Vector3 perp = new Vector3(-baseDir.y, baseDir.x, 0f);

		if (!diagonal)
		{
			// 가로/세로(+)
			return new Vector3[]
			{
				baseDir,    // 목표 방향
				-baseDir,   // 반대
				perp,       // 우측
				-perp       // 좌측
			};
		}
		else
		{
			// 대각선(X): (base ± perp)
			Vector3 d1 = (baseDir + perp).normalized;
			Vector3 d2 = (baseDir - perp).normalized;
			Vector3 d3 = (-baseDir + perp).normalized;
			Vector3 d4 = (-baseDir - perp).normalized;
			return new Vector3[] { d1, d2, d3, d4 };
		}
	}
}


