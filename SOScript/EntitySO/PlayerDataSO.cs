using UnityEngine;

[CreateAssetMenu(fileName = "PlayerDataSO", menuName = "Entity/Entity Data/Player Data")]
public class PlayerDataSO : EntityDataSO
{
    private void Awake()
    {
        entityType = EntityType.Player;
    }
    
    [Header("플레이어 이동속도")]
    public int playerMoveSpeed;

    [Header("정신력")]
    public int maxMentalPower;
    public int currentmentalPower;
    
    [Header("용기 점수 기본값값")]
    public int courageScore = 200;        
    

    [Header("용기 점수별 상태")]
    public CourageState courageState = CourageState.Despair;
    
    [Header("각 단계별로의 용기수치 값")]
    [Header("절망 단계값")]
    public int despiarStepRequirement = 0;
    [Header("불안 단계값")]
    public int anxietyStepRequirement = 1;
    [Header("의심 단계값(용기점수 시작 기본값)")]
    public int doubtStepRequirement = 200;
    [Header("용기 단계값")]
    public int courageStepRequirement = 300;
    [Header("결의 단계값")]
    public int determinationStepRequirement = 400;
    [Header("확신 단계값")]
    public int confidenceStepRequirement = 500;

    [Header("랭크 시스템")]
    public PlayerSkillRank playerSkillRank;   // 외부에서 가져올 수 있지만 수정 불가
    
}

public enum CourageState
{
    Despair, anxiety, doubt, courage, determination, confidence
}

public enum PlayerSkillRank
{
    D, C, B, A, S
}