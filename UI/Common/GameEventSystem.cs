using UnityEngine;
using System;

namespace EDNE.UI
{
    /// <summary>
    /// 게임 이벤트 시스템 (싱글톤)
    /// 옵저버 패턴을 사용하여 게임 전역 이벤트를 관리합니다.
    /// 플레이어 사망, 게임 오버 등의 이벤트를 발행하고 구독할 수 있습니다.
    /// </summary>
    public class GameEventSystem : MonoBehaviour
    {
        #region Singleton
        
        private static GameEventSystem instance;
        
        /// <summary>
        /// GameEventSystem 싱글톤 인스턴스
        /// </summary>
        public static GameEventSystem Instance
        {
            get
            {
                if (instance == null)
                {
                    // 씬에서 GameEventSystem 찾기
                    instance = FindObjectOfType<GameEventSystem>();
                    
                    // 없으면 새로 생성
                    if (instance == null)
                    {
                        GameObject go = new GameObject("GameEventSystem");
                        instance = go.AddComponent<GameEventSystem>();
                        DontDestroyOnLoad(go);
                        Debug.Log("[GameEventSystem] 새로운 인스턴스 생성됨");
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
                Debug.LogWarning("[GameEventSystem] 중복된 인스턴스가 감지되어 제거됩니다.");
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[GameEventSystem] 초기화 완료");
        }
        
        #endregion
        
        #region Player Events
        
        /// <summary>
        /// 플레이어 사망 이벤트
        /// 플레이어가 사망했을 때 발생합니다.
        /// </summary>
        public event Action OnPlayerDeath;
        
        /// <summary>
        /// 플레이어 부활 이벤트
        /// 플레이어가 부활했을 때 발생합니다.
        /// </summary>
        public event Action OnPlayerRespawn;
        
        /// <summary>
        /// 플레이어 사망 이벤트 발생
        /// </summary>
        public void TriggerPlayerDeath()
        {
            Debug.Log("[GameEventSystem] 플레이어 사망 이벤트 발생");
            OnPlayerDeath?.Invoke();
        }
        
        /// <summary>
        /// 플레이어 부활 이벤트 발생
        /// </summary>
        public void TriggerPlayerRespawn()
        {
            Debug.Log("[GameEventSystem] 플레이어 부활 이벤트 발생");
            OnPlayerRespawn?.Invoke();
        }
        
        #endregion
        
        #region Game State Events
        
        /// <summary>
        /// 게임 오버 이벤트
        /// 게임이 종료되었을 때 발생합니다.
        /// </summary>
        public event Action OnGameOver;
        
        /// <summary>
        /// 게임 재시작 이벤트
        /// 게임을 재시작할 때 발생합니다.
        /// </summary>
        public event Action OnGameRestart;
        
        /// <summary>
        /// 게임 일시정지 이벤트
        /// </summary>
        public event Action<bool> OnGamePause; // bool: isPaused
        
        /// <summary>
        /// 게임 오버 이벤트 발생
        /// </summary>
        public void TriggerGameOver()
        {
            Debug.Log("[GameEventSystem] 게임 오버 이벤트 발생");
            OnGameOver?.Invoke();
        }
        
        /// <summary>
        /// 게임 재시작 이벤트 발생
        /// </summary>
        public void TriggerGameRestart()
        {
            Debug.Log("[GameEventSystem] 게임 재시작 이벤트 발생");
            OnGameRestart?.Invoke();
        }
        
        /// <summary>
        /// 게임 일시정지 이벤트 발생
        /// </summary>
        public void TriggerGamePause(bool isPaused)
        {
            Debug.Log($"[GameEventSystem] 게임 일시정지 이벤트 발생: {isPaused}");
            OnGamePause?.Invoke(isPaused);
        }
        
        #endregion
        
        #region Level Events
        
        /// <summary>
        /// 레벨 클리어 이벤트
        /// 레벨을 클리어했을 때 발생합니다.
        /// </summary>
        public event Action<int> OnLevelComplete; // int: level number
        
        /// <summary>
        /// 레벨 시작 이벤트
        /// 새로운 레벨이 시작될 때 발생합니다.
        /// </summary>
        public event Action<int> OnLevelStart; // int: level number
        
        /// <summary>
        /// 레벨 클리어 이벤트 발생
        /// </summary>
        public void TriggerLevelComplete(int levelNumber)
        {
            Debug.Log($"[GameEventSystem] 레벨 {levelNumber} 클리어 이벤트 발생");
            OnLevelComplete?.Invoke(levelNumber);
        }
        
        /// <summary>
        /// 레벨 시작 이벤트 발생
        /// </summary>
        public void TriggerLevelStart(int levelNumber)
        {
            Debug.Log($"[GameEventSystem] 레벨 {levelNumber} 시작 이벤트 발생");
            OnLevelStart?.Invoke(levelNumber);
        }
        
        #endregion
        
        #region Boss Events
        
        /// <summary>
        /// 보스 등장 이벤트
        /// </summary>
        public event Action OnBossAppear;
        
        /// <summary>
        /// 보스 처치 이벤트
        /// </summary>
        public event Action OnBossDefeated;
        
        /// <summary>
        /// 보스 등장 이벤트 발생
        /// </summary>
        public void TriggerBossAppear()
        {
            Debug.Log("[GameEventSystem] 보스 등장 이벤트 발생");
            OnBossAppear?.Invoke();
        }
        
        /// <summary>
        /// 보스 처치 이벤트 발생
        /// </summary>
        public void TriggerBossDefeated()
        {
            Debug.Log("[GameEventSystem] 보스 처치 이벤트 발생");
            OnBossDefeated?.Invoke();
        }
        
        #endregion
        
        #region Utility
        
        /// <summary>
        /// 모든 이벤트 구독 해제
        /// 주의: 씬 전환 시 호출하면 이벤트 리스너가 모두 초기화됩니다.
        /// </summary>
        public void ClearAllEvents()
        {
            OnPlayerDeath = null;
            OnPlayerRespawn = null;
            OnGameOver = null;
            OnGameRestart = null;
            OnGamePause = null;
            OnLevelComplete = null;
            OnLevelStart = null;
            OnBossAppear = null;
            OnBossDefeated = null;
            
            Debug.Log("[GameEventSystem] 모든 이벤트 구독 해제 완료");
        }
        
        private void OnDestroy()
        {
            // 씬 전환 시 이벤트 정리
            if (instance == this)
            {
                ClearAllEvents();
            }
        }
        
        #endregion
    }
}

