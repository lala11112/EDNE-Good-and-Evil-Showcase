using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace EDNE.UI
{
    /// <summary>
    /// 게임 오버 UI 컨트롤러
    /// 플레이어 사망 시 게임 오버 화면을 표시하고 관리합니다.
    /// - 계속하기 (부활)
    /// - 타이틀 화면으로
    /// 옵저버 패턴을 사용하여 GameEventSystem의 이벤트를 구독합니다.
    /// </summary>
    public class GameOverUIController : MonoBehaviour
    {
        [Header("UI 패널")]
        [Tooltip("게임 오버 UI 패널")]
        [SerializeField] private GameObject gameOverPanel;
        
        [Header("텍스트")]
        [Tooltip("게임 오버 타이틀 텍스트")]
        [SerializeField] private TextMeshProUGUI titleText;
        
        [Tooltip("게임 오버 메시지 텍스트")]
        [SerializeField] private TextMeshProUGUI messageText;
        
        [Header("버튼")]
        [Tooltip("계속하기 버튼 (부활)")]
        [SerializeField] private Button continueButton;
        
        [Tooltip("타이틀 화면으로 버튼")]
        [SerializeField] private Button mainMenuButton;
        
        [Header("애니메이션 설정")]
        [Tooltip("페이드 인 애니메이션 시간")]
        [SerializeField] private float fadeInDuration = 1f;
        
        [Tooltip("게임 오버 패널 표시 지연 시간")]
        [SerializeField] private float showDelay = 1f;
        
        [Header("효과음")]
        [Tooltip("게임 오버 사운드")]
        [SerializeField] private AudioClip gameOverSound;
        
        [Tooltip("버튼 클릭 사운드")]
        [SerializeField] private AudioClip buttonClickSound;
        
        // 컴포넌트
        private CanvasGroup canvasGroup;
        private AudioSource audioSource;
        
        // 상태
        private bool isGameOver = false;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
            InitializeButtons();
        }

        private void Start()
        {
            // 게임 시작 시 게임 오버 패널 숨김
            HideGameOverPanel();
        }

        private void OnEnable()
        {
            // 플레이어 이벤트 구독
            SubscribeToPlayerEvents();
        }

        private void OnDisable()
        {
            // 플레이어 이벤트 구독 해제
            UnsubscribeFromPlayerEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 컴포넌트 초기화
        /// </summary>
        private void InitializeComponents()
        {
            // CanvasGroup 자동 할당 또는 추가
            if (gameOverPanel != null)
            {
                canvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameOverPanel.AddComponent<CanvasGroup>();
                }
            }
            
            // AudioSource 자동 할당 또는 추가
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
            
            Debug.Log("[GameOverUIController] 컴포넌트 초기화 완료");
        }

        /// <summary>
        /// 버튼 이벤트 초기화
        /// </summary>
        private void InitializeButtons()
        {
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }
            else
            {
                Debug.LogWarning("[GameOverUIController] 계속하기 버튼이 연결되지 않았습니다!");
            }
            
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
            else
            {
                Debug.LogWarning("[GameOverUIController] 타이틀 버튼이 연결되지 않았습니다!");
            }
        }

        #endregion

        #region Event System (Observer Pattern)

        /// <summary>
        /// 플레이어 이벤트 구독
        /// PlayerController의 static 이벤트를 구독합니다.
        /// </summary>
        private void SubscribeToPlayerEvents()
        {
            PlayerController.OnPlayerDied += OnPlayerDeath;
            PlayerController.OnPlayerRevived += OnPlayerRevived;
            Debug.Log("[GameOverUIController] 플레이어 이벤트 구독 완료");
        }

        /// <summary>
        /// 플레이어 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeFromPlayerEvents()
        {
            PlayerController.OnPlayerDied -= OnPlayerDeath;
            PlayerController.OnPlayerRevived -= OnPlayerRevived;
            Debug.Log("[GameOverUIController] 플레이어 이벤트 구독 해제 완료");
        }

        /// <summary>
        /// 플레이어 사망 이벤트 핸들러
        /// </summary>
        private void OnPlayerDeath()
        {
            Debug.Log("[GameOverUIController] 플레이어 사망 감지 - 게임 오버 UI 표시");
            ShowGameOverPanel();
        }

        /// <summary>
        /// 플레이어 부활 이벤트 핸들러
        /// </summary>
        private void OnPlayerRevived()
        {
            Debug.Log("[GameOverUIController] 플레이어 부활 감지 - 게임 오버 UI 숨김");
            HideGameOverPanel();
        }

        #endregion

        #region Game Over UI Control

        /// <summary>
        /// 게임 오버 패널 표시
        /// </summary>
        private void ShowGameOverPanel()
        {
            if (isGameOver) return;
            
            isGameOver = true;
            StartCoroutine(ShowGameOverCoroutine());
        }

        /// <summary>
        /// 게임 오버 패널 표시 코루틴
        /// </summary>
        private IEnumerator ShowGameOverCoroutine()
        {
            // 지연 시간 대기 (플레이어 사망 애니메이션 재생 시간)
            yield return new WaitForSeconds(showDelay);
            
            // 게임 오버 사운드 재생
            PlaySound(gameOverSound);
            
            // 게임 일시정지
            Time.timeScale = 0f;
            
            // 패널 활성화
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
            
            // 페이드 인 애니메이션
            yield return StartCoroutine(FadeIn());
            
            Debug.Log("[GameOverUIController] 게임 오버 UI 표시 완료");
        }

        /// <summary>
        /// 게임 오버 패널 숨김
        /// </summary>
        private void HideGameOverPanel()
        {
            isGameOver = false;
            
            // 게임 재개
            Time.timeScale = 1f;
            
            // 패널 비활성화
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
            
            // 캔버스 그룹 알파값 초기화
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
            
            Debug.Log("[GameOverUIController] 게임 오버 UI 숨김 완료");
        }

        /// <summary>
        /// 페이드 인 애니메이션
        /// </summary>
        private IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;
            
            float elapsedTime = 0f;
            canvasGroup.alpha = 0f;
            
            while (elapsedTime < fadeInDuration)
            {
                // Time.timeScale이 0일 때도 작동하도록 unscaledDeltaTime 사용
                elapsedTime += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
        }

        #endregion

        #region Button Events

        /// <summary>
        /// 계속하기 버튼 클릭 (부활)
        /// </summary>
        private void OnContinueClicked()
        {
            Debug.Log("[GameOverUIController] 계속하기 버튼 클릭");
            
            PlaySound(buttonClickSound);
            
            // 플레이어 부활 처리
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.Revive();
            }
            else
            {
                Debug.LogError("[GameOverUIController] PlayerController.Instance를 찾을 수 없습니다!");
                HideGameOverPanel(); // 에러 상황에서도 UI는 숨김
            }
        }

        /// <summary>
        /// 타이틀 화면으로 버튼 클릭
        /// </summary>
        private void OnMainMenuClicked()
        {
            Debug.Log("[GameOverUIController] 타이틀 화면으로 버튼 클릭");
            
            PlaySound(buttonClickSound);
            
            // 게임 재개 (씬 전환 전에 timeScale 복구)
            Time.timeScale = 1f;
            
            // 타이틀 씬으로 이동
            UIManager.Instance?.LoadTitleScene();
        }

        #endregion

        #region Audio

        /// <summary>
        /// 사운드 재생
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 외부에서 게임 오버 UI 수동 표시
        /// (이벤트 없이 직접 호출할 경우 사용)
        /// </summary>
        public void ShowGameOver()
        {
            ShowGameOverPanel();
        }

        /// <summary>
        /// 외부에서 게임 오버 UI 수동 숨김
        /// </summary>
        public void HideGameOver()
        {
            HideGameOverPanel();
        }

        /// <summary>
        /// 게임 오버 메시지 변경
        /// </summary>
        public void SetGameOverMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
        }

        /// <summary>
        /// 게임 오버 타이틀 변경
        /// </summary>
        public void SetGameOverTitle(string title)
        {
            if (titleText != null)
            {
                titleText.text = title;
            }
        }

        #endregion
    }
}

