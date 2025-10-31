extern alias PugOther;
extern alias I2Loc;

using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ckAccess.Localization;

namespace ckAccess.Patches.UI
{
    /// <summary>
    /// VERSIÓN SEGURA: Solo anuncia popups cuando NO hay cutscenes activas
    /// </summary>
    [HarmonyPatch(typeof(PugOther.PopUpText), "StartNewDisplaySequence")]
    public static class PopUpTextPatch
    {
        public static void Postfix(PugOther.PopUpText __instance, bool holdToConfirm, List<string> options, float accidentalInputBlockDuration)
        {
            if (__instance == null) return;

            // NO anunciar popups muy cortos (ej. icono de guardado)
            if (accidentalInputBlockDuration <= 0.1f) return;

            // CRÍTICO: NO anunciar durante cutscenes
            if (IsCutsceneActive()) return;

            string mainText = __instance.pugText.ProcessText();
            if (string.IsNullOrEmpty(mainText)) return;

            string textToSpeak = mainText;

            if (holdToConfirm)
            {
                string holdHint = GetHoldToConfirmHint();

                __instance.pugText.SetText(mainText + $"\n({holdHint})");
                __instance.pugText.Render();

                textToSpeak += $", {holdHint}";
            }

            // Determinar si es un mensaje de tutorial basado en el contenido
            bool isTutorialMessage = IsTutorialMessage(mainText);

            if (isTutorialMessage)
            {
                // Mensajes de tutorial van al buffer de tutoriales (teclas '/Ñ)
                Notifications.TutorialBufferSystem.AddTutorial(textToSpeak);
            }
            else
            {
                // Otros popups se anuncian directamente
                UIManager.Speak(textToSpeak);
            }

            if (options != null && options.Count > 0)
            {
                string optionsStr = string.Join(", ", options.Select(o => UIManager.GetLocalizedText(o)));
                Plugin.Instance.StartCoroutine(SpeakDelayed(optionsStr, 0.75f));
            }
        }

        /// <summary>
        /// Detecta si un popup es un mensaje de tutorial.
        /// Estrategia multiidioma: usa el call stack para detectar si viene de un TutorialSequence
        /// </summary>
        private static bool IsTutorialMessage(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            try
            {
                // Obtener la pila de llamadas para detectar el origen del popup
                var stackTrace = new System.Diagnostics.StackTrace();

                for (int i = 0; i < stackTrace.FrameCount; i++)
                {
                    var frame = stackTrace.GetFrame(i);
                    var method = frame?.GetMethod();

                    if (method != null)
                    {
                        string className = method.DeclaringType?.Name ?? "";

                        // Si viene de algún sistema de Tutorial, es un tutorial
                        // Esto funciona en todos los idiomas porque se basa en el código, no en el texto
                        if (className.Contains("Tutorial"))
                        {
                            UnityEngine.Debug.Log($"[PopUpTextPatch] Tutorial detectado por call stack: '{className}'");
                            return true;
                        }
                    }
                }

                // No viene de un tutorial, es un popup normal
                return false;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[PopUpTextPatch] Error detectando tutorial: {ex}");
                return false;
            }
        }

        private static bool IsCutsceneActive()
        {
            try
            {
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

        private static IEnumerator SpeakDelayed(string text, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            // Verificar NUEVAMENTE antes de hablar
            if (!IsCutsceneActive())
            {
                UIManager.Speak(text, false);
            }
        }

        private static string GetHoldToConfirmHint()
        {
            return LocalizationManager.GetText("hold_to_confirm");
        }
    }
}
