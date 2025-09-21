extern alias PugOther;
extern alias Core;
using ckAccess.Patches.UI;
using ckAccess.MapReader;
using Unity.Mathematics;
using Vector3 = Core::UnityEngine.Vector3;
using Mathf = Core::UnityEngine.Mathf;

namespace ckAccess.VirtualCursor
{
    /// <summary>
    /// Sistema de cursor virtual optimizado para accesibilidad en Core Keeper.
    /// Refactorizado para máximo rendimiento y código limpio.
    /// </summary>
    public static class VirtualCursor
    {
        #region State & Configuration

        private static Vector3 _currentPosition;
        private static bool _isInitialized = false;
        // Sistema de prioridades simplificado - sin niveles de detalle

        // Constants
        private const float TILE_SIZE = 1.0f;
        private const float MAX_RANGE = 20f; // Reducido a 20 tiles máximo

        // Public Properties
        public static Vector3 CurrentPosition => _currentPosition;
        public static bool IsInitialized => _isInitialized;

        #endregion

        #region Core Movement & Actions

        /// <summary>
        /// Inicializa el cursor en la posición del jugador.
        /// </summary>
        public static void Initialize()
        {
            if (TryGetPlayerPosition(out Vector3 playerPos))
            {
                _currentPosition = SnapToTileGrid(playerPos);
                _isInitialized = true;
                UIManager.Speak("Cursor virtual inicializado");
                AnnounceTileAtPosition(_currentPosition);
            }
            else
            {
                UIManager.Speak("Error inicializando cursor");
            }
        }

        /// <summary>
        /// Resetea el cursor a la posición del jugador (tecla R).
        /// </summary>
        public static void ResetToPlayer()
        {
            if (TryGetPlayerPosition(out Vector3 playerPos))
            {
                _currentPosition = SnapToTileGrid(playerPos);
                _isInitialized = true;

                int tileX = (int)math.round(_currentPosition.x);
                int tileZ = (int)math.round(_currentPosition.z);
                UIManager.Speak($"Cursor reseteado: x={tileX}, z={tileZ}");
                AnnounceTileAtPosition(_currentPosition);
            }
            else
            {
                UIManager.Speak("Error reseteando cursor");
            }
        }

        /// <summary>
        /// Mueve el cursor en la dirección especificada (I, J, K, L).
        /// </summary>
        public static void MoveCursor(CursorDirection direction)
        {
            if (!_isInitialized)
            {
                Initialize();
                return;
            }

            Vector3 newPosition = CalculateNewPosition(direction);

            if (IsPositionWithinBounds(newPosition))
            {
                _currentPosition = newPosition;
                AnnounceTileAtPosition(_currentPosition);
            }
            else
            {
                UIManager.Speak("Límite alcanzado");
            }
        }

        /// <summary>
        /// Acción primaria en la posición del cursor (tecla U).
        /// </summary>
        public static void PrimaryAction()
        {
            if (!_isInitialized)
            {
                Initialize();
                return;
            }

            UIManager.Speak("Acción primaria");
            // TODO: Implementar click izquierdo simulado
        }

        /// <summary>
        /// Acción secundaria en la posición del cursor (tecla O).
        /// </summary>
        public static void SecondaryAction()
        {
            if (!_isInitialized)
            {
                Initialize();
                return;
            }

            UIManager.Speak("Acción secundaria");
            // TODO: Implementar click derecho simulado
        }

        #endregion

        // INFORMACIÓN DETALLADA ELIMINADA - Ya no necesaria con SimpleWorldReader

        #region Debug & Utility Functions

        /// <summary>
        /// Información detallada de posición (tecla P).
        /// </summary>
        public static void DebugCurrentPosition()
        {
            if (!_isInitialized)
            {
                UIManager.Speak("Cursor no inicializado");
                return;
            }

            int cursorX = (int)math.round(_currentPosition.x);
            int cursorZ = (int)math.round(_currentPosition.z);

            if (TryGetPlayerPosition(out Vector3 playerPos))
            {
                int playerX = (int)math.round(playerPos.x);
                int playerZ = (int)math.round(playerPos.z);
                int relativeX = cursorX - playerX;
                int relativeZ = cursorZ - playerZ;

                UIManager.Speak($"Cursor: x={cursorX}, z={cursorZ} | Relativo: x{relativeX:+0;-0;+0}, z{relativeZ:+0;-0;+0}");
            }
            else
            {
                UIManager.Speak($"Cursor: x={cursorX}, z={cursorZ}");
            }
        }

