using System;

namespace PugTilemap;

public enum TileType
{
	none = 0,
	pit = 33,
	wall = 35,
	[Obsolete("Should only use roofHole")]
	roof = 36,
	roofHole = 37,
	thinWall = 40,
	ground = 46,
	dugUpGround = 49,
	wateredGround = 50,
	circuitPlate = 62,
	ancientCircuitPlate = 63,
	floor = 64,
	bridge = 65,
	fence = 66,
	rug = 67,
	smallStones = 68,
	smallGrass = 69,
	wallGrass = 70,
	debris = 71,
	floorCrack = 72,
	rail = 73,
	greatWall = 74,
	litFloor = 75,
	debris2 = 76,
	looseFlooring = 77,
	immune = 78,
	water = 126,
	wallCrack = 128,
	ore = 129,
	bigRoot = 131,
	groundSlime = 132,
	ancientCrystal = 133,
	chrysalis = 134,
	__max__ = 135,
	__illegal__ = -1
}
