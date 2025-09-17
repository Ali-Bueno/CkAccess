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
        [HarmonyPatch(typeof(PugOther.WorldSettingsMenu), "ActivateMenuIndex")]
        [HarmonyPostfix]
        public static void Postfix_ActivateMenuIndex(PugOther.WorldSettingsMenu __instance)
        {
            Plugin.Instance.StartCoroutine(MenuActivatePatches.ForceSafeSelectionCoroutine(__instance));
        }
    }
}
