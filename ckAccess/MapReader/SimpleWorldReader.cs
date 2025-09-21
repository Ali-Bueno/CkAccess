extern alias PugOther;
extern alias Core;

using System;
using System.Collections.Generic;
using PugTilemap;
using Unity.Mathematics;
using UnityEngine;
using DavyKager;

namespace ckAccess.MapReader
{
    /// <summary>
    /// Sistema SIMPLE y directo de lectura del mundo que prioriza funcionalidad sobre complejidad.
    /// ENFOQUE: Si funciona, está bien. Si no funciona, usar fallback simple.
    /// </summary>
    public static class SimpleWorldReader
    {
        /// <summary>
        /// Lee y anuncia directamente lo que hay en una posición con máxima simplicidad.
        /// PRIORIDAD: 1) Entidades visibles 2) Tiles destructibles 3) Tiles normales
        /// </summary>
        public static void AnnouncePosition(Vector3 worldPosition)
        {
            try
            {
                var result = GetSimpleDescription(worldPosition);
                if (!string.IsNullOrEmpty(result))
                {
                    Tolk.Output(result);
                    Debug.Log($"[SimpleWorldReader] {result}");
                }
                else
                {
                    Tolk.Output("Vacío");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleWorldReader] Error: {e.Message}");
                Tolk.Output("Error de lectura");
            }
        }

        /// <summary>
        /// Obtiene una descripción simple y directa de la posición.
        /// </summary>
        public static string GetSimpleDescription(Vector3 worldPosition)
        {
            // 1. BUSCAR ENTIDADES VISIBLES (lo más importante)
            var entityDescription = GetVisibleEntityDescription(worldPosition);
            if (!string.IsNullOrEmpty(entityDescription))
                return entityDescription;

            // 2. BUSCAR TILES DESTRUCTIBLES/ESPECIALES
            var tileDescription = GetTileDescription(worldPosition);
            if (!string.IsNullOrEmpty(tileDescription))
                return tileDescription;

            // 3. FALLBACK
            return GetBasicDescription(worldPosition);
        }

