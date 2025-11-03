using UnityEngine;

/// <summary>
/// BossController - Skill 파트
/// 1) 3개 내외의 스킬 슬롯을 순서대로 반복 사용
/// 2) HP 임계치 하락 시 특정 스킬 1회 발동 + 발동안 무적
/// 3) MonsterController와 동일한 핸들러/프로젝트일 방식 사용
/// </summary>
public partial class BossController : EntityController
{
	#region Inspector - Skill Slots
	[Header("보스 스킬 슬롯 (순차 사용)")]
	[Tooltip("보스가 순차적으로 사용할 스킬 슬롯 목록입니다. 인덱스 순서대로 반복 실행됩니다.")]
	[SerializeField] private BossSkillSlot[] skillSlots;

	[Header("스킬 시스템 토글")]
	[Tooltip("보스 스킬 시스템 활성/비활성")]
	[SerializeField] private bool enableSkills = true;

	[Header("타겟 설정")]
	[Tooltip("보스가 기술을 사용할 대상. 비어있다면 autoFindPlayer가 true일 때 플레이어를 자동 설정합니다.")]
	[SerializeField] private Transform target;
	[Tooltip("시작 시 PlayerController.Instance를 자동으로 타겟으로 설정")]
	[SerializeField] private bool autoFindPlayer = true;

	[Header("스킬 전역 간격")]
	[Tooltip("어떤 스킬이 끝난 후 다음 스킬을 쓰기까지 최소 대기 시간(초)")]
	[SerializeField] private float globalSkillGap = 0.5f;

	[Header("디버그")]
	[Tooltip("스킬 종료 시 어떤 슬롯/스킬이 끝났는지 로그 출력")]
	[SerializeField] private bool debugSkillLogs = false;

	[Header("페이즈(특수 스킬) 트리거")]
	[Tooltip("HP 임계치 별로 1회 발동되는 페이즈 스킬 트리거 목록 (예: 0.8, 0.5, 0.2)")]
	[SerializeField] private BossPhaseTrigger[] phaseTriggers;
	#endregion

	#region Runtime
    private float[] slotCooldownTimers;
    private int nextSlotIndex = 0;
	private bool isSkillActive = false;
	private bool[] phaseFired;
    private float nextAnySkillAllowedAt = 0f;
    private int[] slotRepeatRemaining; // 슬롯별 남은 반복 횟수
	#endregion

	#region Unity Lifecycle
	private void Start()
	{
		FindPlayerForBoss();
		InitializeBossSkills();
	}
	#endregion

	#region Initialization
	private void FindPlayerForBoss()
	{
		if (!autoFindPlayer || target != null) return;
		if (PlayerController.Instance != null)
		{
			target = PlayerController.Instance.transform;
		}
	}

	private void InitializeBossSkills()
	{
        if (skillSlots != null && skillSlots.Length > 0)
        {
            slotCooldownTimers = new float[skillSlots.Length];
            slotRepeatRemaining = new int[skillSlots.Length];
            for (int i = 0; i < slotCooldownTimers.Length; i++)
            {
                slotCooldownTimers[i] = 0f;
                slotRepeatRemaining[i] = 0;
            }
        }

		if (phaseTriggers != null && phaseTriggers.Length > 0)
		{
			phaseFired = new bool[phaseTriggers.Length];
			for (int i = 0; i < phaseFired.Length; i++)
			{
				phaseFired[i] = false;
			}
		}
	}
	#endregion

