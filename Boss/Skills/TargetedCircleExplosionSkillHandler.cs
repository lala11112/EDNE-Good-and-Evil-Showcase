using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 현재 위치를 스냅샷하여 해당 지점에 원형 경고 후 폭발 데미지를 적용하는 스킬 핸들러
/// - 핸들러 프리팹으로 사용 (SkillHandlerInterface 구현)
/// - 경고: 원형 경고 비주얼 표시 (프리팹 또는 LineRenderer 폴백)
/// - 발동: 경고와 동일한 반경으로 즉시 피해 적용, 색 전환 및 선택적 폭발 VFX
/// - 종료: 잠시 유지 후 정리
/// </summary>
public class TargetedCircleExplosionSkillHandler : MonoBehaviour, SkillHandlerInterface
{
    [Header("타겟/위치")]
    [Tooltip("경고 위치를 플레이어 현재 위치로 스냅샷합니다")] 
    [SerializeField] private bool snapshotPlayerPosition = true;
    [Tooltip("스냅샷이 실패하면 이 오프셋만큼 보스 앞 방향으로 이동")] 
    [SerializeField] private float fallbackForwardOffset = 0.5f;

    [Header("지오메트리")]
    [Tooltip("폭발 반경(유닛)")] 
    [SerializeField] private float radius = 2.0f;

    [Header("타이밍")]
    [Tooltip("경고 표시 시간(초)")] 
    [SerializeField] private float warningDuration = 0.8f;
    [Tooltip("발동 후 비주얼 유지 시간(초)")] 
    [SerializeField] private float activeHoldDuration = 0.2f;

    [Header("색상/머티리얼")]
    [Tooltip("경고 단계 색상 (반투명 권장)")] 
    [SerializeField] private Color warningColor = new Color(1f, 0.25f, 0.25f, 0.35f);
    [Tooltip("발동(폭발) 단계 색상")] 
    [SerializeField] private Color activeColor = new Color(1f, 0.0f, 0.0f, 0.9f);
    [Tooltip("폴백 LineRenderer용 머티리얼 (비우면 Default-Line)")] 
    [SerializeField] private Material lineMaterial;
    [Tooltip("폴백 라인 두께")] 
    [SerializeField] private float lineWidth = 0.08f;
    [Tooltip("폴백 라인 분할(원 정밀도)")] 
    [SerializeField] private int circleSegments = 48;

    [Header("비주얼 프리팹 (선택)")]
    [Tooltip("경고 단계에 사용할 원형 비주얼 프리팹 (없으면 LineRenderer로 그립니다)")] 
    [SerializeField] private GameObject warningPrefab;
    [Tooltip("발동 시 재생할 폭발 VFX 프리팹 (선택)")] 
    [SerializeField] private GameObject explosionVfxPrefab;
    [Tooltip("프리팹 기준 단위 길이(지름을 1로 가정하면 1). 프리팹 스케일 자동화에 사용")] 
    [SerializeField] private float prefabUnitDiameter = 1f;

    [Header("피해")]
    [Tooltip("피해를 적용할 대상 레이어")] 
    [SerializeField] private LayerMask damageLayerMask;

    [Header("디버그")]
    [SerializeField] private bool debugLogs = false;

    private MonsterSkillData skillData;
    private GameObject owner;

    private GameObject warningInstance;
    private LineRenderer warningLine;
    private Vector2 snapshotPos;

    public void Setup(BaseSkillData skill, GameObject owner)
    {
        this.owner = owner;
        this.skillData = skill as MonsterSkillData;
        if (this.skillData == null)
        {
            Debug.LogError("[TargetedCircleExplosion] MonsterSkillData가 필요합니다.");
        }
    }

    public void Use(SkillRank rank)
    {
        // 랭크 무시: 몬스터/보스 패턴은 랭크를 사용하지 않음
        Use();
    }

    // 몬스터/보스 전용: 랭크 없이 호출 가능한 진입점
    public void Use()
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
        // 1) 타겟 위치 스냅샷
        snapshotPos = ResolveTargetPosition();

        // 2) 경고 비주얼 스폰
        SpawnWarningVisual(snapshotPos);

        // 3) 경고 유지
        if (warningDuration > 0f) yield return new WaitForSeconds(warningDuration);

        // 4) 발동: 색 전환 + 데미지 적용 + 폭발 VFX
        SetWarningColor(activeColor);
        ApplyExplosionDamage(snapshotPos, radius);
        SpawnExplosionVfx(snapshotPos);

        // 5) 잠시 유지 후 정리
        if (activeHoldDuration > 0f) yield return new WaitForSeconds(activeHoldDuration);

