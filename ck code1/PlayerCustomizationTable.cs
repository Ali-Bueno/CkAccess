using System.Collections.Generic;
using System.Linq;
using Pug.UnityExtensions;
using UnityEngine;

[CreateAssetMenu(menuName = "Pug/Tables/PlayerCustomizationTable", order = 4)]
public class PlayerCustomizationTable : ScriptableObject, ISerializationCallbackReceiver
{
	public enum CustomizableBodyPartType
	{
		GENDER,
		SKIN_COLOR,
		HAIR,
		HAIR_SHADE,
		HAIR_COLOR,
		HAIR_SHADE_COLOR,
		EYES,
		EYES_COLOR,
		SHIRT,
		PANTS,
		HELM,
		BREAST_ARMOR,
		PANTS_ARMOR,
		PANTS_COLOR,
		SHIRT_COLOR,
		BODY
	}

	[ArrayElementTitle("id")]
	public List<BodySkin> bodySkins;

	public ReorderableColorReplacementData skinColors;

	[ArrayElementTitle("id")]
	public List<HairSkin> hairSkins;

	public ReorderableColorReplacementData hairColors;

	public ReorderableColorReplacementData hairShadeColors;

	[ArrayElementTitle("id")]
	public List<EyesSkin> eyeSkins;

	public ReorderableColorReplacementData eyeColors;

	[ArrayElementTitle("id")]
	public List<ShirtSkin> shirtSkins;

	[ArrayElementTitle("id")]
	public List<PantsSkin> pantsSkins;

	[ArrayElementTitle("id")]
	public List<HelmSkin> helmSkins;

	[ArrayElementTitle("id")]
	public List<BreastArmorSkin> breastArmorSkins;

	[ArrayElementTitle("id")]
	public List<PantsArmorSkin> pantsArmorSkins;

	[HideInInspector]
	public List<BodySkin> bodySkinsSorted;

	[HideInInspector]
	public ColorReplacementData skinColorsSorted;

	[HideInInspector]
	public List<HairSkin> hairSkinsSorted;

	[HideInInspector]
	public ColorReplacementData hairColorsSorted;

	[HideInInspector]
	public ColorReplacementData hairShadeColorsSorted;

	[HideInInspector]
	public List<EyesSkin> eyeSkinsSorted;

	[HideInInspector]
	public ColorReplacementData eyeColorsSorted;

	[HideInInspector]
	public List<ShirtSkin> shirtSkinsSorted;

	[HideInInspector]
	public List<PantsSkin> pantsSkinsSorted;

	[HideInInspector]
	public List<HelmSkin> helmSkinsSorted;

	[HideInInspector]
	public List<BreastArmorSkin> breastArmorSkinsSorted;

	[HideInInspector]
	public List<PantsArmorSkin> pantsArmorSkinsSorted;

