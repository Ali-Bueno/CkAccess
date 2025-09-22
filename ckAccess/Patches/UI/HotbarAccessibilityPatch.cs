extern alias PugOther;
using HarmonyLib;
using UnityEngine;
using ckAccess.Localization;

namespace ckAccess.Patches.UI
{
    /// <summary>
    /// Parche simplificado para accesibilidad del hotbar
    /// </summary>
    public static class HotbarAccessibilityPatch
    {
        /// <summary>
        /// Anuncia información básica del hotbar
        /// </summary>
        public static void AnnounceCurrentHotbarSlot()
        {
            try
            {
                var player = PugOther.Manager.main.player;
                if (player == null)
                {
                    UIManager.Speak(LocalizationManager.GetText("no_weapon_equipped"));
                    return;
                }

                // Simplificado: solo anuncia que hay información del hotbar disponible
                UIManager.Speak(LocalizationManager.GetText("hotbar_info_available"));
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Exception in AnnounceCurrentHotbarSlot: {ex}");
                UIManager.Speak(LocalizationManager.GetText("hotbar_error"));
            }
        }

        /// <summary>
        /// Maneja la navegación del hotbar con teclas especiales (versión simplificada)
        /// </summary>
        public static bool HandleHotbarNavigation()
        {
            try
            {
                // Detectar teclas de navegación del hotbar
                bool altHeld = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

                if (altHeld && Input.GetKeyDown(KeyCode.Space))
                {
                    AnnounceCurrentHotbarSlot();
                    return true;
                }

                return false;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Exception in HandleHotbarNavigation: {ex}");
                return false;
            }
        }
    }
}