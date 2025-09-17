using System;
using UnityEngine;

[Serializable]
public class BreastArmorSkin : SkinBase
{
	public Texture2D breastTexture;

	public Texture2D emissiveBreastTexture;

	public ShirtVisibility shirtVisibility;

	public BreastArmorSkin Clone()
	{
		return new BreastArmorSkin
		{
			breastTexture = breastTexture,
			emissiveBreastTexture = emissiveBreastTexture,
			shirtVisibility = shirtVisibility,
			id = id
		};
	}
}
