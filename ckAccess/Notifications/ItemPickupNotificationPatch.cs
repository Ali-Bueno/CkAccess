extern alias PugOther;
extern alias Core;

using HarmonyLib;
using ckAccess.Localization;
using UnityEngine;
using System.Collections.Generic;

namespace ckAccess.Notifications
{
    /// <summary>
    /// Parche para detectar cuando el jugador recoge items y generar notificaciones.
    /// Monitorea el inventario del jugador para detectar cambios.
    /// </summary>
    [HarmonyPatch]
    public static class ItemPickupNotificationPatch
    {
        // Cache del inventario anterior para detectar cambios
        private static Dictionary<ObjectID, int> _previousInventory = new Dictionary<ObjectID, int>();
        private static float _lastCheckTime = 0f;
        private const float CHECK_INTERVAL = 0.5f; // Revisar cada 500ms

        // Cache para evitar duplicados rápidos
        private static string _lastAnnouncedItem = null;
        private static float _lastAnnounceTime = 0f;
        private const float ANNOUNCE_DEBOUNCE = 0.3f; // 300ms entre anuncios del mismo item

        // Bandera de inicialización para evitar anunciar el inventario inicial
        private static bool _isInitialized = false;

        /// <summary>
        /// Monitorea el inventario del jugador en cada frame para detectar items nuevos
        /// </summary>
        [HarmonyPatch(typeof(PugOther.PlayerController), "ManagedUpdate")]
        [HarmonyPostfix]
        public static void PlayerController_ManagedUpdate_Postfix(PugOther.PlayerController __instance)
        {
            try
            {
                // Solo revisar para el jugador local
                if (!__instance.isLocal)
                    return;

                // Throttle: solo revisar cada CHECK_INTERVAL segundos
                if (Time.time - _lastCheckTime < CHECK_INTERVAL)
                    return;

                _lastCheckTime = Time.time;

                // Obtener inventario actual
                var playerInventory = __instance.playerInventoryHandler;
                if (playerInventory == null)
                    return;

                // Construir snapshot del inventario actual
                var currentInventory = new Dictionary<ObjectID, int>();
                int inventorySize = playerInventory.size;

                for (int i = 0; i < inventorySize; i++)
                {
                    var containedObject = playerInventory.GetContainedObjectData(i);
                    if (containedObject.objectID != ObjectID.None)
                    {
                        if (currentInventory.ContainsKey(containedObject.objectID))
                        {
                            currentInventory[containedObject.objectID] += containedObject.amount;
                        }
                        else
                        {
                            currentInventory[containedObject.objectID] = containedObject.amount;
                        }
                    }
                }

                // Si no está inicializado, solo capturar el estado inicial sin anunciar nada
                if (!_isInitialized)
                {
                    _previousInventory = currentInventory;
                    _isInitialized = true;
                    return;
                }

                // Comparar con inventario anterior para detectar items nuevos o aumentados
                foreach (var kvp in currentInventory)
                {
                    ObjectID objectID = kvp.Key;
                    int currentAmount = kvp.Value;

                    int previousAmount = 0;
                    _previousInventory.TryGetValue(objectID, out previousAmount);

                    // Si la cantidad aumentó, el jugador recogió items
                    if (currentAmount > previousAmount)
                    {
                        int amountGained = currentAmount - previousAmount;
                        AnnounceItemPickup(objectID, amountGained);
                    }
                }

                // Actualizar cache del inventario
                _previousInventory = currentInventory;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[ItemPickupNotification] Error: {ex}");
            }
        }

        /// <summary>
        /// Anuncia que se recogió un item
        /// </summary>
        private static void AnnounceItemPickup(ObjectID objectID, int amount)
        {
            try
            {
                // Obtener nombre del item
                string itemName = GetItemName(objectID);

                if (string.IsNullOrEmpty(itemName))
                    return;

                // Verificar debounce (evitar anunciar el mismo item múltiples veces seguidas)
                if (itemName == _lastAnnouncedItem && Time.time - _lastAnnounceTime < ANNOUNCE_DEBOUNCE)
                    return;

                // Crear mensaje de notificación
                string message;
                if (amount > 1)
                {
                    var amountValue = amount;
                    message = LocalizationManager.GetText("item_picked_multiple", itemName, amountValue.ToString());
                }
                else
                {
                    message = LocalizationManager.GetText("item_picked_single", itemName);
                }

                // Agregar notificación
                NotificationSystem.AddNotification(message, NotificationSystem.NotificationType.ItemPickup);

                // Actualizar cache
                _lastAnnouncedItem = itemName;
                _lastAnnounceTime = Time.time;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[ItemPickupNotification] Error en AnnounceItemPickup: {ex}");
            }
        }

        /// <summary>
        /// Obtiene el nombre de un item
        /// </summary>
        private static string GetItemName(ObjectID objectID)
        {
            try
            {
                // Por ahora usar el toString del ObjectID
                // En el futuro se puede mejorar para obtener nombres localizados
                var id = objectID;
                string name = id.ToString();

                // Formatear el nombre: remover prefijos y hacer más legible
                name = name.Replace("_", " ");
                return name;
            }
            catch
            {
                var id = objectID;
                return id.ToString();
            }
        }
    }
}
