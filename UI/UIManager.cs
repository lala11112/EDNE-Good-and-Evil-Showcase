using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

namespace EDNE.UI
{
    /// <summary>
    /// UI 전역 관리자 (싱글톤)
    /// - 씬 로딩
    /// - 볼륨 설정
    /// - 게임 종료
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private static UIManager instance;

        public static UIManager Instance
        {
            get
            {
                if (instance == null)
                {
                    // 씬에서 UIManager 찾기
                    instance = FindObjectOfType<UIManager>();

                    // 없으면 새로 생성
                    if (instance == null)
                    {
                        GameObject go = new GameObject("UIManager");
                        instance = go.AddComponent<UIManager>();
                        DontDestroyOnLoad(go);
                        Debug.Log("[UIManager] 새로운 인스턴스 생성됨");
                    }
                }
                return instance;
            }
        }

        private void Awake()
        {
            // 싱글톤 중복 방지
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[UIManager] 중복된 인스턴스가 감지되어 제거됩니다.");
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[UIManager] 초기화 완료");
        }

        #region Scene Management

        /// <summary>
        /// 특정 씬으로 이동
        /// </summary>
        public void LoadScene(GameScene scene)
        {
            string sceneName = scene.ToString();
            Debug.Log($"[UIManager] 씬 로드 시작: {sceneName}");

            if (!IsSceneInBuildSettings(sceneName))
            {
                Debug.LogError($"[UIManager] Scene '{sceneName}' is not in Build Settings!");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// 타이틀 씬으로 이동
        /// </summary>
        public void LoadTitleScene()
        {
            LoadScene(GameScene.Title);
        }

        /// <summary>
        /// Build Settings에 씬이 있는지 확인
        /// </summary>
        private bool IsSceneInBuildSettings(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string name = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (name == sceneName)
                    return true;
            }
            return false;
        }

        #endregion

        #region Audio Settings

        [Header("Audio Mixer")]
        [Tooltip("AudioMixer를 연결하세요")]
        [SerializeField] private AudioMixer audioMixer;

        /// <summary>
        /// BGM 볼륨 설정 (0~1)
        /// </summary>
        public void SetBGMVolume(float value)
        {
            value = Mathf.Clamp01(value);
            
            if (audioMixer != null)
            {
                float volumeDB = value > 0 ? 20f * Mathf.Log10(value) : -80f;
                audioMixer.SetFloat("BGMVolume", volumeDB);
            }
            
            PlayerPrefs.SetFloat("BGMVolume", value);
            PlayerPrefs.Save();
            Debug.Log($"[UIManager] BGM 볼륨 설정: {value:F2}");
        }

        /// <summary>
        /// SFX 볼륨 설정 (0~1)
        /// </summary>
        public void SetSFXVolume(float value)
        {
            value = Mathf.Clamp01(value);
            
            if (audioMixer != null)
            {
                float volumeDB = value > 0 ? 20f * Mathf.Log10(value) : -80f;
                audioMixer.SetFloat("SFXVolume", volumeDB);
            }
            
            PlayerPrefs.SetFloat("SFXVolume", value);
            PlayerPrefs.Save();
            Debug.Log($"[UIManager] SFX 볼륨 설정: {value:F2}");
        }

        /// <summary>
        /// 저장된 볼륨 설정 불러오기
        /// </summary>
        public float GetBGMVolume()
        {
            return PlayerPrefs.GetFloat("BGMVolume", 1f);
        }

        public float GetSFXVolume()
        {
            return PlayerPrefs.GetFloat("SFXVolume", 1f);
        }

        #endregion

        #region Game Control

        /// <summary>
        /// 게임 종료
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[UIManager] 게임 종료");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Utility

        private void Start()
        {
            // 저장된 볼륨 설정 적용
            LoadSavedAudioSettings();
        }

        /// <summary>
        /// 저장된 오디오 설정 불러오기
        /// </summary>
        private void LoadSavedAudioSettings()
        {
            float bgmVolume = GetBGMVolume();
            float sfxVolume = GetSFXVolume();

            SetBGMVolume(bgmVolume);
            SetSFXVolume(sfxVolume);

            Debug.Log($"[UIManager] 저장된 볼륨 적용 - BGM: {bgmVolume:F2}, SFX: {sfxVolume:F2}");
        }

        #endregion
    }
}

