extern alias PugOther;
extern alias Core;

using HarmonyLib;
using DavyKager;
using ckAccess.MapReader;
using ckAccess.Helpers;
using ckAccess.Localization;
using UnityEngine;
using PugTilemap;
using Unity.Mathematics;
using Vector3 = Core::UnityEngine.Vector3;

namespace ckAccess.Patches.Player
{
    /// <summary>
    /// Sistema que anuncia cuando el jugador entra en un nuevo bioma/tileset.
    /// Solo anuncia cuando el bioma cambia, evitando repeticiones innecesarias.
    /// </summary>
    [HarmonyPatch]
    public static class BiomeAnnouncerPatch
    {
        // Cache del último bioma anunciado
        private static int _lastAnnouncedTileset = -1;
        private static string _lastAnnouncedBiomeName = null;

        // Control de tiempo para evitar spam
        private static float _lastAnnounceTime = 0f;
        private const float MIN_ANNOUNCE_INTERVAL = 0.5f; // Mínimo 500ms entre anuncios

        // Umbral de movimiento para detectar que el jugador se movió
        private const float MOVEMENT_THRESHOLD = 0.3f;
        private static Vector3 _lastCheckedPosition = Vector3.zero;

        /// <summary>
        /// Parche en PlayerController.ManagedUpdate para detectar cambios de bioma.
        /// MULTIPLAYER-SAFE: Solo procesa el jugador local.
        /// </summary>
        [HarmonyPatch(typeof(PugOther.PlayerController), "ManagedUpdate")]
        [HarmonyPostfix]
        public static void PlayerController_ManagedUpdate_Postfix(PugOther.PlayerController __instance)
        {
            try
            {
                // MULTIPLAYER: Solo procesar si este es el jugador local
                if (!LocalPlayerHelper.IsLocalPlayer(__instance))
                    return;

                // NO anunciar biomas si hay algún menú/UI abierto
                if (!GameplayStateHelper.IsInGameplayWithoutInventory())
                    return;

                // Verificar cooldown de tiempo
                if (Time.time - _lastAnnounceTime < MIN_ANNOUNCE_INTERVAL)
                    return;

                // Obtener posición actual del jugador local
                if (!LocalPlayerHelper.TryGetLocalPlayerPosition(out Vector3 playerPos))
                    return;

                // Inicializar la posición previa si es la primera vez
                if (_lastCheckedPosition == Vector3.zero)
                {
                    _lastCheckedPosition = playerPos;
                    // Inicializar el bioma actual sin anunciar
                    InitializeCurrentBiome(playerPos);
                    return;
                }

                // Solo verificar bioma si el jugador se movió suficiente
                Vector3 movement = playerPos - _lastCheckedPosition;
                float movementDistance = new Vector3(movement.x, 0, movement.z).magnitude;

                if (movementDistance < MOVEMENT_THRESHOLD)
                    return;

                // Actualizar posición de referencia
                _lastCheckedPosition = playerPos;

                // Obtener el tileset actual bajo el jugador
                int currentTileset = GetTilesetAtPosition(playerPos);

                // Si no pudimos leer el tileset, salir
                if (currentTileset < 0)
                    return;

                // Si el bioma cambió, anunciarlo
                if (currentTileset != _lastAnnouncedTileset)
                {
                    AnnounceBiomeChange(currentTileset);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BiomeAnnouncer] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Inicializa el bioma actual sin anunciar (para la primera carga).
        /// </summary>
        private static void InitializeCurrentBiome(Vector3 playerPos)
        {
            try
            {
                int currentTileset = GetTilesetAtPosition(playerPos);
                if (currentTileset >= 0)
                {
                    _lastAnnouncedTileset = currentTileset;
                    _lastAnnouncedBiomeName = TilesetHelper.GetLocalizedName(currentTileset);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BiomeAnnouncer] Error initializing biome: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene el tileset del tile en la posición dada.
        /// </summary>
        private static int GetTilesetAtPosition(Vector3 worldPosition)
        {
            try
            {
                // Copiar a variable local para evitar warning Harmony003
                var pos = worldPosition;

                var multiMap = PugOther.Manager.multiMap;
                if (multiMap == null)
                    return -1;

                var tileLayerLookup = multiMap.GetTileLayerLookup();
                var position = new int2(
                    Mathf.RoundToInt(pos.x),
                    Mathf.RoundToInt(pos.z)
                );

                var topTile = tileLayerLookup.GetTopTile(position);

                // Retornar el tileset del tile actual
                return topTile.tileset;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BiomeAnnouncer] Error reading tileset: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Anuncia el cambio de bioma al jugador.
        /// </summary>
        private static void AnnounceBiomeChange(int newTileset)
        {
            try
            {
                string biomeName = TilesetHelper.GetLocalizedName(newTileset);

                // Evitar anunciar si el nombre es genérico ("Material X")
                if (string.IsNullOrEmpty(biomeName) || biomeName.StartsWith("Material "))
                {
                    // Aún así actualizar el cache para no re-intentar
                    _lastAnnouncedTileset = newTileset;
                    return;
                }

                // Crear mensaje localizado
                string message = LocalizationManager.GetText("biome_entered", biomeName);

                // Anunciar
                Tolk.Output(message, true); // Interrupt = true para anuncio inmediato

                // Actualizar cache
                _lastAnnouncedTileset = newTileset;
                _lastAnnouncedBiomeName = biomeName;
                _lastAnnounceTime = Time.time;

                Debug.Log($"[BiomeAnnouncer] Announced biome change: {biomeName} (tileset {newTileset})");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BiomeAnnouncer] Error announcing biome: {ex.Message}");
            }
        }

        /// <summary>
        /// Limpia el cache del sistema (útil al cambiar de mundo).
        /// </summary>
        public static void ClearCache()
        {
            _lastAnnouncedTileset = -1;
            _lastAnnouncedBiomeName = null;
            _lastCheckedPosition = Vector3.zero;
        }

        /// <summary>
        /// Obtiene el nombre del bioma actual (útil para otros sistemas).
        /// </summary>
        public static string GetCurrentBiomeName()
        {
            return _lastAnnouncedBiomeName ?? LocalizationManager.GetText("unknown");
        }

        /// <summary>
        /// Obtiene el índice del tileset actual.
        /// </summary>
        public static int GetCurrentTilesetIndex()
        {
            return _lastAnnouncedTileset;
        }
    }
}
