using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 빗발치는 믿음 스킬 핸들러 (리워크)
/// [1단계] 이동 경로에 잔상(도화선)을 무한으로 설치
/// [2단계] 재입력 시 모든 잔상이 동시에 적을 향해 돌진 공격
/// [특수] 다른 스킬 사용 시 자동 종료 + 잔상 모두 사라짐
/// </summary>
public class RainOfFaithHandler : MonoBehaviour, SkillHandlerInterface
{
	#region 기본 설정
	
	[Header("기본 데이터")]
	[Tooltip("스킬 데이터 (Setup에서 자동 설정)")]
	private PlayerSkillData skillData;
	
	[Tooltip("스킬 소유자 GameObject")]
	private GameObject owner;
	
	[Tooltip("플레이어 컨트롤러")]
	private PlayerController playerController;
	
	[Tooltip("스킬 슬롯 (쿨타임 시작용)")]
	private SkillSlot skillSlot;
	
	#endregion
	
	#region 설치 모드
	
	[Header("설치 모드 (1단계)")]
	[Tooltip("설치 모드 활성화 여부")]
	private bool isInSetupMode = false;
	
	[Tooltip("랭크별 이동속도 증가 배율")]
	[SerializeField] private float[] moveSpeedMultiplierByRank = new float[] { 1.3f, 1.35f, 1.4f, 1.5f, 1.6f };
	
	[Tooltip("원래 이동속도 (복원용)")]
	private float originalMoveSpeed = 0f;
	
	[Tooltip("현재 랭크")]
	private SkillRank currentRank;
	
	#endregion
	
	#region 잔상 생성 (도화선 설치)
	
	[Header("잔상 설정")]
	[Tooltip("잔상 프리팹 (비활성 상태로 생성)")]
	[SerializeField] private GameObject afterimagePrefab;
	
	[Tooltip("잔상 생성 간격 (초)")]
	[SerializeField] private float afterimageSpawnInterval = 0.15f;
	
	[Tooltip("잔상 기본 데미지 퍼센트")]
	[SerializeField] private float afterimageDamagePercent = 80f;
	
	[Tooltip("폭발 시 잔상 공격 감지 거리")]
	[SerializeField] private float afterimageAttackRange = 8f;
	
	[Tooltip("랭크별 잔상 최대 개수 제한 (0 = 무제한)")]
	[SerializeField] private int[] maxAfterimagesByRank = new int[] { 0, 0, 0, 0, 0 }; // 무제한
	
	[Tooltip("설치된 잔상 목록")]
	private List<GameObject> installedAfterimages = new List<GameObject>();
	
	[Tooltip("마지막 잔상 생성 이후 경과 시간")]
	private float timeSinceLastAfterimage = 0f;
	
	#endregion
	
	#region 폭발 모드 (2단계)
	
	[Header("폭발 모드 (2단계)")]
	[Tooltip("폭발 시 잔상 돌진 속도")]
	[SerializeField] private float afterimageRushSpeed = 20f;
	
	[Tooltip("폭발 시 연쇄 딜레이 (초, 동시 폭발 느낌)")]
	[SerializeField] private float explosionChainDelay = 0.02f;
	
	#endregion
	
	#region 이동 추적
	
	[Header("이동 추적")]
	[Tooltip("이전 위치 (잔상 생성 간격 체크용)")]
	private Vector3 lastPosition;
	
	#endregion
	
	#region S랭크 설정
	
	[Header("S랭크 특수 효과")]
	[Tooltip("S랭크 폭발 데미지 배율")]
	[SerializeField] private float sRankExplosionMultiplier = 2f;
	
	private bool isInSRankMode = false;
	
	#endregion
	
	#region 비주얼
	
	[Header("비주얼")]
	[Tooltip("플레이어 스프라이트 렌더러 (잔상 복사용)")]
	[SerializeField] private SpriteRenderer playerSpriteRenderer;
	
