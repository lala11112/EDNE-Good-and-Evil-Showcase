/*
 * ========================================
 * RotatingCutterBlade.cs
 * ========================================
 * [설명]
 * 방 중앙에 배치되는 회전하는 커터 블레이드 트랩입니다.
 * 중심에서 회전을 관리하고, 자식 칼날 세그먼트들을 제어합니다.
 * 
 * [주요 기능]
 * - 회전 제어 (중심 Transform 회전)
 * - 자식 BladeSegment 자동 찾기 및 활성화/비활성화
 * - 이펙트/사운드 관리
 * - 트랩 시작/정지 제어
 * 
 * [사용 방법]
 * 1. 빈 GameObject 생성 (중심)
 * 2. 빈 자식 GameObject들 생성 (칼날 - 3개 또는 4개)
 * 3. 각 칼날에:
 *    - SpriteRenderer 추가 (칼날 스프라이트 할당)
 *    - BoxCollider2D 또는 EdgeCollider2D 추가 (IsTrigger = true)
 *    - BladeSegment 스크립트 추가
 * 4. 중심 GameObject에 RotatingCutterBlade 스크립트 추가
 * 5. Inspector에서 회전 속도, 칼날 개수 등 설정
 * 6. 이펙트 및 사운드 리소스 할당
 * ========================================
 */

using UnityEngine;
using System.Collections.Generic;

public class RotatingCutterBlade : TrapBase
{
	#region 회전 설정

	[Header("회전 설정")]
	[Tooltip("회전 속도 (도/초)")]
	[SerializeField] private float rotationSpeed = 180f;

	[Tooltip("회전 방향")]
	[SerializeField] private RotationDirection rotationDirection = RotationDirection.Clockwise;

	[Tooltip("회전할 Transform (빈 오브젝트, 자식 칼날들을 포함)")]
	[SerializeField] private Transform rotatingTransform;

	#endregion

	#region 칼날 관리

	[Header("칼날 관리")]
	[Tooltip("칼날 세그먼트들 (자동 찾기 가능)")]
	[SerializeField] private List<BladeSegment> bladeSegments = new List<BladeSegment>();

	[Tooltip("자동으로 자식에서 칼날 찾기")]
	[SerializeField] private bool autoFindBlades = true;

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

		Debug.Log($"[RotatingCutterBlade] {trapName} Awake 시작");

		// 회전 Transform 설정
		if (rotatingTransform == null)
		{
			rotatingTransform = transform;
			Debug.Log($"[RotatingCutterBlade] rotatingTransform 자동 설정: {rotatingTransform.name}");
		}
		else
		{
			Debug.Log($"[RotatingCutterBlade] rotatingTransform 이미 설정됨: {rotatingTransform.name}");
		}

		// AudioSource 자동 추가
		audioSource = GetComponent<AudioSource>();
		if (audioSource == null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.playOnAwake = false;
			audioSource.loop = false;
			audioSource.spatialBlend = 0f; // 2D 사운드
			Debug.Log($"[RotatingCutterBlade] AudioSource 자동 추가 완료");
		}
		else
		{
			Debug.Log($"[RotatingCutterBlade] AudioSource 이미 존재");
		}

		// 칼날 세그먼트 자동 찾기
		if (autoFindBlades)
		{
			FindBladeSegments();
		}

		// 초기 상태: 모든 칼날 비활성화
		DeactivateAllBlades();

