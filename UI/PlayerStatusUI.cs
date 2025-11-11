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
	
	#region UI 컴포넌트 - 용기 시스템
	
	[Header("용기 시스템 UI")]
	[Tooltip("현재 랭크 표시 텍스트 (S, A, B, C, D)")]
	[SerializeField] private TextMeshProUGUI rankText;
	
	[Tooltip("용기 점수 원형 슬라이더 (360도 Filled Image)")]
	[SerializeField] private Image courageCircleImage;
	
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
	
	[Header("용기 랭크별 색상")]
	[Tooltip("S랭크 색상 (금색)")]
	[SerializeField] private Color rankSColor = new Color(1f, 0.84f, 0f); // 금색
	
	[Tooltip("A랭크 색상 (빨간색)")]
	[SerializeField] private Color rankAColor = new Color(1f, 0.2f, 0.2f); // 빨간색
	
	[Tooltip("B랭크 색상 (주황색)")]
	[SerializeField] private Color rankBColor = new Color(1f, 0.65f, 0f); // 주황색
	
	[Tooltip("C랭크 색상 (녹색)")]
	[SerializeField] private Color rankCColor = new Color(0.2f, 1f, 0.2f); // 녹색
	
	[Tooltip("D랭크 색상 (회색)")]
	[SerializeField] private Color rankDColor = new Color(0.5f, 0.5f, 0.5f); // 회색
	
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
	private float targetCourageFillAmount; // 용기 원형 슬라이더 목표값
	
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
		PlayerStatusEvents.OnCourageChanged += OnCourageChanged;
		
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
		PlayerStatusEvents.OnCourageChanged -= OnCourageChanged;
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
		
		// ⭐ 용기 시스템은 이벤트 기반으로 업데이트되므로 Update에서 호출 불필요
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
		
		if (rankText == null)
			Debug.LogWarning("[PlayerStatusUI] 랭크 텍스트가 할당되지 않았습니다.");
		
		if (courageCircleImage == null)
			Debug.LogError("[PlayerStatusUI] 용기 원형 슬라이더 이미지가 할당되지 않았습니다!");
		
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
		targetCourageFillAmount = 0f; // 용기 슬라이더 목표값 초기화
		
		// 색상 초기화
		if (healthFillImage != null)
			healthFillImage.color = healthHighColor;
		
		if (mentalPowerFillImage != null)
			mentalPowerFillImage.color = mentalPowerColor;
		
		// 용기 원형 이미지 초기화
		if (courageCircleImage != null)
		{
			courageCircleImage.type = Image.Type.Filled;
			courageCircleImage.fillMethod = Image.FillMethod.Radial360;
			courageCircleImage.fillOrigin = (int)Image.Origin360.Top; // 12시 방향부터 시작
			courageCircleImage.fillClockwise = true; // 시계 방향으로 채우기
			courageCircleImage.fillAmount = 0f; // 초기값 0%
		}
		
		// 랭크 텍스트 초기화
		if (rankText != null)
		{
			rankText.text = "D"; // 기본 랭크
			rankText.color = rankDColor; // D랭크 색상
		}
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
			
			// 용기 시스템 갱신 (이벤트 핸들러 직접 호출)
			OnCourageChanged(PlayerController.Instance.CourageScore, PlayerController.Instance.CurrentCourageState);
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
	
	/// <summary>
	/// 용기 점수 변경 이벤트 처리
	/// PlayerStatusEvents.OnCourageChanged에서 호출됨
	/// </summary>
	/// <param name="courageScore">현재 용기 점수</param>
	/// <param name="courageState">현재 용기 단계</param>
	private void OnCourageChanged(int courageScore, CourageState courageState)
	{
		if (rankText == null || courageCircleImage == null) return;
		
		if (PlayerController.Instance == null || PlayerController.Instance.PlayerData == null)
		{
			// PlayerController가 없으면 기본값 표시
			rankText.text = "D";
			rankText.color = rankDColor;
			targetCourageFillAmount = 0f; // 목표값 설정
			courageCircleImage.color = rankDColor;
			return;
		}
		
		// 현재 스킬 랭크 가져오기
		SkillRank currentRank = PlayerController.Instance.CurrentSkillRank;
		
		// 현재 랭크 텍스트 업데이트
		rankText.text = currentRank.ToString();
		
		// 랭크별 색상 가져오기
		Color rankColor = GetRankColor(currentRank);
		rankText.color = rankColor;
		courageCircleImage.color = rankColor;
		
		// 다음 랭크까지의 진행률 계산
		float progressToNextRank = CalculateProgressToNextRank(courageScore, currentRank);
		
		// ⭐ 원형 슬라이더 목표값 설정 (애니메이션으로 부드럽게 전환)
		targetCourageFillAmount = progressToNextRank;
		
#if UNITY_EDITOR
		Debug.Log($"[PlayerStatusUI] 용기 UI 업데이트: 랭크 {currentRank}, 점수 {courageScore}, 진행률 {progressToNextRank * 100:F1}%");
#endif
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
		
		// 용기 원형 슬라이더 애니메이션
		if (courageCircleImage != null)
		{
			float currentValue = courageCircleImage.fillAmount;
			float newValue = Mathf.Lerp(currentValue, targetCourageFillAmount, Time.deltaTime * sliderAnimationSpeed);
			courageCircleImage.fillAmount = newValue;
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
	
	#region 용기 시스템 UI (헬퍼 메서드)
	
	/// <summary>
	/// 현재 랭크에 따른 색상 반환
	/// </summary>
	/// <param name="rank">현재 스킬 랭크</param>
	/// <returns>해당 랭크의 색상</returns>
	private Color GetRankColor(SkillRank rank)
	{
		switch (rank)
		{
			case SkillRank.S:
				return rankSColor;
			case SkillRank.A:
				return rankAColor;
			case SkillRank.B:
				return rankBColor;
			case SkillRank.C:
				return rankCColor;
			case SkillRank.D:
			default:
				return rankDColor;
		}
	}
	
	/// <summary>
	/// 다음 랭크까지의 진행률 계산 (0~1 범위)
	/// </summary>
	/// <param name="currentScore">현재 용기 점수</param>
	/// <param name="currentRank">현재 스킬 랭크</param>
	/// <returns>다음 랭크까지의 진행률 (0~1)</returns>
	private float CalculateProgressToNextRank(int currentScore, SkillRank currentRank)
	{
		if (PlayerController.Instance == null || PlayerController.Instance.PlayerData == null)
			return 0f;
		
		PlayerDataSO playerData = PlayerController.Instance.PlayerData;
		
		// 현재 랭크의 시작 점수와 다음 랭크의 요구 점수 가져오기
		int currentRankStartScore = GetRankStartScore(currentRank, playerData);
		int nextRankRequiredScore = GetNextRankRequiredScore(currentRank, playerData);
		
		// S랭크는 최대 랭크이므로 진행률 100%로 고정
		if (currentRank == SkillRank.S)
		{
			return 1f;
		}
		
		// 진행률 계산: (현재 점수 - 현재 랭크 시작 점수) / (다음 랭크 요구 점수 - 현재 랭크 시작 점수)
		int scoreInCurrentRank = currentScore - currentRankStartScore;
		int scoreRangeForRank = nextRankRequiredScore - currentRankStartScore;
		
		if (scoreRangeForRank <= 0)
			return 1f; // 분모가 0이면 100%로 처리
		
		float progress = (float)scoreInCurrentRank / scoreRangeForRank;
		
		// 0~1 범위로 제한
		return Mathf.Clamp01(progress);
	}
	
	/// <summary>
	/// 현재 랭크의 시작 점수 가져오기
	/// </summary>
	/// <param name="rank">현재 스킬 랭크</param>
	/// <param name="playerData">플레이어 데이터 SO</param>
	/// <returns>현재 랭크 시작 점수</returns>
	private int GetRankStartScore(SkillRank rank, PlayerDataSO playerData)
	{
		switch (rank)
		{
			case SkillRank.S:
				return playerData.confidenceStepRequirement; // S랭크 시작 점수 (Confidence)
			case SkillRank.A:
				return playerData.determinationStepRequirement; // A랭크 시작 점수 (Determination)
			case SkillRank.B:
				return playerData.courageStepRequirement; // B랭크 시작 점수 (Courage)
			case SkillRank.C:
				return playerData.doubtStepRequirement; // C랭크 시작 점수 (Doubt)
			case SkillRank.D:
			default:
				return playerData.anxietyStepRequirement; // D랭크 시작 점수 (Anxiety)
		}
	}
	
	/// <summary>
	/// 다음 랭크의 요구 점수 가져오기
	/// </summary>
	/// <param name="rank">현재 스킬 랭크</param>
	/// <param name="playerData">플레이어 데이터 SO</param>
	/// <returns>다음 랭크 요구 점수</returns>
	private int GetNextRankRequiredScore(SkillRank rank, PlayerDataSO playerData)
	{
		switch (rank)
		{
			case SkillRank.S:
				return playerData.confidenceStepRequirement; // S랭크는 최대이므로 자기 자신 반환
			case SkillRank.A:
				return playerData.confidenceStepRequirement; // A→S (Confidence)
			case SkillRank.B:
				return playerData.determinationStepRequirement; // B→A (Determination)
			case SkillRank.C:
				return playerData.courageStepRequirement; // C→B (Courage)
			case SkillRank.D:
			default:
				return playerData.doubtStepRequirement; // D→C (Doubt)
		}
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

