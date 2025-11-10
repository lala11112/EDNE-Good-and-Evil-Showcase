using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 충격파 스킬 오브젝트
/// 경고 → 발동 시퀀스를 실행하고 충돌 감지 및 데미지를 적용합니다
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class Shockwave : MonoBehaviour
{
    [Header("경고 단계 설정")]
    [Tooltip("경고 표시 지속 시간 (초)")]
    [SerializeField] private float warningDuration = 1.0f;
    
    [Tooltip("경고 색상 (반투명)")]
    [SerializeField] private Color warningColor = new Color(1f, 0.5f, 0f, 0.3f); // 주황색 반투명
    
    [Header("발동 단계 설정")]
    [Tooltip("발동 색상")]
    [SerializeField] private Color activeColor = new Color(1f, 0f, 0f, 1f); // 빨간색 불투명
    
    [Tooltip("발동 지속 시간 (초)")]
    [SerializeField] private float activeDuration = 0.3f;

	[Header("발동 단계 VFX (콜라이더에 정합)")]
	[Tooltip("발동 단계에서 스프라이트 대신 표시할 VFX 프리팹")]
	[SerializeField] private GameObject activeVFXPrefab;
	[Tooltip("발동 단계에서 VFX를 사용할지 여부")]
	[SerializeField] private bool useActiveVFX = false;
	[Tooltip("프리팹의 기준 길이/두께 (Scale=1일 때 로컬 크기). 콜라이더 로컬 size를 이 값으로 나눠 최종 로컬 스케일을 계산합니다")]
	[SerializeField] private Vector2 vfxUnitLocalSize = new Vector2(1f, 1f);
	[Tooltip("VFX 사용 시 스프라이트를 숨김")]
	[SerializeField] private bool hideSpriteDuringActiveWhenUsingVFX = true;

    
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    
    private float damage;
    private EntityType attackerType;
    private bool isActive = false; // 발동 상태 (데미지 적용 가능 여부)

    // VFX 정렬/스케일을 위해 시전자→타겟 엔드포인트를 기억
    private bool hasEndpoints = false;
    private Vector3 sourceWorldPos;
    private Vector3 targetWorldPos;
    private float cachedThickness = 1f; // localScale.y에 해당하는 두께
    
    private HashSet<EntityController> hitEntities = new HashSet<EntityController>(); // 이미 맞은 대상 (중복 방지)
    
    #region 초기화
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        
        // 초기 상태: 충돌 비활성화
        boxCollider.isTrigger = true;
        boxCollider.enabled = false;
        
        // ⭐ 2D 트리거 이벤트를 위해 Rigidbody2D 보장
        var rb2d = GetComponent<Rigidbody2D>();
        if (rb2d == null)
        {
            rb2d = gameObject.AddComponent<Rigidbody2D>();
        }
        rb2d.bodyType = RigidbodyType2D.Kinematic;
        rb2d.simulated = true;
        rb2d.gravityScale = 0f;
        rb2d.interpolation = RigidbodyInterpolation2D.None;
        
        // ⭐ Sprite가 없으면 기본 흰색 사각형 생성
        if (spriteRenderer.sprite == null)
        {
            // Unity 기본 스프라이트 생성 (1x1 흰색 텍스처)
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            
            spriteRenderer.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f),
                1f
            );
            
            Debug.Log("[Shockwave] 기본 스프라이트 생성됨");
        }
        
        // 초기 색상을 투명으로 설정
        spriteRenderer.color = new Color(1f, 1f, 1f, 0f);
    }
    
    /// <summary>
    /// 충격파 초기화
    /// </summary>
    public void Initialize(float damage, EntityType attackerType, Vector2 size)
    {
        this.damage = damage;
        this.attackerType = attackerType;
        
        // 크기 설정
        transform.localScale = new Vector3(size.x, size.y, 1f);
        cachedThickness = size.y;
        
        Debug.Log($"[Shockwave] 초기화 - 데미지: {damage}, 크기: {size}");
        
        // 시퀀스 시작
        StartCoroutine(ExecuteSequence());
    }

    /// <summary>
    /// 엔드포인트 기반 초기화: 시전자→타겟 방향과 길이에 맞춰 자동 정렬/스케일
    /// </summary>
    public void InitializeWithEndpoints(float damage, EntityType attackerType, Vector3 source, Vector3 target, float thickness)
    {
        this.damage = damage;
        this.attackerType = attackerType;
        this.sourceWorldPos = source;
        this.targetWorldPos = target;
        this.hasEndpoints = true;
        this.cachedThickness = Mathf.Max(0.01f, thickness);

        Vector3 dir = (target - source);
        float length = dir.magnitude;
        if (length < 0.001f) { dir = transform.right; length = 0.01f; } else { dir /= length; }

        // 회전: 로컬 +X(transform.right)가 dir과 일치하도록
        Quaternion rot = Quaternion.FromToRotation(Vector3.right, new Vector3(dir.x, dir.y, 0f));
        transform.rotation = rot;
        // 위치: 중간 지점
        Vector3 mid = source + dir * (length * 0.5f);
        transform.position = mid;
        // 스케일: X=길이, Y=두께
        transform.localScale = new Vector3(length, cachedThickness, 1f);

        Debug.Log($"[Shockwave] InitializeWithEndpoints - len={length:F2} thick={cachedThickness:F2} src={source} dst={target}");

        StartCoroutine(ExecuteSequence());
    }
    
    #endregion
    
    #region 시퀀스 실행
    
    /// <summary>
    /// 충격파 시퀀스: 경고 → 발동 → 종료
    /// </summary>
    private IEnumerator ExecuteSequence()
    {
        // 1단계: 경고 깜빡임
        yield return StartCoroutine(WarningPhase());
        
        // 2단계: 발동
        yield return StartCoroutine(ActivePhase());
        
        // 3단계: 종료
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 경고 단계: 범위 표시
    /// </summary>
    private IEnumerator WarningPhase()
    {
        Debug.Log($"[Shockwave] 경고 단계 시작 - {warningDuration}초간 범위 표시");
        
        // 경고 색상으로 표시
        spriteRenderer.color = warningColor;
        Debug.Log($"[Shockwave] 범위 표시 시작 (색상: {warningColor})");
        
        // 지정된 시간만큼 대기
        yield return new WaitForSeconds(warningDuration);
        
        Debug.Log("[Shockwave] 경고 단계 완료");
    }
    
    /// <summary>
    /// 발동 단계: 색 변경 및 데미지 적용
    /// </summary>
    private IEnumerator ActivePhase()
    {
        Debug.Log($"[Shockwave] 발동! (색상: {activeColor})");
        
		GameObject vfxInstance = null;
		GameObject vfxPivot = null;
        // 발동 상태로 전환
        isActive = true;

		if (useActiveVFX && activeVFXPrefab != null && boxCollider != null)
		{
			// 스프라이트 표시 여부
			if (hideSpriteDuringActiveWhenUsingVFX)
			{
				spriteRenderer.color = new Color(1f, 1f, 1f, 0f);
			}
			else
			{
				spriteRenderer.color = activeColor;
			}

			// Pivot 생성: 애니메이션이 자식 트랜스폼 회전을 덮어써도, 피벗 회전/스케일로 방향/크기를 유지
			vfxPivot = new GameObject("ShockwaveVFXPivot");
			vfxPivot.transform.SetParent(transform, false);
			vfxPivot.transform.localPosition = (Vector3)boxCollider.offset;
			vfxPivot.transform.localRotation = Quaternion.identity;

			// 스케일: 피벗에 적용 (자식 애니메이션 스케일과 분리)
			Vector2 colLocalSize = boxCollider.size;
			float unitX = Mathf.Max(0.001f, vfxUnitLocalSize.x);
			float unitY = Mathf.Max(0.001f, vfxUnitLocalSize.y);
			vfxPivot.transform.localScale = new Vector3(colLocalSize.x / unitX, colLocalSize.y / unitY, 1f);

			// 프리팹 인스턴스: 피벗의 자식으로 배치, 로컬 정렬
			vfxInstance = Instantiate(activeVFXPrefab, vfxPivot.transform);
			vfxInstance.transform.localPosition = Vector3.zero;
			vfxInstance.transform.localRotation = Quaternion.identity;
			vfxInstance.transform.localScale = Vector3.one;

			// 자식 파티클/스프라이트에 색 적용(선택)
			var pss = vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
			for (int i = 0; i < pss.Length; i++)
			{
				var ps = pss[i];
				var main = ps.main;
				main.startColor = new ParticleSystem.MinMaxGradient(activeColor);
				main.simulationSpace = ParticleSystemSimulationSpace.Local;
				main.scalingMode = ParticleSystemScalingMode.Hierarchy;
				var vel = ps.velocityOverLifetime;
				vel.space = ParticleSystemSimulationSpace.Local;
				ps.Play(true);
			}
			// 렌더러 정렬을 로컬 기준으로 통일
			var psrs = vfxInstance.GetComponentsInChildren<ParticleSystemRenderer>(true);
			for (int i = 0; i < psrs.Length; i++)
			{
				try { psrs[i].alignment = ParticleSystemRenderSpace.Local; } catch { /* older Unity versions */ }
			}
			var childSRs = vfxInstance.GetComponentsInChildren<SpriteRenderer>(true);
			for (int i = 0; i < childSRs.Length; i++)
			{
				childSRs[i].color = activeColor;
			}
		}
		else
		{
			// 기존 방식: 스프라이트 빨간 불투명
			spriteRenderer.color = activeColor;
		}

        boxCollider.enabled = true; // 충돌 활성화
        
        Debug.Log($"[Shockwave] 현재 SpriteRenderer 색상: {spriteRenderer.color}");
        
        // 발동 지속
        yield return new WaitForSeconds(activeDuration);
        
        // 종료
        isActive = false;
        boxCollider.enabled = false;
		if (vfxPivot != null)
		{
			Destroy(vfxPivot);
		}
        
        Debug.Log("[Shockwave] 발동 종료");
    }
    
    #endregion
    
    #region 충돌 처리
    
    private bool TryApplyDamage(EntityController targetEc)
    {
        if (targetEc == null) return false;
        if (hitEntities.Contains(targetEc)) return false;
        bool applied = targetEc.ApplyDamage((int)damage, attackerType);
        if (applied)
        {
            hitEntities.Add(targetEc);
            Debug.Log($"[Shockwave] {targetEc.name}에게 {damage} 데미지!");
        }
        return applied;
    }
    
    /// <summary>
    /// 충돌 시작 (발동 중일 때만 데미지) - 2D
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isActive) return;
        var target = collision.GetComponentInParent<EntityController>();
        TryApplyDamage(target);
    }
    
    /// <summary>
    /// 충돌 지속 중 (발동 중 진입한 대상) - 2D
    /// </summary>
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!isActive) return;
        var target = collision.GetComponentInParent<EntityController>();
        TryApplyDamage(target);
    }
    
    /// <summary>
    /// 3D 콜라이더 대응 (프로젝트에 3D 콜라이더 사용 시)
    /// </summary>
    private void OnTriggerEnter(Collider collision)
    {
        if (!isActive) return;
        var target = collision.GetComponentInParent<EntityController>();
        TryApplyDamage(target);
    }
    
    private void OnTriggerStay(Collider collision)
    {
        if (!isActive) return;
        var target = collision.GetComponentInParent<EntityController>();
        TryApplyDamage(target);
    }
    
    #endregion
}

