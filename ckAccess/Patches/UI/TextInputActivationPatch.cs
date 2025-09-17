
extern alias PugOther;

using HarmonyLib;

namespace ckAccess.Patches.UI
{
    // Patch RadicalMenuOptionTextInput.OnActivated to only activate input field when explicitly told.
    [HarmonyPatch(typeof(PugOther.RadicalMenuOptionTextInput), "OnActivated")]
    public static class RadicalMenuOptionTextInput_OnActivated_Patch
    {
        public static bool _shouldActivateInputField = false;

        [HarmonyPrefix]
        public static bool Prefix(PugOther.RadicalMenuOptionTextInput __instance)
        {
            // Only allow the original OnActivated to run if our flag is set.
            // This prevents automatic activation of the input field.
            return _shouldActivateInputField;
        }
    }

    // Patch RadicalMenu.ActivateSelectedIndex to set the flag before activating a text input option.
    [HarmonyPatch(typeof(PugOther.RadicalMenu), "ActivateSelectedIndex")]
    public static class RadicalMenu_ActivateSelectedIndex_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(PugOther.RadicalMenu __instance)
        {
            var selectedOption = __instance.GetSelectedMenuOption();
            if (selectedOption is PugOther.RadicalMenuOptionTextInput)
            {
                // If the selected option is a text input, set the flag to allow its OnActivated to run.
                RadicalMenuOptionTextInput_OnActivated_Patch._shouldActivateInputField = true;
            }
        }

        [HarmonyPostfix]
        public static void Postfix(PugOther.RadicalMenu __instance)
        {
            // Always reset the flag after the activation attempt.
            RadicalMenuOptionTextInput_OnActivated_Patch._shouldActivateInputField = false;
        }
    }
}
