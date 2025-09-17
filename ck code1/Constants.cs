using UnityEngine;

public static class Constants
{
	public const bool PARENT_AGENTS_TO_MAP = false;

	public const float kPlayerTileDamageConeAngle = 110f;

	public const float kDurabilityPercentageLostOnDeath = 0.15f;

	public const float kPushAwayFromFailedMiningForce = 0.3f;

	public const float kRecentlyAttackedTargetsTrackedDuration = 15f;

	public const float kGracePeriodToStorePrimarySlotInput = 0.2f;

	public const float kGracePeriodToStoreSecondarySlotInput = 0.1f;

	public const float kRangeWeaponBaseCooldown = 0.6f;

	public const float kMeleeBaseCooldown = 0.4f;

	public const float kGlobalAttackCooldown = 0.5f;

	public const float kGlobalCreativeGodModeAttackCooldown = 0.25f;

	public const float kGlobalCreativeGodModeCooldown = 0.15f;

	public const float kClientProjectilesRadiusObjectHitMultiplier = 0.1f;

	public const float hitDurationLifespan = 0.15f;

	public const float hitDelayLifespan = 1f / 15f;

	public const float kBaseTeleportDistance = 4f;

	public const float kOptimizedModeOutOfViewportPadding = 8f;

	public const float slimeBlobSpawnChance = 0.05f;

	public const float sqrDistanceFromCoreToAllowSpawningEnemies = 400f;

	public const float cavelingSpawnChance = 0.03f;

	public const float cavelingShamanSpawnChance = 0.01f;

	public const float cavelingBruteSpawnChance = 0.003f;

	public const float cavelingFarmerSpawnChance = 0.01f;

	public const float cavelingHunterSpawnChance = 0.01f;

	public const float customSceneSpawnChancePerAreaSlimeBiome = 1f;

	public const float customSceneSpawnChancePerAreaSeaBiome = 0.2f;

	public const float customSceneSpawnChancePerAreaDesertBiome = 0.2f;

	public const float customSceneSpawnChancePerArea = 0.5f;

	public const float vertexSkewAmount = 0.4f;

	public const float uvVertexShift = 0.25f;

	public const float CONDITION_MIN_DEV_MULT = 0.9f;

	public const float CONDITION_MAX_DEV_MULT = 1.1f;

	public const float kPoisonDuration = 15f;

	public const float kCharmDuration = 20f;

	public const float kPoisonHealReductionMultiplier = 0.25f;

	public const float kBurningTimeBetweenTicks = 2f;

	public const float kBurningDuration = 8.4f;

	public const float kVoidAuraTimeBetweenTicks = 1f;

	public const float kMoldDamageTimeBetweenTicks = 3f;

	public const float kMoldTimeBetweenIncrease = 5f;

	public const float kMoldTimeBetweenDecrease = 1f;

	public const float kAcidInitialDelay = 0.5f;

	public const float kAcidTimeBetweenTicks = 1f;

	public const float kSlowedDuration = 4f;

	public const float kSnareDuration = 4f;

	public const float kKnockbackStrength = 2f;

	public const float kMovementSpeedBoostDuration = 5f;

	public const float kAttackSpeedBoostDuration = 5f;

	public const float kSlipperyMovementDuration = 6f;

	public const float kStandStillDurationToGetDodge = 3f;

	public const float kRunConsistentlyDurationToGetMovementSpeed = 3f;

	public const float kDurationToLoseRunningBuffs = 0.75f;

	public const float kTimeToGetDamageIncreaseFromRunning = 3f;

	public const float kDamageIncreaseFromRunningDuration = 8f;

	public const float kAuraConditionsRange = 5f;

	public const float kRespawnImmuneToDamageTime = 2f;

	public const float kLoginImmuneToDamageTime = 2f;

	public const float kGainMeleeDamageFromHittingDuration = 8f;

	public const float kGainRangeDamageFromShootingDuration = 8f;

	public const float kChanceOnMeleeHitMeleeAttackSpeedDuration = 2f;

