using UnityEngine;

/// <summary>
/// 데미지 소스 정보 클래스
/// 데미지 소스에서 스킬 정보와 소스 타입을 추출하기 위해 사용됩니다.
/// </summary>
public class DamageSourceInfo
{
    public SkillSlotType? skillSlotType;
    public DamageSourceType sourceType;

    private DamageSourceInfo(SkillSlotType? skillSlotType, DamageSourceType sourceType)
    {
        this.skillSlotType = skillSlotType;
        this.sourceType = sourceType;
    }

    /// <summary>
    /// 데미지 소스에서 정보 추출
    /// </summary>
    /// <param name="damageSource">데미지 소스 객체</param>
    /// <returns>DamageSourceInfo 인스턴스</returns>
    public static DamageSourceInfo FromDamageSource(object damageSource)
    {
        if (damageSource == null)
        {
            return new DamageSourceInfo(null, DamageSourceType.Projectile);
        }

        // 데미지 소스 타입에 따라 정보 추출
        // TODO: 실제 데미지 소스 구조에 맞게 수정 필요
        // 예시: SkillProjectile, SkillArea, SkillContact 등에서 정보 추출
        
        // 기본값: Projectile 타입, 스킬 정보 없음
        SkillSlotType? skillSlot = null;
        DamageSourceType sourceType = DamageSourceType.Projectile;

        // 데미지 소스가 SkillSlotType을 포함하는 경우
        if (damageSource is SkillSlotType)
        {
            skillSlot = (SkillSlotType)damageSource;
            sourceType = DamageSourceType.Projectile;
        }
        // 데미지 소스가 ISkillProjectile 인터페이스를 구현하는 경우 (프로젝타일)
        else if (damageSource is ISkillProjectile skillProjectile)
        {
            skillSlot = skillProjectile.GetSkillSlotType();
            sourceType = DamageSourceType.Projectile;
        }
        // 데미지 소스가 MonoBehaviour이고 ISkillProjectile 컴포넌트를 가진 경우
        else if (damageSource is MonoBehaviour monoBehaviour)
        {
            ISkillProjectile projectile = monoBehaviour.GetComponent<ISkillProjectile>();
            if (projectile != null)
            {
                skillSlot = projectile.GetSkillSlotType();
                sourceType = DamageSourceType.Projectile;
            }
            // AfterimageController인 경우 (RainOfFaith 스킬의 잔상)
            else if (monoBehaviour is AfterimageController afterimage)
            {
                // AfterimageController에서 SkillSlotType 추출
                skillSlot = afterimage.GetSkillSlotType();
                sourceType = DamageSourceType.Contact;
            }
            // SkillHandlerInterface를 구현하는 핸들러인 경우
            else if (monoBehaviour is SkillHandlerInterface handler)
            {
                // 핸들러에서 SkillSlotType 추출
                // CourageRushHandler, IronBladeDanceHandler 등은 GetSkillSlotType() 메서드를 가지고 있음
                if (monoBehaviour is CourageRushHandler courageRushHandler)
                {
                    skillSlot = courageRushHandler.GetSkillSlotType();
                }
                else if (monoBehaviour is IronBladeDanceHandler ironBladeDanceHandler)
                {
                    skillSlot = ironBladeDanceHandler.GetSkillSlotType();
                }
                // 다른 핸들러들도 GetSkillSlotType() 메서드를 추가하면 여기에 추가
                sourceType = DamageSourceType.Contact;
            }
        }

        return new DamageSourceInfo(skillSlot, sourceType);
    }
}

