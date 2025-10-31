extern alias PugOther;
extern alias I2Loc;

using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace ckAccess.Patches.UI
{
    /// <summary>
    /// Parche para leer los emotes/diálogos del personaje (ej. "Esa madera me podría ser útil", "No hay energía")
    /// Los emotes de tutorial se añaden al buffer de notificaciones para poder navegarlos
    /// </summary>
    [HarmonyPatch]
    public static class EmoteTextAccessibilityPatch
    {
        private static string lastAnnouncedEmote = "";
        private static float lastAnnounceTime = 0f;
        private const float DEBOUNCE_TIME = 2.0f; // 2 segundos para evitar duplicados pero permitir repeticiones de tutoriales

        /// <summary>
        /// Target Emote.OnOccupied
        /// </summary>
        static MethodBase TargetMethod()
        {
            var emoteType = AccessTools.TypeByName("Emote");
            if (emoteType == null)
            {
                UnityEngine.Debug.LogError("[EmoteTextPatch] Could not find Emote type");
                return null;
            }

            var method = AccessTools.Method(emoteType, "OnOccupied");
            if (method == null)
            {
                UnityEngine.Debug.LogError("[EmoteTextPatch] Could not find Emote.OnOccupied method");
            }

            return method;
        }

        [HarmonyPostfix]
        public static void Postfix(object __instance)
        {
            try
            {
                // NO anunciar durante cutscenes
                if (IsCutsceneActive())
                {
                    return;
                }

                // Obtener el tipo de emote usando reflexión
                var emoteType = __instance.GetType();
                var emoteTypeField = AccessTools.Field(emoteType, "emoteTypeInput");
                if (emoteTypeField == null) return;

                var emoteTypeValue = emoteTypeField.GetValue(__instance);
                if (emoteTypeValue == null) return;

                // Obtener el enum value como int
                int emoteTypeInt = (int)emoteTypeValue;
                if (emoteTypeInt < 0) return; // __illegal__

                // Log temporal para debugging - ver qué emotes se están activando
                UnityEngine.Debug.Log($"[EmoteTextPatch] EmoteType={emoteTypeInt}");

                // Obtener el texto del emote
                var textField = AccessTools.Field(emoteType, "text");
                if (textField == null) return;

                var pugText = textField.GetValue(__instance);
                if (pugText == null) return;

                // Obtener el texto procesado (ya localizado)
                var processTextMethod = AccessTools.Method(pugText.GetType(), "ProcessText");
                if (processTextMethod == null) return;

                string emoteText = processTextMethod.Invoke(pugText, null) as string;
                if (string.IsNullOrEmpty(emoteText)) return;

                // Debounce: evitar anunciar el mismo emote dos veces seguidas
                float currentTime = Time.unscaledTime;
                if (emoteText == lastAnnouncedEmote && (currentTime - lastAnnounceTime) < DEBOUNCE_TIME)
                {
                    return;
                }

                lastAnnouncedEmote = emoteText;
                lastAnnounceTime = currentTime;

                // Verificar si viene de un tutorial usando call stack (universal, funciona en todos los idiomas)
                bool isTutorial = IsTutorialFromCallStack();

                if (isTutorial)
                {
                    // Los emotes de tutorial van al buffer de tutoriales (teclas '/Ñ)
                    Notifications.TutorialBufferSystem.AddTutorial(emoteText);
                }
                else
                {
                    // Verificar si es feedback crítico que debe anunciarse inmediatamente
                    bool isCriticalFeedback = IsCriticalFeedback(emoteTypeInt);

                    if (isCriticalFeedback)
                    {
                        // Feedback crítico se anuncia directamente
                        UIManager.Speak(emoteText, interrupt: false);
                    }
                    else
                    {
                        // Otros emotes informativos van al buffer de notificaciones
                        Notifications.NotificationSystem.AddNotification(
                            emoteText,
                            Notifications.NotificationSystem.NotificationType.Info
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[EmoteTextPatch] Error: {ex}");
            }
        }

        /// <summary>
        /// Verifica si un emote viene de un sistema de Tutorial usando call stack.
        /// Método universal que funciona en todos los idiomas.
        /// </summary>
        private static bool IsTutorialFromCallStack()
        {
            try
            {
                // Obtener la pila de llamadas
                var stackTrace = new System.Diagnostics.StackTrace();

                for (int i = 0; i < stackTrace.FrameCount; i++)
                {
                    var frame = stackTrace.GetFrame(i);
                    var method = frame?.GetMethod();

                    if (method != null)
                    {
                        string className = method.DeclaringType?.Name ?? "";

                        // Si viene de alguna clase Tutorial, es un tutorial
                        if (className.Contains("Tutorial"))
                        {
                            UnityEngine.Debug.Log($"[EmoteTextPatch] Tutorial detectado por call stack: '{className}'");
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[EmoteTextPatch] Error en call stack: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Verifica si un emote es feedback crítico que debe anunciarse inmediatamente
        /// </summary>
        private static bool IsCriticalFeedback(int emoteTypeValue)
        {
            switch (emoteTypeValue)
            {
                // Feedback inmediato crítico
                case 5:  // ObjectNeedsEnergy (el objeto necesita energía)
                case 6:  // NeedHigherMiningSkill (necesitas más skill de minería)
                case 17: // ObjectIsIndestructible (el objeto es indestructible)
                case 18: // ObjectIsImmune (el objeto es inmune)
                case 27: // NotEnoughMana (no hay suficiente maná)
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Verifica si hay una cutscene activa
        /// </summary>
        private static bool IsCutsceneActive()
        {
            try
            {
                // Verificar CutsceneHandler
                var cutsceneHandlerType = AccessTools.TypeByName("CutsceneHandler");
                if (cutsceneHandlerType != null)
                {
                    var cutsceneHandler = UnityEngine.Object.FindObjectOfType(cutsceneHandlerType);
                    if (cutsceneHandler != null)
                    {
                        var isPlayingProp = AccessTools.Property(cutsceneHandlerType, "isPlaying");
                        if (isPlayingProp != null)
                        {
                            bool isPlaying = (bool)isPlayingProp.GetValue(cutsceneHandler);
                            if (isPlaying) return true;
                        }
                    }
                }

                // Verificar IntroHandler
                var introHandlerType = AccessTools.TypeByName("IntroHandler");
                if (introHandlerType != null)
                {
                    var introHandler = UnityEngine.Object.FindObjectOfType(introHandlerType);
                    if (introHandler != null)
                    {
                        var showingField = AccessTools.Field(introHandlerType, "showing");
                        if (showingField != null)
                        {
                            bool showing = (bool)showingField.GetValue(introHandler);
                            if (showing) return true;
                        }
                    }
                }

                return false;
            }
            catch
            {
                return true; // Ser conservadores
            }
        }
    }
}
