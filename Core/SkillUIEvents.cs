using System;
using UnityEngine;

/// <summary>
/// 스킬 UI 이벤트 시스템
/// 옵저버 패턴을 사용하여 스킬 상태 변경을 UI에 알립니다
/// </summary>
public static class SkillUIEvents
{
	#region 스킬 슬롯 변경 이벤트
	
	/// <summary>
	/// 스킬 슬롯 데이터 변경 이벤트
	/// 매개변수: (슬롯 타입, 스킬 데이터)
	/// </summary>
	public static event Action<SkillSlotType, PlayerSkillData> OnSkillSlotChanged;
	
	/// <summary>
	/// 스킬 슬롯 데이터 변경 이벤트 호출
	/// </summary>
	public static void InvokeSkillSlotChanged(SkillSlotType slotType, PlayerSkillData skillData)
	{
		OnSkillSlotChanged?.Invoke(slotType, skillData);
	}
	
	#endregion
	
	#region 쿨타임 이벤트
	
	/// <summary>
	/// 쿨타임 시작 이벤트
	/// 매개변수: (슬롯 타입, 쿨타임 시간)
	/// </summary>
	public static event Action<SkillSlotType, float> OnCooldownStarted;
	
	/// <summary>
	/// 쿨타임 업데이트 이벤트 (매 프레임)
	/// 매개변수: (슬롯 타입, 남은 쿨타임, 전체 쿨타임)
	/// </summary>
	public static event Action<SkillSlotType, float, float> OnCooldownUpdated;
	
	/// <summary>
	/// 쿨타임 종료 이벤트
	/// 매개변수: (슬롯 타입)
	/// </summary>
	public static event Action<SkillSlotType> OnCooldownEnded;
	
	/// <summary>
	/// 쿨타임 시작 이벤트 호출
	/// </summary>
	public static void InvokeCooldownStarted(SkillSlotType slotType, float cooldownTime)
	{
		OnCooldownStarted?.Invoke(slotType, cooldownTime);
	}
	
	/// <summary>
	/// 쿨타임 업데이트 이벤트 호출
	/// </summary>
	public static void InvokeCooldownUpdated(SkillSlotType slotType, float remainingTime, float totalTime)
	{
		OnCooldownUpdated?.Invoke(slotType, remainingTime, totalTime);
	}
	
	/// <summary>
	/// 쿨타임 종료 이벤트 호출
	/// </summary>
	public static void InvokeCooldownEnded(SkillSlotType slotType)
	{
		OnCooldownEnded?.Invoke(slotType);
	}
	
	#endregion
	
	#region 스킬 사용 가능 상태 이벤트
	
	/// <summary>
	/// 스킬 사용 가능 상태 변경 이벤트
	/// 매개변수: (슬롯 타입, 사용 가능 여부, 불가능 이유)
	/// </summary>
	public static event Action<SkillSlotType, bool, string> OnSkillAvailabilityChanged;
	
	/// <summary>
	/// 스킬 사용 가능 상태 변경 이벤트 호출
	/// </summary>
	public static void InvokeSkillAvailabilityChanged(SkillSlotType slotType, bool isAvailable, string reason = "")
	{
		OnSkillAvailabilityChanged?.Invoke(slotType, isAvailable, reason);
	}
	
	#endregion
	
	#region 스킬 사용 이벤트
	
	/// <summary>
	/// 스킬 사용 이벤트
	/// 매개변수: (슬롯 타입, 스킬 데이터)
	/// </summary>
	public static event Action<SkillSlotType, PlayerSkillData> OnSkillUsed;
	
	/// <summary>
	/// 스킬 사용 실패 이벤트
	/// 매개변수: (슬롯 타입, 실패 이유)
	/// </summary>
	public static event Action<SkillSlotType, string> OnSkillUseFailed;
	
	/// <summary>
	/// 스킬 사용 이벤트 호출
	/// </summary>
	public static void InvokeSkillUsed(SkillSlotType slotType, PlayerSkillData skillData)
	{
		OnSkillUsed?.Invoke(slotType, skillData);
	}
	
	/// <summary>
	/// 스킬 사용 실패 이벤트 호출
	/// </summary>
	public static void InvokeSkillUseFailed(SkillSlotType slotType, string reason)
	{
		OnSkillUseFailed?.Invoke(slotType, reason);
	}
	
	#endregion
	
	#region 유틸리티
	
	/// <summary>
	/// 모든 이벤트 구독 해제 (씬 전환 시 메모리 누수 방지)
	/// </summary>
	public static void ClearAllEvents()
	{
		OnSkillSlotChanged = null;
		OnCooldownStarted = null;
		OnCooldownUpdated = null;
		OnCooldownEnded = null;
		OnSkillAvailabilityChanged = null;
		OnSkillUsed = null;
		OnSkillUseFailed = null;
		
		Debug.Log("[SkillUIEvents] 모든 스킬 UI 이벤트가 초기화되었습니다.");
	}
	
	#endregion
}

