using UnityEngine;

/// <summary>
/// FanAreaVisualizer 디버그 헬퍼
/// Scene 뷰에서 부채꼴 방향을 명확하게 표시합니다
/// </summary>
public class FanAreaVisualizerDebug : MonoBehaviour
{
	[Header("디버그 설정")]
	[SerializeField] private bool showDebugGizmos = true;
	[SerializeField] private float arrowLength = 1f;
	
	private void OnDrawGizmos()
	{
		if (!showDebugGizmos) return;
		
		// 중심점
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(transform.position, 0.1f);
		
		// 전방 방향 표시 (Y+ 방향 = 로컬 위쪽)
		Vector3 forward = transform.up; // 2D에서는 up이 forward
		Gizmos.color = Color.green;
		Gizmos.DrawLine(transform.position, transform.position + forward * arrowLength);
		
		// 화살표 끝에 텍스트 표시
		#if UNITY_EDITOR
		UnityEditor.Handles.Label(transform.position + forward * arrowLength, "전방 방향");
		#endif
		
		// 오른쪽 방향 표시 (X+ 방향)
		Vector3 right = transform.right;
		Gizmos.color = Color.red;
		Gizmos.DrawLine(transform.position, transform.position + right * arrowLength * 0.5f);
		
		// 회전 각도 표시
		Gizmos.color = Color.yellow;
		float angle = transform.rotation.eulerAngles.z;
		#if UNITY_EDITOR
		UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"회전: {angle:F1}도");
		#endif
	}
}

