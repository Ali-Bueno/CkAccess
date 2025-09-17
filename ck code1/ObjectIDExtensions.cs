public static class ObjectIDExtensions
{
	public static bool IsCookedFood(this ObjectID objectID)
	{
		return IsInInclusiveRange(objectID, 9500, 9599);
	}

	public static bool IsGoldenPlant(this ObjectID objectID)
	{
		return IsInInclusiveRange(objectID, 8100, 8149);
	}

	private static bool IsInInclusiveRange(ObjectID objectID, int low, int high)
	{
		if ((int)objectID >= low)
		{
			return (int)objectID <= high;
		}
		return false;
	}
}
