extern alias PugOther;
extern alias Core;

using HarmonyLib;
using ckAccess.Localization;
using UnityEngine;

namespace ckAccess.Notifications
{
    /// <summary>
    /// Parche para detectar cuando el jugador sube de nivel total (suma de niveles de skills).
    /// Core Keeper no tiene un nivel de personaje único, sino niveles individuales por skill.
    /// Este sistema rastrea el nivel total (suma de todas las skills) para notificar progresión general.
    /// </summary>
    [HarmonyPatch]
    public static class LevelUpNotificationPatch
    {
        // Cache del nivel total anterior (suma de todos los niveles de skills)
        private static int _previousTotalLevel = -1;

        /// <summary>
        /// Parche en PlayerController para detectar cambios de nivel total
        /// </summary>
        [HarmonyPatch(typeof(PugOther.PlayerController), "ManagedUpdate")]
        [HarmonyPostfix]
        public static void PlayerController_ManagedUpdate_Postfix(PugOther.PlayerController __instance)
        {
            try
            {
                // Calcular nivel total (suma de todos los niveles de skills)
                int currentTotalLevel = 0;
                foreach (object skillValue in System.Enum.GetValues(typeof(SkillID)))
                {
                    var skillID = (SkillID)skillValue;

                    // Saltar NUM_SKILLS (es solo un contador)
                    if (skillID == SkillID.NUM_SKILLS)
                        continue;

                    int skillXP = PugOther.Manager.saves.GetSkillValue(skillID);
                    int skillLevel = PugOther.SkillExtensions.GetLevelFromSkill(skillID, skillXP);
                    currentTotalLevel += skillLevel;
                }

                // Inicializar en el primer frame
                if (_previousTotalLevel == -1)
                {
                    _previousTotalLevel = currentTotalLevel;
                    return;
                }

                // Verificar si subió de nivel total
                if (currentTotalLevel > _previousTotalLevel)
                {
                    // Crear mensaje de notificación
                    string message = LocalizationManager.GetText("level_up", currentTotalLevel.ToString());

                    // Agregar notificación
                    NotificationSystem.AddNotification(message, NotificationSystem.NotificationType.LevelUp);

                    // Actualizar cache
                    _previousTotalLevel = currentTotalLevel;

                    UnityEngine.Debug.Log($"[LevelUp] Nivel total del jugador subió a {currentTotalLevel}");
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[LevelUpNotification] Error: {ex}");
            }
        }
    }
}