	[Tooltip("설치 모드 시 변경할 아이콘 (null이면 변경 안 함)")]
	[SerializeField] private Sprite setupModeIcon;
	
	[Header("사운드 이펙트")]
	[Tooltip("스킬 시작 효과음")]
	[SerializeField] private AudioClip skillStartSound;
	
	[Tooltip("잔상 생성 효과음")]
	[SerializeField] private AudioClip afterimageSpawnSound;
	
	#endregion
	
	#region SkillHandlerInterface 구현
	
	/// <summary>
	/// 스킬 초기화
	/// </summary>
	public void Setup(BaseSkillData data, GameObject owner)
	{
		this.skillData = data as PlayerSkillData;
		this.owner = owner;
		
		// 리스트 초기화
		if (installedAfterimages == null)
		{
			installedAfterimages = new List<GameObject>();
		}
		
		// PlayerController 찾기
		if (owner != null)
		{
			playerController = owner.GetComponent<PlayerController>();
			if (playerController == null)
			{
				playerController = owner.GetComponentInParent<PlayerController>();
			}
		}
		
		if (playerController == null)
		{
			Debug.LogError("[RainOfFaithHandler] PlayerController를 찾을 수 없습니다!");
			return;
		}
		
		// 플레이어 스프라이트 렌더러 찾기
		if (playerSpriteRenderer == null)
		{
			playerSpriteRenderer = playerController.GetComponentInChildren<SpriteRenderer>();
		}
		
		// SkillSlot 찾기 (쿨타임 시작용)
		skillSlot = GetComponentInParent<SkillSlot>();
		if (skillSlot == null)
		{
			Debug.LogWarning("[RainOfFaithHandler] SkillSlot을 찾을 수 없습니다.");
		}
		
		// 다른 스킬 사용 감지를 위한 이벤트 구독
		SkillUIEvents.OnSkillUsed += OnOtherSkillUsed;
		
		Debug.Log("[RainOfFaithHandler] Setup 완료");
	}
	
	/// <summary>
	/// 스킬 사용
	/// 1단계: 설치 모드 시작
	/// 2단계 (재입력): 폭발 모드 실행
	/// </summary>
	public void Use(SkillRank rank)
	{
		if (playerController == null)
		{
			Debug.LogError("[RainOfFaithHandler] PlayerController가 null입니다!");
			return;
		}
		
		// ⭐ 설치 모드가 활성화되어 있으면 → 폭발 모드 실행 (2단계)
		if (isInSetupMode)
		{
			Debug.Log($"[RainOfFaithHandler] ✨ 재입력 감지! 폭발 모드 시작! 설치된 잔상: {installedAfterimages.Count}개");
			TriggerExplosion();
			return;
		}
		
		// ⭐ 설치 모드가 비활성화되어 있으면 → 설치 모드 시작 (1단계)
		isInSRankMode = (rank == SkillRank.S);
		currentRank = rank;
		
		Debug.Log($"[RainOfFaithHandler] 🔧 설치 모드 시작 (랭크: {rank})");
		
		StartSetupMode(rank);
	}
	
	/// <summary>
	/// 스킬 정리
	/// </summary>
	public void Cleanup()
	{
		// 이벤트 구독 해제
		SkillUIEvents.OnSkillUsed -= OnOtherSkillUsed;
		
		StopSetupMode();
		DestroyAllAfterimages();
	}
	
	#endregion
	
	#region 설치 모드 (1단계)
	
