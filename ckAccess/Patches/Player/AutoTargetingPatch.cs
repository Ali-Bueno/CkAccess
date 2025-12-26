extern alias PugOther;
extern alias Core;
extern alias PugComps;
using HarmonyLib;
using ckAccess.Localization;
using ckAccess.Patches.UI;
using ckAccess.Helpers;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Vector3 = Core::UnityEngine.Vector3;
using PugTilemap;

namespace ckAccess.Patches.Player
{
    /// <summary>
    /// Sistema de auto-targeting que automaticamente apunta a enemigos cercanos
    /// independiente del tipo de input (teclado, ratón, mando)
    /// </summary>
    [HarmonyPatch]
    public static class AutoTargetingPatch
    {
        // Configuración del sistema
        private const float AUTO_TARGET_BASE_RANGE = 5f; // Rango base para auto-targeting
        private const float MELEE_WEAPON_RANGE = 3f; // Rango para armas cuerpo a cuerpo
        private const float RANGED_WEAPON_RANGE = 10f; // Rango para armas a distancia
        private const float MAGIC_WEAPON_RANGE = 8f; // Rango para armas mágicas
        private const float TOOL_RANGE = 2f; // Rango para herramientas
        private const int FRAMES_BETWEEN_SCANS = 5; // MEJORADO: Escanear cada 5 frames (aprox 80ms) para mejor respuesta

        // Estado del sistema - SIEMPRE ACTIVO
        private static bool _systemEnabled = true; // Siempre activo, no se puede desactivar
        private static PugOther.EntityMonoBehaviour _currentTarget = null;
        private static Vector3 _lastTargetPosition = Vector3.zero;
        private static int _frameCounter = 0;
        private static List<EnemyTarget> _nearbyEnemies = new List<EnemyTarget>();

        // Estado para anuncios TTS
        private static HashSet<PugOther.EntityMonoBehaviour> _previouslyInRangeEnemies = new HashSet<PugOther.EntityMonoBehaviour>();
        private static float _lastAnnouncementTime = 0f;
        private const float ANNOUNCEMENT_COOLDOWN = 1.5f; // Cooldown entre anuncios para evitar spam

        // Estructura para almacenar información de enemigos
        private struct EnemyTarget
        {
            public PugOther.EntityMonoBehaviour entity;
            public Vector3 position;
            public float distance;
            public string name;
        }

