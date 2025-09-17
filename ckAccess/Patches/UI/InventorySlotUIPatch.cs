
extern alias PugOther;

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

            var containedObject = instance.GetContainedObjectData();
            string itemIdentifier;
            var sb = new StringBuilder();

            if (containedObject.objectID == ObjectID.None)
            {
                string emptySlotText = UIManager.GetLocalizedText("UI/Inventory/Empty_slot");
                if (string.IsNullOrEmpty(emptySlotText)) emptySlotText = "Empty slot";
                sb.Append(emptySlotText);
                itemIdentifier = instance.GetInstanceID().ToString();
            }
            else
            {
                var objectName = PugOther.PlayerController.GetObjectName(containedObject, true);
                sb.Append(containedObject.amount > 1 ? $"{objectName.text} {containedObject.amount}" : objectName.text);
                itemIdentifier = $"{containedObject.objectID}_{containedObject.variation}_{containedObject.amount}";

                var description = instance.GetHoverDescription();
                var stats = instance.GetHoverStats(false);

                if (description != null)
                {
                    foreach (var line in description)
                    {
                        sb.Append(", ").Append(PugOther.PugText.ProcessText(line.text, line.formatFields, true, false));
                    }
                }

                if (stats != null)
                {
                    foreach (var line in stats)
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
