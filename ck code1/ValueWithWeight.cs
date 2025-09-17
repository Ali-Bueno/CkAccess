using System;

[Serializable]
public struct ValueWithWeight<T>
{
	public T value;

	public float weight;

	public ValueWithWeight(T value, float weight)
	{
		this.value = value;
		this.weight = weight;
	}
}
