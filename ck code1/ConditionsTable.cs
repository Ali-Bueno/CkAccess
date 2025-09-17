using System;
using System.Collections.Generic;
using Pug.UnityExtensions;
using UnityEngine;

[CreateAssetMenu(menuName = "Pug/Tables/ConditionsTable", order = 4)]
public class ConditionsTable : ScriptableObject
{
	[Serializable]
	public class ConditionCategory
	{
		public string category;

		[ArrayElementTitle("Id")]
		public List<ConditionInfo> conditions;
	}

	public List<ConditionCategory> conditionCategories;

	private readonly List<ConditionInfo> _conditionsLookUp = new List<ConditionInfo>();

	public static ConditionsTable GetTable()
	{
		ConditionsTable conditionsTable = Resources.Load<ConditionsTable>("ConditionsTable");
		if (conditionsTable == null)
		{
			Debug.LogError("Could not find ConditionsTable asset");
		}
		return conditionsTable;
	}

	private void Init()
	{
		_conditionsLookUp.Resize(default(ConditionInfo), 328);
		foreach (ConditionCategory conditionCategory in conditionCategories)
		{
			foreach (ConditionInfo condition in conditionCategory.conditions)
			{
				if (condition.Id >= ConditionID.MAX_VALUES)
				{
					Debug.LogError($"Invalid condition ID {condition.Id}");
				}
				else if (_conditionsLookUp[(int)condition.Id].Id != 0)
				{
					Debug.LogError($"Condition {condition.Id} already exists in the table!");
				}
				else
				{
					_conditionsLookUp[(int)condition.Id] = condition;
				}
			}
		}
	}

	public ConditionInfo GetConditionInfo(ConditionID conditionID)
	{
		if (_conditionsLookUp.Count != 328)
		{
			Init();
		}
		return _conditionsLookUp[(int)conditionID];
	}
}
