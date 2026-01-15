extern alias PugOther;
extern alias Core;

using System;
using PugTilemap;
using Unity.Mathematics;
using UnityEngine;
using DavyKager;
using ckAccess.Localization;
using ckAccess.Helpers;

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
        /// Busca entidades visibles cerca de la posición con priorización mejorada.
        /// PRIORIDAD: 1) Objetos colocables del jugador 2) Interactuables 3) Enemigos 4) Otros
        /// </summary>
        private static string GetVisibleEntityDescription(Vector3 worldPosition)
        {
            try
            {
                // Usar el sistema de entityMonoLookUp que sabemos que funciona
                var entityLookup = PugOther.Manager.memory?.entityMonoLookUp;
                if (entityLookup == null) return null;

                var targetPos = new UnityEngine.Vector3(worldPosition.x, worldPosition.y, worldPosition.z);

                // Listas para priorización
                PugOther.EntityMonoBehaviour closestPlaceable = null;
                PugOther.EntityMonoBehaviour closestInteractable = null;
                PugOther.EntityMonoBehaviour closestEnemy = null;
                PugOther.EntityMonoBehaviour closestOther = null;
                float minPlaceableDistance = float.MaxValue;
                float minInteractableDistance = float.MaxValue;
                float minEnemyDistance = float.MaxValue;
                float minOtherDistance = float.MaxValue;

                foreach (var kvp in entityLookup)
                {
                    var entity = kvp.Value;
                    if (entity?.gameObject?.activeInHierarchy != true) continue;

                    var entityPos = entity.WorldPosition;
                    var distance = Vector3.Distance(targetPos, new Vector3(entityPos.x, entityPos.y, entityPos.z));

                    // Solo muy cerca (0.7 tiles)
                    if (distance <= 0.7f)
                    {
                        var category = ObjectCategoryHelper.GetCategory(entity);

                        // Clasificar por prioridad usando detección mejorada
                        if (IsLikelyPlayerPlaced(entity, category))
                        {
                            if (distance < minPlaceableDistance)
                            {
                                closestPlaceable = entity;
                                minPlaceableDistance = distance;
                            }
                        }
                        else if (EntityClassificationHelper.IsInteractable(entity) || ObjectCategoryHelper.IsInteractable(category))
                        {
                            if (distance < minInteractableDistance)
                            {
                                closestInteractable = entity;
                                minInteractableDistance = distance;
                            }
                        }
                        else if (EntityClassificationHelper.IsEnemy(entity) || ObjectCategoryHelper.IsHostile(category))
                        {
                            if (distance < minEnemyDistance)
                            {
                                closestEnemy = entity;
                                minEnemyDistance = distance;
                            }
                        }
                        else
                        {
                            if (distance < minOtherDistance)
                            {
                                closestOther = entity;
                                minOtherDistance = distance;
                            }
                        }
                    }
                }

                // Retornar según prioridad
                if (closestPlaceable != null)
                    return GetEntityDescription(closestPlaceable, isPlayerPlaced: true);
                if (closestInteractable != null)
                    return GetEntityDescription(closestInteractable);
                if (closestEnemy != null)
                    return GetEntityDescription(closestEnemy);
                if (closestOther != null)
                    return GetEntityDescription(closestOther);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SimpleWorldReader] Error buscando entidades: {e.Message}");
            }

            return null;
        }

        /// <summary>
        /// Determina si una entidad probablemente fue colocada por el jugador.
        /// Usa heurísticas basadas en categoría y nombre.
        /// </summary>
        private static bool IsLikelyPlayerPlaced(PugOther.EntityMonoBehaviour entity, ObjectCategoryHelper.ObjectCategory category)
        {
            // Categorías que típicamente son colocadas por jugador
            if (category == ObjectCategoryHelper.ObjectCategory.WorkStation ||
                category == ObjectCategoryHelper.ObjectCategory.Furniture ||
                category == ObjectCategoryHelper.ObjectCategory.Decoration)
            {
                return true;
            }

            // Nombres que indican construcción/colocación
            var name = entity.gameObject.name.ToLower();
            if (name.Contains("placed") || name.Contains("built") || name.Contains("constructed"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Obtiene descripción simple de una entidad usando el sistema de categorización.
        /// MEJORADO: Usa ObjectCategoryHelper y añade indicador de "colocado por jugador".
        /// </summary>
        private static string GetEntityDescription(PugOther.EntityMonoBehaviour entity, bool isPlayerPlaced = false)
        {
            var name = entity.gameObject.name;
            var cleanedName = CleanName(name.Replace("(clone)", "").Replace("_", " ").Trim());

            // Usar sistema de categorización inteligente
            var category = ObjectCategoryHelper.GetCategory(entity);

            // Generar descripción base basada en categoría
            string baseDescription = category switch
            {
                ObjectCategoryHelper.ObjectCategory.Core =>
                    LocalizationManager.GetText("the_core"),

                ObjectCategoryHelper.ObjectCategory.Chest =>
                    LocalizationManager.GetText("work_station", cleanedName),

                ObjectCategoryHelper.ObjectCategory.WorkStation =>
                    LocalizationManager.GetText("work_station", cleanedName),

                ObjectCategoryHelper.ObjectCategory.Enemy =>
                    LocalizationManager.GetText("enemy", cleanedName),

                ObjectCategoryHelper.ObjectCategory.Pickup =>
                    LocalizationManager.GetText("object", cleanedName),

                ObjectCategoryHelper.ObjectCategory.Plant =>
                    LocalizationManager.GetText("plant", cleanedName),

                ObjectCategoryHelper.ObjectCategory.Resource =>
                    LocalizationManager.GetText("resource", cleanedName),

                ObjectCategoryHelper.ObjectCategory.Animal =>
                    LocalizationManager.GetText("entity", $"{LocalizationManager.GetText("prefix_animal")}: {cleanedName}"),

                ObjectCategoryHelper.ObjectCategory.Critter =>
                    LocalizationManager.GetText("entity", $"{LocalizationManager.GetText("prefix_creature")}: {cleanedName}"),

                ObjectCategoryHelper.ObjectCategory.Statue =>
                    LocalizationManager.GetText("entity", $"{LocalizationManager.GetText("prefix_statue")}: {cleanedName}"),

                ObjectCategoryHelper.ObjectCategory.Furniture =>
                    LocalizationManager.GetText("entity", $"{LocalizationManager.GetText("prefix_furniture")}: {cleanedName}"),

                ObjectCategoryHelper.ObjectCategory.Structure =>
                    LocalizationManager.GetText("entity", $"{LocalizationManager.GetText("prefix_structure")}: {cleanedName}"),

                _ => LocalizationManager.GetText("entity", cleanedName)
            };

            // Añadir indicador de "colocado por jugador" si aplica
            if (isPlayerPlaced)
            {
                baseDescription = LocalizationManager.GetText("player_placed", baseDescription);
            }

            return baseDescription;
        }

        /// <summary>
        /// Obtiene descripción de tiles con detección mejorada de tiles colocados por jugador.
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

                // Verificar si probablemente fue colocado por jugador
                bool isPlayerPlaced = PlayerPlacedHelper.IsProbablyPlayerPlacedTile(
                    topTile.tileType,
                    topTile.tileset,
                    position);

                // Obtener nombres
                var tileName = TileTypeHelper.GetLocalizedName(topTile.tileType);
                var materialName = GetSimpleMaterialName(topTile.tileset);

                // Información especial para tiles importantes
                string baseDescription;

                if (TileTypeHelper.IsDamageable(topTile.tileType))
                {
                    var tool = TileTypeHelper.GetRecommendedTool(topTile.tileType);
                    baseDescription = string.IsNullOrEmpty(materialName)
                        ? LocalizationManager.GetText("destructible_tool", tileName, tool)
                        : LocalizationManager.GetText("destructible_material_tool", tileName, materialName, tool);
                }
                else if (TileTypeHelper.IsBlocking(topTile.tileType))
                {
                    baseDescription = string.IsNullOrEmpty(materialName)
                        ? LocalizationManager.GetText("blocking", tileName)
                        : LocalizationManager.GetText("blocking_material", tileName, materialName);
                }
                else if (TileTypeHelper.IsDangerous(topTile.tileType))
                {
                    baseDescription = string.IsNullOrEmpty(materialName)
                        ? LocalizationManager.GetText("dangerous", tileName)
                        : LocalizationManager.GetText("dangerous_material", tileName, materialName);
                }
                else
                {
                    // Tile normal
                    baseDescription = string.IsNullOrEmpty(materialName)
                        ? tileName
                        : LocalizationManager.GetText("tile_with_material", tileName, materialName);
                }

                // Añadir indicador de "colocado por jugador" si aplica
                if (isPlayerPlaced)
                {
                    baseDescription = LocalizationManager.GetText("player_placed", baseDescription);
                }

                return baseDescription;
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