extern alias PugOther;
extern alias PugComps;
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace ckAccess.Helpers
{
    /// <summary>
    /// Helper centralizado para clasificar entidades usando componentes ECS nativos del juego.
    /// Reemplaza la detección basada en patrones de strings por detección basada en componentes.
    /// </summary>
    public static class EntityClassificationHelper
    {
        #region Interactable Detection

        /// <summary>
        /// Verifica si una entidad es interactuable usando componentes ECS.
        /// </summary>
        public static bool IsInteractable(PugOther.EntityMonoBehaviour entity)
        {
            if (entity == null) return false;

            try
            {
                var entityData = entity.entity;
                var world = entity.world;

                if (world == null || entityData == default)
                    return false;

                var em = world.EntityManager;

                // Verificar componentes de interacción nativos
                // InteractableCD indica que el objeto puede ser interactuado
                if (HasComponent(em, entityData, "InteractableCD"))
                    return true;

                // Verificar por ObjectType usando PugDatabase
                var objectID = GetObjectID(entity);
                if (objectID != ObjectID.None)
                {
                    var objectType = GetObjectType(objectID);
                    if (IsInteractableType(objectType))
                        return true;
                }

                // Fallback: verificar por nombre para compatibilidad
                return IsInteractableByName(entity);
            }
            catch
            {
                return IsInteractableByName(entity);
            }
        }

        /// <summary>
        /// Tipos de objeto que son interactuables.
        /// </summary>
        private static bool IsInteractableType(ObjectType type)
        {
            switch (type)
            {
                case ObjectType.PlaceablePrefab: // Incluye workstations, cofres, etc.
                case ObjectType.Creature: // NPCs
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Fallback: detectar interactuables por nombre.
        /// </summary>
        private static bool IsInteractableByName(PugOther.EntityMonoBehaviour entity)
        {
            try
            {
                var name = entity.gameObject?.name?.ToLower();
                if (string.IsNullOrEmpty(name)) return false;

                // Lista reducida de patrones de interactuables conocidos
                return name.Contains("chest") ||
                       name.Contains("workbench") ||
                       name.Contains("furnace") ||
                       name.Contains("table") ||
                       name.Contains("forge") ||
                       name.Contains("anvil") ||
                       name.Contains("altar") ||
                       name.Contains("portal") ||
                       name.Contains("door") ||
                       name.Contains("gate") ||
                       name.Contains("shrine") ||
                       name.Contains("crystal") ||
                       name.Contains("core") ||
                       name.Contains("npc") ||
                       name.Contains("vendor") ||
                       name.Contains("merchant") ||
                       name.Contains("cooking") ||
                       name.Contains("crafting") ||
                       name.Contains("beacon") ||
                       name.Contains("teleporter");
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Enemy Detection

        /// <summary>
        /// Verifica si una entidad es un enemigo usando componentes ECS.
        /// </summary>
        public static bool IsEnemy(PugOther.EntityMonoBehaviour entity)
        {
            if (entity == null) return false;

            try
            {
                var entityData = entity.entity;
                var world = entity.world;

                if (world == null || entityData == default)
                    return IsEnemyByName(entity);

                var em = world.EntityManager;

                // PRIMERO: Excluir minions/pets del jugador
                if (IsPlayerMinion(em, entityData))
                    return false;

                // Verificar si tiene componentes de enemigo activo
                // ChaseStateCD indica que el enemigo está persiguiendo (activo)
                if (HasComponent(em, entityData, "ChaseStateCD"))
                    return true;

                // Verificar estados de ataque
                if (HasComponent(em, entityData, "MeleeAttackStateCD") ||
                    HasComponent(em, entityData, "ChargeAttackStateCD") ||
                    HasComponent(em, entityData, "BeamAttackStateCD"))
                    return true;

                // Verificar si está siendo trackeado como combatiente
                if (HasComponent(em, entityData, "CombatantCD"))
                    return true;

                // Fallback a detección por nombre
                return IsEnemyByName(entity);
            }
            catch
            {
                return IsEnemyByName(entity);
            }
        }

        /// <summary>
        /// Verifica si la entidad es un minion/mascota del jugador.
        /// </summary>
        public static bool IsPlayerMinion(EntityManager em, Entity entity)
        {
            try
            {
                return em.HasComponent<PugComps.MinionCD>(entity) ||
                       em.HasComponent<PugComps.PetCD>(entity) ||
                       em.HasComponent<PugComps.MinionOwnerCD>(entity);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifica si la entidad es un minion usando la sobrecarga de EntityMonoBehaviour.
        /// </summary>
        public static bool IsPlayerMinion(PugOther.EntityMonoBehaviour entity)
        {
            try
            {
                if (entity?.world == null || entity.entity == default)
                    return false;

                return IsPlayerMinion(entity.world.EntityManager, entity.entity);
            }
            catch
            {
                return false;
            }
        }

        // Patrones de enemigos conocidos (reducido y optimizado)
        private static readonly string[] EnemyPrefixes = {
            "slime", "spider", "skeleton", "goblin", "zombie", "demon",
            "larva", "grub", "worm", "bat", "rat", "orc", "ork"
        };

        private static readonly string[] EnemyContains = {
            "shaman", "witch", "mage", "knight", "warrior", "guardian",
            "brute", "crawler", "enemy", "hostile", "monster", "boss"
        };

        private static readonly string[] ExcludePatterns = {
            "statue", "dummy", "mannequin", "totem", "spawner", "decoration",
            "prop", "chest", "container", "plant", "tree", "rock", "ore",
            "minion", "summon", "companion", "familiar", "pet", "ally",
            "npc", "merchant", "vendor", "friendly"
        };

        /// <summary>
        /// Fallback: detectar enemigos por nombre.
        /// </summary>
        private static bool IsEnemyByName(PugOther.EntityMonoBehaviour entity)
        {
            try
            {
                var name = entity.gameObject?.name?.ToLower();
                if (string.IsNullOrEmpty(name)) return false;

                // Excluir patrones conocidos
                foreach (var pattern in ExcludePatterns)
                {
                    if (name.Contains(pattern)) return false;
                }

                // Verificar prefijos de enemigos
                foreach (var prefix in EnemyPrefixes)
                {
                    if (name.StartsWith(prefix)) return true;
                }

                // Verificar patrones contenidos
                foreach (var pattern in EnemyContains)
                {
                    if (name.Contains(pattern)) return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Weapon Classification

        /// <summary>
        /// Clasificación de armas según ObjectType nativo del juego.
        /// </summary>
        public enum WeaponCategory
        {
            None,
            Melee,      // MeleeWeapon
            Ranged,     // RangeWeapon, ThrowingWeapon
            Magic,      // SummoningWeapon, CastingItem, BeamWeapon
            Tool        // Shovel, Hoe, MiningPick, etc.
        }

        /// <summary>
        /// Obtiene la categoría de arma de un ObjectID usando el sistema nativo.
        /// </summary>
        public static WeaponCategory GetWeaponCategory(ObjectID objectID)
        {
            try
            {
                if (objectID == ObjectID.None)
                    return WeaponCategory.None;

                var objectType = GetObjectType(objectID);

                switch (objectType)
                {
                    case ObjectType.MeleeWeapon:
                        return WeaponCategory.Melee;

                    case ObjectType.RangeWeapon:
                    case ObjectType.ThrowingWeapon:
                        return WeaponCategory.Ranged;

                    case ObjectType.SummoningWeapon:
                    case ObjectType.CastingItem:
                    case ObjectType.BeamWeapon:
                        return WeaponCategory.Magic;

                    case ObjectType.Shovel:
                    case ObjectType.Hoe:
                    case ObjectType.MiningPick:
                    case ObjectType.FishingRod:
                    case ObjectType.BugNet:
                    case ObjectType.Sledge:
                    case ObjectType.DrillTool:
                    case ObjectType.PaintTool:
                    case ObjectType.RoofingTool:
                    case ObjectType.WaterCan:
                    case ObjectType.Bucket:
                    case ObjectType.Seeder:
                        return WeaponCategory.Tool;

                    default:
                        return WeaponCategory.None;
                }
            }
            catch
            {
                return WeaponCategory.None;
            }
        }

        /// <summary>
        /// Obtiene el rango efectivo basado en la categoría del arma.
        /// </summary>
        public static float GetWeaponRange(WeaponCategory category)
        {
            switch (category)
            {
                case WeaponCategory.Ranged:
                    return 10f;
                case WeaponCategory.Magic:
                    return 8f;
                case WeaponCategory.Melee:
                    return 3f;
                case WeaponCategory.Tool:
                    return 2f;
                default:
                    return 5f; // Rango base
            }
        }

        /// <summary>
        /// Obtiene el rango efectivo directamente de un ObjectID.
        /// </summary>
        public static float GetWeaponRange(ObjectID objectID)
        {
            return GetWeaponRange(GetWeaponCategory(objectID));
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Obtiene el ObjectID de una entidad.
        /// </summary>
        public static ObjectID GetObjectID(PugOther.EntityMonoBehaviour entity)
        {
            try
            {
                if (entity == null) return ObjectID.None;

                // Intentar obtener desde ObjectDataCD
                var entityData = entity.entity;
                var world = entity.world;

                if (world != null && entityData != default)
                {
                    var em = world.EntityManager;
                    if (em.HasComponent<PugComps.ObjectDataCD>(entityData))
                    {
                        var objectData = em.GetComponentData<PugComps.ObjectDataCD>(entityData);
                        return objectData.objectID;
                    }
                }

                return ObjectID.None;
            }
            catch
            {
                return ObjectID.None;
            }
        }

        /// <summary>
        /// Obtiene el ObjectType de un ObjectID usando PugDatabase.
        /// </summary>
        public static ObjectType GetObjectType(ObjectID objectID)
        {
            try
            {
                if (objectID == ObjectID.None)
                    return ObjectType.NonUsable;

                var info = PugOther.PugDatabase.GetObjectInfo(objectID);
                if (info != null)
                {
                    return info.objectType;
                }

                return ObjectType.NonUsable;
            }
            catch
            {
                return ObjectType.NonUsable;
            }
        }

        /// <summary>
        /// Verifica si una entidad tiene un componente por nombre (usando reflexión).
        /// </summary>
        private static bool HasComponent(EntityManager em, Entity entity, string componentName)
        {
            try
            {
                // Intentar encontrar el tipo de componente
                var componentType = Type.GetType($"PugComps.{componentName}, Pug.ECS.Components") ??
                                   Type.GetType($"PugOther.{componentName}, Pug.Other") ??
                                   Type.GetType($"{componentName}, Pug.ECS.Components");

                if (componentType == null)
                    return false;

                // Usar reflexión para llamar a HasComponent<T>
                var method = typeof(EntityManager).GetMethod("HasComponent", new[] { typeof(Entity) });
                if (method != null)
                {
                    var genericMethod = method.MakeGenericMethod(componentType);
                    return (bool)genericMethod.Invoke(em, new object[] { entity });
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
