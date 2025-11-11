using UnityEngine;
using System.Linq;

/// <summary>
/// MonsterController - Skill 파트
/// 몬스터 스킬 시스템 (플레이어와 동일한 핸들러 프리팹 방식)
/// </summary>
public partial class MonsterController : EntityController
{
    #region Skill System Fields - 스킬 필드
    
    [Header("스킬 설정")]
    [Tooltip("사용할 스킬들의 우선순위 설정")]
    [SerializeField] private SkillPriority[] skillPriorities;
    
    [Tooltip("스킬 시스템 활성화 여부")]
    [SerializeField] private bool enableSkills = true;
    
    private float[] skillCooldownTimers;
    private bool isSkillActive = false; // 스킬 사용 중이면 새 스킬 사용 불가
    
    #endregion
    
    #region Skill Initialization - 스킬 초기화
    
    /// <summary>
    /// 스킬 시스템 초기화 (Start에서 호출)
    /// </summary>
    private void InitializeSkills()
    {
        if (skillPriorities != null && skillPriorities.Length > 0)
        {
            skillCooldownTimers = new float[skillPriorities.Length];
            
            // 모든 쿨타임 초기화
            for (int i = 0; i < skillCooldownTimers.Length; i++)
            {
                skillCooldownTimers[i] = 0f;
            }
            
            Debug.Log($"[MonsterController] {gameObject.name} 스킬 시스템 초기화 (스킬 수: {skillPriorities.Length})");
        }
    }
    
    /// <summary>
    /// 스킬 정리 (OnDestroy에서 호출)
    /// </summary>
    private void CleanupSkills()
    {
        // 필요 시 스킬 관련 정리
    }
    
    #endregion
    
    #region Skill Processing - 스킬 처리
    
    /// <summary>
    /// 메인 스킬 처리 (Update에서 호출)
    /// </summary>
    private void ProcessSkills()
    {
        if (!enableSkills || skillPriorities == null || skillPriorities.Length == 0) return;
        if (target == null) return;
        if (isSkillActive) return; // ⭐ 스킬 사용 중이면 새로운 스킬 사용 불가
        
        if (!IsTargetInRange()) return;
        
        float distance = GetDistanceToTarget();
        
        // ⭐ 스킬 사정거리 체크 및 이동 제어
        if (IsAnySkillInRange(distance))
        {
            DisableMovement(); // NavMesh 정지 (스킬 사정거리 안)
        }
        else
        {
            EnableMovement(); // NavMesh 재개 (스킬 사정거리 밖)
        }
        
        // 스킬 업데이트
        UpdateSkills(distance);
    }
    
