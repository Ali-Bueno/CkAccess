using System.Collections.Generic;
using Unity.Collections;

namespace PugTilemap;

public static class TileTypeUtility
{
	public static readonly TileType[] SurfacePriority;

	private static readonly int[] SurfacePriorityLookup;

	static TileTypeUtility()
	{
		SurfacePriority = new TileType[23]
		{
			TileType.greatWall,
			TileType.ancientCrystal,
			TileType.ore,
			TileType.wall,
			TileType.thinWall,
			TileType.fence,
			TileType.bigRoot,
			TileType.rail,
			TileType.looseFlooring,
			TileType.rug,
			TileType.groundSlime,
			TileType.floor,
			TileType.litFloor,
			TileType.circuitPlate,
			TileType.chrysalis,
			TileType.bridge,
			TileType.ancientCircuitPlate,
			TileType.wateredGround,
			TileType.dugUpGround,
			TileType.ground,
			TileType.water,
			TileType.pit,
			TileType.none
		};
		SurfacePriorityLookup = new int[135];
		for (int i = 0; i < SurfacePriorityLookup.Length; i++)
		{
			SurfacePriorityLookup[i] = -1;
		}
		for (int j = 0; j < SurfacePriority.Length; j++)
		{
			TileType tileType = SurfacePriority[j];
			SurfacePriorityLookup[(int)tileType] = SurfacePriority.Length - j;
		}
	}

	public static bool IsBaseGroundTile(this TileType t)
	{
		if (t != TileType.ground && t != TileType.water)
		{
			return t == TileType.pit;
		}
		return true;
	}

	public static bool IsNonSolidTile(this TileType t)
	{
		if (t != 0 && t != TileType.water)
		{
			return t == TileType.pit;
		}
		return true;
	}

	public static bool IsWallTile(this TileType t)
	{
		if (t != TileType.wall && t != TileType.thinWall && t != TileType.greatWall && t != TileType.ore)
		{
			return t == TileType.ancientCrystal;
		}
		return true;
	}

	public static bool IsWallOrThinWall(this TileType t)
	{
		if (t != TileType.wall)
		{
			return t == TileType.thinWall;
		}
		return true;
	}

	public static bool IsWalkableTile(this TileType t)
	{
		if (t != TileType.ground && t != TileType.circuitPlate && t != TileType.dugUpGround && t != TileType.wateredGround && t != TileType.groundSlime && t != TileType.chrysalis && t != TileType.ancientCircuitPlate && t != TileType.floor && t != TileType.bridge && t != TileType.rug && t != TileType.rail && t != TileType.litFloor)
		{
			return t == TileType.looseFlooring;
		}
		return true;
	}

	public static bool IsFlyOverTile(this TileType t)
	{
		if (t != TileType.ground && t != TileType.circuitPlate && t != TileType.dugUpGround && t != TileType.wateredGround && t != TileType.groundSlime && t != TileType.chrysalis && t != TileType.ancientCircuitPlate && t != TileType.floor && t != TileType.bridge && t != TileType.rug && t != TileType.rail && t != TileType.litFloor && t != TileType.looseFlooring && t != TileType.water && t != TileType.pit && t != TileType.bigRoot)
		{
			return t == TileType.fence;
		}
		return true;
	}

	public static bool IsPseudoTile(this TileType t)
	{
		if (t != TileType.circuitPlate)
		{
			return t == TileType.ancientCircuitPlate;
		}
		return true;
	}

	public static bool CanSpawnCritter(this TileType t)
	{
		if (t != TileType.ground && t != TileType.dugUpGround && t != TileType.wateredGround && t != TileType.groundSlime)
		{
			return t == TileType.chrysalis;
		}
		return true;
	}

	public static bool CantBeDugWithShovelWhileStandingOn(this TileType t)
	{
		if (t != TileType.ground && t != TileType.dugUpGround && t != TileType.wateredGround)
		{
			return t == TileType.bridge;
		}
		return true;
	}

	public static bool IsPaintableEditorTile(this TileType t)
	{
		if (t != TileType.circuitPlate && t != TileType.ancientCircuitPlate && t != TileType.wallCrack && t != TileType.floorCrack && t > TileType.none)
		{
			return t < TileType.__max__;
		}
		return false;
	}

