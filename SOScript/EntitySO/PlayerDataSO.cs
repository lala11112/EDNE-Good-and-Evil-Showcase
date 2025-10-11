using UnityEngine;

/// <summary>
/// 플레이어 데이터 ScriptableObject
/// 플레이어의 기본 스탯과 시스템 설정값을 담습니다.
/// 런타임 데이터(현재 체력, 정신력 등)는 PlayerController에서 관리합니다.
/// </summary>
[CreateAssetMenu(fileName = "PlayerDataSO", menuName = "Entity/Entity Data/Player Data")]
public class PlayerDataSO : EntityDataSO
{
    #region 초기화
    
    private void OnValidate()
    {
        // 에디터에서 생성 시 자동으로 Player 타입 설정
        entityType = EntityType.Player;
    }
    
    #endregion
    
    #region 정신력 시스템
    
    [Header("=== 정신력 시스템 (Mental Power) ===")]
    [Tooltip("최대 정신력 (마나와 유사한 개념)")]
    public int maxMentalPower = 100;
    
    [Tooltip("초기 정신력 (게임 시작 시)")]
    public int initialMentalPower = 100;
    
    [Tooltip("정신력 자동 회복 속도 (초당 회복량)")]
    public float mentalPowerRegenRate = 5f;
    
    #endregion
    
    #region 공격력 시스템 (Attack Power System)
    
    [Header("=== 공격력 시스템 (Attack Power) ===")]
    [Tooltip("기본 공격력 (스킬 데미지 계산의 기준)")]
    public float attackPower = 10f;
    
    #endregion
    
    #region 용기 시스템 (Courage System)
    
    [Header("=== 용기 시스템 (Courage System) ===")]
    [Tooltip("초기 용기 점수 (게임 시작 시)")]
    public int initialCourageScore = 200;
    
    
    [Space(10)]
    [Header("용기 단계별 요구 점수")]
    [Tooltip("절망 (Despair) - 게임오버 상태")]
    public int despairStepRequirement = 0;
    
    [Tooltip("불안 (Anxiety) - 스킬 랭크 D")]
    public int anxietyStepRequirement = 1;
    
    [Tooltip("의심 (Doubt) - 스킬 랭크 C (기본 시작 단계)")]
    public int doubtStepRequirement = 200;
    
    [Tooltip("용기 (Courage) - 스킬 랭크 B")]
    public int courageStepRequirement = 300;
    
    [Tooltip("결의 (Determination) - 스킬 랭크 A")]
    public int determinationStepRequirement = 400;
    
    [Tooltip("확신 (Confidence) - 스킬 랭크 S")]
    public int confidenceStepRequirement = 500;
    
    #endregion
}
