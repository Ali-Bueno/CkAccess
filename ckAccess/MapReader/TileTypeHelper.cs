extern alias PugOther;

using System.Collections.Generic;
using PugTilemap;

namespace ckAccess.MapReader
{
    /// <summary>
    /// Helper para obtener nombres en español de los tipos de tiles.
    /// </summary>
    public static class TileTypeHelper
    {
        /// <summary>
        /// Diccionario con nombres en español para tipos de tiles.
        /// </summary>
        private static readonly Dictionary<TileType, string> SpanishNames = new Dictionary<TileType, string>
        {
            { TileType.none, "Vacío" },
            { TileType.ground, "Suelo" },
            { TileType.wall, "Pared" },
            { TileType.water, "Agua" },
            { TileType.pit, "Hoyo" },
            { TileType.bridge, "Puente" },
            { TileType.floor, "Suelo construido" },
            { TileType.roofHole, "Agujero en el techo" },
            { TileType.thinWall, "Pared delgada" },
            { TileType.dugUpGround, "Tierra excavada" },
            { TileType.wateredGround, "Tierra regada" },
            { TileType.circuitPlate, "Placa de circuito" },
            { TileType.ancientCircuitPlate, "Placa de circuito antigua" },
            { TileType.fence, "Valla" },
            { TileType.rug, "Alfombra" },
            { TileType.smallStones, "Piedras pequeñas" },
            { TileType.smallGrass, "Hierba pequeña" },
            { TileType.wallGrass, "Hierba de pared" },
            { TileType.debris, "Escombros" },
            { TileType.floorCrack, "Grieta en el suelo" },
            { TileType.rail, "Riel" },
            { TileType.greatWall, "Gran pared" },
            { TileType.litFloor, "Suelo iluminado" },
            { TileType.debris2, "Escombros" },
            { TileType.looseFlooring, "Suelo suelto" },
            { TileType.immune, "Zona inmune" },
            { TileType.wallCrack, "Grieta en la pared" },
            { TileType.ore, "Veta de mineral" },
            { TileType.bigRoot, "Raíz grande" },
            { TileType.groundSlime, "Rastro de slime" },
            { TileType.ancientCrystal, "Cristal antiguo" },
            { TileType.chrysalis, "Crisálida" }
        };

        /// <summary>
        /// Obtiene el nombre localizado de un tipo de tile usando el sistema del juego.
        /// </summary>
        /// <param name="tileType">Tipo de tile</param>
        /// <returns>Nombre localizado</returns>
        public static string GetLocalizedName(TileType tileType)
        {
            try
            {
                // Intentar obtener nombre localizado del sistema del juego
                var localizedName = GetLocalizedTileName(tileType);
                if (!string.IsNullOrEmpty(localizedName))
                    return localizedName;
            }
            catch (System.Exception)
            {
                // Fallback silencioso
            }

            // Fallback al sistema español manual
            return SpanishNames.TryGetValue(tileType, out var name) ? name : tileType.ToString();
        }

        /// <summary>
        /// OBSOLETO: Usar GetLocalizedName() en su lugar.
        /// </summary>
        public static string GetSpanishName(TileType tileType)
        {
            return GetLocalizedName(tileType);
        }

