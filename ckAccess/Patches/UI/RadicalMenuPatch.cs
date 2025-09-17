extern alias PugOther;
extern alias I2Loc;

using HarmonyLib;
using BepInEx.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ckAccess.Patches.UI
{
    [HarmonyPatch(typeof(PugOther.RadicalMenu))]
    public static class RadicalMenuPatch
    {
        private static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("RadicalMenuPatch");
        private static string lastAnnouncedText = "";
        private static float lastAnnounceTime = 0f;
        private const float debounceTime = 0.05f; // 50ms

        private static void DebouncedSpeak(string text)
        {
            if (text == lastAnnouncedText && Time.unscaledTime - lastAnnounceTime < debounceTime)
            {
                return;
            }
            
            UIManager.Speak(text);
            lastAnnouncedText = text;
            lastAnnounceTime = Time.unscaledTime;
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnSelectedOptionChanged")]
        public static void Postfix_OnSelectedOptionChanged(PugOther.RadicalMenu __instance)
        {
            AnnounceSelectedOption(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch("SkimLeft")]
        public static void Postfix_SkimLeft(PugOther.RadicalMenu __instance)
        {
            AnnounceSelectedOption(__instance, true);
        }

        [HarmonyPostfix]
        [HarmonyPatch("SkimRight")]
        public static void Postfix_SkimRight(PugOther.RadicalMenu __instance)
        {
            AnnounceSelectedOption(__instance, true);
        }

        public static void AnnounceSelectedOption(PugOther.RadicalMenu __instance, bool valueOnly = false)
        {
            var selectedOption = __instance.GetSelectedMenuOption();
            if (selectedOption == null) return;

            string textToSpeak = GetTextForOption(selectedOption, valueOnly);

            if (!string.IsNullOrEmpty(textToSpeak))
            {
                DebouncedSpeak(textToSpeak);
            }
        }

        private static string GetTextForOption(PugOther.RadicalMenuOption option, bool valueOnly)
        {
            // --- Specific Handlers ---
            if (option is PugOther.CharacterCustomizationOption_Selection customOption)
            {
                return HandleCharacterCustomizationOption(customOption, valueOnly);
            }
            if (option is PugOther.CharacterTypeOption_Selection characterTypeOption)
            {
                string type = UIManager.GetLocalizedText(characterTypeOption.typeText.GetText());
                string desc = UIManager.GetLocalizedText(characterTypeOption.typeDescText.GetText());
                return valueOnly ? type : $"{type}, {desc}";
            }
            if (option is PugOther.WorldSlotMoreOption)
            {
                return GetMoreOptionsTranslation();
            }
            if (option is PugOther.WorldSlotDeleteOption)
            {
                return UIManager.GetLocalizedText("delete");
            }
            if (option is PugOther.SaveSlotPlayOption saveSlotPlay)
            {
                if (saveSlotPlay.text.localize) // Slot is empty
                {
                    return UIManager.GetLocalizedText(saveSlotPlay.text.GetText());
                }
                else // Slot has a character
                {
                    string characterName = saveSlotPlay.characterName.ProcessText();
                    string characterType = saveSlotPlay.characterType.ProcessText();
                    return $"{characterName}, {characterType}";
                }
            }
            if (option is PugOther.SaveSlotDeleteOption)
            {
                return UIManager.GetLocalizedText("delete");
            }
            if (option is PugOther.RadicalJoinGameMenu_JoinMethodDropdown joinMethodDropdown)
            {
                return joinMethodDropdown.textResult.ProcessText();
            }

            // --- General Handler ---
            try
            {
                var pugTexts = option.GetComponentsInChildren<PugOther.PugText>(true);
                if (pugTexts == null || pugTexts.Length == 0) return "";

                var textParts = new List<string>();
                foreach (var pugText in pugTexts)
                {
                    string processedText = pugText.ProcessText();
                    if (!string.IsNullOrWhiteSpace(processedText))
                    {
                        textParts.Add(processedText.Trim());
                    }
                }
                return textParts.Any() ? string.Join(", ", textParts.Distinct()) : "";
            }
            catch (System.Exception e)
            {
                Logger.LogError($"Exception in GetTextForOption for {option.GetType().Name}: {e}");
                return "";
            }
        }

        private static string HandleCharacterCustomizationOption(PugOther.CharacterCustomizationOption_Selection option, bool valueOnly)
        {
            if (option.changesCharacterRole)
            {
                var sb = new StringBuilder();
                sb.Append(option.roleTitleText.ProcessText());
                sb.Append(", ").Append(option.roleTitleDesc.ProcessText());
                sb.Append(", ").Append(option.perksTitle.ProcessText());
                sb.Append(", ").Append(option.roleSkillDesc.ProcessText());
                foreach (var itemDesc in option.roleItemDescs)
                {
                    if (itemDesc.gameObject.activeSelf)
                    {
                        sb.Append(", ").Append(itemDesc.ProcessText());
                    }
                }
                return sb.ToString();
            }
            else
            {
                int currentIndex = option.customizationTable.GetIndexFromId(option.bodyPartType, option.playerController.GetActiveCustomizableBodypart(option.bodyPartType)) + 1;
                int maxVariations = option.customizationTable.GetMaxVariations(option.bodyPartType, 0);
                string valueText = $"Style {currentIndex} of {maxVariations}";

                if (valueOnly)
                {
                    return valueText;
                }
                else
                {
                    string labelText = UIManager.GetLocalizedText(option.labelText.GetText());
                    return $"{labelText}, {valueText}";
                }
            }
        }

        private static string GetMoreOptionsTranslation()
        {
            string lang = PugOther.Manager.prefs.language;
            switch (lang)
            {
                case "es": return "Más opciones";
                case "fr": return "Plus d'options";
                case "de": return "Weitere Optionen";
                case "it": return "Altre opzioni";
                case "pt": return "Mais opções";
                case "ru": return "Дополнительные параметры";
                case "zh-Hans": return "更多选项";
                case "ja": return "その他のオプション";
                case "ko": return "추가 옵션";
                case "en": default: return "More options";
            }
        }
    }

    [HarmonyPatch(typeof(PugOther.RadicalJoinGameMenu_JoinMethodDropdown))]
    public static class RadicalJoinGameMenu_JoinMethodDropdown_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch("NavigateInternally")]
        public static void Postfix_NavigateInternally(bool __result)
        {
            if (!__result) return;

            var currentSelected = PugOther.Manager.ui.currentSelectedUIElement;
            if (currentSelected == null) return;

            var pugText = currentSelected.GetComponentInChildren<PugOther.PugText>();
            if (pugText != null)
            {
                UIManager.Speak(pugText.ProcessText());
            }
        }
    }
}
