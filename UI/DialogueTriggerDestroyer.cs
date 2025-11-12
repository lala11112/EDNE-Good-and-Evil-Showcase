using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// 다이얼로그가 종료되면 이 오브젝트를 자동으로 삭제하는 스크립트
/// Dialogue System Trigger와 함께 사용하여 일회성 대화를 구현합니다.
/// </summary>
public class DialogueTriggerDestroyer : MonoBehaviour
{
	[Header("삭제 설정")]
	[Tooltip("다이얼로그 종료 후 오브젝트를 삭제할 때까지 대기 시간 (초)")]
	[SerializeField] private float destroyDelay = 0.1f;
	
	[Tooltip("다이얼로그 시작 시 콜라이더를 비활성화하여 중복 트리거 방지")]
	[SerializeField] private bool disableColliderOnStart = true;
	
	/// <summary>
	/// 다이얼로그가 이미 시작되었는지 추적 (중복 방지)
	/// </summary>
	private bool hasDialogueStarted = false;
	
	/// <summary>
	/// 이 오브젝트의 대화 제목 (DialogueSystemTrigger에서 가져옴)
	/// </summary>
	private string myConversationTitle = "";
	
	/// <summary>
	/// DialogueSystemTrigger 컴포넌트 캐싱
	/// </summary>
	private DialogueSystemTrigger dialogueTrigger;

	private void Start()
	{
		// DialogueSystemTrigger 컴포넌트 가져오기
		dialogueTrigger = GetComponent<DialogueSystemTrigger>();
		if (dialogueTrigger != null)
		{
			// 이 오브젝트의 대화 제목 저장
			myConversationTitle = dialogueTrigger.conversation;
			
#if UNITY_EDITOR
			Debug.Log($"[DialogueTriggerDestroyer] 초기화 완료: {gameObject.name}, 대화 제목: '{myConversationTitle}'");
#endif
		}
		else
		{
			Debug.LogWarning($"[DialogueTriggerDestroyer] DialogueSystemTrigger를 찾을 수 없습니다! ({gameObject.name})");
		}
		
		// Dialogue System 이벤트 구독
		if (DialogueManager.instance != null)
		{
			DialogueManager.instance.conversationStarted += OnDialogueStart;
			DialogueManager.instance.conversationEnded += OnDialogueEnd;
		}
		else
		{
			Debug.LogWarning($"[DialogueTriggerDestroyer] DialogueManager를 찾을 수 없습니다! ({gameObject.name})");
		}
	}

	private void OnDestroy()
	{
		// 이벤트 구독 해제 (메모리 누수 방지)
		if (DialogueManager.instance != null)
		{
			DialogueManager.instance.conversationStarted -= OnDialogueStart;
			DialogueManager.instance.conversationEnded -= OnDialogueEnd;
		}
	}

	/// <summary>
	/// 다이얼로그 시작 시 호출
	/// 콜라이더를 비활성화하여 중복 트리거 방지
	/// </summary>
	private void OnDialogueStart(Transform actor)
	{
		// 현재 시작된 대화가 이 오브젝트의 대화인지 확인
		string startedConversation = DialogueManager.lastConversationStarted;
		
		if (!hasDialogueStarted && !string.IsNullOrEmpty(myConversationTitle) && startedConversation == myConversationTitle)
		{
			hasDialogueStarted = true;
			
#if UNITY_EDITOR
			Debug.Log($"[DialogueTriggerDestroyer] 내 대화 시작됨: {gameObject.name}, 대화: '{startedConversation}'");
#endif
			
			// 콜라이더 비활성화 (중복 트리거 방지)
			if (disableColliderOnStart)
			{
				Collider2D col = GetComponent<Collider2D>();
				if (col != null)
				{
					col.enabled = false;
#if UNITY_EDITOR
					Debug.Log($"[DialogueTriggerDestroyer] 콜라이더 비활성화: {gameObject.name}");
#endif
				}
			}
		}
	}

	/// <summary>
	/// 다이얼로그 종료 시 호출
	/// 오브젝트를 삭제합니다.
	/// </summary>
	private void OnDialogueEnd(Transform actor)
	{
		// 현재 종료된 대화가 이 오브젝트의 대화인지 확인
		string endedConversation = DialogueManager.lastConversationEnded;
		
		if (hasDialogueStarted && !string.IsNullOrEmpty(myConversationTitle) && endedConversation == myConversationTitle)
		{
			// 오브젝트 삭제 (딜레이 후)
			Destroy(gameObject, destroyDelay);
			
#if UNITY_EDITOR
			Debug.Log($"[DialogueTriggerDestroyer] 내 대화 종료됨! 오브젝트 삭제 예정: {gameObject.name}, 대화: '{endedConversation}' ({destroyDelay}초 후)");
#endif
		}
	}

}

