using UnityEngine;

[CreateAssetMenu(fileName = "SkillLoadout", menuName = "Skills/Skill Loadout")]
public class SkillLoadout : ScriptableObject
{
	public BaseSkillData zSkill;
	public BaseSkillData xSkill;
	public BaseSkillData cSkill;
	public BaseSkillData vSkill;
}
