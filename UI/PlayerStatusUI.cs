using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 플레이어 상태 UI 컨트롤러
/// 옵저버 패턴을 사용하여 PlayerStatusEvents를 구독합니다
/// </summary>
public class PlayerStatusUI : MonoBehaviour
{
	#region UI 컴포넌트 - 체력
	
	[Header("체력 UI")]
	[Tooltip("체력 슬라이더")]
	[SerializeField] private Slider healthSlider;
	
	[Tooltip("체력 수치 텍스트 (현재/최대)")]
	[SerializeField] private TextMeshProUGUI healthText;
	
	[Tooltip("체력바 채우기 이미지 (색상 변경용)")]
	[SerializeField] private Image healthFillImage;
	
	#endregion
	
	#region UI 컴포넌트 - 정신력
	
	[Header("정신력 UI")]
	[Tooltip("정신력 슬라이더")]
	[SerializeField] private Slider mentalPowerSlider;
	
	[Tooltip("정신력 수치 텍스트 (현재/최대)")]
	[SerializeField] private TextMeshProUGUI mentalPowerText;
	
	[Tooltip("정신력바 채우기 이미지 (색상 변경용)")]
	[SerializeField] private Image mentalPowerFillImage;
	
	#endregion
	
	#region 색상 설정
	
	[Header("체력바 색상")]
	[Tooltip("체력이 높을 때 색상 (70% 이상)")]
	[SerializeField] private Color healthHighColor = new Color(0.2f, 0.8f, 0.2f); // 초록색
	
	[Tooltip("체력이 중간일 때 색상 (30~70%)")]
	[SerializeField] private Color healthMidColor = new Color(1f, 0.8f, 0f); // 노란색
	
	[Tooltip("체력이 낮을 때 색상 (30% 이하)")]
	[SerializeField] private Color healthLowColor = new Color(1f, 0.2f, 0.2f); // 빨간색
	
	[Header("정신력바 색상")]
	[Tooltip("정신력 바 기본 색상")]
	[SerializeField] private Color mentalPowerColor = new Color(0.2f, 0.5f, 1f); // 파란색
	
	[Tooltip("정신력이 부족할 때 색상 (30% 이하)")]
	[SerializeField] private Color mentalPowerLowColor = new Color(0.6f, 0.3f, 1f); // 보라색
	
	#endregion
	
	#region 애니메이션 설정
	
	[Header("애니메이션")]
	[Tooltip("슬라이더 애니메이션 속도")]
	[SerializeField] private float sliderAnimationSpeed = 5f;
	
	[Tooltip("텍스트 깜빡임 효과 사용 여부 (데미지/회복 시)")]
	[SerializeField] private bool useTextBlink = true;
	
	[Tooltip("텍스트 깜빡임 지속 시간")]
	[SerializeField] private float textBlinkDuration = 0.3f;
	
	#endregion
	
	#region 런타임 상태
	
	// 슬라이더 애니메이션용 목표값
	private float targetHealthValue;
	private float targetMentalPowerValue;
	
	// 텍스트 깜빡임 타이머
	private float healthBlinkTimer;
	private float mentalPowerBlinkTimer;
	
	#endregion
	
	#region Unity Lifecycle
	
	/// <summary>
	/// 초기화
	/// </summary>
	private void Awake()
	{
		// UI 컴포넌트 유효성 검사
		ValidateComponents();
		
		// 초기값 설정
		InitializeUI();
	}
	
	/// <summary>
	/// 이벤트 구독 시작
	/// </summary>
	private void OnEnable()
	{
		// 옵저버 패턴: 이벤트 구독
		PlayerStatusEvents.OnHealthChanged += OnHealthChanged;
		PlayerStatusEvents.OnMentalPowerChanged += OnMentalPowerChanged;
		
		// PlayerController에서 현재 상태 가져오기
		RefreshFromPlayer();
	}
	
	/// <summary>
	/// 이벤트 구독 해제
	/// </summary>
	private void OnDisable()
	{
		// 옵저버 패턴: 이벤트 구독 해제 (메모리 누수 방지)
		PlayerStatusEvents.OnHealthChanged -= OnHealthChanged;
		PlayerStatusEvents.OnMentalPowerChanged -= OnMentalPowerChanged;
	}
	
	/// <summary>
	/// 매 프레임 업데이트
	/// </summary>
	private void Update()
	{
		// 슬라이더 애니메이션
		AnimateSliders();
		
		// 텍스트 깜빡임 효과
		UpdateTextBlink(Time.deltaTime);
	}
	
	#endregion
	
	#region 초기화
	
