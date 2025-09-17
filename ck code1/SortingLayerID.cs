using System.Linq;
using System.Reflection;
using UnityEngine;

public static class SortingLayerID
{
	public static readonly int Default = SortingLayer.NameToID("Default");

	public static readonly int Back = SortingLayer.NameToID("Back");

	public static readonly int Main = SortingLayer.NameToID("Main");

	public static readonly int Front = SortingLayer.NameToID("Front");

	public static readonly int GUI = SortingLayer.NameToID("GUI");

	public static readonly int PreGUI = SortingLayer.NameToID("PreGUI");

	public static void Init()
	{
	}

	public static string __debug_GetName(int hash)
	{
		FieldInfo fieldInfo = typeof(SortingLayerID).GetFields(BindingFlags.Static | BindingFlags.Public).FirstOrDefault((FieldInfo q) => (int)q.GetValue(null) == hash);
		if (!(fieldInfo != null))
		{
			return "unknown:" + hash;
		}
		return fieldInfo.Name;
	}
}
