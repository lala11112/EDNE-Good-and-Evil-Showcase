using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 보스 전용 룸 컨트롤러 (미니멀)
/// - 타일/룸 콜라이더의 트리거에 플레이어가 닿으면 문을 닫습니다.
/// - 웨이브/스폰 등 다른 로직은 포함하지 않습니다.
/// 사용법:
/// 1) 이 스크립트를 방(타일맵/영역) 오브젝트에 추가
/// 2) 해당 오브젝트에 Collider2D를 추가하고 isTrigger = true 설정
/// 3) 연결할 문들을 connectedDoors에 등록
/// </summary>
public class BossRoomController : MonoBehaviour
{
	[Header("룸 콜라이더 (트리거)")]
	[SerializeField] private Collider2D roomCollider;

	[Header("연결된 문들")]
	[SerializeField] private List<DoorController> connectedDoors = new List<DoorController>();

	[Header("옵션")]
	[SerializeField, Tooltip("문을 한 번만 닫습니다. 두 번째 진입 시는 무시")] private bool closeOnce = true;

	private bool isRoomActive = false;
	private bool hasClosedOnce = false;

	private void Awake()
	{
		if (roomCollider == null)
		{
			roomCollider = GetComponent<Collider2D>();
		}
		if (roomCollider == null)
		{
			Debug.LogWarning("[BossRoomController] 룸 콜라이더가 없습니다. Collider2D를 추가하고 isTrigger를 켜주세요.", this);
		}
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		// Player 태그 또는 PlayerController 확인
		if (!IsPlayer(other)) return;

		if (closeOnce && hasClosedOnce) return;
		if (isRoomActive) return;

		isRoomActive = true;
		hasClosedOnce = true;

		CloseDoors();
		Debug.Log("[BossRoomController] 플레이어 진입 감지: 문 닫힘");
	}

	private bool IsPlayer(Collider2D col)
	{
		if (col == null) return false;
		if (col.CompareTag("Player")) return true;
		return col.GetComponent<PlayerController>() != null;
	}

	private void CloseDoors()
	{
		for (int i = 0; i < connectedDoors.Count; i++)
		{
			var door = connectedDoors[i];
			if (door != null)
			{
				door.CloseDoor();
			}
		}
	}

	// 필요 시 외부에서 호출해 문을 열 수 있도록 공개 메서드 제공
	public void OpenDoors()
	{
		for (int i = 0; i < connectedDoors.Count; i++)
		{
			var door = connectedDoors[i];
			if (door != null)
			{
				door.OpenDoor();
			}
		}
		isRoomActive = false;
	}
}