        /// <summary>
        /// Parche en el sistema de ataque para redirigir automáticamente hacia el enemigo más cercano
        /// MULTIPLAYER-SAFE: Solo procesa el jugador local
        /// </summary>
        [HarmonyPatch(typeof(PugOther.PlayerController), "ManagedUpdate")]
        [HarmonyPostfix]
        public static void ManagedUpdate_Postfix(PugOther.PlayerController __instance)
        {
            try
            {
                if (!_systemEnabled) return;

                // MULTIPLAYER: Solo procesar si este es el jugador local
                if (!LocalPlayerHelper.IsLocalPlayer(__instance))
                    return;

                // Actualizar lista de enemigos cercanos
                _frameCounter++;
                if (_frameCounter >= FRAMES_BETWEEN_SCANS)
                {
                    _frameCounter = 0;
                    UpdateNearbyEnemies(__instance);
                }

                // Verificar si el objetivo actual sigue siendo válido
                ValidateCurrentTarget(__instance);

                // NUEVO: Redirigir continuamente si hay un objetivo válido
                // Esto asegura que el jugador siempre esté apuntando al enemigo
                if (_currentTarget != null && IsValidTarget(_currentTarget))
                {
                    RedirectAttackToTarget(__instance, _currentTarget);
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error en AutoTargeting ManagedUpdate: {ex}");
            }
        }


        /// <summary>
        /// Actualiza la lista de enemigos cercanos y selecciona el mejor objetivo
        /// </summary>
        private static void UpdateNearbyEnemies(PugOther.PlayerController player)
        {
            try
            {
                _nearbyEnemies.Clear();

                if (!TryGetPlayerPosition(player, out Vector3 playerPos))
                    return;

                // Buscar enemigos en el área
                var entityLookup = PugOther.Manager.memory?.entityMonoLookUp;
                if (entityLookup == null) return;

                foreach (var kvp in entityLookup)
                {
                    var entity = kvp.Value;
                    if (entity?.gameObject?.activeInHierarchy != true) continue;

                    // Verificar si es un enemigo
                    if (!IsEnemyEntity(entity)) continue;

                    var entityPos = entity.WorldPosition;
                    var entityWorldPos = new Vector3(entityPos.x, entityPos.y, entityPos.z);
                    var distance = Vector3.Distance(playerPos, entityWorldPos);

                    // Determinar rango efectivo basado en el arma equipada
                    float effectiveRange = CalculateEffectiveRange(player);

                    // Solo considerar enemigos dentro del rango efectivo
                    if (distance <= effectiveRange)
                    {
                        // MEJORA CRÍTICA: Verificar línea de visión (LOS)
                        // Esto evita apuntar a enemigos a través de paredes
                        if (HasLineOfSight(playerPos, entityWorldPos))
                        {
                            var enemy = new EnemyTarget
                            {
                                entity = entity,
                                position = entityWorldPos,
                                distance = distance,
                                name = GetEntityName(entity)
                            };

                            _nearbyEnemies.Add(enemy);
                        }
                    }
                }

                // Seleccionar el mejor objetivo (más cercano por ahora)
                SelectBestTarget();

                // Procesar anuncios TTS de enemigos que entran/salen del rango
                ProcessEnemyRangeAnnouncements();
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error actualizando enemigos cercanos: {ex}");
            }
        }

        /// <summary>
        /// Verifica si hay línea de visión clara entre dos puntos (sin paredes)
        /// Usa algoritmo de Bresenham sobre el grid de tiles
        /// </summary>
        private static bool HasLineOfSight(Vector3 start, Vector3 end)
        {
            try
            {
                var map = PugOther.Manager.multiMap;
                if (map == null) return true; // Fallback seguro

                // Convertir a coordenadas de tile
                int x0 = (int)math.round(start.x);
                int z0 = (int)math.round(start.z);
                int x1 = (int)math.round(end.x);
                int z1 = (int)math.round(end.z);

                // Algoritmo de Bresenham para trazar línea
                int dx = System.Math.Abs(x1 - x0);
                int dz = System.Math.Abs(z1 - z0);
                int sx = x0 < x1 ? 1 : -1;
                int sz = z0 < z1 ? 1 : -1;
                int err = dx - dz;

                // Límite de seguridad para evitar bucles infinitos (max 50 tiles)
                int safetyCounter = 0;
                while (true)
                {
                    if (safetyCounter++ > 50) break;

                    // Verificar tile actual
                    var tile = map.GetTileLayerLookup().GetTopTile(new int2(x0, z0));
                    
                    if (IsVisionBlocking(tile.tileType))
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
        /// Determina si un tipo de tile bloquea la visión/proyectiles
        /// </summary>
        private static bool IsVisionBlocking(TileType type)
        {
            switch (type)
            {
                case TileType.wall:
                case TileType.greatWall:
                case TileType.thinWall:
                case TileType.ore:
                case TileType.ancientCrystal:
                case TileType.bigRoot:
                case TileType.chrysalis:
                    return true;
                
                // Tiles que bloquean movimiento pero NO visión (agujeros, vallas, agua)
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
        /// Selecciona el mejor objetivo de la lista de enemigos cercanos
        /// </summary>
        private static void SelectBestTarget()
        {
            if (_nearbyEnemies.Count == 0)
            {
                _currentTarget = null;
                return;
            }

            // Por ahora, seleccionar el más cercano
            // TODO: Se puede mejorar con prioridades (por tipo, salud, etc.)
            var sortedEnemies = _nearbyEnemies.OrderBy(e => e.distance).ToList();
            var newTarget = sortedEnemies[0].entity;

            // Solo cambiar objetivo si es significativamente mejor o el actual es inválido
            if (_currentTarget == null || !IsValidTarget(_currentTarget) ||
                sortedEnemies[0].distance < GetDistanceToCurrentTarget() - 1f)
            {
                var previousTarget = _currentTarget;
                _currentTarget = newTarget;
                _lastTargetPosition = sortedEnemies[0].position;

                // Anunciar nuevo objetivo solo si cambio y está habilitado el sistema
                if (_systemEnabled && previousTarget != newTarget)
                {
                    string cleanName = GetCleanEnemyName(newTarget);
                    int distance = (int)math.round(sortedEnemies[0].distance);
                    UIManager.Speak($"Objetivo: {cleanName} a {distance} tiles");
                }
            }
        }

        /// <summary>
        /// Redirecciona el ataque hacia el objetivo seleccionado
        /// SIMPLIFICADO: Solo actualiza la dirección de apuntado de manera continua
        /// </summary>
        private static void RedirectAttackToTarget(PugOther.PlayerController player, PugOther.EntityMonoBehaviour target)
        {
            try
            {
                if (!TryGetPlayerPosition(player, out Vector3 playerPos))
                    return;

                var targetPos = target.WorldPosition;
                var targetWorldPos = new Vector3(targetPos.x, targetPos.y, targetPos.z);

                // Calcular dirección hacia el objetivo
                var direction = (targetWorldPos - playerPos).normalized;
                var direction2D = new float3(direction.x, 0f, direction.z);

                // CORE: Actualizar la dirección de apuntado del jugador
                // Esto es lo más importante y funciona para todos los tipos de armas
                player.targetingDirection = direction2D;

                // Actualizar posición del último objetivo para caching
                _lastTargetPosition = targetWorldPos;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error redirigiendo ataque: {ex}");
            }
        }

        /// <summary>
        /// Calcula el rango efectivo basado en el arma equipada
        /// </summary>
        public static float CalculateEffectiveRange(PugOther.PlayerController player)
        {
            try
            {
                // Obtener el item equipado
                var equippedSlot = player.GetEquippedSlot();

                // Obtener información del objeto equipado
                var inventoryHandler = player.playerInventoryHandler;
                if (inventoryHandler == null)
                {
                    return AUTO_TARGET_BASE_RANGE;
                }

                // Convertir EquipmentSlot a índice si es necesario
                int slotIndex = 0;
                try
                {
                    // Intentar obtener el índice del slot equipado
                    if (equippedSlot != null)
                    {
                        // GetEquippedSlot devuelve un EquipmentSlot, necesitamos el índice del hotbar
                        // Por ahora, usar el primer slot del hotbar
                        for (int i = 0; i < 10; i++)
                        {
                            var data = inventoryHandler.GetContainedObjectData(i);
                            if (data.objectID != ObjectID.None)
                            {
                                slotIndex = i;
                                break;
                            }
                        }
                    }
                }
                catch { }

                var itemData = inventoryHandler.GetContainedObjectData(slotIndex);
                if (itemData.objectID == ObjectID.None)
                {
                    return TOOL_RANGE;
                }

                // Detectar tipo de arma basándose en el ObjectID y componentes
                return DetermineWeaponRange(itemData.objectID);
            }
            catch
            {
                return AUTO_TARGET_BASE_RANGE; // Fallback
            }
        }

        /// <summary>
        /// Determina el rango del arma basándose en su ObjectID y tipo
        /// </summary>
        private static float DetermineWeaponRange(ObjectID objectID)
        {
            try
            {
                string itemName = objectID.ToString().ToLower();

                // Armas a distancia (mayor rango)
                if (itemName.Contains("bow") || itemName.Contains("crossbow") ||
                    itemName.Contains("gun") || itemName.Contains("rifle") ||
                    itemName.Contains("slingshot") || itemName.Contains("blowpipe"))
                {
                    return RANGED_WEAPON_RANGE;
                }

                // Armas mágicas (rango medio)
                if (itemName.Contains("staff") || itemName.Contains("wand") ||
                    itemName.Contains("scepter") || itemName.Contains("orb") ||
                    itemName.Contains("tome") || itemName.Contains("book") ||
                    itemName.Contains("crystal") || itemName.Contains("rune"))
                {
                    return MAGIC_WEAPON_RANGE;
                }

                // Armas cuerpo a cuerpo (rango corto)
                if (itemName.Contains("sword") || itemName.Contains("axe") ||
                    itemName.Contains("mace") || itemName.Contains("hammer") ||
                    itemName.Contains("spear") || itemName.Contains("dagger") ||
                    itemName.Contains("club") || itemName.Contains("blade") ||
                    itemName.Contains("scythe") || itemName.Contains("whip"))
                {
                    return MELEE_WEAPON_RANGE;
                }

                // Herramientas (rango mínimo)
                if (itemName.Contains("pickaxe") || itemName.Contains("shovel") ||
                    itemName.Contains("hoe") || itemName.Contains("watering") ||
                    itemName.Contains("fishing") || itemName.Contains("tool"))
                {
                    return TOOL_RANGE;
                }

                // Verificar si tiene componente de arma usando reflexión
                // Verificar si tiene componentes de proyectil (armas a distancia)
                try
                {
                    // Usar reflexión para verificar componentes sin depender de tipos específicos
                    var hasProjectile = false;
                    try
                    {
                        var projectileType = System.Type.GetType("PugComps.ProjectileOnUseCD, Pug.Other");
                        if (projectileType != null)
                        {
                            var hasComponentMethod = typeof(PugOther.PugDatabase).GetMethod("HasComponent");
                            if (hasComponentMethod != null)
                            {
                                var genericMethod = hasComponentMethod.MakeGenericMethod(projectileType);
                                hasProjectile = (bool)genericMethod.Invoke(null, new object[] { objectID });
                            }
                        }
                    }
                    catch { }

                    if (hasProjectile)
                    {
                        return RANGED_WEAPON_RANGE;
                    }

                    // Verificar por tipo de objeto genérico
                    var objectInfo = PugOther.PugDatabase.GetObjectInfo(objectID);
                    if (objectInfo != null)
                    {
                        // Verificar si es equipable (probablemente un arma)
                        if (PugOther.PugDatabase.HasComponent<PugComps.EquipmentCD>(objectID))
                        {
                            return MELEE_WEAPON_RANGE;
                        }
                    }
                }
                catch { }

                // Default: rango base
                return AUTO_TARGET_BASE_RANGE;
            }
            catch
            {
                return AUTO_TARGET_BASE_RANGE;
            }
        }

        /// <summary>
        /// Verifica si una entidad es un enemigo REAL (no estatuas, minions ni decoraciones)
        /// </summary>
        public static bool IsEnemyEntity(PugOther.EntityMonoBehaviour entity)
        {
            try
            {
                var gameObject = entity.gameObject;
                if (gameObject == null) return false;

                // FILTRO CRÍTICO: Excluir MINIONS y PETS del jugador
                // Los minions invocados tienen el componente MinionCD o MinionOwnerCD
                // Las mascotas tienen el componente PetCD
                try
                {
                    var entityData = entity.entity;
                    var world = entity.world;

                    if (world != null && entityData != default)
                    {
                        // Verificar si es un minion invocado por el jugador
                        if (world.EntityManager.HasComponent<PugComps.MinionCD>(entityData))
                        {
                            return false; // Es un minion del jugador, NO es enemigo
                        }

                        // Verificar si es una mascota del jugador
                        if (world.EntityManager.HasComponent<PugComps.PetCD>(entityData))
                        {
                            return false; // Es una mascota, NO es enemigo
                        }

                        // NUEVO: Verificar MinionOwnerCD (componente alternativo para minions)
                        if (world.EntityManager.HasComponent<PugComps.MinionOwnerCD>(entityData))
                        {
                            return false; // Es un minion con dueño, NO es enemigo
                        }
                    }
                }
                catch
                {
                    // Si falla la verificación de componentes, continuar con las otras verificaciones
                }

                string name = gameObject.name.ToLower();

                // FILTRO ADICIONAL POR NOMBRE: Excluir minions por patrones de nombre comunes
                // Los minions del jugador suelen tener estos patrones en sus nombres
                if (name.Contains("minion") ||
                    name.Contains("summon") ||
                    name.Contains("companion") ||
                    name.Contains("familiar") ||
                    name.Contains("pet") ||
                    name.Contains("ally"))
                {
                    return false; // Es un minion/aliado del jugador, NO es enemigo
                }

                // FILTRO ULTRA ESTRICTO: Si contiene "statue" EN CUALQUIER PARTE, NO es enemigo
                if (name.Contains("statue") || name.Contains("statua"))
                    return false;

                // Si contiene "dummy" o "mannequin", tampoco
                if (name.Contains("dummy") || name.Contains("mannequin") || name.Contains("practice"))
                    return false;

                // FILTRO MEJORADO: Excluir objetos que definitivamente NO son enemigos
                // Decoraciones y objetos del entorno
                if (name.Contains("totem") ||
                    name.Contains("spawner") ||
                    name.Contains("turret") ||
                    name.Contains("trap") ||
                    name.Contains("decoration") ||
                    name.Contains("prop") ||
                    name.Contains("destructible") ||
                    name.Contains("breakable") ||
                    name.Contains("crate") ||
                    name.Contains("barrel") ||
                    name.Contains("chest") ||
                    name.Contains("container") ||
                    name.Contains("furniture") ||
                    name.Contains("plant") ||
                    name.Contains("tree") ||
                    name.Contains("rock") ||
                    name.Contains("ore") ||
                    name.Contains("crystal") ||
                    name.Contains("wall") ||
                    name.Contains("floor") ||
                    name.Contains("ceiling"))
                {
                    return false; // No son enemigos, son objetos del mundo
                }

                // Verificar si tiene componentes que lo identifican como enemigo
                // Por ahora no podemos acceder a objectID desde EntityMonoBehaviour directamente
                bool hasEnemyComponents = false;

                // Si no encontramos componentes, usar lista de nombres
                // IMPORTANTE: Solo incluir enemigos REALES que atacan
                // Excluir explícitamente cosas como "fly" si es una mosca decorativa

                // VERIFICACIÓN ADICIONAL: El nombre debe contener algún indicador de movimiento o vida
                // Los enemigos reales suelen tener componentes o sufijos que indican que son entidades vivas
                bool hasLifeIndicator = name.Contains("(clone)") ||
                                       name.Contains("spawned") ||
                                       name.Contains("alive") ||
                                       name.Contains("active") ||
                                       name.Contains("mob_") ||
                                       name.Contains("enemy_") ||
                                       name.Contains("hostile_");

                // Para enemigos conocidos, verificar que NO sean estatuas Y que parezcan vivos
                // El nombre exacto importa - los enemigos reales tienen patrones específicos

                // Slimes reales
                if ((name.StartsWith("slime") || name.StartsWith("orangeslime") ||
                     name.StartsWith("acidslime") || name.StartsWith("poisonslime")) &&
                    !name.Contains("statue"))
                    return true;

                // Arañas reales
                if ((name.StartsWith("spider") || name.StartsWith("cavespider") ||
                     name.StartsWith("webspider")) && !name.Contains("statue"))
                    return true;

                // Esqueletos reales
                if ((name.StartsWith("skeleton") || name.StartsWith("undeadskeleton")) &&
                    !name.Contains("statue"))
                    return true;

                // Goblins reales
                if ((name.StartsWith("goblin") || name.StartsWith("cavegoblin")) &&
                    !name.Contains("statue"))
                    return true;

                // Orcos - CUIDADO: muchas estatuas de orcos
                if ((name.StartsWith("orc_") || name.StartsWith("ork_") ||
                     name.Equals("orc") || name.Equals("ork")) &&
                    !name.Contains("statue"))
                    return true;

                // Zombies reales
                if (name.StartsWith("zombie") && !name.Contains("statue"))
                    return true;

                // Demonios reales
                if (name.StartsWith("demon") && !name.Contains("statue"))
                    return true;

                // Larvas - verificar que no sea spawner
                if ((name.StartsWith("larva") || name.StartsWith("grub")) &&
                    !name.Contains("statue") && !name.Contains("spawner"))
                    return true;

                // Gusanos
                if (name.StartsWith("worm") && !name.Contains("statue"))
                    return true;

                // Murciélagos
                if (name.StartsWith("bat") && !name.Contains("statue"))
                    return true;

                // Ratas
                if (name.StartsWith("rat") && !name.Contains("statue"))
                    return true;
                if (name.Contains("mushroom") && name.Contains("poison"))
                    return true; // Hongos venenosos que atacan
                if (name.Contains("shaman"))
                    return true;
                if (name.Contains("witch"))
                    return true;
                if (name.Contains("mage") && !name.Contains("statue"))
                    return true;
                if (name.Contains("knight") && !name.Contains("statue"))
                    return true;
                if (name.Contains("warrior") && !name.Contains("statue"))
                    return true;
                if (name.Contains("guardian") && !name.Contains("statue"))
                    return true;
                if (name.Contains("brute"))
                    return true;
                if (name.Contains("crawler"))
                    return true;

                // Patrones genéricos pero con más cuidado
                if ((name.Contains("enemy") || name.Contains("hostile") || name.Contains("monster")) &&
                    !name.Contains("statue") && !name.Contains("dummy"))
                    return true;

                // Jefes (bosses) siempre son enemigos
                if (name.Contains("boss") && !name.Contains("statue"))
                    return true;

                // Si tiene componentes de enemigo pero no matchó nombres, probablemente es enemigo
                if (hasEnemyComponents && !name.Contains("npc") && !name.Contains("merchant") &&
                    !name.Contains("vendor") && !name.Contains("friendly"))
                    return true;

                return false; // Por defecto, no es enemigo
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifica si un objetivo sigue siendo válido
        /// </summary>
        private static bool IsValidTarget(PugOther.EntityMonoBehaviour target)
        {
            try
            {
                return target != null &&
                       target.gameObject != null &&
                       target.gameObject.activeInHierarchy &&
                       IsEnemyEntity(target);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Valida el objetivo actual y lo limpia si es inválido
        /// </summary>
        private static void ValidateCurrentTarget(PugOther.PlayerController player)
        {
            if (_currentTarget != null && !IsValidTarget(_currentTarget))
            {
                _currentTarget = null;
            }

            // También verificar si está fuera de rango
            if (_currentTarget != null && TryGetPlayerPosition(player, out Vector3 playerPos))
            {
                float effectiveRange = CalculateEffectiveRange(player);
                float distance = GetDistanceToCurrentTarget();

                if (distance > effectiveRange + 2f) // Un poco de margen
                {
                    _currentTarget = null;
                }
            }
        }

        /// <summary>
        /// Obtiene la distancia al objetivo actual
        /// </summary>
        private static float GetDistanceToCurrentTarget()
        {
            if (_currentTarget == null) return float.MaxValue;

            try
            {
                var targetPos = _currentTarget.WorldPosition;
                return Vector3.Distance(_lastTargetPosition, new Vector3(targetPos.x, targetPos.y, targetPos.z));
            }
            catch
            {
                return float.MaxValue;
            }
        }

        /// <summary>
        /// Obtiene la posición del jugador de forma segura
        /// </summary>
        private static bool TryGetPlayerPosition(PugOther.PlayerController player, out Vector3 position)
        {
            position = Vector3.zero;

            try
            {
                if (player == null) return false;

                var worldPos = player.WorldPosition;
                position = new Vector3(worldPos.x, worldPos.y, worldPos.z);
                return true;
            }
            catch
            {
                try
                {
                    if (player.transform != null)
                    {
                        var pos = player.transform.position;
                        position = new Vector3(pos.x, pos.y, pos.z);
                        return true;
                    }
                }
                catch { }
            }

            return false;
        }

        /// <summary>
        /// Obtiene el nombre de una entidad
        /// </summary>
        private static string GetEntityName(PugOther.EntityMonoBehaviour entity)
        {
            try
            {
                return entity.gameObject?.name ?? "Unknown Enemy";
            }
            catch
            {
                return "Unknown Enemy";
            }
        }

        /// <summary>
        /// Método legacy para compatibilidad - el sistema siempre está activo
        /// </summary>
        public static void SetSystemEnabled(bool enabled)
        {
            // No hacer nada - el sistema siempre está activo
            // Mantenido para compatibilidad pero no tiene efecto
        }

        /// <summary>
        /// Verifica si el sistema está habilitado
        /// </summary>
        public static bool IsSystemEnabled => _systemEnabled;

        /// <summary>
        /// Obtiene el objetivo actual (para debugging)
        /// </summary>
        public static PugOther.EntityMonoBehaviour GetCurrentTarget() => _currentTarget;

        /// <summary>
        /// Procesa los anuncios TTS para enemigos que entran o salen del rango
        /// </summary>
        private static void ProcessEnemyRangeAnnouncements()
        {
            try
            {
                // Verificar cooldown para evitar spam
                float currentTime = UnityEngine.Time.time;
                if (currentTime - _lastAnnouncementTime < ANNOUNCEMENT_COOLDOWN)
                    return;

                // Crear conjunto de enemigos actualmente en rango
                var currentlyInRangeEnemies = new HashSet<PugOther.EntityMonoBehaviour>();
                foreach (var enemy in _nearbyEnemies)
                {
                    if (enemy.entity != null && IsValidTarget(enemy.entity))
                    {
                        currentlyInRangeEnemies.Add(enemy.entity);
                    }
                }

                // Solo anunciar si el sistema está habilitado
                if (!_systemEnabled)
                    return;

                // Detectar enemigos que entraron al rango
                foreach (var enemy in currentlyInRangeEnemies)
                {
                    if (!_previouslyInRangeEnemies.Contains(enemy))
                    {
                        AnnounceEnemyInRange(enemy, true);
                        _lastAnnouncementTime = currentTime;
                        break; // Solo anunciar uno por vez para evitar spam
                    }
                }

                // Detectar enemigos que salieron del rango
                var enemiesToRemove = new List<PugOther.EntityMonoBehaviour>();
                foreach (var enemy in _previouslyInRangeEnemies)
                {
                    if (!currentlyInRangeEnemies.Contains(enemy) || !IsValidTarget(enemy))
                    {
                        if (IsValidTarget(enemy)) // Solo anunciar si todavía existe
                        {
                            AnnounceEnemyInRange(enemy, false);
                            _lastAnnouncementTime = currentTime;
                        }
                        enemiesToRemove.Add(enemy);
                        break; // Solo anunciar uno por vez
                    }
                }

                // Limpiar enemigos que ya no están válidos
                foreach (var enemy in enemiesToRemove)
                {
                    _previouslyInRangeEnemies.Remove(enemy);
                }

                // Actualizar conjunto para el próximo frame
                _previouslyInRangeEnemies = currentlyInRangeEnemies;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error procesando anuncios de enemigos: {ex}");
            }
        }

        /// <summary>
        /// Anuncia cuando un enemigo entra o sale del rango
        /// MULTIPLAYER-SAFE: Usa el jugador local para cálculos
        /// </summary>
        private static void AnnounceEnemyInRange(PugOther.EntityMonoBehaviour enemy, bool enteringRange)
        {
            try
            {
                // Solo anunciar si el sistema está activo
                if (!_systemEnabled)
                    return;

                string enemyName = GetCleanEnemyName(enemy);

                // Calcular distancia y dirección relativa al jugador LOCAL
                var player = LocalPlayerHelper.GetLocalPlayer();
                if (player != null && TryGetPlayerPosition(player, out Vector3 playerPos))
                {
                    var enemyPos = enemy.WorldPosition;
                    var enemyWorldPos = new Vector3(enemyPos.x, enemyPos.y, enemyPos.z);
                    float distance = Vector3.Distance(playerPos, enemyWorldPos);

                    // Calcular dirección
                    string direction = GetRelativeDirection(playerPos, enemyWorldPos);

                    if (enteringRange)
                    {
                        string message = LocalizationManager.GetText("enemy_at_distance", enemyName, direction, ((int)math.round(distance)).ToString());
                        UIManager.Speak(message);
                    }
                    else
                    {
                        string message = LocalizationManager.GetText("enemy_out_of_range", enemyName);
                        UIManager.Speak(message);
                    }
                }
                else
                {
                    // Fallback simple sin dirección
                    string key = enteringRange ? "enemy_in_range" : "enemy_out_of_range";
                    string message = LocalizationManager.GetText(key, enemyName);
                    UIManager.Speak(message);
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error anunciando enemigo: {ex}");
            }
        }

        /// <summary>
        /// Obtiene la dirección relativa de un objetivo respecto al jugador
        /// </summary>
        private static string GetRelativeDirection(Vector3 playerPos, Vector3 targetPos)
        {
            Vector3 diff = targetPos - playerPos;
            float angle = math.degrees(math.atan2(diff.z, diff.x));

            // Normalizar el ángulo entre 0-360
            if (angle < 0) angle += 360;

            // Determinar dirección cardinal
            if (angle >= 337.5f || angle < 22.5f)
                return LocalizationManager.GetText("dir_east");
            else if (angle >= 22.5f && angle < 67.5f)
                return LocalizationManager.GetText("dir_northeast");
            else if (angle >= 67.5f && angle < 112.5f)
                return LocalizationManager.GetText("dir_north");
            else if (angle >= 112.5f && angle < 157.5f)
                return LocalizationManager.GetText("dir_northwest");
            else if (angle >= 157.5f && angle < 202.5f)
                return LocalizationManager.GetText("dir_west");
            else if (angle >= 202.5f && angle < 247.5f)
                return LocalizationManager.GetText("dir_southwest");
            else if (angle >= 247.5f && angle < 292.5f)
                return LocalizationManager.GetText("dir_south");
            else
                return LocalizationManager.GetText("dir_southeast");
        }

        /// <summary>
        /// Obtiene un nombre limpio y descriptivo del enemigo
        /// </summary>
        private static string GetCleanEnemyName(PugOther.EntityMonoBehaviour enemy)
        {
            try
            {
                if (enemy?.gameObject?.name == null)
                    return LocalizationManager.GetText("enemy_unknown");

                string rawName = enemy.gameObject.name.ToLower();

                // Limpiar el nombre removiendo sufijos comunes de Unity
                string cleanName = rawName;

                // Remover sufijos como (Clone), números, etc.
                if (cleanName.Contains("(clone)"))
                    cleanName = cleanName.Replace("(clone)", "").Trim();

                // Remover números al final
                while (cleanName.Length > 0 && char.IsDigit(cleanName[cleanName.Length - 1]))
                {
                    cleanName = cleanName.Substring(0, cleanName.Length - 1);
                }

                // Mapear nombres conocidos a claves de localización
                var nameMapping = new Dictionary<string, string>
                {
                    { "slime", "enemy_slime" },
                    { "spider", "enemy_spider" },
                    { "skeleton", "enemy_skeleton" },
                    { "goblin", "enemy_goblin" },
                    { "orc", "enemy_orc" },
                    { "zombie", "enemy_zombie" },
                    { "demon", "enemy_demon" },
                    { "beast", "enemy_beast" },
                    { "larva", "enemy_larva" },
                    { "grub", "enemy_grub" },
                    { "worm", "enemy_worm" },
                    { "fly", "enemy_fly" },
                    { "bat", "enemy_bat" },
                    { "rat", "enemy_rat" },
                    { "boss", "enemy_boss" }
                };

                // Buscar coincidencias en el mapeo
                foreach (var mapping in nameMapping)
                {
                    if (cleanName.Contains(mapping.Key))
                    {
                        return LocalizationManager.GetText(mapping.Value);
                    }
                }

                // Si no hay mapeo, capitalizar la primera letra
                if (cleanName.Length > 0)
                {
                    return char.ToUpper(cleanName[0]) + cleanName.Substring(1);
                }

                return LocalizationManager.GetText("enemy_generic");
            }
            catch
            {
                return LocalizationManager.GetText("enemy_generic");
            }
        }

        /// <summary>
        /// Obtiene información de debugging del sistema
        /// </summary>
        public static string GetDebugInfo()
        {
            return $"AutoTargeting: Enabled={_systemEnabled}, " +
                   $"CurrentTarget={(_currentTarget?.gameObject?.name ?? "None")}, " +
                   $"NearbyEnemies={_nearbyEnemies.Count}, " +
                   $"InRangeEnemies={_previouslyInRangeEnemies.Count}";
        }

        /// <summary>
        /// Parche en SendClientInputSystem para interceptar el cálculo de dirección
        /// Este es el punto correcto para aplicar auto-targeting universalmente
        /// MULTIPLAYER-SAFE: Solo afecta al jugador local
        /// </summary>
        [HarmonyPatch(typeof(PugOther.SendClientInputSystem), "OnUpdate")]
        [HarmonyPrefix]
        public static void SendClientInputSystem_OnUpdate_Prefix()
        {
            try
            {
                if (!_systemEnabled || _currentTarget == null || !IsValidTarget(_currentTarget))
                    return;

                // Obtener el jugador LOCAL
                var player = LocalPlayerHelper.GetLocalPlayer();
                if (player == null) return;

                // Aplicar el auto-targeting modificando la dirección de apuntado
                RedirectAttackToTarget(player, _currentTarget);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error en SendClientInputSystem_OnUpdate_Prefix: {ex}");
            }
        }


        /// <summary>
        /// Obtiene la posición mundial del objetivo actual para uso externo
        /// </summary>
        public static float3? GetCurrentTargetPosition()
        {
            if (_currentTarget == null || !IsValidTarget(_currentTarget))
                return null;

            var pos = _currentTarget.WorldPosition;
            return new float3(pos.x, pos.y, pos.z);
        }
    }
}