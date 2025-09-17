using System;
using UnityEngine;

[Serializable]
public class HairSkin : SkinBase
{
	public Texture2D hairTexture;

	public Texture2D hairShadeTexture;

	public Texture2D helmHairTexture;

	public HairSkin Clone()
	{
		return new HairSkin
		{
			hairTexture = hairTexture,
			hairShadeTexture = hairShadeTexture,
			helmHairTexture = helmHairTexture,
			id = id
		};
	}
}