        /// <summary>
        /// Anuncia la posición del jugador con sistema de prioridades (tecla M).
        /// </summary>
        public static void AnnouncePlayerPositionDetailed()
        {
            try
            {
                EnhancedWorldMapReaderIntegration.AnnouncePlayerPosition();
            }
            catch
            {
                UIManager.Speak("Error leyendo posición del jugador");
            }
        }

        /// <summary>
        /// Prueba de mapeo de coordenadas (tecla T).
        /// </summary>
        public static void TestCoordinateMapping()
        {
            if (!_isInitialized)
            {
                Initialize();
                return;
            }

            int currentX = (int)math.round(_currentPosition.x);
            int currentZ = (int)math.round(_currentPosition.z);
            UIManager.Speak($"Posición actual: x={currentX}, z={currentZ}");
        }

        #endregion

        #region Private Core Methods

        /// <summary>
        /// Obtiene la posición del jugador de forma optimizada.
        /// </summary>
        private static bool TryGetPlayerPosition(out Vector3 position)
        {
            position = Vector3.zero;

            try
            {
                var player = PugOther.Manager.main?.player;
                if (player == null) return false;

                // Método optimizado: probar WorldPosition primero
                try
                {
                    var worldPos = player.WorldPosition;
                    position = new Vector3(worldPos.x, worldPos.y, worldPos.z);
                    return true;
                }
                catch
                {
                    // Fallback a RenderPosition
                    try
                    {
                        var renderPos = player.RenderPosition;
                        position = new Vector3(renderPos.x, renderPos.y, renderPos.z);
                        return true;
                    }
                    catch
                    {
                        // Último fallback: transform
                        var transform = player.transform;
                        if (transform != null)
                        {
                            position = transform.position;
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // Fallback silencioso
            }

            return false;
        }

        /// <summary>
        /// Ajusta posición al grid de tiles.
        /// </summary>
        private static Vector3 SnapToTileGrid(Vector3 worldPosition)
        {
            return new Vector3(
                Mathf.Round(worldPosition.x),
                worldPosition.y,
                Mathf.Round(worldPosition.z)
            );
        }

        /// <summary>
        /// Calcula nueva posición basada en dirección.
        /// </summary>
        private static Vector3 CalculateNewPosition(CursorDirection direction)
        {
            return direction switch
            {
                CursorDirection.Up => _currentPosition + new Vector3(0, 0, TILE_SIZE),
                CursorDirection.Down => _currentPosition + new Vector3(0, 0, -TILE_SIZE),
                CursorDirection.Left => _currentPosition + new Vector3(-TILE_SIZE, 0, 0),
                CursorDirection.Right => _currentPosition + new Vector3(TILE_SIZE, 0, 0),
                _ => _currentPosition
            };
        }

        /// <summary>
        /// Verifica si la posición está dentro de los límites permitidos.
        /// </summary>
        private static bool IsPositionWithinBounds(Vector3 position)
        {
            if (!TryGetPlayerPosition(out Vector3 playerPos))
                return false;

            float distance = Vector3.Distance(position, playerPos);
            return distance <= MAX_RANGE;
        }

        /// <summary>
        /// Anuncia lo que hay en la posición usando el sistema simple y directo.
        /// </summary>
        private static void AnnounceTileAtPosition(Vector3 position)
        {
            try
            {
                // Sistema simple y directo - convertir Vector3
                var unityPos = new UnityEngine.Vector3(position.x, position.y, position.z);
                SimpleWorldReader.AnnouncePosition(unityPos);
            }
            catch
            {
                // Fallback básico en caso de error
                int tileX = (int)math.round(position.x);
                int tileZ = (int)math.round(position.z);
                UIManager.Speak($"Posición x={tileX}, z={tileZ}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Direcciones de movimiento del cursor.
    /// </summary>
    public enum CursorDirection
    {
        Up,
        Down,
        Left,
        Right
    }
}