	/// <summary>
	/// 설치 모드 시작
	/// </summary>
	private void StartSetupMode(SkillRank rank)
	{
		// 스킬 시작 효과음 재생
		PlaySkillStartSound();
		
		isInSetupMode = true;
		
		// 이동속도 버프 적용
		int rankIndex = (int)rank;
		float speedMultiplier = moveSpeedMultiplierByRank[Mathf.Clamp(rankIndex, 0, moveSpeedMultiplierByRank.Length - 1)];
		ApplySpeedBuff(speedMultiplier);
		
		// 초기화
		lastPosition = playerController.transform.position;
		timeSinceLastAfterimage = 0f;
		
		// ⭐ 설치 모드 시작 시 0.5초 쿨타임 시작 (빠르게 폭발 가능)
		if (skillSlot != null)
		{
			skillSlot.StartCooldownFromHandler(0.5f);
			Debug.Log($"[RainOfFaithHandler] ⏰ 0.5초 쿨타임 시작! 빠르게 폭발 가능합니다.");
			
			// ⭐ 금색 테두리 활성화 (즉시 재사용 가능 표시)
			SkillUIEvents.InvokeCooldownInstantReset(skillSlot.SlotType);
			Debug.Log($"[RainOfFaithHandler] ⚡ 금색 테두리 활성화!");
			
			// ⭐ 아이콘 변경 (설정되어 있으면)
			if (setupModeIcon != null)
			{
				SkillUIEvents.InvokeSkillIconTemporaryChanged(skillSlot.SlotType, setupModeIcon);
				Debug.Log($"[RainOfFaithHandler] 🎨 설치 모드 아이콘으로 변경!");
			}
		}
		
		Debug.Log($"[RainOfFaithHandler] 🔧 설치 모드 활성화 - 속도 배율: {speedMultiplier}x");
	}
	
	/// <summary>
	/// 설치 모드 종료
	/// </summary>
	private void StopSetupMode()
	{
		if (!isInSetupMode) return;
		
		isInSetupMode = false;
		isInSRankMode = false;
		
		// 이동속도 버프 제거
		RemoveSpeedBuff();
		
		// ⭐ 아이콘 복원
		if (skillSlot != null && setupModeIcon != null)
		{
			SkillUIEvents.InvokeSkillIconRestored(skillSlot.SlotType);
			Debug.Log($"[RainOfFaithHandler] 🎨 원래 아이콘으로 복원!");
		}
		
		Debug.Log("[RainOfFaithHandler] 🔧 설치 모드 종료");
	}
	
	#endregion
	
	#region Update (잔상 생성)
	
	/// <summary>
	/// 매 프레임 업데이트: 설치 모드에서 잔상 생성
	/// </summary>
	private void Update()
	{
		if (!isInSetupMode || playerController == null) return;
		
		// 이동 확인
		Vector3 currentPosition = playerController.transform.position;
		float distanceMoved = Vector3.Distance(currentPosition, lastPosition);
		
		// 잔상 생성 타이머 증가
		timeSinceLastAfterimage += Time.deltaTime;
		
		// 최소 이동 거리 이상 이동 + 일정 시간 경과 시 잔상 생성
		if (distanceMoved >= 0.1f && timeSinceLastAfterimage >= afterimageSpawnInterval)
		{
			InstallAfterimage(currentPosition);
			lastPosition = currentPosition;
			timeSinceLastAfterimage = 0f;
		}
	}
	
	#endregion
	
	#region 폭발 모드 (2단계)
	
	/// <summary>
	/// 폭발 모드 실행: 모든 잔상이 동시에 적을 향해 돌진 공격
	/// </summary>
	private void TriggerExplosion()
	{
		if (installedAfterimages.Count == 0)
		{
			Debug.LogWarning("[RainOfFaithHandler] ⚠️ 설치된 잔상이 없습니다!");
			StopSetupMode();
			
			// 잔상이 없으면 원래 쿨타임 시작
			if (skillSlot != null)
			{
				skillSlot.StartCooldownFromHandler();
				Debug.Log($"[RainOfFaithHandler] ⏰ 잔상 없음 - 정상 쿨타임 시작");
			}
			return;
		}
		
		Debug.Log($"[RainOfFaithHandler] 💥 폭발 시작! 총 {installedAfterimages.Count}개의 잔상이 적을 공격합니다!");
		
		// 설치 모드 종료
		StopSetupMode();
		
		// 연쇄 폭발 코루틴 시작
		StartCoroutine(ExplosionSequence());
		
		// ⭐ 폭발 후 원래 스킬의 쿨타임 시작
		if (skillSlot != null)
		{
			skillSlot.StartCooldownFromHandler();
			Debug.Log($"[RainOfFaithHandler] 💥 폭발 완료! 원래 쿨타임 시작 ({skillData?.cooldown ?? 0}초)");
		}
	}
	
