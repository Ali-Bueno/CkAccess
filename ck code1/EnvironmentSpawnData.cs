using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Pug.UnityExtensions;
using PugTilemap;
using UnityEngine;

[Serializable]
public class EnvironmentSpawnData
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

		[Tooltip("We will only spawn on this tile surface.")]
		public TileType tileType;

		[Tooltip("List of allowed tilesets. Empty means any.")]
		public List<Tileset> tilesets;

		[AllowNesting]
		public AdjacentTileList adjacentTiles;

		[Tooltip("A blocked area is for example a dungeon or custom scene.")]
		public bool canSpawnInBlockedArea;

		[Tooltip("Skip spawning this object when filling in a partial map when a new biome is added.")]
		public bool skipSpawnForPartialMaps;
	}

	[HideInInspector]
	public string name;

	public SpawnCheck spawnCheck;

	public List<SpawnObjectData> spawns;
}
