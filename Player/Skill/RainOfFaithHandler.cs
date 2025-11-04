using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 빗발치는 믿음 스킬 핸들러
/// 이동 경로에 잔상을 남겨 적을 공격합니다
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
	
	#endregion
	
	#region 이동속도 버프
	
	[Header("이동속도 버프")]
	[Tooltip("랭크별 스킬 지속 시간 (초)")]
	[SerializeField] private float[] durationByRank = new float[] { 3f, 3.5f, 4f, 4.5f, 5f };
	
	[Tooltip("랭크별 이동속도 증가 배율")]
	[SerializeField] private float[] moveSpeedMultiplierByRank = new float[] { 1.3f, 1.35f, 1.4f, 1.5f, 1.6f };
	
	private bool isActive = false;
	private float remainingDuration = 0f;
	private float originalMoveSpeed = 0f;
	private SkillRank currentRank;
	
	#endregion
	
	#region 잔상 생성
	
	[Header("잔상 설정")]
	[Tooltip("잔상 프리팹")]
	[SerializeField] private GameObject afterimagePrefab;
	
	[Tooltip("잔상 생성 간격 (초)")]
	[SerializeField] private float afterimageSpawnInterval = 0.2f;
	
	[Tooltip("잔상 기본 데미지 퍼센트")]
	[SerializeField] private float afterimageDamagePercent = 50f;
	
	[Tooltip("랭크별 잔상 지속 시간 (초)")]
	[SerializeField] private float[] afterimageLifetimeByRank = new float[] { 5f, 8f, 12f, 16f, 20f };
	
	[Tooltip("랭크별 최대 잔상 개수")]
	[SerializeField] private int[] maxAfterimagesByRank = new int[] { 10, 15, 20, 25, 30 };
	
	private List<GameObject> activeAfterimages = new List<GameObject>();
	private float timeSinceLastAfterimage = 0f;
	
	#endregion
	
	#region 정지 감지
	
	[Header("정지 감지")]
	[Tooltip("정지 판정 시간 (초)")]
	[SerializeField] private float stopThreshold = 0.3f;
	
	[Tooltip("정지 판정 최소 이동 거리")]
	[SerializeField] private float minimumMovementDistance = 0.1f;
	
	private Vector3 lastPosition;
	private float timeSinceMovement = 0f;
	
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
	
	[Header("사운드 이펙트")]
	[Tooltip("스킬 시작 효과음")]
	[SerializeField] private AudioClip skillStartSound;
	
	[Tooltip("잔상 생성 효과음")]
	[SerializeField] private AudioClip afterimageSpawnSound;
	
	#endregion
	
	#region 코루틴 참조
	
	private Coroutine skillCoroutine;
	
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
		if (activeAfterimages == null)
		{
			activeAfterimages = new List<GameObject>();
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
		
		Debug.Log("[RainOfFaithHandler] Setup 완료");
	}
	
	/// <summary>
	/// 스킬 사용
	/// </summary>
	public void Use(SkillRank rank)
	{
		if (playerController == null)
		{
			Debug.LogError("[RainOfFaithHandler] PlayerController가 null입니다!");
			return;
		}
		
		// 이미 활성화 중이면 무시
		if (isActive)
		{
			Debug.Log("[RainOfFaithHandler] 스킬이 이미 활성화 중입니다!");
			return;
		}
		
		// S랭크 모드 확인
		isInSRankMode = (rank == SkillRank.S);
		currentRank = rank;
		
		Debug.Log($"[RainOfFaithHandler] 빗발치는 믿음 사용 (랭크: {rank})");
		
		// 스킬 시작
		StartSkill(rank);
	}
	
	/// <summary>
	/// 스킬 정리
	/// </summary>
	public void Cleanup()
	{
		StopSkill();
		DestroyAllAfterimages();
	}
	
	#endregion
	
	#region 스킬 활성화/비활성화
	
	/// <summary>
	/// 스킬 시작
	/// </summary>
	private void StartSkill(SkillRank rank)
	{
		// 스킬 시작 효과음 재생
		PlaySkillStartSound();
		
		isActive = true;
		
		// 지속 시간 설정
		int rankIndex = (int)rank;
		
		// 배열 안전성 체크
		if (durationByRank == null || durationByRank.Length == 0)
		{
			Debug.LogError("[RainOfFaithHandler] durationByRank 배열이 비어있습니다!");
			isActive = false;
			return;
		}
		
		remainingDuration = durationByRank[Mathf.Clamp(rankIndex, 0, durationByRank.Length - 1)];
		
		// 이동속도 버프 적용
		float speedMultiplier = moveSpeedMultiplierByRank[Mathf.Clamp(rankIndex, 0, moveSpeedMultiplierByRank.Length - 1)];
		ApplySpeedBuff(speedMultiplier);
		
		// 초기화
		lastPosition = playerController.transform.position;
		timeSinceMovement = 0f;
		timeSinceLastAfterimage = 0f;
		
		// 코루틴 시작
		if (skillCoroutine != null)
		{
			StopCoroutine(skillCoroutine);
		}
		skillCoroutine = StartCoroutine(SkillCoroutine());
		
		Debug.Log($"[RainOfFaithHandler] 스킬 시작 - 지속시간: {remainingDuration}초, 속도 배율: {speedMultiplier}x");
	}
	
	/// <summary>
	/// 스킬 종료
	/// </summary>
	private void StopSkill()
	{
		if (!isActive) return;
		
		isActive = false;
		isInSRankMode = false;
		
		// 이동속도 버프 제거
		RemoveSpeedBuff();
		
		// 코루틴 정지
		if (skillCoroutine != null)
		{
			StopCoroutine(skillCoroutine);
			skillCoroutine = null;
		}
		
		Debug.Log("[RainOfFaithHandler] 스킬 종료");
	}
	
	#endregion
	
	#region 스킬 코루틴
	
	/// <summary>
	/// 스킬 메인 루프
	/// </summary>
	private IEnumerator SkillCoroutine()
	{
		while (remainingDuration > 0f && isActive)
		{
			remainingDuration -= Time.deltaTime;
			
			// 이동 확인
			Vector3 currentPosition = playerController.transform.position;
			float distanceMoved = Vector3.Distance(currentPosition, lastPosition);
			
			// ⭐ 잔상 생성 타이머는 항상 증가
			timeSinceLastAfterimage += Time.deltaTime;
			
			if (distanceMoved >= minimumMovementDistance)
			{
				// 이동 중
				timeSinceMovement = 0f;
				lastPosition = currentPosition;
				
				// 잔상 생성 (이동 중일 때만)
				if (timeSinceLastAfterimage >= afterimageSpawnInterval)
				{
					SpawnAfterimage(currentPosition);
					timeSinceLastAfterimage = 0f;
				}
			}
			else
			{
				// 정지 중
				timeSinceMovement += Time.deltaTime;
				
				// 정지 시간 초과 시 스킬 종료
				if (timeSinceMovement >= stopThreshold)
				{
					Debug.Log("[RainOfFaithHandler] 플레이어가 멈춰서 스킬 조기 종료");
					break;
				}
			}
			
			yield return null;
		}
		
		// 스킬 종료
		StopSkill();
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
	/// 잔상 생성
	/// </summary>
	private void SpawnAfterimage(Vector3 position)
	{
		if (afterimagePrefab == null)
		{
			Debug.LogWarning("[RainOfFaithHandler] 잔상 프리팹이 없습니다!");
			return;
		}
		
		// 최대 개수 체크
		int rankIndex = (int)currentRank;
		int maxCount = maxAfterimagesByRank[Mathf.Clamp(rankIndex, 0, maxAfterimagesByRank.Length - 1)];
		
		// 오래된 잔상 제거
		CleanupDestroyedAfterimages();
		while (activeAfterimages.Count >= maxCount)
		{
			if (activeAfterimages.Count > 0)
			{
				GameObject oldest = activeAfterimages[0];
				activeAfterimages.RemoveAt(0);
				if (oldest != null)
				{
					Destroy(oldest);
				}
			}
			else
			{
				break;
			}
		}
		
		// 잔상 생성 효과음 재생 (볼륨을 낮게)
		PlayAfterimageSpawnSound();
		
		// 잔상 생성
		GameObject afterimageObj = Instantiate(afterimagePrefab, position, Quaternion.identity);
		AfterimageController afterimage = afterimageObj.GetComponent<AfterimageController>();
		
		if (afterimage != null)
		{
			// 데미지 계산
			float baseDamage = playerController.CalculateSkillDamage(afterimageDamagePercent);
			float damageMultiplier = skillData.GetDamageMultiplier(currentRank);
			float finalDamage = baseDamage * damageMultiplier;
			
			// 잔상 지속 시간
			float lifetime = afterimageLifetimeByRank[Mathf.Clamp(rankIndex, 0, afterimageLifetimeByRank.Length - 1)];
			
			// 플레이어 스프라이트
			Sprite playerSprite = (playerSpriteRenderer != null) ? playerSpriteRenderer.sprite : null;
			
			// 초기화
			afterimage.Setup(finalDamage, lifetime, isInSRankMode, playerSprite);
			
			activeAfterimages.Add(afterimageObj);
		}
	}
	
	/// <summary>
	/// 파괴된 잔상 정리
	/// </summary>
	private void CleanupDestroyedAfterimages()
	{
		activeAfterimages.RemoveAll(a => a == null);
	}
	
	/// <summary>
	/// 모든 잔상 파괴
	/// </summary>
	private void DestroyAllAfterimages()
	{
		foreach (GameObject afterimage in activeAfterimages)
		{
			if (afterimage != null)
			{
				Destroy(afterimage);
			}
		}
		activeAfterimages.Clear();
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

