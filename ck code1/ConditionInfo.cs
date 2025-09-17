using System;
using UnityEngine;

[Serializable]
public struct ConditionInfo
{
	public ConditionID Id;

	public ConditionEffect effect;

	public bool isAdditiveWithSelf;

	public bool isPermanent;

	public bool isNegative;

	public bool isInheritedByProjectiles;

	public Sprite icon;

	public ConditionID useSameDescAsId;

	public bool isUnique;

	public bool skipShowingSignInfrontOfValue;

	public bool showDecimal;

	public bool skipShowingStatText;
}
