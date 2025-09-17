public static class ObjectTypeUtility
{
	public static bool ShowCooldownForWeaponSlot(ObjectType objectType)
	{
		if (objectType != ObjectType.MeleeWeapon && objectType != ObjectType.RangeWeapon && objectType != ObjectType.SummoningWeapon)
		{
			return objectType == ObjectType.ThrowingWeapon;
		}
		return true;
	}
}
