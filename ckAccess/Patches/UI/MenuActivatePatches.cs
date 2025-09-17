extern alias PugOther;

using HarmonyLib;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace ckAccess.Patches.UI
{
    public static class MenuActivatePatches
    {
        private static IEnumerator ForceSafeSelectionCoroutine(PugOther.RadicalMenu menu)
        {
            yield return new WaitForEndOfFrame();
            PugOther.Manager.input.SetActiveInputField(null);

            if (menu.selectedIndex != -1 && menu.GetSelectedMenuOption()?.IsSelectionEnabled() == true)
            {
                yield break;
            }

            var safeOption = menu.menuOptions.FirstOrDefault(opt => !(opt is PugOther.RadicalMenuOptionTextInput) && opt.IsSelectionEnabled());
            if (safeOption != null)
            {
                int index = menu.GetIndexForOption(safeOption);
                if (index != -1) menu.SelectOptionIndex(index);
            }
            else
            {
                var firstEnabled = menu.menuOptions.FirstOrDefault(opt => opt.IsSelectionEnabled());
                if (firstEnabled != null)
                {
                    int index = menu.GetIndexForOption(firstEnabled);
                    if (index != -1) menu.SelectOptionIndex(index);
                }
            }
        }

        [HarmonyPatch(typeof(PugOther.RadicalJoinGameMenu), "Activate")]
        public static class RadicalJoinGameMenuActivatePatch
        {
            [HarmonyPostfix]
            public static void Postfix(PugOther.RadicalJoinGameMenu __instance)
            {
                Plugin.Instance.StartCoroutine(ForceSafeSelectionCoroutine(__instance));
            }
        }

        [HarmonyPatch(typeof(PugOther.WorldSettingsMenu), "Activate")]
        public static class WorldSettingsMenuActivatePatch
        {
            [HarmonyPostfix]
            public static void Postfix(PugOther.WorldSettingsMenu __instance)
            {
                Plugin.Instance.StartCoroutine(ForceSafeSelectionCoroutine(__instance));
            }
        }

        [HarmonyPatch(typeof(PugOther.CharacterCustomizationMenu), "Activate")]
        public static class CharacterCustomizationMenuActivatePatch
        {
            [HarmonyPostfix]
            public static void Postfix(PugOther.CharacterCustomizationMenu __instance)
            {
                Plugin.Instance.StartCoroutine(ForceSafeSelectionCoroutine(__instance));
            }
        }

        [HarmonyPatch(typeof(PugOther.CharacterTypeSelectionMenu), "Activate")]
        public static class CharacterTypeSelectionMenuActivatePatch
        {
            [HarmonyPostfix]
            public static void Postfix(PugOther.CharacterTypeSelectionMenu __instance)
            {
                Plugin.Instance.StartCoroutine(ForceSafeSelectionCoroutine(__instance));
            }
        }

        [HarmonyPatch(typeof(PugOther.ChooseCharacterMenu), "Activate")]
        public static class ChooseCharacterMenuActivatePatch
        {
            [HarmonyPostfix]
            public static void Postfix(PugOther.ChooseCharacterMenu __instance)
            {
                Plugin.Instance.StartCoroutine(ForceSafeSelectionCoroutine(__instance));
            }
        }

        [HarmonyPatch(typeof(PugOther.SelectWorldMenu), "Activate")]
        public static class SelectWorldMenuActivatePatch
        {
            [HarmonyPostfix]
            public static void Postfix(PugOther.SelectWorldMenu __instance)
            {
                Plugin.Instance.StartCoroutine(ForceSafeSelectionCoroutine(__instance));
            }
        }
    }
}