using UnityEngine;

public static class ObjectLayerMask
{
	public static readonly LayerMask Default = 1 << ObjectLayerID.Default;

	public static readonly LayerMask NoClip = 1 << ObjectLayerID.NoClip;

	public static readonly LayerMask UI = 1 << ObjectLayerID.UI;

	public static readonly LayerMask HUD = 1 << ObjectLayerID.HUD;

	public static readonly LayerMask NoCollisionNoRender = 1 << ObjectLayerID.NoCollisionNoRender;

	public static readonly LayerMask AgentPhysics = 1 << ObjectLayerID.AgentPhysics;

	public static void Init()
	{
	}
}
