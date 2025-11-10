using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace EDNE.UI
{
    /// <summary>
    /// 옵션 패널 공통 컨트롤러
    /// TitleUIController와 GameUIController의 중복 코드를 통합
    /// - 볼륨 슬라이더 관리 (BGM, SFX)
    /// - 전체화면 토글 관리
    /// - 패널 닫기 버튼 관리
    /// </summary>
    public class OptionPanelController : MonoBehaviour
    {
        [Header("Volume Sliders")]
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Screen Settings")]
        [SerializeField] private Toggle fullScreenToggle;

        [Header("Close Button")]
        [SerializeField] private Button closeButton;

        [Header("Events")]
        public UnityEvent OnPanelClosed;

        private void Start()
        {
            InitializeControls();
        }

        /// <summary>
        /// 모든 컨트롤 초기화
        /// </summary>
        private void InitializeControls()
        {
            InitializeVolumeSliders();
            InitializeFullScreenToggle();
            InitializeCloseButton();
        }

        /// <summary>
        /// 볼륨 슬라이더 초기화
        /// </summary>
        private void InitializeVolumeSliders()
        {
            // BGM 볼륨
            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.value = UIManager.Instance.GetBGMVolume();
                bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
            }

            // SFX 볼륨
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = UIManager.Instance.GetSFXVolume();
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }
        }

        /// <summary>
        /// 전체화면 토글 초기화
        /// </summary>
        private void InitializeFullScreenToggle()
        {
            // 저장된 전체화면 설정 불러오기
            if (PlayerPrefs.HasKey("FullScreen"))
            {
                bool savedFullScreen = PlayerPrefs.GetInt("FullScreen") == 1;
                Screen.fullScreen = savedFullScreen;
            }

            if (fullScreenToggle != null)
            {
                fullScreenToggle.isOn = Screen.fullScreen;
                fullScreenToggle.onValueChanged.AddListener(OnFullScreenToggled);
            }
        }

        /// <summary>
        /// 닫기 버튼 초기화
        /// </summary>
        private void InitializeCloseButton()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
        }

        #region Event Handlers

        private void OnBGMVolumeChanged(float value)
        {
            UIManager.Instance.SetBGMVolume(value);
        }

        private void OnSFXVolumeChanged(float value)
        {
            UIManager.Instance.SetSFXVolume(value);
        }

        private void OnFullScreenToggled(bool isOn)
        {
            Debug.Log($"[OptionPanel] 전체화면 토글: {isOn} (현재: {Screen.fullScreen})");
            Screen.fullScreen = isOn;
            Debug.Log($"[OptionPanel] 전체화면 설정 후: {Screen.fullScreen}");
            
            PlayerPrefs.SetInt("FullScreen", isOn ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[OptionPanel] PlayerPrefs 저장 완료");
            
#if UNITY_EDITOR
            Debug.LogWarning("[OptionPanel] Unity Editor에서는 전체화면 전환이 제대로 작동하지 않습니다. 빌드(.exe)에서 테스트하세요!");
#endif
        }

        private void OnCloseButtonClicked()
        {
            Debug.Log("[OptionPanel] 닫기 버튼 클릭");
            
            // 이벤트 발생 (부모 컨트롤러가 처리)
            OnPanelClosed?.Invoke();
        }

        #endregion
    }
}