	/// <summary>
	/// UI 컴포넌트 유효성 검사
	/// </summary>
	private void ValidateComponents()
	{
		if (healthSlider == null)
			Debug.LogError("[PlayerStatusUI] 체력 슬라이더가 할당되지 않았습니다!");
		
		if (mentalPowerSlider == null)
			Debug.LogError("[PlayerStatusUI] 정신력 슬라이더가 할당되지 않았습니다!");
		
		if (healthText == null)
			Debug.LogWarning("[PlayerStatusUI] 체력 텍스트가 할당되지 않았습니다.");
		
		if (mentalPowerText == null)
			Debug.LogWarning("[PlayerStatusUI] 정신력 텍스트가 할당되지 않았습니다.");
		
		// Fill Image 자동 할당 시도
		if (healthFillImage == null && healthSlider != null)
		{
			healthFillImage = healthSlider.fillRect?.GetComponent<Image>();
		}
		
		if (mentalPowerFillImage == null && mentalPowerSlider != null)
		{
			mentalPowerFillImage = mentalPowerSlider.fillRect?.GetComponent<Image>();
		}
	}
	
	/// <summary>
	/// UI 초기값 설정
	/// </summary>
	private void InitializeUI()
	{
		// 슬라이더 범위 설정 (0~1)
		if (healthSlider != null)
		{
			healthSlider.minValue = 0f;
			healthSlider.maxValue = 1f;
			healthSlider.value = 1f;
		}
		
		if (mentalPowerSlider != null)
		{
			mentalPowerSlider.minValue = 0f;
			mentalPowerSlider.maxValue = 1f;
			mentalPowerSlider.value = 1f;
		}
		
		// 목표값 초기화
		targetHealthValue = 1f;
		targetMentalPowerValue = 1f;
		
		// 색상 초기화
		if (healthFillImage != null)
			healthFillImage.color = healthHighColor;
		
		if (mentalPowerFillImage != null)
			mentalPowerFillImage.color = mentalPowerColor;
	}
	
	/// <summary>
	/// PlayerController에서 현재 상태를 가져와 UI 갱신
	/// </summary>
	private void RefreshFromPlayer()
	{
		if (PlayerController.Instance != null)
		{
			// 체력 갱신
			OnHealthChanged(PlayerController.Instance.CurrentHealth, PlayerController.Instance.MaxHealth);
			
			// 정신력 갱신
			OnMentalPowerChanged(PlayerController.Instance.CurrentMentalPower, PlayerController.Instance.MaxMentalPower);
		}
	}
	
	#endregion
	
	#region 이벤트 핸들러 (옵저버 패턴)
	
	/// <summary>
	/// 체력 변경 이벤트 처리
	/// PlayerStatusEvents.OnHealthChanged에서 호출됨
	/// </summary>
	/// <param name="currentHealth">현재 체력</param>
	/// <param name="maxHealth">최대 체력</param>
	private void OnHealthChanged(int currentHealth, int maxHealth)
	{
		if (maxHealth <= 0)
		{
			Debug.LogWarning("[PlayerStatusUI] 최대 체력이 0 이하입니다!");
			return;
		}
		
		// 슬라이더 목표값 설정 (0~1 범위로 정규화)
		targetHealthValue = (float)currentHealth / maxHealth;
		
		// 텍스트 업데이트
		if (healthText != null)
		{
			healthText.text = $"{currentHealth} / {maxHealth}";
		}
		
		// 체력바 색상 업데이트
		UpdateHealthColor(targetHealthValue);
		
		// 깜빡임 효과 시작
		if (useTextBlink)
		{
			healthBlinkTimer = textBlinkDuration;
		}
	}
	
	/// <summary>
	/// 정신력 변경 이벤트 처리
	/// PlayerStatusEvents.OnMentalPowerChanged에서 호출됨
	/// </summary>
	/// <param name="currentMentalPower">현재 정신력</param>
	/// <param name="maxMentalPower">최대 정신력</param>
	private void OnMentalPowerChanged(int currentMentalPower, int maxMentalPower)
	{
		if (maxMentalPower <= 0)
		{
			Debug.LogWarning("[PlayerStatusUI] 최대 정신력이 0 이하입니다!");
			return;
		}
		
		// 슬라이더 목표값 설정 (0~1 범위로 정규화)
		targetMentalPowerValue = (float)currentMentalPower / maxMentalPower;
		
		// 텍스트 업데이트
		if (mentalPowerText != null)
		{
			mentalPowerText.text = $"{currentMentalPower} / {maxMentalPower}";
		}
		
		// 정신력바 색상 업데이트
		UpdateMentalPowerColor(targetMentalPowerValue);
		
		// 깜빡임 효과 시작
		if (useTextBlink)
		{
			mentalPowerBlinkTimer = textBlinkDuration;
		}
	}
	
