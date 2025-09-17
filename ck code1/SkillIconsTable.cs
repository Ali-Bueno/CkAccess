using System.Collections.Generic;
using Pug.UnityExtensions;
using UnityEngine;

[CreateAssetMenu(menuName = "Pug/Tables/SkillIconsTable", order = 4)]
public class SkillIconsTable : ScriptableObject
{
	[ArrayElementTitle("skillID")]
	public List<SkillIcon> skillIcons;

	private Dictionary<SkillID, SkillIcon> skillIconsLookUp;

	public void Init()
	{
		skillIconsLookUp = new Dictionary<SkillID, SkillIcon>();
		foreach (SkillIcon skillIcon in skillIcons)
		{
			if (!skillIconsLookUp.ContainsKey(skillIcon.skillID))
			{
				skillIconsLookUp.Add(skillIcon.skillID, skillIcon);
			}
		}
	}

	public SkillIcon GetIcon(SkillID conditionID)
	{
		if (skillIconsLookUp == null)
		{
			Init();
		}
		if (skillIconsLookUp.ContainsKey(conditionID))
		{
			return skillIconsLookUp[conditionID];
		}
		return null;
	}
}
