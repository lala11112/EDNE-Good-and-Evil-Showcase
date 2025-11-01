using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EDNE.UI
{
    /// <summary>
    /// 인게임 UI 컨트롤러
    /// - HP/MP 바 관리
    /// - 스킬 쿨다운 표시
    /// - 일시정지 메뉴
    /// </summary>
    public class GameUIController : MonoBehaviour
    {
        [Header("Player Stats UI")]
        [SerializeField] private Slider hpBar;
        [SerializeField] private Slider mpBar;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI mpText;

        [Header("Pause Menu")]
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button optionButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Option Panel")]
        [SerializeField] private GameObject optionPanel;
        [SerializeField] private OptionPanelController optionPanelController;

        private bool isPaused = false;

        private void Start()
        {
            InitializeButtons();
            InitializeOptionPanel();
            
            // 초기 상태 설정
            if (pauseMenuPanel != null) 
                pauseMenuPanel.SetActive(false);
            
            if (optionPanel != null) 
                optionPanel.SetActive(false);

            // 게임 시작 시 시간 정상화
            Time.timeScale = 1f;
        }

        private void InitializeOptionPanel()
        {
            // OptionPanelController의 닫기 이벤트 구독
            if (optionPanelController != null)
            {
                optionPanelController.OnPanelClosed.AddListener(CloseOptionPanel);
            }
        }

        private void Update()
        {
            // ESC 키로 일시정지 토글
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (optionPanel != null && optionPanel.activeSelf)
                {
                    CloseOptionPanel();
                }
                else
                {
                    TogglePause();
                }
            }
        }

        #region Button Initialization

        private void InitializeButtons()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeClicked);
            
            if (optionButton != null)
                optionButton.onClick.AddListener(OnOptionClicked);
            
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        #endregion

        #region Pause Menu

        public void TogglePause()
        {
            isPaused = !isPaused;

            if (isPaused)
            {
                Pause();
            }
            else
            {
                Resume();
            }
        }

        private void Pause()
        {
            Time.timeScale = 0f;
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(true);
        }

        private void Resume()
        {
            Time.timeScale = 1f;
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
            if (optionPanel != null)
                optionPanel.SetActive(false);
        }

        private void OnResumeClicked()
        {
            Resume();
            isPaused = false;
        }

        private void OnOptionClicked()
        {
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
            
            if (optionPanel != null)
                optionPanel.SetActive(true);
        }

        private void CloseOptionPanel()
        {
            if (optionPanel != null)
                optionPanel.SetActive(false);
            
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(true);
        }

        private void OnMainMenuClicked()
        {
            Time.timeScale = 1f;
            UIManager.Instance.LoadTitleScene();
        }

        #endregion

        #region Stats Display

        /// <summary>
        /// HP 바 업데이트
        /// </summary>
        public void UpdateHP(float current, float max)
        {
            if (hpBar != null)
            {
                hpBar.value = current / max;
            }

            if (hpText != null)
            {
                hpText.text = $"{current:F0} / {max:F0}";
            }
        }

        /// <summary>
        /// MP 바 업데이트
        /// </summary>
        public void UpdateMP(float current, float max)
        {
            if (mpBar != null)
            {
                mpBar.value = current / max;
            }

            if (mpText != null)
            {
                mpText.text = $"{current:F0} / {max:F0}";
            }
        }

        #endregion
    }
}


