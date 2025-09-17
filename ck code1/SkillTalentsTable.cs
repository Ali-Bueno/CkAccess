using System;
using System.Collections.Generic;
using Pug.UnityExtensions;
using UnityEngine;

[CreateAssetMenu(menuName = "Pug/Tables/SkillTalentsTable", order = 4)]
public class SkillTalentsTable : ScriptableObject
{
	[Serializable]
	public struct SkillTalentTree
	{
		public SkillID skillID;

		[ArrayElementTitle("name, givesCondition, conditionValuePerPoint")]
		public List<SkillTalentInfo> skillTalents;
	}

	[Serializable]
	public struct SkillTalentInfo
	{
		public string name;

		public ConditionID givesCondition;

		public int conditionValuePerPoint;

		public Sprite icon;
	}

	[ArrayElementTitle("skillID")]
	public List<SkillTalentTree> skillTalentTrees;

	public SkillTalentTree GetSkillTalentTree(SkillID skillID)
	{
		foreach (SkillTalentTree skillTalentTree in skillTalentTrees)
		{
			if (skillTalentTree.skillID == skillID)
			{
				return skillTalentTree;
			}
		}
		Debug.LogError("Could not find any skill talent tree for " + skillID);
		return default(SkillTalentTree);
	}

	public ConditionData GetConditionDataForSkillTalent(SkillID skillTreeID, int talentIndex, int points)
	{
		for (int i = 0; i < skillTalentTrees.Count; i++)
		{
			if (skillTalentTrees[i].skillID != skillTreeID)
			{
				continue;
			}
			List<SkillTalentInfo> skillTalents = skillTalentTrees[i].skillTalents;
			for (int j = 0; j < skillTalents.Count; j++)
			{
				if (j == talentIndex)
				{
					ConditionData result = default(ConditionData);
					result.conditionID = skillTalents[j].givesCondition;
					result.value = skillTalents[j].conditionValuePerPoint * points;
					return result;
				}
			}
			break;
		}
		Debug.LogError("Could not find skill talent with index " + talentIndex + " when trying to get condition data.");
		return default(ConditionData);
	}
}
