using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 적이 죽었을 때 자폭하여 주변에 피해를 주는 스크립트
/// 쇼크웨이브와 동일한 방식으로 피해를 적용
/// </summary>
public class ExplosionOnDeathScript : MonoBehaviour, IEnemyDeathScript
{
    #region Inspector Fields - 인스펙터 필드
    
    [Header("자폭 설정")]
    [SerializeField] public float explosionRadius = 3f;
    [SerializeField] private int explosionDamage = 50;
    [SerializeField] private EntityType attackerType = EntityType.Monster;
    
    [Header("시각적 효과 설정")]
    [SerializeField] private bool showExplosionEffect = true;
    [SerializeField] private Color explosionColor = Color.red;
    [SerializeField] private float explosionDuration = 0.5f;
    [SerializeField] private GameObject explosionEffectPrefab; // 자폭 이펙트 프리팹
    
    [Header("플레이어 감지 설정")]
    [SerializeField] private LayerMask playerLayerMask = -1; // 플레이어 레이어
    [SerializeField] private bool autoFindPlayer = true;
    [SerializeField] private Transform targetPlayer;
    
    #endregion
    
    #region Private Variables - 프라이빗 변수
    
    private bool hasExploded = false;
    private HashSet<EntityController> hitEntities = new HashSet<EntityController>(); // 중복 피해 방지
    
    #endregion
    
    #region Unity Lifecycle - 유니티 생명주기
    
    private void Start()
    {
        if (autoFindPlayer && targetPlayer == null)
        {
            // 플레이어 자동 찾기
            if (PlayerController.Instance != null)
            {
                targetPlayer = PlayerController.Instance.transform;
            }
        }
        
        // 프리팹 설정값이 제대로 적용되었는지 확인
        Debug.Log($"[ExplosionOnDeathScript] 설정값 - 반경: {explosionRadius}, 피해: {explosionDamage}");
    }
    
    #endregion
    
    #region IEnemyDeathScript Implementation - 인터페이스 구현
    
    /// <summary>
    /// 적이 죽었을 때 자폭 실행
    /// </summary>
    public void OnEnemyDeath(GameObject deadEnemy)
    {
        if (hasExploded) return; // 중복 실행 방지
        
        hasExploded = true;
        
        Debug.Log($"[ExplosionOnDeathScript] {deadEnemy.name}이 자폭합니다! 반경: {explosionRadius}");
        
        // 자폭 실행
        StartCoroutine(ExecuteExplosion());
    }
    
    /// <summary>
    /// 외부에서 자폭 실행 (프리팹 방식)
    /// </summary>
    public void TriggerExplosion(Vector3 position, GameObject deadEnemy)
    {
        if (hasExploded) return;
        
        hasExploded = true;
        
        // 위치 설정
        transform.position = position;
        
        Debug.Log($"[ExplosionOnDeathScript] {deadEnemy.name}의 위치에서 자폭합니다! 반경: {explosionRadius}");
        
        // 자폭 실행
        StartCoroutine(ExecuteExplosion());
    }
    
    #endregion
    
    #region Explosion Logic - 자폭 로직
    
    /// <summary>
    /// 자폭 실행
    /// </summary>
    private IEnumerator ExecuteExplosion()
    {
        // 1. 시각적 효과 표시
        if (showExplosionEffect)
        {
            yield return StartCoroutine(ShowExplosionEffect());
        }
        
        // 2. 피해 적용
        ApplyExplosionDamage();
        
        // 3. 이펙트 프리팹 생성 (있는 경우)
        if (explosionEffectPrefab != null)
        {
            CreateExplosionEffect();
        }
    }
    
    /// <summary>
    /// 자폭 시각적 효과 표시
    /// </summary>
    private IEnumerator ShowExplosionEffect()
    {
        // 자폭 범위를 시각적으로 표시하는 원형 스프라이트 생성
        GameObject explosionVisual = CreateExplosionVisual();
        
        if (explosionVisual != null)
        {
            // 자폭 지속 시간만큼 대기
            yield return new WaitForSeconds(explosionDuration);
            
            // 시각적 효과 제거
            Destroy(explosionVisual);
        }
    }
    
