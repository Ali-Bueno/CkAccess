extern alias PugOther;
extern alias Core;
using HarmonyLib;
using UnityEngine;
using ckAccess.Patches.UI;
using Rewired;

namespace ckAccess.VirtualCursor
{
    /// <summary>
    /// Patch to handle virtual cursor input when not in UI mode
    /// </summary>
    [HarmonyPatch(typeof(PugOther.UIMouse))]
    public static class VirtualCursorInputPatch
    {
        // Debounce system to prevent multiple inputs
        private static float lastInputTime = 0f;
        private static KeyCode lastPressedKey = KeyCode.None;
        private const float INPUT_DEBOUNCE_TIME = 0.1f; // 100ms between inputs

        // Sistema para rastrear teclas mantenidas actualmente
        private static bool _uKeyHeld = false;
        private static bool _oKeyHeld = false;
        [HarmonyPatch("UpdateMouseUIInput")]
        [HarmonyPostfix]
        public static void UpdateMouseUIInput_Postfix(PugOther.UIMouse __instance)
        {
            try
            {
                // NOTA: PlayerInputPatch.UpdateVirtualAimInput() se llama ahora desde
                // PlayerInputPatch.UpdateState_Prefix() para asegurar que se ejecute siempre.

                // OPTIMIZACIÓN: Solo ejecutar el resto si hay teclas presionadas
                if (!HasAnyVirtualCursorKeyPressed())
                {
                    return;
                }

                // Only handle virtual cursor input when not in UI/inventory mode
                var uiManager = PugOther.Manager.ui;
                if (uiManager != null && uiManager.isAnyInventoryShowing)
                {
                    return; // Let the existing UI navigation handle this
                }

                // Check if we're in a main menu or other incompatible state
                if (IsInExcludedMenu())
                {
                    return; // Skip virtual cursor input
                }

                // Handle hotbar navigation first (works in game and inventory)
                if (Patches.UI.HotbarAccessibilityPatch.HandleHotbarNavigation())
                {
                    return; // Hotbar navigation handled, don't process other input
                }

                // Handle debug/feedback inputs
                HandleVirtualCursorInput();
            }
            catch (System.Exception ex)
            {
                UIManager.Speak($"Error in patch: {ex.Message}");
            }
        }

        /// <summary>
        /// Optimización: Solo detecta si hay teclas del cursor virtual presionadas
        /// para evitar procesamiento innecesario que interfiere con el ratón.
        /// </summary>
        private static bool HasAnyVirtualCursorKeyPressed()
        {
            return Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.J) ||
                   Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.L) ||
                   Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.U) ||
                   Input.GetKeyDown(KeyCode.O) ||
                   Input.GetKeyDown(KeyCode.T) ||
                   Input.GetKeyDown(KeyCode.M) ||
                   // Hotbar navigation keys
                   (Input.GetKey(KeyCode.LeftAlt) && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
                    Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.Space))) ||
                   // También detectar teclas soltadas para acciones
                   Input.GetKeyUp(KeyCode.U) || Input.GetKeyUp(KeyCode.O);
        }

        private static void HandleVirtualCursorInput()
        {
            float currentTime = UnityEngine.Time.time;

            // I/J/K/L ahora controlan el stick derecho virtual - el feedback lo da PlayerInputPatch
            // Solo manejamos teclas de debug/info aquí

            // R en teclado o L3 (presión del stick izquierdo) del mando
            if ((Input.GetKeyDown(KeyCode.R) || IsR3Pressed()) && CanProcessInput(KeyCode.R, currentTime))
            {
                VirtualCursor.ResetToPlayer();
                PlayerInputPatch.ResetCursorDistance(); // Resetear también la distancia del cursor
            }
            else if (Input.GetKeyDown(KeyCode.P) && CanProcessInput(KeyCode.P, currentTime))
            {
                VirtualCursor.DebugCurrentPosition(); // Debug position information
            }
            else if (Input.GetKeyDown(KeyCode.T) && CanProcessInput(KeyCode.T, currentTime))
            {
                VirtualCursor.TestCoordinateMapping(); // Test coordinate mapping
            }
            else if (Input.GetKeyDown(KeyCode.M) && CanProcessInput(KeyCode.M, currentTime))
            {
                VirtualCursor.AnnouncePlayerPositionDetailed(); // Posición detallada del jugador
            }

            // U/O ahora son manejados directamente por PlayerInputPatch como triggers
            // Ya no necesitamos PrimaryAction/SecondaryAction
        }

        /// <summary>
        /// Placeholder para detección de botones del mando
        /// Nota: Core Keeper/Rewired captura completamente el input de los botones del mando
        /// antes de que podamos detectarlos, por lo que esta funcionalidad no está disponible.
        /// El reset del cursor solo funciona con la tecla R del teclado.
        /// </summary>
        private static bool IsR3Pressed()
        {
            // Los botones del mando no se pueden detectar debido a cómo Core Keeper maneja el input
            // El juego consume el input de los botones antes de que nuestros parches puedan interceptarlo
            return false;
        }
        
        private static bool CanProcessInput(KeyCode key, float currentTime)
        {
            // Check if enough time has passed since last input of same key
            if (lastPressedKey == key && (currentTime - lastInputTime) < INPUT_DEBOUNCE_TIME)
            {
                return false;
            }

            // Update last input tracking
            lastInputTime = currentTime;
            lastPressedKey = key;
            return true;
        }

        /// <summary>
        /// Verifica si alguna tecla de acción está siendo mantenida
        /// </summary>
        public static bool IsAnyActionKeyHeld()
        {
            return _uKeyHeld || _oKeyHeld;
        }

        /// <summary>
        /// Verifica si una tecla específica está siendo mantenida
        /// </summary>
        public static bool IsKeyHeld(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.U: return _uKeyHeld;
                case KeyCode.O: return _oKeyHeld;
                default: return false;
            }
        }
        
        private static bool IsInExcludedMenu()
        {
            try
            {
                // CRÍTICO: Si no hay jugador activo, definitivamente estamos en un menú
                var main = PugOther.Manager.main;
                if (main == null || main.player == null)
                    return true;

                // CRÍTICO: Verificar si hay un campo de texto activo (escribiendo nombres, etc.)
                var input = PugOther.Manager.input;
                if (input != null)
                {
                    // Verificar si textInputIsActive (propiedad calculada)
                    try
                    {
                        var textInputIsActiveProp = AccessTools.Property(input.GetType(), "textInputIsActive");
                        if (textInputIsActiveProp != null)
                        {
                            bool textInputIsActive = (bool)textInputIsActiveProp.GetValue(input);
                            if (textInputIsActive)
                                return true; // Estamos escribiendo
                        }
                    }
                    catch { }
                }

                var uiManager = PugOther.Manager.ui;
                if (uiManager != null)
                {

                    // Check for pause menu
                    if (UnityEngine.Time.timeScale == 0f)
                        return true;
                }

                // Verificar el estado del jugador - si está en el mundo y puede moverse
                try
                {
                    var playerController = main.player as PugOther.PlayerController;
                    if (playerController == null)
                        return true;

                    // Si el controlador no está activo, estamos en un menú
                    if (!playerController.enabled || !playerController.gameObject.activeInHierarchy)
                        return true;
                }
                catch { }

                return false;
            }
            catch (System.Exception)
            {
                // Si hay error detectando el estado, asumir que estamos en menú por seguridad
                return true;
            }
        }
    }
}