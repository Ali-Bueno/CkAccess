extern alias PugOther;
using HarmonyLib;
using UnityEngine;
using Rewired;
using ckAccess.Localization;

namespace ckAccess.Patches.UI
{
    /// <summary>
    /// Parche para extender la funcionalidad del inventario con controles U/O
    /// U = Cambiar pestañas / Seleccionar objetos
    /// O = Funciones secundarias (dividir stacks, etc.)
    /// </summary>
    [HarmonyPatch]
    public static class InventoryUIInputPatch
    {
        // Variables removidas para limpiar warnings - funcionalidad movida a UIMouseInputPatch


        /// <summary>
        /// Método público para manejar input U desde otros parches
        /// </summary>
        public static void HandleUInputPublic(PugOther.UIManager uiManager)
        {
            HandleUInput(uiManager);
        }

        /// <summary>
        /// Método público para manejar input O desde otros parches
        /// </summary>
        public static void HandleOInputPublic(PugOther.UIManager uiManager)
        {
            HandleOInput(uiManager);
        }

        /// <summary>
        /// Maneja la tecla U: cambiar pestañas o seleccionar objetos
        /// </summary>
        private static void HandleUInput(PugOther.UIManager uiManager)
        {
            try
            {
                // PRIORIDAD 1: Verificar si estamos en una pestaña de preset de equipo
                if (uiManager.characterWindow != null && uiManager.characterWindow.isShowing)
                {
                    var selectedTab = uiManager.currentSelectedUIElement?.GetComponent<PugOther.CharacterWindowTab>();
                    if (selectedTab != null)
                    {
                        // Verificar si es un preset ANTES de tratar como navegación de pestañas
                        bool isPreset = IsEquipmentPresetTab(selectedTab);

                        if (isPreset)
                        {
                            HandleEquipmentPresetSelection(selectedTab, uiManager);
                            return;
                        }
                        else
                        {
                            HandleTabNavigation(uiManager);
                            return;
                        }
                    }
                }

                // PRIORIDAD 2: Si no estamos en una pestaña, manejar selección de objetos
                HandleItemSelection(uiManager);
            }
            catch (System.Exception ex)
            {
                UIManager.Speak(LocalizationManager.GetText("selection_error", ex.Message));
                UnityEngine.Debug.LogError($"Exception in HandleUInput: {ex}");
            }
        }

        /// <summary>
        /// Maneja la navegación entre pestañas con U
        /// </summary>
        private static void HandleTabNavigation(PugOther.UIManager uiManager)
        {
            try
            {
                var characterWindow = uiManager.characterWindow;
                if (characterWindow?.windowTabs == null || characterWindow.windowTabs.Count == 0)
                {
                    UIManager.Speak(LocalizationManager.GetText("no_tabs_available"));
                    return;
                }

                // Encontrar la pestaña actualmente activa
                int currentTabIndex = -1;
                for (int i = 0; i < characterWindow.windowTabs.Count; i++)
                {
                    if (characterWindow.windowTabs[i].gameObject.activeInHierarchy)
                    {
                        // Verificar si esta pestaña está "activa" verificando el color o estado
                        if (IsTabActive(characterWindow.windowTabs[i]))
                        {
                            currentTabIndex = i;
                            break;
                        }
                    }
                }

                // Si no encontramos la pestaña activa, asumir la primera
                if (currentTabIndex == -1)
                {
                    currentTabIndex = 0;
                }

                // Buscar la siguiente pestaña disponible
                int nextTabIndex = GetNextAvailableTab(characterWindow, currentTabIndex);

                if (nextTabIndex != currentTabIndex)
                {
                    // Cambiar a la siguiente pestaña
                    SwitchToTab(characterWindow, nextTabIndex);
                }
                else
                {
                    UIManager.Speak(LocalizationManager.GetText("no_more_tabs"));
                }
            }
            catch (System.Exception ex)
            {
                UIManager.Speak(LocalizationManager.GetText("navigation_error", ex.Message));
            }
        }

