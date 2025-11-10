using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 배경음악 관리 - 씬마다 다른 음악 재생 가능
/// 같은 태그를 가진 BGM이 있으면 중복 방지
/// </summary>
public class DontDestroyAudio : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("이 BGM을 씬 전환 시에도 유지할지 여부")]
    public bool persistAcrossScenes = true;
    
    [Tooltip("BGM 식별용 태그 (같은 태그면 중복 제거)")]
    public string bgmTag = "BGM";
    
    private AudioSource audioSource;
    private static DontDestroyAudio currentBGM;
    
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        
        if (!persistAcrossScenes)
        {
            // 씬마다 다른 음악 - 유지 안함
            return;
        }
        
        // 싱글톤 패턴으로 중복 방지
        if (currentBGM == null)
        {
            // 첫 번째 BGM
            currentBGM = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log($"[BGM] 배경음악 시작: {audioSource?.clip?.name}");
        }
        else
        {
            // 이미 BGM이 재생 중
            AudioSource existingSource = currentBGM.GetComponent<AudioSource>();
            
            // 같은 음악이면 중복 제거
            if (existingSource != null && audioSource != null && 
                existingSource.clip == audioSource.clip)
            {
                Debug.Log($"[BGM] 같은 음악이 이미 재생 중 - 제거");
                Destroy(gameObject);
                return;
            }
            
            // 다른 음악이면 교체
            Debug.Log($"[BGM] 배경음악 교체: {existingSource?.clip?.name} → {audioSource?.clip?.name}");
            Destroy(currentBGM.gameObject);
            currentBGM = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    
    void OnDestroy()
    {
        if (currentBGM == this)
        {
            currentBGM = null;
        }
    }
}

