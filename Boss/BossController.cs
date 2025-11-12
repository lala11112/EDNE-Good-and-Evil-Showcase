using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using PixelCrushers.DialogueSystem;


/// <summary>
/// 보스 컨트롤러 - EntityController 기반으로 보스의 체력/사망을 관리합니다.
/// EntityDataSO를 사용해 MonsterController와 동일한 방식으로 초기화합니다.
/// </summary>
public partial class BossController : EntityController
{
	#region Inspector Fields - 데이터 및 설정
	[Header("보스 데이터 (EntityDataSO)")]
	[SerializeField] private EntityDataSO bossData;

	[Header("사망 처리 설정")]
	[SerializeField] private bool destroyOnDeath = true;
	[SerializeField] private float destroyDelay = 2f;
	
	[Header("다이얼로그 및 클리어 씬 설정")]
	[SerializeField, Tooltip("보스 사망 시 재생할 다이얼로그 제목 (비워두면 다이얼로그 없이 바로 씬 전환)")] 
	private string bossDeathDialogue = "";
	[SerializeField, Tooltip("보스 사망 후 로드할 씬 이름 (비워두면 로드하지 않음)")] 
	private string gameClearSceneName = "GameClear";
	[SerializeField, Tooltip("다이얼로그 없이 씬 전환할 경우의 지연 시간(초)")] 
	private float gameClearLoadDelay = 2f;
	#endregion

	#region Spawn Sequence
	[Header("스폰 연출")]
	[SerializeField, Tooltip("스폰 애니메이션이 재생되는 기간(초). 이 시간 동안 무적/공격불가")]
	private float spawnAnimationDuration = 2f;
	#endregion


    
 
	#region Events - 외부 알림
	public System.Action<BossController> OnBossDied;
	public System.Action<BossController> OnSpawnSequenceEnded;
	#endregion

	/// <summary>
	/// 다이얼로그 종료 대기 중인지 여부
	/// </summary>
	private bool isWaitingForDialogueEnd = false;

	#region Unity Lifecycle
	protected override void Awake()
	{
		base.Awake();

		// EntityController가 참조하는 데이터 설정
		if (bossData != null)
		{
			entityData = bossData;
		}

		// MonsterController와 유사하게 Awake 시점에 1차 초기화
		Initialize();
	}

	private void OnDestroy()
	{
		// 다이얼로그 이벤트 구독 해제 (메모리 누수 방지)
		if (DialogueManager.instance != null && isWaitingForDialogueEnd)
		{
			DialogueManager.instance.conversationEnded -= OnDeathDialogueEnded;
		}
	}

	protected override void Update()
	{
		// 상태효과 등 기본 갱신은 부모가 처리
		base.Update();

        // 스킬 처리: Skill partial의 ProcessBossSkills 사용
        ProcessBossSkills();
	}
	#endregion

	#region Initialization
	/// <summary>
	/// 런타임에 보스 데이터 설정 및 재초기화
	/// </summary>
	public void SetBossData(EntityDataSO data)
	{
		bossData = data;
		entityData = data;
		Initialize();
	}
	#endregion

	#region Spawn
	/// <summary>
	/// 트리거로 처음 소환되었을 때 호출하여 스폰 연출을 재생하고,
	/// 연출 종료 전까지 무적/공격불가로 유지합니다.
	/// </summary>
	/// <param name="overrideDuration">지정 시 해당 시간으로 덮어씀</param>
	public void StartSpawnSequence(float? overrideDuration = null)
	{
		float dur = Mathf.Max(0f, overrideDuration ?? spawnAnimationDuration);

		// 무적 및 스킬 비활성
		SetInvincible(true);
		enableSkills = false;

		// 스폰 애니메이션 트리거
		var simpleDriver = GetComponentInChildren<MonsterAnimatorSimple>();
		if (simpleDriver != null)
		{
			simpleDriver.TriggerSpawn();
		}
		else
		{
			Debug.LogWarning($"[BossController] {gameObject.name} Spawn requested but MonsterAnimatorSimple not found");
		}

		// 일정 시간 후 해제
		StartCoroutine(CoEndSpawnSequence(dur));
	}

	private IEnumerator CoEndSpawnSequence(float duration)
	{
		if (duration > 0f) yield return new WaitForSeconds(duration);

		// 스폰 연출 종료: 무적 해제 및 스킬 활성화
		SetInvincible(false);
		enableSkills = true;

		// HP UI 등 외부 시스템이 이 시점에 활성화될 수 있도록 알림
		OnSpawnSequenceEnded?.Invoke(this);
		Debug.Log($"[BossController] {gameObject.name} 스폰 연출 종료 - 외부 알림 전송");
	}
	#endregion

