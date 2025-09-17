using System;
using System.Collections.Generic;
using PugTilemap;

[Serializable]
public struct EnvironmentEventTilesRequirement
{
	public int minimumAmountOfTiles;

	public TileType tileType;

	public List<Tileset> tilesets;
}
