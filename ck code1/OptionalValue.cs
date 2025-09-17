using System;
using System.Runtime.CompilerServices;

[Serializable]
public struct OptionalValue<T>
{
	public bool hasValue;

	public T value;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T GetOrDefault(T defaultValue)
	{
		if (!hasValue)
		{
			return defaultValue;
		}
		return value;
	}
}
