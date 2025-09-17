using System.Linq;
using System.Reflection;
using UnityEngine;

public static class ObjectLayerID
{
	public static int UILayerMask;

	public static readonly int Default = LayerMask.NameToLayer("Default");

	public static readonly int TransparentFX = LayerMask.NameToLayer("TransparentFX");

	public static readonly int Water = LayerMask.NameToLayer("Water");

	public static readonly int Wall = LayerMask.NameToLayer("Wall");

	public static readonly int Occluder = LayerMask.NameToLayer("Occluder");

	public static readonly int NoClip = LayerMask.NameToLayer("NoClip");

	public static readonly int UI = LayerMask.NameToLayer("UI");

	public static readonly int HUD = LayerMask.NameToLayer("HUD");

	public static readonly int NoCollisionNoRender = LayerMask.NameToLayer("NoCollisionNoRender");

	public static readonly int AgentPhysics = LayerMask.NameToLayer("AgentPhysics");

	public static readonly int Player = LayerMask.NameToLayer("Player");

	public static readonly int Flocking = LayerMask.NameToLayer("Flocking");

	public static readonly int PlayerDetector = LayerMask.NameToLayer("PlayerDetector");

	public static readonly int PlayerDamageable = LayerMask.NameToLayer("PlayerDamageable");

	public static readonly int AgentPhysicsNonSelfCollide = LayerMask.NameToLayer("AgentPhysicsNonSelfCollide");

	public static readonly int AgentDetector = LayerMask.NameToLayer("AgentDetector");

	public static readonly int FilteredQuad = LayerMask.NameToLayer("Filtered Quad");

	public static readonly int GroundShadow = LayerMask.NameToLayer("GroundShadow");

	public static void Init()
	{
		UILayerMask = 1 << UI;
	}

	public static string __debug_GetName(int hash)
	{
		FieldInfo fieldInfo = typeof(ObjectLayerID).GetFields(BindingFlags.Static | BindingFlags.Public).FirstOrDefault((FieldInfo q) => (int)q.GetValue(null) == hash);
		if (!(fieldInfo != null))
		{
			return "unknown:" + hash;
		}
		return fieldInfo.Name;
	}
}
