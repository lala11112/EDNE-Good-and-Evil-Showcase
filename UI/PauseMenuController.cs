using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 일시정지 메뉴 컨트롤러
/// ESC 키로 일시정지 메뉴를 열고 닫습니다
/// </summary>
public class PauseMenuController : MonoBehaviour
{
	#region 싱글톤
	
	public static PauseMenuController Instance { get; private set; }
	
	#endregion
	
	#region UI 컴포넌트
	
	[Header("메뉴 UI")]
	[Tooltip("일시정지 메뉴 루트 오브젝트")]
	[SerializeField] private GameObject pauseMenuRoot;
	
	[Header("버튼")]
	[Tooltip("계속하기 버튼")]
	[SerializeField] private Button continueButton;
	
	[Tooltip("옵션 버튼")]
	[SerializeField] private Button optionButton;
	
	[Tooltip("타이틀로 가기 버튼")]
	[SerializeField] private Button titleButton;
	
	[Header("옵션 UI")]
	[Tooltip("옵션 UI 프리팹 또는 오브젝트")]
	[SerializeField] private GameObject optionPanel;
	
	[Header("튜토리얼")]
	[Tooltip("튜토리얼 이미지")]
	[SerializeField] private Image tutorialImage;
	
	[Tooltip("튜토리얼 이미지 스프라이트")]
	[SerializeField] private Sprite tutorialSprite;
	
	[Header("설정")]
	[Tooltip("타이틀 씬 이름")]
	[SerializeField] private string titleSceneName = "TitleScene";
	
	[Tooltip("일시정지 시 시간 정지 여부")]
	[SerializeField] private bool freezeTimeOnPause = true;
	
	#endregion
	
	#region 상태
	
	private bool isPaused = false;
	private bool isOptionOpen = false;
	
	#endregion
	
	#region Unity Lifecycle
	
	private void Awake()
	{
		// 싱글톤 설정
		if (Instance == null)
		{
			Instance = this;
			Debug.Log("[PauseMenuController] 싱글톤 인스턴스 생성 완료");
		}
		else
		{
			Debug.LogWarning("[PauseMenuController] 이미 인스턴스가 존재합니다. 이 오브젝트를 파괴합니다.");
			Destroy(gameObject);
			return;
		}
		
		// 초기화
		InitializeUI();
	}
	
	private void Start()
	{
		Debug.Log("[PauseMenuController] Start 호출됨");
		
		// 버튼 이벤트 연결
		SetupButtons();
		
		// 옵션 패널 이벤트 연결
		SetupOptionPanel();
		
		// 초기 상태: 메뉴 숨김
		ClosePauseMenu();
		
		Debug.Log($"[PauseMenuController] 초기화 완료 - pauseMenuRoot 할당 여부: {pauseMenuRoot != null}");
	}
	
