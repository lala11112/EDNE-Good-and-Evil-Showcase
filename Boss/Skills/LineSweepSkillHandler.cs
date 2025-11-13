using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵 밖에서 라인 방향으로 동시에 쓸어오는 투사체 스킬 핸들러
/// - 핸들러 프리팹으로 사용 (SkillHandlerInterface 구현)
/// - 맵 경계(BoxCollider2D)와 방향만 주면 전체 맵을 커버하도록 여러 평행 라인을 자동 생성
/// - 라인 텔레그래프를 일정 시간 보여준 뒤, 라인 위 모든 지점에서 투사체를 동시에 발사
/// </summary>
public class LineSweepSkillHandler : MonoBehaviour, SkillHandlerInterface
{
    [Header("맵 경계")]
    [Tooltip("스킬이 커버할 맵 영역의 경계 Collider2D (TilemapCollider2D 권장)")]
    [SerializeField] private Collider2D mapCollider;
    [Tooltip("이름으로 맵 경계 Collider를 찾습니다. 비워두면 자동 탐색 (예: Level09Wall)")]
    [SerializeField] private string mapColliderName = "Level09Wall";
    [Tooltip("태그로 맵 경계 Collider를 찾습니다. 비워두면 미사용")]
    [SerializeField] private string mapColliderTag = "";
    [Tooltip("직접 경계 값을 입력해 사용 (Collider 탐색을 무시)")]
    [SerializeField] private bool useManualBounds = false;
    [Tooltip("수동 경계 중심(World)")]
    [SerializeField] private Vector2 manualBoundsCenter = Vector2.zero;
    [Tooltip("수동 경계 크기(World)")]
    [SerializeField] private Vector2 manualBoundsSize = new Vector2(40, 40);

    [Header("텔레그래프 막대")]
    [Tooltip("발사 전에 경로를 표시할 막대(바) 프리팹. 중앙 정렬/회전/스케일이 자동 적용됩니다")]
    [SerializeField] private GameObject telegraphBarPrefab;
    [Tooltip("텔레그래프 표시 시간(초)")]
    [SerializeField] private float telegraphDuration = 0.8f;
    [Tooltip("맵 밖 스폰 마진(유닛)")]
    [SerializeField] private float outsideMargin = 2f;
    [Tooltip("막대 길이 스케일 기준(프리팹이 가로 1유닛일 때 1 권장)")]
    [SerializeField] private float telegraphBarUnitLength = 1f;
    [Tooltip("막대 두께 스케일(세로 축)")]
    [SerializeField] private float telegraphBarThickness = 0.2f;
    [Tooltip("프리팹 세로(Y) 1 스케일이 몇 월드 유닛인지 (두께 스케일 기준)")]
    [SerializeField] private float telegraphBarUnitThickness = 1f;

    [Header("액티브 막대(데미지 단계) 비주얼")]
    [Tooltip("데미지 단계에서 사용할 대체 막대 프리팹 (비우면 기존 막대 재활용)")]
    [SerializeField] private GameObject activeBarPrefab;
    [Tooltip("데미지 단계 색상 (프리팹이 LineRenderer/SpriteRenderer일 때 적용)")]
    [SerializeField] private Color activeBarColor = new Color(1f, 0.25f, 0.25f, 1f);
    [Tooltip("데미지 단계 두께 배수 (막대 세로 스케일)")]
    [SerializeField] private float activeBarThicknessMultiplier = 1.5f;
    [Tooltip("데미지 단계 머티리얼 (비우면 기존/기본 유지)")]
    [SerializeField] private Material activeBarMaterial;
    [Tooltip("프리팹 기본 방향(+X=오른쪽, +Y=위, -Y=아래 등). 이 방향이 스윕 방향(dir)과 일치하도록 회전합니다")]
    [SerializeField] private Vector2 prefabForward = Vector2.right;
    [Tooltip("액티브 막대가 유지되는 시간(초). 경고선→데미지선 전환 후 이 시간 뒤 제거")]
    [SerializeField] private float activeBarLifetime = 2f;