        /// <summary>
        /// Busca entidades visibles cerca de la posición (método súper simple).
        /// </summary>
        private static string GetVisibleEntityDescription(Vector3 worldPosition)
        {
            try
            {
                // Usar el sistema de entityMonoLookUp que sabemos que funciona
                var entityLookup = PugOther.Manager.memory?.entityMonoLookUp;
                if (entityLookup == null) return null;

                var targetPos = new UnityEngine.Vector3(worldPosition.x, worldPosition.y, worldPosition.z);

                foreach (var kvp in entityLookup)
                {
                    var entity = kvp.Value;
                    if (entity?.gameObject?.activeInHierarchy != true) continue;

                    var entityPos = entity.WorldPosition;
                    var distance = Vector3.Distance(targetPos, new Vector3(entityPos.x, entityPos.y, entityPos.z));

                    // Solo muy cerca (0.7 tiles)
                    if (distance <= 0.7f)
                    {
                        return GetEntityDescription(entity);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SimpleWorldReader] Error buscando entidades: {e.Message}");
            }

            return null;
        }

        /// <summary>
        /// Obtiene descripción simple de una entidad basada en su nombre de GameObject.
        /// </summary>
        private static string GetEntityDescription(PugOther.EntityMonoBehaviour entity)
        {
            var name = entity.gameObject.name.ToLower();

            // Limpiar nombre básico
            name = name.Replace("(clone)", "").Replace("_", " ").Trim();

            // DETECTAR POR PATRONES SIMPLES

            // Núcleo principal
            if (name.Contains("core"))
                return "El Núcleo (interactuable)";

            // Cofres y almacenamiento
            if (name.Contains("chest") || name.Contains("storage") || name.Contains("container"))
                return $"{CleanName(name)} (almacenamiento)";

            // Estaciones de trabajo
            if (name.Contains("workbench") || name.Contains("anvil") || name.Contains("furnace") ||
                name.Contains("table") || name.Contains("forge") || name.Contains("station"))
                return $"{CleanName(name)} (estación de trabajo)";

            // Enemigos (excluyendo estatuas)
            if (!name.Contains("statue") && !name.Contains("trophy") &&
                (name.Contains("slime") || name.Contains("spider") || name.Contains("larva") ||
                 name.Contains("grub") || name.Contains("mushroom") || name.Contains("scarab") ||
                 name.Contains("mold") || name.Contains("boss") || name.Contains("enemy")))
                return $"{CleanName(name)} (enemigo)";

            // Objetos recolectables
            if (name.Contains("pickup") || name.Contains("drop") || name.Contains("item"))
                return $"{CleanName(name)} (objeto)";

            // Plantas y árboles
            if (name.Contains("tree") || name.Contains("plant") || name.Contains("flower") || name.Contains("mushroom"))
                return $"{CleanName(name)} (planta)";

            // Recursos minerales
            if (name.Contains("ore") || name.Contains("crystal") || name.Contains("mineral") || name.Contains("rock"))
                return $"{CleanName(name)} (recurso)";

            // Cualquier otra entidad
            return $"{CleanName(name)} (entidad)";
        }

        /// <summary>
        /// Obtiene descripción de tiles (simple y directo).
        /// </summary>
        private static string GetTileDescription(Vector3 worldPosition)
        {
            try
            {
                var multiMap = PugOther.Manager.multiMap;
                if (multiMap == null) return null;

                var tileLayerLookup = multiMap.GetTileLayerLookup();
                var position = new int2(Mathf.RoundToInt(worldPosition.x), Mathf.RoundToInt(worldPosition.z));
                var topTile = tileLayerLookup.GetTopTile(position);

                if (topTile.tileType == TileType.none)
                    return null;

                // Obtener nombres
                var tileName = TileTypeHelper.GetLocalizedName(topTile.tileType);
                var materialName = GetSimpleMaterialName(topTile.tileset);

                // Información especial para tiles importantes
                if (TileTypeHelper.IsDamageable(topTile.tileType))
                {
                    var tool = TileTypeHelper.GetRecommendedTool(topTile.tileType);
                    return string.IsNullOrEmpty(materialName)
                        ? $"{tileName} destructible (usar {tool})"
                        : $"{tileName} de {materialName} destructible (usar {tool})";
                }

                if (TileTypeHelper.IsBlocking(topTile.tileType))
                {
                    return string.IsNullOrEmpty(materialName)
                        ? $"{tileName} (bloqueante)"
                        : $"{tileName} de {materialName} (bloqueante)";
                }

                if (TileTypeHelper.IsDangerous(topTile.tileType))
                {
                    return string.IsNullOrEmpty(materialName)
                        ? $"{tileName} (peligroso)"
                        : $"{tileName} de {materialName} (peligroso)";
                }

                // Tile normal
                return string.IsNullOrEmpty(materialName)
                    ? tileName
                    : $"{tileName} de {materialName}";
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SimpleWorldReader] Error leyendo tile: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene nombre simple de material basado en tileset.
        /// </summary>
        private static string GetSimpleMaterialName(int tileset)
        {
            try
            {
                // Intentar usar TilesetHelper existente
                var friendlyName = TilesetHelper.GetLocalizedName(tileset);
                if (!string.IsNullOrEmpty(friendlyName) && friendlyName != $"Material {tileset}")
                    return friendlyName;
            }
            catch { }

            // Fallback a mapeo manual simple
            return tileset switch
            {
                0 => "tierra",       // Dirt
                1 => "piedra",       // Stone
                2 => "obsidiana",    // Obsidian
                3 => "lava",         // Lava
                8 => "naturaleza",   // Nature
                9 => "moho",         // Mold
                10 => "mar",         // Sea
                12 => "arena",       // Sand
                26 => "desierto",    // Desert
                31 => "nieve",       // Snow
                34 => "cristal",     // Glass/Crystal
                59 => "piedra oscura", // DarkStone
                _ => null
            };
        }

        /// <summary>
        /// Descripción básica cuando todo lo demás falla.
        /// </summary>
        private static string GetBasicDescription(Vector3 worldPosition)
        {
            try
            {
                var multiMap = PugOther.Manager.multiMap;
                if (multiMap == null) return "Área no disponible";

                var tileLayerLookup = multiMap.GetTileLayerLookup();
                var position = new int2(Mathf.RoundToInt(worldPosition.x), Mathf.RoundToInt(worldPosition.z));
                var topTile = tileLayerLookup.GetTopTile(position);

                if (topTile.tileType == TileType.none)
                    return "Vacío";

                return TileTypeHelper.GetLocalizedName(topTile.tileType);
            }
            catch
            {
                return "Posición desconocida";
            }
        }

        /// <summary>
        /// Limpia nombres de entidades.
        /// </summary>
        private static string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Desconocido";

            // Capitalizar primera letra de cada palabra
            var words = name.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
                }
            }

            return string.Join(" ", words);
        }
    }
}