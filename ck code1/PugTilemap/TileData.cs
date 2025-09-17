using System;
using UnityEngine;

namespace PugTilemap;

[Serializable]
public class TileData
{
	public TileInfo info;

	public TileData(TileInfo info, Vector3 position)
	{
		this.info = info;
	}
}