	public const float kChanceOnShotCriticalHitChanceDuration = 3f;

	public const float kChanceOnHitToIncreaseRangeDamageDuration = 10f;

	public const float kChanceOnShotToIncreaseMeleeDamageDuration = 10f;

	public const float kMeleeDamageIncreaseFromHittingSameTargetDuration = 3f;

	public const float kStunDuration = 2f;

	public const float kDamageIncreaseAsLowHealthThreshold = 0.3f;

	public const float kHealOverTimeDuration = 20f;

	public const float kHealTimeBetweenTicks = 1f;

	public const float kIncreasedBossDamageFromFishDuration = 60f;

	public const float kAttackSpeedFromCookedFoodDuration = 30f;

	public const float kDamageIncreaseFromMiningDuration = 5f;

	public const float kCritChanceIncreaseAfterApplyPoisonDuration = 5f;

	public const float kAttackSpeedAmountPerPercentageOfMissingHealth = 0.2f;

	public const float kBossProtectiveArmor = 0.3f;

	public const float kMagicBarrierRegenDelay = 8f;

	public const float kManaRegenDelay = 1f;

	public const float kManaRegenPerSecond = 10f;

	public const float kTimeBetweenManaTicks = 0.1f;

	public const float kManaRegenPerUpdate = 1f;

	public const float kMagicBarrierRegenBase = 0.1f;

	public const float kMagicDamageBoostDuration = 6f;

	public const float kSecondaryUseManaCostMultiplier = 2f;

	public const float kSummonMinionManaCostMultiplier = 2f;

	public const float networkSyncDelay = 0.6f;

	public const float extrapolatePlayerTarget = 0.3f;

	public const float kJoystickDeadzone = 0.25f;

	public const float damageMaxDeviation = 0.1f;

	public const float sqrDistanceFromCoreToAllowSpawningEnvironment = 144f;

	public const float priceValueMultiplier = 5f;

	public const float timeBetweenSaves = 60f;

	public const float weakWallHitThreshold = 12f;

	public const float thisWillTakeAWhileThreshold = 12f;

	public const float thisWillTakeForeverThreshold = 30f;

	public const float rareCookedFoodStatsMultiplier = 1.25f;

	public const float culinaryCookedFoodStatsMultiplier = 1.5f;

	public const float goldenPlantsOnlyStatsMultiplier = 1.15f;

	public const float minMaterialToGainFromSalvage = 0.3f;

	public const float maxMaterialToGainFromSalvage = 0.49f;

	public const float maxReinforceCost = 0.5f;

	public const float maxRepairCost = 1f;

	public const float reinforceDurabilityMultiplier = 2f;

	public const float reinforceStatMultiplier = 1.15f;

	public const float timeToForgetLastAttacker = 10f;

	public const float windupMultiplier = 2f;

	public const float additionalOreChance = 0.15f;

	public const float wallBossMaxSpeedIncreaseFromHealthReduction = 4f;

	public const float hardModeEnemyHealthMultiplier = 1.5f;

	public const float hardModeEnemyDamageMultiplier = 2f;

	public const float hardModeBossLootAmountMultiplier = 1.5f;

	public const float casualModeEnemyHealthMultiplier = 0.5f;

	public const float casualModeEnemyDamageMultiplier = 0.5f;

	public const float cameraSmoothSpeedWalking = 3.5f;

	public const float cameraSmoothSpeedMineCart = 7f;

	public const float cameraSmoothSpeedGoKart = 12f;

	public const float popUpBackgroundDefaultAlpha = 0.95f;

	public const float moveConditionThreshold = 0.01f;

	public const float kSulfurSetExplosionRadius = 2f;

	public const int playerBaseDamage = 10;

	public const int playerBaseMiningDamage = 20;

	public const int playerBaseHealth = 100;

	public const int playerBaseDiggingDamage = 20;

	public const int playerBaseMana = 100;

	public const int amountOfPvPTeamsBits = 3;

	public const int amountOfPvPTeams = 8;

	public const int undefinedPvPTeam = -1;

