using System;
using System.Collections.Generic;
using Pug.UnityExtensions;
using PugTilemap;
using UnityEngine;

[CreateAssetMenu(menuName = "Pug/Tables/FishingTable", order = 5)]
public class FishingTable : ScriptableObject
{
	[Serializable]
	public struct FishingInfo
	{
		public List<Biome> biomes;

		public List<Tileset> waterTilesets;

		public LootTableID lootTableID;

		public LootTableID fishLootTableID;
	}

	[Serializable]
	public struct FishStruggleInfo
	{
		public ObjectID fishID;

		[ArrayElementTitle("isStruggling, time")]
		public List<FishStruggleData> struggleData;

		public float difficultyRatio;
	}

	[Serializable]
	public struct FishStruggleData
	{
		public bool isStruggling;

		public float time;
	}

	[ArrayElementTitle("biomes")]
	public List<FishingInfo> fishingInfos;

	public Dictionary<Biome, FishingInfo> fishingInfoByBiome;

	public Dictionary<Tileset, FishingInfo> fishingInfoByWaterTileset;

	[ArrayElementTitle("fishID")]
	public List<FishStruggleInfo> fishStruggleInfos;

	public Dictionary<ObjectID, FishStruggleInfo> fishStruggleInfosLookUp;

	public readonly FishStruggleInfo defaultFishingStruggleInfo = new FishStruggleInfo
	{
		struggleData = new List<FishStruggleData>
		{
			new FishStruggleData
			{
				time = 2f,
				isStruggling = false
			},
			new FishStruggleData
			{
				time = 2f,
				isStruggling = true
			}
		}
	};

	private void OnValidate()
	{
		for (int i = 0; i < fishStruggleInfos.Count; i++)
		{
			FishStruggleInfo fishStruggleInfo = fishStruggleInfos[i];
			float num = 0f;
			float num2 = 0f;
			for (int j = 0; j < fishStruggleInfo.struggleData.Count; j++)
			{
				FishStruggleData fishStruggleData = fishStruggleInfo.struggleData[j];
				float num3 = fishStruggleData.time / (float)fishStruggleInfo.struggleData.Count;
				if (fishStruggleData.isStruggling)
				{
					num += num3;
				}
				else
				{
					num2 += num3;
				}
			}
			if (num <= 0f || num2 <= 0f)
			{
				Debug.LogError("Invalid fish pattern for fish " + fishStruggleInfo.fishID);
				continue;
			}
			float num4 = 1f;
			num4 = ((!(num >= num2)) ? ((0f - num2) / num) : (num / num2));
			fishStruggleInfos[i] = new FishStruggleInfo
			{
				fishID = fishStruggleInfo.fishID,
				struggleData = fishStruggleInfo.struggleData,
				difficultyRatio = num4
			};
		}
	}

	public void Init()
	{
		fishingInfoByBiome = new Dictionary<Biome, FishingInfo>();
		fishingInfoByWaterTileset = new Dictionary<Tileset, FishingInfo>();
		foreach (FishingInfo fishingInfo in fishingInfos)
		{
			foreach (Biome biome in fishingInfo.biomes)
			{
				if (!fishingInfoByBiome.ContainsKey(biome))
				{
					fishingInfoByBiome.Add(biome, fishingInfo);
				}
			}
			foreach (Tileset waterTileset in fishingInfo.waterTilesets)
			{
				if (!fishingInfoByWaterTileset.ContainsKey(waterTileset))
				{
					fishingInfoByWaterTileset.Add(waterTileset, fishingInfo);
				}
			}
			fishStruggleInfosLookUp = new Dictionary<ObjectID, FishStruggleInfo>();
			foreach (FishStruggleInfo fishStruggleInfo in fishStruggleInfos)
			{
				if (!fishStruggleInfosLookUp.ContainsKey(fishStruggleInfo.fishID))
				{
					fishStruggleInfosLookUp.Add(fishStruggleInfo.fishID, fishStruggleInfo);
				}
			}
		}
	}

	public static FishingTable GetTable()
	{
		FishingTable fishingTable = Resources.Load<FishingTable>("FishingTable");
		if (fishingTable == null)
		{
			Debug.LogError("Could not find FishingTable asset");
		}
		return fishingTable;
	}

	public FishStruggleInfo GetFishStruggleInfo(ObjectID fishID)
	{
		if (fishStruggleInfosLookUp.ContainsKey(fishID))
		{
			return fishStruggleInfosLookUp[fishID];
		}
		return defaultFishingStruggleInfo;
	}

	public FishingInfo GetFishingInfoFromWaterTileset(Tileset tileset)
	{
		if (fishingInfoByWaterTileset.ContainsKey(tileset))
		{
			return fishingInfoByWaterTileset[tileset];
		}
		return default(FishingInfo);
	}

	public FishingInfo GetFishingInfoFromBiome(Biome biome)
	{
		if (fishingInfoByBiome.ContainsKey(biome))
		{
			return fishingInfoByBiome[biome];
		}
		return fishingInfoByBiome[Biome.None];
	}

	private static Tileset BiomeToWaterTileset(Biome biome)
	{
		switch (biome)
		{
		case Biome.Slime:
		case Biome.Larva:
			return Tileset.Dirt;
		case Biome.Stone:
			return Tileset.Stone;
		case Biome.Nature:
			return Tileset.Nature;
		case Biome.Sea:
			return Tileset.Sea;
		case Biome.Desert:
			return Tileset.Desert;
		case Biome.Crystal:
			return Tileset.Crystal;
		case Biome.Passage:
			return Tileset.Passage;
		default:
			return Tileset.Dirt;
		}
	}

	private static AreaLevel WaterTilesetToAreaLevel(Tileset tileset)
	{
		return tileset switch
		{
			Tileset.Dirt => AreaLevel.Slime, 
			Tileset.LarvaHive => AreaLevel.Clay, 
			Tileset.Stone => AreaLevel.Stone, 
			Tileset.Nature => AreaLevel.Nature, 
			Tileset.Mold => AreaLevel.Mold, 
			Tileset.Sea => AreaLevel.Sea, 
			Tileset.Desert => AreaLevel.Desert, 
			Tileset.Lava => AreaLevel.Lava, 
			Tileset.Crystal => AreaLevel.Crystal, 
			Tileset.Passage => AreaLevel.Passage, 
			_ => AreaLevel.Slime, 
		};
	}

	public static int GetSkillRequiredForBiome(Biome biome)
	{
		return GetSkillRequiredForWater(BiomeToWaterTileset(biome));
	}

	public static int GetSkillRequiredForWater(Tileset waterTileset)
	{
		return (LevelScaling.GetLevelFromAreaLevelAndRarity(WaterTilesetToAreaLevel(waterTileset), Rarity.Common) - 1) * 20;
	}

	public static float FishingValueToLevel(int fishingValue)
	{
		return (float)fishingValue / 20f;
	}
}
