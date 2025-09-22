extern alias PugOther;
extern alias Core;

using System;
using System.Collections.Generic;
using PugTilemap;
using Unity.Mathematics;
using UnityEngine;
using DavyKager;
using ckAccess.Localization;

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
                    Tolk.Output(LocalizationManager.GetText("empty"));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleWorldReader] Error: {e.Message}");
                Tolk.Output(LocalizationManager.GetText("reading_error"));
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
                return LocalizationManager.GetText("the_core");

            // Cofres y almacenamiento
            if (name.Contains("chest") || name.Contains("storage") || name.Contains("container"))
                return LocalizationManager.GetText("work_station", CleanName(name));

            // Estaciones de trabajo
            if (name.Contains("workbench") || name.Contains("anvil") || name.Contains("furnace") ||
                name.Contains("table") || name.Contains("forge") || name.Contains("station"))
                return LocalizationManager.GetText("work_station", CleanName(name));

            // Enemigos (excluyendo estatuas)
            if (!name.Contains("statue") && !name.Contains("trophy") &&
                (name.Contains("slime") || name.Contains("spider") || name.Contains("larva") ||
                 name.Contains("grub") || name.Contains("mushroom") || name.Contains("scarab") ||
                 name.Contains("mold") || name.Contains("boss") || name.Contains("enemy")))
                return LocalizationManager.GetText("enemy", CleanName(name));

            // Objetos recolectables
            if (name.Contains("pickup") || name.Contains("drop") || name.Contains("item"))
                return LocalizationManager.GetText("object", CleanName(name));

            // Plantas y árboles
            if (name.Contains("tree") || name.Contains("plant") || name.Contains("flower") || name.Contains("mushroom"))
                return LocalizationManager.GetText("plant", CleanName(name));

            // Recursos minerales
            if (name.Contains("ore") || name.Contains("crystal") || name.Contains("mineral") || name.Contains("rock"))
                return LocalizationManager.GetText("resource", CleanName(name));

            // Cualquier otra entidad
            return LocalizationManager.GetText("entity", CleanName(name));
        }

        /// <summary>
        /// Obtiene descripción de tiles (simple y directo).
        /// Actualizado para detectar mejor los tiles colocados recientemente.
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
                        ? LocalizationManager.GetText("destructible_tool", tileName, tool)
                        : LocalizationManager.GetText("destructible_material_tool", tileName, materialName, tool);
                }

                if (TileTypeHelper.IsBlocking(topTile.tileType))
                {
                    return string.IsNullOrEmpty(materialName)
                        ? LocalizationManager.GetText("blocking", tileName)
                        : LocalizationManager.GetText("blocking_material", tileName, materialName);
                }

                if (TileTypeHelper.IsDangerous(topTile.tileType))
                {
                    return string.IsNullOrEmpty(materialName)
                        ? LocalizationManager.GetText("dangerous", tileName)
                        : LocalizationManager.GetText("dangerous_material", tileName, materialName);
                }

                // Tile normal
                return string.IsNullOrEmpty(materialName)
                    ? tileName
                    : LocalizationManager.GetText("tile_with_material", tileName, materialName);
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
                // PRIORIDAD 1: Usar LocalizationManager para consistencia
                string tilesetKey = tileset switch
                {
                    0 => "tileset_dirt",        // Dirt
                    1 => "tileset_stone",       // Stone
                    2 => "tileset_obsidian",    // Obsidian
                    3 => "tileset_lava",        // Lava
                    8 => "tileset_nature",      // Nature
                    9 => "tileset_mold",        // Mold
                    10 => "tileset_sea",        // Sea
                    12 => "tileset_sand",       // Sand
                    26 => "tileset_desert",     // Desert
                    31 => "tileset_snow",       // Snow
                    34 => "tileset_crystal",    // Glass/Crystal
                    59 => "tileset_dark_stone", // DarkStone
                    _ => null
                };

                if (tilesetKey != null)
                {
                    return LocalizationManager.GetText(tilesetKey);
                }

                // FALLBACK: Solo usar TilesetHelper si nuestro sistema no conoce el tileset
                var friendlyName = TilesetHelper.GetLocalizedName(tileset);
                if (!string.IsNullOrEmpty(friendlyName) && friendlyName != $"Material {tileset}")
                    return friendlyName;
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Descripción básica cuando todo lo demás falla.
        /// </summary>
        private static string GetBasicDescription(Vector3 worldPosition)
        {
            try
            {
                var multiMap = PugOther.Manager.multiMap;
                if (multiMap == null) return LocalizationManager.GetText("area_not_available");

                var tileLayerLookup = multiMap.GetTileLayerLookup();
                var position = new int2(Mathf.RoundToInt(worldPosition.x), Mathf.RoundToInt(worldPosition.z));
                var topTile = tileLayerLookup.GetTopTile(position);

                if (topTile.tileType == TileType.none)
                    return LocalizationManager.GetText("empty");

                return TileTypeHelper.GetLocalizedName(topTile.tileType);
            }
            catch
            {
                return LocalizationManager.GetText("unknown_position");
            }
        }

        /// <summary>
        /// Limpia nombres de entidades.
        /// </summary>
        private static string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name)) return LocalizationManager.GetText("unknown");

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