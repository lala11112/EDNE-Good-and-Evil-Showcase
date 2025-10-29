using UnityEngine;

/// <summary>
/// 부채꼴 범위 시각화 컴포넌트
/// 스킬의 부채꼴 공격 범위를 메시로 표시합니다
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class FanAreaVisualizer : MonoBehaviour
{
	[Header("부채꼴 설정")]
	[Tooltip("부채꼴 각도 (도)")]
	[SerializeField] private float fanAngle = 170f;
	
	[Tooltip("부채꼴 반경")]
	[SerializeField] private float radius = 3f;
	
	[Tooltip("메시 해상도 (삼각형 개수)")]
	[SerializeField] private int segments = 30;
	
	[Header("스트라이크 존 설정")]
	[Tooltip("스트라이크 존 표시 여부")]
	[SerializeField] private bool showStrikeZone = true;
	
	[Tooltip("스트라이크 존 두께")]
	[SerializeField] private float strikeZoneThickness = 0.5f;
	
	[Header("색상 설정")]
	[Tooltip("일반 범위 색상")]
	[SerializeField] private Color normalAreaColor = new Color(1f, 1f, 0f, 0.3f); // 노란색 반투명
	
	[Tooltip("스트라이크 존 색상")]
	[SerializeField] private Color strikeZoneColor = new Color(1f, 0f, 0f, 0.5f); // 빨간색 반투명
	
	[Header("애니메이션 설정")]
	[Tooltip("자동으로 페이드아웃 여부")]
	[SerializeField] private bool autoFadeOut = true;
	
	[Tooltip("페이드아웃 시간 (초)")]
	[SerializeField] private float fadeOutDuration = 0.3f;
	
	[Tooltip("최대 표시 시간 (초)")]
	[SerializeField] private float maxDisplayTime = 0.5f;
	
	private MeshFilter meshFilter;
	private MeshRenderer meshRenderer;
	private Material material;
	private float elapsedTime = 0f;
	
	private void Awake()
	{
		meshFilter = GetComponent<MeshFilter>();
		meshRenderer = GetComponent<MeshRenderer>();
		
		// 머티리얼 생성 (Transparent Shader 필요)
		CreateMaterial();
		
		// 초기 메시 생성
		GenerateFanMesh();
	}
	
	private void Update()
	{
		if (autoFadeOut)
		{
			elapsedTime += Time.deltaTime;
			
			// 페이드아웃 처리
			if (elapsedTime > maxDisplayTime - fadeOutDuration)
			{
				float fadeProgress = (elapsedTime - (maxDisplayTime - fadeOutDuration)) / fadeOutDuration;
				float alpha = Mathf.Lerp(1f, 0f, fadeProgress);
				
				if (material != null)
				{
					Color color = material.color;
					color.a = alpha * normalAreaColor.a;
					material.color = color;
				}
			}
			
			// 시간 초과 시 파괴
			if (elapsedTime >= maxDisplayTime)
			{
				Destroy(gameObject);
			}
		}
	}
	
	/// <summary>
	/// 부채꼴 메시 생성
	/// </summary>
	private void GenerateFanMesh()
	{
		Mesh mesh = new Mesh();
		mesh.name = "FanAreaMesh";
		
		// 메시 정점 및 삼각형 생성
		int vertexCount = segments + 2; // 중심점 + 외곽 정점들 + 마지막 정점
		Vector3[] vertices = new Vector3[vertexCount];
		int[] triangles = new int[segments * 3];
		Vector2[] uvs = new Vector2[vertexCount];
		Color[] colors = new Color[vertexCount];
		
		// 중심점 (0, 0, 0)
		vertices[0] = Vector3.zero;
		uvs[0] = new Vector2(0.5f, 0.5f);
		colors[0] = normalAreaColor;
		
		// 부채꼴 각도 계산
		float halfAngle = fanAngle / 2f;
		float angleStep = fanAngle / segments;
		
		// 외곽 정점 생성
		// 기본 방향: 위쪽(Y+) = 0도
		// Unity 2D: Y+가 위, X+가 오른쪽
		for (int i = 0; i <= segments; i++)
		{
			// 중심을 0도로 하고 좌우로 halfAngle만큼 퍼짐
			float currentAngle = -halfAngle + (angleStep * i);
			float radian = currentAngle * Mathf.Deg2Rad;
			
			// Y축이 위쪽(0도), X축이 오른쪽(90도)
			float x = Mathf.Sin(radian) * radius;
			float y = Mathf.Cos(radian) * radius;
			
			vertices[i + 1] = new Vector3(x, y, 0);
			uvs[i + 1] = new Vector2((x / radius + 1f) * 0.5f, (y / radius + 1f) * 0.5f);
			
			// 스트라이크 존 색상 적용
			float distanceFromCenter = Vector3.Distance(Vector3.zero, vertices[i + 1]);
			if (showStrikeZone && distanceFromCenter >= radius - strikeZoneThickness)
			{
				colors[i + 1] = strikeZoneColor;
			}
			else
			{
				colors[i + 1] = normalAreaColor;
			}
		}
		
		// 삼각형 생성
		for (int i = 0; i < segments; i++)
		{
			triangles[i * 3] = 0;
			triangles[i * 3 + 1] = i + 1;
			triangles[i * 3 + 2] = i + 2;
		}
		
		// 메시 설정
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;
		mesh.colors = colors;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		
		meshFilter.mesh = mesh;
	}
	
	/// <summary>
	/// 투명 머티리얼 생성
	/// </summary>
	private void CreateMaterial()
	{
		// Unity 기본 Sprites-Default 셰이더 사용 (2D 게임에 최적화)
		Shader shader = Shader.Find("Sprites/Default");
		
		if (shader == null)
		{
			// Sprites 셰이더가 없으면 Unlit/Transparent 사용
			shader = Shader.Find("Unlit/Transparent");
		}
		
		if (shader == null)
		{
			Debug.LogError("[FanAreaVisualizer] 적절한 셰이더를 찾을 수 없습니다!");
			return;
		}
		
		material = new Material(shader);
		material.color = normalAreaColor;
		
		// 렌더링 설정
		material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		material.SetInt("_ZWrite", 0);
		material.renderQueue = 3000; // Transparent
		
		meshRenderer.material = material;
		meshRenderer.sortingLayerName = "Default";
		meshRenderer.sortingOrder = 100; // 최상위에 표시
	}
	
	/// <summary>
	/// 부채꼴 설정 변경 (런타임)
	/// </summary>
	public void SetFanParameters(float angle, float range, float strikeThickness)
	{
		fanAngle = angle;
		radius = range;
		strikeZoneThickness = strikeThickness;
		GenerateFanMesh();
	}
	
	/// <summary>
	/// 색상 변경 (런타임)
	/// </summary>
	public void SetColors(Color normalColor, Color strikeColor)
	{
		normalAreaColor = normalColor;
		strikeZoneColor = strikeColor;
		
		if (material != null)
		{
			material.color = normalColor;
		}
		
		GenerateFanMesh();
	}
	
	/// <summary>
	/// 페이드아웃 시작
	/// </summary>
	public void StartFadeOut(float duration)
	{
		autoFadeOut = true;
		fadeOutDuration = duration;
		maxDisplayTime = duration;
		elapsedTime = 0f;
	}
}