	private void Update()
	{
		// ESC 키 입력 감지
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Debug.Log($"[PauseMenuController] ESC 키 감지! - isPaused: {isPaused}, isOptionOpen: {isOptionOpen}");
			
			if (isOptionOpen)
			{
				// 옵션이 열려있으면 옵션 닫기
				CloseOptionPanel();
			}
			else if (isPaused)
			{
				// 일시정지 중이면 계속하기
				ContinueGame();
			}
			else
			{
				// 일시정지 메뉴 열기
				OpenPauseMenu();
			}
		}
	}
	
	private void OnDestroy()
	{
		// 싱글톤 해제
		if (Instance == this)
		{
			Instance = null;
		}
		
		// 시간 정상화
		Time.timeScale = 1f;
	}
	
	#endregion
	
	#region 초기화
	
	/// <summary>
	/// UI 초기화
	/// </summary>
	private void InitializeUI()
	{
		Debug.Log("[PauseMenuController] InitializeUI 시작");
		
		if (pauseMenuRoot == null)
		{
			Debug.LogError("[PauseMenuController] ⚠️ pauseMenuRoot가 할당되지 않았습니다! Inspector에서 Pause Menu Root를 할당해주세요!");
			return;
		}
		
		Debug.Log($"[PauseMenuController] pauseMenuRoot 할당됨: {pauseMenuRoot.name}");
		
		// 튜토리얼 이미지 설정
		if (tutorialImage != null && tutorialSprite != null)
		{
			tutorialImage.sprite = tutorialSprite;
			Debug.Log("[PauseMenuController] 튜토리얼 이미지 설정 완료");
		}
		
		// 옵션 패널 초기 상태: 비활성화
		if (optionPanel != null)
		{
			optionPanel.SetActive(false);
			Debug.Log("[PauseMenuController] 옵션 패널 초기 비활성화 완료");
		}
	}
	
	/// <summary>
	/// 버튼 이벤트 설정
	/// </summary>
	private void SetupButtons()
	{
		if (continueButton != null)
		{
			continueButton.onClick.AddListener(ContinueGame);
		}
		else
		{
			Debug.LogWarning("[PauseMenuController] continueButton이 할당되지 않았습니다!");
		}
		
		if (optionButton != null)
		{
			optionButton.onClick.AddListener(OpenOptionPanel);
		}
		else
		{
			Debug.LogWarning("[PauseMenuController] optionButton이 할당되지 않았습니다!");
		}
		
		if (titleButton != null)
		{
			titleButton.onClick.AddListener(GoToTitle);
		}
		else
		{
			Debug.LogWarning("[PauseMenuController] titleButton이 할당되지 않았습니다!");
		}
	}
	
	/// <summary>
	/// 옵션 패널 이벤트 설정
	/// </summary>
	private void SetupOptionPanel()
	{
		if (optionPanel != null)
		{
			// OptionPanelController의 OnPanelClosed 이벤트에 연결
			var optionController = optionPanel.GetComponent<EDNE.UI.OptionPanelController>();
			if (optionController != null)
			{
				optionController.OnPanelClosed.AddListener(CloseOptionPanel);
				Debug.Log("[PauseMenuController] 옵션 패널 닫기 이벤트 연결 완료");
			}
		}
	}
	
	#endregion
	
	#region 일시정지 메뉴 제어
	
	/// <summary>
	/// 일시정지 메뉴 열기
	/// </summary>
	public void OpenPauseMenu()
	{
		Debug.Log("[PauseMenuController] OpenPauseMenu 호출됨");
		
		if (pauseMenuRoot == null)
		{
			Debug.LogError("[PauseMenuController] ⚠️ pauseMenuRoot가 null입니다! 메뉴를 열 수 없습니다.");
			return;
		}
		
		isPaused = true;
		pauseMenuRoot.SetActive(true);
		
		Debug.Log($"[PauseMenuController] pauseMenuRoot 활성화 완료: {pauseMenuRoot.activeSelf}");
		
		// 시간 정지
		if (freezeTimeOnPause)
		{
			Time.timeScale = 0f;
			Debug.Log("[PauseMenuController] 시간 정지 (Time.timeScale = 0)");
		}
		
		// 플레이어 입력 비활성화 (선택 사항)
		DisablePlayerInput();
		
		Debug.Log("[PauseMenuController] ✅ 일시정지 메뉴 열림!");
	}
	
	/// <summary>
	/// 일시정지 메뉴 닫기
	/// </summary>
	public void ClosePauseMenu()
	{
		Debug.Log("[PauseMenuController] ClosePauseMenu 호출됨");
		
		if (pauseMenuRoot == null)
		{
			Debug.LogWarning("[PauseMenuController] pauseMenuRoot가 null입니다.");
			return;
		}
		
		isPaused = false;
		pauseMenuRoot.SetActive(false);
		
		Debug.Log($"[PauseMenuController] pauseMenuRoot 비활성화 완료: {pauseMenuRoot.activeSelf}");
		
		// 시간 재개
		Time.timeScale = 1f;
		Debug.Log("[PauseMenuController] 시간 재개 (Time.timeScale = 1)");
		
		// 옵션 패널도 닫기
		if (optionPanel != null && isOptionOpen)
		{
			CloseOptionPanel();
		}
		
		// 플레이어 입력 활성화 (선택 사항)
		EnablePlayerInput();
		
		Debug.Log("[PauseMenuController] ✅ 일시정지 메뉴 닫힘!");
	}
	
	#endregion
	
	#region 버튼 이벤트
	
	/// <summary>
	/// 계속하기 버튼 (일시정지 해제)
	/// </summary>
	private void ContinueGame()
	{
		ClosePauseMenu();
		Debug.Log("[PauseMenuController] 게임 계속하기");
	}
	
	/// <summary>
	/// 옵션 버튼 (옵션 UI 열기)
	/// </summary>
	private void OpenOptionPanel()
	{
		if (optionPanel == null)
		{
			Debug.LogWarning("[PauseMenuController] optionPanel이 할당되지 않았습니다!");
			return;
		}
		
		isOptionOpen = true;
		optionPanel.SetActive(true);
		
		Debug.Log("[PauseMenuController] 옵션 패널 열림");
	}
	
	/// <summary>
	/// 옵션 패널 닫기
	/// </summary>
	public void CloseOptionPanel()
	{
		if (optionPanel == null) return;
		
		isOptionOpen = false;
		optionPanel.SetActive(false);
		
		Debug.Log("[PauseMenuController] 옵션 패널 닫힘");
	}
	
	/// <summary>
	/// 타이틀로 가기 버튼
	/// </summary>
	private void GoToTitle()
	{
		// 시간 정상화
		Time.timeScale = 1f;
		
		// 타이틀 씬으로 이동
		Debug.Log($"[PauseMenuController] 타이틀 씬으로 이동: {titleSceneName}");
		SceneManager.LoadScene(titleSceneName);
	}
	
	#endregion
	
	#region 플레이어 입력 제어
	
	/// <summary>
	/// 플레이어 입력 비활성화
	/// </summary>
	private void DisablePlayerInput()
	{
		if (PlayerController.Instance != null)
		{
			// PlayerController의 입력을 비활성화하는 메서드가 있다면 호출
			// PlayerController.Instance.DisableInput();
		}
	}
	
	/// <summary>
	/// 플레이어 입력 활성화
	/// </summary>
	private void EnablePlayerInput()
	{
		if (PlayerController.Instance != null)
		{
			// PlayerController의 입력을 활성화하는 메서드가 있다면 호출
			// PlayerController.Instance.EnableInput();
		}
	}
	
	#endregion
	
	#region 유틸리티
	
	/// <summary>
	/// 현재 일시정지 상태인지 확인
	/// </summary>
	public bool IsPaused()
	{
		return isPaused;
	}
	
	/// <summary>
	/// 튜토리얼 이미지 변경 (외부에서 호출 가능)
	/// </summary>
	public void SetTutorialSprite(Sprite sprite)
	{
		if (tutorialImage != null && sprite != null)
		{
			tutorialImage.sprite = sprite;
			Debug.Log($"[PauseMenuController] 튜토리얼 이미지 변경: {sprite.name}");
		}
	}
	
	#endregion
}