	public const int ffaPvPTeam = -2;

	public const int kMinionBaseHealth = 100;

	public const int kMinionHealthAdditionPerLevel = 50;

	public const int minionBaseDamage = 30;

	public const int minionBaseLifespan = 60;

	public const int inventoryMaxAmountPerSlot = 9999;

	public const int craftingChestMaxDistance = 10;

	public const int craftingChestMaxAmount = 20;

	public const int autoStackChestMaxDistance = 10;

	public const int autoStackChestMaxAmount = 20;

	public const int kSlimeSlowAmount = 500;

	public const int kAcidDamage = 12;

	public const int kLavaGroundSlimeBurnDamage = 40;

	public const int kStarvingMovementDecreaseThreshold = 25;

	public const int kWellFedMovementIncreaseThreshold = 95;

	public const int kStarvingHealthDecreaseThreshold = 25;

	public const int kWellFedHealthIncreaseThreshold = 75;

	public const int kStarvingDamageDecreaseThreshold = 25;

	public const int kWellFedDamageIncreaseThreshold = 75;

	public const int kStarvingMovementDecreasePerPoint = -12;

	public const int kWellFedMovementIncrease = 50;

	public const int kStarvingHealthDecreasePerHungerPoint = -1;

	public const int kWellFedHealthIncrease = 5;

	public const int kStarvingDamageDecreasePerHunterPoint = -10;

	public const int kWellFedDamageIncrease = 50;

	public const int kMoldMovementDecrease = -70;

	public const int kMoldMaxMovementDecrease = -700;

	public const int kFishingPerLevel = 20;

	public const int kMaxMovementSpeedDecrease = -900;

	public const int kCritIncreasePerStackFromKills = 3;

	public const int kCritIncreaseFromKillsMaxStacks = 5;

	public const int kCritIncreaseFromKillsDuration = 8;

	public const int kDamageIncreasePerGainMeleeDamageFromHittingStack = 20;

	public const int kDamageIncreasePerGainRangeDamageFromShootingStack = 20;

	public const int kChanceOnHitToIncreaseRangeDamageAmount = 300;

	public const int kChanceOnShotToIncreaseMeleeDamageAmount = 300;

	public const int kHealthRegenFromBeingAtLowHealth = 2;

	public const int kMaxDodgeChance = 90;

	public const int kMaxDamageReduction = 90;

	public const int kTotalBurningTicks = 4;

	public const int kMapPartsMaxFiles = 40;

	public const int kMaxMiningSkill = 100;

	public const int kMaxRunningSkill = 100;

	public const int kMaxMeleeSkill = 100;

	public const int kMaxVitalitySkill = 100;

	public const int kMaxBlacksmithingSkill = 100;

	public const int kMaxRangeSkill = 100;

	public const int kMaxGardeningSkill = 100;

	public const int kMaxFishingSkill = 100;

	public const int kMaxCookingSkill = 100;

	public const int kMaxMagicSkill = 100;

	public const int kMaxSummoningSkill = 100;

	public const int kMaxExplosivesSkill = 100;

	public const int kSkillPointsPerTalentPoint = 5;

	public const int kAdditionalTalentPointWhenMaxSkill = 5;

	public const int kStartingSkill = 3;

	public const int effectTextureWidth = 36;

	public const int effectTextureHeight = 24;

	public const int editorSimTickRate = 20;

	public const int editorNetworkSendRate = 20;

	public const int runtimeSimTickRate = 20;

	public const int runtimeNetworkSendRate = 20;

	public const int defaultSimDistance = 100;

	public const int minFishesPerShoal = 3;

	public const int maxFishesPerShoal = 6;

	public const int cattleHungryThreshold = 1;

	public const int maxCattleBreedingLimit = 50;

	public const int foodGainedPerEat = 1;

	public const int networkMaxPackets = 8;

	public const int maxNonModdedObjectID = 32767;

	public const int lowestObjectIDWithPossibleModdedObjects = 15503;

	public const bool kAgentsInstaDrown = true;

