using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ColorReplacementData
{
	public List<Color> srcColors = new List<Color>();

	public List<ColorList> replacementColors = new List<ColorList>();

	public int GetHashCode(int replacementIndex)
	{
		int num = 0;
		if (srcColors != null)
		{
			num = srcColors.Count;
			for (int i = 0; i < srcColors.Count; i++)
			{
				num = num * 17 + srcColors[i].GetHashCode();
			}
		}
		int num2 = 0;
		if (replacementColors != null && replacementColors.Count > 0 && replacementIndex > -1 && replacementIndex < replacementColors.Count)
		{
			List<Color> colorList = replacementColors[replacementIndex].colorList;
			num2 = colorList.Count;
			for (int j = 0; j < colorList.Count; j++)
			{
				num2 = num2 * 19 + colorList[j].GetHashCode();
			}
		}
		return num ^ num2;
	}
}
