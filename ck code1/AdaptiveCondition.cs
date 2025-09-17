using System;
using UnityEngine;

[Serializable]
public struct AdaptiveCondition
{
	[Header("Variation to replace with")]
	public int variation;

	public int matchesNeeded;

	public bool allowAnyTilesetToMatch;

	[Header("Conditions")]
	public TileCondition leftTile;

	public TileCondition rightTile;

	public TileCondition forwardTile;

	public TileCondition backTile;
}
