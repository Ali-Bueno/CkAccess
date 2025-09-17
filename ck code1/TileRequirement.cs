using System;
using PugTilemap;

[Serializable]
public struct TileRequirement
{
	public TileType tileType;

	public bool mustAlsoMatchTileset;

	public Tileset tileset;
}
