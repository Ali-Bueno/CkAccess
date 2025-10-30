using HarmonyLib;
using UnityEngine;

namespace ckAccess.Tutorials
{
    /// <summary>
    /// Allows navigation through tutorial buffer using ' (quote) and , (comma) keys.
    /// ' = Next tutorial (more recent)
    /// , = Previous tutorial (older)
    /// Shift + ' = Jump to last tutorial
    /// Shift + , = Jump to first tutorial
    /// </summary>
    [HarmonyPatch]
    public static class TutorialNavigationPatch
    {
        private static float lastNavigationTime = 0f;
        private const float NAVIGATION_COOLDOWN = 0.2f; // 200ms between navigations

        /// <summary>
        /// Target method - patch Manager.Update to check for tutorial navigation keys
        /// </summary>
        static System.Reflection.MethodBase TargetMethod()
        {
            var managerType = AccessTools.TypeByName("Manager");
            if (managerType == null)
            {
                UnityEngine.Debug.LogError("Could not find Manager type");
                return null;
            }

            var method = AccessTools.Method(managerType, "Update");
            if (method == null)
            {
                UnityEngine.Debug.LogError("Could not find Manager.Update method");
            }

            return method;
        }

        [HarmonyPostfix]
        public static void Postfix()
        {
            try
            {
                // Only process if enough time has passed
                if (Time.unscaledTime - lastNavigationTime < NAVIGATION_COOLDOWN)
                    return;

                // Check for tutorial navigation keys
                bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                // ' key (Quote) - Next tutorial
                if (Input.GetKeyDown(KeyCode.Quote))
                {
                    if (shiftPressed)
                    {
                        TutorialBuffer.JumpToLast();
                    }
                    else
                    {
                        TutorialBuffer.NavigateNext();
                    }
                    lastNavigationTime = Time.unscaledTime;
                }
                // ¡ is not a standard KeyCode, but we can use the comma key for previous
                // The user mentioned ¡ but that's hard to detect, using Comma instead
                // If they want a different key, we can change it
                else if (Input.GetKeyDown(KeyCode.Comma))
                {
                    if (shiftPressed)
                    {
                        TutorialBuffer.JumpToFirst();
                    }
                    else
                    {
                        TutorialBuffer.NavigatePrevious();
                    }
                    lastNavigationTime = Time.unscaledTime;
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error in tutorial navigation: {ex}");
            }
        }
    }
}