	public const bool kUseMousePositionOrRightJoystickForPlacement = true;

	public const bool kShowDateAndStartTextInTitleScreen = true;

	public const bool kShowUI = true;

	public const bool kUseMiniMap = true;

	public const string GLOBAL_OBJECTS_PATH = "Global Objects (Main Manager)";

	public const string SERVER_GLOBAL_OBJECTS_PATH = "ServerGlobalObjects";

	public const string REWIRED_INPUT_MANAGER_PATH = "Rewired Input Manager";

	public const string DEBUG_SETTINGS_PATH = "DebugSettings";

	public const string ICON_SPRITESHEET_RESOURCE_NAME = "MapWorkshop/MapWorkshopIcons";

	public const string MAP_WORKSHOP_PREFAB_BANK_RESOURCE_NAME = "MapWorkshop/LastUsedMapWorkshopPrefabBank";

	public const string MAP_PARTS_FILE_KEY = "MapPartsFileData";

	public const string CONTROL_MAPPING_USER_DATA_STORE_FILE_TARGET = "CoreKeeper_Controls.json";

	public const string CONTROL_MAPPING_USER_DATA_STORE_PLAYERPREFS_TARGET = "CoreKeeper_Controls";

	public const int CUSTOM_SCENE_TOTAL_TILE_SIZE = 200;

	public const int CUSTOM_SCENE_TILES_MIDDLE_INDEX = 100;

	public const int CUSTOM_SCENE_MAX_SUBMAPS_SIZE = 20;

	public const int CUSTOM_SCENE_SUBMAP_TILES_SIZE = 10;

	public const int CUSTOM_SCENE_SUBMAPS_MIDDLE_INDEX = 10;

	public const int SINGLE_PUGMAP_SIZE = 64;

	public const int ECS_SUBMAP_SIZE = 64;

	public const int ECS_SUBMAP_SIZE_HALF = 32;

	public const int ECS_SUBMAP_SIZE_LOG2 = 6;

	public const int ECS_SUBMAP_RESPAWN_PART_SIZE = 16;

	public const int ECS_SUBMAP_RESPAWN_PARTS_WIDTH = 4;

	public const int ECS_SUBMAP_RESPAWN_PARTS_AMOUNT = 16;

	public const int CLIENT_SUBMAP_SIZE_X = 64;

	public const int CLIENT_SUBMAP_SIZE_Y = 48;

	public const int BIOME_SAMPLING_INTERVAL_LOG2 = 4;

	public const int BIOME_SAMPLING_INTERVAL = 16;

	public const int CLIENT_BIOME_SAMPLES = 6;

	public const int PROCEDURAL_SPAWN_CELL_SUB_MAP_SIZE_LOG2 = 2;

	public const int PROCEDURAL_SPAWN_CELL_SUB_MAP_SIZE = 4;

	public const int PROCEDURAL_SPAWN_CELL_SIZE_LOG2 = 8;

	public const int PROCEDURAL_SPAWN_CELL_SIZE = 256;

	public const int SUB_MAPS_PER_PROCEDURAL_SPAWN_CELL = 16;

	public const int DISTANCE_FROM_PLAYER_TO_UPDATE_ENTITY = 40;

	public const int PLAYER_DISTANCE_TO_UNLOAD_ENTITIES = 300;

	public const int PLAYER_DISTANCE_TO_START_LOAD_ENTITIES = 250;

	public const int PLAYER_DISTANCE_TO_LOAD_ENTITIES = 200;

	public const int DISTANCE_TO_RESPAWN_ENVIRONMENT = 200;

	public const int UNLOADED_WORLD_SEGMENT_SIZE_LOG2 = 7;

	public const int PIXELS_PER_UNIT_I = 16;

	public const float PIXELS_PER_UNIT_F = 16f;

	public const double PIXELS_PER_UNIT_D = 16.0;

	public const float PIXEL_SIZE_F = 0.0625f;

	public const double PIXEL_SIZE_D = 0.0625;

	public const int kScreenPixelWidth = 480;

