using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
        [Tooltip("Game Start 버튼을 누르면 이동할 씬 (드래그 앤 드롭)")]
#if UNITY_EDITOR
        [SerializeField] private SceneAsset targetSceneAsset;
#endif
        [SerializeField] private string targetSceneName = "Level_01_Gameplay_pjw";

        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject optionPanel;
        [SerializeField] private OptionPanelController optionPanelController;

        private void Start()
        {
            InitializeButtons();
            InitializeOptionPanel();
            
            // 처음에는 메인 패널만 활성화
            ShowMainPanel();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 값이 변경될 때 호출 (드래그 앤 드롭 시 자동으로 씬 이름 업데이트)
        /// </summary>
        private void OnValidate()
        {
            if (targetSceneAsset != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(targetSceneAsset);
                targetSceneName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            }
        }
#endif

        private void InitializeOptionPanel()
        {
            // OptionPanelController의 닫기 이벤트 구독
            if (optionPanelController != null)
            {
                optionPanelController.OnPanelClosed.AddListener(ShowMainPanel);
            }
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

        #endregion

        #region Button Events

        private void OnGameStartClicked()
        {
            Debug.Log($"게임 시작! 이동할 씬: {targetSceneName}");
            
            // 씬 이름이 비어있지 않으면 로드
            if (!string.IsNullOrEmpty(targetSceneName))
            {
                SceneManager.LoadScene(targetSceneName);
            }
            else
            {
                Debug.LogError("[TitleUI] 씬 이름이 설정되지 않았습니다!");
            }
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

        #region Panel Management

        private void ShowMainPanel()
        {
            Debug.Log("[TitleUI] ShowMainPanel 호출");
            
            // MainPanel은 항상 활성화 (배경으로 유지)
            if (mainPanel != null)
            {
                // MainPanel의 버튼만 활성화
                SetMainPanelInteractable(true);
                Debug.Log("[TitleUI] MainPanel 상호작용 활성화");
            }
            else
            {
                Debug.LogError("[TitleUI] MainPanel이 연결되지 않았습니다!");
            }
            
            if (optionPanel != null)
            {
                optionPanel.SetActive(false);
                Debug.Log("[TitleUI] OptionPanel 비활성화");
            }
            else
            {
                Debug.LogError("[TitleUI] OptionPanel이 연결되지 않았습니다!");
            }
        }

        private void ShowOptionPanel()
        {
            Debug.Log("[TitleUI] ShowOptionPanel 호출");
            
            // MainPanel을 끄지 않고 뒤에 유지 (상호작용만 비활성화)
            if (mainPanel != null)
            {
                // MainPanel의 버튼만 비활성화 (보이지만 클릭 안 됨)
                SetMainPanelInteractable(false);
                Debug.Log("[TitleUI] MainPanel 상호작용 비활성화");
            }
            else
            {
                Debug.LogWarning("[TitleUI] MainPanel이 연결되지 않았습니다!");
            }
            
            if (optionPanel != null)
            {
                optionPanel.SetActive(true);
                Debug.Log($"[TitleUI] OptionPanel 활성화 - Position: {optionPanel.transform.position}");
            }
            else
            {
                Debug.LogError("[TitleUI] OptionPanel이 연결되지 않았습니다!");
            }
        }

        /// <summary>
        /// MainPanel의 버튼들 상호작용 설정
        /// </summary>
        private void SetMainPanelInteractable(bool interactable)
        {
            if (gameStartButton != null)
                gameStartButton.interactable = interactable;
            
            if (optionButton != null)
                optionButton.interactable = interactable;
            
            if (quitGameButton != null)
                quitGameButton.interactable = interactable;
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
