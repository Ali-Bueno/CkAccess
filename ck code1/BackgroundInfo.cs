using System;
using UnityEngine;

[Serializable]
public class BackgroundInfo
{
	public bool initializeOnLoad;

	public BackgroundType type;

	public GameObject prefab;

	public int layer;
}
