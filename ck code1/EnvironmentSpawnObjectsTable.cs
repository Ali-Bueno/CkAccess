using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Pug/Tables/EnvironmentSpawnObjectsTable", order = 5)]
public class EnvironmentSpawnObjectsTable : ScriptableObject
{
	public List<EnvironmentSpawnData> spawnObjects;

	public List<RespawnData> respawnObjects;

	private void OnValidate()
	{
		bool flag = false;
		HashSet<string> hashSet = new HashSet<string>();
		foreach (EnvironmentSpawnData spawnObject in spawnObjects)
		{
			string text = spawnObject.spawnCheck.biome.ToString();
			string text2 = ((spawnObject.spawns.Count == 0) ? "Empty" : spawnObject.spawns[0].objectID.ToString());
			string text3 = spawnObject.name;
			spawnObject.name = text + "/" + text2;
			string arg = spawnObject.name;
			int num = 1;
			while (hashSet.Contains(spawnObject.name))
			{
				num++;
				spawnObject.name = $"{arg}{num}";
			}
			hashSet.Add(spawnObject.name);
			flag |= !text3.Equals(spawnObject.name);
		}
		hashSet.Clear();
		foreach (RespawnData respawnObject in respawnObjects)
		{
			string text4 = ((respawnObject.spawns.Count == 0) ? "Empty" : respawnObject.spawns[0].objectID.ToString());
			string text5 = respawnObject.name;
			respawnObject.name = text4;
			string arg2 = respawnObject.name;
			int num2 = 1;
			while (hashSet.Contains(respawnObject.name))
			{
				num2++;
				respawnObject.name = $"{arg2}{num2}";
			}
			hashSet.Add(respawnObject.name);
			flag |= !text5.Equals(respawnObject.name);
		}
	}
}
