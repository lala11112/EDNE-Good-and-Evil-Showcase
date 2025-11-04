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
    
    [Space(10)]
    [Header("용기 획득 - 공격적 플레이")]
    [Tooltip("스킬로 적 처치 시 기본 용기 획득량")]
    public int killCourageGain = 15;
    
    [Tooltip("같은 스킬 연속 사용 시 용기 획득량 (페널티)")]
    public int sameSkillKillCourageGain = 10;
    
    [Space(5)]
    [Header("멀티킬 보너스 (동시 처치 수에 따른 보너스)")]
    [Tooltip("2마리 동시 처치 보너스")]
    public int multiKill2Bonus = 20;
    
    [Tooltip("3마리 동시 처치 보너스")]
    public int multiKill3Bonus = 40;
    
    [Tooltip("4마리 동시 처치 보너스")]
    public int multiKill4Bonus = 70;
    
    [Tooltip("5마리 이상 동시 처치 보너스")]
    public int multiKill5Bonus = 120;
    
    [Space(5)]
    [Header("스킬 연계 시스템 (서로 다른 스킬 연속 사용 보너스)")]
    [Tooltip("2종 스킬 연계 보너스")]
    public int combo2Bonus = 20;
    
    [Tooltip("3종 스킬 연계 보너스")]
    public int combo3Bonus = 30;
    
    [Tooltip("4종 스킬 연계 보너스 (풀 체인)")]
    public int combo4Bonus = 50;
    
    [Space(10)]
    [Header("용기 획득 - 방어적 플레이")]
    [Tooltip("완벽한 회피 시 용기 획득량 (근접 회피)")]
    public int perfectDodgeCourageGain = 25;
    
    [Tooltip("무피해 유지 시 초당 용기 획득량 (10초 이상)")]
    public float noDamageCourageGainPerSecond_10 = 5f;
    
    [Tooltip("무피해 유지 시 초당 용기 획득량 (20초 이상)")]
    public float noDamageCourageGainPerSecond_20 = 10f;
    
    [Space(10)]
    [Header("용기 감소 - 페널티")]
    [Tooltip("탄막에 피격 시 용기 감소량")]
    public int projectileHitCourageLoss = 40;
    
    [Tooltip("접촉 피격 시 용기 감소량")]
    public int contactHitCourageLoss = 30;
    
    [Tooltip("전투 없음 페널티 (초당 감소량)")]
    public float noCombatCourageLossPerSecond = 10f;
    
    [Space(5)]
    [Header("연속 피격 보호 (컴백 메커니즘)")]
    [Tooltip("10초 내 2번 피격 시 용기 감소량 (보호 적용)")]
    public int consecutiveHit2CourageLoss = 30;
    
    [Tooltip("10초 내 3번 이상 피격 시 용기 감소량 (보호 적용)")]
    public int consecutiveHit3PlusCourageLoss = 20;
    
    [Space(10)]
    [Header("타이머 설정")]
    [Tooltip("최근 처치 기록 유지 시간 (초)")]
    public float killRecordDuration = 15f;
    
    [Tooltip("최근 스킬 사용 기록 유지 시간 (초)")]
    public float skillUseRecordDuration = 3f;
    
    [Tooltip("멀티킬 판정 시간 (초)")]
    public float multiKillJudgmentDuration = 3f;
    
    [Tooltip("전투 없음 판정 시간 (초)")]
    public float noCombatJudgmentDuration = 5f;
    
    [Tooltip("연속 피격 판정 시간 (초)")]
    public float consecutiveHitJudgmentDuration = 10f;
    
    [Tooltip("무피해 유지 판정 시간 - 10초 (초)")]
    public float noDamageJudgmentDuration_10 = 10f;
    
    [Tooltip("무피해 유지 판정 시간 - 20초 (초)")]
    public float noDamageJudgmentDuration_20 = 20f;
    
    [Tooltip("완벽한 회피 판정 시간 (초)")]
    public float perfectDodgeJudgmentDuration = 0.3f;
    
    #endregion
    
    #region 사망 이펙트
    
    [Header("=== 사망 이펙트 (Death Effect) ===")]
    [Tooltip("플레이어 사망 시 생성될 이펙트 프리팹 (파티클, 애니메이션 등)")]
    public GameObject deathEffectPrefab;
    
    [Tooltip("사망 이펙트 지속 시간 (초) - 자동 삭제")]
    public float deathEffectDuration = 3f;
    
    [Tooltip("사망 사운드 효과음")]
    public AudioClip deathSound;
    
    [Tooltip("사망 시 카메라 쉐이크 강도 (0 = 없음)")]
    [Range(0f, 10f)]
    public float deathCameraShakeStrength = 5f;
    
    #endregion
    
    #region 피격 무적 시간
    
    [Header("=== 피격 무적 시간 (Hit Invincibility) ===")]
    [Tooltip("피격 시 무적 지속 시간 (초) - 연속 피격 방지")]
    [Range(0f, 2f)]
    public float hitInvincibilityDuration = 0.3f;
    
    [Tooltip("무적 중 스프라이트 깜빡임 효과 사용 여부")]
    public bool useBlinkEffect = true;
    
    [Tooltip("깜빡임 주기 (초) - 작을수록 빠르게 깜빡임")]
    [Range(0.05f, 0.5f)]
    public float blinkInterval = 0.1f;
    
    [Tooltip("무적 중 피격 색상 (빨간색 등)")]
    public Color hitColor = Color.red;
    
    #endregion
}
