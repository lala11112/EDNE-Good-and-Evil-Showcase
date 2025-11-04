using UnityEngine;

/// <summary>
/// 데미지 소스 정보
/// 데미지를 입힌 원인에 대한 상세 정보를 담습니다.
/// 용기 시스템 등에서 어떤 스킬로 데미지를 입혔는지 추적하는데 사용됩니다.
/// </summary>
public class DamageSourceInfo
{
    /// <summary>
    /// 데미지를 입힌 스킬 슬롯 타입 (스킬 데미지인 경우)
    /// </summary>
    public SkillSlotType? skillSlotType;

    /// <summary>
    /// 데미지를 입힌 스킬 핸들러 인스턴스 (원본 damageSource)
    /// </summary>
    public object originalSource;

    /// <summary>
    /// 데미지 소스 타입
    /// </summary>
    public DamageSourceType sourceType;

    /// <summary>
    /// 데미지 소스 정보 생성 (스킬 데미지)
    /// </summary>
    public static DamageSourceInfo FromSkill(SkillSlotType slotType, object handlerInstance)
    {
        return new DamageSourceInfo
        {
            skillSlotType = slotType,
            originalSource = handlerInstance,
            sourceType = DamageSourceType.Skill
        };
    }

    /// <summary>
    /// 데미지 소스 정보 생성 (환경/기타 데미지)
    /// </summary>
    public static DamageSourceInfo FromOther(object source, DamageSourceType type = DamageSourceType.Environment)
    {
        return new DamageSourceInfo
        {
            skillSlotType = null,
            originalSource = source,
            sourceType = type
        };
    }

    /// <summary>
    /// 기존 damageSource에서 DamageSourceInfo로 변환
    /// </summary>
    public static DamageSourceInfo FromDamageSource(object damageSource)
    {
        // 이미 DamageSourceInfo면 그대로 반환
        if (damageSource is DamageSourceInfo info)
        {
            return info;
        }

        if (damageSource is MonoBehaviour monoSource)
        {
            // 1. 플레이어 스킬 핸들러인지 확인 (SkillSlot의 자식)
            SkillSlot slot = monoSource.GetComponentInParent<SkillSlot>();
            if (slot != null)
            {
                return FromSkill(slot.SlotType, damageSource);
            }

            // 2. 몬스터 투사체인지 확인 (ISkillProjectile 구현)
            if (monoSource is ISkillProjectile)
            {
                // 몬스터의 투사체는 Projectile 타입
                return FromOther(damageSource, DamageSourceType.Projectile);
            }

            // 3. 몬스터 접촉 데미지인지 확인 (MonsterController)
            if (monoSource is MonsterController)
            {
                return FromOther(damageSource, DamageSourceType.Contact);
            }

            // 4. 트랩/환경 오브젝트 확인 (TrapBase 등)
            // TrapBase를 상속받은 경우 자동으로 환경 데미지로 처리
            if (monoSource is TrapBase)
            {
                return FromOther(damageSource, DamageSourceType.Environment);
            }
        }

        // 그 외의 경우 일반 소스로 처리 (기본값: Environment)
        return FromOther(damageSource, DamageSourceType.Environment);
    }
}

/// <summary>
/// 데미지 소스 타입
/// </summary>
public enum DamageSourceType
{
    Skill,          // 스킬 데미지
    Environment,    // 환경 데미지 (트랩 등)
    Contact,        // 접촉 데미지
    Projectile      // 탄막 데미지 (몬스터의 탄막 등)
}

