/*
 * ========================================
 * EntityController.cs
 * ========================================
 * [설명]
 * 게임 내 체력을 가진 모든 개체(Entity)의 기본 컨트롤러입니다.
 * 
 * [주요 기능]
 * - 체력 관리: 데미지, 회복, 사망 처리
 * - 무적 처리: 무적 상태 토글 및 시간 제한 무적
 * - 상태효과 관리: DOT, HoT, 버프/디버프 등
 * - 데이터 연동: ScriptableObject(SO)를 통한 데이터 관리
 * - 물리 컴포넌트: Rigidbody2D, Collider2D 자동 설정
 * 
 * [상속 구조]
 * EntityController (기본)
 * ├── PlayerController (플레이어)
 * ├── InteractableObjectController (상호작용 오브젝트)
 * └── MonsterController (몬스터)
 *     ├── MeleeMonsterController (근접 몬스터)
 *     ├── RangedMonsterController (원거리 몬스터)
 *     └── BossMonsterController (보스 몬스터)
 * 
 * [확장 포인트]
 * - Initialize(): 엔티티별 초기화 로직 추가
 * - ApplyDamage(): 데미지 계산/효과음/이펙트 추가
 * - Die(): 사망 시 연출/아이템 드롭/풀 반환 등
 * - OnDamaged(): 피격 시 추가 처리 (넉백, 이펙트 등)
 * 
 * [참고]
 * - 이벤트 방송은 별도 중계 컴포넌트에서 처리합니다
 * - EasySave3를 활용한 저장은 각 자식 클래스에서 구현합니다
 * ========================================
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class EntityController : MonoBehaviour
{
	#region 컴포넌트 참조

	[Header("엔티티 데이터 (ScriptableObject)")]
	[Tooltip("이 엔티티의 기본 데이터를 담은 SO (자식 클래스에서 자동 설정됨)")]
	protected EntityDataSO entityData;

	[Header("물리 컴포넌트")]
	[Tooltip("2D 물리 연산용 Rigidbody")]
	[SerializeField] protected Rigidbody2D rb;
	[Tooltip("충돌 감지용 Collider")]
	[SerializeField] protected Collider2D col;

	#endregion

	#region 런타임 상태

	[Header("체력 및 생존 상태")]
	[Tooltip("현재 체력 (0이 되면 사망)")]
	[SerializeField] protected int currentHealth;
	[Tooltip("생존 여부")]
	[SerializeField] protected bool isAlive = true;

	[Header("무적 상태")]
	[Tooltip("무적 상태 여부 (true일 때 데미지 무시)")]
	[SerializeField] protected bool isInvincible = false;

	[Header("상태효과")]
	[Tooltip("현재 적용된 상태효과 목록 (디버깅용)")]
	[SerializeField] protected List<ActiveStatusEffect> activeStatusEffects = new List<ActiveStatusEffect>();

	// 무적 타이머용 코루틴 참조
	private Coroutine invincibilityCoroutine;

	#endregion

	#region 프로퍼티 (읽기 전용)

	public event System.Action<int,int> OnHealthChanged;
    public event System.Action<EntityController> OnDied;

	/// <summary>생존 여부</summary>
	public bool IsAlive => isAlive;

	/// <summary>무적 상태 여부</summary>
	public bool IsInvincible => isInvincible;

	/// <summary>현재 체력</summary>
	public int CurrentHealth => currentHealth;

	/// <summary>최대 체력 (EntityDataSO에서 가져옴)</summary>
	public int MaxHealth => entityData != null ? entityData.maxHealth : 0;

	/// <summary>엔티티 타입 (Player, Monster, Environment 등)</summary>
	public EntityType EntityType => entityData != null ? entityData.entityType : EntityType.Environment;

	/// <summary>현재 적용된 상태효과 목록 (읽기 전용)</summary>
	public IReadOnlyList<ActiveStatusEffect> ActiveStatusEffects => activeStatusEffects;

	#endregion

	#region Unity Lifecycle

	/// <summary>
	/// Unity Editor에서 컴포넌트 추가 시 자동 호출
	/// 필수 컴포넌트를 자동으로 설정합니다
	/// </summary>
	protected virtual void Reset()
	{
		rb = GetComponent<Rigidbody2D>();
		col = GetComponent<Collider2D>();

		if (rb != null)
		{
			rb.gravityScale = 0f; // 2D 탑다운 게임이므로 중력 비활성화
			rb.bodyType = RigidbodyType2D.Kinematic; //  Kinematic: 물리 엔진에 영향받지 않음 (밀림 방지)
			rb.constraints = RigidbodyConstraints2D.FreezeRotation; //  회전 고정 (2D에서는 Z축 제약 없음)
		}

		if (col != null)
		{
			col.isTrigger = false; // 기본은 물리 충돌
		}
	}

	/// <summary>
	/// 게임 시작 시 또는 오브젝트 생성 시 호출
	/// 컴포넌트 참조를 확보합니다
	/// </summary>
	protected virtual void Awake()
	{
		// 컴포넌트 자동 할당
		if (rb == null) rb = GetComponent<Rigidbody2D>();
		if (col == null) col = GetComponent<Collider2D>();

		// 물리 설정
		if (rb != null)
		{
			rb.gravityScale = 0f; // 중력 비활성화
			rb.bodyType = RigidbodyType2D.Kinematic; // ⭐ Kinematic: 밀림 방지
			rb.constraints = RigidbodyConstraints2D.FreezeRotation; // ⭐ 회전 고정 (2D에서는 Z축 제약 없음)
		}
	}

	/// <summary>
	/// 오브젝트 활성화 시 호출
	/// 풀링 시스템 사용 시 재사용을 위해 초기화합니다
	/// </summary>
	protected virtual void OnEnable()
	{
		Initialize();
	}

	/// <summary>
	/// 매 프레임 호출
	/// 상태효과 업데이트를 담당합니다
	/// </summary>
	protected virtual void Update()
	{
		UpdateStatusEffects(Time.deltaTime);
	}

	#endregion

	#region 초기화

	/// <summary>
	/// 엔티티 초기화
	/// 체력, 무적, 상태효과를 초기 상태로 리셋합니다
	/// </summary>
	public virtual void Initialize()
	{
		// EntityDataSO 유효성 검사
		if (entityData == null)
		{
			Debug.LogWarning($"[EntityController] {name}: EntityDataSO가 설정되지 않았습니다. 초기화를 건너뜁니다.");
			isAlive = false;
			return;
		}

		// 체력 초기화
		currentHealth = Mathf.Max(0, entityData.maxHealth);
		isAlive = currentHealth > 0;

		// 상태 초기화
		isInvincible = false;

		// 무적 코루틴 정리
		if (invincibilityCoroutine != null)
		{
			StopCoroutine(invincibilityCoroutine);
			invincibilityCoroutine = null;
		}

		// 상태효과 초기화
		activeStatusEffects.Clear();
	}

	/// <summary>
	/// 엔티티 데이터 SO 설정 (런타임에서 변경 가능)
	/// </summary>
	public virtual void SetEntityData(EntityDataSO data)
	{
		entityData = data;
		Initialize();
	}

	#endregion

	#region 체력 관리

	/// <summary>
	/// 데미지 적용
	/// 무적 상태이거나 이미 사망한 경우 무시됩니다
	/// </summary>
	/// <param name="damageAmount">받을 데미지 양 (양수)</param>
	/// <param name="attackerType">공격자의 엔티티 타입 (팀킬 방지용)</param>
	/// <param name="damageSource">데미지 발생원 (옵션, 로그/통계용)</param>
	/// <returns>데미지가 실제로 적용되었는지 여부 (true: 적용됨, false: 무시됨)</returns>
	public virtual bool ApplyDamage(int damageAmount, EntityType attackerType, object damageSource = null)
	{
		// 데미지 무시 조건
		if (!isAlive) return false; // 이미 사망
		if (isInvincible) return false; // 무적 상태
		if (damageAmount <= 0) return false; // 0 이하 데미지

		// 팀킬 방지: 같은 타입끼리는 데미지 무시
		if (EntityType == attackerType)
		{
			// 플레이어가 플레이어 스킬에 맞지 않음
			// 몬스터가 몬스터 스킬에 맞지 않음
			return false;
		}

		// 체력 감소
		currentHealth = Mathf.Max(0, currentHealth - damageAmount);

		// 체력 변경 이벤트 발생
		OnHealthChanged?.Invoke(currentHealth, MaxHealth);

		// 피격 처리 (자식 클래스에서 오버라이드 가능)
		OnDamaged(damageAmount, damageSource);

		// 사망 확인
		if (currentHealth <= 0)
		{
			Debug.Log($"[EntityController] {name} 체력이 0 이하로 Die() 호출");
			
			Die();
		}

		return true; // 데미지 적용 성공
	}

	/// <summary>
	/// 데미지 적용 (공격자 타입 없이 호출 - 환경 데미지 등)
	/// </summary>
	/// <param name="damageAmount">받을 데미지 양</param>
	/// <param name="damageSource">데미지 발생원</param>
	/// <returns>데미지가 실제로 적용되었는지 여부</returns>
	public virtual bool ApplyDamage(int damageAmount, object damageSource = null)
	{
		// 환경 데미지는 EntityType.Environment로 처리 (모두에게 적용)
		return ApplyDamage(damageAmount, EntityType.Environment, damageSource);
	}

	/// <summary>
	/// 회복 처리
	/// 최대 체력을 초과할 수 없습니다
	/// </summary>
	/// <param name="healAmount">회복량 (양수)</param>
	public virtual void Heal(int healAmount)
	{
		if (!isAlive) return; // 사망 상태에서는 회복 불가
		if (healAmount <= 0) return; // 0 이하 회복량 무시

		int previousHealth = currentHealth;
		currentHealth = Mathf.Min(MaxHealth, currentHealth + healAmount);

		// 실제 회복된 양 계산
		int actualHealed = currentHealth - previousHealth;

		// 체력 변경 이벤트 발생
		OnHealthChanged?.Invoke(currentHealth, MaxHealth);

		// 회복 처리 (자식 클래스에서 오버라이드 가능)
		OnHealed(actualHealed);
	}

	/// <summary>
	/// 체력 변경 이벤트를 자식 클래스에서 안전하게 호출하기 위한 헬퍼
	/// </summary>
	protected void NotifyHealthChanged()
	{
		OnHealthChanged?.Invoke(currentHealth, MaxHealth);
	}

	/// <summary>
	/// 사망 처리
	/// 생존 상태를 false로 변경하고 물리 정지합니다
	/// </summary>
	protected virtual void Die()
	{
		if (!isAlive) return; // 이미 사망한 경우 중복 실행 방지

		isAlive = false;

		// 물리 정지
		if (rb != null)
		{
			rb.linearVelocity = Vector2.zero;
		}

		// 자식 클래스에서 추가 처리:
		// - 사망 애니메이션
		// - 아이템 드롭
		// - 오브젝트 풀 반환
		// - 방송 이벤트 전송
	}

	/// <summary>
	/// 피격 시 호출 (오버라이드용)
	/// 넉백, 효과음, 이펙트 등을 추가할 수 있습니다
	/// </summary>
	protected virtual void OnDamaged(int damageAmount, object damageSource)
	{
		// 자식 클래스에서 구현:
		// - 피격 효과음
		// - 피격 이펙트
		// - 넉백 처리
		// - 무적 시간 부여
	}

	/// <summary>
	/// 회복 시 호출 (오버라이드용)
	/// 회복 이펙트, 효과음 등을 추가할 수 있습니다
	/// </summary>
	protected virtual void OnHealed(int healedAmount)
	{
		// 자식 클래스에서 구현:
		// - 회복 효과음
		// - 회복 이펙트
	}

	#endregion

	#region 무적 처리

	/// <summary>
	/// 무적 상태 설정 (즉시 전환)
	/// </summary>
	/// <param name="value">무적 여부</param>
	public virtual void SetInvincible(bool value)
	{
		if (isInvincible == value) return; // 이미 같은 상태면 무시

		isInvincible = value;

		// 무적이 해제되면 타이머 코루틴 정리
		if (!value && invincibilityCoroutine != null)
		{
			StopCoroutine(invincibilityCoroutine);
			invincibilityCoroutine = null;
		}
	}

	/// <summary>
	/// 시간 제한 무적 설정
	/// 지정된 시간 동안만 무적 상태가 유지됩니다
	/// </summary>
	/// <param name="duration">무적 지속 시간 (초)</param>
	public virtual void SetInvincibleForDuration(float duration)
	{
		if (duration <= 0f)
		{
			SetInvincible(false);
			return;
		}

		// 기존 무적 타이머가 있으면 중지
		if (invincibilityCoroutine != null)
		{
			StopCoroutine(invincibilityCoroutine);
		}

		invincibilityCoroutine = StartCoroutine(InvincibilityTimer(duration));
	}

	/// <summary>
	/// 무적 타이머 코루틴
	/// </summary>
	private IEnumerator InvincibilityTimer(float duration)
	{
		isInvincible = true;
		yield return new WaitForSeconds(duration);
		isInvincible = false;
		invincibilityCoroutine = null;
	}

	#endregion

	#region 상태효과 관리

	/// <summary>
	/// 상태효과 적용
	/// 둔화, 기절 등의 효과를 추가합니다
	/// </summary>
	/// <param name="effect">적용할 상태효과 SO</param>
	public virtual void ApplyStatusEffect(StatusEffectDataSO effect)
	{
		if (effect == null)
		{
			Debug.LogWarning($"[EntityController] {name}: null 상태효과를 적용하려 했습니다.");
			return;
		}

		// 동일한 상태효과가 있는지 확인 (중복 처리)
		ActiveStatusEffect existingEffect = activeStatusEffects.Find(e => e.Effect == effect);

		if (existingEffect != null)
		{
			// 기존 효과 갱신 (시간 리셋 또는 스택 증가)
			existingEffect.RemainingTime = Mathf.Max(existingEffect.RemainingTime, effect.duration);
		}
		else
		{
			// 새로운 효과 추가
			ActiveStatusEffect newEffect = new ActiveStatusEffect
			{
				Effect = effect,
				RemainingTime = Mathf.Max(0f, effect.duration),
				NextTickTime = effect.tickInterval, // 틱 타이머 초기화
				StackCount = 1
			};
			activeStatusEffects.Add(newEffect);

			// 효과 시작 시 처리
			OnStatusEffectApplied(effect);
		}
	}

	/// <summary>
	/// 상태효과 제거
	/// </summary>
	/// <param name="effect">제거할 상태효과</param>
	public virtual void RemoveStatusEffect(StatusEffectDataSO effect)
	{
		if (effect == null) return;

		int removedCount = activeStatusEffects.RemoveAll(e => e.Effect == effect);

		if (removedCount > 0)
		{
			OnStatusEffectRemoved(effect);
		}
	}

	/// <summary>
	/// 모든 상태효과 제거
	/// </summary>
	public virtual void ClearAllStatusEffects()
	{
		foreach (var activeEffect in activeStatusEffects)
		{
			OnStatusEffectRemoved(activeEffect.Effect);
		}

		activeStatusEffects.Clear();
	}

	/// <summary>
	/// 상태효과 업데이트 (매 프레임 호출)
	/// 지속시간 감소 및 Tick 처리를 담당합니다
	/// </summary>
	protected virtual void UpdateStatusEffects(float deltaTime)
	{
		if (activeStatusEffects.Count == 0) return;

		// 역순 순회 (삭제를 위해)
		for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
		{
			ActiveStatusEffect activeEffect = activeStatusEffects[i];

			// Tick 처리 (DOT, HoT 등)
			ProcessStatusEffectTick(activeEffect, deltaTime);

			// 지속시간 감소
			activeEffect.RemainingTime -= deltaTime;

			// 지속시간 종료 확인
			if (activeEffect.RemainingTime <= 0f)
			{
				OnStatusEffectExpired(activeEffect.Effect);
				activeStatusEffects.RemoveAt(i);
			}
		}
	}

	/// <summary>
	/// 상태효과 Tick 처리 (DOT, HoT 등)
	/// 기본 DOT/HOT 처리를 수행하며, 자식 클래스에서 추가 효과를 구현할 수 있습니다
	/// </summary>
	protected virtual void ProcessStatusEffectTick(ActiveStatusEffect activeEffect, float deltaTime)
	{
		if (activeEffect == null || activeEffect.Effect == null) return;

		StatusEffectDataSO effect = activeEffect.Effect;

		// DOT (Damage Over Time) 또는 HOT (Heal Over Time) 처리
		if (effect.effectType == StatusEffectType.Damage || effect.effectType == StatusEffectType.Heal)
		{
			// 틱 타이머 감소
			activeEffect.NextTickTime -= deltaTime;

			// 틱 실행
			if (activeEffect.NextTickTime <= 0f)
			{
				if (effect.effectType == StatusEffectType.Damage)
				{
					// 틱 데미지 적용 (무적 무시)
					int tickDamage = Mathf.RoundToInt(effect.tickValue);
					if (tickDamage > 0)
					{
						currentHealth = Mathf.Max(0, currentHealth - tickDamage);
						OnDamaged(tickDamage, effect); // 피격 처리
						
						// 사망 확인
						if (currentHealth <= 0 && isAlive)
						{
							Die();
						}
					}
				}
				else if (effect.effectType == StatusEffectType.Heal)
				{
					// 틱 힐 적용
					int tickHeal = Mathf.RoundToInt(effect.tickValue);
					if (tickHeal > 0)
					{
						Heal(tickHeal);
					}
				}

				// 다음 틱 시간 리셋
				activeEffect.NextTickTime = effect.tickInterval;
			}
		}

		// 이동속도 변경은 GetMoveSpeed() 같은 메서드에서 계산
		// 행동 제약은 CanMove(), CanAttack() 같은 메서드에서 체크
		// 자식 클래스에서 추가 구현 가능
	}

	/// <summary>
	/// 상태효과 적용 시 호출 (오버라이드용)
	/// </summary>
	protected virtual void OnStatusEffectApplied(StatusEffectDataSO effect)
	{
		// 자식 클래스에서 구현:
		// - 효과 적용 이펙트
		// - 효과음
		// - UI 업데이트
	}

	/// <summary>
	/// 상태효과 제거 시 호출 (오버라이드용)
	/// </summary>
	protected virtual void OnStatusEffectRemoved(StatusEffectDataSO effect)
	{
		// 자식 클래스에서 구현:
		// - 효과 제거 이펙트
		// - UI 업데이트
	}

	/// <summary>
	/// 상태효과 만료 시 호출 (오버라이드용)
	/// </summary>
	protected virtual void OnStatusEffectExpired(StatusEffectDataSO effect)
	{
		// 자식 클래스에서 구현:
		// - 만료 이펙트
	}

	/// <summary>
	/// 이동 가능 여부 체크 (상태효과 고려)
	/// </summary>
	public virtual bool CanMove()
	{
		if (!isAlive) return false;

		// 이동 제약 상태효과 체크
		foreach (var activeEffect in activeStatusEffects)
		{
			if (activeEffect.Effect != null && activeEffect.Effect.preventMovement)
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// 공격 가능 여부 체크 (상태효과 고려)
	/// </summary>
	public virtual bool CanAttack()
	{
		if (!isAlive) return false;

		// 공격 제약 상태효과 체크
		foreach (var activeEffect in activeStatusEffects)
		{
			if (activeEffect.Effect != null && activeEffect.Effect.preventAttack)
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// 스킬 사용 가능 여부 체크 (상태효과 고려)
	/// </summary>
	public virtual bool CanUseSkill()
	{
		if (!isAlive) return false;

		// 스킬 제약 상태효과 체크
		foreach (var activeEffect in activeStatusEffects)
		{
			if (activeEffect.Effect != null && activeEffect.Effect.preventSkill)
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// 현재 이동속도 배율 계산 (상태효과 고려)
	/// </summary>
	public virtual float GetMoveSpeedMultiplier()
	{
		float multiplier = 1f;

		// 모든 상태효과의 속도 배율을 곱셈
		foreach (var activeEffect in activeStatusEffects)
		{
			if (activeEffect.Effect != null)
			{
				multiplier *= activeEffect.Effect.moveSpeedMultiplier;
			}
		}

		return multiplier;
	}

	/// <summary>
	/// 특정 상태효과가 적용 중인지 확인
	/// </summary>
	public virtual bool HasStatusEffect(StatusEffectDataSO effect)
	{
		if (effect == null) return false;
		return activeStatusEffects.Exists(e => e.Effect == effect);
	}

	/// <summary>
	/// 특정 타입의 상태효과가 적용 중인지 확인
	/// </summary>
	public virtual bool HasStatusEffectType(StatusEffectType effectType)
	{
		return activeStatusEffects.Exists(e => e.Effect != null && e.Effect.effectType == effectType);
	}

	#endregion

	#region 충돌 처리 훅

	/// <summary>
	/// 물리 충돌 시작 시 호출 (오버라이드용)
	/// </summary>
	protected virtual void OnCollisionEnter2D(Collision2D collision)
	{
		// 자식 클래스에서 구현:
		// - 충돌 데미지 처리
		// - 충돌 효과음/이펙트
	}

	/// <summary>
	/// 트리거 충돌 시작 시 호출 (오버라이드용)
	/// </summary>
	protected virtual void OnTriggerEnter2D(Collider2D other)
	{
		// 자식 클래스에서 구현:
		// - 투사체 충돌 처리
		// - 아이템 습득
		// - 지역 효과 진입
	}

	#endregion

	#region 디버그 및 유틸리티

	/// <summary>
	/// 엔티티 정보를 문자열로 반환 (디버깅용)
	/// </summary>
	public virtual string GetDebugInfo()
	{
		return $"[{name}] 체력: {currentHealth}/{MaxHealth}, 생존: {isAlive}, 무적: {isInvincible}, 상태효과: {activeStatusEffects.Count}개";
	}

	#endregion
}


/// <summary>
/// 적용 중인 상태효과를 나타내는 클래스
/// 런타임에 활성화된 상태효과의 상태를 추적합니다
/// </summary>
[Serializable]
public class ActiveStatusEffect
{
	[Tooltip("상태효과 ScriptableObject")]
	public StatusEffectDataSO Effect;

	[Tooltip("남은 지속 시간 (초)")]
	public float RemainingTime;
	
	[Tooltip("다음 틱까지 남은 시간 (DOT/HOT용)")]
	public float NextTickTime;
	
	[Tooltip("현재 스택 수 (스택 가능한 효과용)")]
	public int StackCount = 1;
}