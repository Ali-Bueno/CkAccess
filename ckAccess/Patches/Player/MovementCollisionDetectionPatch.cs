extern alias PugOther;
using HarmonyLib;
using ckAccess.Patches.UI;
using Unity.Mathematics;

namespace ckAccess.Patches.Player
{
    /// <summary>
    /// Parche para detectar cuando el jugador se choca con objetos sólidos y pausar los sonidos de pasos.
    /// Proporciona feedback auditivo para jugadores ciegos sobre obstáculos en el movimiento.
    /// </summary>
    [HarmonyPatch(typeof(PugOther.PlayerController))]
    public static class MovementCollisionDetectionPatch
    {
        // Variables para rastrear el estado del movimiento
        private static float3 _lastPlayerPosition = float3.zero;
        private static float3 _previousTargetVelocity = float3.zero;
        private static bool _wasMovingLastFrame = false;
        private static bool _isCollisionDetected = false;
        private static int _collisionFrameCount = 0;

        // Constantes para la detección
        private const float MOVEMENT_THRESHOLD = 0.01f; // Umbral para detectar movimiento
        private const int COLLISION_FRAME_THRESHOLD = 5; // Frames para confirmar colisión
        private const float VELOCITY_THRESHOLD = 0.1f; // Umbral de velocidad objetivo

        /// <summary>
        /// Parche que intercepta el método AE_FootStep para controlar si se reproducen los sonidos
        /// </summary>
        [HarmonyPatch("AE_FootStep")]
        [HarmonyPrefix]
        public static bool AE_FootStep_Prefix(PugOther.PlayerController __instance)
        {
            try
            {
                // Si se detecta colisión, evitar reproducir el sonido de pasos
                if (_isCollisionDetected)
                {
                    return false; // No ejecutar el método original
                }

                return true; // Permitir que se ejecute normalmente
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error en AE_FootStep_Prefix: {ex}");
                return true; // En caso de error, permitir el comportamiento normal
            }
        }

        /// <summary>
        /// Parche que monitorea el estado del jugador en cada frame para detectar colisiones
        /// </summary>
        [HarmonyPatch("ManagedUpdate")]
        [HarmonyPostfix]
        public static void ManagedUpdate_Postfix(PugOther.PlayerController __instance)
        {
            try
            {
                DetectMovementCollision(__instance);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error en ManagedUpdate_Postfix: {ex}");
            }
        }

        /// <summary>
        /// Lógica principal para detectar colisiones de movimiento
        /// </summary>
        private static void DetectMovementCollision(PugOther.PlayerController player)
        {
            try
            {
                // Obtener la posición actual del jugador
                var currentPosition = GetPlayerPosition(player);
                if (currentPosition.Equals(float3.zero))
                    return;

                // Obtener la velocidad objetivo actual
                var currentTargetVelocity = new float3(
                    player.targetMovementVelocity.x,
                    player.targetMovementVelocity.y,
                    player.targetMovementVelocity.z
                );

                // Verificar si el jugador está intentando moverse
                bool isIntentionallyMoving = math.length(currentTargetVelocity) > VELOCITY_THRESHOLD;

                if (isIntentionallyMoving)
                {
                    // Calcular el movimiento real
                    float3 actualMovement = currentPosition - _lastPlayerPosition;
                    float actualDistance = math.length(actualMovement);

                    // Detectar si hay una discrepancia entre intención y movimiento real
                    bool isActuallyMoving = actualDistance > MOVEMENT_THRESHOLD;

                    if (!isActuallyMoving && _wasMovingLastFrame)
                    {
                        // El jugador quiere moverse pero no se está moviendo = colisión
                        _collisionFrameCount++;

                        if (_collisionFrameCount >= COLLISION_FRAME_THRESHOLD && !_isCollisionDetected)
                        {
                            _isCollisionDetected = true;
                            // Opcional: notificar al usuario de la colisión
                            // UIManager.Speak("Bloqueado");
                        }
                    }
                    else if (isActuallyMoving)
                    {
                        // Se está moviendo normalmente, resetear detección de colisión
                        if (_isCollisionDetected)
                        {
                            _isCollisionDetected = false;
                            _collisionFrameCount = 0;
                        }
                        _wasMovingLastFrame = true;
                    }
                }
                else
                {
                    // No hay intención de moverse, resetear estado
                    if (_isCollisionDetected)
                    {
                        _isCollisionDetected = false;
                        _collisionFrameCount = 0;
                    }
                    _wasMovingLastFrame = false;
                }

                // Actualizar estados para el próximo frame
                _lastPlayerPosition = currentPosition;
                _previousTargetVelocity = currentTargetVelocity;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error en DetectMovementCollision: {ex}");
            }
        }

        /// <summary>
        /// Obtiene la posición actual del jugador de forma segura
        /// </summary>
        private static float3 GetPlayerPosition(PugOther.PlayerController player)
        {
            try
            {
                if (player == null) return float3.zero;

                // Intentar obtener WorldPosition primero
                try
                {
                    var worldPos = player.WorldPosition;
                    return new float3(worldPos.x, worldPos.y, worldPos.z);
                }
                catch
                {
                    // Fallback a transform position
                    if (player.transform != null)
                    {
                        var pos = player.transform.position;
                        return new float3(pos.x, pos.y, pos.z);
                    }
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error obteniendo posición del jugador: {ex}");
            }

            return float3.zero;
        }

        /// <summary>
        /// Método público para verificar si hay colisión detectada (para debugging)
        /// </summary>
        public static bool IsCollisionDetected => _isCollisionDetected;

        /// <summary>
        /// Método para resetear manualmente el estado de colisión (útil para debugging)
        /// </summary>
        public static void ResetCollisionState()
        {
            _isCollisionDetected = false;
            _collisionFrameCount = 0;
            _wasMovingLastFrame = false;
        }
    }
}