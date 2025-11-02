using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 개별 스킬 슬롯 UI (롤 스타일)
/// 스킬 아이콘, 쿨타임 오버레이, 키 표시 등을 관리합니다
/// </summary>
public class SkillSlotUI : MonoBehaviour
{
	#region 슬롯 설정
	
	[Header("슬롯 설정")]
	[Tooltip("이 UI가 표시할 스킬 슬롯 타입")]
	[SerializeField] private SkillSlotType slotType;
	
	#endregion
	
	#region UI 컴포넌트
	
	[Header("스킬 아이콘")]
	[Tooltip("스킬 아이콘 이미지")]
	[SerializeField] private Image skillIconImage;
	
	[Tooltip("아이콘 없을 때 기본 스프라이트")]
	[SerializeField] private Sprite defaultIconSprite;
	
	[Header("쿨타임 오버레이 (롤 스타일)")]
	[Tooltip("쿨타임 오버레이 이미지 (Radial Fill)")]
	[SerializeField] private Image cooldownOverlayImage;
	
	[Tooltip("쿨타임 숫자 텍스트")]
	[SerializeField] private TextMeshProUGUI cooldownText;
	
	[Header("키 표시")]
	[Tooltip("키 텍스트 (Z, X, C, V)")]
	[SerializeField] private TextMeshProUGUI keyText;
	
	[Tooltip("키 배경 이미지")]
	[SerializeField] private Image keyBackgroundImage;
	
	[Header("사용 불가 표시")]
	[Tooltip("사용 불가 시 어두운 오버레이")]
	[SerializeField] private Image unavailableOverlayImage;
	
	#endregion
	
	#region 색상 설정
	
	[Header("색상 설정")]
	[Tooltip("쿨타임 오버레이 색상")]
	[SerializeField] private Color cooldownOverlayColor = new Color(0f, 0f, 0f, 0.7f);
	
	[Tooltip("사용 가능 시 테두리 색상")]
	[SerializeField] private Color availableBorderColor = Color.white;
	
	[Tooltip("사용 불가 시 테두리 색상")]
	[SerializeField] private Color unavailableBorderColor = Color.gray;
	
	#endregion
	
	#region 애니메이션 설정
	
	[Header("애니메이션")]
	[Tooltip("스킬 사용 시 플래시 효과")]
	[SerializeField] private bool useFlashEffect = true;
	
	[Tooltip("플래시 색상")]
	[SerializeField] private Color flashColor = Color.white;
	
	[Tooltip("플래시 지속 시간")]
	[SerializeField] private float flashDuration = 0.2f;
	
	#endregion
	
	#region 런타임 상태
	
	private PlayerSkillData currentSkillData;
	private bool isOnCooldown;
	private float currentCooldownTime;
	private float totalCooldownTime;
	
	// 플래시 효과 타이머
	private float flashTimer;
	private Image borderImage; // 테두리 이미지 (자동 탐색)
	
	#endregion
	
	#region Unity Lifecycle
	
	private void Awake()
	{
		// 컴포넌트 유효성 검사
		ValidateComponents();
		
		// 테두리 이미지 찾기 (이 게임오브젝트의 Image 컴포넌트)
		borderImage = GetComponent<Image>();
		
		// 초기 설정
		InitializeUI();
	}
	
	private void OnEnable()
	{
		// 이벤트 구독
		SkillUIEvents.OnSkillSlotChanged += OnSkillSlotChanged;
		SkillUIEvents.OnCooldownStarted += OnCooldownStarted;
		SkillUIEvents.OnCooldownUpdated += OnCooldownUpdated;
		SkillUIEvents.OnCooldownEnded += OnCooldownEnded;
		SkillUIEvents.OnSkillUsed += OnSkillUsed;
		
		// 정신력 변경 이벤트 구독 (사용 가능 여부 체크용)
		PlayerStatusEvents.OnMentalPowerChanged += OnMentalPowerChanged;
		
		// 현재 스킬 상태 새로고침
		RefreshFromSkillSlot();
	}
	
	private void OnDisable()
	{
		// 이벤트 구독 해제
		SkillUIEvents.OnSkillSlotChanged -= OnSkillSlotChanged;
		SkillUIEvents.OnCooldownStarted -= OnCooldownStarted;
		SkillUIEvents.OnCooldownUpdated -= OnCooldownUpdated;
		SkillUIEvents.OnCooldownEnded -= OnCooldownEnded;
		SkillUIEvents.OnSkillUsed -= OnSkillUsed;
		
		PlayerStatusEvents.OnMentalPowerChanged -= OnMentalPowerChanged;
	}
	
	private void Update()
	{
		// 플래시 효과 업데이트
		UpdateFlashEffect(Time.deltaTime);
	}
	
	#endregion
	
	#region 초기화
	
