extern alias PugOther;
extern alias Core;

using HarmonyLib;
using DavyKager;
using ckAccess.MapReader;
using Unity.Mathematics;
using UnityEngine;

namespace ckAccess.VirtualCursor
{
    /// <summary>
    /// Sistema que anuncia el tile que está en frente del jugador según la dirección de movimiento.
    /// Solo anuncia cuando el tile cambia, evitando repeticiones innecesarias.
    /// </summary>
    [HarmonyPatch]
    public static class TileAheadAnnouncerPatch
    {
        // Cache del último tile anunciado para evitar repeticiones
        private static string _lastAnnouncedTile = null;
        private static Vector3 _lastAnnouncedPosition = new Vector3(float.MinValue, 0, float.MinValue);
        private static Vector3 _lastPlayerPosition = Vector3.zero;

        // Distancia en frente del jugador (1 tile adelante)
        private const float AHEAD_DISTANCE = 1.0f;

        // Control de tiempo para evitar spam
        private static float _lastAnnounceTime = 0f;
        private const float MIN_ANNOUNCE_INTERVAL = 0.3f; // Mínimo 300ms entre anuncios

        // Umbral de movimiento para detectar que el jugador se movió
        private const float MOVEMENT_THRESHOLD = 0.1f;

        /// <summary>
        /// Parche en PlayerController.ManagedUpdate para detectar movimiento
        /// </summary>
        [HarmonyPatch(typeof(PugOther.PlayerController), "ManagedUpdate")]
        [HarmonyPostfix]
        public static void PlayerController_ManagedUpdate_Postfix(PugOther.PlayerController __instance)
        {
            try
            {
                // NO anunciar tiles si hay algún menú/UI abierto
                if (IsAnyMenuOpen())
                    return;

                // Verificar cooldown de tiempo
                if (Time.time - _lastAnnounceTime < MIN_ANNOUNCE_INTERVAL)
                    return;

                // Obtener posición actual del jugador
                if (!TryGetPlayerPosition(__instance, out Vector3 playerPos))
                    return;

                // Inicializar la posición previa si es la primera vez
                if (_lastPlayerPosition == Vector3.zero)
                {
                    _lastPlayerPosition = playerPos;
                    return;
                }

                // NUEVO ENFOQUE: Detectar movimiento real comparando posiciones
                Vector3 movement = playerPos - _lastPlayerPosition;
                float movementDistance = new Vector3(movement.x, 0, movement.z).magnitude;

                // Si el jugador no se movió suficiente, no hacer nada
                if (movementDistance < MOVEMENT_THRESHOLD)
                {
                    return;
                }

                // El jugador se movió, calcular dirección del movimiento
                Vector3 movementDirection = new Vector3(movement.x, 0, movement.z).normalized;

                // Calcular posición del tile en frente según la dirección de movimiento REAL
                Vector3 aheadPosition = CalculateAheadPosition(playerPos, movementDirection);

                // Verificar si es una posición diferente a la última anunciada
                float dx = aheadPosition.x - _lastAnnouncedPosition.x;
                float dz = aheadPosition.z - _lastAnnouncedPosition.z;
                float distanceSq = (dx * dx) + (dz * dz);

                if (distanceSq < 0.25f) // 0.5f * 0.5f = 0.25f
                {
                    // Actualizar posición del jugador para el próximo frame
                    _lastPlayerPosition = playerPos;
                    return; // Misma posición, no anunciar
                }

                // Obtener descripción del tile en frente
                string tileDescription = SimpleWorldReader.GetSimpleDescription(aheadPosition);

                // Solo anunciar si el tile cambió
                if (tileDescription != _lastAnnouncedTile)
                {
                    // Anunciar el tile
                    if (!string.IsNullOrEmpty(tileDescription))
                    {
                        Tolk.Output(tileDescription, true); // Interrupt = true para anuncio inmediato
                    }

                    // Actualizar cache
                    _lastAnnouncedTile = tileDescription;
                    _lastAnnouncedPosition = aheadPosition;
                    _lastAnnounceTime = Time.time;
                }

                // Actualizar posición del jugador SIEMPRE al final
                _lastPlayerPosition = playerPos;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[TileAheadAnnouncer] Error: {ex}");
            }
        }

        /// <summary>
        /// Verifica si hay algún menú o UI abierto
        /// </summary>
        private static bool IsAnyMenuOpen()
        {
            try
            {
                var manager = PugOther.Manager.ui;
                if (manager == null) return false;

                // Verificar inventarios, cofres, mesas de trabajo, menús, etc.
                // Este campo cubre la mayoría de los casos: inventario, cofres, estaciones de trabajo, etc.
                return manager.isAnyInventoryShowing;
            }
            catch
            {
                // En caso de error, ser conservadores y asumir que hay un menú abierto
                return true;
            }
        }


        /// <summary>
        /// Calcula la posición del tile en frente del jugador según la dirección de movimiento
        /// </summary>
        private static Vector3 CalculateAheadPosition(Vector3 playerPos, Vector3 direction)
        {
            // Calcular posición 1 tile adelante en la dirección de movimiento
            Vector3 aheadPos = playerPos + (direction * AHEAD_DISTANCE);

            // Redondear a coordenadas de tile enteras
            aheadPos = new Vector3(
                Mathf.Round(aheadPos.x),
                aheadPos.y,
                Mathf.Round(aheadPos.z)
            );

            return aheadPos;
        }

        /// <summary>
        /// Obtiene la posición del jugador de forma segura
        /// </summary>
        private static bool TryGetPlayerPosition(PugOther.PlayerController player, out Vector3 position)
        {
            position = Vector3.zero;

            try
            {
                if (player == null) return false;

                var worldPos = player.WorldPosition;
                position = new Vector3(worldPos.x, worldPos.y, worldPos.z);
                return true;
            }
            catch
            {
                try
                {
                    if (player.transform != null)
                    {
                        var pos = player.transform.position;
                        position = new Vector3(pos.x, pos.y, pos.z);
                        return true;
                    }
                }
                catch { }
            }

            return false;
        }

        /// <summary>
        /// Limpia el cache del sistema
        /// </summary>
        public static void ClearCache()
        {
            _lastAnnouncedTile = null;
            _lastAnnouncedPosition = new Vector3(float.MinValue, 0, float.MinValue);
        }
    }
}
