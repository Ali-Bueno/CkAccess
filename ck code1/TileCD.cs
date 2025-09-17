using System;
using PugTilemap;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct TileCD : IComponentData, IQueryTypeParameter, IEquatable<TileCD>, IComparable<TileCD>
{
	public int tileset;

	public TileType tileType;

	public bool Equals(TileCD other)
	{
		if (other.tileset == tileset)
		{
			return other.tileType == tileType;
		}
		return false;
	}

	public override int GetHashCode()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		return (int)math.hash(new int2(tileset, (int)tileType));
	}

	public int CompareTo(TileCD other)
	{
		if (tileType < other.tileType)
		{
			return -1;
		}
		if (tileType > other.tileType)
		{
			return 1;
		}
		if (tileset >= other.tileset)
		{
			if (tileset <= other.tileset)
			{
				return 0;
			}
			return 1;
		}
		return -1;
	}
}
