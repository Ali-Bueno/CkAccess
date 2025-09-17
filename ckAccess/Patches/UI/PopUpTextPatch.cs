extern alias PugOther;
extern alias I2Loc;

using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ckAccess.Patches.UI
{
    [HarmonyPatch(typeof(PugOther.PopUpText), "StartNewDisplaySequence")]
    public static class PopUpTextPatch
    {
        public static void Postfix(PugOther.PopUpText __instance, bool holdToConfirm, List<string> options, float accidentalInputBlockDuration)
        {
            if (__instance == null) return;

            // Block announcement for very short-lived popups (e.g. saving icon)
            if (accidentalInputBlockDuration <= 0.1f) return;

            string mainText = __instance.pugText.ProcessText();
            if (string.IsNullOrEmpty(mainText)) return;

            string textToSpeak = mainText;

            if (holdToConfirm)
            {
                string holdHint = GetHoldToConfirmHint();
                
                // Update visual text
                __instance.pugText.SetText(mainText + $"\n({holdHint})");
                __instance.pugText.Render();

                // Update text to be spoken
                textToSpeak += $", {holdHint}";
            }

            UIManager.Speak(textToSpeak);

            // Announce the options that will appear
            if (options != null && options.Count > 0)
            {
                string optionsStr = string.Join(", ", options.Select(o => UIManager.GetLocalizedText(o)));
                Plugin.Instance.StartCoroutine(SpeakDelayed(optionsStr, 0.75f));
            }
        }

        private static IEnumerator SpeakDelayed(string text, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            UIManager.Speak(text, false);
        }

        private static string GetHoldToConfirmHint()
        {
            string lang = PugOther.Manager.prefs.language;
            switch (lang)
            {
                case "es": return "mantén pulsado para confirmar";
                case "fr": return "maintenir pour confirmer";
                case "de": return "zum Bestätigen gedrückt halten";
                case "it": return "tieni premuto per confermare";
                case "pt": return "segure para confirmar";
                case "ru": return "удерживайте для подтверждения";
                case "zh-Hans": return "按住以确认";
                case "ja": return "長押しして確認";
                case "ko": return "길게 눌러 확인";
                case "en": 
                default: 
                    return "hold to confirm";
            }
        }
    }
}