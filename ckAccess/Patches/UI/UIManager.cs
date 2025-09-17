extern alias PugOther;
extern alias I2Loc;

using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using PugText = PugOther.PugText;
using Manager = PugOther.Manager;

namespace ckAccess.Patches.UI
{
    public static class UIManager
    {
        public static void Speak(string text, bool interrupt = true)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (interrupt)
            {
                DavyKager.Tolk.Output(text);
            }
            else
            {
                DavyKager.Tolk.Speak(text);
            }
        }

        public static string GetLocalizedText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            return PugText.ProcessText(text, null, true, false);
        }

        public static void Reset()
        {
            // Announce in the new language to confirm the change
            Speak(GetLocalizedText("Menu/Language"));
        }

        public static void AnnounceDelayed(Action announcementAction)
        {
            Plugin.Instance.StartCoroutine(AnnounceChangeCoroutine(announcementAction));
        }

        private static IEnumerator AnnounceChangeCoroutine(Action announcementAction)
        {
            yield return null; // Wait for one frame for the value to update
            announcementAction?.Invoke();
        }
    }
}