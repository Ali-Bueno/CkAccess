extern alias PugOther;
using HarmonyLib;
using UnityEngine;
using ckAccess.Localization;

namespace ckAccess.Patches.UI
{
    /// <summary>
    /// Parche para acciones directas accesibles en inventario:
    /// - Shift+U: Equipar armaduras automáticamente
    /// - O: Usar items directamente del inventario (contextual)
    /// </summary>
    [HarmonyPatch]
    public static class DirectActionsAccessibilityPatch
    {
        /// <summary>
        /// Maneja acciones directas con modificadores
        /// </summary>
        public static bool HandleDirectActions(PugOther.UIManager uiManager)
        {
            try
            {
                if (!uiManager.isAnyInventoryShowing) return false;

                var currentElement = uiManager.currentSelectedUIElement;
                if (currentElement == null) return false;

                // Verificar si es un slot de inventario
                var inventorySlot = currentElement.GetComponent<PugOther.InventorySlotUI>();
                if (inventorySlot == null) return false;

                // Verificar modificadores
                bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                if (Input.GetKeyDown(KeyCode.U) && shiftHeld)
                {
                    return HandleDirectEquip(inventorySlot);
                }
                else if (Input.GetKeyDown(KeyCode.O))
                {
                    return HandleDirectUse(inventorySlot);
                }

                return false;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Exception in HandleDirectActions: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Equipa automáticamente un item (Shift + U)
        /// </summary>
        private static bool HandleDirectEquip(PugOther.InventorySlotUI slot)
        {
            try
            {
                var containedObject = slot.GetContainedObjectData();
                if (containedObject.objectID == ObjectID.None)
                {
                    UIManager.Speak(LocalizationManager.GetText("empty_slot_cannot_equip"));
                    return true;
                }

                // Obtener información del objeto
                string itemName = GetItemName(slot);

                // Verificar si se puede equipar
                if (!IsEquippableItem(containedObject.objectID))
                {
                    UIManager.Speak(LocalizationManager.GetText("item_not_equippable", itemName));
                    return true;
                }

                var uiManager = PugOther.Manager.ui;
                var inventoryHandler = GetInventoryHandlerFromSlot(slot);
                if (inventoryHandler == null)
                {
                    UIManager.Speak(LocalizationManager.GetText("equip_error"));
                    return true;
                }

                // Solo para armaduras: usar AttemptToEquipItem
                bool success = uiManager.AttemptToEquipItem(inventoryHandler, slot.inventorySlotIndex, false);

                if (success)
                {
                    UIManager.Speak(LocalizationManager.GetText("item_equipped", itemName));
                    // Audio se maneja automáticamente por el juego
                }
                else
                {
                    UIManager.Speak(LocalizationManager.GetText("item_cannot_equip", itemName));
                }

                return true;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Exception in HandleDirectEquip: {ex}");
                UIManager.Speak(LocalizationManager.GetText("equip_error"));
                return true;
            }
        }

        /// <summary>
        /// Usa un item directamente del inventario (O) - Contextual
        /// </summary>
        private static bool HandleDirectUse(PugOther.InventorySlotUI slot)
        {
            try
            {
                var containedObject = slot.GetContainedObjectData();
                if (containedObject.objectID == ObjectID.None)
                {
                    UIManager.Speak(LocalizationManager.GetText("empty_slot_cannot_use"));
                    return true;
                }

                string itemName = GetItemName(slot);

                // Usar el comportamiento nativo del click derecho del juego
                // Esto es más robusto y contextual que nuestra implementación anterior
                slot.OnRightClicked(false, false);

                // Proporcionar feedback sobre la acción realizada
                if (IsConsumableItem(containedObject.objectID))
                {
                    UIManager.Speak(LocalizationManager.GetText("item_used", itemName));
                }
                else if (IsPlaceableItem(containedObject.objectID))
                {
                    UIManager.Speak(LocalizationManager.GetText("item_placed", itemName));
                }
                else
                {
                    UIManager.Speak(LocalizationManager.GetText("item_action_performed", itemName));
                }

                return true;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Exception in HandleDirectUse: {ex}");
                UIManager.Speak(LocalizationManager.GetText("use_error"));
                return true;
            }
        }


        /// <summary>
        /// Verifica si un objeto es equipamiento (armadura/accesorios)
        /// </summary>
        private static bool IsArmorEquipment(ObjectID objectID)
        {
            try
            {
                var id = objectID;
                string name = id.ToString().ToLower();
                return name.Contains("armor") || name.Contains("helm") || name.Contains("chest") ||
                       name.Contains("pants") || name.Contains("boots") || name.Contains("ring") ||
                       name.Contains("necklace") || name.Contains("lantern") || name.Contains("bag") ||
                       name.Contains("gauntlet") || name.Contains("glove");
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Verifica si un objeto se puede equipar (solo armadura)
        /// </summary>
        private static bool IsEquippableItem(ObjectID objectID)
        {
            return IsArmorEquipment(objectID);
        }

        /// <summary>
        /// Verifica si un objeto es consumible (comida, pociones)
        /// </summary>
        private static bool IsConsumableItem(ObjectID objectID)
        {
            try
            {
                var id = objectID;
                string name = id.ToString().ToLower();
                return name.Contains("food") || name.Contains("potion") || name.Contains("bread") ||
                       name.Contains("meat") || name.Contains("berry") || name.Contains("fish") ||
                       name.Contains("mushroom") || name.Contains("drink") || name.Contains("health") ||
                       name.Contains("mana") || name.Contains("soup") || name.Contains("cake") ||
                       name.Contains("pie") || name.Contains("stew");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifica si un objeto es colocable (bloques, decoraciones, semillas)
        /// </summary>
        private static bool IsPlaceableItem(ObjectID objectID)
        {
            try
            {
                var id = objectID;
                string name = id.ToString().ToLower();
                return name.Contains("seed") || name.Contains("block") || name.Contains("wall") ||
                       name.Contains("torch") || name.Contains("workbench") || name.Contains("furnace") ||
                       name.Contains("chest") || name.Contains("table") || name.Contains("chair") ||
                       name.Contains("door") || name.Contains("bed") || name.Contains("farm") ||
                       name.Contains("floor") || name.Contains("carpet") || name.Contains("bridge") ||
                       name.Contains("fence") || name.Contains("rail");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtiene el inventoryHandler de un slot usando reflexión
        /// </summary>
        private static PugOther.InventoryHandler GetInventoryHandlerFromSlot(PugOther.InventorySlotUI slot)
        {
            try
            {
                // Usar reflexión para obtener inventoryHandler
                var inventoryHandlerField = typeof(PugOther.InventorySlotUI).GetField("inventoryHandler",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (inventoryHandlerField != null)
                {
                    return inventoryHandlerField.GetValue(slot) as PugOther.InventoryHandler;
                }

                // Alternativa: buscar una propiedad en lugar de campo
                var inventoryHandlerProperty = typeof(PugOther.InventorySlotUI).GetProperty("inventoryHandler",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (inventoryHandlerProperty != null)
                {
                    return inventoryHandlerProperty.GetValue(slot) as PugOther.InventoryHandler;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Obtiene el nombre del item usando el slot directamente
        /// </summary>
        private static string GetItemName(PugOther.InventorySlotUI slot)
        {
            try
            {
                var containedObject = slot.GetContainedObjectData();
                var objectName = PugOther.PlayerController.GetObjectName(containedObject, localize: true);
                if (objectName != null && !string.IsNullOrEmpty(objectName.text))
                {
                    return objectName.text;
                }
                return containedObject.objectID.ToString();
            }
            catch
            {
                return "Unknown Item";
            }
        }
    }
}