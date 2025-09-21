extern alias PugOther;
extern alias PugUnExt;
using HarmonyLib;
using UnityEngine;
using Rewired;

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

            // OPTIMIZACIÓN: Solo procesar si realmente hay un inventario abierto
            // Esto evita interferencia con la quick bar y otros elementos UI
            if (uiManager == null || !uiManager.isAnyInventoryShowing)
            {
                return true; // Let the original method run - no inventory open
            }

            // Verificar que hay un elemento seleccionado válido
            if (uiManager.currentSelectedUIElement == null)
            {
                return true; // Let the original method run - no valid UI element
            }

            // OPTIMIZACIÓN ADICIONAL: Solo procesar si hay input de navegación
            // Esto reduce la interferencia con el ratón cuando no hay input de teclado/gamepad
            bool hasNavigationInput = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) ||
                                     Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) ||
                                     Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) ||
                                     Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);

            var player = ReInput.players.GetPlayer(0);
            if (player != null)
            {
                hasNavigationInput = hasNavigationInput ||
                                   player.GetButtonDown("SwapNextHotbar") ||
                                   player.GetButtonDown("SwapPreviousHotbar") ||
                                   player.GetButtonDown("QuickStack") ||
                                   player.GetButtonDown("Sort");
            }

            // Si no hay input de navegación, dejar que el método original maneje todo
            if (!hasNavigationInput)
            {
                return true; // Let the original method run - no navigation input
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
                var nextElement = uiManager.currentSelectedUIElement.GetAdjacentUIElement(direction, uiManager.currentSelectedUIElement.transform.position);
                if (nextElement != null)
                {
                    __instance.pointer.position = nextElement.transform.position;
                    
                    var method = typeof(PugOther.UIMouse).GetMethod("TrySelectNewElement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (method != null)
                    {
                        method.Invoke(__instance, new object[] { nextElement, false });
                    }
                }
                // We handled the input, skip the original method
                return false; 
            }

            // No custom input detected, let the original method run
            return true;
        }
    }
}