	#region Main Loop
	/// <summary>
/// BossController.Update에서 호출됨
/// </summary>
	private void ProcessBossSkills()
	{
		if (!enableSkills) return;
		if (!isAlive) return;
		if (isSkillActive) return;
		if (Time.time < nextAnySkillAllowedAt) return;
		if (skillSlots == null || skillSlots.Length == 0) return;

		// 페이즈 스킬 체크 (여러 임계치 지원, 각 1회 발동)
		if (phaseTriggers != null && phaseTriggers.Length > 0 && phaseFired != null)
		{
			float hpPercent = MaxHealth > 0 ? (float)currentHealth / MaxHealth : 0f;
			int selectedIndex = -1;
			float selectedThreshold = float.MaxValue;

			for (int i = 0; i < phaseTriggers.Length; i++)
			{
				var trig = phaseTriggers[i];
				if (phaseFired[i]) continue;
				if (trig == null || !trig.IsValid()) continue;
				if (hpPercent <= trig.triggerHealthPercent)
				{
					// 가장 가까운 상한(작은 임계치 우선) 선택
					if (trig.triggerHealthPercent < selectedThreshold)
					{
						selectedThreshold = trig.triggerHealthPercent;
						selectedIndex = i;
					}
				}
			}

			if (selectedIndex != -1)
			{
				UsePhaseSkill(selectedIndex);
				return;
			}
		}

		// 쿨타임 업데이트
		for (int i = 0; i < slotCooldownTimers.Length; i++)
		{
			slotCooldownTimers[i] -= Time.deltaTime;
		}

		// 순차 사용: 다음 슬롯 확인
		int tries = skillSlots.Length;
		while (tries-- > 0)
		{
			int index = nextSlotIndex;
			var slot = skillSlots[index];
            if (slot.skill != null && slotCooldownTimers[index] <= 0f && IsInRange(slot))
			{
				// 반복 남은 횟수가 없으면 [min,max]에서 랜덤으로 셋업
				int minUses = Mathf.Max(1, slot.minRepeatUses);
				int maxUses = Mathf.Max(minUses, slot.maxRepeatUses);
				if (slotRepeatRemaining[index] <= 0)
				{
					slotRepeatRemaining[index] = Random.Range(minUses, maxUses + 1); // inclusive
					if (debugSkillLogs)
					{
						string n = slot.skill != null ? (!string.IsNullOrEmpty(slot.skill.skillName) ? slot.skill.skillName : slot.skill.name) : "<null>";
						Debug.Log($"[BossController] Slot {index} roll repeats={slotRepeatRemaining[index]} for {n}");
					}
				}

				UseSlotSkill(index, slot);
				return;
			}

			nextSlotIndex = (nextSlotIndex + 1) % skillSlots.Length;
		}
	}
	#endregion

	#region Skill Use
	private bool IsInRange(BossSkillSlot slot)
	{
		if (target == null) return true; // 타겟이 없으면 범위 체크 생략
		float distance = Vector2.Distance(transform.position, target.position);
		return distance >= slot.minRange && distance <= slot.maxRange;
	}

	private void UseSlotSkill(int index, BossSkillSlot slot)
	{
		isSkillActive = true;

		if (slot.lockMovementDuringSkill)
		{
			LockMovement(slot.movementLockDuration);
		}

        if (slot.skill != null && slot.skill.handlerPrefab != null)
        {
            UseSkillWithHandler(slot.skill, onEnd: () => { if (debugSkillLogs) { string n = slot.skill != null ? (!string.IsNullOrEmpty(slot.skill.skillName) ? slot.skill.skillName : slot.skill.name) : "<null>"; Debug.Log($"[BossController] Slot {index} ended: {n}"); } OnSlotSkillUseEnd(index); });
        }
        else if (slot.skill != null && slot.skill.skillEffectPrefab != null)
        {
            UseSkillWithProjectile(slot, onEnd: () => { if (debugSkillLogs) { string n = slot.skill != null ? (!string.IsNullOrEmpty(slot.skill.skillName) ? slot.skill.skillName : slot.skill.name) : "<null>"; Debug.Log($"[BossController] Slot {index} ended: {n}"); } OnSlotSkillUseEnd(index); });
        }
		else
		{
			Debug.LogWarning($"[BossController] {gameObject.name} 슬롯 스킬 프리팹이 비어있습니다.");
			isSkillActive = false;
		}
	}

    private void OnSlotSkillUseEnd(int index)
    {
        if (skillSlots == null || skillSlots.Length == 0) return;
        if (index < 0) index = 0;

        // 반복 소모
        if (slotRepeatRemaining != null && index < slotRepeatRemaining.Length)
        {
            slotRepeatRemaining[index] = Mathf.Max(0, slotRepeatRemaining[index] - 1);
            if (debugSkillLogs)
            {
                string n = skillSlots[index]?.skill != null ? (!string.IsNullOrEmpty(skillSlots[index].skill.skillName) ? skillSlots[index].skill.skillName : skillSlots[index].skill.name) : "<null>";
                Debug.Log($"[BossController] Slot {index} finished once. repeats left={slotRepeatRemaining[index]} for {n}");
            }

            if (slotRepeatRemaining[index] > 0)
            {
                // 반복 중에는 쿨타임 적용하지 않음 (전역 간격만 적용)
                slotCooldownTimers[index] = 0f;
                nextSlotIndex = index; // 같은 슬롯 재시도
                return;
            }
        }

        // 반복 없음 또는 모두 소진: 쿨타임 적용 후 다음 슬롯으로
        if (index < slotCooldownTimers.Length)
        {
            var s = skillSlots[index];
            slotCooldownTimers[index] = s != null && s.skill != null ? s.skill.cooldown : 0f;
        }
        nextSlotIndex = (index + 1) % skillSlots.Length;
    }