        if (warningInstance != null) Destroy(warningInstance);
        Cleanup();
    }

    private Vector2 ResolveTargetPosition()
    {
        if (snapshotPlayerPosition)
        {
            // PlayerController.Instance 우선
            var player = PlayerController.Instance != null ? PlayerController.Instance.transform : null;
            if (player != null) return player.position;

            // 태그 검색
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null) return tagged.transform.position;
        }

        // 폴백: 보스 앞쪽으로 약간
        if (owner != null)
        {
            Vector2 pos = owner.transform.position;
            Vector2 forward = owner.transform.right;
            return pos + forward * fallbackForwardOffset;
        }

        return transform.position;
    }

    private void SpawnWarningVisual(Vector2 center)
    {
        if (warningPrefab != null)
        {
            warningInstance = Instantiate(warningPrefab, center, Quaternion.identity);
            AutoScaleCircularPrefab(warningInstance.transform, radius, prefabUnitDiameter);
            TryTintPrefab(warningInstance, warningColor);
            return;
        }

        // 폴백: LineRenderer로 원 테두리 그리기
        warningInstance = new GameObject("WarningCircleFallback");
        warningInstance.transform.position = center;
        warningLine = warningInstance.AddComponent<LineRenderer>();
        warningLine.useWorldSpace = true;
        warningLine.loop = true;
        warningLine.widthMultiplier = Mathf.Max(0.001f, lineWidth);
        warningLine.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        warningLine.startColor = warningColor;
        warningLine.endColor = warningColor;
        int seg = Mathf.Max(12, circleSegments);
        warningLine.positionCount = seg;
        float step = Mathf.PI * 2f / seg;
        for (int i = 0; i < seg; i++)
        {
            float a = step * i;
            Vector3 p = new Vector3(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius, 0f);
            warningLine.SetPosition(i, p);
        }
    }

    private static void AutoScaleCircularPrefab(Transform tr, float targetRadius, float unitDiameter)
    {
        float diameter = Mathf.Max(0.01f, targetRadius * 2f);
        float unit = Mathf.Max(0.01f, unitDiameter);
        Vector3 s = tr.localScale;
        s.x = diameter / unit;
        s.y = diameter / unit;
        tr.localScale = s;
    }

    private void TryTintPrefab(GameObject go, Color c)
    {
        if (go == null) return;
        var srs = go.GetComponentsInChildren<SpriteRenderer>(true);
        if (srs != null && srs.Length > 0)
        {
            for (int i = 0; i < srs.Length; i++) srs[i].color = c;
            return;
        }
        var lrs = go.GetComponentsInChildren<LineRenderer>(true);
        if (lrs != null && lrs.Length > 0)
        {
            for (int i = 0; i < lrs.Length; i++) { lrs[i].startColor = c; lrs[i].endColor = c; }
        }
    }

    private void SetWarningColor(Color c)
    {
        if (warningInstance == null) return;
        TryTintPrefab(warningInstance, c);
        if (warningLine != null)
        {
            warningLine.startColor = c;
            warningLine.endColor = c;
        }
    }

    private void ApplyExplosionDamage(Vector2 center, float r)
    {
        float rr = Mathf.Max(0.01f, r);
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, rr, damageLayerMask);
        if (hits == null || hits.Length == 0) return;

        HashSet<EntityController> damaged = new HashSet<EntityController>();
        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (h == null) continue;
            var entity = h.GetComponentInParent<EntityController>();
            if (entity == null) continue;
            if (damaged.Contains(entity)) continue;
            int dmg = skillData != null ? Mathf.RoundToInt(skillData.damage) : 0;
            bool applied = entity.ApplyDamage(dmg, EntityType.Monster, this);
            if (applied)
            {
                damaged.Add(entity);
                if (debugLogs) Debug.Log($"[TargetedCircleExplosion] {entity.name} hit for {dmg}");
            }
        }
    }

    private void SpawnExplosionVfx(Vector2 center)
    {
        if (explosionVfxPrefab == null) return;
        var vfx = Instantiate(explosionVfxPrefab, center, Quaternion.identity);
        AutoScaleCircularPrefab(vfx.transform, radius, prefabUnitDiameter);
        TryTintPrefab(vfx, activeColor);
        Destroy(vfx, Mathf.Max(0.05f, activeHoldDuration));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.7f);
        Vector3 c = Application.isPlaying ? (Vector3)snapshotPos : transform.position;
        Gizmos.DrawWireSphere(c, Mathf.Max(0.01f, radius));
    }
}


