using UnityEngine;

/// <summary>
/// 플레이어 대쉬 시 생성되는 잔상 효과
/// 시간이 지나면서 페이드 아웃되어 사라집니다
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class AfterimageEffect : MonoBehaviour
{
	[Tooltip("잔상 지속 시간 (초)")]
	public float duration = 0.5f;
	
	[Tooltip("시작 알파값 (0~1)")]
	public float startAlpha = 0.7f;
	
	private SpriteRenderer spriteRenderer;
	private float elapsedTime = 0f;
	private Color originalColor;
	
	/// <summary>
	/// 초기화
	/// </summary>
	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		if (spriteRenderer != null)
		{
			originalColor = spriteRenderer.color;
		}
	}
	
	/// <summary>
	/// 매 프레임 페이드 아웃 처리
	/// </summary>
	private void Update()
	{
		if (spriteRenderer == null) return;
		
		// 시간 증가
		elapsedTime += Time.deltaTime;
		
		// 페이드 아웃 비율 계산 (1 -> 0)
		float fadeRatio = 1f - (elapsedTime / duration);
		fadeRatio = Mathf.Clamp01(fadeRatio);
		
		// 알파값 적용
		Color color = originalColor;
		color.a = startAlpha * fadeRatio;
		spriteRenderer.color = color;
		
		// 지속 시간이 끝나면 파괴
		if (elapsedTime >= duration)
		{
			Destroy(gameObject);
		}
	}
}

