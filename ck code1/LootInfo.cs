using System;
using Pug.UnityExtensions;
using UnityEngine;

[Serializable]
public class LootInfo
{
	public ObjectID objectID;

	public float weight;

	[MinMax(1f, 100f)]
	public Pug.UnityExtensions.RangeInt amount = new Pug.UnityExtensions.RangeInt
	{
		min = 1,
		max = 1
	};

	[HideInInspector]
	public float editorVisualDropChance;

	[HideInInspector]
	public string info;

	[HideInInspector]
	public float accumulatedDropChance;

	public bool isPartOfGuaranteedDrop;

	public Biome onlyDropsInBiome;
}
