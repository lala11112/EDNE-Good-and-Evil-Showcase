/*
 * ========================================
 * RotatingCircleBlade.cs
 * ========================================
 * [설명]
 * 회전하는 원형 칼날 트랩입니다.
 * 접근하는 플레이어에게 데미지를 입히고 넉백시킵니다.
 * 몬스터는 영향을 받지 않습니다.
 * 
 * [주요 기능]
 * - 원형 스프라이트 회전
 * - 플레이어 충돌 감지 및 데미지 + 넉백
 * - PlayerController의 ApplyKnockback 메서드 사용
 * - 작동 시 이펙트/사운드
 * - 정지 시 이펙트/사운드
 * 
 * [사용 방법]
 * 1. 빈 GameObject 생성
 * 2. SpriteRenderer 추가 (회전할 원형 스프라이트 할당)
 * 3. CircleCollider2D 추가 (IsTrigger = true)
 * 4. 이 스크립트 추가
 * 5. Inspector에서 회전 속도, 데미지, 넉백 힘 설정
 * 6. 이펙트 및 사운드 리소스 할당
 * ========================================
 */

using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class RotatingCircleBladeTrap : TrapBase
{
	#region 회전 설정

	[Header("회전 설정")]
	[Tooltip("회전 속도 (도/초)")]
	[SerializeField] private float rotationSpeed = 360f;

	[Tooltip("회전 방향")]
	[SerializeField] private RotationDirection rotationDirection = RotationDirection.Clockwise;

	[Tooltip("회전할 스프라이트 렌더러 (없으면 자동 찾기)")]
	[SerializeField] private SpriteRenderer rotatingSprite;

	#endregion

	#region 공격 설정

	[Header("공격 설정")]
	[Tooltip("적에게 입히는 데미지")]
	[SerializeField] private int damage = 10;

	[Tooltip("넉백 힘")]
	[SerializeField] private float knockbackForce = 15f;

	[Tooltip("넉백 지속 시간 (초)")]
	[SerializeField] private float knockbackDuration = 0.3f;

	[Tooltip("데미지 간격 (초) - 같은 적에게 연속으로 데미지를 주는 시간")]
	[SerializeField] private float damageInterval = 0.5f;

	#endregion

	#region 이펙트 및 사운드

	[Header("작동 시 이펙트/사운드")]
	[Tooltip("기계음 (작동 시작 시 재생)")]
	[SerializeField] private AudioClip machineStartSound;

	[Tooltip("기계음 (작동 중 반복 재생, 없으면 재생 안 함)")]
	[SerializeField] private AudioClip machineLoopSound;

	[Tooltip("불빛 점등 이펙트 (작동 중 지속)")]
	[SerializeField] private GameObject lightEffect;

	[Tooltip("스파크 이펙트 (작동 중 지속)")]
	[SerializeField] private GameObject sparkEffect;

	[Header("정지 시 이펙트/사운드")]
	[Tooltip("스팀 분출 이펙트 (정지 시 한 번)")]
	[SerializeField] private GameObject steamEffect;

	[Tooltip("금속음 감소 효과 (정지 시 재생)")]
	[SerializeField] private AudioClip metalStopSound;

	#endregion

	#region 컴포넌트

	private AudioSource audioSource;
	private Collider2D trapCollider;
	
	// 회전용 Transform (스프라이트 자식 오브젝트 사용 가능)
	private Transform rotatingTransform;

	#endregion

	#region 런타임 상태

	// 데미지를 받은 플레이어 추적 (간격 제어용)
	private Dictionary<PlayerController, float> lastDamageTime = new Dictionary<PlayerController, float>();

	// 이펙트 인스턴스
	private GameObject lightEffectInstance;
	private GameObject sparkEffectInstance;

	// 사운드 관련
	private bool isPlayingLoopSound = false;

	#endregion

	#region Unity Lifecycle

	protected override void Awake()
	{
		base.Awake();
		
		Debug.Log($"[RotatingCircleBlade] {trapName} Awake 시작");

		// 컴포넌트 자동 할당
		if (rotatingSprite == null)
		{
			rotatingSprite = GetComponent<SpriteRenderer>();
			Debug.Log($"[RotatingCircleBlade] SpriteRenderer 자동 할당: {(rotatingSprite != null ? "성공" : "실패")}");
		}
		else
		{
			Debug.Log($"[RotatingCircleBlade] SpriteRenderer 이미 할당됨");
		}

		if (trapCollider == null)
		{
			trapCollider = GetComponent<Collider2D>();
			Debug.Log($"[RotatingCircleBlade] Collider2D 자동 할당: {(trapCollider != null ? "성공" : "실패")}");
		}
		else
		{
			Debug.Log($"[RotatingCircleBlade] Collider2D 이미 할당됨");
		}

		// 회전 Transform 설정 (이 GameObject 또는 자식 오브젝트)
		rotatingTransform = transform;
		Debug.Log($"[RotatingCircleBlade] 회전 Transform 설정: {rotatingTransform.name}");

		// AudioSource 자동 추가
		audioSource = GetComponent<AudioSource>();
		if (audioSource == null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.playOnAwake = false;
			audioSource.loop = false;
			audioSource.spatialBlend = 0f; // 2D 사운드
			Debug.Log($"[RotatingCircleBlade] AudioSource 자동 추가 완료");
		}
		else
		{
			Debug.Log($"[RotatingCircleBlade] AudioSource 이미 존재");
		}

		// 초기 상태: 트리거 활성화
		if (trapCollider != null)
		{
			trapCollider.isTrigger = true;
			trapCollider.enabled = false; // 처음에는 비활성화
			Debug.Log($"[RotatingCircleBlade] Collider2D 초기 설정: isTrigger={trapCollider.isTrigger}, enabled={trapCollider.enabled}");
		}
		else
		{
			Debug.LogError($"[RotatingCircleBlade] Collider2D가 없습니다! 트랩이 작동하지 않을 수 있습니다.");
		}
		
		Debug.Log($"[RotatingCircleBlade] {trapName} Awake 완료 - 회전속도: {rotationSpeed}도/초, 데미지: {damage}, 넉백힘: {knockbackForce}");
	}

	private void Update()
	{
		// 트랩이 실행 중일 때만 회전
		if (isRunning)
		{
			RotateBlade();
			UpdateLoopSound();
		}
	}

	/// <summary>
	/// 에디터에서 컴포넌트 추가 시 자동 설정
	/// </summary>
	private void Reset()
	{
		// Collider2D 자동 설정
		Collider2D col = GetComponent<Collider2D>();
		if (col == null)
		{
			col = gameObject.AddComponent<CircleCollider2D>();
		}

		if (col != null)
		{
			col.isTrigger = true;
		}

		trapName = "Rotating Circle Blade";
	}

	#endregion

	#region TrapBase 구현

	/// <summary>
	/// 트랩 시작 시 호출
	/// </summary>
	protected override void OnTrapStarted()
	{
		Debug.Log($"[RotatingCircleBlade] OnTrapStarted 호출됨 - {trapName}");
		
		// 콜라이더 활성화
		if (trapCollider != null)
		{
			trapCollider.enabled = true;
			Debug.Log($"[RotatingCircleBlade] Collider2D 활성화 완료 (isTrigger={trapCollider.isTrigger}, enabled={trapCollider.enabled})");
		}
		else
		{
			Debug.LogError($"[RotatingCircleBlade] Collider2D가 null입니다! 트랩이 충돌을 감지할 수 없습니다.");
		}

		// 회전 Transform 확인
		if (rotatingTransform == null)
		{
			Debug.LogWarning($"[RotatingCircleBlade] rotatingTransform이 null입니다! 회전하지 않을 수 있습니다.");
		}

		// 작동 시 이펙트/사운드 재생
		PlayStartEffects();

		Debug.Log($"[RotatingCircleBlade] {trapName} 작동 시작 완료 - isRunning={isRunning}");
	}

	/// <summary>
	/// 트랩 정지 시 호출
	/// </summary>
	protected override void OnTrapStopped()
	{
		// 콜라이더 비활성화
		if (trapCollider != null)
		{
			trapCollider.enabled = false;
		}

		// 정지 시 이펙트/사운드 재생
		PlayStopEffects();

		// 데미지 추적 초기화
		lastDamageTime.Clear();

		Debug.Log($"[RotatingCircleBlade] {trapName} 작동 정지");
	}

	#endregion

	#region 회전 처리

	/// <summary>
	/// 칼날 회전 처리
	/// </summary>
	private void RotateBlade()
	{
		if (rotatingTransform == null)
		{
			Debug.LogWarning($"[RotatingCircleBlade] rotatingTransform이 null입니다. 회전할 수 없습니다.");
			return;
		}

		float rotationAmount = rotationSpeed * Time.deltaTime;
		
		// 회전 방향에 따라 부호 결정
		if (rotationDirection == RotationDirection.CounterClockwise)
		{
			rotationAmount = -rotationAmount;
		}

		// Z축 회전 (2D에서는 Z축 기준)
		rotatingTransform.Rotate(0f, 0f, rotationAmount);
	}

	#endregion

	#region 충돌 처리

	/// <summary>
	/// 트리거 충돌 시작 시 호출
	/// </summary>
	private void OnTriggerEnter2D(Collider2D other)
	{
		if (!isRunning)
		{
			Debug.Log($"[RotatingCircleBlade] OnTriggerEnter2D 호출되었지만 트랩이 실행 중이 아님 (isRunning={isRunning})");
			return;
		}

		Debug.Log($"[RotatingCircleBlade] OnTriggerEnter2D: {other.name} 충돌 감지");

		// 플레이어 감지 (몬스터는 무시)
		PlayerController player = other.GetComponent<PlayerController>();
		if (player != null)
		{
			Debug.Log($"[RotatingCircleBlade] PlayerController 발견: {player.name}");
			HandlePlayerHit(player);
		}
		else
		{
			Debug.Log($"[RotatingCircleBlade] {other.name}에 PlayerController가 없습니다. (몬스터 또는 기타 오브젝트 - 무시)");
		}
	}

	/// <summary>
	/// 트리거 충돌 유지 시 호출 (연속 충돌 처리)
	/// </summary>
	private void OnTriggerStay2D(Collider2D other)
	{
		if (!isRunning) return;

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
		Debug.Log($"[RotatingCircleBlade] HandlePlayerHit 호출: {player?.name ?? "null"}");
		
		if (player == null)
		{
			Debug.LogWarning($"[RotatingCircleBlade] player가 null입니다.");
			return;
		}
		
		if (!player.IsAlive)
		{
			Debug.Log($"[RotatingCircleBlade] {player.name}이 이미 사망했습니다. 무시됩니다.");
			return;
		}

		Debug.Log($"[RotatingCircleBlade] {player.name} PlayerController 찾음 - 현재 체력: {player.CurrentHealth}/{player.MaxHealth}");

		// 데미지 간격 체크
		if (lastDamageTime.ContainsKey(player))
		{
			float timeSinceLastDamage = Time.time - lastDamageTime[player];
			if (timeSinceLastDamage < damageInterval)
			{
				Debug.Log($"[RotatingCircleBlade] {player.name} 데미지 간격 대기 중... ({timeSinceLastDamage:F2}초 / {damageInterval}초)");
				return; // 아직 간격이 안 지났음
			}
			else
			{
				Debug.Log($"[RotatingCircleBlade] {player.name} 데미지 간격 충족됨 ({timeSinceLastDamage:F2}초 경과)");
			}
		}

		// 데미지 적용 (플레이어에게 환경 데미지 적용)
		Debug.Log($"[RotatingCircleBlade] {player.name}에게 {damage} 데미지 적용 시도...");
		bool damageApplied = player.ApplyDamage(damage, EntityType.Environment, this);
		
		if (damageApplied)
		{
			// 데미지 시간 기록
			lastDamageTime[player] = Time.time;

			// 넉백 적용 (플레이어의 ApplyKnockback 메서드 사용)
			Debug.Log($"[RotatingCircleBlade] {player.name} 넉백 적용 시도...");
			ApplyKnockbackToPlayer(player);

			Debug.Log($"[RotatingCircleBlade] {player.name}에게 {damage} 데미지 + 넉백 적용 완료! (남은 체력: {player.CurrentHealth}/{player.MaxHealth})");
		}
		else
		{
			Debug.LogWarning($"[RotatingCircleBlade] {player.name}에게 데미지 적용 실패 (무적 상태이거나 이미 사망)");
		}
	}

	/// <summary>
	/// 플레이어에게 넉백 적용
	/// PlayerController의 ApplyKnockback 메서드를 사용합니다.
	/// </summary>
	/// <param name="player">넉백을 적용할 플레이어</param>
	private void ApplyKnockbackToPlayer(PlayerController player)
	{
		if (player == null)
		{
			Debug.LogWarning($"[RotatingCircleBlade] ApplyKnockbackToPlayer: player가 null입니다.");
			return;
		}

		// 트랩 중심에서 플레이어 방향으로 넉백 방향 계산
		Vector2 direction = (player.transform.position - transform.position).normalized;
		Debug.Log($"[RotatingCircleBlade] {player.name} 넉백 방향 계산: {direction}, 힘: {knockbackForce}, 시간: {knockbackDuration}초");

		// 플레이어의 ApplyKnockback 메서드 사용
		player.ApplyKnockback(direction, knockbackForce, knockbackDuration);
		Debug.Log($"[RotatingCircleBlade] {player.name}에게 PlayerController.ApplyKnockback 호출 완료");
	}

	#endregion

	#region 이펙트 및 사운드

	/// <summary>
	/// 작동 시작 이펙트/사운드 재생
	/// </summary>
	private void PlayStartEffects()
	{
		Debug.Log($"[RotatingCircleBlade] PlayStartEffects 호출됨");
		
		// 기계음 재생
		if (machineStartSound != null && audioSource != null)
		{
			audioSource.PlayOneShot(machineStartSound);
			Debug.Log($"[RotatingCircleBlade] 기계음 재생: {machineStartSound.name}");
		}
		else
		{
			if (machineStartSound == null)
				Debug.LogWarning($"[RotatingCircleBlade] machineStartSound가 설정되지 않았습니다.");
			if (audioSource == null)
				Debug.LogError($"[RotatingCircleBlade] audioSource가 null입니다!");
		}

		// 불빛 점등 이펙트 활성화
		if (lightEffect != null)
		{
			lightEffectInstance = Instantiate(lightEffect, transform.position, transform.rotation, transform);
			lightEffectInstance.SetActive(true);
			Debug.Log($"[RotatingCircleBlade] 불빛 점등 이펙트 활성화: {lightEffect.name}");
		}
		else
		{
			Debug.LogWarning($"[RotatingCircleBlade] lightEffect가 설정되지 않았습니다.");
		}

		// 스파크 이펙트 활성화
		if (sparkEffect != null)
		{
			sparkEffectInstance = Instantiate(sparkEffect, transform.position, transform.rotation, transform);
			sparkEffectInstance.SetActive(true);
			Debug.Log($"[RotatingCircleBlade] 스파크 이펙트 활성화: {sparkEffect.name}");
		}
		else
		{
			Debug.LogWarning($"[RotatingCircleBlade] sparkEffect가 설정되지 않았습니다.");
		}

		// 루프 사운드 시작 (있는 경우)
		if (machineLoopSound != null && audioSource != null)
		{
			isPlayingLoopSound = true;
			audioSource.clip = machineLoopSound;
			audioSource.loop = true;
			audioSource.Play();
			Debug.Log($"[RotatingCircleBlade] 루프 사운드 시작: {machineLoopSound.name}");
		}
		else
		{
			if (machineLoopSound == null)
				Debug.Log($"[RotatingCircleBlade] machineLoopSound가 설정되지 않아 루프 사운드를 재생하지 않습니다.");
		}
	}

	/// <summary>
	/// 정지 이펙트/사운드 재생
	/// </summary>
	private void PlayStopEffects()
	{
		Debug.Log($"[RotatingCircleBlade] PlayStopEffects 호출됨");
		
		// 루프 사운드 정지
		if (isPlayingLoopSound && audioSource != null)
		{
			audioSource.Stop();
			audioSource.loop = false;
			isPlayingLoopSound = false;
			Debug.Log($"[RotatingCircleBlade] 루프 사운드 정지 완료");
		}

		// 금속음 감소 효과 재생
		if (metalStopSound != null && audioSource != null)
		{
			audioSource.PlayOneShot(metalStopSound);
			Debug.Log($"[RotatingCircleBlade] 금속음 감소 효과 재생: {metalStopSound.name}");
		}
		else
		{
			if (metalStopSound == null)
				Debug.LogWarning($"[RotatingCircleBlade] metalStopSound가 설정되지 않았습니다.");
		}

		// 불빛 이펙트 제거
		if (lightEffectInstance != null)
		{
			Destroy(lightEffectInstance);
			lightEffectInstance = null;
			Debug.Log($"[RotatingCircleBlade] 불빛 이펙트 제거 완료");
		}

		// 스파크 이펙트 제거
		if (sparkEffectInstance != null)
		{
			Destroy(sparkEffectInstance);
			sparkEffectInstance = null;
			Debug.Log($"[RotatingCircleBlade] 스파크 이펙트 제거 완료");
		}

		// 스팀 분출 이펙트 재생 (한 번)
		if (steamEffect != null)
		{
			GameObject steamInstance = Instantiate(steamEffect, transform.position, transform.rotation);
			Destroy(steamInstance, 3f); // 3초 후 자동 삭제
			Debug.Log($"[RotatingCircleBlade] 스팀 분출 이펙트 재생: {steamEffect.name} (3초 후 자동 삭제)");
		}
		else
		{
			Debug.LogWarning($"[RotatingCircleBlade] steamEffect가 설정되지 않았습니다.");
		}
	}

	/// <summary>
	/// 루프 사운드 업데이트
	/// </summary>
	private void UpdateLoopSound()
	{
		// 루프 사운드가 설정되어 있고 재생 중이 아니면 재생
		if (machineLoopSound != null && audioSource != null && !isPlayingLoopSound)
		{
			audioSource.clip = machineLoopSound;
			audioSource.loop = true;
			audioSource.Play();
			isPlayingLoopSound = true;
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
		return $"{baseInfo}\n회전 속도: {rotationSpeed}도/초, 데미지: {damage}, 넉백 힘: {knockbackForce}";
	}

	#endregion
}

/// <summary>
/// 회전 방향 열거형
/// </summary>
public enum RotationDirection
{
	/// <summary>
	/// 시계 방향
	/// </summary>
	Clockwise,

	/// <summary>
	/// 반시계 방향
	/// </summary>
	CounterClockwise
}

