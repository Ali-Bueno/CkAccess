using System;
using System.Collections.Generic;
using Pug.UnityExtensions;

[Serializable]
public struct EnvironmentSpawnObjectsInBiome
{
	public Biome spawnsInBiome;

	[ArrayElementTitle("objectId")]
	public List<EnvironmentSpawnObject> environmentSpawnObjects;
}
