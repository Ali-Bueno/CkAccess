using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(menuName = "Pug/Tables/UpgradeCostsTable", order = 4)]
public class UpgradeCostsTable : ScriptableObject
{
	public int barBaseCost;

	public float barMultiplier = 1f;

	public float barExpontential = 1f;

	public int coinBaseCost;

	public float coinMultiplier = 1f;

	public float coinExpontential = 1f;

	public List<UpgradeCosts> upgradeCosts;

	public static PetInfosTable GetTable()
	{
		PetInfosTable petInfosTable = Resources.Load<PetInfosTable>("UpgradeCostsTable");
		if (petInfosTable == null)
		{
			Debug.LogError("Could not find UpgradeCostsTable asset");
		}
		return petInfosTable;
	}

	private void OnValidate()
	{
		int maxLevel = LevelScaling.GetMaxLevel();
		while (upgradeCosts.Count <= maxLevel)
		{
			upgradeCosts.Add(new UpgradeCosts
			{
				upgradeCost = new List<UpgradeCost>
				{
					new UpgradeCost
					{
						item = ObjectID.CopperBar
					},
					new UpgradeCost
					{
						item = ObjectID.AncientCoin
					}
				}
			});
		}
		for (int i = 0; i < upgradeCosts.Count; i++)
		{
			List<UpgradeCost> upgradeCost = upgradeCosts[i].upgradeCost;
			for (int j = 0; j < upgradeCost.Count; j++)
			{
				if (upgradeCost[j].item == ObjectID.AncientCoin)
				{
					upgradeCost[j].amount = (int)math.round((float)coinBaseCost + math.pow((float)i, coinExpontential) * coinMultiplier);
				}
				else if (j == 0)
				{
					upgradeCost[j].amount = (int)math.round((float)barBaseCost + math.pow((float)i, barExpontential) * barMultiplier);
				}
			}
		}
	}

	public List<UpgradeCost> GetUpgradeCost(int level)
	{
		if (upgradeCosts.Count > level)
		{
			return upgradeCosts[level].upgradeCost;
		}
		return upgradeCosts[upgradeCosts.Count - 1].upgradeCost;
	}
}