	/// <summary>
	/// 폭발 연쇄 시퀀스
	/// </summary>
	private IEnumerator ExplosionSequence()
	{
		// 모든 잔상 활성화 (동시에, 약간의 딜레이로 연쇄 느낌)
		for (int i = 0; i < installedAfterimages.Count; i++)
		{
			GameObject afterimageObj = installedAfterimages[i];
			if (afterimageObj == null) continue;
			
			AfterimageController afterimage = afterimageObj.GetComponent<AfterimageController>();
			if (afterimage != null)
			{
				// 잔상 활성화 (적 향해 돌진 시작)
				afterimage.ActivateAndAttack(afterimageAttackRange, afterimageRushSpeed);
			}
			
			// 연쇄 딜레이
			if (explosionChainDelay > 0f)
			{
				yield return new WaitForSeconds(explosionChainDelay);
			}
		}
		
		// 잔상 목록 초기화
		installedAfterimages.Clear();
		
		Debug.Log("[RainOfFaithHandler] 💥 모든 잔상 활성화 완료!");
	}
	
	#endregion
	
	#region 다른 스킬 사용 감지
	
	/// <summary>
	/// 다른 스킬이 사용되었을 때 호출
	/// </summary>
	private void OnOtherSkillUsed(SkillSlotType usedSlotType, PlayerSkillData usedSkillData)
	{
		// 설치 모드가 아니면 무시
		if (!isInSetupMode) return;
		
		// 자기 자신의 스킬이면 무시 (재입력 처리는 Use() 메서드에서)
		if (usedSlotType == skillSlot?.SlotType) return;
		
		// 다른 스킬 사용 → 설치 모드 자동 종료 + 모든 잔상 파괴
		Debug.LogWarning($"[RainOfFaithHandler] ⚠️ 다른 스킬({usedSkillData.skillName}) 사용 감지! 설치 모드 자동 종료 + 잔상 모두 제거");
		
		StopSetupMode();
		DestroyAllAfterimages();
		
		// ⭐ 자동 종료 시 원래 쿨타임 시작
		if (skillSlot != null)
		{
			skillSlot.StartCooldownFromHandler();
			Debug.Log($"[RainOfFaithHandler] ⏰ 자동 종료 - 원래 쿨타임 시작");
		}
	}
	
	#endregion
	
	#region 이동속도 버프
	
	/// <summary>
	/// 이동속도 버프 적용
	/// </summary>
	private void ApplySpeedBuff(float multiplier)
	{
		if (playerController.PlayerData != null)
		{
			originalMoveSpeed = playerController.PlayerData.moveSpeed;
			playerController.PlayerData.moveSpeed = originalMoveSpeed * multiplier;
			Debug.Log($"[RainOfFaithHandler] 이동속도 버프 적용: {originalMoveSpeed} → {playerController.PlayerData.moveSpeed}");
		}
	}
	
	/// <summary>
	/// 이동속도 버프 제거
	/// </summary>
	private void RemoveSpeedBuff()
	{
		if (playerController.PlayerData != null && originalMoveSpeed > 0f)
		{
			playerController.PlayerData.moveSpeed = originalMoveSpeed;
			Debug.Log($"[RainOfFaithHandler] 이동속도 버프 제거: {playerController.PlayerData.moveSpeed}");
		}
	}
	
	#endregion
	
	#region 잔상 관리
	
