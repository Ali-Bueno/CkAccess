extern alias PugOther;

using HarmonyLib;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace ckAccess.Patches.UI
{
    [HarmonyPatch]
    public static class WorldSettingsMenuPatches
    {
        // This coroutine handles the delayed input field deselection and option selection.
        private static IEnumerator DelayedFocusReset(PugOther.WorldSettingsMenu menu)
        {
            yield return new WaitForEndOfFrame();

            // Ensure no text input field is active.
            PugOther.Manager.input.SetActiveInputField(null);

            // Force select the first valid option in the newly active sub-menu.
            var activeSubMenuOptions = menu.GetAllCurrentlyActiveMenuOptions();
            var firstSelectableOption = activeSubMenuOptions.FirstOrDefault(opt => opt.IsSelectionEnabled());

            if (firstSelectableOption != null)
            {
                int index = menu.GetIndexForOption(firstSelectableOption);
                if (index != -1)
                {
                    menu.SelectOptionIndex(index);
                }
            }
        }

        // This patch fixes the input lock when changing tabs in the World Settings menu.
        // It ensures that after a tab is activated, the input field is deselected
        // and a valid menu option is selected, preventing focus issues.
        [HarmonyPatch(typeof(PugOther.WorldSettingsMenu), "ActivateMenuIndex")]
        [HarmonyPostfix]
        public static void Postfix_ActivateMenuIndex(PugOther.WorldSettingsMenu __instance)
        {
            // Start the coroutine to handle the delayed focus reset.
            Plugin.Instance.StartCoroutine(DelayedFocusReset(__instance));
        }
    }
}
