using System;
using System.Collections.Generic;

[Serializable]
public struct EnvironmentEventParams
{
	public EnvironmentEventType eventType;

	public List<Biome> biomes;

	public float minDistanceFromCore;

	public int maxAmountOfNearbyObjects;

	public int minTotalTilesFulfillingRequirements;

	public List<EnvironmentEventTilesRequirement> tileRequirements;
}
