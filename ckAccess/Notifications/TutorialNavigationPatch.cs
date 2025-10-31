extern alias PugOther;
extern alias Core;

using HarmonyLib;
using UnityEngine;

namespace ckAccess.Notifications
{
    /// <summary>
    /// Parche para manejar la navegación por el buffer de tutoriales.
    /// ' (apóstrofe) = Siguiente tutorial (más reciente)
    /// ¡ (exclamación invertida) = Tutorial anterior (más antiguo)
    /// Shift + ' = Saltar al último tutorial
    /// Shift + ¡ = Saltar al primer tutorial
    /// </summary>
    [HarmonyPatch(typeof(PugOther.PlayerController), "ManagedUpdate")]
    public static class TutorialNavigationPatch
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

                // ' (apóstrofe) = Quote en teclado US
                // En teclado español, ' está en la tecla con el acento
                if (Input.GetKeyDown(KeyCode.Quote))
                {
                    if (shiftHeld)
                    {
                        // Shift + ' = Saltar al último tutorial
                        TutorialBufferSystem.JumpToLatest();
                    }
                    else
                    {
                        // ' solo = Siguiente tutorial
                        TutorialBufferSystem.NavigateToNext();
                    }
                    _lastNavigationTime = Time.time;
                }
                // ¡ (exclamación invertida) en teclado español está en AltGr + 1
                // Pero también vamos a usar Semicolon (;/Ñ en español) como alternativa
                else if (Input.GetKeyDown(KeyCode.Semicolon))
                {
                    if (shiftHeld)
                    {
                        // Shift + Ñ = Saltar al primer tutorial
                        TutorialBufferSystem.JumpToFirst();
                    }
                    else
                    {
                        // Ñ solo = Tutorial anterior
                        TutorialBufferSystem.NavigateToPrevious();
                    }
                    _lastNavigationTime = Time.time;
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[TutorialNavigation] Error: {ex}");
            }
        }
    }
}
