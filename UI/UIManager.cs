using UnityEngine;
using UnityEngine.SceneManagement;

namespace EDNE.UI
{
    /// <summary>
    /// 전체 UI를 관리하는 싱글톤 매니저
    /// - 씬 전환 (Enum 기반)
    /// - UI 상태 관리
    /// - 옵션 설정
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UIManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("UIManager");
                        _instance = go.AddComponent<UIManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #region Scene Management (Enum 기반)
        
        /// <summary>
        /// 씬 로드 (Enum 기반 - 타입 안전)
        /// </summary>
        public void LoadScene(GameScene scene)
        {
            string sceneName = scene.ToString();
            
            if (!IsSceneInBuildSettings(sceneName))
            {
                Debug.LogError($"[UIManager] Scene '{sceneName}' is not in Build Settings!");
                return;
            }
            
            Debug.Log($"[UIManager] Loading scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// 타이틀 화면으로 이동
        /// </summary>
        public void LoadTitleScene()
        {
            LoadScene(GameScene.Title);
        }

        /// <summary>
        /// 게임 시작 (메인 게임플레이 씬으로 이동)
        /// </summary>
        public void LoadGameplayScene()
        {
            LoadScene(GameScene.Level_01_Gameplay);
        }

        /// <summary>
        /// 현재 씬 재시작
        /// </summary>
        public void RestartCurrentScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// 게임 종료
        /// </summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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

        #region Game Settings

        /// <summary>
        /// 마스터 볼륨 설정
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            AudioListener.volume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("MasterVolume", volume);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 전체화면 토글
        /// </summary>
        public void ToggleFullScreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            PlayerPrefs.SetInt("FullScreen", Screen.fullScreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        #endregion

        #region Data Loading

        private void Start()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            // 저장된 설정 로드
            if (PlayerPrefs.HasKey("MasterVolume"))
            {
                AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume");
            }

            if (PlayerPrefs.HasKey("FullScreen"))
            {
                Screen.fullScreen = PlayerPrefs.GetInt("FullScreen") == 1;
            }
        }

        #endregion
    }
}

