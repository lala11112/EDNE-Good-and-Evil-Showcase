using UnityEngine;

/// <summary>
/// 스킬 투사체(Projectile) 인터페이스
/// 모든 스킬 투사체(Fireball, Arrow, Missile 등)가 구현해야 하는 공통 규약
/// 
/// ✅ 용도:
/// 1. 다양한 스킬 투사체를 일관된 방식으로 다루기 위함
/// 2. MonsterController/PlayerController에서 타입 안전성 보장
/// 3. 팀킬 방지 시스템 통합 (EntityType으로 아군/적군 구분)
/// 4. 스킬 데이터 설정의 표준화
/// </summary>
public interface ISkillProjectile
{
    /// <summary>
    /// 스킬 데이터 설정
    /// </summary>
    /// <param name="damage">데미지 값</param>
    /// <param name="speed">투사체 속도</param>
    void SetSkillData(float damage, float speed);
    
    /// <summary>
    /// 공격자 타입 설정 (팀킬 방지용)
    /// </summary>
    /// <param name="attackerType">공격자의 EntityType (Player, Monster 등)</param>
    void SetAttackerType(EntityType attackerType);
    
    /// <summary>
    /// 스킬 소유자 설정 (선택 사항)
    /// 스킬이 이동을 제어할 수 있도록 소유자 컨트롤러 전달
    /// </summary>
    /// <param name="owner">스킬을 사용한 컨트롤러 (PlayerController, MonsterController 등)</param>
    void SetOwnerController(MonoBehaviour owner);
    
    /// <summary>
    /// 투사체의 관통 여부를 설정합니다.
    /// </summary>
    /// <param name="isPiercing">true = 관통형 (충돌해도 파괴 안됨), false = 충돌 시 파괴</param>
    void SetPiercing(bool isPiercing);
    
    /// <summary>
    /// 스킬 슬롯 타입 가져오기 (용기 시스템용)
    /// </summary>
    /// <returns>스킬 슬롯 타입 (없으면 null)</returns>
    SkillSlotType? GetSkillSlotType();
}

