extern alias PugOther;
using HarmonyLib;
using Unity.Mathematics;

namespace ckAccess.VirtualCursor
{
    /// <summary>
    /// Patch directo en PlayerInput para simular botones presionados
    /// </summary>
    [HarmonyPatch(typeof(PugOther.PlayerInput))]
    public static class PlayerInputPatch
    {
        // Variables para controlar la simulación de input - ahora representan estado continuo
        private static bool _simulateInteract = false;
        private static bool _simulateSecondInteract = false;
        private static bool _simulateInteractWithObject = false;

        // Variables para detectar el primer frame de una nueva acción
        private static bool _interactJustPressed = false;
        private static bool _secondInteractJustPressed = false;
        private static bool _interactWithObjectJustPressed = false;

        /// <summary>
        /// Activa la simulación de INTERACT - estado continuo
        /// </summary>
        public static void SimulateInteract()
        {
            if (!_simulateInteract)
            {
                _simulateInteract = true;
                _interactJustPressed = true;
            }
        }

        /// <summary>
        /// Activa la simulación de SECOND_INTERACT - estado continuo
        /// </summary>
        public static void SimulateSecondInteract()
        {
            if (!_simulateSecondInteract)
            {
                _simulateSecondInteract = true;
                _secondInteractJustPressed = true;
            }
        }

        /// <summary>
        /// Activa la simulación de INTERACT_WITH_OBJECT - estado continuo
        /// </summary>
        public static void SimulateInteractWithObject()
        {
            if (!_simulateInteractWithObject)
            {
                _simulateInteractWithObject = true;
                _interactWithObjectJustPressed = true;
            }
        }

        /// <summary>
        /// Detiene todas las simulaciones
        /// </summary>
        public static void StopAllSimulations()
        {
            _simulateInteract = false;
            _simulateSecondInteract = false;
            _simulateInteractWithObject = false;
            _interactJustPressed = false;
            _secondInteractJustPressed = false;
            _interactWithObjectJustPressed = false;
        }

        [HarmonyPatch("WasButtonPressedDownThisFrame")]
        [HarmonyPostfix]
        public static void WasButtonPressedDownThisFrame_Postfix(ref bool __result, PugOther.PlayerInput.InputType inputType)
        {
            try
            {
                // Solo devolver true en el primer frame cuando se activa una simulación
                if (_interactJustPressed && inputType == PugOther.PlayerInput.InputType.INTERACT)
                {
                    __result = true;
                    _interactJustPressed = false; // Solo una vez por activación
                }
                else if (_secondInteractJustPressed && inputType == PugOther.PlayerInput.InputType.SECOND_INTERACT)
                {
                    __result = true;
                    _secondInteractJustPressed = false; // Solo una vez por activación
                }
                else if (_interactWithObjectJustPressed && inputType == PugOther.PlayerInput.InputType.INTERACT_WITH_OBJECT)
                {
                    __result = true;
                    _interactWithObjectJustPressed = false; // Solo una vez por activación
                }
            }
            catch (System.Exception ex)
            {
                Patches.UI.UIManager.Speak($"Error en WasButtonPressedDownThisFrame: {ex.Message}");
            }
        }

        [HarmonyPatch("IsButtonCurrentlyDown")]
        [HarmonyPostfix]
        public static void IsButtonCurrentlyDown_Postfix(ref bool __result, PugOther.PlayerInput.InputType inputType)
        {
            try
            {
                // Simular que el botón está presionado mientras la simulación está activa
                if (_simulateInteract && inputType == PugOther.PlayerInput.InputType.INTERACT)
                {
                    __result = true;
                }
                else if (_simulateSecondInteract && inputType == PugOther.PlayerInput.InputType.SECOND_INTERACT)
                {
                    __result = true;
                }
                else if (_simulateInteractWithObject && inputType == PugOther.PlayerInput.InputType.INTERACT_WITH_OBJECT)
                {
                    __result = true;
                }
            }
            catch (System.Exception ex)
            {
                Patches.UI.UIManager.Speak($"Error en IsButtonCurrentlyDown: {ex.Message}");
            }
        }
    }
}