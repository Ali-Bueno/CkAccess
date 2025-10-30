extern alias PugOther;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace ckAccess.VirtualCursor
{
    /// <summary>
    /// Emula el comportamiento del stick derecho del mando (right stick) y los triggers (R2/L2)
    /// I/J/K/L = Stick derecho para apuntar
    /// U = R2 (Right Trigger) = INTERACT
    /// O = L2 (Left Trigger) = SECOND_INTERACT
    /// </summary>
    [HarmonyPatch(typeof(PugOther.PlayerInput))]
    public static class PlayerInputPatch
    {
        // Estado actual del "stick derecho virtual" basado en I/J/K/L
        private static Vector2 _virtualAimInput = Vector2.zero;

        // Sistema de coordenadas del cursor relativo al jugador
        private static int _cursorOffsetX = 0; // Offset en eje X (J/L)
        private static int _cursorOffsetZ = 0; // Offset en eje Z (I/K)

        private static float _lastKeyPressTime = 0f; // Para debounce de teclado
        private const int MAX_CURSOR_OFFSET = 10; // Máximo offset en cualquier dirección
        private const float KEY_DEBOUNCE_TIME = 0.2f; // 200ms para evitar repetición automática del teclado

        /// <summary>
        /// Actualiza el estado del stick derecho virtual basado en las teclas presionadas
        /// </summary>
        public static void UpdateVirtualAimInput()
        {
            // Solo actualizar si no hay inventarios abiertos (estamos en gameplay)
            if (PugOther.Manager.ui != null && PugOther.Manager.ui.isAnyInventoryShowing)
            {
                _virtualAimInput = Vector2.zero;
                return;
            }

            float currentTime = UnityEngine.Time.time;

            // Detectar pulsaciones únicas con debounce
            if ((Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.K) ||
                Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.L)) &&
                (currentTime - _lastKeyPressTime) > KEY_DEBOUNCE_TIME)
            {
                _lastKeyPressTime = currentTime;

                // Modificar coordenadas del cursor según la tecla presionada
                bool hitLimit = false;

                if (Input.GetKeyDown(KeyCode.I)) // Norte (Z+)
                {
                    _cursorOffsetZ++;
                    if (_cursorOffsetZ > MAX_CURSOR_OFFSET)
                    {
                        _cursorOffsetZ = MAX_CURSOR_OFFSET;
                        hitLimit = true;
                    }
                }
                else if (Input.GetKeyDown(KeyCode.K)) // Sur (Z-)
                {
                    _cursorOffsetZ--;
                    if (_cursorOffsetZ < -MAX_CURSOR_OFFSET)
                    {
                        _cursorOffsetZ = -MAX_CURSOR_OFFSET;
                        hitLimit = true;
                    }
                }
                else if (Input.GetKeyDown(KeyCode.J)) // Oeste (X-)
                {
                    _cursorOffsetX--;
                    if (_cursorOffsetX < -MAX_CURSOR_OFFSET)
                    {
                        _cursorOffsetX = -MAX_CURSOR_OFFSET;
                        hitLimit = true;
                    }
                }
                else if (Input.GetKeyDown(KeyCode.L)) // Este (X+)
                {
                    _cursorOffsetX++;
                    if (_cursorOffsetX > MAX_CURSOR_OFFSET)
                    {
                        _cursorOffsetX = MAX_CURSOR_OFFSET;
                        hitLimit = true;
                    }
                }

                // Calcular la dirección del stick virtual basado en la posición del cursor
                UpdateStickDirectionFromCursorPosition();

                // Anunciar la nueva posición o el límite
                if (hitLimit)
                {
                    Patches.UI.UIManager.Speak(Localization.LocalizationManager.GetText("cursor_limit_reached"));
                }
                else
                {
                    AnnounceCursorPosition();
                }
            }

            // CRÍTICO: Mantener el stick apuntando hacia el cursor incluso sin teclas presionadas
            // Esto permite que U/O funcionen en la dirección correcta
            if (_cursorOffsetX != 0 || _cursorOffsetZ != 0)
            {
                UpdateStickDirectionFromCursorPosition();
            }
            else
            {
                _virtualAimInput = Vector2.zero;
            }
        }

        /// <summary>
        /// Actualiza la dirección del stick virtual para que apunte hacia la posición del cursor
        /// </summary>
        private static void UpdateStickDirectionFromCursorPosition()
        {
            if (_cursorOffsetX == 0 && _cursorOffsetZ == 0)
            {
                _virtualAimInput = Vector2.zero;
                return;
            }

            // El stick virtual debe apuntar en la dirección del cursor relativo al jugador
            Vector2 direction = new Vector2(_cursorOffsetX, _cursorOffsetZ);

            // Normalizar para que el stick tenga magnitud 1
            if (direction.magnitude > 0.01f)
            {
                direction.Normalize();
            }

            _virtualAimInput = direction;
        }

        /// <summary>
        /// Resetea el cursor a la posición del jugador (tecla R)
        /// </summary>
        public static void ResetCursorDistance()
        {
            _cursorOffsetX = 0;
            _cursorOffsetZ = 0;
            _virtualAimInput = Vector2.zero;
        }

        /// <summary>
        /// Obtiene la posición actual del cursor virtual basado en las coordenadas
        /// </summary>
        public static Vector3 GetVirtualCursorPosition()
        {
            var player = PugOther.Manager.main?.player;
            if (player == null) return Vector3.zero;

            var playerPos = player.WorldPosition;

            return new Vector3(
                playerPos.x + _cursorOffsetX,
                playerPos.y,
                playerPos.z + _cursorOffsetZ
            );
        }

        /// <summary>
        /// Anuncia la posición actual del cursor
        /// </summary>
        private static void AnnounceCursorPosition()
        {
            try
            {
                var cursorPosition = GetVirtualCursorPosition();

                // Obtener descripción de lo que hay en esa posición
                string objectInfo = MapReader.SimpleWorldReader.GetSimpleDescription(cursorPosition);

                // Anunciar solo el contenido, sin distancia
                string announcement = !string.IsNullOrEmpty(objectInfo) ? objectInfo : "Vacío";

                Patches.UI.UIManager.Speak(announcement);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error announcing cursor position: {ex}");
            }
        }

        /// <summary>
        /// Convierte un vector de dirección a nombre en español
        /// </summary>
        private static string GetDirectionName(Vector2 dir)
        {
            // Direcciones cardinales e intermedias
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360;

            if (angle >= 337.5f || angle < 22.5f) return "derecha";
            if (angle >= 22.5f && angle < 67.5f) return "arriba-derecha";
            if (angle >= 67.5f && angle < 112.5f) return "arriba";
            if (angle >= 112.5f && angle < 157.5f) return "arriba-izquierda";
            if (angle >= 157.5f && angle < 202.5f) return "izquierda";
            if (angle >= 202.5f && angle < 247.5f) return "abajo-izquierda";
            if (angle >= 247.5f && angle < 292.5f) return "abajo";
            if (angle >= 292.5f && angle < 337.5f) return "abajo-derecha";

            return "centro";
        }

        /// <summary>
        /// Intercepta GetInputAxisValue para retornar nuestro input virtual del stick derecho
        /// </summary>
        [HarmonyPatch("GetInputAxisValue", typeof(PugOther.PlayerInput.InputAxisType), typeof(PugOther.PlayerInput.InputAxisType))]
        [HarmonyPostfix]
        public static void GetInputAxisValue_Postfix(
            ref Vector2 __result,
            PugOther.PlayerInput.InputAxisType horizontalAxisType,
            PugOther.PlayerInput.InputAxisType verticalAxisType)
        {
            // Solo interceptar cuando se solicita CHARACTER_AIM (stick derecho)
            if (horizontalAxisType == PugOther.PlayerInput.InputAxisType.CHARACTER_AIM_HORIZONTAL &&
                verticalAxisType == PugOther.PlayerInput.InputAxisType.CHARACTER_AIM_VERTICAL)
            {
                // Si hay input virtual del stick, sobrescribir el resultado
                if (_virtualAimInput.magnitude > 0.01f)
                {
                    __result = _virtualAimInput;
                }
            }
        }

        /// <summary>
        /// Mapea U a R2 (Right Trigger) = INTERACT
        /// </summary>
        [HarmonyPatch("WasButtonPressedDownThisFrame")]
        [HarmonyPostfix]
        public static void WasButtonPressedDownThisFrame_Postfix(ref bool __result, PugOther.PlayerInput.InputType inputType)
        {
            // Solo mapear si no hay inventarios abiertos (estamos en gameplay)
            if (PugOther.Manager.ui != null && PugOther.Manager.ui.isAnyInventoryShowing)
                return;

            // U = R2 = INTERACT (ataque/acción primaria)
            if (inputType == PugOther.PlayerInput.InputType.INTERACT && Input.GetKeyDown(KeyCode.U))
            {
                __result = true;
            }
            // O = L2 = SECOND_INTERACT (acción secundaria/usar)
            else if (inputType == PugOther.PlayerInput.InputType.SECOND_INTERACT && Input.GetKeyDown(KeyCode.O))
            {
                __result = true;
            }
        }

        /// <summary>
        /// Mantener triggers presionados para acciones continuas
        /// </summary>
        [HarmonyPatch("IsButtonCurrentlyDown")]
        [HarmonyPostfix]
        public static void IsButtonCurrentlyDown_Postfix(ref bool __result, PugOther.PlayerInput.InputType inputType)
        {
            // Solo mapear si no hay inventarios abiertos (estamos en gameplay)
            if (PugOther.Manager.ui != null && PugOther.Manager.ui.isAnyInventoryShowing)
                return;

            // U mantenido = R2 mantenido = INTERACT continuo (minar continuamente)
            if (inputType == PugOther.PlayerInput.InputType.INTERACT && Input.GetKey(KeyCode.U))
            {
                __result = true;
            }
            // O mantenido = L2 mantenido = SECOND_INTERACT continuo (colocar continuamente)
            else if (inputType == PugOther.PlayerInput.InputType.SECOND_INTERACT && Input.GetKey(KeyCode.O))
            {
                __result = true;
            }
        }


        // Métodos vacíos para compatibilidad con el código existente
        public static void SimulateInteract() { }
        public static void SimulateSecondInteract() { }
        public static void StopAllSimulations() { }
    }
}