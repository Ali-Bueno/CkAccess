using System;
using UnityEngine;

[Serializable]
public class ShirtSkin : SkinBase
{
	public Texture2D maleShirtTexture;

	public Texture2D femaleShirtTexture;

	public ReorderableColorReplacementData colorReplacementData;

	[HideInInspector]
	public ColorReplacementData colorReplacementDataSorted;

	public ShirtSkin Clone()
	{
		return new ShirtSkin
		{
			maleShirtTexture = maleShirtTexture,
			femaleShirtTexture = femaleShirtTexture,
			colorReplacementDataSorted = colorReplacementData.GetSortedColorReplacementData(),
			id = id
		};
	}
}