	public const int kScreenPixelHeight = 270;

	public const int kScreenWidth = 30;

	public const int kScreenHeight = 16;

	public const int kHalfScreenPixelWidth = 240;

	public const int kHalfScreenPixelHeight = 135;

	public const float kUnitWidth = 30f;

	public const float kUnitHeight = 16.875f;

	public const int kMinimumSupportedPixelPerfectResolutionWidth = 1920;

	public const int kMinimumSupportedPixelPerfectResolutionHeight = 1080;

	public const int kNumberOfSubPixels = 4;

	public static readonly Color RADICAL_PURPLE_G = new Color32(60, 0, 142, 73);

	public static readonly Color RADICAL_RED = new Color32(172, 50, 50, byte.MaxValue);

	public static readonly Color RADICAL_SKYBLUE = new Color32(99, 155, byte.MaxValue, byte.MaxValue);

	public static readonly Color RADICAL_PURPLE = new Color32(118, 66, 138, byte.MaxValue);

	public static readonly Color RADICAL_SALMON = new Color32(217, 87, 99, byte.MaxValue);

	public static readonly Color RADICAL_GREEN = new Color32(106, 190, 48, byte.MaxValue);

	public static readonly Color RADICAL_DARKBROWN = new Color32(102, 57, 49, byte.MaxValue);

	public static readonly Color RADICAL_YELLOW = new Color32(byte.MaxValue, 189, 10, byte.MaxValue);

	public static readonly Color RADICAL_PALEBLUE = new Color32(203, 219, 252, byte.MaxValue);

	public static readonly Color RADICAL_ORANGE = new Color32(223, 113, 38, byte.MaxValue);

	public static readonly Color RADICAL_TAN = new Color32(217, 160, 102, byte.MaxValue);

	public static readonly Color WHITE = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

	public static readonly Color BLACK = new Color32(0, 0, 0, byte.MaxValue);

	public static readonly Color GOOD_GREEN = new Color32(80, 240, 80, byte.MaxValue);

	public static readonly Color BAD_RED = new Color32(byte.MaxValue, 57, 57, byte.MaxValue);

	public static readonly Color[] MP_PLAYER_COLORS = new Color[4] { RADICAL_SKYBLUE, RADICAL_SALMON, RADICAL_GREEN, RADICAL_ORANGE };

	public static readonly Color[] ALL_RADICAL_COLORS = new Color[10] { RADICAL_RED, RADICAL_SKYBLUE, RADICAL_PURPLE, RADICAL_SALMON, RADICAL_GREEN, RADICAL_DARKBROWN, RADICAL_YELLOW, RADICAL_PALEBLUE, RADICAL_ORANGE, RADICAL_TAN };

