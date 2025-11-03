extern alias PugOther;
extern alias PugUnExt;
extern alias Core;
using HarmonyLib;
using UnityEngine;
using Rewired;
using System;
using System.Linq;
using ckAccess.Localization;
using Vector3 = Core::UnityEngine.Vector3;

namespace ckAccess.Patches.UI
{
    /// <summary>
    /// Parche para habilitar navegación por teclado (WASD/Flechas) y D-Pad en inventarios
    /// REFACTORIZADO: Código más limpio, sin duplicación, mejor manejo del pointer
    /// </summary>
    [HarmonyPatch(typeof(PugOther.UIMouse))]
    public static class UIMouseInputPatch
    {
        // Variable estática para rastrear la última sección anunciada
        private static string _lastAnnouncedSection = "";

        /// <summary>
        /// Parche para GetMouseUIViewPosition - hace que el juego ignore el mouse físico en inventarios
        /// y use la posición del elemento seleccionado
        /// </summary>
        [HarmonyPatch("GetMouseUIViewPosition")]
        [HarmonyPrefix]
        public static bool GetMouseUIViewPosition_Prefix(ref Core::UnityEngine.Vector2 __result)
        {
            try
            {
                var uiManager = PugOther.Manager.ui;

                // Solo interferir si hay inventario abierto Y hay un elemento seleccionado
                if (uiManager != null && uiManager.isAnyInventoryShowing && uiManager.currentSelectedUIElement != null)
                {
                    // Devolver la posición del elemento seleccionado en lugar del mouse físico
                    var pos = uiManager.currentSelectedUIElement.transform.position;
                    __result = new Core::UnityEngine.Vector2(pos.x, pos.y);
                    return false; // Skip el método original
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error in GetMouseUIViewPosition_Prefix: {ex}");
            }

            return true; // Ejecutar el método original
        }

        /// <summary>
        /// Parche PREFIX para interceptar navegación WASD/D-Pad Y acciones U/O
        /// ANTES de que el juego procese el mouse físico
        /// </summary>
        [HarmonyPatch("UpdateMouseUIInput")]
        [HarmonyPrefix]
        public static void UpdateMouseUIInput_Prefix(PugOther.UIMouse __instance, out bool leftClickWasUsed, out bool rightClickWasUsed)
        {
            leftClickWasUsed = false;
            rightClickWasUsed = false;

            try
            {
                var uiManager = PugOther.Manager.ui;

                // Solo procesar si hay un inventario, árbol de talentos o UI de crafting abierto
                if (uiManager == null || (!uiManager.isAnyInventoryShowing && !IsSkillTalentTreeOpen(uiManager) && !uiManager.isCraftingUIShowing))
                {
                    return;
                }

                // PRIMERO: Detectar navegación (WASD/Flechas/D-Pad)
                var direction = DetectNavigationInput();
                if (direction != PugUnExt.Pug.UnityExtensions.Direction.Id.zero)
                {
                    HandleKeyboardNavigation(__instance, uiManager, direction);
                }

                // SEGUNDO: Verificar que hay un elemento seleccionado para acciones U/O
                if (uiManager.currentSelectedUIElement == null)
                {
                    return;
                }

                // SEGUNDO: Detectar input de acciones (U/O y gamepad)
                bool uInput = Input.GetKeyDown(KeyCode.U) || IsGamepadButtonPressed("R2");
                bool oInput = Input.GetKeyDown(KeyCode.O) || IsGamepadButtonPressed("L2");

                // Manejar acciones U/O
                if (uInput)
                {
                    InventoryUIInputPatch.HandleUInputPublic(uiManager);
                }
                else if (oInput)
                {
                    InventoryUIInputPatch.HandleOInputPublic(uiManager);
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error in UIMouseInputPatch Prefix: {ex}");
            }
        }

        /// <summary>
        /// Detecta input de navegación de todas las fuentes (teclado + D-Pad)
        /// </summary>
        private static PugUnExt.Pug.UnityExtensions.Direction.Id DetectNavigationInput()
        {
            // Teclado (WASD / Flechas)
            bool wPressed = Input.GetKeyDown(KeyCode.W);
            bool sPressed = Input.GetKeyDown(KeyCode.S);
            bool aPressed = Input.GetKeyDown(KeyCode.A);
            bool dPressed = Input.GetKeyDown(KeyCode.D);
            bool upPressed = Input.GetKeyDown(KeyCode.UpArrow);
            bool downPressed = Input.GetKeyDown(KeyCode.DownArrow);
            bool leftPressed = Input.GetKeyDown(KeyCode.LeftArrow);
            bool rightPressed = Input.GetKeyDown(KeyCode.RightArrow);

            if (upPressed || wPressed)
                return PugUnExt.Pug.UnityExtensions.Direction.Id.forward;
            if (downPressed || sPressed)
                return PugUnExt.Pug.UnityExtensions.Direction.Id.back;
            if (leftPressed || aPressed)
                return PugUnExt.Pug.UnityExtensions.Direction.Id.left;
            if (rightPressed || dPressed)
                return PugUnExt.Pug.UnityExtensions.Direction.Id.right;

            // D-Pad (usando las acciones correctas del juego)
            var player = ReInput.players.GetPlayer(0);
            if (player != null)
            {
                if (player.GetButtonDown("SwapNextHotbar")) // D-Pad Up
                    return PugUnExt.Pug.UnityExtensions.Direction.Id.forward;
                if (player.GetButtonDown("SwapPreviousHotbar")) // D-Pad Down
                    return PugUnExt.Pug.UnityExtensions.Direction.Id.back;
                if (player.GetButtonDown("QuickStack")) // D-Pad Left
                    return PugUnExt.Pug.UnityExtensions.Direction.Id.left;
                if (player.GetButtonDown("Sort")) // D-Pad Right
                    return PugUnExt.Pug.UnityExtensions.Direction.Id.right;
            }

            return PugUnExt.Pug.UnityExtensions.Direction.Id.zero;
        }

        /// <summary>
        /// Maneja la navegación con teclado/D-Pad
        /// OPTIMIZADO: Código más simple y robusto
        /// </summary>
        private static void HandleKeyboardNavigation(PugOther.UIMouse uiMouse, PugOther.UIManager uiManager, PugUnExt.Pug.UnityExtensions.Direction.Id direction)
        {
            try
            {
                var currentElement = uiManager.currentSelectedUIElement;
                if (currentElement == null) return;

                // Obtener el siguiente elemento usando el método nativo del juego
                var nextElement = currentElement.GetAdjacentUIElement(direction, currentElement.transform.position);

                if (nextElement != null && nextElement.isShowing)
                {
                    // Actualizar la posición del puntero del mouse al nuevo elemento
                    var pos = nextElement.transform.position;
                    uiMouse.pointer.position = new Vector3(pos.x, pos.y, pos.z);

                    // Llamar a TrySelectNewElement usando reflexión
                    var method = typeof(PugOther.UIMouse).GetMethod("TrySelectNewElement",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (method != null)
                    {
                        method.Invoke(uiMouse, new object[] { nextElement, false });
                    }

                    // Anunciar el nuevo elemento si es necesario
                    AnnounceElementIfNeeded(nextElement);
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error en HandleKeyboardNavigation: {ex}");
            }
        }

        /// <summary>
        /// Parche POSTFIX en TrySelectNewElement para detectar cambios de selección
        /// Esto funciona tanto con teclado como con mando
        /// Solo anuncia cambios de sección, NO mueve el pointer (el juego ya lo hace)
        /// </summary>
        [HarmonyPatch("TrySelectNewElement", new Type[] { typeof(PugOther.UIelement), typeof(bool) })]
        [HarmonyPostfix]
        public static void TrySelectNewElement_Postfix(PugOther.UIelement selectedUIElement)
        {
            try
            {
                if (selectedUIElement != null)
                {
                    // Solo anunciar secciones si es un slot de inventario
                    var slot = selectedUIElement.GetComponent<PugOther.InventorySlotUI>();
                    if (slot != null)
                    {
                        AnnounceInventorySectionIfChanged(slot);
                    }
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error in TrySelectNewElement_Postfix: {ex}");
            }
        }

        /// <summary>
        /// Anuncia elementos que no tienen parches específicos
        /// SIMPLIFICADO: Solo anuncia lo esencial
        /// </summary>
        private static void AnnounceElementIfNeeded(PugOther.UIelement element)
        {
            try
            {
                if (element == null) return;

                // Verificar si es un slot de inventario y anunciar cambio de sección
                var slot = element.GetComponent<PugOther.InventorySlotUI>();
                if (slot != null)
                {
                    AnnounceInventorySectionIfChanged(slot);
                    return; // Los slots ya tienen su propio sistema de anuncios
                }

                // Verificar si es un botón
                var button = element.GetComponent<PugOther.ButtonUIElement>();
                if (button != null)
                {
                    // FILTRO: No anunciar skills - tienen su propio parche específico
                    var skillComponent = button.GetComponent<PugOther.SkillUIElement>();
                    if (skillComponent != null) return;

                    var hoverTitle = button.GetHoverTitle();
                    if (hoverTitle != null && !string.IsNullOrEmpty(hoverTitle.text))
                    {
                        string buttonText = UIManager.GetLocalizedText(hoverTitle.text);
                        if (string.IsNullOrEmpty(buttonText)) buttonText = hoverTitle.text;

                        UIManager.Speak(LocalizationManager.GetText("button", buttonText));
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
            }
            catch
            {
                // Error silencioso
            }
        }

        /// <summary>
        /// Anuncia cuando el usuario cambia entre secciones (cofre/inventario)
        /// </summary>
        private static void AnnounceInventorySectionIfChanged(PugOther.InventorySlotUI slot)
        {
            try
            {
                // Acceder al contenedor del slot mediante reflexión
                var containerField = typeof(PugOther.SlotUIBase).GetField("slotsUIContainer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (containerField != null)
                {
                    var container = containerField.GetValue(slot) as PugOther.ItemSlotsUIContainer;
                    if (container != null)
                    {
                        string sectionName = GetSectionName(container.containerType);

                        // Solo anunciar si cambió de sección
                        if (sectionName != _lastAnnouncedSection)
                        {
                            _lastAnnouncedSection = sectionName;
                            UIManager.Speak(LocalizationManager.GetText(sectionName));
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error in AnnounceInventorySectionIfChanged: {ex}");
            }
        }

        /// <summary>
        /// Obtiene el nombre de la sección según el tipo de contenedor
        /// </summary>
        private static string GetSectionName(PugOther.ItemSlotsUIContainerType containerType)
        {
            switch (containerType)
            {
                case PugOther.ItemSlotsUIContainerType.PlayerInventory:
                    return "section_player_inventory";
                case PugOther.ItemSlotsUIContainerType.ChestInventory:
                    return "section_chest_inventory";
                case PugOther.ItemSlotsUIContainerType.CraftingInventory:
                    return "section_crafting_inventory";
                case PugOther.ItemSlotsUIContainerType.PlayerEquipment:
                    return "section_equipment";
                case PugOther.ItemSlotsUIContainerType.PouchInventory:
                    return "section_pouch";
                default:
                    return "section_unknown";
            }
        }

        /// <summary>
        /// Verifica si hay un árbol de talentos de skill abierto
        /// </summary>
        private static bool IsSkillTalentTreeOpen(PugOther.UIManager uiManager)
        {
            try
            {
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

        /// <summary>
        /// Verifica si un botón del gamepad está presionado usando Rewired
        /// </summary>
        private static bool IsGamepadButtonPressed(string buttonName)
        {
            try
            {
                var player = ReInput.players.GetPlayer(0);
                if (player == null) return false;

                // R2 y L2 son triggers, no botones directos
                // Necesitamos verificar usando el sistema de acciones
                if (buttonName == "R2")
                {
                    // R2 trigger - verificar axis
                    return player.GetAxis("RightTrigger") > 0.5f && !WasR2PressedLastFrame();
                }
                else if (buttonName == "L2")
                {
                    // L2 trigger - verificar axis
                    return player.GetAxis("LeftTrigger") > 0.5f && !WasL2PressedLastFrame();
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool _wasR2PressedLastFrame = false;
        private static bool _wasL2PressedLastFrame = false;

        private static bool WasR2PressedLastFrame()
        {
            try
            {
                var player = ReInput.players.GetPlayer(0);
                bool isPressed = player.GetAxis("RightTrigger") > 0.5f;
                bool wasPressed = _wasR2PressedLastFrame;
                _wasR2PressedLastFrame = isPressed;
                return wasPressed;
            }
            catch
            {
                return false;
            }
        }

        private static bool WasL2PressedLastFrame()
        {
            try
            {
                var player = ReInput.players.GetPlayer(0);
                bool isPressed = player.GetAxis("LeftTrigger") > 0.5f;
                bool wasPressed = _wasL2PressedLastFrame;
                _wasL2PressedLastFrame = isPressed;
                return wasPressed;
            }
            catch
            {
                return false;
            }
        }
    }
}
