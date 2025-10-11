using UnityEngine;

[CreateAssetMenu(fileName = "SkillLoadout", menuName = "Skills/Skill Loadout")]
public class SkillLoadout : ScriptableObject
{
	public PlayerSkillData zSkill;
	public PlayerSkillData xSkill;
	public PlayerSkillData cSkill;
	public PlayerSkillData vSkill;
}
