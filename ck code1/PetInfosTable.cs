using System;
using System.Collections.Generic;
using Pug.UnityExtensions;
using UnityEngine;

[CreateAssetMenu(menuName = "Pug/Tables/PetInfosTable", order = 4)]
public class PetInfosTable : ScriptableObject
{
	[Serializable]
	public struct PetTalentInfo
	{
		public PetTalent petTalentID;

		public ConditionID conditionID;

		public int value;

		public int buffValue;

		public Sprite meleeIcon;

		public Sprite rangeIcon;

		public Sprite buffIcon;

		[ArrayElementTitle("petId, multiplier")]
		public List<PetTalentMultiplierOverride> multiplierOverrides;

		public Sprite GetIcon(PetType petType)
		{
			return petType switch
			{
				PetType.Melee => meleeIcon, 
				PetType.Range => rangeIcon, 
				PetType.Buff => buffIcon, 
				_ => meleeIcon, 
			};
		}
	}

	[Serializable]
	public struct PetTalentInfoBlittable
	{
		public PetTalent petTalentID;

		public ConditionID conditionID;

		public int value;

		public int buffValue;
	}

	[Serializable]
	public struct PetTalentMultiplierOverride
	{
		public ObjectID petId;

		public float multiplier;
	}

	[Serializable]
	public class PetSkinInfo
	{
		public ObjectID petId;

		public List<PetSkin> skins;
	}

	[Serializable]
	public class PetSkin
	{
		public Texture2D primaryGradientMap;
	}

	[ArrayElementTitle("petTalentID")]
	public List<PetTalentInfo> petTalents;

	private Dictionary<PetTalent, PetTalentInfo> petTalentsLookUp;

	[ArrayElementTitle("petId")]
	public List<PetSkinInfo> petSkins;

	private Dictionary<ObjectID, PetSkinInfo> petSkinsLookUp;

	public static PetInfosTable GetTable()
	{
		PetInfosTable petInfosTable = Resources.Load<PetInfosTable>("PetInfosTable");
		if (petInfosTable == null)
		{
			Debug.LogError("Could not find PetInfosTable asset");
		}
		return petInfosTable;
	}

	private void OnValidate()
	{
		petTalentsLookUp = null;
	}

	public PetTalentInfo GetTalent(PetTalent petTalentId)
	{
		if (petTalentsLookUp == null)
		{
			petTalentsLookUp = new Dictionary<PetTalent, PetTalentInfo>();
			foreach (PetTalentInfo petTalent in petTalents)
			{
				petTalentsLookUp.Add(petTalent.petTalentID, petTalent);
			}
		}
		return petTalentsLookUp[petTalentId];
	}

	public PetSkinInfo GetPetSkinInfo(ObjectID objectID)
	{
		if (petSkinsLookUp == null)
		{
			petSkinsLookUp = new Dictionary<ObjectID, PetSkinInfo>();
			foreach (PetSkinInfo petSkin in petSkins)
			{
				petSkinsLookUp.Add(petSkin.petId, petSkin);
			}
		}
		if (petSkinsLookUp.ContainsKey(objectID))
		{
			return petSkinsLookUp[objectID];
		}
		return null;
	}
}
