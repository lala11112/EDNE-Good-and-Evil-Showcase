using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// Z 키로 다이얼로그를 진행하고, 다이얼로그 중에 게임을 일시정지하는 핸들러
/// Dialogue System for Unity와 함께 사용합니다.
/// </summary>
public class DialogueInputHandler : MonoBehaviour
{
    /// <summary>
    /// 다이얼로그 중에 게임 입력을 차단할지 여부를 나타내는 정적 플래그
    /// SkillController 등 다른 입력 시스템에서 이 값을 확인하여 입력을 차단합니다.
    /// </summary>
    public static bool IsDialogueActive { get; private set; } = false;

    /// <summary>
    /// 다이얼로그가 진행 중인지 확인하는 플래그
    /// </summary>
    private bool isDialogueActive = false;

    /// <summary>
    /// 다이얼로그 시작 전 저장된 게임 속도 (복원용)
    /// </summary>
    private float previousTimeScale = 1f;

    /// <summary>
    /// 다이얼로그 중 게임 속도를 0으로 설정할지 여부
    /// </summary>
    [SerializeField]
    [Tooltip("다이얼로그 중에 게임을 일시정지할지 선택 (마치 옵션 메뉴처럼)")]
    private bool pauseGameDuringDialogue = true;

    private void Start()
    {
        // DialogueManager의 대사 시작/종료 이벤트에 리스너 등록
        if (DialogueManager.instance != null)
        {
            DialogueManager.instance.conversationStarted += OnDialogueStart;
            DialogueManager.instance.conversationEnded += OnDialogueEnd;
        }
        else
        {
            Debug.LogWarning("DialogueManager를 찾을 수 없습니다! 씬에 DialogueManager가 있는지 확인해주세요.");
        }
    }

    private void OnDestroy()
    {
        // 리스너 해제 (중요: 메모리 누수 방지)
        if (DialogueManager.instance != null)
        {
            DialogueManager.instance.conversationStarted -= OnDialogueStart;
            DialogueManager.instance.conversationEnded -= OnDialogueEnd;
        }

        // 만약 다이얼로그 중 게임이 종료되었다면, 게임 속도 복원
        if (isDialogueActive && pauseGameDuringDialogue)
        {
            Time.timeScale = previousTimeScale;
        }
    }

    private void Update()
    {
        // 다이얼로그가 활성화되어 있을 때만 Z 키 입력 감지
        if (isDialogueActive && Input.GetKeyDown(KeyCode.Z))
        {
            // DialogueManager에 Continue 명령 전송
            DialogueManager.instance.SendMessage("OnContinueConversation");
        }
    }

    /// <summary>
    /// 대사가 시작될 때 호출되는 콜백
    /// 게임 속도를 0으로 설정하여 게임을 일시정지합니다.
    /// 또한 다른 입력 시스템(스킬, 이동 등)에서 참조할 정적 플래그를 설정합니다.
    /// </summary>
    /// <param name="actor">대사를 하는 캐릭터의 Transform</param>
    private void OnDialogueStart(Transform actor)
    {
        isDialogueActive = true;
        
        // 정적 플래그 업데이트 (다른 시스템에서 참조 가능)
        IsDialogueActive = true;

        // 게임 속도 조정이 활성화되어 있으면 게임 멈추기
        if (pauseGameDuringDialogue)
        {
            // 현재 게임 속도를 저장 (이미 멈춘 상태에서 다이얼로그가 시작될 수 있음)
            previousTimeScale = Time.timeScale;

            // 게임 속도를 0으로 설정하여 일시정지
            Time.timeScale = 0f;

            Debug.Log("다이얼로그 시작: 게임 일시정지 (Time.timeScale = 0) | 입력 차단 활성화");
        }
    }

    /// <summary>
    /// 대사가 종료될 때 호출되는 콜백
    /// 게임 속도를 이전 상태로 복원합니다.
    /// 또한 다른 입력 시스템에서 참조할 정적 플래그를 해제합니다.
    /// </summary>
    /// <param name="actor">대사를 하던 캐릭터의 Transform</param>
    private void OnDialogueEnd(Transform actor)
    {
        isDialogueActive = false;
        
        // 정적 플래그 업데이트 (다른 시스템 입력 복구)
        IsDialogueActive = false;

        // 게임 속도 조정이 활성화되어 있으면 게임 복구하기
        if (pauseGameDuringDialogue)
        {
            // 이전에 저장한 게임 속도로 복원
            Time.timeScale = previousTimeScale;

            Debug.Log($"다이얼로그 종료: 게임 재개 (Time.timeScale = {previousTimeScale}) | 입력 차단 해제");
        }
    }
}

