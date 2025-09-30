using UnityEngine;

[CreateAssetMenu(fileName = "BaseSkillData", menuName = "Skills/Skill Data")]
public class BaseSkillData : ScriptableObject
{
    [Header("기본 정보")]
    public string skillName;
    public SkillType skillType;
    public string description;
    public float cooldown;
    public int mentalCost;
    
    [Header("실행 스크립트")]
    public string scriptClassName;        // ← 스크립트 클래스명
    public string methodName;   // ← 실행할 메서드명

    [Header("핸들러 프리팹")]
    public GameObject handlerPrefab; // ISkillHandler가 붙은 프리팹
}