using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Pug/Tables/ExtraLootAtStartTable", order = 3)]
public class ExtraLootAtStartTable : ScriptableObject
{
	[Serializable]
	public struct Loot
	{
		public App needsApp;

		public Dlc needsDlc;

		public List<ObjectData> objects;
	}

	public List<Loot> loot;
}