	private void ValidateComponents()
	{
		if (skillIconImage == null)
			Debug.LogError($"[SkillSlotUI] {slotType} - 스킬 아이콘 이미지가 할당되지 않았습니다!");
		
		if (cooldownOverlayImage == null)
			Debug.LogWarning($"[SkillSlotUI] {slotType} - 쿨타임 오버레이 이미지가 할당되지 않았습니다.");
		
		if (cooldownText == null)
			Debug.LogWarning($"[SkillSlotUI] {slotType} - 쿨타임 텍스트가 할당되지 않았습니다.");
		
		if (keyText == null)
			Debug.LogWarning($"[SkillSlotUI] {slotType} - 키 텍스트가 할당되지 않았습니다.");
	}
	
	private void InitializeUI()
	{
		// 쿨타임 오버레이 설정
		if (cooldownOverlayImage != null)
		{
			cooldownOverlayImage.color = cooldownOverlayColor;
			cooldownOverlayImage.fillMethod = Image.FillMethod.Radial360;
			cooldownOverlayImage.fillOrigin = (int)Image.Origin360.Top;
			cooldownOverlayImage.fillClockwise = false; // 시계 반대 방향
			cooldownOverlayImage.fillAmount = 0f;
			cooldownOverlayImage.gameObject.SetActive(false);
		}
		
		// 쿨타임 텍스트 초기화
		if (cooldownText != null)
		{
			cooldownText.text = "";
			cooldownText.gameObject.SetActive(false);
		}
		
		// 키 텍스트 설정
		if (keyText != null)
		{
			keyText.text = slotType.ToString();
		}
		
		// 기본 아이콘 설정
		if (skillIconImage != null && defaultIconSprite != null)
		{
			skillIconImage.sprite = defaultIconSprite;
			skillIconImage.color = Color.gray; // 스킬 없음 표시
		}
		
		// 사용 불가 오버레이 초기화
		if (unavailableOverlayImage != null)
		{
			unavailableOverlayImage.gameObject.SetActive(false);
		}
	}
	
	/// <summary>
	/// SkillSlot에서 현재 상태 가져오기
	/// </summary>
	private void RefreshFromSkillSlot()
	{
		if (PlayerController.Instance == null) return;
		
		SkillController skillController = PlayerController.Instance.GetComponent<SkillController>();
		if (skillController == null) return;
		
		SkillSlot skillSlot = skillController.GetSkillSlot(slotType);
		if (skillSlot == null) return;
		
		// 스킬 데이터 갱신
		if (skillSlot.CurrentSkillData != null)
		{
			UpdateSkillIcon(skillSlot.CurrentSkillData);
		}
		
		// 쿨타임 상태 갱신
		if (skillSlot.IsOnCooldown)
		{
			StartCooldownDisplay(skillSlot.TotalCooldown);
			UpdateCooldownDisplay(skillSlot.CurrentCooldown, skillSlot.TotalCooldown);
		}
	}
	
	#endregion
	
	#region 이벤트 핸들러
	
	/// <summary>
	/// 스킬 슬롯 변경 이벤트
	/// </summary>
	private void OnSkillSlotChanged(SkillSlotType changedSlotType, PlayerSkillData skillData)
	{
		if (changedSlotType != slotType) return;
		
		currentSkillData = skillData;
		UpdateSkillIcon(skillData);
	}
	
	/// <summary>
	/// 쿨타임 시작 이벤트
	/// </summary>
	private void OnCooldownStarted(SkillSlotType changedSlotType, float cooldownTime)
	{
		if (changedSlotType != slotType) return;
		
		StartCooldownDisplay(cooldownTime);
	}
	
	/// <summary>
	/// 쿨타임 업데이트 이벤트
	/// </summary>
	private void OnCooldownUpdated(SkillSlotType changedSlotType, float remainingTime, float totalTime)
	{
		if (changedSlotType != slotType) return;
		
		UpdateCooldownDisplay(remainingTime, totalTime);
	}
	
	/// <summary>
	/// 쿨타임 종료 이벤트
	/// </summary>
	private void OnCooldownEnded(SkillSlotType changedSlotType)
	{
		if (changedSlotType != slotType) return;
		
		EndCooldownDisplay();
	}
	
	/// <summary>
	/// 스킬 사용 이벤트
	/// </summary>
	private void OnSkillUsed(SkillSlotType changedSlotType, PlayerSkillData skillData)
	{
		if (changedSlotType != slotType) return;
		
		// 플래시 효과
		if (useFlashEffect)
		{
			StartFlashEffect();
		}
	}
	
	/// <summary>
	/// 정신력 변경 이벤트 (사용 가능 여부 업데이트)
	/// </summary>
	private void OnMentalPowerChanged(int currentMentalPower, int maxMentalPower)
	{
		UpdateAvailability();
	}
	
	#endregion
	
	#region UI 업데이트
	