		Debug.Log($"[RotatingCutterBlade] {trapName} Awake 완료 - 회전속도: {rotationSpeed}도/초, 칼날 개수: {bladeSegments.Count}개");
	}

	private void Update()
	{
		// 트랩이 실행 중일 때만 회전
		if (isRunning)
		{
			RotateCutter();
			UpdateLoopSound();
		}
	}

	/// <summary>
	/// 에디터에서 컴포넌트 추가 시 자동 설정
	/// </summary>
	private void Reset()
	{
		trapName = "Rotating Cutter Blade";
	}

	#endregion

	#region TrapBase 구현

	/// <summary>
	/// 트랩 시작 시 호출
	/// </summary>
	protected override void OnTrapStarted()
	{
		Debug.Log($"[RotatingCutterBlade] OnTrapStarted 호출됨 - {trapName}");

		// 회전 Transform 확인
		if (rotatingTransform == null)
		{
			Debug.LogError($"[RotatingCutterBlade] rotatingTransform이 null입니다! 회전하지 않을 수 있습니다.");
		}

		// 모든 칼날 활성화
		ActivateAllBlades();

		// 작동 시 이펙트/사운드 재생
		PlayStartEffects();

		Debug.Log($"[RotatingCutterBlade] {trapName} 작동 시작 완료 - isRunning={isRunning}, 활성화된 칼날: {GetActiveBladeCount()}개");
	}

	/// <summary>
	/// 트랩 정지 시 호출
	/// </summary>
	protected override void OnTrapStopped()
	{
		Debug.Log($"[RotatingCutterBlade] OnTrapStarted 호출됨 - {trapName}");

		// 모든 칼날 비활성화
		DeactivateAllBlades();

		// 정지 시 이펙트/사운드 재생
		PlayStopEffects();

		Debug.Log($"[RotatingCutterBlade] {trapName} 작동 정지 완료");
	}

	#endregion

	#region 회전 처리

	/// <summary>
	/// 커터 블레이드 회전 처리
	/// </summary>
	private void RotateCutter()
	{
		if (rotatingTransform == null)
		{
			Debug.LogWarning($"[RotatingCutterBlade] rotatingTransform이 null입니다. 회전할 수 없습니다.");
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

	#region 칼날 관리

	/// <summary>
	/// 자식에서 칼날 세그먼트 자동 찾기
	/// </summary>
	private void FindBladeSegments()
	{
		bladeSegments.Clear();

		// 자식 오브젝트에서 BladeSegment 찾기
		BladeSegment[] foundSegments = GetComponentsInChildren<BladeSegment>();
		bladeSegments.AddRange(foundSegments);

		Debug.Log($"[RotatingCutterBlade] 자식에서 칼날 세그먼트 찾기: {bladeSegments.Count}개 발견");

		// 각 칼날 정보 출력
		for (int i = 0; i < bladeSegments.Count; i++)
		{
			if (bladeSegments[i] != null)
			{
				Debug.Log($"[RotatingCutterBlade] 칼날 {i + 1}: {bladeSegments[i].name}");
			}
		}

		if (bladeSegments.Count == 0)
		{
			Debug.LogWarning($"[RotatingCutterBlade] 칼날 세그먼트를 찾을 수 없습니다! 자식 오브젝트에 BladeSegment 컴포넌트를 추가하세요.");
		}
	}

	/// <summary>
	/// 모든 칼날 활성화
	/// </summary>
	private void ActivateAllBlades()
	{
		int activatedCount = 0;

		foreach (var blade in bladeSegments)
		{
			if (blade != null)
			{
				blade.ActivateSegment();
				activatedCount++;
			}
		}

		Debug.Log($"[RotatingCutterBlade] 모든 칼날 활성화 완료: {activatedCount}개 활성화");
	}

	/// <summary>
	/// 모든 칼날 비활성화
	/// </summary>
	private void DeactivateAllBlades()
	{
		int deactivatedCount = 0;

		foreach (var blade in bladeSegments)
		{
			if (blade != null)
			{
				blade.DeactivateSegment();
				deactivatedCount++;
			}
		}

		Debug.Log($"[RotatingCutterBlade] 모든 칼날 비활성화 완료: {deactivatedCount}개 비활성화");
	}

	/// <summary>
	/// 현재 활성화된 칼날 개수
	/// </summary>
	/// <returns>활성화된 칼날 개수</returns>
	private int GetActiveBladeCount()
	{
		int count = 0;
		foreach (var blade in bladeSegments)
		{
			if (blade != null && blade.IsActive)
			{
				count++;
			}
		}
		return count;
	}

	/// <summary>
	/// 칼날 세그먼트 수동 추가
	/// </summary>
	/// <param name="blade">추가할 칼날</param>
	public void AddBladeSegment(BladeSegment blade)
	{
		if (blade == null)
		{
			Debug.LogError($"[RotatingCutterBlade] null 칼날을 추가하려고 시도했습니다.");
			return;
		}

		if (!bladeSegments.Contains(blade))
		{
			bladeSegments.Add(blade);
			Debug.Log($"[RotatingCutterBlade] 칼날 추가: {blade.name} (총 {bladeSegments.Count}개)");
		}
		else
		{
			Debug.LogWarning($"[RotatingCutterBlade] 칼날 {blade.name}이 이미 등록되어 있습니다.");
		}
	}

	#endregion

	#region 이펙트 및 사운드

	/// <summary>
	/// 작동 시작 이펙트/사운드 재생
	/// </summary>
	private void PlayStartEffects()
	{
		Debug.Log($"[RotatingCutterBlade] PlayStartEffects 호출됨");

		// 기계음 재생
		if (machineStartSound != null && audioSource != null)
		{
			audioSource.PlayOneShot(machineStartSound);
			Debug.Log($"[RotatingCutterBlade] 기계음 재생: {machineStartSound.name}");
		}
		else
		{
			if (machineStartSound == null)
				Debug.LogWarning($"[RotatingCutterBlade] machineStartSound가 설정되지 않았습니다.");
			if (audioSource == null)
				Debug.LogError($"[RotatingCutterBlade] audioSource가 null입니다!");
		}

		// 불빛 점등 이펙트 활성화
		if (lightEffect != null)
		{
			lightEffectInstance = Instantiate(lightEffect, transform.position, transform.rotation, transform);
			lightEffectInstance.SetActive(true);
			Debug.Log($"[RotatingCutterBlade] 불빛 점등 이펙트 활성화: {lightEffect.name}");
		}
		else
		{
			Debug.LogWarning($"[RotatingCutterBlade] lightEffect가 설정되지 않았습니다.");
		}

		// 스파크 이펙트 활성화
		if (sparkEffect != null)
		{
			sparkEffectInstance = Instantiate(sparkEffect, transform.position, transform.rotation, transform);
			sparkEffectInstance.SetActive(true);
			Debug.Log($"[RotatingCutterBlade] 스파크 이펙트 활성화: {sparkEffect.name}");
		}
		else
		{
			Debug.LogWarning($"[RotatingCutterBlade] sparkEffect가 설정되지 않았습니다.");
		}

		// 루프 사운드 시작 (있는 경우)
		if (machineLoopSound != null && audioSource != null)
		{
			isPlayingLoopSound = true;
			audioSource.clip = machineLoopSound;
			audioSource.loop = true;
			audioSource.Play();
			Debug.Log($"[RotatingCutterBlade] 루프 사운드 시작: {machineLoopSound.name}");
		}
		else
		{
			if (machineLoopSound == null)
				Debug.Log($"[RotatingCutterBlade] machineLoopSound가 설정되지 않아 루프 사운드를 재생하지 않습니다.");
		}
	}

	/// <summary>
	/// 정지 이펙트/사운드 재생
	/// </summary>
	private void PlayStopEffects()
	{
		Debug.Log($"[RotatingCutterBlade] PlayStopEffects 호출됨");

		// 루프 사운드 정지
		if (isPlayingLoopSound && audioSource != null)
		{
			audioSource.Stop();
			audioSource.loop = false;
			isPlayingLoopSound = false;
			Debug.Log($"[RotatingCutterBlade] 루프 사운드 정지 완료");
		}

		// 금속음 감소 효과 재생
		if (metalStopSound != null && audioSource != null)
		{
			audioSource.PlayOneShot(metalStopSound);
			Debug.Log($"[RotatingCutterBlade] 금속음 감소 효과 재생: {metalStopSound.name}");
		}
		else
		{
			if (metalStopSound == null)
				Debug.LogWarning($"[RotatingCutterBlade] metalStopSound가 설정되지 않았습니다.");
		}

		// 불빛 이펙트 제거
		if (lightEffectInstance != null)
		{
			Destroy(lightEffectInstance);
			lightEffectInstance = null;
			Debug.Log($"[RotatingCutterBlade] 불빛 이펙트 제거 완료");
		}

		// 스파크 이펙트 제거
		if (sparkEffectInstance != null)
		{
			Destroy(sparkEffectInstance);
			sparkEffectInstance = null;
			Debug.Log($"[RotatingCutterBlade] 스파크 이펙트 제거 완료");
		}

		// 스팀 분출 이펙트 재생 (한 번)
		if (steamEffect != null)
		{
			GameObject steamInstance = Instantiate(steamEffect, transform.position, transform.rotation);
			Destroy(steamInstance, 3f); // 3초 후 자동 삭제
			Debug.Log($"[RotatingCutterBlade] 스팀 분출 이펙트 재생: {steamEffect.name} (3초 후 자동 삭제)");
		}
		else
		{
			Debug.LogWarning($"[RotatingCutterBlade] steamEffect가 설정되지 않았습니다.");
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
		int activeBlades = GetActiveBladeCount();
		return $"{baseInfo}\n회전 속도: {rotationSpeed}도/초, 칼날 개수: {bladeSegments.Count}개 (활성화: {activeBlades}개)";
	}

	/// <summary>
	/// 회전 방향 가져오기 (BladeSegment에서 호출)
	/// </summary>
	/// <returns>회전 방향</returns>
	public RotationDirection GetRotationDirection()
	{
		return rotationDirection;
	}

	#endregion
}

