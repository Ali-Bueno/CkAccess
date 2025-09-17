using System;
using Pug.UnityExtensions;
using UnityEngine;

[CreateAssetMenu(menuName = "Pug/Tables/FactionsLookUp", order = 4)]
public class FactionsLookUp : ScriptableObject, ISerializationCallbackReceiver
{
	[BooleanMatrix(typeof(FactionID))]
	public BooleanMatrix factionMatrix = new BooleanMatrix(Enum.GetValues(typeof(FactionID)).Length);

	public void OnBeforeSerialize()
	{
	}

	public void OnAfterDeserialize()
	{
	}
}
