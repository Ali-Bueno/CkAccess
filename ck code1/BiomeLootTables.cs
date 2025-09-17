using System;
using System.Collections.Generic;
using Pug.UnityExtensions;

[Serializable]
public class BiomeLootTables
{
	public AreaLevel biomeLevel;

	[ArrayElementTitle("id")]
	public List<LootTable> lootTables;
}
