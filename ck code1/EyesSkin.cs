using System;
using UnityEngine;

[Serializable]
public class EyesSkin : SkinBase
{
	public Texture2D eyesTexture;

	public EyesSkin Clone()
	{
		return new EyesSkin
		{
			eyesTexture = eyesTexture,
			id = id
		};
	}
}
