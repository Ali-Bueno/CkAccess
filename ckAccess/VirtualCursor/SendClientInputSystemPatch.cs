extern alias PugOther;
using HarmonyLib;
using Unity.Mathematics;
using UnityEngine;

namespace ckAccess.VirtualCursor
{
    /// <summary>
    /// Patch crítico que intercepta CalculateMouseOrJoystickWorldPoint para forzar
    /// que use la posición del cursor virtual cuando está activo.
    ///
    /// PROBLEMA RESUELTO:
    /// - Con TECLADO (O): el juego usa PATH 1 (mouse físico) - ignoraba cursor virtual
    /// - Con MANDO (L2): el juego usa PATH 2 (stick derecho) - funcionaba con cursor virtual
    ///
    /// SOLUCIÓN:
    /// Este parche sobrescribe la posición calculada cuando el cursor virtual está activo,
    /// independientemente del método de input usado.
    /// </summary>
    [HarmonyPatch(typeof(PugOther.SendClientInputSystem))]
    public static class SendClientInputSystemPatch
    {
        /// <summary>
        /// Intercepta el cálculo de posición del mouse/joystick para usar cursor virtual cuando está activo.
        /// Este es el método que el juego usa para determinar DÓNDE colocar objetos.
        /// </summary>
        [HarmonyPatch("CalculateMouseOrJoystickWorldPoint")]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.VeryHigh)] // Alta prioridad para ejecutar después de AutoTargeting
        public static void CalculateMouseOrJoystickWorldPoint_Postfix(
            ref float2 __result,
            PugOther.PlayerController playerController)
        {
            try
            {
                // Solo en gameplay, no en menús
                if (!IsInGameplay())
                    return;

                // PRIORIDAD 1: Auto-targeting (si está activo, tiene máxima prioridad)
                var autoTargetPos = Patches.Player.AutoTargetingPatch.GetCurrentTargetPosition();
                if (autoTargetPos.HasValue)
                {
                    __result = new float2(autoTargetPos.Value.x, autoTargetPos.Value.z);
                    return;
                }

                // PRIORIDAD 2: Cursor virtual (si está activo y alejado del jugador)
                if (PlayerInputPatch.HasActiveCursor())
                {
                    Vector3 virtualCursorPos = PlayerInputPatch.GetVirtualCursorPosition();
                    __result = new float2(virtualCursorPos.x, virtualCursorPos.z);
                    return;
                }

                // PRIORIDAD 3: Dejar el comportamiento original del juego
                // (mouse físico para teclado, stick para mando)
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[SendClientInputSystemPatch] Error en CalculateMouseOrJoystickWorldPoint: {ex}");
            }
        }

        /// <summary>
        /// Verifica si estamos en gameplay (no en menús)
        /// Usa la misma lógica que PlayerInputPatch
        /// </summary>
        private static bool IsInGameplay()
        {
            try
            {
                // No funcionar si no hay Manager o jugador
                if (PugOther.Manager.main == null || PugOther.Manager.main.player == null)
                    return false;

                // No funcionar en inventarios (el sistema de inventario tiene su propio manejo)
                if (PugOther.Manager.ui?.isAnyInventoryShowing == true)
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
