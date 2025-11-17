extern alias PugOther;

using PugTilemap;
using Unity.Mathematics;

namespace ckAccess.MapReader
{
    /// <summary>
    /// Sistema de heurísticas para detectar tiles y objetos colocados por el jugador.
    /// NOTA: Core Keeper NO mantiene un flag persistente de "colocado por jugador",
    /// así que usamos heurísticas basadas en patrones del juego.
    /// </summary>
    public static class PlayerPlacedHelper
    {
        /// <summary>
        /// Verifica si un tile probablemente fue colocado por el jugador.
        /// Usa múltiples heurísticas para determinar esto.
        /// </summary>
        public static bool IsProbablyPlayerPlacedTile(TileType tileType, int tileset, int2 position)
        {
            // 1. Tipos que SOLO pueden ser colocados por jugador
            if (IsPlayerOnlyTileType(tileType))
                return true;

            // 2. Tilesets pintados (definitivamente del jugador)
            if (IsPaintedTileset(tileset))
                return true;

            // 3. Combinaciones específicas que indican colocación manual
            if (IsUnusualTileCombination(tileType, tileset))
                return true;

            return false;
        }

        /// <summary>
        /// Tipos de tiles que solo pueden ser colocados por el jugador.
        /// </summary>
        private static bool IsPlayerOnlyTileType(TileType tileType)
        {
            return tileType switch
            {
                TileType.fence => true,
                TileType.rail => true,
                TileType.bridge => true,
                TileType.rug => true,
                TileType.litFloor => true,  // Suelos iluminados colocables
                _ => false
            };
        }

        /// <summary>
        /// Verifica si un tileset es de los que solo se obtienen pintando.
        /// Basado en el análisis del código del juego (PlacementHandlerPainting.cs).
        /// </summary>
        private static bool IsPaintedTileset(int tileset)
        {
            // Rangos de tilesets pintados según el código del juego
            if (tileset >= 15 && tileset <= 22) return true;  // Painted variants 1
            if (tileset >= 37 && tileset <= 40) return true;  // Painted variants 2
            if (tileset >= 41 && tileset <= 52) return true;  // Painted variants 3
            if (tileset >= 61 && tileset <= 64) return true;  // Painted variants 4

            return false;
        }

        /// <summary>
        /// Detecta combinaciones inusuales de tile+tileset que probablemente indican colocación manual.
        /// Por ejemplo: paredes de piedra en bioma de tierra, etc.
        /// </summary>
        private static bool IsUnusualTileCombination(TileType tileType, int tileset)
        {
            // Por ahora retornamos false, pero esto podría expandirse
            // para detectar tiles "fuera de lugar" en un bioma
            // (requeriría analizar tiles circundantes para determinar bioma)
            return false;
        }

        /// <summary>
        /// Verifica si un objeto es del tipo PlaceablePrefab (800),
        /// que indica objetos que el jugador puede colocar.
        /// NOTA: Esta verificación requiere acceso a ObjectDataCD y PugDatabase,
        /// se implementará en SimpleWorldReader.
        /// </summary>
        public static bool IsPlaceableObjectType(int objectTypeValue)
        {
            // ObjectType.PlaceablePrefab = 800
            return objectTypeValue == 800;
        }

        /// <summary>
        /// Categorías de ObjectCategoryTag que típicamente indican objetos colocados por jugador.
        /// </summary>
        public static bool IsPlayerPlacedCategory(string categoryTag)
        {
            return categoryTag switch
            {
                "DefensiveStructure" => true,
                "LightSource" => true,
                "TileBlock" => true,
                _ => false
            };
        }
    }
}
