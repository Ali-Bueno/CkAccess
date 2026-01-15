extern alias PugOther;
using HarmonyLib;

namespace ckAccess.Helpers
{
    /// <summary>
    /// Helper centralizado para detectar el estado del juego (gameplay vs menús).
    /// Unifica la lógica duplicada de detección de estado en un solo lugar.
    /// </summary>
    public static class GameplayStateHelper
    {
        /// <summary>
        /// Verifica si el jugador está en gameplay activo (no en menús, no escribiendo, no pausado).
        /// </summary>
        /// <returns>True si está en gameplay activo, false en caso contrario</returns>
        public static bool IsInGameplay()
        {
            try
            {
                // Si no hay jugador activo, no estamos en gameplay
                var main = PugOther.Manager.main;
                if (main == null || main.player == null)
                    return false;

                // Verificar si hay un campo de texto activo
                if (IsTextInputActive())
                    return false;

                // Si el juego está pausado, no estamos en gameplay
                if (UnityEngine.Time.timeScale == 0f)
                    return false;

                // Verificar que el controlador del jugador esté activo
                if (!IsPlayerControllerActive(main))
                    return false;

                return true;
            }
            catch
            {
                // Si hay error detectando el estado, asumir que no estamos en gameplay por seguridad
                return false;
            }
        }

        /// <summary>
        /// Verifica si el jugador está en un menú excluido (opuesto de IsInGameplay).
        /// Útil para parches que necesitan la lógica invertida.
        /// </summary>
        /// <returns>True si está en menú/excluido, false si está en gameplay</returns>
        public static bool IsInExcludedMenu()
        {
            return !IsInGameplay();
        }

        /// <summary>
        /// Verifica si el jugador está en gameplay Y no tiene inventario abierto.
        /// Útil para sistemas que deben desactivarse cuando hay UI abierta.
        /// </summary>
        /// <returns>True si está en gameplay sin inventario, false en caso contrario</returns>
        public static bool IsInGameplayWithoutInventory()
        {
            if (!IsInGameplay())
                return false;

            // Verificar inventario
            var uiManager = PugOther.Manager.ui;
            if (uiManager != null && uiManager.isAnyInventoryShowing)
                return false;

            return true;
        }

        /// <summary>
        /// Verifica si hay un campo de texto activo (escribiendo nombres, chat, etc.)
        /// </summary>
        private static bool IsTextInputActive()
        {
            try
            {
                var input = PugOther.Manager.input;
                if (input == null)
                    return false;

                var textInputIsActiveProp = AccessTools.Property(input.GetType(), "textInputIsActive");
                if (textInputIsActiveProp != null)
                {
                    return (bool)textInputIsActiveProp.GetValue(input);
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Verifica si el PlayerController está activo y habilitado.
        /// </summary>
        private static bool IsPlayerControllerActive(PugOther.Manager main)
        {
            try
            {
                var playerController = main.player as PugOther.PlayerController;
                if (playerController == null)
                    return false;

                if (!playerController.enabled || !playerController.gameObject.activeInHierarchy)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
