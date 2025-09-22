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
        /// Simula click izquierdo en la posición del cursor virtual.
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

                // Usar el nuevo sistema de parches para simular la acción
                var worldPos = new float3(_currentPosition.x, _currentPosition.y, _currentPosition.z);
                SendClientInputSystemPatch.SetVirtualCursorPrimaryAction(worldPos);

                // Removed verbose debug message to avoid spam when holding keys
            }
            catch (System.Exception ex)
            {
                UIManager.Speak(LocalizationManager.GetText("primary_action_error", ex.Message));
            }
        }

        /// <summary>
        /// Acción secundaria en la posición del cursor (tecla O).
        /// Simula click derecho en la posición del cursor virtual para usar objetos equipados.
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

                // Verificar si se está usando un item colocable
                // Por ahora, usamos un enfoque más simple basado en patrones de comportamiento
                // En el futuro se puede mejorar con detección de items específica

                // Solo anunciar posición para items que probablemente sean colocables
                // Esto se hace de manera conservadora para evitar spam de TTS
                int tileX = (int)math.round(_currentPosition.x);
                int tileZ = (int)math.round(_currentPosition.z);

                // Nota: Por ahora usamos un enfoque silencioso por defecto
                // Solo anunciará si realmente se coloca algo (lo que detectará el juego)

                // Usar el nuevo sistema de parches para simular la acción secundaria (usar objetos)
                var worldPos = new float3(_currentPosition.x, _currentPosition.y, _currentPosition.z);
                SendClientInputSystemPatch.SetVirtualCursorSecondaryAction(worldPos);

                // Programar actualización después de la acción
                SchedulePositionUpdate();

                // Removed verbose debug message to avoid spam when holding keys
            }
            catch (System.Exception ex)
            {
                UIManager.Speak(LocalizationManager.GetText("secondary_action_error", ex.Message));
            }
        }

        /// <summary>
        /// Acción de interacción en la posición del cursor (equivalente a tecla E).
        /// Interactúa con objetos, cofres, puertas, etc.
        /// </summary>
        public static void InteractionAction()
        {
            if (!_isInitialized)
            {
                Initialize();
                return;
            }

            try
            {
                // Usar el nuevo sistema de parches para simular la interacción
                var worldPos = new float3(_currentPosition.x, _currentPosition.y, _currentPosition.z);
                SendClientInputSystemPatch.SetVirtualCursorInteraction(worldPos);
            }
            catch (System.Exception ex)
            {
                UIManager.Speak(LocalizationManager.GetText("interaction_error", ex.Message));
            }
        }

        /// <summary>
        /// Detiene la acción primaria (cuando se suelta la tecla U)
        /// </summary>
        public static void StopPrimaryAction()
        {
            try
            {
                SendClientInputSystemPatch.StopVirtualCursorAction();
                PlayerInputPatch.StopAllSimulations();
            }
            catch (System.Exception ex)
            {
                UIManager.Speak($"Error deteniendo acción primaria: {ex.Message}");
            }
        }

        /// <summary>
        /// Detiene la acción secundaria (cuando se suelta la tecla O)
        /// </summary>
        public static void StopSecondaryAction()
        {
            try
            {
                SendClientInputSystemPatch.StopVirtualCursorAction();
                PlayerInputPatch.StopAllSimulations();
            }
            catch (System.Exception ex)
            {
                UIManager.Speak($"Error deteniendo acción secundaria: {ex.Message}");
            }
        }

        /// <summary>
        /// Detiene la interacción (cuando se suelta la tecla E)
        /// </summary>
        public static void StopInteractionAction()
        {
            try
            {
                SendClientInputSystemPatch.StopVirtualCursorAction();
                PlayerInputPatch.StopAllSimulations();
            }
            catch (System.Exception ex)
            {
                UIManager.Speak($"Error deteniendo interacción: {ex.Message}");
            }
        }

        #endregion

        #region Funciones auxiliares para simulación de clicks

        /// <summary>
        /// Convierte una posición del mundo a coordenadas de pantalla para el mouse
        /// </summary>
        private static Vector3 WorldToScreenPosition(Vector3 worldPosition)
        {
            try
            {
                var camera = PugOther.Manager.camera?.gameCamera;
                if (camera != null)
                {
                    // Convertir posición del mundo a coordenadas de pantalla
                    var screenPos = camera.WorldToScreenPoint(worldPosition);
                    return screenPos;
                }
                return Vector3.zero;
            }
            catch (System.Exception ex)
            {
                UIManager.Speak($"Error convirtiendo coordenadas: {ex.Message}");
                return Vector3.zero;
            }
        }


        /// <summary>
        /// Programa la restauración de la posición del mouse
        /// </summary>
        private static void ScheduleMouseRestore(PugOther.UIMouse uiMouse, Vector3 originalPosition)
        {
            var timer = new System.Timers.Timer(50); // 50ms después
            timer.Elapsed += (sender, e) =>
            {
                try
                {
                    if (uiMouse?.pointer != null)
                    {
                        uiMouse.pointer.position = originalPosition;
                    }
                    timer.Dispose();
                }
                catch (System.Exception ex)
                {
                    UIManager.Speak($"Error restaurando mouse: {ex.Message}");
                }
            };
            timer.AutoReset = false;
            timer.Start();
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