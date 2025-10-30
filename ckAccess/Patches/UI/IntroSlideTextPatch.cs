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

            // También resetear el patch de limpieza
            IntroUpdateCleanupPatch.Reset();
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

    /// <summary>
    /// Patch adicional para limpiar el estado de input cuando la intro termina
    /// </summary>
    [HarmonyPatch]
    public static class IntroEndCleanupPatch
    {
        /// <summary>
        /// Target IntroHandler.LoadNextScene - se ejecuta cuando la intro termina y carga la escena Main
        /// </summary>
        static MethodBase TargetMethod()
        {
            var introHandlerType = AccessTools.TypeByName("IntroHandler");
            if (introHandlerType == null)
            {
                UnityEngine.Debug.LogError("Could not find IntroHandler type for cleanup patch");
                return null;
            }

            // LoadNextScene es el método que se ejecuta cuando la intro termina
            var method = AccessTools.Method(introHandlerType, "LoadNextScene");
            if (method == null)
            {
                UnityEngine.Debug.LogError("Could not find IntroHandler.LoadNextScene method for cleanup patch");
                return null;
            }

            UnityEngine.Debug.Log("[IntroEndCleanup] Successfully patched IntroHandler.LoadNextScene");
            return method;
        }

        /// <summary>
        /// Postfix - limpiar el estado de input después de que la intro se cierra
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix()
        {
            try
            {
                UnityEngine.Debug.Log("[IntroEndCleanup] Intro cerrada, limpiando estado de input...");

                // Resetear el patch de texto
                IntroSlideTextPatch.Reset();

                // CRÍTICO: Limpiar cualquier campo de input activo que pueda estar bloqueando el movimiento
                try
                {
                    var input = PugOther.Manager.input;
                    if (input != null)
                    {
                        // InputManager.activeInputField es una propiedad con setter privado
                        var activeInputFieldProp = AccessTools.Property(input.GetType(), "activeInputField");
                        if (activeInputFieldProp != null)
                        {
                            var setMethod = activeInputFieldProp.GetSetMethod(true);
                            if (setMethod != null)
                            {
                                setMethod.Invoke(input, new object[] { null });
                                UnityEngine.Debug.Log("[IntroEndCleanup] activeInputField limpiado via setter");
                            }
                        }

                        // También intentar con SetActiveInputField si existe
                        var setActiveInputMethod = AccessTools.Method(input.GetType(), "SetActiveInputField");
                        if (setActiveInputMethod != null)
                        {
                            setActiveInputMethod.Invoke(input, new object[] { null });
                            UnityEngine.Debug.Log("[IntroEndCleanup] activeInputField limpiado via método");
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[IntroEndCleanup] Error limpiando input state: {ex.Message}");
                }

                UnityEngine.Debug.Log("[IntroEndCleanup] Limpieza completada - el jugador debería poder moverse ahora");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[IntroEndCleanup] Error general en cleanup: {ex}");
            }
        }
    }

    /// <summary>
    /// Patch adicional en Update para limpiar más temprano cuando slidesDone se vuelve true
    /// </summary>
    [HarmonyPatch]
    public static class IntroUpdateCleanupPatch
    {
        private static bool hasCleanedUp = false;

        /// <summary>
        /// Target IntroHandler.Update
        /// </summary>
        static MethodBase TargetMethod()
        {
            var introHandlerType = AccessTools.TypeByName("IntroHandler");
            if (introHandlerType == null)
            {
                UnityEngine.Debug.LogError("Could not find IntroHandler type for Update cleanup patch");
                return null;
            }

            var method = AccessTools.Method(introHandlerType, "Update");
            if (method == null)
            {
                UnityEngine.Debug.LogError("Could not find IntroHandler.Update method for cleanup patch");
                return null;
            }

            return method;
        }

        /// <summary>
        /// Postfix - verificar si slidesDone se volvió true y limpiar el input
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(object __instance)
        {
            try
            {
                // Solo limpiar una vez
                if (hasCleanedUp)
                    return;

                // Obtener el campo slidesDone usando reflexión
                var slidesDoneField = AccessTools.Field(__instance.GetType(), "slidesDone");
                if (slidesDoneField == null)
                    return;

                bool slidesDone = (bool)slidesDoneField.GetValue(__instance);

                // Si slidesDone es true, es hora de limpiar el input
                if (slidesDone)
                {
                    hasCleanedUp = true;
                    UnityEngine.Debug.Log("[IntroUpdateCleanup] slidesDone detectado, limpiando input...");

                    // Limpiar el estado de input
                    CleanupInputState();
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[IntroUpdateCleanup] Error: {ex}");
            }
        }

        /// <summary>
        /// Limpia el estado de input
        /// </summary>
        private static void CleanupInputState()
        {
            try
            {
                var input = PugOther.Manager.input;
                if (input != null)
                {
                    // InputManager.activeInputField es una propiedad con setter privado
                    var activeInputFieldProp = AccessTools.Property(input.GetType(), "activeInputField");
                    if (activeInputFieldProp != null)
                    {
                        var setMethod = activeInputFieldProp.GetSetMethod(true);
                        if (setMethod != null)
                        {
                            setMethod.Invoke(input, new object[] { null });
                            UnityEngine.Debug.Log("[IntroUpdateCleanup] activeInputField limpiado via setter");
                        }
                    }

                    // También intentar con SetActiveInputField si existe
                    var setActiveInputMethod = AccessTools.Method(input.GetType(), "SetActiveInputField");
                    if (setActiveInputMethod != null)
                    {
                        setActiveInputMethod.Invoke(input, new object[] { null });
                        UnityEngine.Debug.Log("[IntroUpdateCleanup] activeInputField limpiado via método");
                    }
                }

                UnityEngine.Debug.Log("[IntroUpdateCleanup] Limpieza completada!");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[IntroUpdateCleanup] Error limpiando: {ex.Message}");
            }
        }

        /// <summary>
        /// Reset para la próxima vez que se abra la intro
        /// </summary>
        public static void Reset()
        {
            hasCleanedUp = false;
        }
    }
}
