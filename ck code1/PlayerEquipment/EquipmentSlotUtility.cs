namespace PlayerEquipment;

public static class EquipmentSlotUtility
{
	public static bool IsMeleeWeaponSlotWithSound(EquipmentSlotType slotType)
	{
		if (slotType != EquipmentSlotType.MeleeWeaponSlot && slotType != EquipmentSlotType.ShovelSlot && slotType != EquipmentSlotType.HoeSlot)
		{
			return slotType == EquipmentSlotType.BugNet;
		}
		return true;
	}

	public static bool IsWeaponSlot(EquipmentSlotType slotType)
	{
		if (slotType != EquipmentSlotType.MeleeWeaponSlot)
		{
			return slotType == EquipmentSlotType.RangeWeaponSlot;
		}
		return true;
	}
}
