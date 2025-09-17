
extern alias PugOther;
extern alias PugComps;

using HarmonyLib;
using ckAccess.Patches.UI;
using System.Text;

namespace ckAccess.Patches
{
    [HarmonyPatch(typeof(PugOther.InventorySlotUI))]
    public static class InventorySlotUIPatch
    {
        private static string lastAnnouncedSlot = "";

        private static void AnnounceSlot(PugOther.InventorySlotUI instance)
        {
            if (instance == null || !instance.isShowing)
            {
                return;
            }

            var sb = new StringBuilder();
            string slotTypeName = GetSlotTypeName(instance.slotType);
            if (!string.IsNullOrEmpty(slotTypeName))
            {
                sb.Append(slotTypeName).Append(": ");
            }

            var containedObject = instance.GetContainedObjectData();
            string itemIdentifier;

            if (containedObject.objectID == ObjectID.None)
            {
                string emptySlotText = UIManager.GetLocalizedText("UI/Inventory/Empty_slot");
                if (string.IsNullOrEmpty(emptySlotText)) emptySlotText = "Empty";
                sb.Append(emptySlotText);
                itemIdentifier = $"{instance.slotType}_{instance.GetInstanceID()}";
            }
            else
            {
                var objectName = PugOther.PlayerController.GetObjectName(containedObject, true);
                var objectInfo = PugOther.PugDatabase.GetObjectInfo(containedObject.objectID);
                itemIdentifier = $"{instance.slotType}_{containedObject.objectID}_{containedObject.variation}_{containedObject.amount}";

                // 1. Name
                sb.Append(objectName.text);

                // 2. Amount (if stackable)
                if (objectInfo != null && objectInfo.isStackable)
                {
                    sb.Append($" {containedObject.amount}");
                }

                // 3. Durability and other Attributes
                if (PugOther.PugDatabase.HasComponent<PugComps.DurabilityCD>(containedObject.objectID))
                {
                    var durabilityCD = PugOther.PugDatabase.GetComponent<PugComps.DurabilityCD>(containedObject.objectID, 0);
                    sb.Append($", Durability: {containedObject.amount} / {durabilityCD.maxDurability}");
                }

                var stats = instance.GetHoverStats(false);
                if (stats != null)
                {
                    foreach (var line in stats)
                    {
                        sb.Append(", ").Append(PugOther.PugText.ProcessText(line.text, line.formatFields, true, false));
                    }
                }

                // 4. Tooltip / Description
                var description = instance.GetHoverDescription();
                if (description != null)
                {
                    foreach (var line in description)
                    {
                        sb.Append(", ").Append(PugOther.PugText.ProcessText(line.text, line.formatFields, true, false));
                    }
                }
            }

            if (itemIdentifier != lastAnnouncedSlot)
            {
                UIManager.Speak(sb.ToString());
                lastAnnouncedSlot = itemIdentifier;
            }
        }

        private static string GetSlotTypeName(PugOther.ItemSlotsUIType slotType)
        {
            switch (slotType)
            {
                case PugOther.ItemSlotsUIType.HelmSlot: return "Helm";
                case PugOther.ItemSlotsUIType.BreastSlot: return "Chest";
                case PugOther.ItemSlotsUIType.PantsSlot: return "Pants";
                case PugOther.ItemSlotsUIType.NecklaceSlot: return "Necklace";
                case PugOther.ItemSlotsUIType.RingSlot1: return "Ring 1";
                case PugOther.ItemSlotsUIType.RingSlot2: return "Ring 2";
                case PugOther.ItemSlotsUIType.OffhandSlot: return "Offhand";
                case PugOther.ItemSlotsUIType.BagSlot: return "Bag";
                case PugOther.ItemSlotsUIType.PetSlot: return "Pet";
                case PugOther.ItemSlotsUIType.LanternSlot: return "Lantern";
                case PugOther.ItemSlotsUIType.HelmVanitySlot: return "Vanity Helm";
                case PugOther.ItemSlotsUIType.BreastVanitySlot: return "Vanity Chest";
                case PugOther.ItemSlotsUIType.PantsVanitySlot: return "Vanity Pants";
                case PugOther.ItemSlotsUIType.Pouch1: return "Pouch 1";
                case PugOther.ItemSlotsUIType.Pouch2: return "Pouch 2";
                case PugOther.ItemSlotsUIType.Pouch3: return "Pouch 3";
                case PugOther.ItemSlotsUIType.Pouch4: return "Pouch 4";
                default: return "";
            }
        }

        [HarmonyPatch("OnSelected")]
        [HarmonyPostfix]
        public static void Postfix_OnSelected(PugOther.InventorySlotUI __instance)
        {
            AnnounceSlot(__instance);
        }

        [HarmonyPatch("OnDeselected")]
        [HarmonyPostfix]
        public static void Postfix_OnDeselected(PugOther.InventorySlotUI __instance)
        {
            lastAnnouncedSlot = "";
        }
    }
}
