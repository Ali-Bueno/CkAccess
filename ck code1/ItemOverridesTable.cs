using System.Collections.Generic;
using Pug.UnityExtensions;
using UnityEngine;

[CreateAssetMenu(menuName = "Pug/Tables/ItemOverridesTable", order = 4)]
public class ItemOverridesTable : ScriptableObject
{
	[ArrayElementTitle("objectData.objectID, objectData.variation, objectData.amount")]
	public List<IconOverrides> iconOverrides;

	private Dictionary<ObjectID, List<IconOverrides>> _iconOverridesLookUpFromId;

	[ArrayElementTitle("objectData.objectID, objectData.variation")]
	public List<NameOverride> nameOverrides;

	private Dictionary<ObjectData, NameOverride> _nameOverridesLookUp;

	public static ItemOverridesTable GetTable()
	{
		ItemOverridesTable itemOverridesTable = Resources.Load<ItemOverridesTable>("ItemOverridesTable");
		if (itemOverridesTable == null)
		{
			Debug.LogError("Could not find ItemOverridesTable asset");
		}
		return itemOverridesTable;
	}

	public void Init()
	{
		_iconOverridesLookUpFromId = new Dictionary<ObjectID, List<IconOverrides>>();
		foreach (IconOverrides iconOverride in iconOverrides)
		{
			ObjectData objectData = default(ObjectData);
			objectData.objectID = iconOverride.objectData.objectID;
			objectData.variation = iconOverride.objectData.variation;
			ObjectData objectData2 = objectData;
			if (!_iconOverridesLookUpFromId.ContainsKey(objectData2.objectID))
			{
				_iconOverridesLookUpFromId.Add(objectData2.objectID, new List<IconOverrides>());
			}
			_iconOverridesLookUpFromId[objectData2.objectID].Add(iconOverride);
		}
		_nameOverridesLookUp = new Dictionary<ObjectData, NameOverride>();
		foreach (NameOverride nameOverride in nameOverrides)
		{
			ObjectData objectData = default(ObjectData);
			objectData.objectID = nameOverride.objectData.objectID;
			objectData.variation = nameOverride.objectData.variation;
			ObjectData key = objectData;
			if (!_nameOverridesLookUp.ContainsKey(key))
			{
				_nameOverridesLookUp.Add(key, nameOverride);
			}
			else
			{
				Debug.LogError("Duplicate name override for " + key.objectID.ToString() + " in ItemOverridesTable.asset");
			}
		}
	}

	public Sprite GetIconOverride(ObjectData objectData, bool getSmallIcon)
	{
		if (_iconOverridesLookUpFromId.ContainsKey(objectData.objectID))
		{
			foreach (IconOverrides item in _iconOverridesLookUpFromId[objectData.objectID])
			{
				if ((!item.amountMustMatch || item.objectData.amount == objectData.amount) && (!item.variationMustMatch || item.objectData.variation == objectData.variation))
				{
					return getSmallIcon ? item.smallIcon : item.icon;
				}
			}
		}
		return null;
	}

	public string GetNameTermOverride(ObjectData objectData)
	{
		if (_nameOverridesLookUp.ContainsKey(objectData))
		{
			return _nameOverridesLookUp[objectData].term;
		}
		return null;
	}
}
