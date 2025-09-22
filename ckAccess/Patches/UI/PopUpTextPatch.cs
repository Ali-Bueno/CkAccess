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
            return LocalizationManager.GetText("hold_to_confirm");
        }
    }
}