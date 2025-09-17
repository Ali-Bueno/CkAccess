public class Melody
{
	public readonly MelodyID id;

	public readonly int[] melody;

	public readonly float[] durations;

	public readonly float durationMod;

	public bool autoplay;

	public int this[int i] => melody[i];

	public int Length => melody.Length;

	public int DurationsLength => durations.Length;

	public Melody()
	{
		id = MelodyID.None;
		melody = new int[1];
		durations = new float[1] { 1f };
		autoplay = false;
		durationMod = 1f;
	}

	public Melody(MelodyID id, int[] melody = null, float[] durations = null, float durationMod = 1f, bool autoplay = true)
	{
		this.id = id;
		this.melody = melody ?? new int[1];
		this.durations = durations ?? new float[1] { 1f };
		this.autoplay = autoplay;
		this.durationMod = durationMod;
	}
}
