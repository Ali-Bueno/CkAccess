extern alias PugOther;
extern alias Core;

using System;
using System.Collections.Generic;
using System.Linq;
using PugTilemap;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ckAccess.MapReader
{
    /// <summary>
    /// Sistema REAL de lectura del mundo que utiliza los sistemas nativos del juego
    /// para detectar tiles, objetos, enemigos e interactuables.
    /// </summary>
    public static class RealWorldMapReader
    {
        /// <summary>
        /// Lee información completa y REAL de una posición del mundo.
        /// </summary>
        /// <param name="worldPosition">Posición en coordenadas del mundo</param>
        /// <returns>Información real de la posición</returns>
        public static RealWorldPositionInfo ReadRealPosition(Vector3 worldPosition)
        {
            var info = new RealWorldPositionInfo();
            var int2Position = new int2(
                Mathf.RoundToInt(worldPosition.x),
                Mathf.RoundToInt(worldPosition.z)
            );

            try
            {
                // 1. Leer tiles usando el sistema real (optimizado)
                info.TileInfo = GetRealTileInformation(int2Position);

                // 2. Buscar entidades reales (optimizado - solo si no hay tile bloqueante)
                if (!info.TileInfo.IsBlocking)
                {
                    info.EntitiesInfo = GetRealEntitiesInformation(worldPosition);
                }
                else
                {
                    info.EntitiesInfo = new RealEntitiesInfo(); // Vacío para mejor rendimiento
                }

                // 3. Información de estado
                info.IsInitialized = IsMapAvailable();
                info.HasAnyTile = info.TileInfo.HasValidTile;
                info.HasAnyEntity = info.EntitiesInfo.Entities.Count > 0;

                return info;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RealWorldMapReader] Error leyendo posición {worldPosition}: {e.Message}");
                return info; // Retornar info vacía en caso de error
            }
        }

        /// <summary>
        /// Obtiene información REAL de tiles usando el sistema nativo.
        /// </summary>
        private static RealTileInfo GetRealTileInformation(int2 position)
        {
            var tileInfo = new RealTileInfo();

            try
            {
                if (!IsMapAvailable()) return tileInfo;

                var multiMap = PugOther.Manager.multiMap;
                if (multiMap == null) return tileInfo;

                // OPTIMIZADO: Acceso directo sin logs
                var tileLayerLookup = multiMap.GetTileLayerLookup();
                var topTile = tileLayerLookup.GetTopTile(position);

                tileInfo.TopTile = topTile;
                tileInfo.HasValidTile = topTile.tileType != TileType.none;

                if (tileInfo.HasValidTile)
                {
                    tileInfo.TileName = TileTypeHelper.GetLocalizedName(topTile.tileType);
                    tileInfo.TilesetName = TilesetHelper.GetLocalizedName(topTile.tileset);
                    tileInfo.MaterialCategory = TileTypeHelper.GetMaterialCategory(topTile.tileType);
                    tileInfo.IsBlocking = TileTypeHelper.IsBlocking(topTile.tileType);
                    tileInfo.IsDamageable = TileTypeHelper.IsDamageable(topTile.tileType);
                    tileInfo.IsDangerous = TileTypeHelper.IsDangerous(topTile.tileType);
                    tileInfo.Hardness = TileTypeHelper.GetHardness(topTile.tileType);
                    tileInfo.RecommendedTool = TileTypeHelper.GetRecommendedTool(topTile.tileType);

                    // DEBUG REMOVIDO para optimizar rendimiento
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[RealWorldMapReader] Error obteniendo tiles: {e.Message}");
            }

            return tileInfo;
        }

        /// <summary>
        /// Obtiene información REAL de entidades usando Manager.memory.entityMonoLookUp.
        /// </summary>
        private static RealEntitiesInfo GetRealEntitiesInformation(Vector3 worldPosition)
        {
            var entitiesInfo = new RealEntitiesInfo();

            try
            {
                // OPTIMIZADO: Verificación rápida
                var entityLookup = PugOther.Manager.memory?.entityMonoLookUp;
                if (entityLookup == null) return entitiesInfo;

                // ULTRA OPTIMIZADO: Límites agresivos para máximo rendimiento
                var unityWorldPos = new UnityEngine.Vector3(worldPosition.x, worldPosition.y, worldPosition.z);
                int maxEntitiesCheck = 15; // Drásticamente reducido
                int checkedCount = 0;
                int foundEntities = 0;
                const int MAX_ENTITIES_RESULT = 2; // Máximo 2 entidades por posición

                foreach (var kvp in entityLookup)
                {
                    if (checkedCount++ > maxEntitiesCheck || foundEntities >= MAX_ENTITIES_RESULT)
                        break; // Límites agresivos

                    var entityMono = kvp.Value;
                    if (entityMono?.gameObject?.activeInHierarchy != true)
                        continue;

                    // OPTIMIZADO: Cálculo de distancia ultra rápido
                    var entityWorldPos = entityMono.WorldPosition;
                    var dx = unityWorldPos.x - entityWorldPos.x;
                    var dz = unityWorldPos.z - entityWorldPos.z;
                    var distanceSquared = dx * dx + dz * dz;

                    // OPTIMIZADO: Radio reducido para mejor rendimiento
                    if (distanceSquared <= 0.36f) // 0.6 tiles al cuadrado (más cerca)
                    {
                        var distance = Mathf.Sqrt(distanceSquared);
                        var realEntity = CreateRealEntityInfo(entityMono, worldPosition, distance);
                        if (realEntity != null)
                        {
                            entitiesInfo.Entities.Add(realEntity);
                            if (distance <= 0.7f) // Radio más estricto para "exacto"
                            {
                                entitiesInfo.EntitiesAtExactPosition.Add(realEntity);
                            }
                        }
                    }
                }

                // OPTIMIZADO: Ordenar solo si hay entidades
                if (entitiesInfo.Entities.Count > 0)
                {
                    entitiesInfo.Entities.Sort((a, b) => a.Distance.CompareTo(b.Distance));
                    entitiesInfo.EntitiesAtExactPosition.Sort((a, b) => a.Distance.CompareTo(b.Distance));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[RealWorldMapReader] Error en entidades: {e.Message}");
            }

            return entitiesInfo;
        }

        /// <summary>
        /// Crea información detallada de una entidad real.
        /// </summary>
        private static RealEntityInfo CreateRealEntityInfo(PugOther.EntityMonoBehaviour entityMono, Vector3 cursorPosition, float distance)
        {
            try
            {
                var realEntity = new RealEntityInfo();

                // Información básica
                realEntity.EntityMono = entityMono;
                var worldPos = entityMono.WorldPosition;
                realEntity.Position = new UnityEngine.Vector3(worldPos.x, worldPos.y, worldPos.z);
                realEntity.Distance = distance;
                realEntity.EntityName = entityMono.gameObject.name;

                // Intentar obtener ObjectID usando reflexión
                try
                {
                    // Usar reflexión para acceder al ObjectDataCD sin depender del tipo compilado
                    var objectID = GetEntityObjectIDSafe(entityMono);
                    if (objectID != ObjectID.None)
                    {
                        realEntity.ObjectID = objectID;
                        realEntity.ObjectName = GetEnhancedObjectName(objectID, entityMono);
                        realEntity.Category = GetEnhancedCategory(objectID, entityMono);

                        // Propiedades del objeto
                        realEntity.IsTool = ObjectTypeHelper.IsTool(objectID);
                        realEntity.IsResource = ObjectTypeHelper.IsResource(objectID);
                        realEntity.IsFood = ObjectTypeHelper.IsFood(objectID);
                        realEntity.IsStorage = ObjectTypeHelper.IsStorage(objectID);

                        // Cantidad por defecto
                        realEntity.Amount = 1;
                    }
                    else
                    {
                        // Fallback: intentar identificar por el nombre del GameObject
                        realEntity.ObjectName = GetNameFromGameObject(entityMono.gameObject);
                        realEntity.Category = GetCategoryFromGameObject(entityMono.gameObject);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[RealWorldMapReader] No se pudo obtener información del objeto de {entityMono.gameObject.name}: {e.Message}");
                    realEntity.ObjectName = GetNameFromGameObject(entityMono.gameObject);
                    realEntity.Category = GetCategoryFromGameObject(entityMono.gameObject);
                }

                // Determinar tipo de entidad basado en componentes
                realEntity.EntityType = DetermineEntityType(entityMono);

                // Información de interacción
                realEntity.IsInteractable = IsEntityInteractable(entityMono);
                realEntity.IsPickupable = IsEntityPickupable(entityMono);
                realEntity.IsDestructible = IsEntityDestructible(entityMono);

                return realEntity;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RealWorldMapReader] Error creando información de entidad: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene un nombre mejorado para el objeto basándose en su ID y contexto.
        /// </summary>
        private static string GetEnhancedObjectName(ObjectID objectID, PugOther.EntityMonoBehaviour entityMono)
        {
            // Primero intentar obtener el nombre de la base de datos completa
            var name = GameDatabase.GetName(objectID);
            if (!string.IsNullOrEmpty(name) && name != objectID.ToString())
            {
                return name;
            }

            // Si no está en la base de datos, usar el helper original
            return ObjectTypeHelper.GetLocalizedName(objectID);
        }

        /// <summary>
        /// Obtiene una categoría mejorada para el objeto.
        /// </summary>
        private static string GetEnhancedCategory(ObjectID objectID, PugOther.EntityMonoBehaviour entityMono)
        {
            // Usar la base de datos completa para categorías
            return GameDatabase.GetCategory(objectID);
        }

        /// <summary>
        /// Intenta obtener un nombre descriptivo del GameObject.
        /// </summary>
        private static string GetNameFromGameObject(Core::UnityEngine.GameObject gameObject)
        {
            var name = gameObject.name;

            // Limpiar nombres comunes del juego
            name = name.Replace("(Clone)", "");
            name = name.Replace("_", " ");
            name = name.Trim();

            // Traducciones comunes basadas en nombres de GameObjects
            var lowerName = name.ToLower();

            if (lowerName.Contains("slime")) return "Slime";
            if (lowerName.Contains("chest")) return "Cofre";
            if (lowerName.Contains("workbench")) return "Mesa de Trabajo";
            if (lowerName.Contains("core")) return "Núcleo";
            if (lowerName.Contains("enemy")) return "Enemigo";
            if (lowerName.Contains("mob")) return "Criatura";
            if (lowerName.Contains("ore")) return "Mineral";
            if (lowerName.Contains("crystal")) return "Cristal";
            if (lowerName.Contains("plant")) return "Planta";
            if (lowerName.Contains("mushroom")) return "Hongo";

            return name;
        }

        /// <summary>
        /// Intenta obtener una categoría basándose en el GameObject.
        /// </summary>
        private static string GetCategoryFromGameObject(Core::UnityEngine.GameObject gameObject)
        {
            var name = gameObject.name.ToLower();

            // Verificar por componentes específicos
            if (gameObject.GetComponent("Enemy") != null) return "Enemigo";
            if (gameObject.GetComponent("Inventory") != null) return "Almacenamiento";
            if (gameObject.GetComponent("CraftingStation") != null) return "Estación de Crafteo";
            if (gameObject.GetComponent("PickupItem") != null) return "Objeto";
            if (gameObject.GetComponent("DroppedItem") != null) return "Objeto Tirado";

            // Por nombre
            if (name.Contains("enemy") || name.Contains("mob") || name.Contains("slime")) return "Enemigo";
            if (name.Contains("chest") || name.Contains("storage")) return "Almacenamiento";
            if (name.Contains("workbench") || name.Contains("anvil") || name.Contains("furnace")) return "Estación de Crafteo";
            if (name.Contains("ore") || name.Contains("crystal")) return "Recurso";
            if (name.Contains("core")) return "Núcleo";

            return "Objeto";
        }

        /// <summary>
        /// Determina el tipo de entidad basado en sus componentes.
        /// </summary>
        private static EntityType DetermineEntityType(PugOther.EntityMonoBehaviour entityMono)
        {
            try
            {
                var gameObject = entityMono.gameObject;
                var objectID = GetEntityObjectID(entityMono);
                var objectName = objectID.ToString().ToLower();
                var gameObjectName = gameObject.name.ToLower();

                // PRIORIDAD 1: El Core (siempre importante)
                if (objectID == ObjectID.TheCore ||
                    objectID == ObjectID.BrokenCore ||
                    objectID == ObjectID.OracleCardCore)
                {
                    return EntityType.Core;
                }

                // PRIORIDAD 2: Verificar si es un enemigo REAL (no estatuas)
                // Las estatuas tienen "Statue" en el nombre
                bool isStatue = objectName.Contains("statue") ||
                                objectName.Contains("trophy") ||
                                gameObjectName.Contains("statue") ||
                                gameObjectName.Contains("trophy");

                if (!isStatue)
                {
                    // Verificar componentes de enemigo real
                    bool hasEnemyComponents = gameObject.GetComponent("Enemy") != null ||
                                             gameObject.GetComponent("AI") != null ||
                                             gameObject.GetComponent("EnemyCD") != null ||
                                             gameObject.GetComponent("CreatureCD") != null;

                    // Solo es enemigo si tiene componentes de enemigo Y está en la lista de enemigos
                    if (hasEnemyComponents && IsRealEnemy(objectID))
                    {
                        return EntityType.Enemy;
                    }

                    // Verificar por health pero excluir objetos no-enemigos
                    if (gameObject.GetComponent("Health") != null &&
                        !IsCraftingStation(objectID) &&
                        !objectName.Contains("player") &&
                        !objectName.Contains("chest") &&
                        !objectName.Contains("workbench") &&
                        IsRealEnemy(objectID))
                    {
                        return EntityType.Enemy;
                    }
                }

                // PRIORIDAD 3: Estaciones de trabajo
                if (IsCraftingStation(objectID))
                {
                    return EntityType.CraftingStation;
                }

                // PRIORIDAD 4: Cofres
                if (IsChestObjectID(objectID) ||
                    (gameObject.GetComponent("Inventory") != null && objectName.Contains("chest")))
                {
                    return EntityType.Storage;
                }

                // PRIORIDAD 5: Minerales y recursos
                if (IsMineralObjectID(objectID))
                {
                    return EntityType.Mineral;
                }

                // PRIORIDAD 6: Items recogibles
                if (gameObject.GetComponent("PickupItem") != null ||
                    gameObject.GetComponent("DroppedItem") != null)
                {
                    return EntityType.Item;
                }

                // PRIORIDAD 7: NPCs y mercaderes
                if (objectName.Contains("merchant") ||
                    objectName.Contains("npc") ||
                    gameObject.GetComponent("Merchant") != null)
                {
                    return EntityType.NPC;
                }

                // PRIORIDAD 8: Decoración (incluyendo estatuas)
                if (isStatue ||
                    gameObject.GetComponent("Decoration") != null ||
                    !IsEntityInteractable(entityMono))
                {
                    return EntityType.Decoration;
                }

                return EntityType.Unknown;
            }
            catch
            {
                return EntityType.Unknown;
            }
        }

        /// <summary>
        /// Verifica si un ObjectID es un enemigo REAL (no decorativo).
        /// </summary>
        private static bool IsRealEnemy(ObjectID objectID)
        {
            // Excluir estatuas y trofeos
            var name = objectID.ToString().ToLower();
            if (name.Contains("statue") || name.Contains("trophy"))
                return false;

            // Lista de IDs de enemigos reales
            switch (objectID)
            {
                // Slimes reales
                case ObjectID.Slime:
                case ObjectID.SlimeBlob:
                case ObjectID.AggressiveSlimeBlob:
                case ObjectID.PoisonSlime:
                case ObjectID.PoisonSlimeBlob:
                case ObjectID.SlipperySlime:
                case ObjectID.SlipperySlimeBlob:
                case ObjectID.LavaSlime:
                case ObjectID.LavaSlimeBlob:
                case ObjectID.RoyalSlimeBlob:
                // Jefes Slime
                case ObjectID.SlimeBoss:
                case ObjectID.PoisonSlimeBoss:
                case ObjectID.SlipperySlimeBoss:
                case ObjectID.KingSlime:
                case ObjectID.LavaSlimeBoss:
                // Larvas
                case ObjectID.Larva:
                case ObjectID.BigLarva:
                case ObjectID.AcidLarva:
                case ObjectID.BossLarva:
                case ObjectID.LarvaHiveBoss:
                case ObjectID.LarvaHiveHalloweenBoss:
                // Cavelings
                case ObjectID.Caveling:
                case ObjectID.CavelingShaman:
                case ObjectID.CavelingGardener:
                case ObjectID.CavelingHunter:
                case ObjectID.InfectedCaveling:
                case ObjectID.CavelingBrute:
                case ObjectID.CavelingScholar:
                case ObjectID.CavelingAssassin:
                case ObjectID.CavelingMummy:
                case ObjectID.CavelingSkirmisher:
                case ObjectID.CavelingSpearman:
                // Otros enemigos
                case ObjectID.MushroomEnemy:
                case ObjectID.MushroomBrute:
                case ObjectID.SnarePlant:
                case ObjectID.SmallTentacle:
                case ObjectID.MoldTentacle:
                case ObjectID.CrabEnemy:
                case ObjectID.BombScarab:
                case ObjectID.GoldenBombScarab:
                case ObjectID.LavaButterfly:
                case ObjectID.DesertBrute:
                case ObjectID.CrystalBigSnail:
                case ObjectID.OrbitalTurret:
                case ObjectID.Mimite:
                // Cigarras
                case ObjectID.NatureCicadaEnemy:
                case ObjectID.DesertCicadaEnemy:
                case ObjectID.CicadaNymph:
                // Jefes principales
                case ObjectID.ShamanBoss:
                case ObjectID.BirdBoss:
                case ObjectID.OctopusBoss:
                case ObjectID.ScarabBoss:
                case ObjectID.CoreBoss:
                case ObjectID.WallBoss:
                case ObjectID.GiantCicadaBoss:
                case ObjectID.HydraBossNature:
                case ObjectID.HydraBossSea:
                case ObjectID.HydraBossDesert:
                    return true;

                default:
                    // Verificar patrones genéricos para enemigos no listados
                    return (name.Contains("enemy") ||
                            name.Contains("boss") ||
                            name.Contains("worm") ||
                            name.Contains("segment")) &&
                           !name.Contains("statue") &&
                           !name.Contains("trophy");
            }
        }

        /// <summary>
        /// Verifica si un ObjectID es un enemigo conocido.
        /// </summary>
        private static bool IsEnemyObjectID(ObjectID objectID)
        {
            return GameDatabase.IsEnemy(objectID);
        }

        /// <summary>
        /// Verifica si un ObjectID es un cofre conocido.
        /// </summary>
        private static bool IsChestObjectID(ObjectID objectID)
        {
            return GameDatabase.IsChest(objectID);
        }

        /// <summary>
        /// Verifica si un ObjectID es un mineral conocido.
        /// </summary>
        private static bool IsMineralObjectID(ObjectID objectID)
        {
            return GameDatabase.IsOre(objectID);
        }

        /// <summary>
        /// Verifica si un ObjectID corresponde a una estación de crafteo.
        /// </summary>
        private static bool IsCraftingStation(ObjectID objectID)
        {
            return GameDatabase.IsCraftingStation(objectID);
        }

        /// <summary>
        /// Verifica si una entidad es interactuable.
        /// </summary>
        private static bool IsEntityInteractable(PugOther.EntityMonoBehaviour entityMono)
        {
            try
            {
                // Buscar componentes que indiquen interactividad
                var gameObject = entityMono.gameObject;
                return gameObject.GetComponent("Interactable") != null ||
                       gameObject.GetComponent("Inventory") != null ||
                       gameObject.GetComponent("CraftingStation") != null ||
                       gameObject.GetComponent("Workbench") != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifica si una entidad se puede recoger.
        /// </summary>
        private static bool IsEntityPickupable(PugOther.EntityMonoBehaviour entityMono)
        {
            try
            {
                var gameObject = entityMono.gameObject;
                var objectID = GetEntityObjectID(entityMono);
                return gameObject.GetComponent("PickupItem") != null ||
                       gameObject.GetComponent("DroppedItem") != null ||
                       ObjectTypeHelper.IsResource(objectID) ||
                       ObjectTypeHelper.IsFood(objectID) ||
                       ObjectTypeHelper.IsTool(objectID);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifica si una entidad es destructible.
        /// </summary>
        private static bool IsEntityDestructible(PugOther.EntityMonoBehaviour entityMono)
        {
            try
            {
                var gameObject = entityMono.gameObject;
                return gameObject.GetComponent("Health") != null ||
                       gameObject.GetComponent("Destructible") != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifica si el sistema de mapas está disponible.
        /// </summary>
        private static bool IsMapAvailable()
        {
            return PugOther.Manager.multiMap != null;
        }

        /// <summary>
        /// Obtiene el ObjectID de una entidad usando reflexión para acceder al ObjectDataCD.
        /// </summary>
        private static ObjectID GetEntityObjectID(PugOther.EntityMonoBehaviour entityMono)
        {
            return GetEntityObjectIDSafe(entityMono);
        }

        /// <summary>
        /// Obtiene el ObjectID de una entidad usando reflexión segura.
        /// </summary>
        private static ObjectID GetEntityObjectIDSafe(PugOther.EntityMonoBehaviour entityMono)
        {
            try
            {
                if (entityMono.entity != Unity.Entities.Entity.Null && entityMono.world != null)
                {
                    var entityManager = entityMono.world.EntityManager;

                    // Usar reflexión para acceder al tipo ObjectDataCD
                    var pugOtherAssembly = typeof(PugOther.Manager).Assembly;
                    var objectDataCDType = pugOtherAssembly.GetType("ObjectDataCD");

                    if (objectDataCDType != null)
                    {
                        // Usar reflexión para llamar HasComponent<ObjectDataCD>
                        var hasComponentMethod = typeof(EntityManager).GetMethod("HasComponent")
                            .MakeGenericMethod(objectDataCDType);
                        var hasComponent = (bool)hasComponentMethod.Invoke(entityManager, new object[] { entityMono.entity });

                        if (hasComponent)
                        {
                            // Usar reflexión para llamar GetComponentData<ObjectDataCD>
                            var getComponentMethod = typeof(EntityManager).GetMethod("GetComponentData")
                                .MakeGenericMethod(objectDataCDType);
                            var componentData = getComponentMethod.Invoke(entityManager, new object[] { entityMono.entity });

                            // Obtener el campo objectID usando reflexión
                            var objectIDField = objectDataCDType.GetField("objectID");
                            if (objectIDField != null)
                            {
                                return (ObjectID)objectIDField.GetValue(componentData);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RealWorldMapReader] Error obteniendo ObjectID mediante reflexión: {e.Message}");
            }
            return ObjectID.None;
        }

        /// <summary>
        /// Genera una descripción completa y real de la posición.
        /// </summary>
        public static string GetRealPositionDescription(Vector3 worldPosition, DetailLevel detailLevel = DetailLevel.Standard)
        {
            try
            {
                var info = ReadRealPosition(worldPosition);
                return GenerateDescription(info, detailLevel);
            }
            catch (Exception e)
            {
                Debug.LogError($"[RealWorldMapReader] Error generando descripción: {e.Message}");
                return "Error leyendo posición";
            }
        }

        /// <summary>
        /// Genera una descripción según el nivel de detalle CON SISTEMA DE PRIORIDADES MEJORADO.
        /// </summary>
        private static string GenerateDescription(RealWorldPositionInfo info, DetailLevel detailLevel)
        {
            // PRIORIDAD 1: Objetos importantes en la posición exacta
            if (info.EntitiesInfo.EntitiesAtExactPosition.Count > 0)
            {
                // Ordenar entidades por importancia
                var sortedEntities = info.EntitiesInfo.EntitiesAtExactPosition
                    .OrderBy(e => GetEntityPriority(e))
                    .ToList();

                var primaryEntity = sortedEntities[0];
                var description = GetEnhancedEntityDescription(primaryEntity, detailLevel);

                // Si hay más objetos en la misma posición y el detalle es alto, mencionarlos
                if (sortedEntities.Count > 1 && detailLevel >= DetailLevel.Detailed)
                {
                    description += $" (y {sortedEntities.Count - 1} objeto(s) más)";
                }

                return description;
            }

            // PRIORIDAD 2: Tiles bloqueantes (paredes, obstáculos)
            if (info.TileInfo.HasValidTile && info.TileInfo.IsBlocking)
            {
                return GetEnhancedTileDescription(info.TileInfo, detailLevel, true);
            }

            // PRIORIDAD 3: Tiles normales del suelo
            if (info.TileInfo.HasValidTile)
            {
                return GetEnhancedTileDescription(info.TileInfo, detailLevel, false);
            }

            // Si no hay nada en la posición exacta
            return "Vacío";
        }

        /// <summary>
        /// Obtiene la prioridad de una entidad (menor = mayor prioridad).
        /// </summary>
        private static int GetEntityPriority(RealEntityInfo entity)
        {
            switch (entity.EntityType)
            {
                case EntityType.Core: return 0;           // Máxima prioridad
                case EntityType.Enemy: return 1;          // Enemigos reales
                case EntityType.NPC: return 2;            // NPCs y mercaderes
                case EntityType.CraftingStation: return 3; // Estaciones de trabajo
                case EntityType.Storage: return 4;        // Cofres
                case EntityType.Item: return 5;           // Items recogibles
                case EntityType.Mineral: return 6;        // Minerales
                case EntityType.Decoration: return 7;     // Decoración y estatuas
                default: return 8;                        // Desconocido
            }
        }

        /// <summary>
        /// Genera una descripción mejorada de una entidad con más detalles contextuales.
        /// </summary>
        private static string GetEnhancedEntityDescription(RealEntityInfo entity, DetailLevel detailLevel)
        {
            var entityDesc = entity.ObjectName;

            switch (entity.EntityType)
            {
                case EntityType.Core:
                    // El Core siempre es importante
                    if (entity.ObjectID == ObjectID.TheCore)
                    {
                        entityDesc = "El Núcleo";
                        if (detailLevel >= DetailLevel.Standard)
                            entityDesc += " [Objetivo Principal]";
                    }
                    else
                    {
                        entityDesc = entity.ObjectName;
                        if (detailLevel >= DetailLevel.Standard)
                            entityDesc += " [Núcleo]";
                    }
                    break;

                case EntityType.Enemy:
                    // Enemigos reales (no estatuas)
                    entityDesc = entity.ObjectName;
                    if (detailLevel >= DetailLevel.Standard)
                    {
                        entityDesc += " [Enemigo]";
                    }
                    break;

                case EntityType.CraftingStation:
                    // Estaciones de trabajo
                    entityDesc = entity.ObjectName;
                    if (detailLevel >= DetailLevel.Standard)
                    {
                        var stationType = GetCraftingStationType(entity.ObjectID);
                        if (!string.IsNullOrEmpty(stationType))
                            entityDesc += $" [{stationType}]";
                    }
                    break;

                case EntityType.Storage:
                    // Cofres y almacenamiento
                    entityDesc = entity.ObjectName;
                    if (detailLevel >= DetailLevel.Standard)
                    {
                        if (entity.ObjectName.Contains("Cerrado") || entity.ObjectName.Contains("Locked"))
                            entityDesc += " [Requiere llave]";
                        else
                            entityDesc += " [Almacenamiento]";
                    }
                    break;

                case EntityType.Mineral:
                    // Minerales y recursos
                    entityDesc = GetMineralDescription(entity.ObjectID, detailLevel);
                    break;

                case EntityType.Item:
                    // Items recogibles
                    entityDesc = entity.ObjectName;
                    if (entity.Amount > 1)
                        entityDesc += $" x{entity.Amount}";
                    if (detailLevel >= DetailLevel.Standard)
                        entityDesc += " [Objeto]";
                    break;

                case EntityType.NPC:
                    // NPCs y mercaderes
                    entityDesc = entity.ObjectName;
                    if (detailLevel >= DetailLevel.Standard)
                        entityDesc += " [NPC]";
                    break;

                case EntityType.Decoration:
                    // Decoración (incluyendo estatuas de jefes)
                    entityDesc = entity.ObjectName;
                    if (detailLevel >= DetailLevel.Standard)
                    {
                        if (entity.ObjectName.Contains("Estatua") || entity.ObjectName.Contains("Statue"))
                            entityDesc += " [Decoración]";
                        else if (entity.ObjectName.Contains("Trofeo") || entity.ObjectName.Contains("Trophy"))
                            entityDesc += " [Trofeo]";
                    }
                    break;

                default:
                    // Objetos desconocidos
                    entityDesc = entity.ObjectName;
                    if (entity.Amount > 1)
                        entityDesc += $" x{entity.Amount}";
                    if (detailLevel >= DetailLevel.Standard && !string.IsNullOrEmpty(entity.Category))
                        entityDesc += $" [{entity.Category}]";
                    break;
            }

            // Información de interacción para nivel detallado
            if (detailLevel >= DetailLevel.Detailed)
            {
                if (entity.EntityType == EntityType.Enemy)
                {
                    entityDesc += " (Atacar)";
                }
                else if (entity.IsInteractable)
                {
                    entityDesc += " (Interactuar: E)";
                }
                else if (entity.IsPickupable)
                {
                    entityDesc += " (Recoger: E)";
                }
            }

            return entityDesc;
        }

        /// <summary>
        /// Obtiene una descripción de distancia amigable.
        /// </summary>
        private static string GetDistanceDescription(float distance)
        {
            if (distance < 1.5f) return "Muy cerca";
            if (distance < 3f) return "Cerca";
            if (distance < 6f) return "Media distancia";
            if (distance < 10f) return "Lejos";
            return "Muy lejos";
        }

        /// <summary>
        /// Obtiene una descripción específica para minerales.
        /// </summary>
        private static string GetMineralDescription(ObjectID objectID, DetailLevel detailLevel)
        {
            switch (objectID)
            {
                case ObjectID.CopperOre:
                case ObjectID.CopperOreBoulder:
                    return detailLevel >= DetailLevel.Standard ? "Mineral de Cobre [Recurso]" : "Mineral de Cobre";
                case ObjectID.TinOre:
                case ObjectID.TinOreBoulder:
                    return detailLevel >= DetailLevel.Standard ? "Mineral de Estaño [Recurso]" : "Mineral de Estaño";
                case ObjectID.IronOre:
                case ObjectID.IronOreBoulder:
                    return detailLevel >= DetailLevel.Standard ? "Mineral de Hierro [Recurso]" : "Mineral de Hierro";
                default:
                    var name = ObjectTypeHelper.GetLocalizedName(objectID);
                    return detailLevel >= DetailLevel.Standard ? $"{name} [Mineral]" : name;
            }
        }

        /// <summary>
        /// Genera una descripción mejorada de un tile con información de material.
        /// </summary>
        private static string GetEnhancedTileDescription(RealTileInfo tileInfo, DetailLevel detailLevel, bool isBlocking)
        {
            var tileDesc = "";

            // Para tiles de mineral (ore), dar nombre específico del material
            if (tileInfo.TopTile.tileType == TileType.ore)
            {
                tileDesc = GetOreDescription(tileInfo.TopTile, tileInfo.TilesetName, detailLevel);
            }
            else
            {
                // Usar la descripción inteligente existente
                tileDesc = GenerateSmartTileDescription(tileInfo, detailLevel);
            }

            // Agregar información adicional según el contexto
            if (isBlocking)
            {
                if (tileInfo.IsDamageable)
                {
                    if (detailLevel >= DetailLevel.Standard)
                    {
                        tileDesc += " [Destructible]";
                        if (detailLevel >= DetailLevel.Detailed && !string.IsNullOrEmpty(tileInfo.RecommendedTool))
                        {
                            tileDesc += $" (Usar: {tileInfo.RecommendedTool})";
                        }
                    }
                }
                else if (detailLevel >= DetailLevel.Standard)
                {
                    tileDesc += " [Bloqueante]";
                }
            }

            if (tileInfo.IsDangerous && detailLevel >= DetailLevel.Standard)
            {
                tileDesc += " [Peligroso]";
            }

            return tileDesc;
        }

        /// <summary>
        /// Obtiene una descripción específica para minerales basada en el tileset.
        /// </summary>
        private static string GetOreDescription(TileInfo tile, string tilesetName, DetailLevel detailLevel)
        {
            // En Core Keeper, los minerales usan tilesets específicos según el bioma
            // El tileset index puede ayudar a identificar el tipo de mineral
            var tilesetIndex = tile.tileset;
            var oreName = "";

            // Identificar mineral por índice de tileset y patrones comunes
            switch (tilesetIndex)
            {
                case 0:  // Dirt - Minerales básicos del inicio (Cobre)
                case 1:  // Stone - Zona de piedra (Cobre/Estaño)
                    oreName = DetermineBasicOreType(tile);
                    break;
                case 2:  // Obsidian - Zona de obsidiana (Hierro)
                    oreName = "Mineral de hierro";
                    break;
                case 6:  // LarvaHive - Zona de panal (Estaño)
                    oreName = "Mineral de estaño";
                    break;
                case 9:  // Mold - Zona de moho (Hierro)
                    oreName = "Mineral de hierro";
                    break;
                case 10: // Sea - Zona marina (Hierro/Escarlata)
                    oreName = "Mineral escarlata";
                    break;
                case 11: // Clay - Zona de arcilla (Estaño/Cobre)
                    oreName = DetermineBasicOreType(tile);
                    break;
                case 26: // Desert - Zona del desierto (Escarlata)
                case 27: // DesertTemple
                    oreName = "Mineral escarlata";
                    break;
                case 36: // Snow - Zona de nieve (Octarina)
                    oreName = "Mineral de octarina";
                    break;
                case 59: // DarkStone - Zona oscura (Galaxita)
                case 61: // Alien - Zona alienígena (Galaxita)
                    oreName = "Mineral de galaxita";
                    break;
                default:
                    // Intentar detectar por el nombre del tileset
                    oreName = tilesetName?.ToLower() switch
                    {
                        var s when s.Contains("copper") => "Mineral de cobre",
                        var s when s.Contains("tin") || s.Contains("estaño") => "Mineral de estaño",
                        var s when s.Contains("iron") || s.Contains("hierro") => "Mineral de hierro",
                        var s when s.Contains("scarlet") || s.Contains("escarlata") => "Mineral escarlata",
                        var s when s.Contains("octarine") || s.Contains("octarina") => "Mineral de octarina",
                        var s when s.Contains("galaxite") || s.Contains("galaxita") => "Mineral de galaxita",
                        _ => "Mineral"
                    };
                    break;
            }

            // Si aún no identificamos el mineral, usar nombre genérico con detalles
            if (string.IsNullOrEmpty(oreName) || oreName == "Mineral")
            {
                if (detailLevel >= DetailLevel.Detailed && !string.IsNullOrEmpty(tilesetName))
                {
                    oreName = $"Mineral ({tilesetName})";
                }
                else
                {
                    oreName = "Mineral";
                }
            }

            // Añadir información adicional según el nivel de detalle
            if (detailLevel >= DetailLevel.Standard)
            {
                oreName += " [Minable]";
            }

            return oreName;
        }

        /// <summary>
        /// Determina el tipo de mineral básico (cobre o estaño) basándose en patrones.
        /// </summary>
        private static string DetermineBasicOreType(TileInfo tile)
        {
            // En las zonas iniciales, alternan entre cobre y estaño
            // Esto es una aproximación ya que el juego usa generación procedural
            // Podríamos usar la posición o algún otro factor para determinar
            return "Mineral de cobre"; // Por defecto cobre en zonas básicas
        }

        /// <summary>
        /// Determina el tipo de estación de crafteo basado en el ObjectID.
        /// </summary>
        private static string GetCraftingStationType(ObjectID objectID)
        {
            var name = objectID.ToString().ToLower();

            if (name.Contains("workbench")) return "Mesa de trabajo";
            if (name.Contains("anvil")) return "Yunque";
            if (name.Contains("furnace")) return "Horno";
            if (name.Contains("cookingpot")) return "Olla de cocina";
            if (name.Contains("alchemytable")) return "Mesa de alquimia";
            if (name.Contains("paintershop")) return "Taller de pintura";
            if (name.Contains("salvageandrepair")) return "Estación de reciclaje";
            if (name.Contains("carpenterstable")) return "Mesa de carpintero";
            if (name.Contains("railwayforge")) return "Forja ferroviaria";

            return "Estación de crafteo";
        }

        /// <summary>
        /// Verifica si un ObjectID es un mineral/ore.
        /// </summary>
        private static bool IsOre(ObjectID objectID)
        {
            var name = objectID.ToString().ToLower();
            return name.Contains("ore") ||
                   name.EndsWith("bar") ||
                   name.Contains("ingot");
        }

        /// <summary>
        /// Genera una descripción inteligente del tile combinando tipo y material del tileset.
        /// OPTIMIZADO: Usa mapeos directos para máximo rendimiento.
        /// </summary>
        private static string GenerateSmartTileDescription(RealTileInfo tileInfo, DetailLevel detailLevel)
        {
            var tileName = tileInfo.TileName;
            var tilesetName = tileInfo.TilesetName;

            // Si no hay información de tileset válida, usar solo el nombre del tile
            if (string.IsNullOrEmpty(tilesetName) ||
                tilesetName == "Tierra" ||  // tileset 0 por defecto
                tilesetName.StartsWith("Material"))
            {
                return tileName;
            }

            // OPTIMIZADO: Mapeo directo de materiales específicos basado en enum real
            switch (tilesetName.ToLower())
            {
                case "arena":           // Tileset 12 - Sand
                    return GetMaterialDescription(tileName, "Arena");

                case "piedra":          // Tileset 1 - Stone
                    return GetMaterialDescription(tileName, "Piedra");

                case "obsidiana":       // Tileset 2 - Obsidian
                    return GetMaterialDescription(tileName, "Obsidiana");

                case "lava":            // Tileset 3 - Lava
                    return GetMaterialDescription(tileName, "Lava");

                case "naturaleza":      // Tileset 8 - Nature
                    return GetMaterialDescription(tileName, "Naturaleza");

                case "moho":            // Tileset 9 - Mold
                    return GetMaterialDescription(tileName, "Moho");

                case "mar":             // Tileset 10 - Sea
                    return GetMaterialDescription(tileName, "Mar");

                case "arcilla":         // Tileset 11 - Clay
                    return GetMaterialDescription(tileName, "Arcilla");

                case "césped":          // Tileset 13 - Turf
                    return GetMaterialDescription(tileName, "Césped");

                case "desierto":        // Tileset 26 - Desert
                    return GetMaterialDescription(tileName, "Desierto");

                case "nieve":           // Tileset 31 - Snow
                    return GetMaterialDescription(tileName, "Nieve");

                case "cristal":         // Tileset 34/60 - Glass/Crystal
                    return GetMaterialDescription(tileName, "Cristal");

                case "pradera":         // Tileset 35 - Meadow
                    return GetMaterialDescription(tileName, "Pradera");

                case "explosivo":       // Tileset 36 - Explosive
                    return GetMaterialDescription(tileName, "Explosivo");

                case "piedra oscura":   // Tileset 59 - DarkStone
                    return GetMaterialDescription(tileName, "Piedra oscura");

                case "alienígena":      // Tileset 61 - Alien
                    return GetMaterialDescription(tileName, "Alienígena");

                case "oasis":           // Tileset 71 - Oasis
                    return GetMaterialDescription(tileName, "Oasis");

                default:
                    // Para materiales de construcción u otros, usar formato detallado
                    return detailLevel >= DetailLevel.Standard ?
                        $"{tileName} ({tilesetName})" : tileName;
            }
        }

        /// <summary>
        /// OPTIMIZADO: Genera descripción específica de material sin múltiples contains().
        /// </summary>
        private static string GetMaterialDescription(string tileName, string material)
        {
            var lowerTileName = tileName.ToLower();

            // Casos especiales para Arena - decir directamente "Arena" en lugar de "Tierra de arena"
            if (material == "Arena" && (lowerTileName.Contains("tierra") || lowerTileName.Contains("ground")))
            {
                return "Arena";
            }

            // Para otros materiales de tierra/suelo
            if (lowerTileName.Contains("tierra") || lowerTileName.Contains("ground") || lowerTileName.Contains("suelo"))
            {
                return material switch
                {
                    "Piedra" => "Tierra de piedra",
                    "Madera" => "Suelo de madera",
                    "Lava" => "Tierra volcánica",
                    "Corrupto" => "Tierra corrupta",
                    "Hielo" => "Tierra helada",
                    "Limo" => "Tierra viscosa",
                    "Cristal" => "Tierra cristalina",
                    "Naturaleza" => "Tierra fértil",
                    "Volcánico" => "Tierra volcánica",
                    "Oasis" => "Tierra de oasis",
                    _ => $"Tierra de {material.ToLower()}"
                };
            }

            // Para paredes
            if (lowerTileName.Contains("pared") || lowerTileName.Contains("wall"))
            {
                return $"Pared de {material.ToLower()}";
            }

            // Para otros tiles, combinación simple
            return $"{tileName} de {material.ToLower()}";
        }

        /// <summary>
        /// Obtiene dirección relativa simple.
        /// </summary>
        private static string GetDirection(Vector3 offset)
        {
            if (Mathf.Abs(offset.x) > Mathf.Abs(offset.z))
            {
                return offset.x > 0 ? "Este" : "Oeste";
            }
            else
            {
                return offset.z > 0 ? "Norte" : "Sur";
            }
        }

        /// <summary>
        /// Niveles de detalle para las descripciones.
        /// </summary>
        public enum DetailLevel
        {
            Brief,      // Solo lo esencial
            Standard,   // Información estándar
            Detailed,   // Información detallada
            Complete    // Información completa
        }
    }
}