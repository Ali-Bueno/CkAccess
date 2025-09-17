using System.Collections.Generic;
using Pug.UnityExtensions;
using UnityEngine;

[CreateAssetMenu(menuName = "Pug/Tables/WeaponEffectsTable", order = 4)]
public class WeaponEffectsTable : ScriptableObject
{
	[ArrayElementTitle("type")]
	public List<WeaponEffect> weaponEffects;

	public static ItemOverridesTable GetTable()
	{
		ItemOverridesTable itemOverridesTable = Resources.Load<ItemOverridesTable>("WeaponEffectsTable");
		if (itemOverridesTable == null)
		{
			Debug.LogError("Could not find WeaponEffectsTable asset");
		}
		return itemOverridesTable;
	}

	public Material GetWeaponEffectMaterial(WeaponEffectType type)
	{
		foreach (WeaponEffect weaponEffect in weaponEffects)
		{
			if (weaponEffect.type == type)
			{
				return weaponEffect.material;
			}
		}
		return null;
	}
}
