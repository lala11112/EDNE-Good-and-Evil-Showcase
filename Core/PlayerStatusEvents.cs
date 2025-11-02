using System;
using UnityEngine;

/// <summary>
/// 플레이어 상태 변경 이벤트 시스템
/// 옵저버 패턴을 사용하여 UI와 게임 로직을 분리합니다
/// </summary>
public static class PlayerStatusEvents
{
	#region 체력 이벤트
	
	/// <summary>
	/// 체력 변경 이벤트
	/// 매개변수: (현재 체력, 최대 체력)
	/// </summary>
	public static event Action<int, int> OnHealthChanged;
	
	/// <summary>
	/// 체력 변경 이벤트 호출
	/// </summary>
	public static void InvokeHealthChanged(int currentHealth, int maxHealth)
	{
		OnHealthChanged?.Invoke(currentHealth, maxHealth);
	}
	
	#endregion
	
	#region 정신력 이벤트
	
	/// <summary>
	/// 정신력 변경 이벤트
	/// 매개변수: (현재 정신력, 최대 정신력)
	/// </summary>
	public static event Action<int, int> OnMentalPowerChanged;
	
	/// <summary>
	/// 정신력 소모 이벤트
	/// 매개변수: (소모량)
	/// </summary>
	public static event Action<int> OnMentalPowerConsumed;
	
	/// <summary>
	/// 정신력 회복 이벤트
	/// 매개변수: (회복량)
	/// </summary>
	public static event Action<int> OnMentalPowerRestored;
	
	/// <summary>
	/// 정신력 변경 이벤트 호출
	/// </summary>
	public static void InvokeMentalPowerChanged(int currentMentalPower, int maxMentalPower)
	{
		OnMentalPowerChanged?.Invoke(currentMentalPower, maxMentalPower);
	}
	
	/// <summary>
	/// 정신력 소모 이벤트 호출
	/// </summary>
	public static void InvokeMentalPowerConsumed(int amount)
	{
		OnMentalPowerConsumed?.Invoke(amount);
	}
	
	/// <summary>
	/// 정신력 회복 이벤트 호출
	/// </summary>
	public static void InvokeMentalPowerRestored(int amount)
	{
		OnMentalPowerRestored?.Invoke(amount);
	}
	
	#endregion
	
	#region 용기 시스템 이벤트 (선택적)
	
	/// <summary>
	/// 용기 점수 변경 이벤트
	/// 매개변수: (현재 용기 점수, 용기 단계)
	/// </summary>
	public static event Action<int, CourageState> OnCourageChanged;
	
	/// <summary>
	/// 용기 단계 변경 이벤트
	/// 매개변수: (이전 단계, 새로운 단계)
	/// </summary>
	public static event Action<CourageState, CourageState> OnCourageStateChanged;
	
	/// <summary>
	/// 용기 점수 변경 이벤트 호출
	/// </summary>
	public static void InvokeCourageChanged(int courageScore, CourageState courageState)
	{
		OnCourageChanged?.Invoke(courageScore, courageState);
	}
	
	/// <summary>
	/// 용기 단계 변경 이벤트 호출
	/// </summary>
	public static void InvokeCourageStateChanged(CourageState previousState, CourageState newState)
	{
		OnCourageStateChanged?.Invoke(previousState, newState);
	}
	
	#endregion
	
	#region 플레이어 상태 이벤트
	
	/// <summary>
	/// 플레이어 사망 이벤트
	/// </summary>
	public static event Action OnPlayerDied;
	
	/// <summary>
	/// 플레이어 리스폰 이벤트
	/// </summary>
	public static event Action OnPlayerRespawned;
	
	/// <summary>
	/// 플레이어 사망 이벤트 호출
	/// </summary>
	public static void InvokePlayerDied()
	{
		OnPlayerDied?.Invoke();
	}
	
	/// <summary>
	/// 플레이어 리스폰 이벤트 호출
	/// </summary>
	public static void InvokePlayerRespawned()
	{
		OnPlayerRespawned?.Invoke();
	}
	
	#endregion
	
	#region 유틸리티
	
	/// <summary>
	/// 모든 이벤트 구독 해제 (씬 전환 시 메모리 누수 방지)
	/// </summary>
	public static void ClearAllEvents()
	{
		OnHealthChanged = null;
		OnMentalPowerChanged = null;
		OnMentalPowerConsumed = null;
		OnMentalPowerRestored = null;
		OnCourageChanged = null;
		OnCourageStateChanged = null;
		OnPlayerDied = null;
		OnPlayerRespawned = null;
		
		Debug.Log("[PlayerStatusEvents] 모든 이벤트가 초기화되었습니다.");
	}
	
	#endregion
}

