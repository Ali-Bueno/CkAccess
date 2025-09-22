extern alias PugOther;
using HarmonyLib;

namespace ckAccess.Patches.UI
{
    /// <summary>
    /// Parche para accesibilizar los árboles de talento de habilidades
    /// </summary>
    [HarmonyPatch(typeof(PugOther.SkillTalentTreeUI))]
    public static class SkillTalentTreePatch
    {
        /// <summary>
        /// Detectar cuando se abre un árbol de talentos
        /// </summary>
        [HarmonyPatch("ShowTalentTree")]
        [HarmonyPostfix]
        public static void ShowTalentTree_Postfix(PugOther.SkillTalentTreeUI __instance, SkillID skillToShow)
        {
            try
            {
                // Obtener el nombre de la habilidad
                string skillName = GetSkillName(skillToShow);
                UIManager.Speak($"Árbol de talentos abierto: {skillName}");
            }
            catch
            {
                UIManager.Speak("Árbol de talentos abierto");
            }
        }

        /// <summary>
        /// Detectar cuando se cierra un árbol de talentos
        /// </summary>
        [HarmonyPatch("HideTalentTree")]
        [HarmonyPostfix]
        public static void HideTalentTree_Postfix(PugOther.SkillTalentTreeUI __instance)
        {
            try
            {
                UIManager.Speak("Árbol de talentos cerrado.");
            }
            catch
            {
                // Error silencioso
            }
        }

        /// <summary>
        /// Obtiene el nombre localizado de una habilidad
        /// </summary>
        private static string GetSkillName(SkillID skillToShow)
        {
            try
            {
                // Intentar obtener el nombre desde el sistema de localización
                string skillKey = $"skill_{skillToShow}";
                string localizedName = UIManager.GetLocalizedText(skillKey);

                if (!string.IsNullOrEmpty(localizedName) && localizedName != skillKey)
                {
                    return localizedName;
                }

                // Fallback al nombre del enum
                return skillToShow.ToString();
            }
            catch
            {
                return skillToShow.ToString();
            }
        }
    }

    /// <summary>
    /// Parche para accesibilizar los elementos individuales de talento
    /// </summary>
    [HarmonyPatch(typeof(PugOther.SkillTalentUIElement))]
    public static class SkillTalentUIElementPatch
    {
        private static string lastAnnouncedTalent = "";

        /// <summary>
        /// Anuncia información del talento cuando se selecciona
        /// </summary>
        [HarmonyPatch("OnSelected")]
        [HarmonyPostfix]
        public static void OnSelected_Postfix(PugOther.SkillTalentUIElement __instance)
        {
            try
            {
                if (__instance == null) return;

                string talentInfo = GetTalentInformation(__instance);
                if (!string.IsNullOrEmpty(talentInfo))
                {
                    string talentId = $"talent_{__instance.GetInstanceID()}";
                    if (talentId != lastAnnouncedTalent)
                    {
                        UIManager.Speak(talentInfo);
                        lastAnnouncedTalent = talentId;
                    }
                }
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
            lastAnnouncedTalent = "";
        }

        /// <summary>
        /// Anuncia cuando se hace click en un talento
        /// </summary>
        [HarmonyPatch("OnLeftClicked")]
        [HarmonyPostfix]
        public static void OnLeftClicked_Postfix(PugOther.SkillTalentUIElement __instance)
        {
            try
            {
                if (__instance == null) return;

                // Obtener información básica del talento para confirmar la acción
                var hoverTitle = __instance.GetHoverTitle();
                if (hoverTitle != null && !string.IsNullOrEmpty(hoverTitle.text))
                {
                    string talentName = ProcessTalentText(hoverTitle.text, hoverTitle.formatFields);

                    // Confirmar la acción (el juego ya manejó si era válida)
                    UIManager.Speak($"Acción en talento: {talentName}");
                }
            }
            catch
            {
                UIManager.Speak("Talento seleccionado");
            }
        }

        /// <summary>
        /// Obtiene información completa del talento
        /// </summary>
        private static string GetTalentInformation(PugOther.SkillTalentUIElement talent)
        {
            try
            {
                var sb = new System.Text.StringBuilder();

                // 1. Título del talento
                var hoverTitle = talent.GetHoverTitle();
                if (hoverTitle != null && !string.IsNullOrEmpty(hoverTitle.text))
                {
                    string talentTitle = ProcessTalentText(hoverTitle.text, hoverTitle.formatFields);
                    if (!string.IsNullOrEmpty(talentTitle))
                    {
                        sb.Append(talentTitle);
                    }
                }

                // 2. Estadísticas del talento (efectos)
                var hoverStats = talent.GetHoverStats(false);
                if (hoverStats != null && hoverStats.Count > 0)
                {
                    foreach (var stat in hoverStats)
                    {
                        if (!string.IsNullOrEmpty(stat.text))
                        {
                            string statText = ProcessTalentText(stat.text, stat.formatFields);
                            if (!string.IsNullOrEmpty(statText))
                            {
                                sb.Append(". ").Append(statText);
                            }
                        }
                    }
                }

                // 3. Descripción del talento
                var hoverDescription = talent.GetHoverDescription();
                if (hoverDescription != null && hoverDescription.Count > 0)
                {
                    foreach (var desc in hoverDescription)
                    {
                        if (!string.IsNullOrEmpty(desc.text))
                        {
                            string descText = ProcessTalentText(desc.text, desc.formatFields);
                            if (!string.IsNullOrEmpty(descText) && !descText.Contains("Click") && !descText.Contains("clic"))
                            {
                                sb.Append(". ").Append(descText);
                            }
                        }
                    }
                }

                string result = sb.ToString();
                return !string.IsNullOrEmpty(result) ? result : "Talento";
            }
            catch
            {
                return "Talento";
            }
        }

        /// <summary>
        /// Procesa el texto del talento aplicando formatFields correctamente
        /// </summary>
        private static string ProcessTalentText(string text, object[] formatFields)
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
    }
}