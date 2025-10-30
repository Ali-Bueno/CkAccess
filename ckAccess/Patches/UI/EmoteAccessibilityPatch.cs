extern alias I2Loc;

using HarmonyLib;
using System;
using UnityEngine;

namespace ckAccess.Patches.UI
{
    /// <summary>
    /// Patches Emote system to announce player messages like:
    /// - "Need higher mining skill"
    /// - "I need a drill for this"
    /// - "Object is indestructible"
    /// - Tutorial messages
    /// - And many more contextual messages
    /// </summary>
    [HarmonyPatch]
    public static class EmoteAccessibilityPatch
    {
        private static string lastAnnouncedEmote = "";
        private static float lastAnnounceTime = 0f;
        private const float DEBOUNCE_TIME = 0.5f; // Same as the game's emote cooldown

        /// <summary>
        /// Specify the method to patch using TargetMethod
        /// </summary>
        static System.Reflection.MethodBase TargetMethod()
        {
            var emoteType = AccessTools.TypeByName("Emote");
            if (emoteType == null)
            {
                UnityEngine.Debug.LogError("Could not find Emote type");
                return null;
            }

            var method = AccessTools.Method(emoteType, "OnOccupied");
            if (method == null)
            {
                UnityEngine.Debug.LogError("Could not find Emote.OnOccupied method");
            }

            return method;
        }

        /// <summary>
        /// Patch OnOccupied to announce the emote text after it's been generated
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(dynamic __instance)
        {
            try
            {
                // Skip if the game is hiding UI
                // Note: We can't easily access Manager.prefs.hideInGameUI with dynamic,
                // so we skip this check. Emotes are only spawned when UI is shown anyway.

                // Alternative: check if instance is null as a basic validation
                if (__instance == null)
                    return;

                // Get the text component (PugText)
                dynamic pugText = __instance.text;
                if (pugText == null)
                    return;

                // Get the rendered/processed text (with localization applied)
                string emoteText = pugText.ProcessText();

                if (string.IsNullOrEmpty(emoteText))
                    return;

                // Skip system placeholders (missing emotes show as "<missing X in Emote switch case>")
                if (emoteText.StartsWith("<missing"))
                    return;

                // Skip exclamation marks (these are just icons, not meaningful messages)
                if (emoteText == "!")
                    return;

                // Debounce: prevent announcing the same emote multiple times rapidly
                float currentTime = Time.unscaledTime;
                if (emoteText == lastAnnouncedEmote &&
                    (currentTime - lastAnnounceTime) < DEBOUNCE_TIME)
                {
                    return;
                }

                lastAnnouncedEmote = emoteText;
                lastAnnounceTime = currentTime;

                // Check if this is a tutorial message and add to buffer
                bool isTutorial = IsTutorialEmote(__instance);
                if (isTutorial)
                {
                    Tutorials.TutorialBuffer.AddTutorial(emoteText);
                }

                // Announce the emote message
                UIManager.Speak(emoteText);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error in Emote accessibility patch: {ex}");
            }
        }

        /// <summary>
        /// Check if this emote is a tutorial message
        /// </summary>
        private static bool IsTutorialEmote(dynamic emoteInstance)
        {
            try
            {
                // Get the emoteTypeInput field
                var emoteType = emoteInstance.emoteTypeInput;

                // Convert to int to check the enum value
                int emoteTypeValue = (int)emoteType;

                // Tutorial emote types are:
                // TutorialBreakWood = 51
                // TutorialCraftTorch = 52
                // TutorialCraftMiningPick = 53
                // TutorialCraftWorkbench = 54
                // TutorialSmeltOre = 55
                // NoMinionToCommand = 56

                return emoteTypeValue >= 51 && emoteTypeValue <= 55;
            }
            catch
            {
                return false;
            }
        }
    }
}
