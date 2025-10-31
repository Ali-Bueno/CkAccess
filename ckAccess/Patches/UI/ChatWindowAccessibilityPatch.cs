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
    /// VERSIÓN SEGURA: Solo funciona cuando NO hay cutscenes activas
    /// </summary>
    [HarmonyPatch]
    public static class ChatWindowAccessibilityPatch
    {
        private static string lastAnnouncedMessage = "";
        private static float lastAnnounceTime = 0f;
        private const float DEBOUNCE_TIME = 0.3f;

        static MethodBase TargetMethod()
        {
            var chatWindowType = AccessTools.TypeByName("ChatWindow");
            if (chatWindowType == null)
            {
                UnityEngine.Debug.LogError("[ChatPatch] Could not find ChatWindow type");
                return null;
            }

            var method = AccessTools.Method(chatWindowType, "AddInfoText", new Type[] {
                typeof(string[]),
                typeof(Rarity),
                typeof(PugOther.ChatWindow.MessageTextType),
                typeof(bool)
            });

            if (method == null)
            {
                UnityEngine.Debug.LogError("[ChatPatch] Could not find ChatWindow.AddInfoText method");
            }

            return method;
        }

        [HarmonyPostfix]
        public static void Postfix(
            string[] formatFields,
            PugOther.ChatWindow.MessageTextType messageTextType)
        {
            try
            {
                // CRÍTICO: NO anunciar si hay cutscene activa
                if (IsCutsceneActive())
                {
                    return;
                }

                Plugin.Instance.StartCoroutine(AnnounceMessageDelayed(formatFields, messageTextType));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ChatPatch] Error: {ex}");
            }
        }

        /// <summary>
        /// Verifica si hay una cutscene activa (intro, spawn desde núcleo, etc.)
        /// </summary>
        private static bool IsCutsceneActive()
        {
            try
            {
                // 1. Verificar CutsceneHandler
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
                            if (isPlaying)
                            {
                                return true;
                            }
                        }
                    }
                }

                // 2. Verificar IntroHandler
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
                            if (showing)
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch
            {
                // En caso de error, ser conservadores y no anunciar
                return true;
            }
        }

        private static System.Collections.IEnumerator AnnounceMessageDelayed(string[] formatFields, PugOther.ChatWindow.MessageTextType messageTextType)
        {
            yield return null;

            try
            {
                // Verificar NUEVAMENTE antes de anunciar (por si la cutscene empezó mientras esperábamos)
                if (IsCutsceneActive())
                {
                    yield break;
                }

                string locKey = GetLocalizationKeyForMessageType(messageTextType);

                if (!string.IsNullOrEmpty(locKey))
                {
                    string messageText = I2Loc::I2.Loc.LocalizationManager.GetTranslation(locKey);

                    if (formatFields != null && formatFields.Length > 0)
                    {
                        messageText = string.Format(messageText, (object[])formatFields);
                    }

                    float currentTime = Time.unscaledTime;
                    if (messageText == lastAnnouncedMessage &&
                        (currentTime - lastAnnounceTime) < DEBOUNCE_TIME)
                    {
                        yield break;
                    }

                    lastAnnouncedMessage = messageText;
                    lastAnnounceTime = currentTime;

                    UIManager.Speak(messageText);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ChatPatch] Error announcing: {ex}");
            }
        }

        private static string GetLocalizationKeyForMessageType(PugOther.ChatWindow.MessageTextType messageTextType)
        {
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
