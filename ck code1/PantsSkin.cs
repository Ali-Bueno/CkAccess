using System;
using UnityEngine;

[Serializable]
public class PantsSkin : SkinBase
{
	public Texture2D pantsTexture;

	public ReorderableColorReplacementData colorReplacementData;

	[HideInInspector]
	public ColorReplacementData colorReplacementDataSorted;

	public PantsSkin Clone()
	{
		return new PantsSkin
		{
			pantsTexture = pantsTexture,
			colorReplacementDataSorted = colorReplacementData.GetSortedColorReplacementData(),
			id = id
		};
	}
}
