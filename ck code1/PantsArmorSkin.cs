using System;
using UnityEngine;

[Serializable]
public class PantsArmorSkin : SkinBase
{
	public Texture2D pantsTexture;

	public Texture2D emissivePantsTexture;

	public PantsVisibility pantsVisibility;

	public PantsArmorSkin Clone()
	{
		return new PantsArmorSkin
		{
			pantsTexture = pantsTexture,
			emissivePantsTexture = emissivePantsTexture,
			pantsVisibility = pantsVisibility,
			id = id
		};
	}
}
