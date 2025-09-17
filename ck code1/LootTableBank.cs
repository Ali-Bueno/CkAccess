using System.Collections.Generic;
using System.Globalization;
using Pug.UnityExtensions;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(menuName = "Pug/Tables/LootTableBank", order = 3)]
public class LootTableBank : ScriptableObject, ISerializationCallbackReceiver
{
	[ArrayElementTitle("biomeLevel")]
	public List<BiomeLootTables> biomeLootTables;

	public void OnBeforeSerialize()
	{
	}

	public void OnAfterDeserialize()
	{
		foreach (BiomeLootTables biomeLootTable in biomeLootTables)
		{
			foreach (LootTable lootTable in biomeLootTable.lootTables)
			{
				bool flag = lootTable.maxUniqueDrops > 1;
				lootTable.guaranteedLootInfos.Clear();
				foreach (LootInfo lootInfo in lootTable.lootInfos)
				{
					if (lootInfo.isPartOfGuaranteedDrop)
					{
						if (flag)
						{
							lootTable.guaranteedLootInfos.Add(new LootInfo
							{
								objectID = lootInfo.objectID,
								amount = lootInfo.amount,
								weight = lootInfo.weight,
								isPartOfGuaranteedDrop = lootInfo.isPartOfGuaranteedDrop,
								onlyDropsInBiome = lootInfo.onlyDropsInBiome
							});
						}
						else
						{
							lootInfo.isPartOfGuaranteedDrop = false;
							Debug.LogError("Not allowed to have guaranteed drops in loot tables that only drops one item.");
						}
					}
				}
				bool num = lootTable.guaranteedLootInfos.Count > 0;
				int minUniqueDrops = (num ? math.max(0, lootTable.minUniqueDrops - 1) : lootTable.minUniqueDrops);
				int maxUniqueDrops = (num ? math.max(0, lootTable.maxUniqueDrops - 1) : lootTable.maxUniqueDrops);
				if (num)
				{
					InitLoot(lootTable.guaranteedLootInfos, 1, 1, null);
				}
				InitLoot(lootTable.lootInfos, minUniqueDrops, maxUniqueDrops, lootTable.guaranteedLootInfos);
			}
		}
	}

	private void InitLoot(List<LootInfo> lootInfos, int minUniqueDrops, int maxUniqueDrops, List<LootInfo> guaranteedLootInfos)
	{
		float num = 0f;
		foreach (LootInfo lootInfo3 in lootInfos)
		{
			num += lootInfo3.weight;
		}
		float num2 = 0f;
		float num3 = (float)(maxUniqueDrops + minUniqueDrops) / 2f;
		foreach (LootInfo lootInfo in lootInfos)
		{
			float num4 = ((num <= 0f) ? 0f : (lootInfo.weight / num));
			float num5 = (1f - math.pow(1f - num4, num3)) * 100f;
			lootInfo.editorVisualDropChance = num5;
			float num6 = 0f;
			if (guaranteedLootInfos != null && lootInfo.isPartOfGuaranteedDrop)
			{
				LootInfo lootInfo2 = guaranteedLootInfos.Find((LootInfo x) => x.objectID == lootInfo.objectID);
				lootInfo.editorVisualDropChance = (1f - (1f - lootInfo2.editorVisualDropChance / 100f) * (1f - num5 / 100f)) * 100f;
				num6 = lootInfo2.editorVisualDropChance / 100f * ((float)(lootInfo2.amount.min + lootInfo2.amount.max) / 2f) * 100f;
			}
			float num7 = num4 * ((float)(lootInfo.amount.min + lootInfo.amount.max) / 2f) * num3 * 100f + num6;
			lootInfo.info = lootInfo.editorVisualDropChance.ToString(CultureInfo.InvariantCulture) + "%       Amount per hundred " + num7.ToString(CultureInfo.InvariantCulture);
			num2 += num4;
			lootInfo.accumulatedDropChance = num2;
		}
	}

	public List<LootTable> GetLootTables()
	{
		List<LootTable> list = new List<LootTable>();
		foreach (BiomeLootTables biomeLootTable in biomeLootTables)
		{
			foreach (LootTable lootTable in biomeLootTable.lootTables)
			{
				list.Add(lootTable);
			}
		}
		return list;
	}
}
