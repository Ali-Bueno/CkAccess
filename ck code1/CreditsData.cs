using System;
using System.Collections.Generic;
using System.Linq;
using Pug.UnityExtensions;
using UnityEngine;

[CreateAssetMenu(menuName = "Pug/Tables/CreditsData", order = 3)]
public class CreditsData : ScriptableObject, ISerializationCallbackReceiver
{
	[Serializable]
	public class CreditsEntry
	{
		public CreditLayout layout;

		public CreditTitle title;

		public List<CreditName> creditNames;

		public bool useCommaSeparationForNames;

		public bool skipTitle;

		public bool endWithComma;
	}

	[ArrayElementTitle("title")]
	public List<CreditsEntry> creditsElements;

	private const string creditsPreFix = "Credits/";

	public static string TitleToTerm(CreditTitle _title)
	{
		return "Credits/" + _title;
	}

	public void OnBeforeSerialize()
	{
		foreach (CreditsEntry creditsElement in creditsElements)
		{
			creditsElement.creditNames = creditsElement.creditNames.OrderBy((CreditName c) => c.ToString()).ToList();
		}
	}

	public void OnAfterDeserialize()
	{
	}
}
