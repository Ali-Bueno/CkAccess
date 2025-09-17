public static class LevelScaling
{
	public static int GetLevelFromAreaLevelAndRarity(AreaLevel areaLevel, Rarity rarity)
	{
		int num = 0;
		switch (areaLevel)
		{
		case AreaLevel.StartArea:
			num = 1;
			break;
		case AreaLevel.Slime:
			num = 2;
			break;
		case AreaLevel.Clay:
			num = 4;
			break;
		case AreaLevel.LarvaHive:
			num = 6;
			break;
		case AreaLevel.Stone:
			num = 6;
			break;
		case AreaLevel.Nature:
			num = 8;
			break;
		case AreaLevel.Mold:
			num = 9;
			break;
		case AreaLevel.Sea:
			num = 10;
			break;
		case AreaLevel.City:
			num = 11;
			break;
		case AreaLevel.Desert:
			num = 12;
			break;
		case AreaLevel.Lava:
			num = 13;
			break;
		case AreaLevel.Crystal:
			num = 14;
			break;
		case AreaLevel.Passage:
			num = 15;
			break;
		case AreaLevel.Obsidian:
			num = 20;
			break;
		}
		return (int)(num + rarity);
	}

	public static int GetMaxLevel()
	{
		return GetLevelFromAreaLevelAndRarity(AreaLevel.Passage, Rarity.Legendary);
	}
}