	#region EntityController Overrides
	/// <summary>
	/// 데미지 적용 시 페이즈 임계치에 도달하면 체력을 임계치에서 멈추고 페이즈 스킬을 발동합니다.
	/// </summary>
	public override bool ApplyDamage(int damageAmount, EntityType attackerType, object damageSource = null)
	{
		// 기본 무시 조건
		if (!isAlive) return false;
		if (isInvincible) return false;
		if (damageAmount <= 0) return false;
		if (EntityType == attackerType) return false;

		// 페이즈 게이트 계산: 이번 타격으로 아직 발동하지 않은 임계치를 하회하게 되는지 검사
		int selectedIndex = -1;
		int selectedThresholdHealth = int.MinValue;

		if (MaxHealth > 0 && phaseTriggers != null && phaseTriggers.Length > 0 && phaseFired != null)
		{
			int current = currentHealth;
			int projected = Mathf.Max(0, currentHealth - damageAmount);

			for (int i = 0; i < phaseTriggers.Length; i++)
			{
				var trig = phaseTriggers[i];
				if (trig == null || !trig.IsValid()) continue;
				if (i >= phaseFired.Length || phaseFired[i]) continue;

				int thresholdHealth = Mathf.CeilToInt(MaxHealth * trig.triggerHealthPercent);
				// 현재 > 임계치, 적용 후 <= 임계치이면 해당 임계치에 걸림
				if (current > thresholdHealth && projected <= thresholdHealth)
				{
					// 가장 가까운(큰) 임계치를 선택
					if (thresholdHealth > selectedThresholdHealth)
					{
						selectedThresholdHealth = thresholdHealth;
						selectedIndex = i;
					}
				}
			}
		}

		if (selectedIndex != -1)
		{
			int previousHealth = currentHealth;
			currentHealth = selectedThresholdHealth; // 체력을 임계치에서 멈춤


			// 체력 변경 이벤트
			NotifyHealthChanged();

			// 실제 적용된 데미지 알림
			int actualDamage = previousHealth - currentHealth;
			if (actualDamage > 0)
			{
				OnDamaged(actualDamage, damageSource);
			}

            // 즉시 페이즈 스킬 발동 전에 반복 셋업 (Skill 파트의 러untime 필드 사용)
            if (phaseTriggers != null && selectedIndex >= 0 && selectedIndex < phaseTriggers.Length)
            {
                int minUses = Mathf.Max(1, phaseTriggers[selectedIndex].minRepeatUses);
                int maxUses = Mathf.Max(minUses, phaseTriggers[selectedIndex].maxRepeatUses);
                if (phaseRepeatRemaining != null && (phaseRepeatRemaining.Length <= selectedIndex || phaseRepeatRemaining[selectedIndex] <= 0))
                {
                    // 배열 길이는 초기화 시 생성됨. 방어적으로 길이 체크 후 셋업
                    if (phaseRepeatRemaining != null && selectedIndex < phaseRepeatRemaining.Length)
                    {
                        phaseRepeatRemaining[selectedIndex] = Random.Range(minUses, maxUses + 1);
                    }
                }
                // 반복 루프 시작 인덱스 설정
                if (currentRepeatingPhaseIndex == -1)
                {
                    currentRepeatingPhaseIndex = selectedIndex;
                }
            }

            // 페이즈 스킬 발동 (무적/스케줄은 Skill 파트에서 처리)
            UsePhaseSkill(selectedIndex);
			return true;
		}

		// 페이즈 게이트에 걸리지 않으면 기본 처리
		return base.ApplyDamage(damageAmount, attackerType, damageSource);
	}

	/// <summary>
	/// 공격자 타입 없이 들어오는 데미지(환경 등)도 동일한 게이트 로직 적용
	/// </summary>
	public override bool ApplyDamage(int damageAmount, object damageSource = null)
	{
		return ApplyDamage(damageAmount, EntityType.Environment, damageSource);
	}

	/// <summary>
	/// 피격 처리 (효과음/연출은 추후 확장)
	/// </summary>
	protected override void OnDamaged(int damageAmount, object damageSource)
	{
		base.OnDamaged(damageAmount, damageSource);
		Debug.Log($"[BossController] {gameObject.name}가 {damageAmount} 데미지를 받음 (HP: {currentHealth}/{MaxHealth})");

		// 애니메이션: 피격 트리거 즉시 실행 (MonsterAnimatorSimple 사용)
		var simpleDriver = GetComponentInChildren<MonsterAnimatorSimple>();
		if (simpleDriver != null)
		{
			simpleDriver.TriggerHit();
		}

	}

