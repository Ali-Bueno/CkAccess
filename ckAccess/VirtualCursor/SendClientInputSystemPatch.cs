extern alias PugOther;
using HarmonyLib;
using Unity.Mathematics;

namespace ckAccess.VirtualCursor
{
    /// <summary>
    /// Patch que intercepta el SendClientInputSystem para redirigir las acciones del cursor virtual
    /// </summary>
    [HarmonyPatch(typeof(PugOther.SendClientInputSystem))]
    public static class SendClientInputSystemPatch
    {
        // Variables para controlar cuándo usar el cursor virtual
        private static bool _useVirtualCursorForPrimaryAction = false;
        private static bool _useVirtualCursorForSecondaryAction = false;
        private static bool _useVirtualCursorForInteraction = false;
        private static float3 _virtualCursorWorldPosition;

        /// <summary>
        /// Configura el cursor virtual para la próxima acción primaria (U)
        /// </summary>
        public static void SetVirtualCursorPrimaryAction(float3 worldPosition)
        {
            _virtualCursorWorldPosition = worldPosition;
            _useVirtualCursorForPrimaryAction = true;
            _useVirtualCursorForSecondaryAction = false;
            _useVirtualCursorForInteraction = false;
        }

        /// <summary>
        /// Configura el cursor virtual para la próxima acción secundaria (O)
        /// </summary>
        public static void SetVirtualCursorSecondaryAction(float3 worldPosition)
        {
            _virtualCursorWorldPosition = worldPosition;
            _useVirtualCursorForPrimaryAction = false;
            _useVirtualCursorForSecondaryAction = true;
            _useVirtualCursorForInteraction = false;
        }

        /// <summary>
        /// Configura el cursor virtual para la próxima interacción
        /// </summary>
        public static void SetVirtualCursorInteraction(float3 worldPosition)
        {
            _virtualCursorWorldPosition = worldPosition;
            _useVirtualCursorForPrimaryAction = false;
            _useVirtualCursorForSecondaryAction = false;
            _useVirtualCursorForInteraction = true;
        }

        [HarmonyPatch("CalculateMouseOrJoystickWorldPoint")]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.High)] // Alta prioridad para ejecutar antes que AutoTargeting
        public static void CalculateMouseOrJoystickWorldPoint_Postfix(ref float2 __result, PugOther.PlayerController playerController)
        {
            try
            {
                // Primero verificar si hay un objetivo de auto-targeting activo
                var autoTargetPos = Patches.Player.AutoTargetingPatch.GetCurrentTargetPosition();

                if (autoTargetPos.HasValue)
                {
                    // Si hay auto-target activo, usar esa posición
                    __result = new float2(autoTargetPos.Value.x, autoTargetPos.Value.z);
                }
                else if (_useVirtualCursorForPrimaryAction || _useVirtualCursorForSecondaryAction || _useVirtualCursorForInteraction)
                {
                    // Si no hay auto-target pero sí cursor virtual, usar cursor virtual
                    __result = _virtualCursorWorldPosition.xy;
                }
                // Si no hay ninguno de los dos, dejar el resultado original del juego
            }
            catch (System.Exception ex)
            {
                // En caso de error, mantener el comportamiento original
                UnityEngine.Debug.LogError($"Error en parche de cursor virtual: {ex}");
            }
        }

        [HarmonyPatch("OnUpdate")]
        [HarmonyPrefix]
        public static void OnUpdate_Prefix()
        {
            try
            {
                // Activar las simulaciones de input cuando el cursor virtual está activo
                if (_useVirtualCursorForPrimaryAction)
                {
                    PlayerInputPatch.SimulateInteract();
                    // No resetear aquí - mantener activo mientras la tecla esté presionada
                }
                else if (_useVirtualCursorForSecondaryAction)
                {
                    PlayerInputPatch.SimulateSecondInteract();
                    // No resetear aquí - mantener activo mientras la tecla esté presionada
                }
                else if (_useVirtualCursorForInteraction)
                {
                    PlayerInputPatch.SimulateInteractWithObject();
                    // No resetear aquí - mantener activo mientras la tecla esté presionada
                }
            }
            catch (System.Exception ex)
            {
                Patches.UI.UIManager.Speak($"Error activando simulación: {ex.Message}");
            }
        }

        /// <summary>
        /// Detiene todas las acciones del cursor virtual
        /// </summary>
        public static void StopVirtualCursorAction()
        {
            _useVirtualCursorForPrimaryAction = false;
            _useVirtualCursorForSecondaryAction = false;
            _useVirtualCursorForInteraction = false;
        }

    }
}