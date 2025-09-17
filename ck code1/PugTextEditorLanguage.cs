using UnityEngine;

[CreateAssetMenu(menuName = "Pug/Editor/PugTextEditorLanguage", order = 4)]
public class PugTextEditorLanguage : ScriptableObject
{
	public string language;

	private static PugTextEditorLanguage GetScriptableObject()
	{
		PugTextEditorLanguage pugTextEditorLanguage = Resources.Load<PugTextEditorLanguage>("PugTextEditorLanguage");
		if (pugTextEditorLanguage == null)
		{
			pugTextEditorLanguage = CreateScriptableObject();
		}
		return pugTextEditorLanguage;
	}

	public static string GetLanguage()
	{
		return GetScriptableObject().language;
	}

	public static void SetLanguage(string language)
	{
		GetScriptableObject().language = language;
	}

	private static PugTextEditorLanguage CreateScriptableObject()
	{
		PugTextEditorLanguage pugTextEditorLanguage = ScriptableObject.CreateInstance<PugTextEditorLanguage>();
		pugTextEditorLanguage.language = "English";
		return pugTextEditorLanguage;
	}
}
