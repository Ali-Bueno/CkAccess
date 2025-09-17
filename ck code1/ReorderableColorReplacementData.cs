using System;
using System.Collections.Generic;
using Pug.UnityExtensions;
using UnityEngine;

[Serializable]
public class ReorderableColorReplacementData
{
	public List<Color> srcColors = new List<Color>();

	[ArrayElementTitle("id")]
	public List<ReorderableColorList> replacementColors = new List<ReorderableColorList>();

	public ColorReplacementData GetSortedColorReplacementData()
	{
		ColorReplacementData colorReplacementData = new ColorReplacementData
		{
			srcColors = new List<Color>(srcColors.Count),
			replacementColors = new List<ColorList>(replacementColors.Count)
		};
		for (int i = 0; i < srcColors.Count; i++)
		{
			colorReplacementData.srcColors.Add(srcColors[i]);
		}
		for (int j = 0; j < replacementColors.Count; j++)
		{
			colorReplacementData.replacementColors.Add(new ColorList());
		}
		foreach (ReorderableColorList replacementColor in replacementColors)
		{
			foreach (Color color in replacementColor.colorList)
			{
				colorReplacementData.replacementColors[replacementColor.id].colorList.Add(color);
			}
		}
		return colorReplacementData;
	}
}