	public void OnBeforeSerialize()
	{
		List<List<SkinBase>> obj = new List<List<SkinBase>>
		{
			bodySkins.Cast<SkinBase>().ToList(),
			skinColors.replacementColors.Cast<SkinBase>().ToList(),
			hairSkins.Cast<SkinBase>().ToList(),
			hairColors.replacementColors.Cast<SkinBase>().ToList(),
			hairShadeColors.replacementColors.Cast<SkinBase>().ToList(),
			eyeSkins.Cast<SkinBase>().ToList(),
			eyeColors.replacementColors.Cast<SkinBase>().ToList(),
			shirtSkins.Cast<SkinBase>().ToList(),
			pantsSkins.Cast<SkinBase>().ToList(),
			helmSkins.Cast<SkinBase>().ToList(),
			breastArmorSkins.Cast<SkinBase>().ToList(),
			pantsArmorSkins.Cast<SkinBase>().ToList()
		};
		HashSet<int> hashSet = new HashSet<int>();
		foreach (List<SkinBase> item in obj)
		{
			foreach (SkinBase item2 in item)
			{
				item2.id = GetAvailableID(hashSet, item2);
				hashSet.Add(item2.id);
			}
			hashSet.Clear();
		}
		foreach (ShirtSkin shirtSkin in shirtSkins)
		{
			foreach (ReorderableColorList replacementColor in shirtSkin.colorReplacementData.replacementColors)
			{
				replacementColor.id = GetAvailableID(hashSet, replacementColor);
				hashSet.Add(replacementColor.id);
			}
			hashSet.Clear();
		}
		foreach (PantsSkin pantsSkin in pantsSkins)
		{
			foreach (ReorderableColorList replacementColor2 in pantsSkin.colorReplacementData.replacementColors)
			{
				replacementColor2.id = GetAvailableID(hashSet, replacementColor2);
				hashSet.Add(replacementColor2.id);
			}
			hashSet.Clear();
		}
		bodySkinsSorted = new List<BodySkin>();
		foreach (BodySkin bodySkin in bodySkins)
		{
			bodySkinsSorted.Add(bodySkin.Clone());
		}
		bodySkinsSorted.Sort((BodySkin a, BodySkin b) => a.id.CompareTo(b.id));
		skinColorsSorted = skinColors.GetSortedColorReplacementData();
		hairSkinsSorted = new List<HairSkin>();
		foreach (HairSkin hairSkin in hairSkins)
		{
			hairSkinsSorted.Add(hairSkin.Clone());
		}
		hairSkinsSorted.Sort((HairSkin a, HairSkin b) => a.id.CompareTo(b.id));
		hairColorsSorted = hairColors.GetSortedColorReplacementData();
		hairShadeColorsSorted = hairShadeColors.GetSortedColorReplacementData();
		eyeSkinsSorted = new List<EyesSkin>();
		foreach (EyesSkin eyeSkin in eyeSkins)
		{
			eyeSkinsSorted.Add(eyeSkin.Clone());
		}
		eyeSkinsSorted.Sort((EyesSkin a, EyesSkin b) => a.id.CompareTo(b.id));
		eyeColorsSorted = eyeColors.GetSortedColorReplacementData();
		shirtSkinsSorted = new List<ShirtSkin>();
		foreach (ShirtSkin shirtSkin2 in shirtSkins)
		{
			shirtSkinsSorted.Add(shirtSkin2.Clone());
		}
		shirtSkinsSorted.Sort((ShirtSkin a, ShirtSkin b) => a.id.CompareTo(b.id));
		pantsSkinsSorted = new List<PantsSkin>();
		foreach (PantsSkin pantsSkin2 in pantsSkins)
		{
			pantsSkinsSorted.Add(pantsSkin2.Clone());
		}
		pantsSkinsSorted.Sort((PantsSkin a, PantsSkin b) => a.id.CompareTo(b.id));
		helmSkinsSorted = new List<HelmSkin>();
		foreach (HelmSkin helmSkin in helmSkins)
		{
			helmSkinsSorted.Add(helmSkin.Clone());
		}
		helmSkinsSorted.Sort((HelmSkin a, HelmSkin b) => a.id.CompareTo(b.id));
		breastArmorSkinsSorted = new List<BreastArmorSkin>();
		foreach (BreastArmorSkin breastArmorSkin in breastArmorSkins)
		{
			breastArmorSkinsSorted.Add(breastArmorSkin.Clone());
		}
		breastArmorSkinsSorted.Sort((BreastArmorSkin a, BreastArmorSkin b) => a.id.CompareTo(b.id));
		pantsArmorSkinsSorted = new List<PantsArmorSkin>();
		foreach (PantsArmorSkin pantsArmorSkin in pantsArmorSkins)
		{
			pantsArmorSkinsSorted.Add(pantsArmorSkin.Clone());
		}
		pantsArmorSkinsSorted.Sort((PantsArmorSkin a, PantsArmorSkin b) => a.id.CompareTo(b.id));
	}

	private int GetAvailableID(HashSet<int> busyIds, SkinBase skin)
	{
		if (busyIds.Contains(skin.id))
		{
			for (int i = 0; i < int.MaxValue; i++)
			{
				if (!busyIds.Contains(i))
				{
					busyIds.Add(i);
					return i;
				}
			}
			Debug.LogError("Could not find any available id.");
			return skin.id;
		}
		return skin.id;
	}

	public void OnAfterDeserialize()
	{
	}

