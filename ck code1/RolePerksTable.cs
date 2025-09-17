using System;
using System.Collections.Generic;
using Pug.UnityExtensions;
using UnityEngine;

[CreateAssetMenu(menuName = "Pug/Tables/RolePerksTable", order = 3)]
public class RolePerksTable : ScriptableObject
{
	[Serializable]
	public struct Perks
	{
		public CharacterRole role;

		public SkillID starterSkill;

		public List<ObjectData> starterItems;
	}

	[ArrayElementTitle("role")]
	public List<Perks> perks;

	public Perks GetPerks(CharacterRole role)
	{
		foreach (Perks perk in perks)
		{
			if (perk.role == role)
			{
				return perk;
			}
		}
		return default(Perks);
	}

	public CharacterRole GetRole(int index)
	{
		return perks[index].role;
	}

	public int GetIndex(CharacterRole role)
	{
		for (int i = 0; i < perks.Count; i++)
		{
			if (perks[i].role == role)
			{
				return i;
			}
		}
		return 0;
	}
}
