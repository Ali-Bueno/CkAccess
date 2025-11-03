extern alias PugOther;
using HarmonyLib;
using UnityEngine;
using Rewired;

namespace ckAccess.Patches.UI
{
    /// <summary>
    /// SIMPLIFICADO: Hace que WASD emule directamente el D-Pad
    /// Intercepta PlayerInput porque es lo que el juego usa
    /// </summary>
    [HarmonyPatch(typeof(PugOther.PlayerInput))]
    public static class PlayerInputMenuNavigationPatch
    {
        private static bool _loggedOnce = false;

        /// <summary>
        /// Intercepta WasButtonPressedDownThisFrame para añadir WASD como input adicional
        /// </summary>
        [HarmonyPatch("WasButtonPressedDownThisFrame", new System.Type[] { typeof(PugOther.PlayerInput.InputType), typeof(bool) })]
        [HarmonyPostfix]
        public static void WasButtonPressedDownThisFrame_Postfix(
            PugOther.PlayerInput.InputType inputType,
            bool discardDisabledInput,
            ref bool __result)
        {
            try
            {
                // Log una vez para confirmar que el parche funciona
                if (!_loggedOnce)
                {
                    Plugin.Log.LogInfo("[PlayerInputMenuNavigationPatch] WasButtonPressedDownThisFrame patch is active!");
                    _loggedOnce = true;
                }

                // Si ya detectó el botón, no hacer nada
                if (__result)
                {
                    return;
                }

                // Solo procesar en inventarios
                var uiManager = PugOther.Manager.ui;
                if (uiManager == null || !uiManager.isAnyInventoryShowing)
                {
                    return;
                }

                // Añadir WASD como input adicional para los botones de menú
                switch (inputType)
                {
                    case PugOther.PlayerInput.InputType.MENU_UP:
                        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                        {
                            __result = true;
                            Plugin.Log.LogInfo("[PlayerInputMenuNavigationPatch] W/Up → MENU_UP");
                        }
                        break;

                    case PugOther.PlayerInput.InputType.MENU_DOWN:
                        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                        {
                            __result = true;
                            Plugin.Log.LogInfo("[PlayerInputMenuNavigationPatch] S/Down → MENU_DOWN");
                        }
                        break;

                    case PugOther.PlayerInput.InputType.MENU_LEFT:
                        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                        {
                            __result = true;
                            Plugin.Log.LogInfo("[PlayerInputMenuNavigationPatch] A/Left → MENU_LEFT");
                        }
                        break;

                    case PugOther.PlayerInput.InputType.MENU_RIGHT:
                        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                        {
                            __result = true;
                            Plugin.Log.LogInfo("[PlayerInputMenuNavigationPatch] D/Right → MENU_RIGHT");
                        }
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[PlayerInputMenuNavigationPatch] Error: {ex}");
            }
        }
    }
}