    [Header("라인 VFX (파티클 대체)")]
    [Tooltip("액티브 막대를 대신해 라인 전체 길이에 맞춰 표시할 파티클 프리팹")]
    [SerializeField] private GameObject lineVFXPrefab;
    [Tooltip("라인 VFX 사용 (켜면 액티브 막대 대신 파티클만 표시)")]
    [SerializeField] private bool useLineVFXForActiveBars = false;
    [Tooltip("프리팹의 기본 진행축 (위→아래 애니메이션이면 (0,-1))")]
    [SerializeField] private Vector2 lineVFXForward = new Vector2(0f, -1f);
	[Tooltip("스폰 시 VFX를 라인 길이/두께에 맞춰 자동 스케일")]
	[SerializeField] private bool vfxAutoScale = true;
	[Tooltip("라인 VFX 사용 시에도 히트박스 가이드를 함께 표시")]
	[SerializeField] private bool showHitboxGuideWithVFX = false;
	[Tooltip("히트박스 가이드(바) 색상")] 
	[SerializeField] private Color hitboxGuideColor = new Color(1f, 1f, 0f, 0.9f);

    [Header("Fallback Bar (telegraphBarPrefab이 null일 때)")]
    [Tooltip("Fallback 바 색상")]
    [SerializeField] private Color fallbackBarColor = new Color(1f, 0.1f, 0.1f, 0.9f);
    [Tooltip("Fallback 바 머티리얼 (비우면 Sprites/Default)")]
    [SerializeField] private Material fallbackBarMaterial;
    [Tooltip("Fallback 바 Sorting Order")]
    [SerializeField] private int fallbackBarSortingOrder = 1000;
    [Tooltip("Fallback 바 Sorting Layer 이름 (비우면 Default)")]
    [SerializeField] private string fallbackBarSortingLayer = "Default";

    [Header("패턴 설정")]
    [Tooltip("라인 진행 방향. (1, -1) 등으로 대각선을 만들 수 있음")]
    [SerializeField] private Vector2 direction = new Vector2(1f, -1f);
    [Tooltip("라인 간격(유닛). 맵 전체를 이 간격으로 평행 라인으로 채웁니다")]
    [SerializeField] private float lineSpacing = 2f;
    [Tooltip("라인 위에서 투사체 간격(유닛)")]
    [SerializeField] private float projectileSpacing = 1f;
    [Tooltip("라인이 맵을 가로지르는 데 걸릴 시간(초). 0 이하면 스킬 데이터의 projectileSpeed 사용")]
    [SerializeField] private float travelTime = 2f;

    [Header("데미지 스윕(투사체 없이 선 자체로 피해")]
    [Tooltip("true면 텔레그래프 후 선 자체가 이동하며 피해를 줍니다")]
    [SerializeField] private bool useDamageSweep = false;
    [Tooltip("스윕 선의 길이(유닛) - 충돌 박스 가로")]
    [SerializeField] private float damageHitboxLength = 1.2f;
    [Tooltip("스윕 선의 두께(유닛) - 충돌 박스 세로")]
    [SerializeField] private float damageHitboxThickness = 0.4f;
    // 피해량은 MonsterSkillData.damage를 사용합니다 (인스펙터에서 설정하지 않음)
    [Tooltip("같은 스윕 동안 동일 대상 중복 타격 방지")]
    [SerializeField] private bool damageOncePerSweep = true;
    [Tooltip("피해 적용 대상 레이어")]
    [SerializeField] private LayerMask damageLayerMask;

    [Header("투사체 발사")]
    [Tooltip("true면 스윕 대신/또는 함께 투사체를 발사합니다")]
    [SerializeField] private bool spawnProjectiles = true;

    [Header("다중 방향 (교차 등)")]
    [Tooltip("true면 directions 배열의 모든 방향으로 동시에 스윕합니다")]
    [SerializeField] private bool useMultiDirections = false;
    [Tooltip("여러 방향을 지정 (예: (1,0), (0,1) 로 가로+세로)")]
    [SerializeField] private Vector2[] directions;

	[Header("교대 패턴 (A/B 번갈아)")]
	[Tooltip("두 개의 방향 세트를 번갈아 사용합니다 (사용 시 위 '다중 방향' 대신 본 옵션이 우선).")]
	[SerializeField] private bool useAlternatingDirectionSets = false;
	[Tooltip("세트 A 방향들 (예: (1,0), (0,-1) → 가로/세로)")]
	[SerializeField] private Vector2[] directionSetA;
	[Tooltip("세트 B 방향들 (예: (1,-1), (-1,-1) → 대각선 교차)")]
	[SerializeField] private Vector2[] directionSetB;
	[Tooltip("첫 실행 시 세트 A부터 시작할지 여부")]
	[SerializeField] private bool startWithSetA = true;