	/// <summary>
	/// 사망 처리: 이벤트 브로드캐스트 및 선택적 파괴
	/// 다이얼로그가 설정되어 있으면 다이얼로그를 재생하고, 종료 후 클리어 씬으로 전환합니다.
	/// </summary>
	protected override void Die()
	{
		base.Die();

		Debug.Log($"[BossController] {gameObject.name} 사망");

		// 진행 중인 스킬/연출 즉시 중지
		CancelAllSkillsImmediately();

		// 애니메이션: 사망 트리거 실행 (MonsterAnimatorSimple만 사용)
		var simpleDriver = GetComponentInChildren<MonsterAnimatorSimple>(true);
		if (simpleDriver == null)
		{
			Debug.LogWarning($"[BossController] {gameObject.name} 사망 트리거 요청 실패 - MonsterAnimatorSimple을 찾을 수 없음 (비활성 포함 검색)");
		}
		if (simpleDriver != null)
		{
			Debug.Log($"[BossController] {gameObject.name} 사망 트리거 시도 (MonsterAnimatorSimple)");
			simpleDriver.TriggerDeath();
		}
		OnBossDied?.Invoke(this);

		// 보스 오브젝트 파괴 처리
		if (destroyOnDeath)
		{
			Destroy(gameObject, Mathf.Max(0f, destroyDelay));
		}

		// 다이얼로그 설정이 있으면 다이얼로그 재생 후 씬 전환
		// 없으면 바로 씬 전환
		if (!string.IsNullOrEmpty(bossDeathDialogue))
		{
			StartDeathDialogue();
		}
		else
		{
			// 다이얼로그 없이 게임 클리어 씬 로드
			LoadGameClearScene();
		}
	}

	/// <summary>
	/// 보스 사망 시 다이얼로그를 시작합니다.
	/// </summary>
	private void StartDeathDialogue()
	{
		if (DialogueManager.instance == null)
		{
			Debug.LogWarning($"[BossController] DialogueManager를 찾을 수 없습니다! 다이얼로그 없이 씬을 전환합니다.");
			LoadGameClearScene();
			return;
		}

		// 다이얼로그 종료 이벤트 구독
		isWaitingForDialogueEnd = true;
		DialogueManager.instance.conversationEnded += OnDeathDialogueEnded;

		// 다이얼로그 시작
		DialogueManager.StartConversation(bossDeathDialogue);
		Debug.Log($"[BossController] 보스 사망 다이얼로그 시작: '{bossDeathDialogue}'");
	}

	/// <summary>
	/// 보스 사망 다이얼로그가 종료되었을 때 호출되는 콜백
	/// </summary>
	private void OnDeathDialogueEnded(Transform actor)
	{
		// 현재 종료된 대화가 보스 사망 다이얼로그인지 확인
		string endedConversation = DialogueManager.lastConversationEnded;
		
		if (endedConversation == bossDeathDialogue)
		{
			Debug.Log($"[BossController] 보스 사망 다이얼로그 종료: '{endedConversation}' - 클리어 씬 전환");
			
			// 이벤트 구독 해제
			if (DialogueManager.instance != null)
			{
				DialogueManager.instance.conversationEnded -= OnDeathDialogueEnded;
			}
			isWaitingForDialogueEnd = false;

			// 클리어 씬으로 전환
			LoadGameClearScene();
		}
	}

	/// <summary>
	/// 게임 클리어 씬을 로드합니다.
	/// </summary>
	private void LoadGameClearScene()
	{
		if (!string.IsNullOrEmpty(gameClearSceneName))
		{
			// 다이얼로그가 있었으면 지연 없이 즉시 전환, 없었으면 설정된 지연 시간 사용
			float delay = string.IsNullOrEmpty(bossDeathDialogue) ? gameClearLoadDelay : 0f;
			StartCoroutine(CoLoadSceneAfterDelay(gameClearSceneName, Mathf.Max(0f, delay)));
			Debug.Log($"[BossController] 게임 클리어 씬 로드 예약: '{gameClearSceneName}' ({delay}초 후)");
		}
	}
	#endregion

	#region Scene Load
	private IEnumerator CoLoadSceneAfterDelay(string sceneName, float delaySeconds)
	{
		if (delaySeconds > 0f) yield return new WaitForSeconds(delaySeconds);
		SceneManager.LoadScene(sceneName);
	}
	#endregion
    
 
}