	public int GetMaxVariations(CustomizableBodyPartType bodyPartType, int skinId)
	{
		switch (bodyPartType)
		{
		case CustomizableBodyPartType.GENDER:
		case CustomizableBodyPartType.BODY:
			return bodySkinsSorted.Count;
		case CustomizableBodyPartType.SKIN_COLOR:
			return skinColorsSorted.replacementColors.Count + 1;
		case CustomizableBodyPartType.HAIR:
			return hairSkinsSorted.Count;
		case CustomizableBodyPartType.HAIR_COLOR:
			return hairColorsSorted.replacementColors.Count + 1;
		case CustomizableBodyPartType.HAIR_SHADE:
			return hairSkinsSorted.Count;
		case CustomizableBodyPartType.HAIR_SHADE_COLOR:
			return hairShadeColorsSorted.replacementColors.Count + 1;
		case CustomizableBodyPartType.EYES:
			return eyeSkinsSorted.Count;
		case CustomizableBodyPartType.EYES_COLOR:
			return eyeColorsSorted.replacementColors.Count + 1;
		case CustomizableBodyPartType.SHIRT:
			return shirtSkinsSorted.Count;
		case CustomizableBodyPartType.SHIRT_COLOR:
			return shirtSkinsSorted[0].colorReplacementDataSorted.replacementColors.Count + 1;
		case CustomizableBodyPartType.PANTS:
			return pantsSkinsSorted.Count;
		case CustomizableBodyPartType.PANTS_COLOR:
			return pantsSkinsSorted[0].colorReplacementDataSorted.replacementColors.Count + 1;
		case CustomizableBodyPartType.HELM:
			return helmSkinsSorted.Count;
		case CustomizableBodyPartType.BREAST_ARMOR:
			return breastArmorSkinsSorted.Count;
		case CustomizableBodyPartType.PANTS_ARMOR:
			return pantsArmorSkinsSorted.Count;
		default:
			Debug.LogError("Couldnt find max variations for " + bodyPartType);
			return 0;
		}
	}

	public HelmHairType GetHelmHairType(int helmId)
	{
		if (helmId >= 0 && helmId < helmSkinsSorted.Count)
		{
			return helmSkinsSorted[helmId].hairType;
		}
		return HelmHairType.Hide;
	}

	public ShirtVisibility GetShirtVisibility(int breastArmorId)
	{
		if (breastArmorId >= 0 && breastArmorId < breastArmorSkinsSorted.Count)
		{
			return breastArmorSkinsSorted[breastArmorId].shirtVisibility;
		}
		return ShirtVisibility.Hide;
	}

	public PantsVisibility GetPantsVisibility(int pantsArmorId)
	{
		if (pantsArmorId >= 0 && pantsArmorId < pantsArmorSkinsSorted.Count)
		{
			return pantsArmorSkinsSorted[pantsArmorId].pantsVisibility;
		}
		return PantsVisibility.Hide;
	}

	public Texture2D GetSkinTexture(CustomizableBodyPartType bodyPartType, int index, HelmHairType activeHelmHairType = HelmHairType.FullyShow, int gender = 0, ShirtVisibility shirtVisibility = ShirtVisibility.FullyShow, PantsVisibility pantsVisibility = PantsVisibility.FullyShow)
	{
		if (GetMaxVariations(bodyPartType, index) <= index)
		{
			Debug.LogError("Texture id " + index + " does not exist for " + bodyPartType);
			return null;
		}
		switch (bodyPartType)
		{
		case CustomizableBodyPartType.HAIR:
			return activeHelmHairType switch
			{
				HelmHairType.Hide => null, 
				HelmHairType.PartlyShown => hairSkinsSorted[index].helmHairTexture, 
				_ => hairSkinsSorted[index].hairTexture, 
			};
		case CustomizableBodyPartType.HAIR_SHADE:
			return hairSkinsSorted[index].hairShadeTexture;
		case CustomizableBodyPartType.EYES:
			return eyeSkinsSorted[index].eyesTexture;
		case CustomizableBodyPartType.SHIRT:
			if (shirtVisibility == ShirtVisibility.Hide)
			{
				return null;
			}
			if (gender != 0)
			{
				return shirtSkinsSorted[index].femaleShirtTexture;
			}
			return shirtSkinsSorted[index].maleShirtTexture;
		case CustomizableBodyPartType.PANTS:
			if (pantsVisibility == PantsVisibility.Hide)
			{
				return null;
			}
			return pantsSkinsSorted[index].pantsTexture;
		case CustomizableBodyPartType.HELM:
			return helmSkinsSorted[index].helmTexture;
		case CustomizableBodyPartType.BREAST_ARMOR:
			return breastArmorSkinsSorted[index].breastTexture;
		case CustomizableBodyPartType.PANTS_ARMOR:
			return pantsArmorSkinsSorted[index].pantsTexture;
		case CustomizableBodyPartType.BODY:
			return bodySkinsSorted[index].bodyTexture;
		default:
			Debug.LogError("No texture variation exists for " + bodyPartType);
			return null;
		}
	}