	private void UsePhaseSkill(int index)
	{
		if (phaseTriggers == null || index < 0 || index >= phaseTriggers.Length) return;
		var phaseTrigger = phaseTriggers[index];
		if (phaseTrigger == null || !phaseTrigger.IsValid()) return;

		if (phaseFired != null && index >= 0 && index < phaseFired.Length)
		{
			phaseFired[index] = true;
		}
		isSkillActive = true;

		if (phaseTrigger.lockMovementDuringSkill)
		{
			LockMovement(phaseTrigger.movementLockDuration);
		}

		if (phaseTrigger.grantInvincibility)
		{
			SetInvincible(true);
		}

		var skill = phaseTrigger.phaseSkill;
        if (skill.handlerPrefab != null)
		{
			UseSkillWithHandler(skill, onEnd: () => { if (phaseTrigger.grantInvincibility) SetInvincible(false); if (debugSkillLogs) { string n = !string.IsNullOrEmpty(skill.skillName) ? skill.skillName : skill.name; Debug.Log($"[BossController] Phase skill ended: {n}"); } });
		}
		else if (skill.skillEffectPrefab != null)
		{
			UseSkillWithProjectile(phaseTrigger, onEnd: () => { if (phaseTrigger.grantInvincibility) SetInvincible(false); if (debugSkillLogs) { string n = !string.IsNullOrEmpty(skill.skillName) ? skill.skillName : skill.name; Debug.Log($"[BossController] Phase skill ended: {n}"); } });
		}
		else
		{
			Debug.LogWarning($"[BossController] 페이즈 스킬 프리팹이 비어있습니다.");
			if (phaseTrigger.grantInvincibility) SetInvincible(false);
			isSkillActive = false;
		}
	}

	private void UseSkillWithHandler(MonsterSkillData skill, System.Action onEnd = null)
	{
		GameObject handlerObj = Instantiate(skill.handlerPrefab, transform);
		SkillHandlerInterface handler = handlerObj.GetComponent<SkillHandlerInterface>();
		if (handler != null)
		{
			handler.Setup(skill, gameObject);
			handler.Use(SkillRank.D);
			StartCoroutine(WaitForSkillEnd(handlerObj, onEnd));
		}
		else
		{
			Debug.LogError($"[BossController] {skill.handlerPrefab.name}에 SkillHandlerInterface가 없습니다!");
			Destroy(handlerObj);
			isSkillActive = false;
			onEnd?.Invoke();
		}
	}

	private void UseSkillWithProjectile(BossSkillSlot slot, System.Action onEnd = null)
	{
		var skill = slot.skill;
		GameObject projObj = Instantiate(skill.skillEffectPrefab, transform.position, Quaternion.identity);
		ISkillProjectile projScript = projObj.GetComponent<ISkillProjectile>();
		if (projScript != null)
		{
			projScript.SetSkillData(skill.damage, skill.projectileSpeed);
			projScript.SetAttackerType(EntityType.Monster);
			projScript.SetPiercing(slot.isPiercing);
			StartCoroutine(WaitForSkillEnd(projObj, onEnd));

			Rigidbody2D rb = projObj.GetComponent<Rigidbody2D>();
			if (rb != null && target != null)
			{
				Vector2 dir = (target.position - transform.position).normalized;
				rb.linearVelocity = dir * skill.projectileSpeed;
			}
		}
		else
		{
			Debug.LogWarning($"[BossController] {skill.skillEffectPrefab.name}에 ISkillProjectile이 없습니다!");
			Destroy(projObj);
			isSkillActive = false;
			onEnd?.Invoke();
		}
	}

