using UnityEngine;

/// <summary>
/// 다이얼로그 종료 시 이 오브젝트를 삭제하는 간단한 스크립트
/// Dialogue System Trigger의 OnConversationEnd 이벤트에서 호출합니다.
/// </summary>
public class DestroyOnDialogueEnd : MonoBehaviour
{
	[Header("삭제 설정")]
	[Tooltip("삭제 전 대기 시간 (초)")]
	[SerializeField] private float delay = 0f;

	/// <summary>
	/// 이 메서드를 Dialogue System Trigger의 OnConversationEnd 이벤트에 연결하세요.
	/// </summary>
	public void DestroyThisObject()
	{
		Destroy(gameObject, delay);
		
#if UNITY_EDITOR
		Debug.Log($"[DestroyOnDialogueEnd] 오브젝트 삭제 예정: {gameObject.name} ({delay}초 후)");
#endif
	}
}