	public Texture2D GetEmissiveTexture(CustomizableBodyPartType bodyPartType, int skinIndex, HelmHairType activeHelmHairType = HelmHairType.FullyShow, int gender = 0, ShirtVisibility shirtVisibility = ShirtVisibility.FullyShow, PantsVisibility pantsVisibility = PantsVisibility.FullyShow)
	{
		if (GetMaxVariations(bodyPartType, skinIndex) <= skinIndex)
		{
			Debug.LogError("Texture id " + skinIndex + " does not exist for " + bodyPartType);
			return null;
		}
		return bodyPartType switch
		{
			CustomizableBodyPartType.HELM => helmSkinsSorted[skinIndex].emissiveHelmTexture, 
			CustomizableBodyPartType.BREAST_ARMOR => breastArmorSkinsSorted[skinIndex].emissiveBreastTexture, 
			CustomizableBodyPartType.PANTS_ARMOR => pantsArmorSkinsSorted[skinIndex].emissivePantsTexture, 
			_ => null, 
		};
	}

	public ColorReplacementData GetColorReplacementData(CustomizableBodyPartType bodyPartType, int skinIndex)
	{
		switch (bodyPartType)
		{
		case CustomizableBodyPartType.GENDER:
		case CustomizableBodyPartType.SKIN_COLOR:
			return skinColorsSorted;
		case CustomizableBodyPartType.HAIR_SHADE_COLOR:
			return hairShadeColorsSorted;
		case CustomizableBodyPartType.HAIR_COLOR:
			return hairColorsSorted;
		case CustomizableBodyPartType.EYES_COLOR:
			return eyeColorsSorted;
		case CustomizableBodyPartType.SHIRT_COLOR:
			return shirtSkinsSorted[skinIndex].colorReplacementDataSorted;
		case CustomizableBodyPartType.PANTS_COLOR:
			return pantsSkinsSorted[skinIndex].colorReplacementDataSorted;
		default:
			Debug.LogError("No color replacer data exists for " + bodyPartType);
			return null;
		}
	}

	public Vector3 GetPixelOffset(CustomizableBodyPartType bodyPartType, int index)
	{
		if (GetMaxVariations(bodyPartType, index) <= index)
		{
			Debug.LogError("Pixel offset id " + index + " does not exist for " + bodyPartType);
			return Vector3.zero;
		}
		Vector3 vector = Vector3.zero;
		if (bodyPartType == CustomizableBodyPartType.HELM)
		{
			vector = new Vector3(helmSkinsSorted[index].pixelOffset.x, helmSkinsSorted[index].pixelOffset.y, 0f);
		}
		else
		{
			Debug.LogError("Pixel offset has not been set up for " + bodyPartType);
		}
		return vector * 0.0625f;
	}

