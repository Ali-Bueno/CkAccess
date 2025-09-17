using System;
using UnityEngine;

[Serializable]
public class BodySkin : SkinBase
{
	public Texture2D bodyTexture;

	public BodySkin Clone()
	{
		return new BodySkin
		{
			bodyTexture = bodyTexture,
			id = id
		};
	}
}