    // ShockWave 이동 막대는 제거. 요청에 따라 텔레그래프 막대 + 투사체만 사용
    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
	[Tooltip("히트박스 두께를 실제 보이는 바의 두께(렌더러)와 일치시킵니다")]
	[SerializeField] private bool hitboxMatchVisual = true;

	// 라인 VFX 사용 시, 바 GameObject가 없어도 현재 프레임의 비주얼 두께(월드)를 추적하기 위한 캐시
	private float lastVisualThicknessWorld = 0f;


    private MonsterSkillData skillData;
    private GameObject owner;

	// 핸들러 인스턴스 간에도 교대를 유지하기 위한 정적 토글
	private static bool s_nextUseSetAInitialized = false;
	private static bool s_nextUseSetA = true;

    public void Setup(BaseSkillData skill, GameObject owner)
    {
        this.owner = owner;
        this.skillData = skill as MonsterSkillData;
        if (this.skillData == null)
        {
            Debug.LogError("[LineSweepSkillHandler] MonsterSkillData가 필요합니다. 전달된 BaseSkillData 타입을 확인하세요.");
        }
    }

    public void Use(SkillRank rank)
    {
        StartCoroutine(CoUse());
    }

    public void Cleanup()
    {
        // 현재 핸들러는 일회성으로 동작하므로 파괴만 수행
        if (this != null && gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator CoUse()
    {
		// 교대 토글 초기화 (프로젝트 실행 중 최초 1회)
		if (!s_nextUseSetAInitialized)
		{
			s_nextUseSetA = startWithSetA;
			s_nextUseSetAInitialized = true;
		}

        if (!useManualBounds && mapCollider == null)
        {
            // 이름 우선 탐색 (상위/하위 포함)
            if (!string.IsNullOrEmpty(mapColliderName))
            {
                var allCols = GameObject.FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
                foreach (var c in allCols)
                {
                    if (c == null) continue;
                    if (c.gameObject.name == mapColliderName || c.transform.root.name == mapColliderName)
                    {
                        mapCollider = c;
                        break;
                    }
                }
            }

            // 태그 탐색
            if (mapCollider == null && !string.IsNullOrEmpty(mapColliderTag))
            {
                var tagged = GameObject.FindGameObjectsWithTag(mapColliderTag);
                foreach (var go in tagged)
                {
                    var c = go.GetComponent<Collider2D>();
                    if (c != null) { mapCollider = c; break; }
                }
            }

            // 자동 할당 시도: 우선 TilemapCollider2D, 없으면 BoxCollider2D
            if (mapCollider == null)
            {
                var autoTileCol = FindObjectOfType<UnityEngine.Tilemaps.TilemapCollider2D>();
                if (autoTileCol != null) mapCollider = autoTileCol;
            }
            if (mapCollider == null)
            {
                mapCollider = FindObjectOfType<BoxCollider2D>();
            }
        }

        if ((!useManualBounds && mapCollider == null) || skillData == null)
        {
            Debug.LogWarning("[LineSweepSkillHandler] mapCollider 또는 skillData가 설정되지 않았습니다.");
            Destroy(gameObject);
            yield break;
        }

		// 방향 목록 준비
		System.Collections.Generic.List<Vector2> dirList = new System.Collections.Generic.List<Vector2>();
		if (useAlternatingDirectionSets)
		{
			// 이번 실행에 사용할 세트 선택
			Vector2[] pick = s_nextUseSetA ? directionSetA : directionSetB;
			if (pick != null && pick.Length > 0)
			{
				for (int i = 0; i < pick.Length; i++)
				{
					Vector2 d = pick[i];
					if (d.sqrMagnitude > 1e-6f) dirList.Add(d.normalized);
				}
			}

			// 유효한 방향이 없으면 단일 기본 방향 사용
			if (dirList.Count == 0)
			{
				Vector2 baseDir = direction.sqrMagnitude < 1e-6f ? Vector2.down : direction;
				dirList.Add(baseDir.normalized);
			}

			// 다음 실행 때는 반대 세트를 사용하도록 토글
			s_nextUseSetA = !s_nextUseSetA;
		}
		else
		{
			if (useMultiDirections && directions != null && directions.Length > 0)
			{
				for (int i = 0; i < directions.Length; i++)
				{
					Vector2 d = directions[i];
					if (d.sqrMagnitude > 1e-6f) dirList.Add(d.normalized);
				}
			}
			if (dirList.Count == 0)
			{
				Vector2 baseDir = direction.sqrMagnitude < 1e-6f ? Vector2.down : direction;
				dirList.Add(baseDir.normalized);
			}
		}
        Bounds b;
        if (useManualBounds)
        {
            b = new Bounds(manualBoundsCenter, new Vector3(Mathf.Max(0.01f, manualBoundsSize.x), Mathf.Max(0.01f, manualBoundsSize.y), 1f));
        }
        else
        {
            b = mapCollider.bounds;
            var tilemapCol = mapCollider as UnityEngine.Tilemaps.TilemapCollider2D;
            if (tilemapCol != null && tilemapCol.usedByComposite)
            {
                var comp = tilemapCol.GetComponent<CompositeCollider2D>();
                if (comp == null) comp = tilemapCol.GetComponentInParent<CompositeCollider2D>();
                if (comp != null) b = comp.bounds;
            }
        }

        if (b.size.x <= 0.01f || b.size.y <= 0.01f)
        {
            Debug.LogError($"[LineSweepSkillHandler] Bounds 크기가 0입니다. useManualBounds={useManualBounds} mapCollider={(mapCollider?mapCollider.name:"null")}");
            Destroy(gameObject);
            yield break;
        }

        // 각 방향 별로 처리

        // 텔레그래프 생성 목록 + 라인 데이터
        List<GameObject> telegraphs = new List<GameObject>();
        List<(Vector2 entry, Vector2 exit, Vector2 dir)> lineInfos = new List<(Vector2, Vector2, Vector2)>();

        // 모든 방향에 대해 텔레그래프 생성
        foreach (var dir in dirList)
        {
            Vector2 n = new Vector2(-dir.y, dir.x);
            float halfExtent = ComputeHalfExtentAlongNormal(b, n);
            float cover = halfExtent + outsideMargin;

            for (float offset = -cover; offset <= cover + 0.001f; offset += Mathf.Max(0.01f, lineSpacing))
            {
                Vector2 center = (Vector2)b.center + n * offset;
                if (!LineVsAABB(center, dir, b, out float tMin, out float tMax))
                {
                    continue;
                }

                Vector2 entry = center + dir * tMin;
                Vector2 exit = center + dir * tMax;

                GameObject g = SpawnTelegraphBar(entry, exit, dir);
                if (g != null) telegraphs.Add(g);
                lineInfos.Add((entry, exit, dir));
            }
        }

        if (telegraphs.Count == 0)
        {
            Debug.LogWarning($"[LineSweepSkillHandler] 생성된 텔레그래프가 없습니다. boundsCenter={b.center} boundsSize={b.size} directions={dirList.Count} lineSpacing={lineSpacing}");
        }

        if (debugLogs)
        {
            Debug.Log($"[LineSweepSkillHandler] telegraphs={telegraphs.Count} mapCollider={mapCollider?.name} bounds={b.min}->{b.max} directions={dirList.Count}");
        }

        // 텔레그래프 표시 시간 대기
        yield return new WaitForSeconds(Mathf.Max(0f, telegraphDuration));

        // 스윕 데미지 또는 투사체 발사
        if (useDamageSweep)
        {
            for (int i = 0; i < lineInfos.Count; i++)
            {
                var info = lineInfos[i];
                GameObject bar = null;
                if (i < telegraphs.Count) bar = telegraphs[i];
                InstantDamageLine(info.entry, info.exit, info.dir, bar);
            }
            telegraphs.Clear();
        }
        else
        {
            foreach (var g in telegraphs)
            {
                if (g != null) Destroy(g);
            }
            telegraphs.Clear();
        }

        // 모든 방향에 대해 발사 (옵션)
        if (spawnProjectiles)
        {
            foreach (var dir in dirList)
            {
                Vector2 n = new Vector2(-dir.y, dir.x);
                float halfExtent = ComputeHalfExtentAlongNormal(b, n);
                float cover = halfExtent + outsideMargin;

                for (float offset = -cover; offset <= cover + 0.001f; offset += Mathf.Max(0.01f, lineSpacing))
                {
                    Vector2 center = (Vector2)b.center + n * offset;
                    if (!LineVsAABB(center, dir, b, out float tMin, out float tMax))
                    {
                        continue;
                    }

                    Vector2 entry = center + dir * tMin;
                    Vector2 exit = center + dir * tMax;
                    Vector2 spawnStart = entry - dir * outsideMargin;

                    float totalDist = (exit - spawnStart).magnitude;
                    float speed = travelTime > 0f ? totalDist / travelTime : skillData.projectileSpeed;
                    Vector2 spawnPos = spawnStart;

                    if (skillData.skillEffectPrefab == null)
                    {
                        Debug.LogError("[LineSweepSkillHandler] MonsterSkillData.skillEffectPrefab이 비어있습니다.");
                        continue;
                    }

                    GameObject proj = Instantiate(skillData.skillEffectPrefab, spawnPos, Quaternion.identity);

                    var p = proj.GetComponent<ISkillProjectile>();
                    if (p != null)
                    {
                        p.SetSkillData(skillData.damage, speed);
                        p.SetAttackerType(EntityType.Monster);
                        p.SetPiercing(false);
                    }

                    var rb = proj.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.linearVelocity = dir * speed;
                    }
                }
            }
        }

        Destroy(gameObject);
    }

    private GameObject SpawnTelegraphBar(Vector2 entry, Vector2 exit, Vector2 dir)
    {
        if (telegraphBarPrefab != null)
        {
            Vector2 mid = (entry + exit) * 0.5f;
            float length = (exit - entry).magnitude;
            Quaternion rot = Quaternion.FromToRotation(new Vector3(prefabForward.x, prefabForward.y, 0f), new Vector3(dir.x, dir.y, 0f));
            GameObject bar = Instantiate(telegraphBarPrefab, mid, rot);
            // 길이/두께 스케일 적용 (프리팹의 기준이 X=가로라고 가정)
            if (telegraphBarUnitLength > 0f)
            {
                Vector3 s = bar.transform.localScale;
                s.x = length / telegraphBarUnitLength;
                s.y = telegraphBarUnitThickness > 0f ? (telegraphBarThickness / telegraphBarUnitThickness) : telegraphBarThickness;
                bar.transform.localScale = s;
            }
            return bar;
        }

        // Fallback: LineRenderer로 빨간 막대 생성
        GameObject go = new GameObject("TelegraphBarFallback");
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.startWidth = telegraphBarThickness;
        lr.endWidth = telegraphBarThickness;
        lr.material = fallbackBarMaterial != null ? fallbackBarMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.startColor = fallbackBarColor;
        lr.endColor = fallbackBarColor;
        if (!string.IsNullOrEmpty(fallbackBarSortingLayer)) lr.sortingLayerName = fallbackBarSortingLayer;
        lr.sortingOrder = fallbackBarSortingOrder;
        lr.SetPosition(0, entry);
        lr.SetPosition(1, exit);
        return go;
    }

    private IEnumerator SweepDamageCoroutine(Vector2 entry, Vector2 exit, Vector2 dir, GameObject bar)
    {
        // 텔레그래프 -> 액티브 비주얼로 전환
        bar = ConvertBarToActive(bar, entry, exit, dir);

        Vector2 spawnStart = entry - dir * outsideMargin;
        float totalDist = (exit - spawnStart).magnitude;
        float duration = travelTime > 0f ? travelTime : Mathf.Max(0.01f, totalDist / Mathf.Max(0.01f, skillData != null ? skillData.projectileSpeed : totalDist));
        float elapsed = 0f;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        HashSet<Collider2D> hitSet = damageOncePerSweep ? new HashSet<Collider2D>() : null;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector2 pos = Vector2.Lerp(spawnStart, exit, t);
            // 바 시각효과 이동
            if (bar != null)
            {
                bar.transform.position = pos;
                bar.transform.rotation = Quaternion.FromToRotation(new Vector3(prefabForward.x, prefabForward.y, 0f), new Vector3(dir.x, dir.y, 0f));
                // 길이/두께
                var tr = bar.transform;
                Vector3 s = tr.localScale;
                s.x = damageHitboxLength / Mathf.Max(0.01f, telegraphBarUnitLength);
                float visualThicknessWorld = damageHitboxThickness * Mathf.Max(0.01f, activeBarThicknessMultiplier);
                s.y = telegraphBarUnitThickness > 0f ? (visualThicknessWorld / telegraphBarUnitThickness) : visualThicknessWorld;
                tr.localScale = s;
            }

			// 히트 체크
			float hitThicknessWorld = damageHitboxThickness * Mathf.Max(0.01f, activeBarThicknessMultiplier);
			if (hitboxMatchVisual)
			{
				float vis = TryGetVisualThicknessWorld(bar);
				if (vis > 0.001f) hitThicknessWorld = vis; else if (lastVisualThicknessWorld > 0.001f) hitThicknessWorld = lastVisualThicknessWorld;
			}
            Vector2 size = new Vector2(Mathf.Max(0.01f, damageHitboxLength), Mathf.Max(0.01f, hitThicknessWorld));
            if (debugLogs) DebugDrawOrientedBox(pos, size, angle, new Color(1f, 1f, 0f, 0.8f), 0.05f);
            Collider2D[] hits = Physics2D.OverlapBoxAll(pos, size, angle, damageLayerMask);
            for (int i = 0; i < hits.Length; i++)
            {
                var col = hits[i];
                if (col == null) continue;
                if (hitSet != null && hitSet.Contains(col)) continue;

                var entity = col.GetComponentInParent<EntityController>();
                if (entity != null)
                {
                    int dmg = skillData != null ? Mathf.RoundToInt(skillData.damage) : 0;
                    entity.ApplyDamage(dmg, EntityType.Monster, this);
                    if (hitSet != null) hitSet.Add(col);
                }
            }

            yield return null;
        }

        if (bar != null) Destroy(bar, Mathf.Max(0f, activeBarLifetime));
    }

