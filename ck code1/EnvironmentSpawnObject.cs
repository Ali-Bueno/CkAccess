using System;
using System.Collections.Generic;
using Pug.UnityExtensions;
using PugTilemap;
using UnityEngine;

[Serializable]
public class EnvironmentSpawnObject
{
	public ObjectID objectId;

	[MinMax(0f, 10f)]
	public Pug.UnityExtensions.RangeInt variation;

	public int amount = 1;

	[HideInInspector]
	public bool isTile;

	[HideInInspector]
	public TileType tileType;

	public Tileset tileset;

	public float spawnChance;

	public float maxSpawnPerTile;

	public float respawnChance;

	[Tooltip("The respawn chance will be multiplied with ((1 - respawnChanceDecay) ^ (amount of already existing entities)) effectively reducing the respawn chance when there are more entities of same type.")]
	public float respawnChanceDecay;

	[HideInInspector]
	public float respawnChanceWhenAdjacentToTileType;

	[Tooltip("The maximum amount of entities to spawn per respawn. If set to for example 1.4 then the second one has a 40% chance to spawn (multiplied with the respawn chance).")]
	public float maxSpawnsPerRespawn;

	[Tooltip("The minimum amount of valid tiles required to spawn at all in a respawn area. No requirement if left as 0.")]
	public float minTilesRequiredToRespawn;

	public float spawnChanceWhenAdjacentToTileType;

	[ArrayElementTitle("tileType")]
	public List<TileRequirement> adjacentTiles;

	public EnvironmentObjectSpawnAlgorithm spawnAlgorithm;

	public float clusterSpawnChance;

	public float clusterSpreadChance;

	public ClusterSpreadType clusterSpreadType;

	[HideInInspector]
	public Biome spawnsInBiome;

	[Tooltip("A blocked area is for example a dungeon or custom scene.")]
	public bool canSpawnInBlockedArea;

	[Tooltip("Nearby lights will hinder spawning.")]
	[HideInInspector]
	public bool cannotSpawnInLight;

	[Tooltip("Skip spawning this object when filling in a partial map when a new biome is added.")]
	public bool skipSpawnForPartialMaps;

	[Header("Requirements:")]
	[Tooltip("If left to 0 then just spawns anywhere within biome")]
	[MinMax(0f, 1000f)]
	public Pug.UnityExtensions.RangeInt mustBeWithinDistanceFromCore;

	public TileType spawnsOnTileType;

	public List<Tileset> onlySpawnsOnTilesets;
}
