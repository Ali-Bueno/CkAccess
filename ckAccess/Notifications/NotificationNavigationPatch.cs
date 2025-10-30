extern alias PugOther;
extern alias Core;

using HarmonyLib;
using UnityEngine;

namespace ckAccess.Notifications
{
    /// <summary>
    /// Parche para manejar la navegación por el historial de notificaciones.
    /// Punto (.) = Siguiente notificación (más reciente)
    /// Coma (,) = Anterior notificación (más antigua)
    /// Shift + Punto (.) = Saltar a última notificación
    /// Shift + Coma (,) = Saltar a primera notificación
    /// </summary>
    [HarmonyPatch(typeof(PugOther.PlayerController), "ManagedUpdate")]
    public static class NotificationNavigationPatch
    {
        // Debounce para evitar múltiples activaciones
        private static float _lastNavigationTime = 0f;
        private const float NAVIGATION_COOLDOWN = 0.2f; // 200ms entre navegaciones

        [HarmonyPostfix]
        public static void ManagedUpdate_Postfix()
        {
            try
            {
                // Verificar cooldown
                if (Time.time - _lastNavigationTime < NAVIGATION_COOLDOWN)
                    return;

                // Verificar si Shift está presionado
                bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                if (shiftHeld)
                {
                    // Shift + Punto (.) = Saltar a última notificación
                    if (Input.GetKeyDown(KeyCode.Period))
                    {
                        NotificationSystem.JumpToLatest();
                        _lastNavigationTime = Time.time;
                    }
                    // Shift + Coma (,) = Saltar a primera notificación
                    else if (Input.GetKeyDown(KeyCode.Comma))
                    {
                        NotificationSystem.JumpToFirst();
                        _lastNavigationTime = Time.time;
                    }
                }
                else
                {
                    // Punto (.) solo = Siguiente notificación (más reciente)
                    if (Input.GetKeyDown(KeyCode.Period))
                    {
                        NotificationSystem.NavigateToNext();
                        _lastNavigationTime = Time.time;
                    }
                    // Coma (,) solo = Anterior notificación (más antigua)
                    else if (Input.GetKeyDown(KeyCode.Comma))
                    {
                        NotificationSystem.NavigateToPrevious();
                        _lastNavigationTime = Time.time;
                    }
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[NotificationNavigation] Error: {ex}");
            }
        }
    }
}
