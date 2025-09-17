using System;
using System.Collections.Generic;
using Pug.Sprite;
using Pug.UnityExtensions;
using PugTilemap;
using UnityEngine;

[Serializable]
public class ObjectInfo
{
	public ObjectID objectID;

	public int initialAmount = 1;

	public int variation;

	public bool variationIsDynamic;

	public int variationToToggleTo;

	public ObjectType objectType;

	public List<ObjectCategoryTag> tags;

	public Rarity rarity;

	public int level;

	public int sellValue = -1;

	public float buyValueMultiplier = 1f;

	public Sprite icon;

	public Vector2 iconOffset;

	public SpriteAssetSkin iconSkinAsset;

	public Sprite smallIcon;

	public bool isStackable;

	public Vector2Int prefabTileSize = Vector2Int.one;

	public Vector2Int prefabCornerOffset;

	public bool centerIsAtEntityPosition;

	public List<Sprite> additionalSprites;

	[HideInInspector]
	public int tileset;

	[HideInInspector]
	public TileType tileType;

	public List<PrefabInfo> prefabInfos;

	public CraftingSettings craftingSettings;

	[ArrayElementTitle("objectID")]
	public List<CraftingObject> requiredObjectsToCraft;

	public float craftingTime;

	public bool appearInMapUI;

	public Color mapColor;

	public bool isCustomScenePrefab;

	[ArrayElementTitle("language, gender")]
	public List<LanguageGender> languageGenders;

	public Gender GetLanguageGender(string language)
	{
		foreach (LanguageGender languageGender in languageGenders)
		{
			if (languageGender.language.ToString() == language)
			{
				return languageGender.gender;
			}
		}
		return Gender.Neutral;
	}
}