	#endregion
	
	#region 애니메이션
	
	/// <summary>
	/// 슬라이더 부드럽게 애니메이션
	/// </summary>
	private void AnimateSliders()
	{
		// 체력 슬라이더 애니메이션
		if (healthSlider != null)
		{
			float currentValue = healthSlider.value;
			float newValue = Mathf.Lerp(currentValue, targetHealthValue, Time.deltaTime * sliderAnimationSpeed);
			healthSlider.value = newValue;
		}
		
		// 정신력 슬라이더 애니메이션
		if (mentalPowerSlider != null)
		{
			float currentValue = mentalPowerSlider.value;
			float newValue = Mathf.Lerp(currentValue, targetMentalPowerValue, Time.deltaTime * sliderAnimationSpeed);
			mentalPowerSlider.value = newValue;
		}
	}
	
	/// <summary>
	/// 텍스트 깜빡임 효과 업데이트
	/// </summary>
	private void UpdateTextBlink(float deltaTime)
	{
		// 체력 텍스트 깜빡임
		if (healthBlinkTimer > 0f)
		{
			healthBlinkTimer -= deltaTime;
			
			if (healthText != null)
			{
				// 깜빡임 효과 (투명도 조절)
				float alpha = Mathf.PingPong(healthBlinkTimer * 10f, 1f);
				Color textColor = healthText.color;
				textColor.a = Mathf.Lerp(0.5f, 1f, alpha);
				healthText.color = textColor;
			}
		}
		else if (healthText != null)
		{
			// 깜빡임 종료 - 완전 불투명
			Color textColor = healthText.color;
			textColor.a = 1f;
			healthText.color = textColor;
		}
		
		// 정신력 텍스트 깜빡임
		if (mentalPowerBlinkTimer > 0f)
		{
			mentalPowerBlinkTimer -= deltaTime;
			
			if (mentalPowerText != null)
			{
				// 깜빡임 효과 (투명도 조절)
				float alpha = Mathf.PingPong(mentalPowerBlinkTimer * 10f, 1f);
				Color textColor = mentalPowerText.color;
				textColor.a = Mathf.Lerp(0.5f, 1f, alpha);
				mentalPowerText.color = textColor;
			}
		}
		else if (mentalPowerText != null)
		{
			// 깜빡임 종료 - 완전 불투명
			Color textColor = mentalPowerText.color;
			textColor.a = 1f;
			mentalPowerText.color = textColor;
		}
	}
	
	#endregion
	
	#region 색상 관리
	
	/// <summary>
	/// 체력 퍼센테이지에 따라 색상 업데이트
	/// </summary>
	/// <param name="percentage">체력 퍼센테이지 (0~1)</param>
	private void UpdateHealthColor(float percentage)
	{
		if (healthFillImage == null) return;
		
		Color targetColor;
		
		if (percentage > 0.7f)
		{
			// 70% 초과: 초록색
			targetColor = healthHighColor;
		}
		else if (percentage > 0.3f)
		{
			// 30~70%: 초록색 → 노란색 보간
			float t = (percentage - 0.3f) / 0.4f; // 0.3~0.7을 0~1로 정규화
			targetColor = Color.Lerp(healthMidColor, healthHighColor, t);
		}
		else
		{
			// 30% 이하: 노란색 → 빨간색 보간
			float t = percentage / 0.3f; // 0~0.3을 0~1로 정규화
			targetColor = Color.Lerp(healthLowColor, healthMidColor, t);
		}
		
		healthFillImage.color = targetColor;
	}
	
	/// <summary>
	/// 정신력 퍼센테이지에 따라 색상 업데이트
	/// </summary>
	/// <param name="percentage">정신력 퍼센테이지 (0~1)</param>
	private void UpdateMentalPowerColor(float percentage)
	{
		if (mentalPowerFillImage == null) return;
		
		Color targetColor;
		
		if (percentage > 0.3f)
		{
			// 30% 초과: 기본 파란색
			targetColor = mentalPowerColor;
		}
		else
		{
			// 30% 이하: 파란색 → 보라색 보간
			float t = percentage / 0.3f; // 0~0.3을 0~1로 정규화
			targetColor = Color.Lerp(mentalPowerLowColor, mentalPowerColor, t);
		}
		
		mentalPowerFillImage.color = targetColor;
	}
	
	#endregion
	
	#region 수동 갱신 (디버그/테스트용)
	
	/// <summary>
	/// UI 강제 갱신 (외부 호출용)
	/// </summary>
	[ContextMenu("UI 강제 갱신")]
	public void ForceRefresh()
	{
		RefreshFromPlayer();
	}
	
	#endregion
}