    /// <summary>
    /// 자폭 시각적 효과 오브젝트 생성
    /// </summary>
    private GameObject CreateExplosionVisual()
    {
        GameObject visual = new GameObject("ExplosionVisual");
        visual.transform.position = transform.position;
        // 시각적 원의 크기를 실제 반경과 정확히 맞춤 (지름 = 반지름 * 2)
        float diameter = explosionRadius * 2f;
        visual.transform.localScale = new Vector3(diameter, diameter, 1f);
        Debug.Log($"[ExplosionOnDeathScript] 시각적 효과 지름: {diameter} (반경: {explosionRadius})");
        
        // SpriteRenderer 추가
        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        
        // 기본 원형 스프라이트 생성 (PPU를 텍스처 크기로 설정하여 스케일=월드유닛)
        Texture2D circleTexture = CreateCircleTexture(64, explosionColor);
        sr.sprite = Sprite.Create(
            circleTexture,
            new Rect(0, 0, circleTexture.width, circleTexture.height),
            new Vector2(0.5f, 0.5f),
            circleTexture.width
        );
        
        // 투명도 설정
        Color color = explosionColor;
        color.a = 0.7f;
        sr.color = color;
        
        return visual;
    }
    
    /// <summary>
    /// 원형 텍스처 생성
    /// </summary>
    private Texture2D CreateCircleTexture(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.5f;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    texture.SetPixel(x, y, color);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    /// <summary>
    /// 자폭 피해 적용
    /// </summary>
    private void ApplyExplosionDamage()
    {
        // 반경 내의 모든 콜라이더 검사
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius, playerLayerMask);
        
        foreach (Collider2D collider in colliders)
        {
            // EntityController 컴포넌트 찾기
            EntityController targetEntity = collider.GetComponentInParent<EntityController>();
            
            if (targetEntity != null && TryApplyDamage(targetEntity))
            {
                Debug.Log($"[ExplosionOnDeathScript] {targetEntity.name}에게 {explosionDamage} 자폭 피해!");
            }
        }
    }
    
    /// <summary>
    /// 개별 대상에게 피해 적용 (쇼크웨이브와 동일한 방식)
    /// </summary>
    private bool TryApplyDamage(EntityController targetEntity)
    {
        if (targetEntity == null) return false;
        if (hitEntities.Contains(targetEntity)) return false; // 중복 피해 방지
        
        // 쇼크웨이브와 동일한 방식으로 피해 적용
        bool applied = targetEntity.ApplyDamage(explosionDamage, attackerType);
        
        if (applied)
        {
            hitEntities.Add(targetEntity);
        }
        
        return applied;
    }
    
    /// <summary>
    /// 자폭 이펙트 프리팹 생성
    /// </summary>
    private void CreateExplosionEffect()
    {
        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            
            // 이펙트가 일정 시간 후 자동으로 파괴되도록 설정
            Destroy(effect, 2f);
        }
    }
    
    #endregion
    
    #region Public Methods - 퍼블릭 메서드
    
    /// <summary>
    /// 자폭 반경 설정
    /// </summary>
    public void SetExplosionRadius(float radius)
    {
        explosionRadius = radius;
    }
    
    /// <summary>
    /// 자폭 피해 설정
    /// </summary>
    public void SetExplosionDamage(int damage)
    {
        explosionDamage = damage;
    }
    
    /// <summary>
    /// 타겟 플레이어 설정
    /// </summary>
    public void SetTargetPlayer(Transform player)
    {
        targetPlayer = player;
    }
    
    /// <summary>
    /// 자폭 수동 실행 (테스트용)
    /// </summary>
    [ContextMenu("자폭 테스트")]
    public void TestExplosion()
    {
        if (!hasExploded)
        {
            OnEnemyDeath(gameObject);
        }
    }
    
    #endregion
    
    #region Debug - 디버그
    
    /// <summary>
    /// 씬 뷰에서 자폭 반경 표시
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
    
    #endregion
}
