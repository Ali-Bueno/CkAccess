using System;
using System.Runtime.CompilerServices;

[Serializable]
public struct WorldGenerationTypeDependentValue<T>
{
	public T classic;

	public T fullRelease;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly T Get(WorldGenerationType type)
	{
		return type switch
		{
			WorldGenerationType.Classic => classic, 
			WorldGenerationType.FullRelease => fullRelease, 
			WorldGenerationType.Creative => classic, 
			_ => throw new ArgumentOutOfRangeException("type", type, null), 
		};
	}
}
