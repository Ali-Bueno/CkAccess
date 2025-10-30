extern alias PugOther;
using HarmonyLib;
using UnityEngine;

namespace ckAccess.Patches.Player
{
    /// <summary>
    /// Patch agresivo que fuerza el desbloqueo del input cuando detecta que está bloqueado incorrectamente
    /// </summary>
    [HarmonyPatch(typeof(PugOther.PlayerController), "ManagedUpdate")]
    public static class ForceInputUnlockPatch
    {
        private static bool hasLoggedFix = false;
        private static float lastCheckTime = 0f;
        private const float CHECK_INTERVAL = 0.5f; // Verificar cada 0.5 segundos

        [HarmonyPostfix]
        public static void Postfix(PugOther.PlayerController __instance)
        {
            try
            {
                // Solo verificar periódicamente para no sobrecargar
                if (Time.time - lastCheckTime < CHECK_INTERVAL)
                    return;

                lastCheckTime = Time.time;

                // Verificar si estamos en gameplay (no en menús)
                if (!IsInGameplay())
                    return;

                // CRÍTICO: Forzar limpieza si detectamos que el input está bloqueado
                var input = PugOther.Manager.input;
                if (input != null)
                {
                    // InputManager tiene un campo "activeInputField" y una propiedad "textInputIsActive"
                    var activeInputFieldProp = AccessTools.Property(input.GetType(), "activeInputField");
                    var textInputIsActiveProp = AccessTools.Property(input.GetType(), "textInputIsActive");

                    if (activeInputFieldProp != null && textInputIsActiveProp != null)
                    {
                        // Verificar si textInputIsActive está en true
                        bool textInputIsActive = (bool)textInputIsActiveProp.GetValue(input);

                        if (textInputIsActive)
                        {
                            // Obtener el activeInputField
                            var activeField = activeInputFieldProp.GetValue(input);

                            // Si está activo pero no debería estarlo en gameplay, limpiarlo
                            if (activeField != null)
                            {
                                // Usar reflexión para setear activeInputField a null
                                var setMethod = activeInputFieldProp.GetSetMethod(true); // true para acceder a setter privado
                                if (setMethod != null)
                                {
                                    setMethod.Invoke(input, new object[] { null });

                                    if (!hasLoggedFix)
                                    {
                                        UnityEngine.Debug.Log("[ForceInputUnlock] ¡activeInputField limpiado! Input desbloqueado");
                                        hasLoggedFix = true;
                                    }
                                }
                                else
                                {
                                    // Intentar usando SetActiveInputField si existe
                                    var setActiveInputMethod = AccessTools.Method(input.GetType(), "SetActiveInputField");
                                    if (setActiveInputMethod != null)
                                    {
                                        setActiveInputMethod.Invoke(input, new object[] { null });

                                        if (!hasLoggedFix)
                                        {
                                            UnityEngine.Debug.Log("[ForceInputUnlock] ¡activeInputField limpiado via SetActiveInputField!");
                                            hasLoggedFix = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                // Fallar silenciosamente para no interrumpir el juego
                UnityEngine.Debug.LogWarning($"[ForceInputUnlock] Error (ignorado): {ex.Message}");
            }
        }

        /// <summary>
        /// Verifica si estamos en gameplay real
        /// </summary>
        private static bool IsInGameplay()
        {
            try
            {
                var main = PugOther.Manager.main;
                if (main == null || main.player == null)
                    return false;

                var uiManager = PugOther.Manager.ui;
                if (uiManager != null)
                {
                    // Si hay inventario abierto, no es gameplay libre
                    if (uiManager.isAnyInventoryShowing)
                        return false;

                    // Si el juego está pausado, no es gameplay
                    if (Time.timeScale == 0f)
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Reset para logging
        /// </summary>
        public static void ResetLogging()
        {
            hasLoggedFix = false;
        }
    }
}
