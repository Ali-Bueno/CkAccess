extern alias PugOther;
extern alias Core;
using HarmonyLib;
using UnityEngine;
using ckAccess.Patches.UI;

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
        private static bool _eKeyHeld = false;
        [HarmonyPatch("UpdateMouseUIInput")]
        [HarmonyPostfix]
        public static void UpdateMouseUIInput_Postfix(PugOther.UIMouse __instance)
        {
            try
            {
                // OPTIMIZACIÓN: Solo ejecutar si hay teclas presionadas para minimizar interferencia
                if (!HasAnyVirtualCursorKeyPressed())
                {
                    return; // No hay input del cursor virtual, evitar procesamiento innecesario
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

                // Handle virtual cursor input - this runs AFTER the original method
                // so it doesn't interfere with mouse input
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
                   Input.GetKeyDown(KeyCode.O) || Input.GetKeyDown(KeyCode.E) ||
                   Input.GetKeyDown(KeyCode.T) ||
                   Input.GetKeyDown(KeyCode.M) ||
                   // Hotbar navigation keys
                   (Input.GetKey(KeyCode.LeftAlt) && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
                    Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.Space))) ||
                   // También detectar teclas soltadas para acciones
                   Input.GetKeyUp(KeyCode.U) || Input.GetKeyUp(KeyCode.O) || Input.GetKeyUp(KeyCode.E);
        }

        private static void HandleVirtualCursorInput()
        {
            float currentTime = UnityEngine.Time.time;

            // Virtual cursor movement with I, J, K, L keys (vi/vim style)
            // Top-down view: I=up, K=down, J=left, L=right
            if (Input.GetKeyDown(KeyCode.I) && CanProcessInput(KeyCode.I, currentTime))
            {
                VirtualCursor.MoveCursor(CursorDirection.Up);     // I = Up (+Z)
            }
            else if (Input.GetKeyDown(KeyCode.K) && CanProcessInput(KeyCode.K, currentTime))
            {
                VirtualCursor.MoveCursor(CursorDirection.Down);   // K = Down (-Z)
            }
            else if (Input.GetKeyDown(KeyCode.J) && CanProcessInput(KeyCode.J, currentTime))
            {
                VirtualCursor.MoveCursor(CursorDirection.Left);   // J = Left (-X)
            }
            else if (Input.GetKeyDown(KeyCode.L) && CanProcessInput(KeyCode.L, currentTime))
            {
                VirtualCursor.MoveCursor(CursorDirection.Right);  // L = Right (+X)
            }
            else if (Input.GetKeyDown(KeyCode.R) && CanProcessInput(KeyCode.R, currentTime))
            {
                VirtualCursor.ResetToPlayer();
            }
            // Manejar teclas de acción - detectar presión inicial y estado mantenido
            if (Input.GetKeyDown(KeyCode.U) && CanProcessInput(KeyCode.U, currentTime))
            {
                _uKeyHeld = true;
                VirtualCursor.PrimaryAction(); // Left click equivalent
            }
            else if (Input.GetKeyUp(KeyCode.U))
            {
                _uKeyHeld = false;
                VirtualCursor.StopPrimaryAction(); // Stop action when key released
            }
            else if (Input.GetKeyDown(KeyCode.O) && CanProcessInput(KeyCode.O, currentTime))
            {
                _oKeyHeld = true;
                VirtualCursor.SecondaryAction(); // Right click equivalent (usar objetos)
            }
            else if (Input.GetKeyUp(KeyCode.O))
            {
                _oKeyHeld = false;
                VirtualCursor.StopSecondaryAction();
            }
            else if (Input.GetKeyDown(KeyCode.E) && CanProcessInput(KeyCode.E, currentTime))
            {
                _eKeyHeld = true;
                VirtualCursor.InteractionAction(); // Interaction key equivalent (interactuar con objetos)
            }
            else if (Input.GetKeyUp(KeyCode.E))
            {
                _eKeyHeld = false;
                VirtualCursor.StopInteractionAction();
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
            // TECLA TAB ELIMINADA - Ya no necesaria
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
            return _uKeyHeld || _oKeyHeld || _eKeyHeld;
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
                case KeyCode.E: return _eKeyHeld;
                default: return false;
            }
        }
        
        private static bool IsInExcludedMenu()
        {
            try
            {
                // Check for main menu state - if the game is not in play mode
                var main = PugOther.Manager.main;
                if (main?.player == null)
                    return true; // No player means we're likely in menus
                
                // Check if pause menu is open
                var uiManager = PugOther.Manager.ui;
                if (uiManager != null)
                {
                    // Check for pause menu - usually indicated by game being paused or specific UI state
                    if (UnityEngine.Time.timeScale == 0f) // Game is paused
                        return true;
                        
                    // Alternative check: look for pause menu objects
                    var pauseMenuObjects = Core::UnityEngine.Object.FindObjectsOfType<Core::UnityEngine.MonoBehaviour>();
                    foreach (var obj in pauseMenuObjects)
                    {
                        if (obj?.gameObject?.name?.ToLower().Contains("pause") == true)
                            return true;
                    }
                }
                
                return false;
            }
            catch (System.Exception)
            {
                // If we can't determine the state, assume we're in an excluded menu
                return true;
            }
        }
    }
}