	/// <summary>
	/// 스킬 아이콘 업데이트
	/// </summary>
	private void UpdateSkillIcon(PlayerSkillData skillData)
	{
		if (skillIconImage == null) return;
		
		if (skillData != null && skillData.skillIcon != null)
		{
			skillIconImage.sprite = skillData.skillIcon;
			skillIconImage.color = Color.white;
		}
		else
		{
			skillIconImage.sprite = defaultIconSprite;
			skillIconImage.color = Color.gray;
		}
	}
	
	/// <summary>
	/// 쿨타임 표시 시작
	/// </summary>
	private void StartCooldownDisplay(float totalTime)
	{
		isOnCooldown = true;
		totalCooldownTime = totalTime;
		currentCooldownTime = totalTime;
		
		if (cooldownOverlayImage != null)
		{
			cooldownOverlayImage.gameObject.SetActive(true);
			cooldownOverlayImage.fillAmount = 1f;
		}
		
		if (cooldownText != null)
		{
			cooldownText.gameObject.SetActive(true);
		}
	}
	
	/// <summary>
	/// 쿨타임 표시 업데이트
	/// </summary>
	private void UpdateCooldownDisplay(float remainingTime, float totalTime)
	{
		currentCooldownTime = remainingTime;
		totalCooldownTime = totalTime;
		
		// Radial Fill Amount 업데이트 (1.0 → 0.0으로 감소)
		if (cooldownOverlayImage != null && totalTime > 0)
		{
			float fillAmount = remainingTime / totalTime;
			cooldownOverlayImage.fillAmount = fillAmount;
		}
		
		// 쿨타임 텍스트 업데이트 (1초 이상일 때만 표시)
		if (cooldownText != null)
		{
			if (remainingTime >= 1f)
			{
				cooldownText.text = Mathf.Ceil(remainingTime).ToString();
			}
			else if (remainingTime > 0f)
			{
				cooldownText.text = remainingTime.ToString("F1");
			}
			else
			{
				cooldownText.text = "";
			}
		}
	}
	
	/// <summary>
	/// 쿨타임 표시 종료
	/// </summary>
	private void EndCooldownDisplay()
	{
		isOnCooldown = false;
		currentCooldownTime = 0f;
		
		if (cooldownOverlayImage != null)
		{
			cooldownOverlayImage.fillAmount = 0f;
			cooldownOverlayImage.gameObject.SetActive(false);
		}
		
		if (cooldownText != null)
		{
			cooldownText.text = "";
			cooldownText.gameObject.SetActive(false);
		}
		
		UpdateAvailability();
	}
	
	/// <summary>
	/// 사용 가능 여부 업데이트
	/// </summary>
	private void UpdateAvailability()
	{
		bool isAvailable = true;
		bool showUnavailableOverlay = false;
		
		// 정신력 체크 (쿨타임과 무관하게 정신력 부족 시 사용 불가 표시)
		if (currentSkillData != null && PlayerController.Instance != null)
		{
			if (PlayerController.Instance.CurrentMentalPower < currentSkillData.mentalCost)
			{
				isAvailable = false;
				showUnavailableOverlay = true; // 정신력 부족은 오버레이 표시
			}
		}
		
		// 쿨타임 체크 (테두리 색상용, 오버레이는 별도로 처리됨)
		if (isOnCooldown)
		{
			isAvailable = false;
			// 쿨타임은 UnavailableOverlay를 사용하지 않음 (CooldownOverlay 사용)
		}
		
		// 사용 불가 오버레이 표시 (정신력 부족 등 쿨타임 외의 이유)
		if (unavailableOverlayImage != null)
		{
			unavailableOverlayImage.gameObject.SetActive(showUnavailableOverlay);
		}
		
		// 테두리 색상 변경
		if (borderImage != null)
		{
			borderImage.color = isAvailable ? availableBorderColor : unavailableBorderColor;
		}
	}
	
	#endregion
	
	#region 플래시 효과
	
	/// <summary>
	/// 플래시 효과 시작
	/// </summary>
	private void StartFlashEffect()
	{
		flashTimer = flashDuration;
	}
	
	/// <summary>
	/// 플래시 효과 업데이트
	/// </summary>
	private void UpdateFlashEffect(float deltaTime)
	{
		if (flashTimer > 0f)
		{
			flashTimer -= deltaTime;
			
			if (borderImage != null)
			{
				// 플래시 효과 (흰색 → 원래 색상)
				float t = flashTimer / flashDuration;
				Color targetColor = isOnCooldown ? unavailableBorderColor : availableBorderColor;
				borderImage.color = Color.Lerp(targetColor, flashColor, t);
			}
		}
	}
	
	#endregion
	
	#region 수동 갱신 (디버그용)
	
	[ContextMenu("UI 강제 갱신")]
	public void ForceRefresh()
	{
		RefreshFromSkillSlot();
	}
	
	#endregion
}

