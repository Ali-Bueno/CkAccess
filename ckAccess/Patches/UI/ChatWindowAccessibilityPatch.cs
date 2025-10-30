extern alias PugOther;
extern alias I2Loc;

using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace ckAccess.Patches.UI
{
    /// <summary>
    /// Patches ChatWindow to announce game messages (items picked up, level ups, etc.)
    /// These messages are different from the NotificationSystem - they come from the game itself.
    /// </summary>
    [HarmonyPatch]
    public static class ChatWindowAccessibilityPatch
    {
        private static string lastAnnouncedMessage = "";
        private static float lastAnnounceTime = 0f;
        private const float DEBOUNCE_TIME = 0.3f; // Prevent duplicate announcements

        /// <summary>
        /// Specify the method to patch using TargetMethod
        /// </summary>
        static MethodBase TargetMethod()
        {
            // Get the ChatWindow type from the PugOther assembly
            var chatWindowType = AccessTools.TypeByName("ChatWindow");
            if (chatWindowType == null)
            {
                UnityEngine.Debug.LogError("Could not find ChatWindow type");
                return null;
            }

            // Find the AddInfoText method with the specific signature
            var method = AccessTools.Method(chatWindowType, "AddInfoText", new Type[] {
                typeof(string[]),
                typeof(Rarity),
                typeof(PugOther.ChatWindow.MessageTextType),
                typeof(bool)
            });

            if (method == null)
            {
                UnityEngine.Debug.LogError("Could not find ChatWindow.AddInfoText method");
            }

            return method;
        }

        /// <summary>
        /// Use a coroutine to read the message after it's been created and rendered
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(
            string[] formatFields,
            PugOther.ChatWindow.MessageTextType messageTextType)
        {
            try
            {
                // Use a small delay to ensure the message has been processed
                Plugin.Instance.StartCoroutine(AnnounceMessageDelayed(formatFields, messageTextType));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error in ChatWindow accessibility patch: {ex}");
            }
        }

        private static System.Collections.IEnumerator AnnounceMessageDelayed(string[] formatFields, PugOther.ChatWindow.MessageTextType messageTextType)
        {
            // Wait a frame for the message to be created
            yield return null;

            try
            {
                // Get the localization key for this message type
                string locKey = GetLocalizationKeyForMessageType(messageTextType);

                if (!string.IsNullOrEmpty(locKey))
                {
                    // Get the localized text
                    string messageText = I2Loc::I2.Loc.LocalizationManager.GetTranslation(locKey);

                    // Apply format fields if provided
                    if (formatFields != null && formatFields.Length > 0)
                    {
                        messageText = string.Format(messageText, (object[])formatFields);
                    }

                    // Debounce: prevent duplicate announcements
                    float currentTime = Time.unscaledTime;
                    if (messageText == lastAnnouncedMessage &&
                        (currentTime - lastAnnounceTime) < DEBOUNCE_TIME)
                    {
                        yield break;
                    }

                    lastAnnouncedMessage = messageText;
                    lastAnnounceTime = currentTime;

                    // Announce the message
                    UIManager.Speak(messageText);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error announcing chat message: {ex}");
            }
        }

        private static string GetLocalizationKeyForMessageType(PugOther.ChatWindow.MessageTextType messageTextType)
        {
            // Map message types to their localization keys
            // These keys are used by the game to look up translations
            switch (messageTextType)
            {
                case PugOther.ChatWindow.MessageTextType.NewItem:
                    return "NewItemGained";
                case PugOther.ChatWindow.MessageTextType.CaughtItem:
                    return "CaughtItem";
                case PugOther.ChatWindow.MessageTextType.NewTalentPointAvailable:
                    return "NewTalentPointAvailable";
                case PugOther.ChatWindow.MessageTextType.DurabilityLost:
                    return "DurabilityLost";
                case PugOther.ChatWindow.MessageTextType.AdditionalItemGained:
                    return "AdditionalItemGained";
                case PugOther.ChatWindow.MessageTextType.GainedItem:
                    return "GainedItem";
                case PugOther.ChatWindow.MessageTextType.HardcoreDeath:
                    return "HardcoreDeath";
                case PugOther.ChatWindow.MessageTextType.ReceivedItems:
                    return "ReceivedItems";
                case PugOther.ChatWindow.MessageTextType.DiedFromStarvation:
                    return "DiedFromStarvation";
                case PugOther.ChatWindow.MessageTextType.PetLeveledUp:
                    return "PetLeveledUp";
                case PugOther.ChatWindow.MessageTextType.GainedSoul:
                    return "GainedSoul";
                case PugOther.ChatWindow.MessageTextType.StartingClassicWorld:
                    return "StartingClassicWorld";
                case PugOther.ChatWindow.MessageTextType.ReconnectAttempt:
                    return "ReconnectAttempt";
                case PugOther.ChatWindow.MessageTextType.ReconnectSuccess:
                    return "ReconnectSuccess";
                case PugOther.ChatWindow.MessageTextType.EnemiesScaledUp:
                    return "EnemiesScaledUp";
                case PugOther.ChatWindow.MessageTextType.EnemiesScaledDown:
                    return "EnemiesScaledDown";
                default:
                    return "";
            }
        }
    }
}
