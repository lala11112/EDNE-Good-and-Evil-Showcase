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
    public int initialCourageScore = 0;
    
    [Space(10)]
    [Header("용기 시스템 - 단계 요구 점수 (누적 용기 점수)")]
    [Tooltip("불안 (Anxiety) - 스킬 랭크 D (누적 0)")]
    public int anxietyStepRequirement = 0;
    
    [Tooltip("의심 (Doubt) - 스킬 랭크 C (누적 100)")]
    public int doubtStepRequirement = 100;
    
    [Tooltip("용기 (Courage) - 스킬 랭크 B (누적 250)")]
    public int courageStepRequirement = 250;
    
    [Tooltip("결의 (Determination) - 스킬 랭크 A (누적 450)")]
    public int determinationStepRequirement = 450;
    
    [Tooltip("확신 (Confidence) - 스킬 랭크 S (누적 700)")]
    public int confidenceStepRequirement = 700;
    
    [Space(10)]
    [Header("용기 획득 - 기본 전투 행동")]
    [Tooltip("기본 처치 용기")]
    public int killCourageGain = 15;
    
    [Tooltip("같은 스킬 연속 사용 페널티")]
    public int sameSkillKillCourageGain = 10;
    
    [Space(10)]
    [Header("용기 획득 - 멀티킬 보너스")]
    [Tooltip("2킬 보너스")]
    public int multiKill2Bonus = 20;
    
    [Tooltip("3킬 보너스")]
    public int multiKill3Bonus = 30;
    
    [Tooltip("4킬 보너스")]
    public int multiKill4Bonus = 40;
    
    [Tooltip("5킬 이상 보너스")]
    public int multiKill5Bonus = 50;
    
    [Space(10)]
    [Header("용기 획득 - 스킬 연계 보너스")]
    [Tooltip("2종 콤보 보너스")]
    public int combo2Bonus = 20;
    
    [Tooltip("3종 콤보 보너스")]
    public int combo3Bonus = 30;
    
    [Tooltip("4종 콤보 보너스 (풀 체인)")]
    public int combo4Bonus = 50;
    
    [Space(10)]
    [Header("용기 획득 - 무피해 보너스")]
    [Tooltip("10초 무피해 판정 시간")]
    public float noDamageJudgmentDuration_10 = 10f;
    
    [Tooltip("10초 무피해 보너스/초")]
    public float noDamageCourageGainPerSecond_10 = 5f;
    
    [Tooltip("20초 무피해 판정 시간")]
    public float noDamageJudgmentDuration_20 = 20f;
    
    [Tooltip("20초 무피해 보너스/초")]
    public float noDamageCourageGainPerSecond_20 = 10f;
    
    [Space(10)]
    [Header("용기 획득 - 완벽 회피")]
    [Tooltip("완벽 회피 용기 획득량")]
    public int perfectDodgeCourageGain = 25;
    
    [Tooltip("완벽 회피 판정 거리 (기본 거리)")]
    public float perfectDodgeDistance = 0.5f;
    
    [Tooltip("완벽 회피 판정 시간 (초) - 이 시간 동안 이동할 수 있는 거리만큼 추가 판정")]
    public float perfectDodgeJudgmentTime = 0.3f;
    
    [Space(10)]
    [Header("용기 감소 - 피격")]
    [Tooltip("탄막 피격 용기 감소량")]
    public int projectileHitCourageLoss = 40;
    
    [Tooltip("접촉 피격 용기 감소량")]
    public int contactHitCourageLoss = 30;
    
    [Tooltip("환경 피격 용기 감소량")]
    public int environmentHitCourageLoss = 30;
    
    [Space(10)]
    [Header("용기 감소 - 연속 피격 보호")]
    [Tooltip("2번 연속 피격 용기 감소량")]
    public int consecutiveHit2CourageLoss = 30;
    
    [Tooltip("3번 이상 연속 피격 용기 감소량")]
    public int consecutiveHit3PlusCourageLoss = 20;
    
    [Space(10)]
    [Header("용기 감소 - 전투 없음 페널티")]
    [Tooltip("5초간 전투 없음 판정")]
    public float noCombatJudgmentDuration = 5f;
    
    [Tooltip("전투 없음 페널티/초")]
    public float noCombatCourageLossPerSecond = 10f;
    
    [Space(10)]
    [Header("용기 시스템 - 기록 유지 시간")]
    [Tooltip("처치 기록 유지 시간")]
    public float killRecordDuration = 15f;
    
    [Tooltip("스킬 사용 기록 유지 시간 (연계 판정용)")]
    public float skillUseRecordDuration = 5f;
    
    [Tooltip("멀티킬 판정 시간")]
    public float multiKillJudgmentDuration = 3f;
    
    [Tooltip("연속 피격 판정 시간")]
    public float consecutiveHitJudgmentDuration = 10f;
    
    [Space(10)]
    [Header("전투 시스템")]
    [Tooltip("초기 전투 시간")]
    public float initialCombatDuration = 15f;
    
    [Tooltip("전투 행동 시 연장 시간")]
    public float combatExtensionDuration = 15f;
    
    [Tooltip("전투 종료 후 재개 가능 시간")]
    public float combatEndJudgmentDuration = 15f;
    
    [Space(10)]
    [Header("S랭크 궁극기 버프 (용기의 여운)")]
    [Tooltip("버프 지속 시간 (초)")]
    public float ultimateBuffDuration = 15f;
    
    [Tooltip("용기 획득량 배율 (1.5 = 1.5배 증가)")]
    [Range(1f, 3f)]
    public float ultimateBuffCourageGainMultiplier = 1.5f;
    
    [Tooltip("정신력 회복 속도 배율 (2.0 = 2배 증가)")]
    [Range(1f, 5f)]
    public float ultimateBuffMentalPowerRegenMultiplier = 2f;
    
    [Tooltip("이동 속도 배율 (1.2 = 1.2배 증가)")]
    [Range(1f, 2f)]
    public float ultimateBuffMoveSpeedMultiplier = 1.2f;
    
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
