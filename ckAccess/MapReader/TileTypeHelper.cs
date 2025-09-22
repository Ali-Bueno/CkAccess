extern alias PugOther;

using System.Collections.Generic;
using PugTilemap;
using ckAccess.Localization;

namespace ckAccess.MapReader
{
    /// <summary>
    /// Helper para obtener nombres localizados de los tipos de tiles.
    /// Ahora usa el sistema de localización universal.
    /// </summary>
    public static class TileTypeHelper
    {
        /// <summary>
        /// Diccionario con claves de localización para tipos de tiles.
        /// </summary>
        private static readonly Dictionary<TileType, string> LocalizationKeys = new Dictionary<TileType, string>
        {
            { TileType.none, "tile_none" },
            { TileType.ground, "tile_ground" },
            { TileType.wall, "tile_wall" },
            { TileType.water, "tile_water" },
            { TileType.pit, "tile_pit" },
            { TileType.bridge, "tile_bridge" },
            { TileType.floor, "tile_floor" },
            { TileType.roofHole, "tile_roof_hole" },
            { TileType.thinWall, "tile_thin_wall" },
            { TileType.dugUpGround, "tile_dug_up_ground" },
            { TileType.wateredGround, "tile_watered_ground" },
            { TileType.circuitPlate, "tile_circuit_plate" },
            { TileType.ancientCircuitPlate, "tile_ancient_circuit_plate" },
            { TileType.fence, "tile_fence" },
            { TileType.rug, "tile_rug" },
            { TileType.smallStones, "tile_small_stones" },
            { TileType.smallGrass, "tile_small_grass" },
            { TileType.wallGrass, "tile_wall_grass" },
            { TileType.debris, "tile_debris" },
            { TileType.floorCrack, "tile_floor_crack" },
            { TileType.rail, "tile_rail" },
            { TileType.greatWall, "tile_great_wall" },
            { TileType.litFloor, "tile_lit_floor" },
            { TileType.debris2, "tile_debris2" },
            { TileType.looseFlooring, "tile_loose_flooring" },
            { TileType.immune, "tile_immune" },
            { TileType.wallCrack, "tile_wall_crack" },
            { TileType.ore, "tile_ore" },
            { TileType.bigRoot, "tile_big_root" },
            { TileType.groundSlime, "tile_ground_slime" },
            { TileType.ancientCrystal, "tile_ancient_crystal" },
            { TileType.chrysalis, "tile_chrysalis" }
        };

        /// <summary>
        /// Obtiene el nombre localizado de un tipo de tile usando el sistema universal.
        /// </summary>
        /// <param name="tileType">Tipo de tile</param>
        /// <returns>Nombre localizado</returns>
        public static string GetLocalizedName(TileType tileType)
        {
            try
            {
                // Usar el sistema de localización universal
                if (LocalizationKeys.TryGetValue(tileType, out var localizationKey))
                {
                    return LocalizationManager.GetText(localizationKey);
                }
            }
            catch (System.Exception)
            {
                // Fallback silencioso
            }

            // Último fallback al nombre del enum
            return tileType.ToString();
        }

        /// <summary>
        /// OBSOLETO: Usar GetLocalizedName() en su lugar.
        /// </summary>
        public static string GetSpanishName(TileType tileType)
        {
            return GetLocalizedName(tileType);
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
            string materialKey = tileType switch
            {
                TileType.ground => "material_earth",
                TileType.floor => "material_construction",
                TileType.wall => "material_stone",
                TileType.ore => "material_mineral",
                TileType.water => "material_liquid",
                TileType.bridge => "material_construction",
                TileType.ancientCrystal => "material_crystal",
                TileType.bigRoot => "material_organic",
                // TileType.lava => "material_liquid", // No disponible en esta versión
                // TileType.ice => "material_liquid", // No disponible en esta versión
                _ => "material_unknown"
            };

            return LocalizationManager.GetText(materialKey);
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
            string soundKey = tileType switch
            {
                TileType.ground => "sound_earth",
                TileType.floor => "sound_stone",
                TileType.wall => "sound_stone",
                TileType.ore => "sound_metal",
                TileType.water => "sound_splash",
                TileType.bridge => "sound_wood",
                TileType.smallStones => "sound_gravel",
                TileType.rug => "sound_textile",
                TileType.rail => "sound_metal",
                _ => "sound_neutral"
            };

            return LocalizationManager.GetText(soundKey);
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
            string toolKey = tileType switch
            {
                TileType.ore => "tool_pickaxe",
                TileType.wall => "tool_pickaxe",
                TileType.ancientCrystal => "tool_high_quality_pickaxe",
                TileType.bigRoot => "tool_axe",
                TileType.ground => "tool_shovel",
                TileType.dugUpGround => "tool_shovel",
                TileType.smallStones => "tool_pickaxe_or_shovel",
                _ => "tool_none"
            };

            return LocalizationManager.GetText(toolKey);
        }
    }
}