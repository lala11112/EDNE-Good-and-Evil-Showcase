using UnityEngine;

/// <summary>
/// 스킬 핸들러 인터페이스
/// 모든 스킬 핸들러(플레이어/몬스터)가 구현해야 하는 공통 규약
/// 
/// ✅ 용도:
/// 1. 스킬 로직의 표준화
/// 2. PlayerController/MonsterController에서 일관된 방식으로 스킬 실행
/// 3. 핸들러 프리팹 패턴 지원
/// </summary>
public interface SkillHandlerInterface
{
    /// <summary>
    /// 스킬 핸들러 초기 설정
    /// </summary>
    /// <param name="skillData">스킬 데이터 SO (PlayerSkillData 또는 MonsterSkillData)</param>
    /// <param name="owner">스킬 소유자 GameObject (Player 또는 Monster)</param>
    void Setup(BaseSkillData skillData, GameObject owner);
    
    /// <summary>
    /// 스킬 사용
    /// </summary>
    /// <param name="rank">스킬 랭크 (플레이어용, 몬스터는 무시)</param>
    void Use(SkillRank rank);
    
    /// <summary>
    /// 스킬 정리 (종료 시 호출)
    /// </summary>
    void Cleanup();
}