	public static readonly int[] PRIMES = new int[342]
	{
		2, 3, 5, 7, 11, 13, 17, 19, 23, 29,
		31, 37, 41, 43, 47, 53, 59, 61, 67, 71,
		73, 79, 83, 89, 97, 101, 103, 107, 109, 113,
		127, 131, 137, 139, 149, 151, 157, 163, 167, 173,
		179, 181, 191, 193, 197, 199, 211, 223, 227, 229,
		233, 239, 241, 251, 257, 263, 269, 271, 277, 281,
		283, 293, 307, 311, 313, 317, 331, 337, 347, 349,
		353, 359, 367, 373, 379, 383, 389, 397, 401, 409,
		419, 421, 431, 433, 439, 443, 449, 457, 461, 463,
		467, 479, 487, 491, 499, 503, 509, 521, 523, 541,
		547, 557, 563, 569, 571, 577, 587, 593, 599, 601,
		607, 613, 617, 619, 631, 641, 643, 647, 653, 659,
		661, 673, 677, 683, 691, 701, 709, 719, 727, 733,
		739, 743, 751, 757, 761, 769, 773, 787, 797, 809,
		811, 821, 823, 827, 829, 839, 853, 857, 859, 863,
		877, 881, 883, 887, 907, 911, 919, 929, 937, 941,
		947, 953, 967, 971, 977, 983, 991, 997, 1009, 1013,
		1019, 1021, 1031, 1033, 1039, 1049, 1051, 1061, 1063, 1069,
		1087, 1091, 1093, 1097, 1103, 1109, 1117, 1123, 1129, 1151,
		1153, 1163, 1171, 1181, 1187, 1193, 1201, 1213, 1217, 1223,
		1229, 1231, 1237, 1249, 1259, 1277, 1279, 1283, 1289, 1291,
		1297, 1301, 1303, 1307, 1319, 1321, 1327, 1361, 1367, 1373,
		1381, 1399, 1409, 1423, 1427, 1429, 1433, 1439, 1447, 1451,
		1453, 1459, 1471, 1481, 1483, 1487, 1489, 1493, 1499, 1511,
		1523, 1531, 1543, 1549, 1553, 1559, 1567, 1571, 1579, 1583,
		1597, 1601, 1607, 1609, 1613, 1619, 1621, 1627, 1637, 1657,
		1663, 1667, 1669, 1693, 1697, 1699, 1709, 1721, 1723, 1733,
		1741, 1747, 1753, 1759, 1777, 1783, 1787, 1789, 1801, 1811,
		1823, 1831, 1847, 1861, 1867, 1871, 1873, 1877, 1879, 1889,
		1901, 1907, 1913, 1931, 1933, 1949, 1951, 1973, 1979, 1987,
		1993, 1997, 1999, 2003, 2011, 2017, 2027, 2029, 2039, 2053,
		2063, 2069, 2081, 2083, 2087, 2089, 2099, 2111, 2113, 2129,
		2131, 2137, 2141, 2143, 2153, 2161, 2179, 2203, 2207, 2213,
		2221, 2237, 2239, 2243, 2251, 2267, 2269, 2273, 2281, 2287,
		2293, 2297
	};

	public static readonly string[] paintableColorNames = new string[14]
	{
		"Yellow", "Green", "Red", "Purple", "Blue", "Brown", "White", "Black", "Orange", "Cyan",
		"Pink", "Grey", "Peach", "Teal"
	};

	public const int MAX_NUMBER_CHARACTERS = 30;

	public const int MAX_NUMBER_WORLDS = 30;

	public const int MAX_NUMBER_CREATIVE_CHARACTERS = 30;

	public static readonly Vector2[] kAnalogInputSnapTable = new Vector2[56]
	{
		Vector2.right,
		Vector2.right,
		Vector2.zero,
		Vector2.zero,
		Vector2.zero,
		Vector2.zero,
		(Vector2.right + Vector2.up).normalized,
		(Vector2.right + Vector2.up).normalized,
		Vector2.zero,
		Vector2.zero,
		Vector2.zero,
		Vector2.zero,
		Vector2.up,
		Vector2.up,
		Vector2.up,
		Vector2.up,
		Vector2.zero,
		Vector2.zero,
		Vector2.zero,
		Vector2.zero,
		(Vector2.left + Vector2.up).normalized,
		(Vector2.left + Vector2.up).normalized,
		Vector2.zero,
		Vector2.zero,
		Vector2.zero,
		Vector2.zero,
		Vector2.left,
		Vector2.left,
		Vector2.left,
		Vector2.left,
		Vector2.zero,
		Vector2.zero,
		Vector2.zero,
		Vector2.zero,
		(Vector2.left + Vector2.down).normalized,
		(Vector2.left + Vector2.down).normalized,
		Vector2.zero,
		Vector2.zero,
		Vector2.zero,
		Vector2.zero,
		Vector2.down,
		Vector2.down,
		Vector2.down,
		Vector2.down,
		Vector2.zero,
		Vector2.zero,
		Vector2.zero,
		Vector2.zero,
		(Vector2.right + Vector2.down).normalized,
		(Vector2.right + Vector2.down).normalized,
		Vector2.zero,
		Vector2.zero,
		Vector2.zero,
		Vector2.zero,
		Vector2.right,
		Vector2.right
	};
}
