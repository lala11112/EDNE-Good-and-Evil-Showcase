using UnityEngine;

[CreateAssetMenu(fileName = "TestZSkillData", menuName = "Skills/SkillTest/TestZSkillData01")]
public class Test01ZSkillData : BaseSkillData
{
    [Header("거절의 검무 설정")]
    public float attackRange = 2f;     // 사정거리
    public int attackDamage = 100;
}
