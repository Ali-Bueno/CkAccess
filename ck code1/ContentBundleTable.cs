using System;
using System.Collections.Generic;
using System.Text;
using NaughtyAttributes;
using Pug.UnityExtensions;
using UnityEngine;

[CreateAssetMenu(menuName = "Pug/Tables/ContentBundleTable")]
public class ContentBundleTable : ScriptableObject
{
	[Serializable]
	public class ContentBundleInfo
	{
		[ReadOnly]
		[AllowNesting]
		public ContentBundleID id;

		public bool canBeActivatedByPlayer = true;

		public List<ContentBundleID> dependencies = new List<ContentBundleID>();
	}

	[ArrayElementTitle("id")]
	public List<ContentBundleInfo> contentBundles;

	public bool HasNewContentToActivate(ContentBundleID firstUnknownBundle)
	{
		if (firstUnknownBundle >= ContentBundleID.__MAX_VALUE)
		{
			return false;
		}
		if (firstUnknownBundle < ContentBundleID.Classic || (int)firstUnknownBundle >= contentBundles.Count)
		{
			Debug.LogWarning($"Invalid firstUnknownBundle: {firstUnknownBundle}");
			return false;
		}
		for (int i = (int)firstUnknownBundle; i < contentBundles.Count; i++)
		{
			if (contentBundles[i].canBeActivatedByPlayer)
			{
				return true;
			}
		}
		return false;
	}

	public void OnValidate()
	{
		contentBundles.Resize(null, 8);
		for (int j = 0; j < contentBundles.Count; j++)
		{
			if (contentBundles[j] == null)
			{
				contentBundles[j] = new ContentBundleInfo();
			}
			ContentBundleID contentBundleID = (ContentBundleID)j;
			if (contentBundleID != contentBundles[j].id)
			{
				contentBundles[j].id = contentBundleID;
				contentBundles[j].dependencies.Clear();
			}
		}
		List<int> list = new List<int>();
		if (TryFindCyclicDependency(list))
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("Found cyclic dependency in ContentBundleTable:\n");
			stringBuilder.Append(string.Join(" -> ", list.ConvertAll((int i) => contentBundles[i].id.ToString())));
			Debug.LogError(stringBuilder.ToString());
		}
	}

	private bool TryFindCyclicDependency(List<int> cycle = null)
	{
		List<bool> list = new List<bool>();
		list.Resize(elementToFillOutWith: false, contentBundles.Count);
		List<int> list2 = new List<int>();
		list2.Resize(-1, contentBundles.Count);
		for (int i = 0; i < contentBundles.Count; i++)
		{
			if (list2[i] == -1)
			{
				list[i] = true;
				if (Dfs(i, list, list2, cycle))
				{
					return true;
				}
				list[i] = false;
			}
		}
		return false;
	}

	private bool Dfs(int node, List<bool> isInStack, List<int> parent, List<int> cycle)
	{
		foreach (ContentBundleID dependency in contentBundles[node].dependencies)
		{
			if (isInStack[(int)dependency])
			{
				parent[(int)dependency] = node;
				if (cycle != null)
				{
					ExtractCycle((int)dependency, parent, cycle);
				}
				return true;
			}
			if (parent[(int)dependency] == -1)
			{
				parent[(int)dependency] = node;
				isInStack[(int)dependency] = true;
				if (Dfs((int)dependency, isInStack, parent, cycle))
				{
					return true;
				}
				isInStack[(int)dependency] = false;
			}
		}
		return false;
	}

	private void ExtractCycle(int end, List<int> parent, List<int> cycle)
	{
		int num = end;
		cycle.Add(end);
		while (parent[num] != end)
		{
			num = parent[num];
			cycle.Add(num);
		}
		cycle.Add(end);
	}
}
