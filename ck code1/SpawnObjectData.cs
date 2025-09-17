using System;
using NaughtyAttributes;
using Pug.UnityExtensions;
using UnityEngine;

[Serializable]
public struct SpawnObjectData
{
	public EnvironmentSpawnType spawnType;

	public ObjectID objectID;

	public bool advancedVariationControl;

	[AllowNesting]
	[HideIf("advancedVariationControl")]
	[MinMax(0f, 10f)]
	public Pug.UnityExtensions.RangeInt variation;

	[AllowNesting]
	[ShowIf("advancedVariationControl")]
	public WeightedVariationsList weightedVariations;

	[Tooltip("0 will use the default amount value")]
	public int amount;

	[AllowNesting]
	[ShowIf("spawnType", EnvironmentSpawnType.Cluster)]
	[Tooltip("Chance to spawn at something at a position we have spread to. Lower value will give a sparser spawn")]
	public float clusterSpawnChance;

	[AllowNesting]
	[ShowIf("spawnType", EnvironmentSpawnType.Cluster)]
	[Tooltip("Chance to spread this spawn to another tile. Higher value will yield a larger spawn area.")]
	public float clusterSpreadChance;

	[AllowNesting]
	[ShowIf("spawnType", EnvironmentSpawnType.Cluster)]
	public bool clusterSpreadFourWayOnly;
}
