extern alias PugOther;
extern alias Core;
extern alias PugUnExt;
using ckAccess.Patches.UI;
using ckAccess.MapReader;
using ckAccess.Localization;
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
                UIManager.Speak(LocalizationManager.GetText("virtual_cursor_initialized"));
                AnnounceTileAtPosition(_currentPosition);
            }
            else
            {
                UIManager.Speak(LocalizationManager.GetText("cursor_initialization_error"));
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
                UIManager.Speak(LocalizationManager.GetText("cursor_reset", tileX, tileZ));
                AnnounceTileAtPosition(_currentPosition);
            }
            else
            {
                UIManager.Speak(LocalizationManager.GetText("cursor_reset_error"));
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
                UIManager.Speak(LocalizationManager.GetText("limit_reached"));
            }
        }

        /// <summary>
        /// Acción primaria en la posición del cursor (tecla U).
        /// SOLO mueve el cursor nativo del juego - el juego detecta el click automáticamente.
        /// INTEGRADO con auto-targeting para automáticamente apuntar a enemigos cercanos.
        /// </summary>
        public static void PrimaryAction()
        {
            if (!_isInitialized)
            {
                Initialize();
                return;
            }

            try
            {
                // Asegurar que el jugador tenga un slot válido equipado
                EnsureValidSlotEquipped();

                // INTEGRACIÓN AUTO-TARGETING: Verificar si hay enemigos cercanos
                Vector3 targetPosition = _currentPosition;
                var autoTarget = ckAccess.Patches.Player.AutoTargetingPatch.GetCurrentTarget();

                if (autoTarget != null && ckAccess.Patches.Player.AutoTargetingPatch.IsSystemEnabled)
                {
                    // Si hay auto-target activo, usar la posición del enemigo en lugar del cursor
                    var enemyPos = autoTarget.WorldPosition;
                    targetPosition = new Vector3(enemyPos.x, enemyPos.y, enemyPos.z);
                }

                // Simplemente mover el cursor - el juego detecta el click de la tecla U automáticamente
                MoveNativeCursorToPosition(targetPosition);
            }
            catch (System.Exception ex)
            {
                UIManager.Speak(LocalizationManager.GetText("primary_action_error", ex.Message));
            }
        }

        /// <summary>
        /// Acción secundaria en la posición del cursor (tecla O).
        /// SOLO mueve el cursor nativo del juego - el juego detecta el click automáticamente.
        /// </summary>
        public static void SecondaryAction()
        {
            if (!_isInitialized)
            {
                Initialize();
                return;
            }

            try
            {
                // Asegurar que el jugador tenga un slot válido equipado
                EnsureValidSlotEquipped();

                // Simplemente mover el cursor - el juego detecta el click de la tecla O automáticamente
                MoveNativeCursorToPosition(_currentPosition);

                // Programar actualización después de la acción
                SchedulePositionUpdate();
            }
            catch (System.Exception ex)
            {
                UIManager.Speak(LocalizationManager.GetText("secondary_action_error", ex.Message));
            }
        }

        /// <summary>
        /// Ya no es necesario detener nada - el juego maneja todo automáticamente
        /// </summary>
        public static void StopPrimaryAction()
        {
            // No hacer nada - el juego maneja esto automáticamente
        }

        /// <summary>
        /// Ya no es necesario detener nada - el juego maneja todo automáticamente
        /// </summary>
        public static void StopSecondaryAction()
        {
            // No hacer nada - el juego maneja esto automáticamente
        }

        #endregion

        #region Native Cursor Movement

        /// <summary>
        /// Mueve el cursor nativo del juego a la posición del mundo especificada.
        /// Usa el método ToMouseViewSpace del juego para convertir correctamente las coordenadas.
        /// </summary>
        private static void MoveNativeCursorToPosition(Vector3 worldPosition)
        {
            try
            {
                var uiMouse = PugOther.Manager.ui?.mouse;
                if (uiMouse?.pointer == null)
                {
                    return;
                }

                // Convertir posición del mundo a vista usando el método NATIVO del juego
                // Este método ya existe en UIMouse y hace la conversión correcta
                var viewPosition = uiMouse.ToMouseViewSpace(worldPosition);

                // Simplemente asignar la posición - el juego se encarga del resto
                uiMouse.pointer.localPosition = new Vector3(viewPosition.x, viewPosition.y, 0f);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error moving cursor: {ex}");
            }
        }

        /// <summary>
        /// Programa una actualización de la posición después de colocar un objeto
        /// para detectar los cambios inmediatamente
        /// </summary>
        private static void SchedulePositionUpdate()
        {
            var timer = new System.Timers.Timer(100); // 100ms después para que el cambio se registre
            timer.Elapsed += (sender, e) =>
            {
                try
                {
                    if (_isInitialized)
                    {
                        AnnounceTileAtPosition(_currentPosition);
                    }
                    timer.Dispose();
                }
                catch (System.Exception ex)
                {
                    UIManager.Speak($"Error actualizando posición: {ex.Message}");
                }
            };
            timer.AutoReset = false;
            timer.Start();
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
                UIManager.Speak(LocalizationManager.GetText("cursor_not_initialized"));
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
                UIManager.Speak(LocalizationManager.GetText("position_reading_error"));
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
            UIManager.Speak(LocalizationManager.GetText("current_position", currentX, currentZ));
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

        /// <summary>
        /// Verifica si un objeto es colocable (bloques, decoraciones, semillas)
        /// </summary>
        private static bool IsPlaceableItem(ObjectID objectID)
        {
            try
            {
                string name = objectID.ToString().ToLower();
                return name.Contains("seed") || name.Contains("block") || name.Contains("wall") ||
                       name.Contains("torch") || name.Contains("workbench") || name.Contains("furnace") ||
                       name.Contains("chest") || name.Contains("table") || name.Contains("chair") ||
                       name.Contains("door") || name.Contains("bed") || name.Contains("farm") ||
                       name.Contains("floor") || name.Contains("carpet") || name.Contains("bridge") ||
                       name.Contains("fence") || name.Contains("rail") || name.Contains("statue") ||
                       name.Contains("decoration") || name.Contains("plant") || name.Contains("spike") ||
                       name.Contains("trap") || name.Contains("lantern") || name.Contains("crystal");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtiene el nombre de un item usando su ObjectID
        /// </summary>
        private static string GetItemName(ObjectID objectID)
        {
            try
            {
                // Simplificado: usar solo el toString del objectID por ahora
                // En el futuro se puede mejorar con acceso a la base de datos del juego
                return objectID.ToString();
            }
            catch
            {
                return "Unknown Item";
            }
        }

        /// <summary>
        /// Asegura que el jugador tenga un slot válido equipado para las acciones del cursor virtual
        /// </summary>
        private static void EnsureValidSlotEquipped()
        {
            try
            {
                var player = PugOther.Manager.main.player;
                if (player == null) return;

                // Verificación simplificada: si no tiene slot equipado, intentar equipar slot 0
                var currentSlot = player.GetEquippedSlot();
                if (currentSlot == null)
                {
                    // Intentar equipar el slot 0 (primera posición del hotbar)
                    player.EquipSlot(0);
                }

                // TODO: En el futuro se puede mejorar para buscar automáticamente
                // el primer slot no vacío, pero por ahora esto es suficiente
                // para resolver el problema básico de dependencia del ratón
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error in EnsureValidSlotEquipped: {ex}");
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