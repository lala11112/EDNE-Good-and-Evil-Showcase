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
    
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    
    private float damage;
    private EntityType attackerType;
    private bool isActive = false; // 발동 상태 (데미지 적용 가능 여부)
    
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
        
        Debug.Log($"[Shockwave] 초기화 - 데미지: {damage}, 크기: {size}");
        
        // 시퀀스 시작
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
        
        // 발동 상태로 전환
        isActive = true;
        spriteRenderer.color = activeColor;
        boxCollider.enabled = true; // 충돌 활성화
        
        Debug.Log($"[Shockwave] 현재 SpriteRenderer 색상: {spriteRenderer.color}");
        
        // 발동 지속
        yield return new WaitForSeconds(activeDuration);
        
        // 종료
        isActive = false;
        boxCollider.enabled = false;
        
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