	private void UseSkillWithProjectile(BossPhaseTrigger phase, System.Action onEnd = null)
	{
		var skill = phase.phaseSkill;
		GameObject projObj = Instantiate(skill.skillEffectPrefab, transform.position, Quaternion.identity);
		ISkillProjectile projScript = projObj.GetComponent<ISkillProjectile>();
		if (projScript != null)
		{
			projScript.SetSkillData(skill.damage, skill.projectileSpeed);
			projScript.SetAttackerType(EntityType.Monster);
			projScript.SetPiercing(phase.isPiercing);
			StartCoroutine(WaitForSkillEnd(projObj, onEnd));

			Rigidbody2D rb = projObj.GetComponent<Rigidbody2D>();
			if (rb != null && target != null)
			{
				Vector2 dir = (target.position - transform.position).normalized;
				rb.linearVelocity = dir * skill.projectileSpeed;
			}
		}
		else
		{
			Debug.LogWarning($"[BossController] {skill.skillEffectPrefab.name}에 ISkillProjectile이 없습니다!");
			Destroy(projObj);
			isSkillActive = false;
			onEnd?.Invoke();
		}
	}

	private System.Collections.IEnumerator WaitForSkillEnd(GameObject obj, System.Action onEnd = null)
	{
		while (obj != null)
		{
			yield return null;
		}
		isSkillActive = false;
		nextAnySkillAllowedAt = Time.time + Mathf.Max(0f, globalSkillGap);
		onEnd?.Invoke();
		Debug.Log($"[BossController] {gameObject.name} 스킬 종료 - 다음 스킬 사용 가능");
	}

    // EndSkill 제거됨: 반복 종료 시 OnSlotSkillUseEnd에서 직접 nextSlotIndex 갱신
	#endregion

	#region Movement hooks (optional)
	private void LockMovement(float duration)
	{
		// Boss 이동 제어가 도입되면 여기서 잠금 적용
		// 현재는 별도 이동 시스템이 없어 비워둠
	}
	#endregion
}

[System.Serializable]
public class BossSkillSlot
{
	[Header("스킬 데이터")]
	[Tooltip("실행할 스킬 데이터 (핸들러 프리팹 또는 투사체 프리팹)")]
	public MonsterSkillData skill;

	[Header("사정거리")]
	[Tooltip("이 스킬이 발동 가능한 최소 거리")]
	public float minRange = 0f;
	[Tooltip("이 스킬이 발동 가능한 최대 거리")]
	public float maxRange = 999f;

	[Header("스킬 중 이동 잠금")]
	[Tooltip("스킬 시전/실행 동안 이동을 잠급니다")]
	public bool lockMovementDuringSkill = false;
	[Tooltip("이동 잠금 지속 시간(초). 0이면 스킬 내부에서 자동 해제 시점까지")]
	public float movementLockDuration = 0f;

	[Header("투사체 설정")]
	[Tooltip("투사체 사용 시 관통 여부 (핸들러 방식은 내부 구현에 따름)")]
	public bool isPiercing = false;

	[Header("반복 사용")]
	[Tooltip("이 슬롯을 연속으로 사용할 최소 횟수 (1 이상)")]
	public int minRepeatUses = 1;
	[Tooltip("이 슬롯을 연속으로 사용할 최대 횟수 (min 이상)")]
	public int maxRepeatUses = 1;
}

[System.Serializable]
public class BossPhaseTrigger
{
	[Header("발동 HP 비율 (0~1)")]
	[Tooltip("보스 HP가 이 비율 이하로 떨어지면 발동 (0~1)")]
	[Range(0f, 1f)] public float triggerHealthPercent = 0.5f;

	[Header("특수 스킬 (1회 발동)")]
	[Tooltip("해당 페이즈에서 1회 발동할 스킬")]
	public MonsterSkillData phaseSkill;

	[Header("발동 중 무적")]
	[Tooltip("페이즈 스킬 실행 중 보스를 무적으로 설정")]
	public bool grantInvincibility = true;

	[Header("스킬 중 이동 잠금")]
	[Tooltip("페이즈 스킬 실행 중 이동 잠금")]
	public bool lockMovementDuringSkill = true;
	[Tooltip("페이즈 이동 잠금 지속 시간(초)")]
	public float movementLockDuration = 0f;

	[Header("투사체 설정")]
	[Tooltip("페이즈 스킬이 투사체를 사용할 경우 관통 여부")]
	public bool isPiercing = false;

	public bool IsValid()
	{
		return phaseSkill != null;
	}
}


