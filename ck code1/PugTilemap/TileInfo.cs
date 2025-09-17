using System;
using Unity.Mathematics;

namespace PugTilemap;

public struct TileInfo : IEquatable<TileInfo>
{
	public int tileset;

	public TileType tileType;

	public int state;

	public TileInfo(int tileset, TileType tileType, int state)
	{
		this.tileset = tileset;
		this.tileType = tileType;
		this.state = state;
	}

	public bool Equals(TileInfo other)
	{
		if (other.tileset == tileset && other.tileType == tileType)
		{
			return other.state == state;
		}
		return false;
	}

	public override int GetHashCode()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		return (int)math.hash(new int3(tileset, (int)tileType, state));
	}
}
