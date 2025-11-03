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
