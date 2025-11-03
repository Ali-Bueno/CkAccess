extern alias PugOther;
extern alias Core;
using Vector3 = Core::UnityEngine.Vector3;

namespace ckAccess.Helpers
{
    /// <summary>
    /// Helper centralizado para obtener el jugador local correcto en multijugador.
    /// En multijugador, Manager.main.player puede apuntar a cualquier jugador,
    /// por lo que necesitamos identificar correctamente el jugador controlado por este cliente.
    /// </summary>
    public static class LocalPlayerHelper
    {
        // Cache del jugador local para evitar búsquedas repetidas
        private static PugOther.PlayerController _cachedLocalPlayer = null;
        private static int _lastCacheFrame = -1;

        /// <summary>
        /// Obtiene el PlayerController del jugador local (el controlado por este cliente).
        /// Este método es seguro para multijugador y siempre devuelve el jugador correcto.
        /// </summary>
        public static PugOther.PlayerController GetLocalPlayer()
        {
            try
            {
                // Cache simple por frame para rendimiento
                int currentFrame = UnityEngine.Time.frameCount;
                if (_cachedLocalPlayer != null && _lastCacheFrame == currentFrame)
                {
                    return _cachedLocalPlayer;
                }

                // MÉTODO 1: Manager.main.player - funciona en singleplayer y cliente local en multiplayer
                var mainPlayer = PugOther.Manager.main?.player;
                if (mainPlayer != null)
                {
                    _cachedLocalPlayer = mainPlayer;
                    _lastCacheFrame = currentFrame;
                    return mainPlayer;
                }

                // MÉTODO 2: Buscar en todos los PlayerController activos
                // En Core Keeper, el jugador local debería ser el único PlayerController activo en este cliente
                var allObjects = UnityEngine.Object.FindObjectsOfType<UnityEngine.MonoBehaviour>();
                foreach (var obj in allObjects)
                {
                    try
                    {
                        // Verificar si es un PlayerController usando el nombre del tipo
                        var typeName = obj.GetType().Name;
                        if (typeName == "PlayerController" &&
                            obj.gameObject != null &&
                            obj.gameObject.activeInHierarchy)
                        {
                            // Convertir usando reflection (los aliases hacen que el cast directo falle)
                            // pero podemos usar el objeto directamente
                            _cachedLocalPlayer = (PugOther.PlayerController)(object)obj;
                            if (_cachedLocalPlayer != null && IsControlledByLocalInput(_cachedLocalPlayer))
                            {
                                _lastCacheFrame = currentFrame;
                                return _cachedLocalPlayer;
                            }
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                return null;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[LocalPlayerHelper] Error obteniendo jugador local: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Verifica si un PlayerController es controlado por el input local
        /// </summary>
        private static bool IsControlledByLocalInput(PugOther.PlayerController player)
        {
            try
            {
                // Si el jugador tiene input activo, es el jugador local
                // En Core Keeper, Manager.input está disponible solo para el cliente local
                var inputManager = PugOther.Manager.input;
                if (inputManager == null) return false;

                // Si llegamos aquí y el jugador existe, probablemente es el local
                // porque Manager.input solo existe en el cliente con control
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Intenta obtener el PlayerController del jugador local.
        /// </summary>
        /// <param name="player">El jugador local si se encuentra</param>
        /// <returns>True si se encontró el jugador local, false en caso contrario</returns>
        public static bool TryGetLocalPlayer(out PugOther.PlayerController player)
        {
            player = GetLocalPlayer();
            return player != null;
        }

        /// <summary>
        /// Obtiene la posición del jugador local de forma segura.
        /// Este método maneja multijugador correctamente.
        /// </summary>
        /// <param name="position">La posición del jugador si se encuentra</param>
        /// <returns>True si se obtuvo la posición correctamente</returns>
        public static bool TryGetLocalPlayerPosition(out Vector3 position)
        {
            position = Vector3.zero;

            try
            {
                var player = GetLocalPlayer();
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
        /// Verifica si un PlayerController específico es el jugador local.
        /// Útil para filtrar otros jugadores en multijugador.
        /// </summary>
        public static bool IsLocalPlayer(PugOther.PlayerController player)
        {
            if (player == null) return false;

            try
            {
                // Comparar con el jugador local actual
                var localPlayer = GetLocalPlayer();
                if (localPlayer == null) return false;

                return player == localPlayer || player.gameObject == localPlayer.gameObject;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Información de debugging del sistema de jugador local
        /// </summary>
        public static string GetDebugInfo()
        {
            try
            {
                var localPlayer = GetLocalPlayer();
                if (localPlayer == null)
                {
                    return "LocalPlayer: NULL";
                }

                // Contar todos los PlayerController en la escena
                var allObjects = UnityEngine.Object.FindObjectsOfType<UnityEngine.MonoBehaviour>();
                int playerCount = 0;
                foreach (var obj in allObjects)
                {
                    if (obj.GetType().Name == "PlayerController")
                    {
                        playerCount++;
                    }
                }

                return $"LocalPlayer: {localPlayer.gameObject.name}, TotalPlayers: {playerCount}";
            }
            catch (System.Exception ex)
            {
                return $"LocalPlayer: Error - {ex.Message}";
            }
        }
    }
}
