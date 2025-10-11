using UnityEngine;

[CreateAssetMenu(fileName = "TestZSkillData", menuName = "Skills/SkillTest/TestZSkillData02")]
public class Test02ZSkillData : PlayerSkillData
{
    [Header("테스트의 모시깽이이 설정")]
    public float attackRange = 2f;     // 사정거리
    public int attackDamage = 100;
    public string testString = "테스트문자열";
}
