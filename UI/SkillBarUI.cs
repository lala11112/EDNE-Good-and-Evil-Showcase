using UnityEngine;

/// <summary>
/// 스킬 바 UI 컨트롤러
/// 4개의 스킬 슬롯 UI를 관리합니다
/// </summary>
public class SkillBarUI : MonoBehaviour
{
	#region 스킬 슬롯 UI
	
	[Header("스킬 슬롯 UI (자동 탐색)")]
	[Tooltip("Z 슬롯 UI")]
	[SerializeField] private SkillSlotUI zSlotUI;
	
	[Tooltip("X 슬롯 UI")]
	[SerializeField] private SkillSlotUI xSlotUI;
	
	[Tooltip("C 슬롯 UI")]
	[SerializeField] private SkillSlotUI cSlotUI;
	
	[Tooltip("V 슬롯 UI")]
	[SerializeField] private SkillSlotUI vSlotUI;
	
	#endregion
	
	#region 레이아웃 설정
	
	[Header("레이아웃 (선택 사항)")]
	[Tooltip("스킬 슬롯 간 간격 (자동 배치 시)")]
	[SerializeField] private float slotSpacing = 10f;
	
	[Tooltip("스킬 슬롯 크기 (자동 배치 시)")]
	[SerializeField] private Vector2 slotSize = new Vector2(64f, 64f);
	
	#endregion
	
	#region Unity Lifecycle
	
	private void Awake()
	{
		// 자식 오브젝트에서 SkillSlotUI 자동 탐색
		if (zSlotUI == null || xSlotUI == null || cSlotUI == null || vSlotUI == null)
		{
			AutoFindSlotUIs();
		}
	}
	
	private void Start()
	{
		// 초기화
		ValidateSlotUIs();
	}
	
	#endregion
	
	#region 초기화
	
	/// <summary>
	/// 자식 오브젝트에서 스킬 슬롯 UI 자동 탐색
	/// </summary>
	private void AutoFindSlotUIs()
	{
		SkillSlotUI[] allSlotUIs = GetComponentsInChildren<SkillSlotUI>(true);
		
		foreach (var slotUI in allSlotUIs)
		{
			// SkillSlotUI의 SlotType 프로퍼티를 통해 자동 할당
			// 리플렉션을 사용하여 private 필드 접근
			var slotTypeField = slotUI.GetType().GetField("slotType", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			
			if (slotTypeField != null)
			{
				SkillSlotType slotType = (SkillSlotType)slotTypeField.GetValue(slotUI);
				
				switch (slotType)
				{
					case SkillSlotType.Z:
						if (zSlotUI == null) zSlotUI = slotUI;
						break;
					case SkillSlotType.X:
						if (xSlotUI == null) xSlotUI = slotUI;
						break;
					case SkillSlotType.C:
						if (cSlotUI == null) cSlotUI = slotUI;
						break;
					case SkillSlotType.V:
						if (vSlotUI == null) vSlotUI = slotUI;
						break;
				}
			}
		}
	}
	
	/// <summary>
	/// 스킬 슬롯 UI 유효성 검사
	/// </summary>
	private void ValidateSlotUIs()
	{
		if (zSlotUI == null)
			Debug.LogWarning("[SkillBarUI] Z 슬롯 UI가 할당되지 않았습니다!");
		
		if (xSlotUI == null)
			Debug.LogWarning("[SkillBarUI] X 슬롯 UI가 할당되지 않았습니다!");
		
		if (cSlotUI == null)
			Debug.LogWarning("[SkillBarUI] C 슬롯 UI가 할당되지 않았습니다!");
		
		if (vSlotUI == null)
			Debug.LogWarning("[SkillBarUI] V 슬롯 UI가 할당되지 않았습니다!");
	}
	
	#endregion
	
	#region 공개 API
	
	/// <summary>
	/// 특정 슬롯 UI 가져오기
	/// </summary>
	public SkillSlotUI GetSlotUI(SkillSlotType slotType)
	{
		switch (slotType)
		{
			case SkillSlotType.Z: return zSlotUI;
			case SkillSlotType.X: return xSlotUI;
			case SkillSlotType.C: return cSlotUI;
			case SkillSlotType.V: return vSlotUI;
			default: return null;
		}
	}
	
	/// <summary>
	/// 모든 슬롯 UI 새로고침
	/// </summary>
	[ContextMenu("모든 슬롯 UI 새로고침")]
	public void RefreshAllSlots()
	{
		zSlotUI?.ForceRefresh();
		xSlotUI?.ForceRefresh();
		cSlotUI?.ForceRefresh();
		vSlotUI?.ForceRefresh();
	}
	
	/// <summary>
	/// 스킬 바 표시/숨김
	/// </summary>
	public void SetVisible(bool visible)
	{
		gameObject.SetActive(visible);
	}
	
	#endregion
	
	#region 레이아웃 자동 배치 (에디터 전용)
	
	#if UNITY_EDITOR
	/// <summary>
	/// 스킬 슬롯 UI 자동 배치 (에디터 전용)
	/// </summary>
	[ContextMenu("슬롯 UI 자동 배치 (가로)")]
	private void AutoLayoutHorizontal()
	{
		SkillSlotUI[] slots = new SkillSlotUI[] { zSlotUI, xSlotUI, cSlotUI, vSlotUI };
		
		for (int i = 0; i < slots.Length; i++)
		{
			if (slots[i] == null) continue;
			
			RectTransform rectTransform = slots[i].GetComponent<RectTransform>();
			if (rectTransform == null) continue;
			
			// 위치 설정
			float xPos = i * (slotSize.x + slotSpacing);
			rectTransform.anchoredPosition = new Vector2(xPos, 0);
			
			// 크기 설정
			rectTransform.sizeDelta = slotSize;
		}
		
		Debug.Log("[SkillBarUI] 슬롯 UI를 가로로 자동 배치했습니다.");
	}
	#endif
	
	#endregion
}

