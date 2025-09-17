using System;
using UnityEngine;

[Serializable]
public struct IconOverrides
{
	public ObjectData objectData;

	public bool amountMustMatch;

	public bool variationMustMatch;

	public Sprite icon;

	public Sprite smallIcon;
}
