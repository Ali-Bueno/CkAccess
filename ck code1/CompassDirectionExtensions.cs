using System;
using Unity.Mathematics;

public static class CompassDirectionExtensions
{
	public static CompassDirection CompassDirectionFromCore(int2 worldPosition)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		if (math.all(worldPosition == 0))
		{
			return CompassDirection.Undefined;
		}
		int num = (int)math.round(math.atan2((float)worldPosition.y, (float)worldPosition.x) / 2f / MathF.PI * 8f);
		if (num < 0)
		{
			num += 8;
		}
		num++;
		return (CompassDirection)num;
	}
}
