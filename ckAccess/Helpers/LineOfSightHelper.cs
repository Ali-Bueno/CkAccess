extern alias PugOther;
using PugTilemap;
using Unity.Mathematics;
using UnityEngine;

namespace ckAccess.Helpers
{
    /// <summary>
    /// Helper centralizado para verificar línea de visión entre dos puntos.
    /// Usa el algoritmo de Bresenham sobre el grid de tiles del juego.
    /// </summary>
    public static class LineOfSightHelper
    {
        // Tilesets que permiten ver a través
        private const int GLASS_TILESET = 34;

        /// <summary>
        /// Verifica si hay línea de visión clara entre dos puntos (sin paredes bloqueando).
        /// </summary>
        /// <param name="from">Posición de origen</param>
        /// <param name="to">Posición de destino</param>
        /// <param name="maxDistance">Distancia máxima a verificar (default: 50 tiles)</param>
        /// <returns>True si hay línea de visión, false si está bloqueada</returns>
        public static bool HasLineOfSight(Vector3 from, Vector3 to, int maxDistance = 50)
        {
            try
            {
                float distance = Vector3.Distance(from, to);

                // Si están muy cerca, siempre hay línea de visión
                if (distance < 2f) return true;

                var multiMap = PugOther.Manager.multiMap;
                if (multiMap == null) return true; // Fallback seguro

                var tileLayerLookup = multiMap.GetTileLayerLookup();

                // Convertir a coordenadas de tile
                int x0 = Mathf.RoundToInt(from.x);
                int z0 = Mathf.RoundToInt(from.z);
                int x1 = Mathf.RoundToInt(to.x);
                int z1 = Mathf.RoundToInt(to.z);

                // Algoritmo de Bresenham para trazar línea
                int dx = System.Math.Abs(x1 - x0);
                int dz = System.Math.Abs(z1 - z0);
                int sx = x0 < x1 ? 1 : -1;
                int sz = z0 < z1 ? 1 : -1;
                int err = dx - dz;

                int safetyCounter = 0;
                while (true)
                {
                    if (safetyCounter++ > maxDistance) break;

                    // Verificar tile actual
                    var position = new int2(x0, z0);
                    var topTile = tileLayerLookup.GetTopTile(position);

                    // Verificar si bloquea visión
                    if (IsVisionBlocking(topTile.tileType, topTile.tileset))
                    {
                        return false;
                    }

                    if (x0 == x1 && z0 == z1) break;

                    int e2 = 2 * err;
                    if (e2 > -dz)
                    {
                        err -= dz;
                        x0 += sx;
                    }
                    if (e2 < dx)
                    {
                        err += dx;
                        z0 += sz;
                    }
                }

                return true;
            }
            catch
            {
                return true; // En caso de error, asumir visible para no romper nada
            }
        }

        /// <summary>
        /// Sobrecarga para float3 (Unity.Mathematics)
        /// </summary>
        public static bool HasLineOfSight(float3 from, float3 to, int maxDistance = 50)
        {
            return HasLineOfSight(
                new Vector3(from.x, from.y, from.z),
                new Vector3(to.x, to.y, to.z),
                maxDistance);
        }

        /// <summary>
        /// Determina si un tipo de tile bloquea la visión.
        /// </summary>
        /// <param name="tileType">Tipo de tile</param>
        /// <param name="tileset">ID del tileset</param>
        /// <returns>True si bloquea visión, false si permite ver a través</returns>
        public static bool IsVisionBlocking(TileType tileType, int tileset)
        {
            // Primero verificar si es transparente
            if (IsSeeThrough(tileType, tileset))
                return false;

            // Tipos de tile que bloquean visión
            switch (tileType)
            {
                case TileType.wall:
                case TileType.greatWall:
                case TileType.thinWall:
                case TileType.ore:
                case TileType.ancientCrystal:
                case TileType.bigRoot:
                case TileType.chrysalis:
                    return true;

                // Tiles que bloquean movimiento pero NO visión
                case TileType.pit:
                case TileType.fence:
                case TileType.water:
                case TileType.rail:
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Verifica si un tipo de tile permite ver a través de él.
        /// </summary>
        /// <param name="tileType">Tipo de tile</param>
        /// <param name="tileset">ID del tileset</param>
        /// <returns>True si permite ver a través</returns>
        public static bool IsSeeThrough(TileType tileType, int tileset)
        {
            // Vallas siempre permiten ver a través
            if (tileType == TileType.fence) return true;

            // Cristales (tileset 34 = glass/crystal)
            if (tileset == GLASS_TILESET) return true;

            // Paredes finas de cristal
            if (tileType == TileType.thinWall && tileset == GLASS_TILESET) return true;

            return false;
        }

        /// <summary>
        /// Calcula la dirección cardinal de un punto a otro.
        /// </summary>
        /// <param name="from">Posición de origen</param>
        /// <param name="to">Posición de destino</param>
        /// <returns>Dirección en grados (0-360, donde 0 = Este)</returns>
        public static float GetAngleToTarget(Vector3 from, Vector3 to)
        {
            Vector3 diff = to - from;
            float angle = math.degrees(math.atan2(diff.z, diff.x));

            // Normalizar entre 0-360
            if (angle < 0) angle += 360;

            return angle;
        }

        /// <summary>
        /// Obtiene la dirección cardinal como texto localizado.
        /// </summary>
        /// <param name="from">Posición de origen</param>
        /// <param name="to">Posición de destino</param>
        /// <returns>Clave de localización de la dirección</returns>
        public static string GetCardinalDirection(Vector3 from, Vector3 to)
        {
            float angle = GetAngleToTarget(from, to);

            if (angle >= 337.5f || angle < 22.5f)
                return "dir_east";
            else if (angle >= 22.5f && angle < 67.5f)
                return "dir_northeast";
            else if (angle >= 67.5f && angle < 112.5f)
                return "dir_north";
            else if (angle >= 112.5f && angle < 157.5f)
                return "dir_northwest";
            else if (angle >= 157.5f && angle < 202.5f)
                return "dir_west";
            else if (angle >= 202.5f && angle < 247.5f)
                return "dir_southwest";
            else if (angle >= 247.5f && angle < 292.5f)
                return "dir_south";
            else
                return "dir_southeast";
        }
    }
}
