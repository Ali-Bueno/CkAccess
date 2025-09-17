using System;

public enum Biome
{
	None,
	Slime,
	Larva,
	Stone,
	[Obsolete("Not used in full release world generation")]
	Obsidian,
	Nature,
	[Obsolete("Not used in full release world generation")]
	GreatWall,
	Sea,
	Desert,
	Crystal,
	Passage,
	__MAX_VALUE__
}
