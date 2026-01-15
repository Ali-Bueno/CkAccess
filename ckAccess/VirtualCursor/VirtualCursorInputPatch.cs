extern alias PugOther;
extern alias Core;
using HarmonyLib;
using UnityEngine;
using ckAccess.Patches.UI;
using ckAccess.Helpers;

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
                if (GameplayStateHelper.IsInExcludedMenu())
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

            // R en teclado para resetear cursor
            if (Input.GetKeyDown(KeyCode.R) && CanProcessInput(KeyCode.R, currentTime))
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
    }
}