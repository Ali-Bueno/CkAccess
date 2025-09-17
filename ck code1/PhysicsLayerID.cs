public static class PhysicsLayerID
{
	public const uint Nothing = 0u;

	public const uint DefaultCollider = 1u;

	public const uint PlayerTrigger = 2u;

	public const uint PlayerCollider = 4u;

	public const uint EnemyTrigger = 8u;

	public const uint EnemyCollider = 16u;

	public const uint PickUpObject = 32u;

	public const uint DefaultTrigger = 64u;

	public const uint LightArea = 128u;

	public const uint DefaultLowCollider = 256u;

	public const uint DefaultLowTrigger = 512u;

	public const uint DefaultLowTriggerNonBlocking = 1024u;

	public const uint ProjectileTrigger = 2048u;

	public const uint ElectricalTrigger = 4096u;

	public const uint EnvironmentTrigger = 8192u;

	public const uint VelocityAffectorTrigger = 16384u;

	public const uint CritterCollider = 32768u;

	public const uint PlacementBlocker = 65536u;

	public const uint WaterCollider = 131072u;

	public const uint ShorelineCollider = 262144u;

	public const uint AllColliders = 131349u;

	public const uint AllBlocking = 131935u;

	public const uint AllTriggers = 32330u;

	public const uint AllDefaultColliders = 131329u;

	public const uint NonHittableLayers = 127008u;

	public const uint Everything = uint.MaxValue;
}
