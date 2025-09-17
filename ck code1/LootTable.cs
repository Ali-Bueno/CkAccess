using System;
using System.Collections.Generic;
using Pug.UnityExtensions;

[Serializable]
public class LootTable
{
	public LootTableID id;

	public int minUniqueDrops;

	public int maxUniqueDrops;

	public bool dontAllowDuplicates;

	[ArrayElementTitle("objectID, info")]
	public List<LootInfo> lootInfos;

	[ArrayElementTitle("objectID, info")]
	public List<LootInfo> guaranteedLootInfos;
}
