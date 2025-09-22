extern alias PugOther;
using HarmonyLib;
using UnityEngine;
using Rewired;

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
                // Verificar si estamos en una pestaña de la ventana de personaje
                if (uiManager.characterWindow != null && uiManager.characterWindow.isShowing)
                {
                    var selectedTab = uiManager.currentSelectedUIElement?.GetComponent<PugOther.CharacterWindowTab>();
                    if (selectedTab != null)
                    {
                        HandleTabNavigation(uiManager);
                        return;
                    }
                }

                // Si no estamos en una pestaña, manejar selección de objetos
                HandleItemSelection(uiManager);
            }
            catch (System.Exception ex)
            {
                UIManager.Speak($"Error en input U: {ex.Message}");
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
                    UIManager.Speak("No hay pestañas disponibles");
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
                    UIManager.Speak("No hay más pestañas disponibles");
                }
            }
            catch (System.Exception ex)
            {
                UIManager.Speak($"Error navegando pestañas: {ex.Message}");
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
                        UIManager.Speak("Pestaña: Equipamiento");
                        break;
                    case 1: // Habilidades
                        characterWindow.ShowSkillsWindow();
                        UIManager.Speak("Pestaña: Habilidades");
                        break;
                    case 2: // Almas
                        characterWindow.ShowSoulsWindow();
                        UIManager.Speak("Pestaña: Almas");
                        break;
                    default:
                        UIManager.Speak($"Pestaña {tabIndex + 1}");
                        break;
                }
            }
            catch (System.Exception ex)
            {
                UIManager.Speak($"Error cambiando pestaña: {ex.Message}");
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
                    UIManager.Speak("Ningún elemento seleccionado");
                    return;
                }

                // Verificar si es un slot de inventario
                var inventorySlot = uiManager.currentSelectedUIElement.GetComponent<PugOther.InventorySlotUI>();
                if (inventorySlot != null)
                {
                    HandleInventorySlotSelection(inventorySlot);
                    return;
                }

                // Verificar si es una habilidad (skill)
                var skillElement = uiManager.currentSelectedUIElement.GetComponent<PugOther.SkillUIElement>();
                if (skillElement != null)
                {
                    HandleSkillSelection(skillElement);
                    return;
                }

                // Verificar si es un talento en el árbol de talentos
                var talentElement = uiManager.currentSelectedUIElement as PugOther.SkillTalentUIElement;
                if (talentElement != null)
                {
                    HandleTalentSelection(talentElement);
                    return;
                }

                // Si no es un elemento específico, simular click izquierdo genérico
                SimulateLeftClick(uiManager);
            }
            catch (System.Exception ex)
            {
                UIManager.Speak($"Error seleccionando objeto: {ex.Message}");
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
                    UIManager.Speak("Slot vacío");
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
                UIManager.Speak($"Seleccionado: {objectName.text}");
            }
            catch (System.Exception ex)
            {
                UIManager.Speak($"Error en selección de slot: {ex.Message}");
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
                skill.OnLeftClicked(false, false);

                // Obtener información básica de la habilidad para confirmar
                var hoverTitle = skill.GetHoverTitle();
                if (hoverTitle != null && !string.IsNullOrEmpty(hoverTitle.text))
                {
                    string skillName = UIManager.GetLocalizedText(hoverTitle.text);
                    if (string.IsNullOrEmpty(skillName))
                    {
                        skillName = PugOther.PugText.ProcessText(hoverTitle.text, hoverTitle.formatFields, true, false);
                    }

                    if (!string.IsNullOrEmpty(skillName))
                    {
                        UIManager.Speak($"Abriendo árbol de talentos: {skillName}");
                    }
                    else
                    {
                        UIManager.Speak("Abriendo árbol de talentos");
                    }
                }
                else
                {
                    UIManager.Speak("Abriendo árbol de talentos");
                }
            }
            catch (System.Exception ex)
            {
                UIManager.Speak($"Error abriendo habilidad: {ex.Message}");
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
                    UIManager.Speak("Ningún elemento seleccionado para acción secundaria");
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
                UIManager.Speak($"Error en acción secundaria: {ex.Message}");
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
                    UIManager.Speak("Slot vacío, no hay acción disponible");
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

                UIManager.Speak("Acción secundaria realizada");
            }
            catch (System.Exception ex)
            {
                UIManager.Speak($"Error en acción secundaria de slot: {ex.Message}");
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
                    UIManager.Speak($"Acción en talento: {talentName}");
                }
                else
                {
                    UIManager.Speak("Talento seleccionado");
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
    }
}