    /// <summary>
    /// 현재 거리에서 사용 가능한 스킬이 있는지 확인
    /// </summary>
    private bool IsAnySkillInRange(float distance)
    {
        if (skillPriorities == null || skillPriorities.Length == 0)
            return false;
        
        for (int i = 0; i < skillPriorities.Length; i++)
        {
            if (skillPriorities[i].skill != null &&
                skillCooldownTimers[i] <= 0 &&
                distance >= skillPriorities[i].minRange &&
                distance <= skillPriorities[i].maxRange)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 스킬 업데이트 및 사용
    /// </summary>
    private void UpdateSkills(float distanceToTarget)
    {
        // 모든 스킬의 쿨타임 감소
        for (int i = 0; i < skillPriorities.Length; i++)
        {
            skillCooldownTimers[i] -= Time.deltaTime;
        }
        
        // 사용 가능한 스킬들을 우선순위 순으로 정렬
        var availableSkills = skillPriorities
            .Where((skillPriority, index) =>
                skillPriority.skill != null &&
                skillCooldownTimers[System.Array.IndexOf(skillPriorities, skillPriority)] <= 0 &&
                distanceToTarget >= skillPriority.minRange &&
                distanceToTarget <= skillPriority.maxRange)
            .OrderBy(skillPriority => skillPriority.priority)
            .ToArray();
        
        // 가장 높은 우선순위 스킬 사용
        if (availableSkills.Length > 0)
        {
            var selectedSkill = availableSkills[0];
            int skillIndex = System.Array.IndexOf(skillPriorities, selectedSkill);
            
            UseSkill(skillIndex, selectedSkill);
            skillCooldownTimers[skillIndex] = selectedSkill.skill.cooldown;
            
            Debug.Log($"[MonsterController] {gameObject.name}가 {selectedSkill.skill.skillName} 사용! " +
                      $"(우선순위: {selectedSkill.priority}, 거리: {distanceToTarget:F1})");
        }
    }
    
    /// <summary>
    /// 스킬 사용 (핸들러 프리팹 방식)
    /// </summary>
    private void UseSkill(int index, SkillPriority skillPriority)
    {
        MonsterSkillData skill = skillPriority.skill;
        
        if (skill == null)
        {
            Debug.LogError($"[MonsterController] {gameObject.name}의 스킬 데이터가 null입니다!");
            return;
        }
        
        // ⭐ 스킬 사용 중 플래그 설정
        isSkillActive = true;

        // ⭐ 애니메이션: 공격 트리거 발동
        TriggerAttackAnimation();
        
        // ⭐ 방법 1: 핸들러 프리팹 사용 (추천)
        if (skill.handlerPrefab != null)
        {
            UseSkillWithHandler(skill, skillPriority);
        }
        // ⭐ 방법 2: 직접 이펙트 프리팹 사용 (기존 방식, 하위 호환)
        else if (skill.skillEffectPrefab != null)
        {
            UseSkillWithProjectile(skill, skillPriority);
        }
        else
        {
            Debug.LogWarning($"[MonsterController] {skill.skillName}에 핸들러 프리팹이나 이펙트 프리팹이 설정되지 않았습니다!");
            isSkillActive = false;
        }
    }
    
    /// <summary>
    /// 핸들러 프리팹을 사용한 스킬 실행 (플레이어와 동일)
    /// </summary>
    private void UseSkillWithHandler(MonsterSkillData skill, SkillPriority skillPriority)
    {
        // ⭐ 이동 잠금 설정 (Movement.cs의 메서드 사용)
        if (skillPriority.lockMovementDuringSkill)
        {
            LockMovement(skillPriority.movementLockDuration);
        }
        
        // 핸들러 프리팹 생성
        GameObject handlerObj = Instantiate(skill.handlerPrefab, transform);
        SkillHandlerInterface handler = handlerObj.GetComponent<SkillHandlerInterface>();
        
        if (handler != null)
        {
            // 핸들러 Setup 및 실행
            handler.Setup(skill, gameObject);
            handler.Use(SkillRank.D); // 몬스터는 기본 랭크 사용
            
            // ⭐ 핸들러가 투사체를 생성하는 경우를 대비하여, 
            // 핸들러에서 생성된 투사체에 관통 여부를 설정하도록 핸들러에 isPiercing 전달 필요
            // (현재는 핸들러 내부에서 직접 처리하므로 추후 확장 시 고려)
            
            // 핸들러는 일회용이므로 스킬 종료 후 제거
            StartCoroutine(WaitForSkillEnd(handlerObj));
        }
        else
        {
            Debug.LogError($"[MonsterController] {skill.handlerPrefab.name}에 SkillHandlerInterface가 없습니다!");
            Destroy(handlerObj);
            isSkillActive = false;
        }
    }
    
    /// <summary>
    /// 직접 투사체 프리팹을 사용한 스킬 실행 (기존 방식, 하위 호환)
    /// </summary>
    private void UseSkillWithProjectile(MonsterSkillData skill, SkillPriority skillPriority)
    {
        if (target == null) return;
        
        // ⭐ 이동 잠금 설정 (Movement.cs의 메서드 사용)
        if (skillPriority.lockMovementDuringSkill)
        {
            LockMovement(skillPriority.movementLockDuration);
        }
        
        GameObject projObj = Instantiate(skill.skillEffectPrefab, transform.position, Quaternion.identity);
        
        ISkillProjectile projScript = projObj.GetComponent<ISkillProjectile>();
        
        if (projScript != null)
        {
            // ⭐ 몬스터는 직접 데미지 값 사용
            float finalDamage = skill.damage;
            
            // 스킬 데이터 설정
            projScript.SetSkillData(finalDamage, skill.projectileSpeed);
            
            // 공격자 타입 설정 (팀킬 방지)
            projScript.SetAttackerType(EntityType.Monster);
            
            // ⭐ 관통 여부 설정 (SkillPriority에서 가져옴)
            projScript.SetPiercing(skillPriority.isPiercing);
            
            // 스킬이 파괴될 때까지 대기
            StartCoroutine(WaitForSkillEnd(projObj));
            
            // Rigidbody2D가 있으면 발사
            Rigidbody2D rb = projObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 dir = (target.position - transform.position).normalized;
                rb.linearVelocity = dir * skill.projectileSpeed;
            }
        }
        else
        {
            Debug.LogWarning($"[MonsterController] {skill.skillEffectPrefab.name}에 ISkillProjectile이 없습니다!");
            Destroy(projObj);
            isSkillActive = false;
        }
    }
    
    /// <summary>
    /// 스킬 오브젝트가 파괴될 때까지 대기
    /// </summary>
    private System.Collections.IEnumerator WaitForSkillEnd(GameObject skillObj)
    {
        // ⭐ 스킬 오브젝트가 파괴될 때까지 대기
        while (skillObj != null)
        {
            yield return null;
        }
        
        // ⭐ 스킬 종료 - 플래그 해제 및 이동 재개
        isSkillActive = false;
        
        // 스킬이 끝나면 이동 재개하지 않음 (ProcessSkills에서 거리 체크 후 자동 재개)
        // EnableMovement(); // 주석 처리 - 자동으로 거리에 따라 제어됨
        
        Debug.Log($"[MonsterController] {gameObject.name} 스킬 종료 - 다음 스킬 사용 가능");
    }
    
    #endregion
    
    private void TriggerAttackAnimation()
    {
		Debug.Log($"[MonsterController] {gameObject.name} Attack animation requested");
        // 우선 Simple 드라이버가 있으면 사용
        var simpleDriver = GetComponentInChildren<MonsterAnimatorSimple>();
        if (simpleDriver != null)
        {
            simpleDriver.TriggerAttack();
			Debug.Log($"[MonsterController] {gameObject.name} Attack via MonsterAnimatorSimple");
            return;
        }

        // 그 외 기존 드라이버
        var driver = GetComponentInChildren<MonsterAnimatorDriver>();
        if (driver != null)
        {
            Vector2? dir = null;
            if (target != null)
            {
                Vector2 toTarget = (Vector2)(target.position - transform.position);
                if (toTarget.sqrMagnitude > 0.0001f) dir = toTarget.normalized;
            }
            driver.TriggerAttack(dir);
			Debug.Log($"[MonsterController] {gameObject.name} Attack via MonsterAnimatorDriver (dir={(dir.HasValue ? dir.Value.ToString() : "null")})");
			return;
        }

		Debug.LogWarning($"[MonsterController] {gameObject.name} Attack requested but no animator driver found");
    }
    
    #region Skill Control - 스킬 제어 (외부 호출용)
    
    /// <summary>
    /// 스킬 시스템 활성화
    /// </summary>
    public void EnableSkills()
    {
        enableSkills = true;
        Debug.Log($"[MonsterController] {gameObject.name} 스킬 활성화");
    }
    
    /// <summary>
    /// 스킬 시스템 비활성화
    /// </summary>
    public void DisableSkills()
    {
        enableSkills = false;
        Debug.Log($"[MonsterController] {gameObject.name} 스킬 비활성화");
    }
    
    /// <summary>
    /// 스킬 시스템 토글
    /// </summary>
    public void ToggleSkills()
    {
        enableSkills = !enableSkills;
        Debug.Log($"[MonsterController] {gameObject.name} 스킬 토글: {enableSkills}");
    }
    
    /// <summary>
    /// 스킬 활성화 상태 확인
    /// </summary>
    public bool IsSkillsEnabled()
    {
        return enableSkills;
    }
    
    /// <summary>
    /// 특정 스킬의 쿨타임 초기화
    /// </summary>
    public void ResetSkillCooldown(int skillIndex)
    {
        if (skillCooldownTimers != null && skillIndex >= 0 && skillIndex < skillCooldownTimers.Length)
        {
            skillCooldownTimers[skillIndex] = 0f;
            Debug.Log($"[MonsterController] {gameObject.name}의 스킬 [{skillIndex}] 쿨타임 초기화");
        }
    }
    
    /// <summary>
    /// 모든 스킬의 쿨타임 초기화
    /// </summary>
    public void ResetAllSkillCooldowns()
    {
        if (skillCooldownTimers != null)
        {
            for (int i = 0; i < skillCooldownTimers.Length; i++)
            {
                skillCooldownTimers[i] = 0f;
            }
            Debug.Log($"[MonsterController] {gameObject.name}의 모든 스킬 쿨타임 초기화");
        }
    }
    
    #endregion
}

