using UnityEngine;

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
	#endregion

    

	#region Events - 외부 알림
	public System.Action<BossController> OnBossDied;
	#endregion

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

			// 즉시 페이즈 스킬 발동 (발동 중 무적은 트리거 옵션으로 제어)
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
	}

	/// <summary>
	/// 사망 처리: 이벤트 브로드캐스트 및 선택적 파괴
	/// </summary>
	protected override void Die()
	{
		base.Die();

		Debug.Log($"[BossController] {gameObject.name} 사망");
		OnBossDied?.Invoke(this);

		if (destroyOnDeath)
		{
			Destroy(gameObject, Mathf.Max(0f, destroyDelay));
		}
	}
	#endregion

    
}


