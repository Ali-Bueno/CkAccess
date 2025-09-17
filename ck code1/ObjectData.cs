using System;
using Unity.Mathematics;

[Serializable]
public struct ObjectData : IEquatable<ObjectData>
{
	public ObjectID objectID;

	public int variation;

	public int amount;

	public bool Equals(ObjectData other)
	{
		if (objectID == other.objectID)
		{
			return variation == other.variation;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is ObjectData other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		return (int)math.hash(new int2((int)objectID, variation));
	}
}
