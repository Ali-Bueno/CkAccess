extern alias PugOther;
using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace ckAccess.Patches.UI
{
    /// <summary>
    /// Patches IntroHandler.UpdateShowing to announce slide text AFTER it's been fully rendered.
    /// This is the safest approach - we only read the text after Unity has finished rendering it.
    /// </summary>
    [HarmonyPatch]
    public static class IntroSlideTextPatch
    {
        private static int lastAnnouncedSlideIndex = -1;
        private static bool hasAnnouncedCurrentSlide = false;

        /// <summary>
        /// Reset the patch state when a new intro starts
        /// </summary>
        public static void Reset()
        {
            lastAnnouncedSlideIndex = -1;
            hasAnnouncedCurrentSlide = false;
        }

        /// <summary>
        /// Target IntroHandler.UpdateShowing - runs when slide is being shown
        /// </summary>
        static MethodBase TargetMethod()
        {
            var introHandlerType = AccessTools.TypeByName("IntroHandler");
            if (introHandlerType == null)
            {
                UnityEngine.Debug.LogError("Could not find IntroHandler type");
                return null;
            }

            var method = AccessTools.Method(introHandlerType, "UpdateShowing");
            if (method == null)
            {
                UnityEngine.Debug.LogError("Could not find IntroHandler.UpdateShowing method");
            }

            return method;
        }

        /// <summary>
        /// Prefix - check if we should announce the text
        /// </summary>
        [HarmonyPrefix]
        public static void Prefix(object __instance)
        {
            try
            {
                // NO anunciar el skip message aquí - se hace en Start()

                // Use reflection to safely get the currentSlideIndex field
                var currentSlideIndexField = AccessTools.Field(__instance.GetType(), "currentSlideIndex");
                if (currentSlideIndexField == null)
                    return;

                int currentSlideIndex = (int)currentSlideIndexField.GetValue(__instance);

                // Check if this is a new slide
                if (currentSlideIndex != lastAnnouncedSlideIndex)
                {
                    hasAnnouncedCurrentSlide = false;
                    lastAnnouncedSlideIndex = currentSlideIndex;
                }

                // If we already announced this slide, don't do it again
                if (hasAnnouncedCurrentSlide)
                    return;

                // Get the text field using reflection
                var textField = AccessTools.Field(__instance.GetType(), "text");
                if (textField == null)
                    return;

                var pugText = textField.GetValue(__instance);
                if (pugText == null)
                    return;

                // Get displayedTextString using reflection
                var displayedTextField = AccessTools.Field(pugText.GetType(), "displayedTextString");
                if (displayedTextField == null)
                    return;

                string displayedText = displayedTextField.GetValue(pugText) as string;

                // Only announce if there's actual text
                if (!string.IsNullOrEmpty(displayedText))
                {
                    // Mark as announced before speaking to avoid race conditions
                    hasAnnouncedCurrentSlide = true;

                    // Announce the text
                    UIManager.Speak(displayedText);
                }
            }
            catch (Exception ex)
            {
                // Silent fail - log warning but don't crash the game
                UnityEngine.Debug.LogWarning($"IntroSlideText accessibility error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Patch en Start para anunciar el mensaje de skip al inicio
    /// </summary>
    [HarmonyPatch]
    public static class IntroStartPatch
    {
        /// <summary>
        /// Target IntroHandler.Start
        /// </summary>
        static MethodBase TargetMethod()
        {
            var introHandlerType = AccessTools.TypeByName("IntroHandler");
            if (introHandlerType == null)
            {
                UnityEngine.Debug.LogError("Could not find IntroHandler type for Start patch");
                return null;
            }

            var method = AccessTools.Method(introHandlerType, "Start");
            if (method == null)
            {
                UnityEngine.Debug.LogError("Could not find IntroHandler.Start method");
                return null;
            }

            return method;
        }

        /// <summary>
        /// Postfix - anunciar skip message después de que Start() termine
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix()
        {
            try
            {
                // Esperar un pequeño momento y luego anunciar
                Plugin.Instance.StartCoroutine(AnnounceSkipMessageDelayed());
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[IntroStart] Error: {ex}");
            }
        }

        private static IEnumerator AnnounceSkipMessageDelayed()
        {
            // Esperar 0.5 segundos para que la intro esté lista
            yield return new UnityEngine.WaitForSeconds(0.5f);

            string skipMessage = Localization.LocalizationManager.GetText("intro_skip_message");
            UIManager.Speak(skipMessage, interrupt: true);
            UnityEngine.Debug.Log("[IntroStart] Skip message announced");
        }
    }

}
