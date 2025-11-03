/*
 * ========================================
 * TrapBase.cs
 * ========================================
 * [설명]
 * 모든 트랩의 기본 클래스입니다.
 * RoomController에서 트랩을 일괄 관리하기 위한 인터페이스를 제공합니다.
 * 
 * [주요 기능]
 * - 트랩 시작/정지 인터페이스
 * - 트랩 활성화/비활성화 상태 관리
 * - RoomController 자동 연결
 * 
 * [사용 방법]
 * 1. 이 클래스를 상속받아 구체적인 트랩 클래스 생성
 * 2. StartTrap(), StopTrap() 메서드를 구현
 * 3. RoomController의 자식 오브젝트로 배치하거나 수동으로 연결
 * ========================================
 */

using UnityEngine;

/// <summary>
/// 모든 트랩의 기본 클래스
/// RoomController에서 일괄 관리하기 위한 공통 인터페이스 제공
/// </summary>
public abstract class TrapBase : MonoBehaviour
{
	#region 트랩 기본 설정

	[Header("트랩 기본 설정")]
	[Tooltip("트랩 이름")]
	[SerializeField] protected string trapName = "Trap";

	[Tooltip("트랩 활성화 여부")]
	[SerializeField] protected bool isActive = true;

	#endregion

	#region RoomController 연결

	[Header("RoomController 연결")]
	[Tooltip("이 트랩이 속한 RoomController (자동 찾기 가능)")]
	[SerializeField] protected RoomController connectedRoomController;

	#endregion

	#region 트랩 상태

	/// <summary>
	/// 트랩이 실행 중인지 여부
	/// </summary>
	[SerializeField] protected bool isRunning = false;

	/// <summary>
	/// 트랩 활성화 여부
	/// </summary>
	public bool IsActive => isActive;

	/// <summary>
	/// 트랩 실행 중 여부
	/// </summary>
	public bool IsRunning => isRunning;

	#endregion

	#region Unity Lifecycle

	/// <summary>
	/// 트랩 초기화
	/// </summary>
	protected virtual void Awake()
	{
		// RoomController 자동 찾기 (자식 오브젝트에서)
		if (connectedRoomController == null)
		{
			connectedRoomController = GetComponentInParent<RoomController>();
			if (connectedRoomController != null)
			{
				Debug.Log($"[TrapBase] {trapName}에서 RoomController 자동 찾기 성공: {connectedRoomController.name}");
			}
		}
	}

	#endregion

	#region 트랩 제어 (추상 메서드)

	/// <summary>
	/// 트랩 시작
	/// RoomController에서 방 진입 시 호출됩니다.
	/// </summary>
	public virtual void StartTrap()
	{
		if (!isActive)
		{
			Debug.LogWarning($"[TrapBase] {trapName}이 비활성화되어 있어 시작할 수 없습니다.");
			return;
		}

		if (isRunning)
		{
			Debug.LogWarning($"[TrapBase] {trapName}이 이미 실행 중입니다.");
			return;
		}

		isRunning = true;
		OnTrapStarted();
		Debug.Log($"[TrapBase] {trapName} 시작");
	}

	/// <summary>
	/// 트랩 정지
	/// RoomController에서 방 클리어 시 호출됩니다.
	/// </summary>
	public virtual void StopTrap()
	{
		if (!isRunning)
		{
			Debug.LogWarning($"[TrapBase] {trapName}이 실행 중이 아닙니다.");
			return;
		}

		isRunning = false;
		OnTrapStopped();
		Debug.Log($"[TrapBase] {trapName} 정지");
	}

	/// <summary>
	/// 트랩 시작 시 호출 (오버라이드용)
	/// 자식 클래스에서 트랩별 시작 로직을 구현합니다.
	/// </summary>
	protected abstract void OnTrapStarted();

	/// <summary>
	/// 트랩 정지 시 호출 (오버라이드용)
	/// 자식 클래스에서 트랩별 정지 로직을 구현합니다.
	/// </summary>
	protected abstract void OnTrapStopped();

	#endregion

	#region 트랩 활성화/비활성화

	/// <summary>
	/// 트랩 활성화
	/// </summary>
	public virtual void ActivateTrap()
	{
		isActive = true;
		Debug.Log($"[TrapBase] {trapName} 활성화");
	}

	/// <summary>
	/// 트랩 비활성화
	/// </summary>
	public virtual void DeactivateTrap()
	{
		// 실행 중이면 먼저 정지
		if (isRunning)
		{
			StopTrap();
		}

		isActive = false;
		Debug.Log($"[TrapBase] {trapName} 비활성화");
	}

	#endregion

	#region RoomController 연결

	/// <summary>
	/// RoomController 설정
	/// </summary>
	/// <param name="roomController">연결할 RoomController</param>
	public void SetRoomController(RoomController roomController)
	{
		connectedRoomController = roomController;
		Debug.Log($"[TrapBase] {trapName}에 RoomController 설정: {roomController?.name ?? "null"}");
	}

	/// <summary>
	/// 연결된 RoomController 가져오기
	/// </summary>
	/// <returns>연결된 RoomController</returns>
	public RoomController GetRoomController()
	{
		return connectedRoomController;
	}

	#endregion

	#region 디버그 및 유틸리티

	/// <summary>
	/// 트랩 정보를 문자열로 반환 (디버깅용)
	/// </summary>
	public virtual string GetDebugInfo()
	{
		return $"트랩 이름: {trapName}, 활성화: {isActive}, 실행 중: {isRunning}";
	}

	#endregion
}

