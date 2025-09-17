using System;
using UnityEngine;

[Serializable]
public class HelmSkin : SkinBase
{
	public Texture2D helmTexture;

	public Texture2D emissiveHelmTexture;

	public HelmHairType hairType;

	public Vector2Int pixelOffset;

	public HelmSkin Clone()
	{
		return new HelmSkin
		{
			helmTexture = helmTexture,
			emissiveHelmTexture = emissiveHelmTexture,
			hairType = hairType,
			id = id,
			pixelOffset = pixelOffset
		};
	}
}
