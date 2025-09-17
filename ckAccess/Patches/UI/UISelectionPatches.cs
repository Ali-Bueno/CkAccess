extern alias PugOther;
using HarmonyLib;
using System.Text;

namespace ckAccess.Patches.UI
{
    [HarmonyPatch]
    public static class UISelectionPatches
    {
        private static void AnnounceTabInfo(PugOther.CharacterWindowTab tab)
        {
            if (tab == null) return;
            var textAndFormat = tab.GetHoverTitle();
            if (textAndFormat != null && !string.IsNullOrEmpty(textAndFormat.text))
            {
                UIManager.Speak(textAndFormat.text);
            }
        }

        private static void AnnounceSkillInfo(PugOther.SkillUIElement skill)
        {
            if (skill == null) return;
            
            var sb = new StringBuilder();
            var title = skill.GetHoverTitle();
            if (title != null && !string.IsNullOrEmpty(title.text))
            {
                sb.Append(PugOther.PugText.ProcessText(title.text, title.formatFields, true, false));
            }

            var description = skill.GetHoverDescription();
            if (description != null)
            {
                foreach (var line in description)
                {
                    sb.Append(", ").Append(PugOther.PugText.ProcessText(line.text, line.formatFields, true, false));
                }
            }

            var stats = skill.GetHoverStats(false);
            if (stats != null)
            {
                foreach (var line in stats)
                {
                    sb.Append(", ").Append(PugOther.PugText.ProcessText(line.text, line.formatFields, true, false));
                }
            }

            UIManager.Speak(sb.ToString());
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PugOther.UIelement), "OnSelected")]
        public static void OnSelected_Postfix(PugOther.UIelement __instance)
        {
            if (__instance is PugOther.CharacterWindowTab tab)
            {
                AnnounceTabInfo(tab);
            }
            else if (__instance is PugOther.SkillUIElement skill)
            {
                AnnounceSkillInfo(skill);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PugOther.CharacterWindowTab), "OnLeftClicked")]
        public static void OnLeftClicked_Postfix(PugOther.CharacterWindowTab __instance)
        {
            AnnounceTabInfo(__instance);
        }
    }
}
