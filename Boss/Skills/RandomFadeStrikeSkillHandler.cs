using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 여러 후보 오브젝트 중 하나를 무작위로 선택하여
/// 알파를 0 -> 1로 서서히 올린 뒤(텔레그래프), 정점에서 플레이어에게 피해를 주고 다시 0으로 내리는 스킬 핸들러
/// - 핸들러 프리팹으로 사용 (SkillHandlerInterface 구현)
/// - SpriteRenderer 기반 투명도 제어
/// </summary>
public class RandomFadeStrikeSkillHandler : MonoBehaviour, SkillHandlerInterface
{
    [Header("타겟 오브젝트들 (이름 기반)")]

    [Header("씬 검색 (이름/경로로 타겟 찾기)")]
    [Tooltip("간단 모드: 이름만으로 타겟을 찾습니다 (owner 하위). 경로 모드는 비워두세요.")]
    [SerializeField] private string[] fadeTargetNames;
    [Tooltip("Start 시점에 이름/경로 해상도를 수행합니다.")]
    [SerializeField] private bool resolveNamesOnStart = true;
    [Tooltip("Use 시점에 이름/경로 해상도를 한 번 더 수행합니다 (동적 로드 대응).")]
    [SerializeField] private bool resolveNamesOnUse = true;

    [Header("검색 범위")]
    [Tooltip("이름 검색을 씬 전체로 확장합니다. 끄면 이 핸들러의 루트(씬 루트) 기준으로만 검색합니다.")]
    [SerializeField] private bool searchWholeScene = false;

    // 타이밍/페이드 관련 복잡 옵션 제거 (요구사항: 2초 노랑 → 즉시 빨강 후 알파 0)
    [Header("경고/전환 타이밍 (인스펙터 설정)")]
    [Tooltip("노란색 경고 유지 시간(초)")]
    [SerializeField] private float warningHoldSeconds = 2f;
    [Tooltip("빨간색 유지 시간(초) - 데미지 적용 후")]
    [SerializeField] private float redHoldSeconds = 1f;

    [Header("피해 설정")]
    private int damageAmount = 0; // MonsterSkillData.damage에서 세팅
    [Tooltip("피해를 줄 대상 레이어 마스크 (플레이어/동맹 등 대상 선택)")]
    [SerializeField] private LayerMask damageLayerMask;

    // 비주얼 관련 복잡 옵션 제거 (고정 색 사용)

    [Header("초기화 설정")]
    [Tooltip("시작 시 모든 타겟의 알파를 0으로 강제 설정합니다.")]
    [SerializeField] private bool setAlphaZeroOnStart = true;

    [Header("디버그")]
    [SerializeField] private bool debugLogs = false;

    private MonsterSkillData skillData;
    private GameObject owner;
    private readonly List<GameObject> nameResolvedTargets = new List<GameObject>();

    public void Setup(BaseSkillData skill, GameObject owner)
    {
        this.owner = owner;
        this.skillData = skill as MonsterSkillData;
        if (this.skillData != null)
        {
            int rounded = Mathf.RoundToInt(this.skillData.damage);
            if (rounded > 0) damageAmount = rounded;
        }
    }

    private void Start()
    {
        if (resolveNamesOnStart)
        {
            ResolveTargetsByNames();
        }

        if (setAlphaZeroOnStart && nameResolvedTargets.Count > 0)
        {
            for (int i = 0; i < nameResolvedTargets.Count; i++)
            {
                ApplyAlphaToAllSprites(nameResolvedTargets[i], 0f);
            }
        }
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
        if (resolveNamesOnUse)
        {
            ResolveTargetsByNames();
        }

        // 후보 수집
        List<GameObject> candidates = new List<GameObject>();
        HashSet<GameObject> dedup = new HashSet<GameObject>();
        if (nameResolvedTargets.Count > 0)
        {
            for (int i = 0; i < nameResolvedTargets.Count; i++)
            {
                var t = nameResolvedTargets[i];
                if (t != null && dedup.Add(t)) candidates.Add(t);
            }
        }

        if (candidates.Count == 0)
        {
            if (debugLogs) Debug.LogWarning("[RandomFadeStrikeSkillHandler] 유효한 타겟이 없습니다.");
            Cleanup();
            yield break;
        }

        // 1~2개 무작위 선택 (최대 2개)
        int maxSelectable = Mathf.Min(2, candidates.Count);
        int selectCount = Mathf.Clamp(Random.Range(1, 3), 1, maxSelectable);
        List<GameObject> pool = new List<GameObject>(candidates);
        List<GameObject> picked = new List<GameObject>(selectCount);
        for (int i = 0; i < selectCount; i++)
        {
            int k = Random.Range(0, pool.Count);
            picked.Add(pool[k]);
            pool.RemoveAt(k);
        }

        // 병렬 실행 후 모두 완료될 때까지 대기
        int remaining = picked.Count;
        for (int i = 0; i < picked.Count; i++)
        {
            var target = picked[i];
            StartCoroutine(CoRunOnTarget(target, () => { remaining--; }));
        }
        while (remaining > 0) { yield return null; }

        // 종료
        Cleanup();
    }

    private IEnumerator CoRunOnTarget(GameObject selected, System.Action onDone)
    {
        if (selected == null)
        {
            onDone?.Invoke();
            yield break;
        }
        if (!selected.activeSelf) selected.SetActive(true);

        var spriteRenderers = selected.GetComponentsInChildren<SpriteRenderer>(true);
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            if (debugLogs) Debug.LogWarning($"[RandomFadeStrikeSkillHandler] 타겟 '{selected.name}'에 SpriteRenderer가 없습니다.");
            onDone?.Invoke();
            yield break;
        }

