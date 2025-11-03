/*
 * ========================================
 * SpikeTrap.cs
 * ========================================
 * [설명]
 * 바닥에 설치된 가시 트랩입니다.
 * 방에 들어가면 활성화되고, 주기적으로 가시가 솟아올라 플레이어에게 데미지를 입힙니다.
 * 
 * [주요 기능]
 * - 방 진입 시 자동 활성화
 * - 주기적인 가시 솟아오름/내려감 (애니메이션)
 * - 플레이어 충돌 감지 및 데미지 적용
 * - 전투 클리어 시 자동 비활성화 및 리셋
 * 
 * [동작 순서]
 * 1. 방 진입 시 활성화
 * 2. 2초 대기 → 가시 솟음 (Spike_Up 애니메이션)
 * 3. 가시 올라온 상태 1초 유지 → 플레이어 충돌 시 데미지
 * 4. 1초 후 가시 내려감 (Spike_Down 애니메이션)
 * 5. 3초 후 다시 2~4단계 반복
 * 6. 전투 클리어 시 모든 가시 비활성화 및 Reset
 * 
 * [사용 방법]
 * 1. 빈 GameObject 생성
 * 2. Animator 추가 (Spike_Up, Spike_Down 애니메이션)
 * 3. Collider2D 추가 (IsTrigger = true)
 * 4. 이 스크립트 추가
 * 5. Inspector에서 데미지, 타이밍 설정
 * ========================================
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Animator))]
public class SpikeTrap : TrapBase
{
	#region 트랩 설정

	[Header("트랩 설정")]
	[Tooltip("플레이어에게 입히는 데미지")]
	[SerializeField] private int damage = 1;

	[Tooltip("데미지 간격 (초) - 같은 플레이어에게 연속으로 데미지를 주는 시간")]
	[SerializeField] private float damageInterval = 0.5f;

	#endregion

	#region 타이밍 설정

	[Header("타이밍 설정")]
	[Tooltip("가시 솟기 전 대기 시간 (초)")]
	[SerializeField] private float waitBeforeRise = 2f;

	[Tooltip("가시가 올라온 상태 유지 시간 (초)")]
	[SerializeField] private float spikeUpDuration = 1f;

	[Tooltip("가시 내려간 후 다음 사이클까지 대기 시간 (초)")]
	[SerializeField] private float waitAfterDown = 3f;

	#endregion

	#region 애니메이션 설정

	[Header("애니메이션 설정")]
	[Tooltip("가시 올라오는 애니메이션 트리거 이름")]
	[SerializeField] private string spikeUpTrigger = "Spike_Up";

	[Tooltip("가시 내려가는 애니메이션 트리거 이름")]
	[SerializeField] private string spikeDownTrigger = "Spike_Down";

	[Tooltip("리셋 애니메이션 트리거 이름 (선택사항)")]
	[SerializeField] private string resetTrigger = "Reset";

	#endregion

	#region 컴포넌트

	private Collider2D trapCollider;
	private Animator animator;

	#endregion

	#region 런타임 상태

	/// <summary>
	/// 가시가 올라온 상태인지 여부
	/// </summary>
	private bool isSpikeUp = false;

	/// <summary>
	/// 트랩 루프 코루틴
	/// </summary>
	private Coroutine trapLoopCoroutine;

	/// <summary>
	/// 데미지를 받은 플레이어 추적 (간격 제어용)
	/// </summary>
	private Dictionary<PlayerController, float> lastDamageTime = new Dictionary<PlayerController, float>();

	#endregion

	#region Unity Lifecycle

	protected override void Awake()
	{
		base.Awake();

		// 컴포넌트 자동 할당
		trapCollider = GetComponent<Collider2D>();
		if (trapCollider != null)
		{
			trapCollider.isTrigger = true;
			trapCollider.enabled = false; // 처음에는 비활성화
		}
		else
		{
			Debug.LogError($"[SpikeTrap] Collider2D가 없습니다!");
		}

		animator = GetComponent<Animator>();
		if (animator == null)
		{
			Debug.LogError($"[SpikeTrap] Animator가 없습니다!");
		}

		// 초기 상태
		isSpikeUp = false;
	}

	/// <summary>
	/// 에디터에서 컴포넌트 추가 시 자동 설정
	/// </summary>
	private void Reset()
	{
		trapName = "Spike Trap";
	}

	#endregion

	#region TrapBase 구현

	/// <summary>
	/// 트랩 시작 시 호출
	/// </summary>
	protected override void OnTrapStarted()
	{
		Debug.Log($"[SpikeTrap] {trapName} 활성화됨");

		// 콜라이더 활성화
		if (trapCollider != null)
		{
			trapCollider.enabled = true;
		}

		// 트랩 루프 시작
		if (trapLoopCoroutine == null)
		{
			trapLoopCoroutine = StartCoroutine(TrapLoopCoroutine());
		}
	}

	/// <summary>
	/// 트랩 정지 시 호출
	/// </summary>
	protected override void OnTrapStopped()
	{
		// 트랩 루프 정지
		if (trapLoopCoroutine != null)
		{
			StopCoroutine(trapLoopCoroutine);
			trapLoopCoroutine = null;
		}

		// 콜라이더 비활성화
		if (trapCollider != null)
		{
			trapCollider.enabled = false;
		}

		// 가시 내리기 및 리셋
		ResetTrap();

		// 데미지 추적 초기화
		lastDamageTime.Clear();
	}

	#endregion

	#region 트랩 루프

	/// <summary>
	/// 트랩 루프 코루틴
	/// 주기적으로 가시를 솟게 하고 내리게 합니다.
	/// </summary>
	private IEnumerator TrapLoopCoroutine()
	{
		while (isRunning)
		{
			// 1. 초기 대기 시간
			yield return new WaitForSeconds(waitBeforeRise);

			// 2. 가시 솟기
			if (isRunning)
			{
				RiseSpike();
			}

			// 3. 가시 올라온 상태 유지
			yield return new WaitForSeconds(spikeUpDuration);

			// 4. 가시 내리기
			if (isRunning)
			{
				LowerSpike();
			}

			// 5. 다음 사이클까지 대기
			yield return new WaitForSeconds(waitAfterDown);
		}
	}

	#endregion

	#region 가시 제어

	/// <summary>
	/// 가시 솟기
	/// </summary>
	private void RiseSpike()
	{
		isSpikeUp = true;

		// 애니메이션 트리거
		if (animator != null && !string.IsNullOrEmpty(spikeUpTrigger))
		{
			animator.SetTrigger(spikeUpTrigger);
		}
	}

	/// <summary>
	/// 가시 내리기
	/// </summary>
	private void LowerSpike()
	{
		isSpikeUp = false;

		// 애니메이션 트리거
		if (animator != null && !string.IsNullOrEmpty(spikeDownTrigger))
		{
			animator.SetTrigger(spikeDownTrigger);
		}
	}

	/// <summary>
	/// 트랩 리셋 (가시 내리기 및 초기 상태로)
	/// </summary>
	private void ResetTrap()
	{
		isSpikeUp = false;

		// 리셋 애니메이션 (있는 경우)
		if (animator != null && !string.IsNullOrEmpty(resetTrigger))
		{
			animator.SetTrigger(resetTrigger);
		}
		else
		{
			// 리셋 애니메이션이 없으면 내리기 애니메이션 사용
			if (animator != null && !string.IsNullOrEmpty(spikeDownTrigger))
			{
				animator.SetTrigger(spikeDownTrigger);
			}
		}
	}

	/// <summary>
	/// 가시가 올라온 상태인지 여부
	/// </summary>
	public bool IsSpikeUp => isSpikeUp;

	#endregion

	#region 충돌 처리

	/// <summary>
	/// 트리거 충돌 시작 시 호출
	/// </summary>
	private void OnTriggerEnter2D(Collider2D other)
	{
		if (!isRunning || !isSpikeUp)
		{
			return;
		}

		// 플레이어 감지 (몬스터는 무시)
		PlayerController player = other.GetComponent<PlayerController>();
		if (player != null)
		{
			HandlePlayerHit(player);
		}
	}

	/// <summary>
	/// 트리거 충돌 유지 시 호출 (연속 충돌 처리)
	/// </summary>
	private void OnTriggerStay2D(Collider2D other)
	{
		if (!isRunning || !isSpikeUp) return;

		// 플레이어 감지 (몬스터는 무시)
		PlayerController player = other.GetComponent<PlayerController>();
		if (player != null)
		{
			HandlePlayerHit(player);
		}
	}

	/// <summary>
	/// 플레이어 충돌 처리
	/// </summary>
	/// <param name="player">충돌한 플레이어</param>
	private void HandlePlayerHit(PlayerController player)
	{
		if (player == null || !player.IsAlive)
		{
			return;
		}

		// 데미지 간격 체크
		if (lastDamageTime.ContainsKey(player))
		{
			float timeSinceLastDamage = Time.time - lastDamageTime[player];
			if (timeSinceLastDamage < damageInterval)
			{
				Debug.Log($"[SpikeTrap] {player.name} 데미지 간격 대기 중... ({timeSinceLastDamage:F2}초 / {damageInterval}초)");
				return; // 아직 간격이 안 지났음
			}
		}

		// 데미지 적용 (플레이어에게 환경 데미지 적용)
		bool damageApplied = player.ApplyDamage(damage, EntityType.Environment, this);

		if (damageApplied)
		{
			// 데미지 시간 기록
			lastDamageTime[player] = Time.time;
		}
	}

	#endregion

	#region 디버그 및 유틸리티

	/// <summary>
	/// 트랩 정보를 문자열로 반환 (디버깅용)
	/// </summary>
	public override string GetDebugInfo()
	{
		string baseInfo = base.GetDebugInfo();
		return $"{baseInfo}\n데미지: {damage}, 가시 상태: {(isSpikeUp ? "올라옴" : "내려감")}, 대기: {waitBeforeRise}초, 유지: {spikeUpDuration}초, 다음: {waitAfterDown}초";
	}

	#endregion
}
