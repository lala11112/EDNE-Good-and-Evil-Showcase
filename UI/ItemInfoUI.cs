using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 아이템 정보 UI 컨트롤러
/// 아이템에 접근했을 때 아이템 정보를 표시합니다.
/// 싱글톤 패턴을 사용하여 씬에 하나만 존재합니다.
/// </summary>
public class ItemInfoUI : MonoBehaviour
{
    #region 싱글톤 패턴
    
    /// <summary>
    /// ItemInfoUI 인스턴스 (씬에 하나만 존재)
    /// </summary>
    public static ItemInfoUI Instance { get; private set; }
    
    #endregion
    
    [Header("UI 컴포넌트")]
    [Tooltip("아이템 이름 텍스트")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    
    [Tooltip("아이템 랭크 텍스트")]
    [SerializeField] private TextMeshProUGUI itemRankText;
    
    [Tooltip("아이템 설명 텍스트")]
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    
    [Tooltip("아이템 아이콘 이미지")]
    [SerializeField] private Image itemIconImage;
    
    [Tooltip("아이템 획득 안내 텍스트 (예: '아이템 획득 (E)')")]
    [SerializeField] private TextMeshProUGUI interactHintText;
    
    [Header("UI 설정")]
    [Tooltip("기본적으로 UI를 숨길지 여부")]
    [SerializeField] private bool hideOnStart = true;
    
    [Tooltip("획득 안내 텍스트 포맷 (예: '아이템 획득 ({0})')")]
    [SerializeField] private string interactHintFormat = "아이템 획득 ({0})";
    
    [Tooltip("상호작용 키 (기본값: E)")]
    [SerializeField] private string interactKey = "E";
    
    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("[ItemInfoUI] 씬에 ItemInfoUI가 여러 개 있습니다! 중복된 인스턴스를 제거합니다.");
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // 기본적으로 UI 숨김
        if (hideOnStart)
        {
            HideUI();
        }
    }
    
    /// <summary>
    /// 아이템 정보를 UI에 표시
    /// </summary>
    /// <param name="itemData">표시할 아이템 데이터</param>
    public void ShowItemInfo(ItemDataSO itemData)
    {
        if (itemData == null)
        {
            Debug.LogWarning("[ItemInfoUI] itemData가 null입니다!");
            return;
        }
        
        // 아이템 이름 설정
        if (itemNameText != null)
        {
            itemNameText.text = itemData.itemName;
        }
        
        // 아이템 랭크 설정
        if (itemRankText != null)
        {
            itemRankText.text = itemData.itemRank.ToString();
        }
        
        // 아이템 설명 설정
        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = itemData.itemDescription;
        }
        
        // 아이템 아이콘 설정
        if (itemIconImage != null)
        {
            if (itemData.itemIcon != null)
            {
                itemIconImage.sprite = itemData.itemIcon;
                itemIconImage.enabled = true;
            }
            else
            {
                itemIconImage.enabled = false;
            }
        }
        
        // 획득 안내 텍스트 설정
        if (interactHintText != null)
        {
            interactHintText.text = string.Format(interactHintFormat, interactKey);
        }
        
        // UI 표시
        ShowUI();
    }
    
    /// <summary>
    /// UI 표시
    /// </summary>
    public void ShowUI()
    {
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// UI 숨김
    /// </summary>
    public void HideUI()
    {
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 상호작용 키 설정
    /// </summary>
    /// <param name="key">상호작용 키 (예: "E", "F")</param>
    public void SetInteractKey(string key)
    {
        interactKey = key;
        
        // 획득 안내 텍스트 업데이트
        if (interactHintText != null && gameObject.activeSelf)
        {
            interactHintText.text = string.Format(interactHintFormat, interactKey);
        }
    }
}

