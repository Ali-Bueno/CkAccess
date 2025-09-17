using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Pug.UnityExtensions;
using PugTilemap;
using UnityEngine;

[Serializable]
public class RespawnData
{
	[Serializable]
	public struct SpawnCheck
	{
		[Serializable]
		public struct AdjacentTileList
		{
			[ArrayElementTitle("tileType")]
			public List<TileRequirement> list;
		}

		public EnvironmentalSpawnChance spawnChance;

		[Tooltip("Only spawns in this biome. None means any.")]
		public Biome biome;

		[Tooltip("The respawn chance will be multiplied with ((1 - respawnChanceDecay) ^ (amount of already existing entities)) effectively reducing the respawn chance when there are more entities of same type.")]
		public float spawnChanceDecay;

		[Tooltip("The maximum amount to spawn per valid tile.")]
		public PlatformDependentValue<float> maxSpawnPerTile;

		[Tooltip("The max number of objects that can spawn per respawn.")]
		public int maxSpawnsPerRespawn;

		[Tooltip("The minimum amount of valid tiles required to spawn at all in a respawn area. No requirement if left as 0.")]
		public float minTilesRequired;

		[Tooltip("We will only spawn on this tile surface.")]
		public TileType tileType;

		[Tooltip("List of allowed tilesets. Empty means any.")]
		public List<Tileset> tilesets;

		[AllowNesting]
		public AdjacentTileList adjacentTiles;
	}

	[HideInInspector]
	public string name;

	public SpawnCheck spawnCheck;

	public List<SpawnObjectData> spawns;
}
