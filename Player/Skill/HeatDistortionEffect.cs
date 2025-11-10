using UnityEngine;

/// <summary>
/// 열 왜곡 효과 컨트롤러
/// AOE 충격파 범위에서 배경을 아지랑이처럼 일렁거리게 만듭니다
/// </summary>
public class HeatDistortionEffect : MonoBehaviour
{
	[Header("왜곡 설정")]
	[Tooltip("왜곡 강도 (0~0.1)")]
	[SerializeField] private float distortionStrength = 0.03f;
	
	[Tooltip("왜곡 속도")]
	[SerializeField] private float distortionSpeed = 2f;
	
	[Tooltip("페이드 인 시간 (초)")]
	[SerializeField] private float fadeInTime = 0.2f;
	
	[Tooltip("최대 지속 시간 (초)")]
	[SerializeField] private float maxDuration = 3f;
	
	[Tooltip("페이드 아웃 시간 (초)")]
	[SerializeField] private float fadeOutTime = 0.5f;
	
	[Tooltip("틴트 컬러 (열기 색상)")]
	[SerializeField] private Color tintColor = new Color(1f, 0.8f, 0.6f, 0.5f);
	
	private Material distortionMaterial;
	private SpriteRenderer spriteRenderer;
	private float elapsedTime = 0f;
	private float currentAlpha = 0f;
	
	/// <summary>
	/// 초기화
	/// </summary>
	private void Awake()
	{
		// SpriteRenderer 생성 (없으면)
		spriteRenderer = GetComponent<SpriteRenderer>();
		if (spriteRenderer == null)
		{
			spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
		}
		
		// 기본 스프라이트 생성 (흰색 원형)
		if (spriteRenderer.sprite == null)
		{
			spriteRenderer.sprite = CreateCircleSprite();
		}
		
		// 열 왜곡 쉐이더 적용
		Shader distortionShader = Shader.Find("Custom/HeatDistortion");
		if (distortionShader != null)
		{
			distortionMaterial = new Material(distortionShader);
			
			// 노이즈 텍스처 생성 또는 로드
			Texture2D noiseTexture = GenerateNoiseTexture(256, 256);
			distortionMaterial.SetTexture("_MainTex", noiseTexture);
			distortionMaterial.SetFloat("_DistortionStrength", distortionStrength);
			distortionMaterial.SetFloat("_Speed", distortionSpeed);
			distortionMaterial.SetColor("_Color", tintColor);
			
			spriteRenderer.material = distortionMaterial;
			spriteRenderer.sortingOrder = 100; // 높은 순서로 설정 (다른 이펙트 위에)
		}
		else
		{
			Debug.LogWarning("[HeatDistortionEffect] Custom/HeatDistortion 쉐이더를 찾을 수 없습니다!");
		}
	}
	
	/// <summary>
	/// 매 프레임 업데이트
	/// </summary>
	private void Update()
	{
		elapsedTime += Time.deltaTime;
		
		// 알파값 계산 (페이드 인 -> 유지 -> 페이드 아웃)
		if (elapsedTime < fadeInTime)
		{
			// 페이드 인
			currentAlpha = Mathf.Lerp(0f, tintColor.a, elapsedTime / fadeInTime);
		}
		else if (elapsedTime < maxDuration - fadeOutTime)
		{
			// 유지
			currentAlpha = tintColor.a;
		}
		else if (elapsedTime < maxDuration)
		{
			// 페이드 아웃
			float fadeOutProgress = (elapsedTime - (maxDuration - fadeOutTime)) / fadeOutTime;
			currentAlpha = Mathf.Lerp(tintColor.a, 0f, fadeOutProgress);
		}
		else
		{
			// 지속 시간 종료
			Destroy(gameObject);
			return;
		}
		
		// 머티리얼 알파값 업데이트
		if (distortionMaterial != null)
		{
			Color color = tintColor;
			color.a = currentAlpha;
			distortionMaterial.SetColor("_Color", color);
		}
	}
	
	/// <summary>
	/// 원형 스프라이트 생성
	/// </summary>
	private Sprite CreateCircleSprite()
	{
		int size = 128;
		Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
		Color[] pixels = new Color[size * size];
		
		Vector2 center = new Vector2(size / 2f, size / 2f);
		float radius = size / 2f;
		
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float distance = Vector2.Distance(new Vector2(x, y), center);
				float alpha = 1f - Mathf.Clamp01(distance / radius);
				alpha = Mathf.SmoothStep(0f, 1f, alpha); // 부드러운 가장자리
				
				pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
			}
		}
		
		texture.SetPixels(pixels);
		texture.Apply();
		
		return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
	}
	
	/// <summary>
	/// 노이즈 텍스처 생성 (펄린 노이즈)
	/// </summary>
	private Texture2D GenerateNoiseTexture(int width, int height)
	{
		Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
		texture.wrapMode = TextureWrapMode.Repeat;
		
		Color[] pixels = new Color[width * height];
		
		// 펄린 노이즈 스케일
		float scale = 5f;
		
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				// 여러 주파수의 노이즈 조합 (프랙탈 노이즈)
				float xCoord = (float)x / width * scale;
				float yCoord = (float)y / height * scale;
				
				float noise1 = Mathf.PerlinNoise(xCoord, yCoord);
				float noise2 = Mathf.PerlinNoise(xCoord * 2f, yCoord * 2f) * 0.5f;
				float noise3 = Mathf.PerlinNoise(xCoord * 4f, yCoord * 4f) * 0.25f;
				
				float noise = (noise1 + noise2 + noise3) / 1.75f;
				
				pixels[y * width + x] = new Color(noise, noise, noise, 1f);
			}
		}
		
		texture.SetPixels(pixels);
		texture.Apply();
		
		return texture;
	}
	
	/// <summary>
	/// 정리
	/// </summary>
	private void OnDestroy()
	{
		if (distortionMaterial != null)
		{
			Destroy(distortionMaterial);
		}
	}
}