    private void InstantDamageLine(Vector2 entry, Vector2 exit, Vector2 dir, GameObject bar)
    {
        // 액티브 비주얼로 전환 및 전체 라인 길이로 배치
        bar = ConvertBarToActive(bar, entry, exit, dir);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Vector2 mid = (entry + exit) * 0.5f;
        float length = (exit - entry).magnitude;
        if (bar != null)
        {
            bar.transform.position = mid;
            bar.transform.rotation = Quaternion.FromToRotation(new Vector3(prefabForward.x, prefabForward.y, 0f), new Vector3(dir.x, dir.y, 0f));
            var tr = bar.transform;
            Vector3 s = tr.localScale;
            s.x = length / Mathf.Max(0.01f, telegraphBarUnitLength);
            float visualThicknessWorld = damageHitboxThickness * Mathf.Max(0.01f, activeBarThicknessMultiplier);
            s.y = telegraphBarUnitThickness > 0f ? (visualThicknessWorld / telegraphBarUnitThickness) : visualThicknessWorld;
            tr.localScale = s;
        }

		// 한 번에 전 구간 피해 적용 (ShockWave 핸들러 방식)
        HashSet<Collider2D> hitSet = damageOncePerSweep ? new HashSet<Collider2D>() : null;
		float hitThicknessWorld = damageHitboxThickness * Mathf.Max(0.01f, activeBarThicknessMultiplier);
		if (hitboxMatchVisual)
		{
			float vis = TryGetVisualThicknessWorld(bar);
			if (vis > 0.001f) hitThicknessWorld = vis; else if (lastVisualThicknessWorld > 0.001f) hitThicknessWorld = lastVisualThicknessWorld;
		}
        Vector2 size = new Vector2(Mathf.Max(0.01f, length), Mathf.Max(0.01f, hitThicknessWorld));
        if (debugLogs) DebugDrawOrientedBox(mid, size, angle, new Color(1f, 0.8f, 0.2f, 0.8f), 0.15f);
        Collider2D[] hits = Physics2D.OverlapBoxAll(mid, size, angle, damageLayerMask);
        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (col == null) continue;
            if (hitSet != null && hitSet.Contains(col)) continue;
            var entity = col.GetComponentInParent<EntityController>();
            if (entity != null)
            {
                int dmg = skillData != null ? Mathf.RoundToInt(skillData.damage) : 0;
                entity.ApplyDamage(dmg, EntityType.Monster, this);
                if (hitSet != null) hitSet.Add(col);
            }
        }