        // 원본 복원/프리페이드 제거: 요구사항에 따라 즉시 노랑 표시

        // 단순 색 전환: 반투명 노랑(2초) → 즉시 진한 빨강
        Color preColor = new Color(1f, 0.92f, 0.16f, 0.5f);    // 노랑, α=0.5
        Color postColor = new Color(1f, 0f, 0f, 1f);            // 빨강, α=1
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            spriteRenderers[i].enabled = true;
            spriteRenderers[i].color = preColor; // 데미지 없음
        }
        // 외부 스크립트가 색/알파를 덮어쓰는 경우를 대비해 매 프레임 고정 적용
        float preElapsed = 0f;
        while (preElapsed < warningHoldSeconds)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                spriteRenderers[i].color = preColor;
            }
            preElapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            spriteRenderers[i].color = postColor; // 전환과 동시에 타격
        }
        ApplyDamageOnColliders(selected);
        // 빨간색 유지 시간 노출(전환이 명확히 보이도록)
        if (redHoldSeconds > 0f)
        {
            yield return new WaitForSeconds(redHoldSeconds);
        }

        // 전환과 동시에 타격 이후 즉시 알파 0
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SetAlpha(spriteRenderers[i], 0f);
        }

        onDone?.Invoke();
    }

    private void ResolveTargetsByNames()
    {
        nameResolvedTargets.Clear();
        Transform root = transform.root != null ? transform.root : transform;

        // 1) 이름만으로 간단 검색 (우선)
        bool any = false;
        if (fadeTargetNames != null && fadeTargetNames.Length > 0)
        {
            for (int i = 0; i < fadeTargetNames.Length; i++)
            {
                string nm = fadeTargetNames[i];
                if (string.IsNullOrWhiteSpace(nm)) continue;
                Transform tr = null;
                if (searchWholeScene)
                {
                    tr = FindInAllScenesByName(nm.Trim());
                }
                else
                {
                    tr = FindInHierarchyByName(root, nm.Trim());
                }
                if (tr != null)
                {
                    nameResolvedTargets.Add(tr.gameObject);
                    any = true;
                }
                else if (debugLogs)
                {
                    Debug.LogWarning($"[RandomFadeStrikeSkillHandler] 이름 '{nm}' 로 타겟을 찾지 못했습니다.");
                }
            }
        }

        // 경로 모드는 제거됨
    }

    private static Transform FindInHierarchyByName(Transform root, string targetName)
    {
        if (root == null) return null;
        if (root.name == targetName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var hit = FindInHierarchyByName(root.GetChild(i), targetName);
            if (hit != null) return hit;
        }
        return null;
    }

    private static Transform FindInAllScenesByName(string targetName)
    {
        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            var scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded) continue;
            var roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                var hit = FindInHierarchyByName(roots[r].transform, targetName);
                if (hit != null) return hit;
            }
        }
        return null;
    }

    // 페이드 유틸 제거 (요구사항 단순 동작만 유지)

    private void ApplyDamageOnColliders(GameObject targetRoot)
    {
        if (targetRoot == null) return;
        var colliders = targetRoot.GetComponentsInChildren<Collider2D>(true);
        if (colliders == null || colliders.Length == 0)
        {
            if (debugLogs) Debug.LogWarning("[RandomFadeStrikeSkillHandler] 콜라이더가 없어 데미지를 적용하지 못했습니다.");
            return;
        }

        HashSet<Collider2D> visited = new HashSet<Collider2D>();
        List<Collider2D> overlapResults = new List<Collider2D>(16);
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(damageLayerMask);
        filter.useTriggers = true;

        for (int c = 0; c < colliders.Length; c++)
        {
            var col = colliders[c];
            if (col == null || !col.enabled) continue;
            overlapResults.Clear();
            int count = col.Overlap(filter, overlapResults);
            if (count <= 0)
            {
                // 회전/모양 이슈 대비: AABB로 보조 검사
                Bounds b = col.bounds;
                var hits = Physics2D.OverlapBoxAll(b.center, b.size, 0f, damageLayerMask);
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i] != null) overlapResults.Add(hits[i]);
                }
            }

            for (int i = 0; i < overlapResults.Count; i++)
            {
                var h = overlapResults[i];
                if (h == null || visited.Contains(h)) continue;
                visited.Add(h);

                var entity = h.GetComponentInParent<EntityController>();
                if (entity != null)
                {
                    entity.ApplyDamage(damageAmount, EntityType.Monster, this);
                }
            }
        }
    }

    private static void ApplyAlphaToAllSprites(GameObject root, float alpha)
    {
        if (root == null) return;
        var srs = root.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < srs.Length; i++)
        {
            SetAlpha(srs[i], alpha);
        }
    }

    private static void SetAlpha(SpriteRenderer sr, float alpha)
    {
        if (sr == null) return;
        Color c = sr.color;
        c.a = Mathf.Clamp01(alpha);
        sr.color = c;
    }

    private static void SetColorAlpha(SpriteRenderer sr, Color rgbSource, float alpha)
    {
        if (sr == null) return;
        sr.color = new Color(rgbSource.r, rgbSource.g, rgbSource.b, Mathf.Clamp01(alpha));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
        if (nameResolvedTargets != null && nameResolvedTargets.Count > 0)
        {
            for (int i = 0; i < nameResolvedTargets.Count; i++)
            {
                var t = nameResolvedTargets[i];
                if (t == null) continue;
                var cols = t.GetComponentsInChildren<Collider2D>(true);
                for (int c = 0; c < cols.Length; c++)
                {
                    var b = cols[c].bounds;
                    Gizmos.DrawWireCube(b.center, b.size);
                }
            }
        }
    }
}


