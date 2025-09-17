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
            if (uiManager == null || !uiManager.isAnyInventoryShowing || uiManager.currentSelectedUIElement == null)
            {
                return true; // Let the original method run
            }

            var player = ReInput.players.GetPlayer(0);
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