	public static bool ShouldNotExistOnItsOwn(this TileType t)
	{
		return t.GetSurfacePriority() < TileType.none.GetSurfacePriority();
	}

	public static int GetSurfacePriority(this TileType t)
	{
		return SurfacePriorityLookup[(int)t];
	}

	[GenerateTestsForBurstCompatibility]
	public static char GetDebugCharacter(this TileType t)
	{
		switch (t)
		{
		case TileType.ancientCrystal:
			return '*';
		case TileType.ore:
			return '$';
		case TileType.wall:
		case TileType.thinWall:
		case TileType.greatWall:
			return '#';
		case TileType.fence:
			return '-';
		case TileType.bigRoot:
			return '&';
		case TileType.rug:
			return '=';
		case TileType.circuitPlate:
			return '<';
		case TileType.ancientCircuitPlate:
			return '>';
		case TileType.bridge:
			return '¤';
		case TileType.floor:
			return '_';
		case TileType.groundSlime:
			return '@';
		case TileType.chrysalis:
			return '@';
		case TileType.wateredGround:
			return ';';
		case TileType.dugUpGround:
			return ':';
		case TileType.ground:
			return '_';
		case TileType.water:
			return '~';
		case TileType.pit:
			return '.';
		case TileType.rail:
			return '+';
		case TileType.litFloor:
			return '_';
		case TileType.looseFlooring:
			return '_';
		case TileType.none:
			return ' ';
		default:
			return '?';
		}
	}

	[GenerateTestsForBurstCompatibility]
	public static int GetSurfacePriorityFromJob(this TileType t)
	{
		switch (t)
		{
		case TileType.greatWall:
			return 1000;
		case TileType.ancientCrystal:
			return 950;
		case TileType.ore:
			return 950;
		case TileType.wall:
		case TileType.thinWall:
			return 900;
		case TileType.fence:
			return 850;
		case TileType.bigRoot:
			return 800;
		case TileType.rail:
			return 790;
		case TileType.looseFlooring:
			return 780;
		case TileType.rug:
			return 770;
		case TileType.groundSlime:
			return 750;
		case TileType.floor:
			return 730;
		case TileType.litFloor:
			return 720;
		case TileType.circuitPlate:
			return 710;
		case TileType.chrysalis:
			return 700;
		case TileType.bridge:
			return 650;
		case TileType.ancientCircuitPlate:
			return 620;
		case TileType.wateredGround:
			return 600;
		case TileType.dugUpGround:
			return 500;
		case TileType.ground:
			return 400;
		case TileType.water:
			return 300;
		case TileType.pit:
			return 200;
		case TileType.none:
			return 100;
		default:
			return -1;
		}
	}

	public static TileData GetHighestSurfacePriority(List<TileData> tileTypes)
	{
		if (tileTypes == null)
		{
			return null;
		}
		TileData result = null;
		int num = int.MinValue;
		if (tileTypes != null)
		{
			foreach (TileData tileType in tileTypes)
			{
				if (num < SurfacePriorityLookup[(int)tileType.info.tileType])
				{
					result = tileType;
					num = SurfacePriorityLookup[(int)tileType.info.tileType];
				}
			}
		}
		return result;
	}

	public static bool IsDamageableTile(this TileType t)
	{
		if (t != TileType.greatWall && t != TileType.wall && t != TileType.thinWall && t != TileType.bigRoot && t != TileType.fence && t != TileType.floor && t != TileType.bridge && t != TileType.rug && t != TileType.groundSlime && t != TileType.chrysalis && t != TileType.circuitPlate && t != TileType.rail && t != TileType.litFloor)
		{
			return t == TileType.looseFlooring;
		}
		return true;
	}

	public static bool IsBlockingTile(this TileType t, bool includeLowColliders = true)
	{
		if (t != TileType.wall && t != TileType.thinWall && t != TileType.greatWall)
		{
			if (includeLowColliders)
			{
				if (t != TileType.bigRoot && t != TileType.water && t != TileType.fence)
				{
					return t == TileType.pit;
				}
				return true;
			}
			return false;
		}
		return true;
	}

	public static bool IsBlockingOnWalkableTile(this TileType t)
	{
		if (t != TileType.wall && t != TileType.thinWall && t != TileType.bigRoot && t != TileType.fence)
		{
			return t == TileType.greatWall;
		}
		return true;
	}

