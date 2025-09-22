extern alias PugOther;
extern alias PugUnExt;
using HarmonyLib;
using UnityEngine;
using Rewired;
using System.Linq;
using ckAccess.Localization;

namespace ckAccess.Patches.UI
{
    [HarmonyPatch(typeof(PugOther.UIMouse))]
    public static class UIMouseInputPatch
    {
        [HarmonyPatch("UpdateMouseUIInput")]
        [HarmonyPrefix]
        public static bool UpdateMouseUIInput_Prefix(PugOther.UIMouse __instance, out bool leftClickWasUsed, out bool rightClickWasUsed)
        {
            leftClickWasUsed = false;
            rightClickWasUsed = false;

            var uiManager = PugOther.Manager.ui;

            // OPTIMIZACIÓN: Solo procesar si hay un inventario o árbol de talentos abierto
            // Esto evita interferencia con la quick bar y otros elementos UI
            if (uiManager == null || (!uiManager.isAnyInventoryShowing && !IsSkillTalentTreeOpen(uiManager)))
            {
                return true; // Let the original method run - no inventory or talent tree open
            }

            // Verificar que hay un elemento seleccionado válido
            if (uiManager.currentSelectedUIElement == null)
            {
                return true; // Let the original method run - no valid UI element
            }

            // OPTIMIZACIÓN ADICIONAL: Solo procesar si hay input de navegación o acciones
            // Esto reduce la interferencia con el ratón cuando no hay input de teclado/gamepad
            bool hasNavigationInput = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) ||
                                     Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) ||
                                     Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) ||
                                     Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);

            // Añadir detección de U/O
            bool hasActionInput = Input.GetKeyDown(KeyCode.U) || Input.GetKeyDown(KeyCode.O);

            var player = ReInput.players.GetPlayer(0);
            if (player != null)
            {
                hasNavigationInput = hasNavigationInput ||
                                   player.GetButtonDown("SwapNextHotbar") ||
                                   player.GetButtonDown("SwapPreviousHotbar") ||
                                   player.GetButtonDown("QuickStack") ||
                                   player.GetButtonDown("Sort");
            }

            // Si no hay input de navegación ni de acciones, dejar que el método original maneje todo
            if (!hasNavigationInput && !hasActionInput)
            {
                return true; // Let the original method run - no relevant input
            }

            // Manejar acciones U/O antes que navegación
            if (hasActionInput)
            {
                if (Input.GetKeyDown(KeyCode.U))
                {
                    InventoryUIInputPatch.HandleUInputPublic(uiManager);
                }
                else if (Input.GetKeyDown(KeyCode.O))
                {
                    InventoryUIInputPatch.HandleOInputPublic(uiManager);
                }
                return true; // Permitir que el método original continúe
            }

            if (player == null)
            {
                return true; // Let the original method run
            }

            PugUnExt.Pug.UnityExtensions.Direction.Id direction = PugUnExt.Pug.UnityExtensions.Direction.Id.zero;

            // Keyboard Input
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                direction = PugUnExt.Pug.UnityExtensions.Direction.Id.forward;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                direction = PugUnExt.Pug.UnityExtensions.Direction.Id.back;
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                direction = PugUnExt.Pug.UnityExtensions.Direction.Id.left;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                direction = PugUnExt.Pug.UnityExtensions.Direction.Id.right;
            }

            // D-Pad Input (using the correct, logged action names)
            if (direction == PugUnExt.Pug.UnityExtensions.Direction.Id.zero)
            {
                if (player.GetButtonDown("SwapNextHotbar")) // D-Pad Up
                {
                    direction = PugUnExt.Pug.UnityExtensions.Direction.Id.forward;
                }
                else if (player.GetButtonDown("SwapPreviousHotbar")) // D-Pad Down
                {
                    direction = PugUnExt.Pug.UnityExtensions.Direction.Id.back;
                }
                else if (player.GetButtonDown("QuickStack")) // D-Pad Left
                {
                    direction = PugUnExt.Pug.UnityExtensions.Direction.Id.left;
                }
                else if (player.GetButtonDown("Sort")) // D-Pad Right
                {
                    direction = PugUnExt.Pug.UnityExtensions.Direction.Id.right;
                }
            }

            if (direction != PugUnExt.Pug.UnityExtensions.Direction.Id.zero)
            {
                // MEJORADO: Forzar posición del ratón al centro antes de navegar
                // Esto soluciona el problema de dependencia de la posición del ratón físico
                var currentElement = uiManager.currentSelectedUIElement;
                if (currentElement != null)
                {
                    // Usar la posición actual del elemento como punto de referencia
                    var referencePosition = currentElement.transform.position;
                    __instance.pointer.position = referencePosition;

                    var nextElement = currentElement.GetAdjacentUIElement(direction, referencePosition);
                    if (nextElement != null)
                    {
                        __instance.pointer.position = nextElement.transform.position;

                        var method = typeof(PugOther.UIMouse).GetMethod("TrySelectNewElement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (method != null)
                        {
                            method.Invoke(__instance, new object[] { nextElement, false });
                        }

                        // IMPORTANTE: Anunciar elementos que no fueron detectados automáticamente
                        AnnounceElementIfNeeded(nextElement);
                    }
                    else
                    {
                        // Si no hay elemento adyacente, buscar el siguiente disponible en toda la interfaz
                        var alternativeElement = FindNextAvailableElement(direction, currentElement);
                        if (alternativeElement != null)
                        {
                            __instance.pointer.position = alternativeElement.transform.position;

                            var method = typeof(PugOther.UIMouse).GetMethod("TrySelectNewElement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (method != null)
                            {
                                method.Invoke(__instance, new object[] { alternativeElement, false });
                            }

                            AnnounceElementIfNeeded(alternativeElement);
                        }
                    }
                }
                // We handled the input, skip the original method
                return false;
            }

            // No custom input detected, let the original method run
            return true;
        }

        /// <summary>
        /// Anuncia elementos que no tienen parches específicos
        /// </summary>
        private static void AnnounceElementIfNeeded(PugOther.UIelement element)
        {
            try
            {
                if (element == null) return;

                // Verificar si es un botón sin parche específico
                var button = element.GetComponent<PugOther.ButtonUIElement>();
                if (button != null)
                {
                    // FILTRO: No anunciar skills - tienen su propio parche específico
                    var skillComponent = button.GetComponent<PugOther.SkillUIElement>();
                    if (skillComponent != null) return; // Es una skill, no anunciar como botón

                    var hoverTitle = button.GetHoverTitle();
                    if (hoverTitle != null && !string.IsNullOrEmpty(hoverTitle.text))
                    {
                        string buttonText = UIManager.GetLocalizedText(hoverTitle.text);
                        if (string.IsNullOrEmpty(buttonText)) buttonText = hoverTitle.text;

                        // Detectar tipos específicos de botones por su texto
                        string lowerText = buttonText.ToLower();
                        if (lowerText.Contains("stats") || lowerText.Contains("estadística"))
                        {
                            UIManager.Speak(LocalizationManager.GetText("stats_button", buttonText));
                        }
                        else if (lowerText.Contains("preset") || lowerText.Contains("equip"))
                        {
                            UIManager.Speak(LocalizationManager.GetText("equipment_preset", buttonText));
                        }
                        else if (lowerText.Contains("craft") || lowerText.Contains("fabricar"))
                        {
                            UIManager.Speak(LocalizationManager.GetText("crafting_button", buttonText));
                        }
                        else if (lowerText.Contains("sort") || lowerText.Contains("organizar"))
                        {
                            UIManager.Speak(LocalizationManager.GetText("organize_button", buttonText));
                        }
                        else if (lowerText.Contains("quick") || lowerText.Contains("rápido"))
                        {
                            UIManager.Speak(LocalizationManager.GetText("quick_action_button", buttonText));
                        }
                        else
                        {
                            UIManager.Speak(LocalizationManager.GetText("button", buttonText));
                        }
                        return;
                    }
                }

                // Verificar si es una pestaña (tab)
                var tab = element.GetComponent<PugOther.CharacterWindowTab>();
                if (tab != null)
                {
                    var hoverTitle = tab.GetHoverTitle();
                    if (hoverTitle != null && !string.IsNullOrEmpty(hoverTitle.text))
                    {
                        string tabText = UIManager.GetLocalizedText(hoverTitle.text);
                        if (string.IsNullOrEmpty(tabText)) tabText = hoverTitle.text;
                        UIManager.Speak(LocalizationManager.GetText("tab", tabText));
                        return;
                    }
                }

                // Verificar por nombre del GameObject para elementos no identificados
                string objectName = element.gameObject.name;
                if (!string.IsNullOrEmpty(objectName) &&
                    !objectName.StartsWith("GameObject") &&
                    !ShouldIgnoreElement(objectName))
                {
                    string lowerName = objectName.ToLower();

                    // Detectar elementos específicos por nombre
                    if (lowerName.Contains("preset"))
                    {
                        UIManager.Speak(LocalizationManager.GetText("preset", CleanObjectName(objectName)));
                    }
                    else if (lowerName.Contains("stats") || lowerName.Contains("statistics"))
                    {
                        UIManager.Speak(LocalizationManager.GetText("statistics", CleanObjectName(objectName)));
                    }
                    else if (lowerName.Contains("pouch") || lowerName.Contains("bag"))
                    {
                        UIManager.Speak(LocalizationManager.GetText("bag", CleanObjectName(objectName)));
                    }
                    else if (lowerName.Contains("shortcut"))
                    {
                        UIManager.Speak(LocalizationManager.GetText("shortcut", CleanObjectName(objectName)));
                    }
                    else if (lowerName.Contains("button"))
                    {
                        UIManager.Speak(LocalizationManager.GetText("button", CleanObjectName(objectName)));
                    }
                    else if (lowerName.Contains("tab"))
                    {
                        UIManager.Speak(LocalizationManager.GetText("tab", CleanObjectName(objectName)));
                    }
                    // NO anunciar slots normales - ya tienen su propio parche
                }
            }
            catch
            {
                // Error silencioso
            }
        }

        /// <summary>
        /// Limpia el nombre del objeto para mejor legibilidad
        /// </summary>
        private static string CleanObjectName(string objectName)
        {
            if (string.IsNullOrEmpty(objectName)) return "Desconocido";

            // Remover sufijos comunes de Unity
            objectName = objectName.Replace("(Clone)", "")
                                  .Replace("UI", "")
                                  .Replace("Element", "")
                                  .Replace("Button", "")
                                  .Replace("Tab", "")
                                  .Trim();

            // Capitalizar primera letra
            if (objectName.Length > 0)
            {
                objectName = char.ToUpper(objectName[0]) + objectName.Substring(1);
            }

            return objectName;
        }

        /// <summary>
        /// Busca el siguiente elemento disponible cuando GetAdjacentUIElement falla
        /// SIMPLIFICADO: Por ahora solo retorna null para evitar errores de compilación
        /// </summary>
        private static PugOther.UIelement FindNextAvailableElement(PugUnExt.Pug.UnityExtensions.Direction.Id direction, PugOther.UIelement currentElement)
        {
            // TODO: Implementar búsqueda alternativa sin FindObjectsOfType
            return null;
        }

        /// <summary>
        /// Verifica si un elemento debe ser ignorado para anuncios
        /// </summary>
        private static bool ShouldIgnoreElement(string objectName)
        {
            if (string.IsNullOrEmpty(objectName)) return true;

            string lowerName = objectName.ToLower();

            // Ignorar elementos clonados de inventario (ya tienen parches específicos)
            if (lowerName.Contains("inventoryslot") && lowerName.Contains("clone")) return true;
            if (lowerName.Contains("inventoryslotplayer")) return true;
            if (lowerName.Contains("slot") && (lowerName.Contains("clone") || lowerName.Contains("masked"))) return true;

            // Ignorar otros elementos internos
            if (lowerName.Contains("background")) return true;
            if (lowerName.Contains("outline")) return true;
            if (lowerName.Contains("shadow")) return true;
            if (lowerName.Contains("container")) return true;

            return false;
        }

        /// <summary>
        /// Verifica si un elemento está en contexto de inventario
        /// </summary>
        private static bool IsElementInInventoryContext(PugOther.UIelement element)
        {
            try
            {
                // Verificar si está en ventana de personaje
                var characterWindow = element.GetComponentInParent<PugOther.CharacterWindowUI>();
                if (characterWindow != null) return true;

                // Verificar si está en algún inventario
                var inventoryUI = element.GetComponentInParent<PugOther.InventoryUI>();
                if (inventoryUI != null) return true;

                // Verificar por nombre del GameObject
                string objectName = element.gameObject.name.ToLower();
                return objectName.Contains("inventory") ||
                       objectName.Contains("slot") ||
                       objectName.Contains("tab") ||
                       objectName.Contains("button") ||
                       objectName.Contains("equipment");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifica si hay un árbol de talentos de skill abierto
        /// </summary>
        private static bool IsSkillTalentTreeOpen(PugOther.UIManager uiManager)
        {
            try
            {
                // Por ahora, detección simplificada - TODO: mejorar cuando sepamos la API exacta
                // Si hay un elemento seleccionado que es SkillTalentUIElement, asumimos que está abierto
                var currentElement = uiManager.currentSelectedUIElement;
                if (currentElement != null)
                {
                    var talentElement = currentElement as PugOther.SkillTalentUIElement;
                    if (talentElement != null) return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}