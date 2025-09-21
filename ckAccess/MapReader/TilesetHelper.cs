extern alias PugOther;

using System.Collections.Generic;

namespace ckAccess.MapReader
{
    /// <summary>
    /// Helper OPTIMIZADO para obtener nombres de tilesets con caché de rendimiento.
    /// </summary>
    public static class TilesetHelper
    {
        /// <summary>
        /// Caché de nombres de tilesets para optimizar rendimiento.
        /// </summary>
        private static readonly Dictionary<int, string> _nameCache = new Dictionary<int, string>();
        /// <summary>
        /// Mapeo REAL basado en el enum PugTilemap.Tileset del juego (ck code1/PugTilemap/Tileset.cs).
        /// </summary>
        private static readonly Dictionary<int, string> TilesetToMaterialMap = new Dictionary<int, string>
        {
            // Enum real de PugTilemap.Tileset
            { 0, "Tierra" },                  // Dirt
            { 1, "Piedra" },                  // Stone
            { 2, "Obsidiana" },               // Obsidian
            { 3, "Lava" },                    // Lava
            { 4, "Extras" },                  // Extras
            { 5, "Madera de construcción" },  // BaseBuildingWood
            { 6, "Panal de larvas" },         // LarvaHive
            { 7, "Piedra de construcción" },  // BaseBuildingStone
            { 8, "Naturaleza" },              // Nature
            { 9, "Moho" },                    // Mold
            { 10, "Mar" },                    // Sea
            { 11, "Arcilla" },                // Clay
            { 12, "Arena" },                  // Sand ← ¡Esta es la clave!
            { 13, "Césped" },                 // Turf
            { 14, "Construcción sin pintar" }, // BaseBuildingUnpainted
            { 15, "Construcción amarilla" },  // BaseBuildingYellow
            { 16, "Construcción verde" },     // BaseBuildingGreen
            { 17, "Construcción roja" },      // BaseBuildingRed
            { 18, "Construcción púrpura" },   // BaseBuildingPurple
            { 19, "Construcción azul" },      // BaseBuildingBlue
            { 20, "Construcción marrón" },    // BaseBuildingBrown
            { 21, "Construcción blanca" },    // BaseBuildingWhite
            { 22, "Construcción negra" },     // BaseBuildingBlack
            { 23, "Construcción escarlata" }, // BaseBuildingScarlet
            { 24, "Ciudad" },                 // City
            { 25, "Construcción coral" },     // BaseBuildingCoral
            { 26, "Desierto" },               // Desert
            { 27, "Templo del desierto" },    // DesertTemple
            { 28, "Construcción galaxita" },  // BaseBuildingGalaxite
            { 29, "Templo del desierto 2" },  // DesertTemple2
            { 30, "Construcción metálica" },  // BaseBuildingMetal
            { 31, "Nieve" },                  // Snow
            { 32, "Construcción San Valentín" }, // BaseBuildingValentine
            { 33, "Construcción Pascua" },    // BaseBuildingEaster
            { 34, "Cristal" },                // Glass
            { 35, "Pradera" },                // Meadow
            { 36, "Explosivo" },              // Explosive
            { 59, "Piedra oscura" },          // DarkStone
            { 60, "Cristal" },                // Crystal
            { 61, "Alienígena" },             // Alien
            { 71, "Oasis" },                  // Oasis ← Correcto número!
            { 72, "Bosque explosivo" },       // ExplosiveForest
            { 73, "Desierto explosivo" }      // ExplosiveDesert
        };

        /// <summary>
        /// Obtiene el nombre localizado de un tileset usando el sistema del juego.
        /// </summary>
        /// <param name="tilesetIndex">Índice del tileset</param>
        /// <returns>Nombre localizado del tileset</returns>
        public static string GetLocalizedName(int tilesetIndex)
        {
            // OPTIMIZACIÓN: Verificar caché primero
            if (_nameCache.TryGetValue(tilesetIndex, out var cachedName))
            {
                return cachedName;
            }

            string result;
            try
            {
                // PRIORIDAD 1: Usar el sistema nativo del juego - TilesetTypeUtility.GetFriendlyName()
                var friendlyName = GetTilesetFriendlyName(tilesetIndex);
                if (!string.IsNullOrEmpty(friendlyName))
                {
                    result = friendlyName;
                }
                else
                {
                    // PRIORIDAD 2: Intentar obtener nombre localizado del PugGlossary
                    var localizedName = GetLocalizedTilesetName(tilesetIndex);
                    if (!string.IsNullOrEmpty(localizedName))
                    {
                        result = localizedName;
                    }
                    else
                    {
                        // FALLBACK: Usar mapeo directo de tilesets
                        result = TilesetToMaterialMap.TryGetValue(tilesetIndex, out var name)
                            ? name
                            : $"Material {tilesetIndex}";
                    }
                }
            }
            catch (System.Exception)
            {
                // Fallback silencioso
                result = TilesetToMaterialMap.TryGetValue(tilesetIndex, out var name)
                    ? name
                    : $"Material {tilesetIndex}";
            }

            // OPTIMIZACIÓN: Cachear resultado para futuras consultas
            _nameCache[tilesetIndex] = result;
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
    }
}