extern alias PugOther;

using HarmonyLib;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace ckAccess.Patches.UI
{
    public static class MenuActivatePatches
    {
        // --- Coroutines ---

        private static IEnumerator ForceSelectionCoroutine(PugOther.RadicalMenu menu)
        {
            yield return new WaitForEndOfFrame();
            menu.DeselectAnyCurrentOption(playEffect: false);
            if (menu.menuOptions.Count > 0)
            {
                menu.SelectOptionIndex(0);
            }
        }

        private static IEnumerator UndoAndForceSelectionCoroutine(PugOther.CharacterCustomizationMenu menu)
        {
            yield return new WaitForEndOfFrame();

            // Directly counteract the game's action by deactivating the input field.
            // This is the key to unlocking navigation.
            PugOther.Manager.input.SetActiveInputField(null);

            // Now that input is unlocked, find and select a safe option to navigate from.
            var safeOption = menu.menuOptions.FirstOrDefault(opt => opt != menu.nameInput && opt.IsSelectionEnabled());
            
            if (safeOption != null)
            {
                int index = menu.GetIndexForOption(safeOption);
                if (index != -1)
                {
                    menu.SelectOptionIndex(index);
                }
            }
            else if (menu.menuOptions.Count > 0)
            {
                menu.SelectOptionIndex(0); // Fallback
            }
        }

        private static IEnumerator UndoAndForceSelectionRadicalMenuCoroutine(PugOther.RadicalMenu menu)
        {
            yield return new WaitForEndOfFrame();
            PugOther.Manager.input.SetActiveInputField(null);
            menu.DeselectAnyCurrentOption(false);

            var safeOption = menu.menuOptions.FirstOrDefault(opt => !(opt is PugOther.RadicalMenuOptionTextInput) && opt.IsSelectionEnabled());

            if (safeOption != null)
            {
                int index = menu.GetIndexForOption(safeOption);
                if (index != -1)
                {
                    menu.SelectOptionIndex(index);
                }
            }
            else if (menu.menuOptions.Count > 0)
            {
                menu.SelectOptionIndex(0); // Fallback
            }
        }

        // --- Patches ---

        [HarmonyPatch(typeof(PugOther.WorldSettingsMenu), "Activate")]
        public static class WorldSettingsMenuActivatePatch
        {
            [HarmonyPostfix]
            public static void Postfix(PugOther.WorldSettingsMenu __instance)
            {
                if (!PugOther.Manager.input.SystemIsUsingMouse())
                {
                    Plugin.Instance.StartCoroutine(ForceSelectionCoroutine(__instance));
                }
            }
        }

        [HarmonyPatch(typeof(PugOther.CharacterCustomizationMenu), "Activate")]
        public static class CharacterCustomizationMenuActivatePatch
        {
            [HarmonyPostfix]
            public static void Postfix(PugOther.CharacterCustomizationMenu __instance)
            {
                if (!PugOther.Manager.input.SystemIsUsingMouse())
                {
                    Plugin.Instance.StartCoroutine(UndoAndForceSelectionCoroutine(__instance));
                }
            }
        }

        [HarmonyPatch(typeof(PugOther.CharacterTypeSelectionMenu), "Activate")]
        public static class CharacterTypeSelectionMenuActivatePatch
        {
            [HarmonyPostfix]
            public static void Postfix(PugOther.CharacterTypeSelectionMenu __instance)
            {
                if (!PugOther.Manager.input.SystemIsUsingMouse())
                {
                    Plugin.Instance.StartCoroutine(ForceSelectionCoroutine(__instance));
                }
            }
        }

        [HarmonyPatch(typeof(PugOther.ChooseCharacterMenu), "Activate")]
        public static class ChooseCharacterMenuActivatePatch
        {
            [HarmonyPostfix]
            public static void Postfix(PugOther.ChooseCharacterMenu __instance)
            {
                if (!PugOther.Manager.input.SystemIsUsingMouse())
                {
                    Plugin.Instance.StartCoroutine(ForceSelectionCoroutine(__instance));
                }
            }
        }

        [HarmonyPatch(typeof(PugOther.SelectWorldMenu), "Activate")]
        public static class SelectWorldMenuActivatePatch
        {
            [HarmonyPostfix]
            public static void Postfix(PugOther.SelectWorldMenu __instance)
            {
                if (!PugOther.Manager.input.SystemIsUsingMouse())
                {
                    Plugin.Instance.StartCoroutine(ForceSelectionCoroutine(__instance));
                }
            }
        }

        [HarmonyPatch(typeof(PugOther.RadicalJoinGameMenu), "Activate")]
        public static class RadicalJoinGameMenuActivatePatch
        {
            [HarmonyPostfix]
            public static void Postfix(PugOther.RadicalJoinGameMenu __instance)
            {
                if (!PugOther.Manager.input.SystemIsUsingMouse())
                {
                    Plugin.Instance.StartCoroutine(UndoAndForceSelectionRadicalMenuCoroutine(__instance));
                }
            }
        }
    }
}