	public static bool IsBlockingParticlesTile(this TileType t)
	{
		if (t != TileType.wall && t != TileType.thinWall)
		{
			return t == TileType.greatWall;
		}
		return true;
	}

	public static bool BlockingAdaptsToAllTilesets(this TileType t)
	{
		return t == TileType.thinWall;
	}

	public static bool IsIgnoreClear(this TileType t)
	{
		return t == TileType.roofHole;
	}

	public static bool IsLowCollider(this TileType t)
	{
		if (t != TileType.bigRoot && t != TileType.fence && t != TileType.water)
		{
			return t == TileType.pit;
		}
		return true;
	}

	public static bool ShouldRerenderLights(this TileType t)
	{
		if (t != TileType.wall && t != TileType.ground)
		{
			return t == TileType.bridge;
		}
		return true;
	}

	public static TileCD GetBlockingTile(NativeArray<TileCD> tiles, bool includeLowColliders = true)
	{
		TileCD result = default(TileCD);
		for (int i = 0; i < tiles.Length; i++)
		{
			if (tiles[i].tileType.IsBlockingTile(includeLowColliders))
			{
				return tiles[i];
			}
		}
		return result;
	}

	public static bool HasThinCollider(this TileType t)
	{
		if (t != TileType.fence)
		{
			return t == TileType.thinWall;
		}
		return true;
	}

	public static bool HasMediumCollider(this TileType t)
	{
		return t == TileType.bigRoot;
	}

	public static bool IsContainedResource(this TileType t)
	{
		if (t != TileType.ore)
		{
			return t == TileType.ancientCrystal;
		}
		return true;
	}

	public static bool CanGrowOn(this TileType t)
	{
		if (t != TileType.ground && t != TileType.dugUpGround)
		{
			return t == TileType.wateredGround;
		}
		return true;
	}

	public static bool ShouldUseFenceLikeAdaption(this TileType t)
	{
		if (t != TileType.fence)
		{
			return t == TileType.thinWall;
		}
		return true;
	}

	public static bool ShouldUseFenceLikeAdaptionTowardsTileType(this TileType t, TileType tileType)
	{
		if (t == TileType.fence || t == TileType.thinWall)
		{
			return tileType == TileType.wall;
		}
		return false;
	}

	public static void GetNeededTile(this TileType t, ref NativeList<TileType> neededTile)
	{
		switch (t)
		{
		case TileType.wall:
		case TileType.thinWall:
		case TileType.circuitPlate:
		case TileType.ancientCircuitPlate:
		case TileType.floor:
		case TileType.fence:
		case TileType.rug:
		case TileType.smallStones:
		case TileType.debris:
		case TileType.rail:
		case TileType.litFloor:
		case TileType.debris2:
		case TileType.looseFlooring:
		case TileType.bigRoot:
		case TileType.groundSlime:
		case TileType.chrysalis:
		{
			TileType tileType = TileType.ground;
			neededTile.Add(ref tileType);
			tileType = TileType.bridge;
			neededTile.Add(ref tileType);
			break;
		}
		case TileType.dugUpGround:
		case TileType.smallGrass:
		case TileType.floorCrack:
		{
			TileType tileType = TileType.ground;
			neededTile.Add(ref tileType);
			break;
		}
		case TileType.wateredGround:
		{
			TileType tileType = TileType.ground;
			neededTile.Add(ref tileType);
			tileType = TileType.dugUpGround;
			neededTile.Add(ref tileType);
			break;
		}
		case TileType.wallGrass:
		case TileType.wallCrack:
		case TileType.ore:
		case TileType.ancientCrystal:
		{
			TileType tileType = TileType.wall;
			neededTile.Add(ref tileType);
			break;
		}
		case TileType.bridge:
		{
			TileType tileType = TileType.pit;
			neededTile.Add(ref tileType);
			tileType = TileType.water;
			neededTile.Add(ref tileType);
			break;
		}
		}
	}

	public static void GetInvalidTile(this TileType t, ref NativeList<TileType> invalidTile)
	{
		switch (t)
		{
		case TileType.ground:
		{
			TileType tileType = TileType.bridge;
			invalidTile.Add(ref tileType);
			break;
		}
		case TileType.bridge:
		{
			TileType tileType = TileType.ground;
			invalidTile.Add(ref tileType);
			break;
		}
		}
	}
}
