using System.Collections.Generic;
using Pug.UnityExtensions;
using UnityEngine;

[CreateAssetMenu(menuName = "Pug/Tables/EnvironmentEventsTable", order = 5)]
public class EnvironmentEventsTable : ScriptableObject
{
	[ArrayElementTitle("eventType")]
	public List<EnvironmentEventParams> eventRequirements;
}
