extern alias PugOther;
extern alias Core;

using HarmonyLib;
using ckAccess.Localization;
using System.Collections.Generic;
using UnityEngine;

namespace ckAccess.Notifications
{
    /// <summary>
    /// Parche para detectar cuando el jugador mejora una skill y generar notificaciones.
    /// </summary>
    [HarmonyPatch]
    public static class SkillUpNotificationPatch
    {
        // Cache de niveles de skills anteriores
        private static Dictionary<SkillID, int> _previousSkillLevels = new Dictionary<SkillID, int>();

        /// <summary>
        /// Parche en PlayerController para detectar cambios en skills
        /// </summary>
        [HarmonyPatch(typeof(PugOther.PlayerController), "ManagedUpdate")]
        [HarmonyPostfix]
        public static void PlayerController_ManagedUpdate_Postfix(PugOther.PlayerController __instance)
        {
            try
            {
                // Verificar cada skill
                foreach (object skillValue in System.Enum.GetValues(typeof(SkillID)))
                {
                    var skillID = (SkillID)skillValue;

                    // Saltar NUM_SKILLS (es solo un contador)
                    if (skillID == SkillID.NUM_SKILLS)
                        continue;

                    // Obtener nivel actual de la skill
                    int skillXP = PugOther.Manager.saves.GetSkillValue(skillID);
                    int currentLevel = PugOther.SkillExtensions.GetLevelFromSkill(skillID, skillXP);

                    // Verificar si tenemos un nivel anterior registrado
                    if (_previousSkillLevels.TryGetValue(skillID, out int previousLevel))
                    {
                        // Verificar si la skill subió de nivel
                        if (currentLevel > previousLevel)
                        {
                            // Obtener nombre de la skill
                            string skillName = GetSkillName(skillID);

                            // Crear mensaje de notificación
                            string message = LocalizationManager.GetText("skill_up", skillName, currentLevel.ToString());

                            // Agregar notificación
                            NotificationSystem.AddNotification(message, NotificationSystem.NotificationType.SkillUp);

                            UnityEngine.Debug.Log($"[SkillUp] {skillName} subió a nivel {currentLevel}");
                        }
                    }

                    // Actualizar cache
                    _previousSkillLevels[skillID] = currentLevel;
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[SkillUpNotification] Error: {ex}");
            }
        }

        /// <summary>
        /// Obtiene el nombre localizado de una skill
        /// </summary>
        private static string GetSkillName(SkillID skillID)
        {
            // Copiar a variable local para evitar warning Harmony003
            var skill = skillID;

            // Mapeo de SkillID a claves de localización
            string localizationKey = $"skill_{skill.ToString().ToLower()}";

            // Intentar obtener texto localizado
            string localizedName = LocalizationManager.GetText(localizationKey);

            // Si no hay localización, usar el enum como fallback
            if (localizedName == localizationKey || string.IsNullOrEmpty(localizedName))
            {
                return skill.ToString();
            }

            return localizedName;
        }
    }
}