        /// <summary>
        /// Verifica si una pestaña está actualmente activa
        /// </summary>
        private static bool IsTabActive(PugOther.CharacterWindowTab tab)
        {
            try
            {
                // Una pestaña activa tiene color blanco en el background
                var background = tab.background;
                if (background == null) return false;

                var currentColor = background.color;
                var whiteColor = UnityEngine.Color.white;

                // Comparar colores con tolerancia debido a precisión flotante
                return UnityEngine.Mathf.Approximately(currentColor.r, whiteColor.r) &&
                       UnityEngine.Mathf.Approximately(currentColor.g, whiteColor.g) &&
                       UnityEngine.Mathf.Approximately(currentColor.b, whiteColor.b) &&
                       UnityEngine.Mathf.Approximately(currentColor.a, whiteColor.a);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtiene el índice de la siguiente pestaña disponible
        /// </summary>
        private static int GetNextAvailableTab(PugOther.CharacterWindowUI characterWindow, int currentIndex)
        {
            int totalTabs = characterWindow.windowTabs.Count;

            // Buscar la siguiente pestaña activa (que esté visible)
            for (int i = 1; i <= totalTabs; i++)
            {
                int nextIndex = (currentIndex + i) % totalTabs;
                var tab = characterWindow.windowTabs[nextIndex];

                if (tab.gameObject.activeInHierarchy)
                {
                    return nextIndex;
                }
            }

            return currentIndex; // Si no hay más pestañas, quedarse en la actual
        }

        /// <summary>
        /// Cambia a la pestaña especificada
        /// </summary>
        private static void SwitchToTab(PugOther.CharacterWindowUI characterWindow, int tabIndex)
        {
            try
            {
                // Usar los métodos públicos de CharacterWindowUI para cambiar pestañas
                switch (tabIndex)
                {
                    case 0: // Equipamiento
                        characterWindow.ShowEquipmentWindow();
                        UIManager.Speak(LocalizationManager.GetText("tab_equipment"));
                        break;
                    case 1: // Habilidades
                        characterWindow.ShowSkillsWindow();
                        UIManager.Speak(LocalizationManager.GetText("tab_skills"));
                        break;
                    case 2: // Almas
                        characterWindow.ShowSoulsWindow();
                        UIManager.Speak(LocalizationManager.GetText("tab_souls"));
                        break;
                    default:
                        UIManager.Speak(LocalizationManager.GetText("tab_number", tabIndex + 1));
                        break;
                }
            }
            catch (System.Exception ex)
            {
                UIManager.Speak(LocalizationManager.GetText("navigation_error", ex.Message));
            }
        }

        /// <summary>
        /// Maneja la selección de objetos con U
        /// </summary>
        private static void HandleItemSelection(PugOther.UIManager uiManager)
        {
            try
            {
                if (uiManager.currentSelectedUIElement == null)
                {
                    UIManager.Speak(LocalizationManager.GetText("no_element_selected"));
                    return;
                }

                // PRIORIDAD 1: Verificar si es una pestaña de preset de equipo (DEBE ser primera verificación)
                var presetTab = uiManager.currentSelectedUIElement.GetComponent<PugOther.CharacterWindowTab>();
                if (presetTab != null)
                {
                    bool isPreset = IsEquipmentPresetTab(presetTab);

                    if (isPreset)
                    {
                        HandleEquipmentPresetSelection(presetTab, uiManager);
                        return; // IMPORTANTE: Salir aquí para evitar verificaciones adicionales
                    }
                }

                // PRIORIDAD 2: Verificar si es un slot de inventario
                var inventorySlot = uiManager.currentSelectedUIElement.GetComponent<PugOther.InventorySlotUI>();
                if (inventorySlot != null)
                {
                    HandleInventorySlotSelection(inventorySlot);
                    return;
                }

                // PRIORIDAD 3: Verificar si es un botón de estadísticas
                if (IsStatsButton(uiManager.currentSelectedUIElement))
                {
                    HandleStatsButtonSelection(uiManager);
                    return;
                }

                // PRIORIDAD 4: Verificar si es una habilidad (skill) - SOLO si no es preset
                var skillElement = uiManager.currentSelectedUIElement.GetComponent<PugOther.SkillUIElement>();
                if (skillElement != null)
                {
                    // Double-check: Asegurar que no es un preset tab que también tiene SkillUIElement
                    if (presetTab != null && IsEquipmentPresetTab(presetTab))
                    {
                        HandleEquipmentPresetSelection(presetTab, uiManager);
                        return;
                    }

                    HandleSkillSelection(skillElement);
                    return;
                }

                // PRIORIDAD 5: Verificar si es un talento en el árbol de talentos
                var talentElement = uiManager.currentSelectedUIElement as PugOther.SkillTalentUIElement;
                if (talentElement != null)
                {
                    HandleTalentSelection(talentElement);
                    return;
                }

                // PRIORIDAD 6: Si no es un elemento específico, simular click izquierdo genérico
                SimulateLeftClick(uiManager);
            }
            catch (System.Exception ex)
            {
                UIManager.Speak($"Error seleccionando objeto: {ex.Message}");
                UnityEngine.Debug.LogError($"Exception in HandleItemSelection: {ex}");
            }
        }

        /// <summary>
        /// Maneja la selección específica de slots de inventario
        /// </summary>
        private static void HandleInventorySlotSelection(PugOther.InventorySlotUI slot)
        {
            try
            {
                var containedObject = slot.GetContainedObjectData();

                if (containedObject.objectID == ObjectID.None)
                {
                    UIManager.Speak(LocalizationManager.GetText("empty_slot"));
                    return;
                }

                // Simular click izquierdo en el slot usando reflection
                var clickEvent = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                {
                    button = UnityEngine.EventSystems.PointerEventData.InputButton.Left,
                    position = slot.transform.position
                };

                var pointerClickMethod = typeof(PugOther.InventorySlotUI).GetMethod("OnPointerClick",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (pointerClickMethod != null)
                {
                    pointerClickMethod.Invoke(slot, new object[] { clickEvent });
                }
                else
                {
                    // Fallback: intentar como IPointerClickHandler
                    var clickHandler = slot as UnityEngine.EventSystems.IPointerClickHandler;
                    clickHandler?.OnPointerClick(clickEvent);
                }

                // Anunciar la acción
                var objectName = PugOther.PlayerController.GetObjectName(containedObject, true);
                UIManager.Speak(LocalizationManager.GetText("selected_item", objectName.text));
            }
            catch (System.Exception ex)
            {
                UIManager.Speak(LocalizationManager.GetText("slot_selection_error", ex.Message));
            }
        }

        /// <summary>
        /// Maneja la selección específica de habilidades (skills)
        /// </summary>
        private static void HandleSkillSelection(PugOther.SkillUIElement skill)
        {
            try
            {
                // Llamar directamente al método OnLeftClicked de la habilidad
                // El SkillTalentTreePatch se encargará de anunciar si se abre o cierra el árbol
                skill.OnLeftClicked(false, false);

                // NO anunciar aquí - dejar que SkillTalentTreePatch maneje los anuncios
                // de apertura/cierre del árbol de talentos correctamente
            }
            catch (System.Exception ex)
            {
                UIManager.Speak(LocalizationManager.GetText("skill_error", ex.Message));
            }
        }

        /// <summary>
        /// Simula un click izquierdo en el elemento actual
        /// </summary>
        private static void SimulateLeftClick(PugOther.UIManager uiManager)
        {
            try
            {
                var clickEvent = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                {
                    button = UnityEngine.EventSystems.PointerEventData.InputButton.Left,
                    position = uiManager.currentSelectedUIElement.transform.position
                };

                var clickable = uiManager.currentSelectedUIElement.GetComponent<UnityEngine.EventSystems.IPointerClickHandler>();
                clickable?.OnPointerClick(clickEvent);

                UIManager.Speak("Click izquierdo simulado");
            }
            catch (System.Exception ex)
            {
                UIManager.Speak($"Error simulando click: {ex.Message}");
            }
        }

        /// <summary>
        /// Maneja la tecla O (funciones secundarias)
        /// </summary>
        private static void HandleOInput(PugOther.UIManager uiManager)
        {
            try
            {
                if (uiManager.currentSelectedUIElement == null)
                {
                    UIManager.Speak(LocalizationManager.GetText("no_element_selected_secondary"));
                    return;
                }

                // Verificar si es un slot de inventario
                var inventorySlot = uiManager.currentSelectedUIElement.GetComponent<PugOther.InventorySlotUI>();
                if (inventorySlot != null)
                {
                    HandleInventorySlotSecondaryAction(inventorySlot);
                    return;
                }

                // Para otros elementos, simular click derecho
                SimulateRightClick(uiManager);
            }
            catch (System.Exception ex)
            {
                UIManager.Speak(LocalizationManager.GetText("secondary_action_error_ui", ex.Message));
            }
        }

        /// <summary>
        /// Maneja acciones secundarias en slots de inventario (click derecho)
        /// </summary>
        private static void HandleInventorySlotSecondaryAction(PugOther.InventorySlotUI slot)
        {
            try
            {
                var containedObject = slot.GetContainedObjectData();

                if (containedObject.objectID == ObjectID.None)
                {
                    UIManager.Speak(LocalizationManager.GetText("empty_slot_no_action"));
                    return;
                }

                // Simular click derecho en el slot usando reflection
                var clickEvent = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                {
                    button = UnityEngine.EventSystems.PointerEventData.InputButton.Right,
                    position = slot.transform.position
                };

                var pointerClickMethod = typeof(PugOther.InventorySlotUI).GetMethod("OnPointerClick",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (pointerClickMethod != null)
                {
                    pointerClickMethod.Invoke(slot, new object[] { clickEvent });
                }
                else
                {
                    // Fallback: intentar como IPointerClickHandler
                    var clickHandler = slot as UnityEngine.EventSystems.IPointerClickHandler;
                    clickHandler?.OnPointerClick(clickEvent);
                }

                UIManager.Speak(LocalizationManager.GetText("secondary_action_performed"));
            }
            catch (System.Exception ex)
            {
                UIManager.Speak(LocalizationManager.GetText("secondary_slot_error", ex.Message));
            }
        }

        /// <summary>
        /// Simula un click derecho en el elemento actual
        /// </summary>
        private static void SimulateRightClick(PugOther.UIManager uiManager)
        {
            try
            {
                var clickEvent = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                {
                    button = UnityEngine.EventSystems.PointerEventData.InputButton.Right,
                    position = uiManager.currentSelectedUIElement.transform.position
                };

                var clickable = uiManager.currentSelectedUIElement.GetComponent<UnityEngine.EventSystems.IPointerClickHandler>();
                clickable?.OnPointerClick(clickEvent);

                UIManager.Speak("Click derecho simulado");
            }
            catch (System.Exception ex)
            {
                UIManager.Speak($"Error simulando click derecho: {ex.Message}");
            }
        }

        /// <summary>
        /// Maneja la selección específica de talentos en árboles de talento
        /// </summary>
        private static void HandleTalentSelection(PugOther.SkillTalentUIElement talent)
        {
            try
            {
                // Llamar al método OnLeftClicked del talento para invertir punto
                talent.OnLeftClicked(false, false);

                // Obtener información básica del talento para confirmar
                var hoverTitle = talent.GetHoverTitle();
                if (hoverTitle != null && !string.IsNullOrEmpty(hoverTitle.text))
                {
                    string talentName = ProcessTalentText(hoverTitle.text, hoverTitle.formatFields);

                    // Confirmar la acción (el juego ya manejó si era válida)
                    UIManager.Speak(LocalizationManager.GetText("talent_action", talentName));
                }
                else
                {
                    UIManager.Speak(LocalizationManager.GetText("talent_selected"));
                }
            }
            catch (System.Exception ex)
            {
                UIManager.Speak($"Error seleccionando talento: {ex.Message}");
            }
        }

        /// <summary>
        /// Procesa el texto del talento aplicando formatFields correctamente
        /// </summary>
        private static string ProcessTalentText(string text, object[] formatFields)
        {
            if (string.IsNullOrEmpty(text)) return "";

            try
            {
                // Si hay formatFields, aplicarlos primero
                if (formatFields != null && formatFields.Length > 0)
                {
                    string[] stringFields = new string[formatFields.Length];
                    for (int i = 0; i < formatFields.Length; i++)
                    {
                        stringFields[i] = formatFields[i]?.ToString() ?? "";
                    }

                    string processedText = PugOther.PugText.ProcessText(text, stringFields, true, false);
                    if (!string.IsNullOrEmpty(processedText))
                    {
                        return processedText;
                    }
                }

                // Fallback a localización
                string localizedText = UIManager.GetLocalizedText(text);
                if (!string.IsNullOrEmpty(localizedText))
                {
                    return localizedText;
                }

                // Último fallback
                return text;
            }
            catch
            {
                return text;
            }
        }

        /// <summary>
        /// Obtiene el nombre de la pestaña basado en su índice
        /// </summary>
        private static string GetTabName(int tabIndex)
        {
            switch (tabIndex)
            {
                case 0: return "Inventario";
                case 1: return "Equipamiento";
                case 2: return "Habilidades";
                case 3: return "Crafteo";
                default: return $"Pestaña {tabIndex + 1}";
            }
        }

        /// <summary>
        /// Verifica si la pestaña es un preset de equipo
        /// </summary>
        private static bool IsEquipmentPresetTab(PugOther.CharacterWindowTab tab)
        {
            try
            {
                // Método 1: Verificar usando characterWindow.presetTabs (más confiable)
                var characterWindow = PugOther.Manager.ui.characterWindow;
                if (characterWindow?.presetTabs != null)
                {
                    bool isInPresetTabs = characterWindow.presetTabs.Contains(tab);
                    if (isInPresetTabs)
                    {
                        return true;
                    }
                }

                // Método 2: Verificar por posición en ventana
                if (characterWindow != null)
                {
                    var allTabs = characterWindow.GetComponentsInChildren<PugOther.CharacterWindowTab>(true);
                    for (int i = 0; i < allTabs.Length; i++)
                    {
                        if (allTabs[i] == tab)
                        {
                            // Los presets suelen estar en índices específicos (0-5)
                            if (i >= 0 && i <= 5)
                            {
                                string tabName = tab.gameObject.name.ToLower();
                                bool isPresetByName = tabName.Contains("preset") ||
                                                     tabName.Contains("loadout") ||
                                                     tabName.StartsWith("tab") ||
                                                     System.Text.RegularExpressions.Regex.IsMatch(tabName, @"tab[_\s]*[0-9]");

                                if (isPresetByName)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }

                // Método 3: Verificar por nombre de GameObject (fallback)
                string simpleName = tab.gameObject.name.ToLower();
                return simpleName.Contains("preset") || simpleName.Contains("equipment");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Exception in IsEquipmentPresetTab: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Maneja la selección de presets de equipo
        /// </summary>
        private static void HandleEquipmentPresetSelection(PugOther.CharacterWindowTab presetTab, PugOther.UIManager uiManager)
        {
            try
            {
                // Obtener la ventana de personaje
                var characterWindow = uiManager.characterWindow;
                if (characterWindow?.presetTabs == null)
                {
                    UIManager.Speak(LocalizationManager.GetText("equipment_preset_error"));
                    return;
                }

                // Encontrar el índice del preset seleccionado
                int presetIndex = characterWindow.presetTabs.IndexOf(presetTab);
                if (presetIndex < 0)
                {
                    UIManager.Speak(LocalizationManager.GetText("equipment_preset_not_found"));
                    return;
                }

                // Cambiar al preset usando el método correcto del juego
                characterWindow.SetActivePreset(presetIndex);

                // Buscar información del preset para anunciar
                var hoverTitle = presetTab.GetHoverTitle();
                if (hoverTitle != null && !string.IsNullOrEmpty(hoverTitle.text))
                {
                    string presetName = UIManager.GetLocalizedText(hoverTitle.text);
                    if (string.IsNullOrEmpty(presetName)) presetName = hoverTitle.text;
                    UIManager.Speak(LocalizationManager.GetText("equipment_preset_selected", presetName));
                }
                else
                {
                    UIManager.Speak(LocalizationManager.GetText("equipment_preset_selected_generic", presetIndex + 1));
                }
            }
            catch (System.Exception ex)
            {
                UIManager.Speak(LocalizationManager.GetText("equipment_preset_error", ex.Message));
            }
        }

        /// <summary>
        /// Verifica si el elemento es un botón de estadísticas
        /// </summary>
        private static bool IsStatsButton(PugOther.UIelement element)
        {
            try
            {
                string elementName = element.gameObject.name.ToLower();
                return elementName.Contains("stats") ||
                       elementName.Contains("statistics") ||
                       elementName.Contains("character") && elementName.Contains("window");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Maneja la selección del botón de estadísticas
        /// </summary>
        private static void HandleStatsButtonSelection(PugOther.UIManager uiManager)
        {
            try
            {
                // Intentar acceder al método ToggleStatsWindow en CharacterWindowUI
                var characterWindow = uiManager.characterWindow;
                if (characterWindow != null)
                {
                    // Usar reflection para acceder al método ToggleStatsWindow
                    var toggleMethod = typeof(PugOther.CharacterWindowUI).GetMethod("ToggleStatsWindow",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (toggleMethod != null)
                    {
                        toggleMethod.Invoke(characterWindow, null);
                        UIManager.Speak(LocalizationManager.GetText("stats_window_toggled"));
                    }
                    else
                    {
                        // Fallback: simular click genérico
                        SimulateLeftClick(uiManager);
                        UIManager.Speak(LocalizationManager.GetText("stats"));
                    }
                }
                else
                {
                    SimulateLeftClick(uiManager);
                    UIManager.Speak(LocalizationManager.GetText("stats_button_activated"));
                }
            }
            catch (System.Exception ex)
            {
                UIManager.Speak(LocalizationManager.GetText("stats_error", ex.Message));
            }
        }
    }
}