using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace EDNE.UI
{
    /// <summary>
    /// 씬 이름을 Enum으로 관리
    /// 타입 안전성 + 유연성
    /// </summary>
    public enum GameScene
    {
        // 메인 씬들
        Title,
        Play,
        Level_01_Gameplay,
        
        // 개발/테스트 씬들
        Hyunmin,
        JIHUN,
        dohyeon,
        Enemy,
        door_scene,
        Test_pjw,
        
        // 추가 씬 (필요시)
        Level_02_Gameplay,
        Level_03_Gameplay,
        GameOver,
        
        // 새 씬 추가는 여기에!
    }

    /// <summary>
    /// 범용 씬 로더 컴포넌트
    /// - 어떤 버튼에도 붙일 수 있음
    /// - Inspector에서 타겟 씬 선택
    /// - Button.OnClick에 연결하여 사용
    /// 
    /// 사용법:
    /// 1. 버튼에 이 컴포넌트 추가
    /// 2. Inspector에서 Target Scene 선택
    /// 3. Button.OnClick → SceneLoader.LoadTargetScene
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SceneLoader : MonoBehaviour
    {
        [Header("Target Scene")]
        [Tooltip("이 버튼을 누르면 이동할 씬")]
        [SerializeField] private GameScene targetScene = GameScene.Level_01_Gameplay;

        [Header("Load Options")]
        [SerializeField] private bool useLoadingScreen = false;
        [SerializeField] private float fadeTime = 0.5f;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void Start()
        {
            // 자동으로 버튼에 연결
            button.onClick.AddListener(LoadTargetScene);
        }

        /// <summary>
        /// 설정된 씬으로 이동 (Button.OnClick에서 호출)
        /// </summary>
        public void LoadTargetScene()
        {
            LoadScene(targetScene);
        }

        /// <summary>
        /// 특정 씬으로 이동
        /// </summary>
        public void LoadScene(GameScene scene)
        {
            string sceneName = scene.ToString();

            // Build Settings에 있는지 확인
            if (!IsSceneInBuildSettings(sceneName))
            {
                Debug.LogError($"Scene '{sceneName}' is not in Build Settings!");
                return;
            }

            if (useLoadingScreen)
            {
                // TODO: 로딩 화면과 함께 로드
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
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
    }
}

