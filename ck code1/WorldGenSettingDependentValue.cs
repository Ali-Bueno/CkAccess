using System;
using System.Runtime.CompilerServices;

[Serializable]
public struct WorldGenSettingDependentValue<T>
{
	public WorldGenerationSettingType worldGenSetting;

	public T off;

	public T low;

	public T normal;

	public T high;

	public T extreme;

	public static WorldGenSettingDependentValue<T> FromConstant(T value)
	{
		WorldGenSettingDependentValue<T> result = default(WorldGenSettingDependentValue<T>);
		result.off = value;
		result.low = value;
		result.normal = value;
		result.high = value;
		result.extreme = value;
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T GetValue(WorldGenerationSettingLevel level)
	{
		return level switch
		{
			WorldGenerationSettingLevel.Off => off, 
			WorldGenerationSettingLevel.Low => low, 
			WorldGenerationSettingLevel.Normal => normal, 
			WorldGenerationSettingLevel.High => high, 
			WorldGenerationSettingLevel.Extreme => extreme, 
			_ => throw new ArgumentOutOfRangeException("level", level, null), 
		};
	}
}
