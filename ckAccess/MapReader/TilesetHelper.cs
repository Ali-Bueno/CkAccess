extern alias PugOther;

using System.Collections.Generic;
using ckAccess.Localization;

namespace ckAccess.MapReader
{
    /// <summary>
    /// Helper OPTIMIZADO para obtener nombres de tilesets con caché de rendimiento.
    /// </summary>
    public static class TilesetHelper
    {
        /// <summary>
        /// Caché de nombres de tilesets para optimizar rendimiento.
        /// CLAVE: (tilesetIndex, languageCode) para soportar cambios de idioma en tiempo real.
        /// </summary>
        private static readonly Dictionary<string, string> _nameCache = new Dictionary<string, string>();

        /// <summary>
        /// Idioma del último caché para detectar cambios de idioma.
        /// </summary>
        private static string _lastLanguage = null;

        /// <summary>
        /// Obtiene el nombre localizado de un tileset usando el sistema del juego.
        /// </summary>
        /// <param name="tilesetIndex">Índice del tileset</param>
        /// <returns>Nombre localizado del tileset</returns>
        public static string GetLocalizedName(int tilesetIndex)
        {
            // Detectar cambio de idioma y limpiar caché si es necesario
            string currentLanguage = LocalizationManager.GetText("tile_ground"); // Usar una clave conocida como indicador
            if (_lastLanguage != null && _lastLanguage != currentLanguage)
            {
                System.Console.WriteLine($"[TilesetHelper] Language changed from '{_lastLanguage}' to '{currentLanguage}', clearing cache");
                _nameCache.Clear();
            }
            _lastLanguage = currentLanguage;

            // OPTIMIZACIÓN: Verificar caché primero (ahora consciente del idioma)
            string cacheKey = $"{tilesetIndex}_{currentLanguage}";
            if (_nameCache.TryGetValue(cacheKey, out var cachedName))
            {
                return cachedName;
            }

            string result;
            try
            {
                // PRIORIDAD 1: Usar nuestro LocalizationManager (más confiable)
                result = GetFallbackLocalizedName(tilesetIndex);
                System.Console.WriteLine($"[TilesetHelper] LocalizationManager returned: '{result}' for tileset {tilesetIndex}");

                // Si nuestro sistema no tiene el tileset, usar el sistema nativo como fallback
                if (result.StartsWith("Material "))
                {
                    var friendlyName = GetTilesetFriendlyName(tilesetIndex);
                    if (!string.IsNullOrEmpty(friendlyName))
                    {
                        System.Console.WriteLine($"[TilesetHelper] Using game's friendly name: '{friendlyName}'");
                        result = friendlyName;
                    }
                    else
                    {
                        var localizedName = GetLocalizedTilesetName(tilesetIndex);
                        if (!string.IsNullOrEmpty(localizedName))
                        {
                            System.Console.WriteLine($"[TilesetHelper] Using game's localized name: '{localizedName}'");
                            result = localizedName;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[TilesetHelper] Exception: {ex.Message}");
                // Fallback silencioso
                result = GetFallbackLocalizedName(tilesetIndex);
            }

            // OPTIMIZACIÓN: Cachear resultado para futuras consultas (con idioma incluido en la clave)
            // Reutilizar la variable cacheKey ya declarada arriba
            _nameCache[cacheKey] = result;
            return result;
        }

        /// <summary>
        /// OBSOLETO: Usar GetLocalizedName() en su lugar.
        /// </summary>
        public static string GetSpanishName(int tilesetIndex)
        {
            return GetLocalizedName(tilesetIndex);
        }

        /// <summary>
        /// Caché estático para reflexión costosa.
        /// </summary>
        private static System.Type _tilesetUtilityType;
        private static System.Reflection.MethodInfo _getFriendlyNameMethod;
        private static bool _reflectionInitialized = false;

        /// <summary>
        /// Obtiene el nombre del tileset usando TilesetTypeUtility.GetFriendlyName() del juego.
        /// ULTRA OPTIMIZADO: Caché de reflexión para evitar llamadas costosas.
        /// </summary>
        private static string GetTilesetFriendlyName(int tilesetIndex)
        {
            try
            {
                // OPTIMIZACIÓN: Inicializar reflexión solo una vez
                if (!_reflectionInitialized)
                {
                    _tilesetUtilityType = System.Type.GetType("PugTilemap.TilesetTypeUtility, Assembly-CSharp");
                    if (_tilesetUtilityType != null)
                    {
                        _getFriendlyNameMethod = _tilesetUtilityType.GetMethod("GetFriendlyName",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    }
                    _reflectionInitialized = true;
                }

                if (_getFriendlyNameMethod == null) return null;

                var friendlyName = (string)_getFriendlyNameMethod.Invoke(null, new object[] { tilesetIndex });
                return friendlyName;
            }
            catch (System.Exception)
            {
                // Fallback silencioso
                return null;
            }
        }

        /// <summary>
        /// Obtiene el nombre localizado usando el sistema de localización del juego.
        /// </summary>
        /// <summary>
        /// Caché para PugGlossary para evitar FindObjectOfType costoso.
        /// </summary>
        private static System.Type _glossaryType;
        private static object _glossaryInstance;
        private static System.Reflection.MethodInfo _glossaryGetMethod;
        private static bool _glossaryInitialized = false;

        private static string GetLocalizedTilesetName(int tilesetIndex)
        {
            try
            {
                // OPTIMIZACIÓN: Inicializar PugGlossary solo una vez
                if (!_glossaryInitialized)
                {
                    _glossaryType = System.Type.GetType("PugGlossary, Pug.Other");
                    if (_glossaryType != null)
                    {
                        _glossaryInstance = UnityEngine.Object.FindObjectOfType(_glossaryType);
                        if (_glossaryInstance != null)
                        {
                            _glossaryGetMethod = _glossaryType.GetMethod("Get", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        }
                    }
                    _glossaryInitialized = true;
                }

                if (_glossaryGetMethod == null || _glossaryInstance == null) return null;

                // Intentar diferentes patrones de claves de localización para tilesets
                string[] possibleKeys = {
                    $"Tilesets/{tilesetIndex}",
                    $"Tileset/{tilesetIndex}",
                    $"tilesets/{tilesetIndex}",
                    $"tileset/{tilesetIndex}",
                    $"Biomes/{tilesetIndex}",
                    $"Biome/{tilesetIndex}",
                    tilesetIndex.ToString()
                };

                foreach (var key in possibleKeys)
                {
                    try
                    {
                        var localizedText = (string)_glossaryGetMethod.Invoke(_glossaryInstance, new object[] { key });
                        if (!string.IsNullOrEmpty(localizedText) && !localizedText.StartsWith("M`i`s`s`i`n`g"))
                            return localizedText;
                    }
                    catch (System.Exception)
                    {
                        continue;
                    }
                }
            }
            catch (System.Exception)
            {
                // Fallback silencioso
            }

            return null;
        }

        /// <summary>
        /// Traduce nombres de tilesets conocidos del inglés al español.
        /// </summary>
        private static string TranslateToSpanish(string englishName)
        {
            return englishName?.ToLower() switch
            {
                "basic" or "default" => "Básico",
                "stone" => "Piedra",
                "wood" or "forest" => "Bosque",
                "desert" or "sand" => "Desierto",
                "ocean" or "water" => "Océano",
                "cave" or "cavern" => "Caverna",
                "lava" or "magma" => "Lava",
                "crystal" => "Cristal",
                "ancient" => "Antiguo",
                "corrupt" or "corruption" => "Corrupto",
                "celestial" or "heaven" => "Celestial",
                "ice" or "frozen" => "Hielo",
                "swamp" or "marsh" => "Pantano",
                "volcanic" => "Volcánico",
                "underground" => "Subterráneo",
                _ => englishName ?? "Desconocido"
            };
        }

        /// <summary>
        /// Verifica si un tileset es especial/único.
        /// </summary>
        public static bool IsSpecialTileset(int tilesetIndex)
        {
            return tilesetIndex switch
            {
                >= 6 and <= 10 => true, // Tilesets especiales
                _ => false
            };
        }

        /// <summary>
        /// Obtiene la prioridad visual de un tileset para renderizado.
        /// </summary>
        public static int GetRenderPriority(int tilesetIndex)
        {
            return tilesetIndex switch
            {
                0 => 0,  // Básico - menor prioridad
                1 => 1,  // Piedra
                2 => 1,  // Bosque
                3 => 1,  // Desierto
                4 => 1,  // Océano
                5 => 2,  // Caverna
                >= 6 => 3, // Tilesets especiales - mayor prioridad
                _ => 0
            };
        }

        /// <summary>
        /// Obtiene el nombre fallback usando LocalizationManager.
        /// Mapeo completo basado en PugTilemap.Tileset del juego.
        /// </summary>
        private static string GetFallbackLocalizedName(int tilesetIndex)
        {
            string tilesetKey = tilesetIndex switch
            {
                // Tilesets básicos
                0 => "tileset_dirt",
                1 => "tileset_stone",
                2 => "tileset_obsidian",
                3 => "tileset_lava",
                4 => "tileset_extras",
                5 => "tileset_base_building_wood",
                6 => "tileset_larva_hive",
                7 => "tileset_base_building_stone",
                8 => "tileset_nature",
                9 => "tileset_mold",
                10 => "tileset_sea",
                11 => "tileset_clay",
                12 => "tileset_sand",
                13 => "tileset_turf",

                // Construcciones coloreadas
                14 => "tileset_base_building_unpainted",
                15 => "tileset_base_building_yellow",
                16 => "tileset_base_building_green",
                17 => "tileset_base_building_red",
                18 => "tileset_base_building_purple",
                19 => "tileset_base_building_blue",
                20 => "tileset_base_building_brown",
                21 => "tileset_base_building_white",
                22 => "tileset_base_building_black",
                23 => "tileset_base_building_scarlet",
                25 => "tileset_base_building_coral",
                28 => "tileset_base_building_galaxite",
                30 => "tileset_base_building_metal",
                32 => "tileset_base_building_valentine",
                33 => "tileset_base_building_easter",

                // Biomas especiales
                24 => "tileset_city",
                26 => "tileset_desert",
                27 => "tileset_desert_temple",
                29 => "tileset_desert_temple2",
                31 => "tileset_snow",
                34 => "tileset_glass",
                35 => "tileset_meadow",
                36 => "tileset_explosive",
                59 => "tileset_dark_stone",
                60 => "tileset_crystal",
                61 => "tileset_alien",
                71 => "tileset_oasis",
                72 => "tileset_explosive_forest",
                73 => "tileset_explosive_desert",

                _ => null
            };

            if (tilesetKey != null)
            {
                return LocalizationManager.GetText(tilesetKey);
            }

            // Último fallback
            return $"Material {tilesetIndex}";
        }
    }
}