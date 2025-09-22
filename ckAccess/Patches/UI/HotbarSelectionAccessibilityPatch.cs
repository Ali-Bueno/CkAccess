extern alias PugOther;
using HarmonyLib;
using UnityEngine;
using ckAccess.Localization;

namespace ckAccess.Patches.UI
{
    /// <summary>
    /// Parche para anunciar items al seleccionar slots del hotbar
    /// </summary>
    [HarmonyPatch]
    public static class HotbarSelectionAccessibilityPatch
    {
        /// <summary>
        /// Intercepta la selección de slots del hotbar para anunciar el item equipado
        /// </summary>
        [HarmonyPatch(typeof(PugOther.PlayerController), "EquipSlot")]
        [HarmonyPostfix]
        public static void EquipSlot_Postfix(PugOther.PlayerController __instance, int slotIndex, bool __result)
        {
            try
            {
                // Solo anunciar si la operación fue exitosa y es el jugador local
                if (!__result || !__instance.isLocal) return;

                int slotNumber = slotIndex + 1; // Los slots son 0-indexados, pero mostramos 1-10

                // Intentar obtener el item del slot seleccionado
                string itemName = GetItemNameFromHotbarSlot(__instance, slotIndex);

                if (string.IsNullOrEmpty(itemName))
                {
                    // Si no hay item o no se puede obtener, anunciar solo el número
                    UIManager.Speak(LocalizationManager.GetText("hotbar_slot_selected", slotNumber));
                }
                else
                {
                    // Anunciar el nombre del item
                    UIManager.Speak(itemName);
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Exception in EquipSlot_Postfix: {ex}");
            }
        }

        /// <summary>
        /// Obtiene el nombre del item en un slot específico del hotbar
        /// </summary>
        private static string GetItemNameFromHotbarSlot(PugOther.PlayerController player, int slotIndex)
        {
            try
            {
                // Los slots del hotbar pueden estar en el inventario del jugador
                // Los primeros 10 slots (0-9) suelen ser el hotbar
                var playerInventory = player.playerInventoryHandler;
                if (playerInventory == null) return null;

                var containedObject = playerInventory.GetContainedObjectData(slotIndex);
                if (containedObject.objectID == ObjectID.None)
                {
                    return LocalizationManager.GetText("empty_hotbar_slot");
                }

                // Obtener el nombre localizado del objeto
                var objectName = PugOther.PlayerController.GetObjectName(containedObject, localize: true);
                if (objectName != null && !string.IsNullOrEmpty(objectName.text))
                {
                    return objectName.text;
                }

                // Fallback: usar el toString del ObjectID
                return containedObject.objectID.ToString();
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error getting item name from hotbar slot {slotIndex}: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene el nombre de un item de forma simplificada
        /// </summary>
        private static string GetItemNameSimple(ObjectID objectID)
        {
            try
            {
                // Por ahora usar el toString del ObjectID
                // En el futuro se puede mejorar con acceso a la base de datos del juego
                return objectID.ToString();
            }
            catch
            {
                return LocalizationManager.GetText("unknown");
            }
        }
    }
}