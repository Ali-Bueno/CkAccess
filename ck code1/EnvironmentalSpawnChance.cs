using System;

[Serializable]
public struct EnvironmentalSpawnChance
{
	public enum Source
	{
		Constant,
		WorldGenSetting
	}

	public Source source;

	public PlatformDependentValue<float> constantValue;

	public WorldGenSettingDependentValue<float> worldGenDependentValue;

	public WorldGenSettingDependentValue<float> AsWorldGenSettingDependentValue()
	{
		if (source != 0)
		{
			return worldGenDependentValue;
		}
		return WorldGenSettingDependentValue<float>.FromConstant(constantValue.GetValueForCurrentPlatform());
	}
}