        if (bar != null) Destroy(bar, Mathf.Max(0f, activeBarLifetime));
    }


    private GameObject ConvertBarToActive(GameObject bar, Vector2 entry, Vector2 exit, Vector2 dir)
    {
        // 간단 경로: 라인 VFX를 쓰기로 한 경우, 파티클만 생성하고 막대는 사용하지 않음
        if (useLineVFXForActiveBars && lineVFXPrefab != null)
        {
            SpawnLineVFX(entry, exit, dir);
            if (showHitboxGuideWithVFX)
            {
                if (bar != null) ApplyBarVisuals(bar, hitboxGuideColor, null);
                return bar; // 가이드를 유지해 판정 기준을 시각적으로 노출
            }
            if (bar != null) Destroy(bar);
            return null;
        }

        // 대체 프리팹이 있으면 교체
        if (activeBarPrefab != null)
        {
            if (bar != null) Destroy(bar);
            Vector2 mid = (entry + exit) * 0.5f;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            // 회전은 prefabForward 기준이 아니므로 Euler 각도를 사용하지 않고 Quaternion.FromToRotation을 사용해야 합니다.
            Quaternion rot = Quaternion.FromToRotation(new Vector3(prefabForward.x, prefabForward.y, 0f), new Vector3(dir.x, dir.y, 0f));
            GameObject newBar = Instantiate(activeBarPrefab, mid, rot); 
            ApplyBarVisuals(newBar, activeBarColor, activeBarMaterial);
            return newBar;
        }

        // 기존 막대 재활용: 색/머티리얼 변경
        if (bar != null)
        {
            ApplyBarVisuals(bar, activeBarColor, activeBarMaterial);
        }
        return bar;
    }

    private void ApplyBarVisuals(GameObject bar, Color color, Material material)
    {
        if (bar == null) return;
        var lr = bar.GetComponent<LineRenderer>();
        if (lr != null)
        {
            if (material != null) lr.material = material;
            lr.startColor = color;
            lr.endColor = color;
            return;
        }
        var sr = bar.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = color;
            if (material != null) sr.sharedMaterial = material;
        }
    }

    private void SpawnLineVFX(Vector2 entry, Vector2 exit, Vector2 dir)
    {
        if (lineVFXPrefab == null) return;
        Vector2 mid = (entry + exit) * 0.5f;
        float length = (exit - entry).magnitude;
        
        // lineVFXForward를 기준으로 회전 계산
        Quaternion rot = Quaternion.FromToRotation(new Vector3(lineVFXForward.x, lineVFXForward.y, 0f), new Vector3(dir.x, dir.y, 0f));
        var go = Instantiate(lineVFXPrefab, mid, rot);
        
        // 자동 스케일: 프리팹의 길이축(lineVFXForward)에 맞춰 길이/두께를 조정
        if (vfxAutoScale && telegraphBarUnitLength > 0f)
        {
            Vector3 s = go.transform.localScale;
            bool forwardIsX = Mathf.Abs(lineVFXForward.x) >= Mathf.Abs(lineVFXForward.y);
            float thicknessWorld = damageHitboxThickness * Mathf.Max(0.01f, activeBarThicknessMultiplier);
            if (forwardIsX)
            {
                s.x = Mathf.Max(0.01f, length / Mathf.Max(0.01f, telegraphBarUnitLength));
                s.y = Mathf.Max(0.01f, telegraphBarUnitThickness > 0f ? (thicknessWorld / telegraphBarUnitThickness) : thicknessWorld);
            }
            else
            {
                s.y = Mathf.Max(0.01f, length / Mathf.Max(0.01f, telegraphBarUnitLength));
                s.x = Mathf.Max(0.01f, telegraphBarUnitThickness > 0f ? (thicknessWorld / telegraphBarUnitThickness) : thicknessWorld);
            }
            go.transform.localScale = s;
            // 라인 VFX가 활성일 때 현재 비주얼 두께(월드)를 캐시해 판정 두께와 일치시킵니다.
            lastVisualThicknessWorld = thicknessWorld;
        }
        
        // 파티클 기본 설정만 강제하고, 크기 조정은 하지 않음
        var pss = go.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < pss.Length; i++)
        {
            var ps = pss[i];
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            // 부모 스케일을 파티클에 적용하려면 Hierarchy 필요
            main.scalingMode = vfxAutoScale ? ParticleSystemScalingMode.Hierarchy : ParticleSystemScalingMode.Local;
            
            // 크기(Shape.scale)는 변경하지 않습니다
            // Velocity Over Lifetime도 로컬 기준으로 강제해 회전이 이동 방향에 반영되도록
            var vel = ps.velocityOverLifetime;
            vel.space = ParticleSystemSimulationSpace.Local;
            ps.Play(true);
        }
        // 정렬 설정은 프리팹 기본값 사용

        Destroy(go, Mathf.Max(0f, activeBarLifetime));
    }

    private static void DebugDrawOrientedBox(Vector2 center, Vector2 size, float angleDeg, Color color, float duration)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        Vector2 hx = new Vector2(size.x * 0.5f * cos, size.x * 0.5f * sin);
        Vector2 hy = new Vector2(-size.y * 0.5f * sin, size.y * 0.5f * cos);
        Vector2 c = center;
        Vector3 p0 = c - hx - hy;
        Vector3 p1 = c + hx - hy;
        Vector3 p2 = c + hx + hy;
        Vector3 p3 = c - hx + hy;
        Debug.DrawLine(p0, p1, color, duration);
        Debug.DrawLine(p1, p2, color, duration);
        Debug.DrawLine(p2, p3, color, duration);
        Debug.DrawLine(p3, p0, color, duration);
    }

    private float TryGetVisualThicknessWorld(GameObject bar)
    {
        if (bar == null) return 0f;
        var lr = bar.GetComponent<LineRenderer>();
        if (lr != null)
        {
            // LineRenderer widthMultiplier는 월드 유닛
            return Mathf.Max(0.001f, (lr.startWidth + lr.endWidth) * 0.5f);
        }
        var sr = bar.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            // 회전 포함된 월드 바운즈의 최소 변을 두께로 사용 (막대가 길이축이 X라고 가정)
            var b = sr.bounds;
            return Mathf.Max(0.001f, Mathf.Min(b.size.x, b.size.y));
        }
        return 0f;
    }



    // 이동 ShockWave는 제거됨

    private static float ComputeHalfExtentAlongNormal(Bounds b, Vector2 n)
    {
        n.Normalize();
        Vector2 c = b.center;
        Vector2[] corners = new Vector2[4]
        {
            new Vector2(b.min.x, b.min.y),
            new Vector2(b.min.x, b.max.y),
            new Vector2(b.max.x, b.min.y),
            new Vector2(b.max.x, b.max.y)
        };
        float maxAbs = 0f;
        for (int i = 0; i < corners.Length; i++)
        {
            float d = Vector2.Dot(corners[i] - c, n);
            maxAbs = Mathf.Max(maxAbs, Mathf.Abs(d));
        }
        return maxAbs;
    }

    private static bool LineVsAABB(Vector2 center, Vector2 dir, Bounds b, out float tMin, out float tMax)
    {
        dir.Normalize();
        tMin = float.NegativeInfinity;
        tMax = float.PositiveInfinity;

        // X축 슬래브
        if (Mathf.Abs(dir.x) > 1e-5f)
        {
            float tx1 = (b.min.x - center.x) / dir.x;
            float tx2 = (b.max.x - center.x) / dir.x;
            float txMin = Mathf.Min(tx1, tx2);
            float txMax = Mathf.Max(tx1, tx2);
            tMin = Mathf.Max(tMin, txMin);
            tMax = Mathf.Min(tMax, txMax);
        }
        else if (center.x < b.min.x || center.x > b.max.x) return false;

        // Y축 슬래브
        if (Mathf.Abs(dir.y) > 1e-5f)
        {
            float ty1 = (b.min.y - center.y) / dir.y;
            float ty2 = (b.max.y - center.y) / dir.y;
            float tyMin = Mathf.Min(ty1, ty2);
            float tyMax = Mathf.Max(ty1, ty2);
            tMin = Mathf.Max(tMin, tyMin);
            tMax = Mathf.Min(tMax, tyMax);
        }
        else if (center.y < b.min.y || center.y > b.max.y) return false;

        return tMax >= tMin;
    }
}