extern alias PugOther;
using HarmonyLib;

namespace ckAccess.Patches.UI
{
    /// <summary>
    /// Parche específico y limpio para accesibilidad de skills
    /// </summary>
    [HarmonyPatch(typeof(PugOther.ButtonUIElement))]
    public static class SkillAccessibilityPatch
    {
        private static string lastAnnouncedSkill = "";

        /// <summary>
        /// Intercepta la selección de botones y maneja skills específicamente
        /// </summary>
        [HarmonyPatch("OnSelected")]
        [HarmonyPostfix]
        public static void OnSelected_Postfix(PugOther.ButtonUIElement __instance)
        {
            try
            {
                if (__instance == null) return;

                // Solo procesar si estamos en contexto de inventario/personaje
                if (!IsInInventoryContext(__instance)) return;

                // Verificar si este botón es una skill
                var skillComponent = __instance.GetComponent<PugOther.SkillUIElement>();
                if (skillComponent != null)
                {
                    AnnounceSkill(skillComponent);
                }
                // IMPORTANTE: Si es skill, NO hacer nada más (no anunciar como botón)
            }
            catch
            {
                // Error silencioso
            }
        }

        /// <summary>
        /// Limpia el cache cuando se deselecciona
        /// </summary>
        [HarmonyPatch("OnDeselected")]
        [HarmonyPostfix]
        public static void OnDeselected_Postfix()
        {
            lastAnnouncedSkill = "";
        }

        /// <summary>
        /// Anuncia información completa de la skill
        /// </summary>
        private static void AnnounceSkill(PugOther.SkillUIElement skill)
        {
            try
            {
                var hoverTitle = skill.GetHoverTitle();
                if (hoverTitle != null && !string.IsNullOrEmpty(hoverTitle.text))
                {
                    string skillText = ProcessSkillText(hoverTitle.text, hoverTitle.formatFields);

                    if (!string.IsNullOrEmpty(skillText))
                    {
                        string skillId = $"skill_{skill.skillID}_{skill.GetInstanceID()}";
                        if (skillId != lastAnnouncedSkill)
                        {
                            UIManager.Speak(skillText);
                            lastAnnouncedSkill = skillId;
                        }
                    }
                }
            }
            catch
            {
                // Error silencioso
            }
        }

        /// <summary>
        /// Procesa el texto de la skill aplicando formatFields correctamente
        /// </summary>
        private static string ProcessSkillText(string text, object[] formatFields)
        {
            if (string.IsNullOrEmpty(text)) return "";

            try
            {
                // Si hay formatFields, aplicarlos primero
                if (formatFields != null && formatFields.Length > 0)
                {
                    string[] stringFields = new string[formatFields.Length];
                    for (int i = 0; i < formatFields.Length; i++)
                    {
                        stringFields[i] = formatFields[i]?.ToString() ?? "";
                    }

                    string processedText = PugOther.PugText.ProcessText(text, stringFields, true, false);
                    if (!string.IsNullOrEmpty(processedText))
                    {
                        return processedText;
                    }
                }

                // Fallback a localización
                string localizedText = UIManager.GetLocalizedText(text);
                if (!string.IsNullOrEmpty(localizedText))
                {
                    return localizedText;
                }

                // Último fallback
                return text;
            }
            catch
            {
                return text;
            }
        }

        /// <summary>
        /// Verifica si estamos en contexto de inventario/personaje
        /// </summary>
        private static bool IsInInventoryContext(PugOther.UIelement element)
        {
            try
            {
                // Verificar si está en ventana de personaje
                var characterWindow = element.GetComponentInParent<PugOther.CharacterWindowUI>();
                if (characterWindow != null) return true;

                // Verificar si está en algún inventario
                var inventoryUI = element.GetComponentInParent<PugOther.InventoryUI>();
                if (inventoryUI != null) return true;

                // Verificar si el UIManager tiene inventario abierto
                var uiManager = PugOther.Manager.ui;
                return uiManager != null && uiManager.isAnyInventoryShowing;
            }
            catch
            {
                return false;
            }
        }
    }
}