	/// <summary>
	/// 잔상 설치 (비활성 상태로 생성)
	/// </summary>
	private void InstallAfterimage(Vector3 position)
	{
		if (afterimagePrefab == null)
		{
			Debug.LogWarning("[RainOfFaithHandler] 잔상 프리팹이 없습니다!");
			return;
		}
		
		// 최대 개수 체크 (0 = 무제한)
		int rankIndex = (int)currentRank;
		int maxCount = maxAfterimagesByRank[Mathf.Clamp(rankIndex, 0, maxAfterimagesByRank.Length - 1)];
		
		if (maxCount > 0 && installedAfterimages.Count >= maxCount)
		{
			// 최대 개수 도달 → 가장 오래된 잔상 제거
			CleanupDestroyedAfterimages();
			if (installedAfterimages.Count >= maxCount && installedAfterimages.Count > 0)
			{
				GameObject oldest = installedAfterimages[0];
				installedAfterimages.RemoveAt(0);
				if (oldest != null)
				{
					Destroy(oldest);
				}
			}
		}
		
		// 잔상 생성 효과음 재생 (볼륨을 낮게)
		PlayAfterimageSpawnSound();
		
		// 잔상 생성 (비활성 상태)
		GameObject afterimageObj = Instantiate(afterimagePrefab, position, Quaternion.identity);
		AfterimageController afterimage = afterimageObj.GetComponent<AfterimageController>();
		
		if (afterimage != null)
		{
			// 데미지 계산
			float baseDamage = playerController.CalculateSkillDamage(afterimageDamagePercent);
			float damageMultiplier = skillData.GetDamageMultiplier(currentRank);
			float finalDamage = baseDamage * damageMultiplier;
			
			// 플레이어 스프라이트
			Sprite playerSprite = (playerSpriteRenderer != null) ? playerSpriteRenderer.sprite : null;
			
			// 초기화 (비활성 상태로)
			afterimage.SetupAsInactive(finalDamage, isInSRankMode, playerSprite);
			
			installedAfterimages.Add(afterimageObj);
			
			Debug.Log($"[RainOfFaithHandler] 🔧 잔상 설치 완료! 총 {installedAfterimages.Count}개");
		}
	}
	
	/// <summary>
	/// 파괴된 잔상 정리
	/// </summary>
	private void CleanupDestroyedAfterimages()
	{
		installedAfterimages.RemoveAll(a => a == null);
	}
	
	/// <summary>
	/// 모든 잔상 파괴
	/// </summary>
	private void DestroyAllAfterimages()
	{
		foreach (GameObject afterimage in installedAfterimages)
		{
			if (afterimage != null)
			{
				Destroy(afterimage);
			}
		}
		installedAfterimages.Clear();
		
		Debug.Log("[RainOfFaithHandler] 🗑️ 모든 잔상 제거");
	}
	
	#endregion
	
	#region 사운드 재생
	
	/// <summary>
	/// 스킬 시작 효과음 재생
	/// </summary>
	private void PlaySkillStartSound()
	{
		if (skillStartSound != null && playerController != null)
		{
			AudioSource audioSource = playerController.GetComponent<AudioSource>();
			if (audioSource == null)
			{
				audioSource = playerController.gameObject.AddComponent<AudioSource>();
				audioSource.playOnAwake = false;
				audioSource.spatialBlend = 0f; // 2D 사운드
			}
			audioSource.PlayOneShot(skillStartSound);
			Debug.Log("[RainOfFaithHandler] 스킬 시작 효과음 재생");
		}
	}
	
	/// <summary>
	/// 잔상 생성 효과음 재생
	/// </summary>
	private void PlayAfterimageSpawnSound()
	{
		if (afterimageSpawnSound != null && playerController != null)
		{
			AudioSource audioSource = playerController.GetComponent<AudioSource>();
			if (audioSource == null)
			{
				audioSource = playerController.gameObject.AddComponent<AudioSource>();
				audioSource.playOnAwake = false;
				audioSource.spatialBlend = 0f; // 2D 사운드
			}
			// 잔상 생성은 빈번하므로 볼륨을 낮춤
			audioSource.PlayOneShot(afterimageSpawnSound, 0.3f);
		}
	}
	
	#endregion
}

