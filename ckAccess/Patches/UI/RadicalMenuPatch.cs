extern alias PugOther;
extern alias I2Loc;

using HarmonyLib;
using BepInEx.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ckAccess.Patches.UI
{
    [HarmonyPatch(typeof(PugOther.RadicalMenu))]
    public static class RadicalMenuPatch
    {
        private static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("RadicalMenuPatch");

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

            // --- Specific Handlers ---

            if (selectedOption is PugOther.CharacterCustomizationOption_Selection customOption)
            {
                HandleCharacterCustomizationOption(customOption, valueOnly);
                return;
            }
            if (selectedOption is PugOther.CharacterTypeOption_Selection characterTypeOption)
            {
                string type = UIManager.GetLocalizedText(characterTypeOption.typeText.GetText());
                string desc = UIManager.GetLocalizedText(characterTypeOption.typeDescText.GetText());
                UIManager.Speak(valueOnly ? type : $"{type}, {desc}");
                return;
            }
            if (selectedOption is PugOther.WorldSlotMoreOption)
            {
                UIManager.Speak(GetMoreOptionsTranslation());
                return;
            }
            if (selectedOption is PugOther.WorldSlotDeleteOption)
            {
                UIManager.Speak(UIManager.GetLocalizedText("delete"));
                return;
            }

            if (selectedOption is PugOther.SaveSlotPlayOption saveSlotPlay)
            {
                if (saveSlotPlay.text.localize) // Slot is empty
                {
                    UIManager.Speak(UIManager.GetLocalizedText(saveSlotPlay.text.GetText()));
                }
                else // Slot has a character
                {
                    string characterName = saveSlotPlay.characterName.ProcessText();
                    string characterType = saveSlotPlay.characterType.ProcessText();
                    UIManager.Speak($"{characterName}, {characterType}");
                }
                return;
            }
            if (selectedOption is PugOther.SaveSlotDeleteOption)
            {
                UIManager.Speak(UIManager.GetLocalizedText("delete"));
                return;
            }

            if (selectedOption is PugOther.RadicalJoinGameMenu_JoinMethodDropdown joinMethodDropdown)
            {
                // Announce the current value of the dropdown.
                // The announcement of the options *inside* the dropdown is handled in the patch below.
                string label = joinMethodDropdown.textResult.ProcessText();
                UIManager.Speak(label);
                return;
            }

            // --- General Handler ---
            try
            {
                var pugTexts = selectedOption.GetComponentsInChildren<PugOther.PugText>(true);
                if (pugTexts == null || pugTexts.Length == 0) return;

                var textParts = new List<string>();
                foreach (var pugText in pugTexts)
                {
                    // Use ProcessText to get the final, formatted string.
                    string processedText = pugText.ProcessText(); 
                    if (!string.IsNullOrWhiteSpace(processedText))
                    {
                        textParts.Add(processedText.Trim());
                    }
                }

                if (textParts.Any())
                {
                    string textToSpeak = string.Join(", ", textParts.Distinct());
                    UIManager.Speak(textToSpeak);
                }
            }
            catch (System.Exception e)
            {
                Logger.LogError($"Exception in AnnounceSelectedOption for {selectedOption.GetType().Name}: {e}");
            }
        }

        private static void HandleCharacterCustomizationOption(PugOther.CharacterCustomizationOption_Selection option, bool valueOnly)
        {
            if (option.changesCharacterRole)
            {
                // For roles, read all available text fields using ProcessText to get formatted strings.
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
                UIManager.Speak(sb.ToString());
            }
            else
            {
                // For appearance, we build the text manually.
                int currentIndex = option.customizationTable.GetIndexFromId(option.bodyPartType, option.playerController.GetActiveCustomizableBodypart(option.bodyPartType)) + 1;
                int maxVariations = option.customizationTable.GetMaxVariations(option.bodyPartType, 0);
                string valueText = $"Style {currentIndex} of {maxVariations}";

                if (valueOnly)
                {
                    UIManager.Speak(valueText);
                }
                else
                {
                    string labelText = UIManager.GetLocalizedText(option.labelText.GetText());
                    UIManager.Speak($"{labelText}, {valueText}");
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
