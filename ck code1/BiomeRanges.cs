using System;
using Pug.UnityExtensions;
using Unity.Collections;
using Unity.Mathematics;

public struct BiomeRanges
{
	public Biome biome;

	public float start;

	public float end;

	public float shaderStart;

	public float shaderEnd;

	public float startAngle;

	public float endAngle;

	public bool exists;

	public static BiomeRanges All
	{
		get
		{
			BiomeRanges result = default(BiomeRanges);
			result.start = 0f;
			result.end = float.PositiveInfinity;
			result.shaderStart = 0f;
			result.shaderEnd = float.PositiveInfinity;
			result.startAngle = 0f;
			result.endAngle = 360f;
			result.exists = true;
			return result;
		}
	}

	[Obsolete("Use BiomeLookup instead to support full release worldgen.")]
	public static bool IsWithinBiome(int2 pos, BiomeRanges biomeRanges, float distancePadding = 0f, float anglePaddingInDegrees = 0f)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		if (!biomeRanges.exists)
		{
			return false;
		}
		float num = math.length(float2.op_Implicit(pos));
		if (num < biomeRanges.start - distancePadding || num > biomeRanges.end + distancePadding)
		{
			return false;
		}
		return IsWithinBiomeSideAngles(pos, biomeRanges, anglePaddingInDegrees);
	}

	[Obsolete("Use BiomeLookup instead to support full release worldgen.")]
	public static bool IsWithinBiomeSideAngles(int2 pos, BiomeRanges biomeRanges, float anglePaddingInDegrees = 0f)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		if (!biomeRanges.exists)
		{
			return false;
		}
		if (biomeRanges.startAngle == 0f && biomeRanges.endAngle == 360f)
		{
			return true;
		}
		float num = math.degrees(math.atan2((float)pos.y, (float)pos.x)) + 180f;
		float num2 = biomeRanges.endAngle + anglePaddingInDegrees;
		float num3 = biomeRanges.startAngle - anglePaddingInDegrees;
		if (num2 > 360f)
		{
			if (num > num2 % 360f && num < num3)
			{
				return false;
			}
		}
		else if (num < num3 || num > num2)
		{
			return false;
		}
		return true;
	}

	[Obsolete("Use BiomeLookup instead to support full release worldgen.")]
	public static bool TryGetRandomPositionWithinBiomeRanges(ref Random rng, BiomeRanges biomeRanges, float anglePadding, out int2 result)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		result = int2.zero;
		if (!biomeRanges.exists)
		{
			return false;
		}
		float num = ((Random)(ref rng)).NextFloat(biomeRanges.startAngle + anglePadding, biomeRanges.endAngle - anglePadding) - 180f;
		float num2 = ((Random)(ref rng)).NextFloat(biomeRanges.start, biomeRanges.end);
		result = (int2)math.round(new float2(math.cos(math.radians(num)), math.sin(math.radians(num))) * num2);
		return true;
	}

	[Obsolete("Use BiomeLookup instead to support full release worldgen.")]
	public static bool TryGetRandomPositionInBiome(ref Random rng, BiomeRanges biomeRanges, out int2 result, int minDistanceFromCore = 0, int maxDistanceFromCore = int.MaxValue)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		result = int2.zero;
		if (!biomeRanges.exists)
		{
			return false;
		}
		maxDistanceFromCore = ((maxDistanceFromCore == int.MaxValue) ? ((int)math.round(1.5f * (float)minDistanceFromCore)) : maxDistanceFromCore);
		result = PugRandom.UniformDiskSample(ref rng, minDistanceFromCore, maxDistanceFromCore, (biomeRanges.startAngle - 180f) * (MathF.PI / 180f), (biomeRanges.endAngle - 180f) * (MathF.PI / 180f)).RoundToInt2();
		return true;
	}

	[Obsolete("Use BiomeLookup instead to support full release worldgen.")]
	public static Biome GetBiomeAtPosition(int2 pos, FixedList512Bytes<BiomeRanges> biomeRanges)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		Enumerator<BiomeRanges> enumerator = biomeRanges.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				BiomeRanges current = enumerator.Current;
				if (IsWithinBiome(pos, current))
				{
					return current.biome;
				}
			}
		}
		finally
		{
			((IDisposable)enumerator).Dispose();
		}
		return Biome.None;
	}

	public static float3 GetDirectionToMiddleOfBiome(BiomeRanges biomeRanges)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		float num = (biomeRanges.startAngle + (biomeRanges.endAngle - biomeRanges.startAngle) / 2f + 180f) % 360f * (MathF.PI / 180f);
		return math.mul(quaternion.AxisAngle(math.up(), 0f - num), math.right());
	}
}
