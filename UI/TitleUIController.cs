using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EDNE.UI
{
    /// <summary>
    /// 타이틀 화면 전용 UI 컨트롤러
    /// - Unity 기본 컴포넌트 사용
    /// - 버튼 이벤트 처리
    /// - 패널 관리
    /// </summary>
    public class TitleUIController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button gameStartButton;
        [SerializeField] private Button optionButton;
        [SerializeField] private Button quitGameButton;

        [Header("Scene Settings")]
        [Tooltip("Game Start 버튼을 누르면 이동할 씬")]
        [SerializeField] private GameScene targetScene = GameScene.Level_01_Gameplay;

        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject optionPanel;

        [Header("Option Controls")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Toggle fullScreenToggle;

        private void Start()
        {
            InitializeButtons();
            InitializeOptionPanel();
            
            // 처음에는 메인 패널만 활성화
            ShowMainPanel();
        }

        #region Button Initialization

        private void InitializeButtons()
        {
            // Unity Button 이벤트 연결
            if (gameStartButton != null)
                gameStartButton.onClick.AddListener(OnGameStartClicked);
            
            if (optionButton != null)
                optionButton.onClick.AddListener(OnOptionClicked);
            
            if (quitGameButton != null)
                quitGameButton.onClick.AddListener(OnQuitGameClicked);
        }

        private void InitializeOptionPanel()
        {
            if (volumeSlider != null)
            {
                volumeSlider.value = AudioListener.volume;
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }

            if (fullScreenToggle != null)
            {
                fullScreenToggle.isOn = Screen.fullScreen;
                fullScreenToggle.onValueChanged.AddListener(OnFullScreenToggled);
            }
        }

        #endregion

        #region Button Events

        private void OnGameStartClicked()
        {
            Debug.Log($"게임 시작! 이동할 씬: {targetScene}");
            UIManager.Instance.LoadScene(targetScene);
        }

        private void OnOptionClicked()
        {
            Debug.Log("옵션 열기");
            ShowOptionPanel();
        }

        private void OnQuitGameClicked()
        {
            Debug.Log("게임 종료");
            UIManager.Instance.QuitGame();
        }

        #endregion

        #region Option Events

        private void OnVolumeChanged(float value)
        {
            UIManager.Instance.SetMasterVolume(value);
        }

        private void OnFullScreenToggled(bool isOn)
        {
            Screen.fullScreen = isOn;
            PlayerPrefs.SetInt("FullScreen", isOn ? 1 : 0);
            PlayerPrefs.Save();
        }

        #endregion

        #region Panel Management

        private void ShowMainPanel()
        {
            if (mainPanel != null) mainPanel.SetActive(true);
            if (optionPanel != null) optionPanel.SetActive(false);
        }

        private void ShowOptionPanel()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (optionPanel != null) optionPanel.SetActive(true);
        }

        #endregion

        #region Keyboard Input

        private void Update()
        {
            // ESC 키로 옵션 패널 닫기
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (optionPanel != null && optionPanel.activeSelf)
                {
                    ShowMainPanel();
                }
            }
        }

        #endregion
    }
}
