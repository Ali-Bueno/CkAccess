using System;
using System.Runtime.CompilerServices;

[Serializable]
public struct PlatformDependentValue<T>
{
	public bool isPlatformSpecific;

	public T pc;

	public T playstation5;

	public T xboxSeries;

	public T playstation4;

	public T xboxOne;

	public T nintendoSwitch;

	public PlatformDependentValue(T value)
	{
		isPlatformSpecific = false;
		pc = value;
		playstation5 = value;
		xboxSeries = value;
		playstation4 = value;
		xboxOne = value;
		nintendoSwitch = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T GetValueForCurrentPlatform()
	{
		_ = isPlatformSpecific;
		return pc;
	}
}
