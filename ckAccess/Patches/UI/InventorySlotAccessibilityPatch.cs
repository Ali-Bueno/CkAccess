extern alias PugOther;
extern alias Core;
extern alias I2Loc;
using HarmonyLib;
using ckAccess.Localization;
using UnityEngine;
using Vector3 = Core::UnityEngine.Vector3;

namespace ckAccess.Patches.UI
{
    /// <summary>
    /// Parche para mejorar la accesibilidad de los slots del inventario con feedback contextual
    /// Anuncia qué objeto está seleccionado y qué acciones se pueden realizar según el contexto
    /// DESHABILITADO - InventorySlotUIPatch.cs ya hace esto mejor
    /// </summary>
    // [HarmonyPatch(typeof(PugOther.InventorySlotUI))]
    public static class InventorySlotAccessibilityPatch
    {
        private static string _lastAnnouncedSlot = "";
        private static float _lastAnnounceTime = 0f;
        private const float DEBOUNCE_TIME = 0.3f;

        /// <summary>
        /// Parche en OnSelected para anunciar el objeto cuando se selecciona un slot
        /// </summary>
        // [HarmonyPatch("OnSelected")]
        // [HarmonyPostfix]
        public static void OnSelected_Postfix(PugOther.InventorySlotUI __instance)
        {
            try
            {
                // Debounce para evitar anuncios duplicados
                string slotId = GetSlotIdentifier(__instance);
                if (slotId == _lastAnnouncedSlot && Time.unscaledTime - _lastAnnounceTime < DEBOUNCE_TIME)
                {
                    return;
                }

                _lastAnnouncedSlot = slotId;
                _lastAnnounceTime = Time.unscaledTime;

                AnnounceSlotSelection(__instance);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error in InventorySlotAccessibilityPatch.OnSelected: {ex}");
            }
        }

        /// <summary>
        /// Anuncia el objeto seleccionado y las acciones disponibles según el contexto
        /// </summary>
        private static void AnnounceSlotSelection(PugOther.InventorySlotUI slot)
        {
            try
            {
                var objectData = slot.GetContainedObjectData();

                // Si el slot está vacío
                if (objectData.objectID == ObjectID.None)
                {
                    AnnounceEmptySlot(slot);
                    return;
                }

                // Obtener información del objeto
                string objectName = GetObjectName(objectData.objectID);
                int amount = objectData.amount;

                // Construir mensaje simple: solo nombre y cantidad
                string message = amount > 1
                    ? $"{objectName} x{amount}"
                    : objectName;

                UIManager.Speak(message);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error in AnnounceSlotSelection: {ex}");
            }
        }

        /// <summary>
        /// Anuncia que el slot está vacío
        /// </summary>
        private static void AnnounceEmptySlot(PugOther.InventorySlotUI slot)
        {
            try
            {
                UIManager.Speak(LocalizationManager.GetText("empty_slot"));
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error in AnnounceEmptySlot: {ex}");
            }
        }

        /// <summary>
        /// Obtiene el nombre localizado de un objeto
        /// </summary>
        private static string GetObjectName(ObjectID objectID)
        {
            try
            {
                var objectInfo = PugOther.PugDatabase.GetObjectInfo(objectID);
                if (objectInfo != null)
                {
                    // Intentar obtener el nombre a través del sistema de localización del juego
                    string termKey = $"Items/{objectID}";
                    string localized = I2Loc::I2.Loc.LocalizationManager.GetTranslation(termKey);

                    if (!string.IsNullOrEmpty(localized) && localized != termKey)
                    {
                        return localized;
                    }
                }

                // Fallback: usar el nombre del enum pero formateado
                return FormatEnumName(objectID.ToString());
            }
            catch
            {
                return FormatEnumName(objectID.ToString());
            }
        }

        /// <summary>
        /// Formatea el nombre del enum para que sea más legible
        /// Ejemplo: "WoodWall" -> "Wood Wall"
        /// </summary>
        private static string FormatEnumName(string enumName)
        {
            if (string.IsNullOrEmpty(enumName)) return "Unknown";

            // Insertar espacios antes de mayúsculas
            var result = new System.Text.StringBuilder();
            for (int i = 0; i < enumName.Length; i++)
            {
                if (i > 0 && char.IsUpper(enumName[i]) && !char.IsUpper(enumName[i - 1]))
                {
                    result.Append(' ');
                }
                result.Append(enumName[i]);
            }
            return result.ToString();
        }

        /// <summary>
        /// Obtiene un identificador único para el slot (para debounce)
        /// </summary>
        private static string GetSlotIdentifier(PugOther.InventorySlotUI slot)
        {
            try
            {
                var objectData = slot.GetContainedObjectData();
                return $"{slot.inventorySlotIndex}_{objectData.objectID}_{objectData.amount}";
            }
            catch
            {
                return slot.GetHashCode().ToString();
            }
        }
    }
}