        /// <summary>
        /// Obtiene el nombre localizado usando el sistema de localización del juego.
        /// </summary>
        private static string GetLocalizedTileName(TileType tileType)
        {
            try
            {
                // Usar reflexión para acceder al PugGlossary del juego
                var glossaryType = System.Type.GetType("PugGlossary, Pug.Other");
                if (glossaryType == null) return null;

                var glossaryInstance = UnityEngine.Object.FindObjectOfType(glossaryType);
                if (glossaryInstance == null) return null;

                var getMethod = glossaryType.GetMethod("Get", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (getMethod == null) return null;

                // Intentar diferentes patrones de claves de localización
                string[] possibleKeys = {
                    $"Tiles/{tileType}",
                    $"Tile/{tileType}",
                    $"tiles/{tileType}",
                    $"tile/{tileType}",
                    tileType.ToString()
                };

                foreach (var key in possibleKeys)
                {
                    try
                    {
                        var localizedText = (string)getMethod.Invoke(glossaryInstance, new object[] { key });
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
        /// Verifica si un tile es mineable/destructible.
        /// </summary>
        public static bool IsMineable(TileType tileType)
        {
            return tileType switch
            {
                TileType.wall => true,
                TileType.ore => true,
                TileType.ground => true,
                TileType.dugUpGround => true,
                TileType.debris => true,
                TileType.debris2 => true,
                TileType.bigRoot => true,
                TileType.ancientCrystal => true,
                _ => false
            };
        }

        /// <summary>
        /// Verifica si un tile bloquea el movimiento.
        /// </summary>
        public static bool IsBlocking(TileType tileType)
        {
            return tileType switch
            {
                TileType.wall => true,
                TileType.greatWall => true,
                TileType.thinWall => true,
                TileType.fence => true,
                TileType.ore => true,
                TileType.pit => true,
                TileType.bigRoot => true,
                TileType.ancientCrystal => true,
                _ => false
            };
        }

        /// <summary>
        /// Verifica si un tile es cambiable (se puede colocar algo encima).
        /// </summary>
        public static bool IsWalkable(TileType tileType)
        {
            return tileType switch
            {
                TileType.ground => true,
                TileType.floor => true,
                TileType.litFloor => true,
                TileType.bridge => true,
                TileType.dugUpGround => true,
                TileType.wateredGround => true,
                TileType.circuitPlate => true,
                TileType.ancientCircuitPlate => true,
                TileType.rug => true,
                TileType.rail => true,
                TileType.smallStones => true,
                TileType.smallGrass => true,
                TileType.wallGrass => true,
                TileType.looseFlooring => true,
                _ => false
            };
        }

        /// <summary>
        /// Verifica si un tile es destructible/minable.
        /// </summary>
        public static bool IsDamageable(TileType tileType)
        {
            return tileType switch
            {
                TileType.wall => true,
                TileType.thinWall => true,
                TileType.ore => true,
                TileType.bigRoot => true,
                TileType.ancientCrystal => true,
                TileType.smallStones => true,
                // TileType.bigTiles => true, // No disponible en esta versión
                _ => false
            };
        }

        /// <summary>
        /// Obtiene la dureza/resistencia de un tile.
        /// </summary>
        public static float GetHardness(TileType tileType)
        {
            return tileType switch
            {
                TileType.ground => 0.5f,
                TileType.wall => 2.0f,
                TileType.ore => 3.0f,
                TileType.ancientCrystal => 5.0f,
                TileType.greatWall => 10.0f,
                TileType.bigRoot => 4.0f,
                TileType.thinWall => 1.5f,
                TileType.smallStones => 1.0f,
                _ => 1.0f
            };
        }

        /// <summary>
        /// Obtiene la categoría del material del tile.
        /// </summary>
        public static string GetMaterialCategory(TileType tileType)
        {
            return tileType switch
            {
                TileType.ground => "Tierra",
                TileType.floor => "Construcción",
                TileType.wall => "Piedra",
                TileType.ore => "Mineral",
                TileType.water => "Líquido",
                TileType.bridge => "Construcción",
                TileType.ancientCrystal => "Cristal",
                TileType.bigRoot => "Orgánico",
                // TileType.lava => "Líquido caliente", // No disponible en esta versión
                // TileType.ice => "Hielo", // No disponible en esta versión
                _ => "Desconocido"
            };
        }

        /// <summary>
        /// Verifica si un tile es peligroso.
        /// </summary>
        public static bool IsDangerous(TileType tileType)
        {
            return tileType switch
            {
                // TileType.lava => true, // No disponible en esta versión
                TileType.pit => true,
                TileType.water => true, // Puede ser peligroso para algunos personajes
                _ => false
            };
        }

        /// <summary>
        /// Obtiene el sonido que haría al caminar sobre el tile.
        /// </summary>
        public static string GetFootstepSound(TileType tileType)
        {
            return tileType switch
            {
                TileType.ground => "Tierra",
                TileType.floor => "Piedra",
                TileType.wall => "Piedra",
                TileType.ore => "Metal",
                TileType.water => "Chapoteo",
                TileType.bridge => "Madera",
                TileType.smallStones => "Grava",
                TileType.rug => "Textil",
                TileType.rail => "Metal",
                _ => "Neutral"
            };
        }

        /// <summary>
        /// Verifica si un tile requiere herramientas específicas para ser minado.
        /// </summary>
        public static bool RequiresSpecificTool(TileType tileType)
        {
            return tileType switch
            {
                TileType.ore => true,
                TileType.ancientCrystal => true,
                TileType.wall => true,
                TileType.bigRoot => true,
                _ => false
            };
        }

        /// <summary>
        /// Obtiene la herramienta recomendada para minar el tile.
        /// </summary>
        public static string GetRecommendedTool(TileType tileType)
        {
            return tileType switch
            {
                TileType.ore => "Pico",
                TileType.wall => "Pico",
                TileType.ancientCrystal => "Pico de alta calidad",
                TileType.bigRoot => "Hacha",
                TileType.ground => "Pala",
                TileType.dugUpGround => "Pala",
                TileType.smallStones => "Pico o Pala",
                _ => "Ninguna"
            };
        }
    }
}