	public int GetIndexFromId(CustomizableBodyPartType bodyPartType, int id)
	{
		if (GetMaxVariations(bodyPartType, id) <= id)
		{
			Debug.LogError("Id " + id + " does not exist for " + bodyPartType);
			return 0;
		}
		switch (bodyPartType)
		{
		case CustomizableBodyPartType.GENDER:
		case CustomizableBodyPartType.BODY:
			return bodySkins.FindIndex((BodySkin x) => x.id == id);
		case CustomizableBodyPartType.SKIN_COLOR:
			if (id == 0)
			{
				return 0;
			}
			return skinColors.replacementColors.FindIndex((ReorderableColorList x) => x.id == id - 1) + 1;
		case CustomizableBodyPartType.HAIR:
			return hairSkins.FindIndex((HairSkin x) => x.id == id);
		case CustomizableBodyPartType.HAIR_COLOR:
			if (id == 0)
			{
				return 0;
			}
			return hairColors.replacementColors.FindIndex((ReorderableColorList x) => x.id == id - 1) + 1;
		case CustomizableBodyPartType.HAIR_SHADE:
			return hairSkins.FindIndex((HairSkin x) => x.id == id);
		case CustomizableBodyPartType.HAIR_SHADE_COLOR:
			if (id == 0)
			{
				return 0;
			}
			return hairShadeColors.replacementColors.FindIndex((ReorderableColorList x) => x.id == id - 1) + 1;
		case CustomizableBodyPartType.EYES:
			return eyeSkins.FindIndex((EyesSkin x) => x.id == id);
		case CustomizableBodyPartType.EYES_COLOR:
			if (id == 0)
			{
				return 0;
			}
			return eyeColors.replacementColors.FindIndex((ReorderableColorList x) => x.id == id - 1) + 1;
		case CustomizableBodyPartType.SHIRT:
			return shirtSkins.FindIndex((ShirtSkin x) => x.id == id);
		case CustomizableBodyPartType.SHIRT_COLOR:
			return shirtSkins[0].colorReplacementData.replacementColors.FindIndex((ReorderableColorList x) => x.id == id - 1) + 1;
		case CustomizableBodyPartType.PANTS:
			return pantsSkins.FindIndex((PantsSkin x) => x.id == id);
		case CustomizableBodyPartType.PANTS_COLOR:
			return pantsSkins[0].colorReplacementData.replacementColors.FindIndex((ReorderableColorList x) => x.id == id - 1) + 1;
		case CustomizableBodyPartType.HELM:
			return helmSkins.FindIndex((HelmSkin x) => x.id == id);
		case CustomizableBodyPartType.BREAST_ARMOR:
			return breastArmorSkins.FindIndex((BreastArmorSkin x) => x.id == id);
		case CustomizableBodyPartType.PANTS_ARMOR:
			return pantsArmorSkins.FindIndex((PantsArmorSkin x) => x.id == id);
		default:
			Debug.LogError("No ids variation exists for " + bodyPartType);
			return 0;
		}
	}

	public int GetIdFromIndex(CustomizableBodyPartType bodyPartType, int index)
	{
		if (GetMaxVariations(bodyPartType, index) <= index)
		{
			Debug.LogError("Index " + index + " does not exist for " + bodyPartType);
			return 0;
		}
		switch (bodyPartType)
		{
		case CustomizableBodyPartType.GENDER:
		case CustomizableBodyPartType.BODY:
			return bodySkins[index].id;
		case CustomizableBodyPartType.SKIN_COLOR:
			if (index == 0)
			{
				return 0;
			}
			return skinColors.replacementColors[index - 1].id + 1;
		case CustomizableBodyPartType.HAIR:
			return hairSkins[index].id;
		case CustomizableBodyPartType.HAIR_COLOR:
			if (index == 0)
			{
				return 0;
			}
			return hairColors.replacementColors[index - 1].id + 1;
		case CustomizableBodyPartType.HAIR_SHADE:
			return hairSkins[index].id;
		case CustomizableBodyPartType.HAIR_SHADE_COLOR:
			if (index == 0)
			{
				return 0;
			}
			return hairShadeColors.replacementColors[index - 1].id + 1;
		case CustomizableBodyPartType.EYES:
			return eyeSkins[index].id;
		case CustomizableBodyPartType.EYES_COLOR:
			if (index == 0)
			{
				return 0;
			}
			return eyeColors.replacementColors[index - 1].id + 1;
		case CustomizableBodyPartType.SHIRT:
			return shirtSkins[index].id;
		case CustomizableBodyPartType.SHIRT_COLOR:
			if (index == 0)
			{
				return 0;
			}
			return shirtSkins[0].colorReplacementData.replacementColors[index - 1].id + 1;
		case CustomizableBodyPartType.PANTS:
			return pantsSkins[index].id;
		case CustomizableBodyPartType.PANTS_COLOR:
			if (index == 0)
			{
				return 0;
			}
			return pantsSkins[0].colorReplacementData.replacementColors[index - 1].id + 1;
		case CustomizableBodyPartType.HELM:
			return helmSkins[index].id;
		case CustomizableBodyPartType.BREAST_ARMOR:
			return breastArmorSkins[index].id;
		case CustomizableBodyPartType.PANTS_ARMOR:
			return pantsArmorSkins[index].id;
		default:
			Debug.LogError("No ids variation exists for " + bodyPartType);
			return 0;
		}
	}
}
