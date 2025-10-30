extern alias PugOther;
extern alias Core;

using System;
using System.Collections.Generic;
using GameObject = Core::UnityEngine.GameObject;

namespace ckAccess.MapReader
{
    /// <summary>
    /// Sistema de categorización inteligente de objetos basado en el código del juego.
    /// Elimina hardcoding y usa estructura de datos mantenible.
    /// </summary>
    public static class ObjectCategoryHelper
    {
        // Categorías principales del juego
        public enum ObjectCategory
        {
            Unknown,
            Core,
            Chest,
            WorkStation,
            Enemy,
            Pickup,
            Plant,
            Resource,
            Decoration,
            Furniture,
            Animal,
            Critter,
            Structure,
            Door,
            Statue
        }

        // Mapeo de patterns a categorías - Basado en análisis de "ck object" folder
        private static readonly Dictionary<string, ObjectCategory> CategoryPatterns = new Dictionary<string, ObjectCategory>
        {
            // Core game objects
            ["core"] = ObjectCategory.Core,
            ["brokencore"] = ObjectCategory.Core,

            // Storage
            ["chest"] = ObjectCategory.Chest,
            ["storage"] = ObjectCategory.Chest,
            ["container"] = ObjectCategory.Chest,

            // Crafting stations (basado en SimpleCraftingBuilding.cs y similares)
            ["workbench"] = ObjectCategory.WorkStation,
            ["anvil"] = ObjectCategory.WorkStation,
            ["furnace"] = ObjectCategory.WorkStation,
            ["forge"] = ObjectCategory.WorkStation,
            ["table"] = ObjectCategory.WorkStation,
            ["station"] = ObjectCategory.WorkStation,
            ["smelter"] = ObjectCategory.WorkStation,
            ["kiln"] = ObjectCategory.WorkStation,
            ["cookingpot"] = ObjectCategory.WorkStation,
            ["cartographytable"] = ObjectCategory.WorkStation,
            ["eggincubator"] = ObjectCategory.WorkStation,
            ["tablesaw"] = ObjectCategory.WorkStation,

            // Animals (basado en archivos como Cow.cs, Goat.cs, etc.)
            ["cow"] = ObjectCategory.Animal,
            ["goat"] = ObjectCategory.Animal,
            ["dodo"] = ObjectCategory.Animal,
            ["turtle"] = ObjectCategory.Animal,
            ["rolypoly"] = ObjectCategory.Animal,
            ["camel"] = ObjectCategory.Animal,

            // Critters (basado en Critter.cs hierarchy)
            ["critter"] = ObjectCategory.Critter,
            ["butterfly"] = ObjectCategory.Critter,

            // Enemies (nombres comunes)
            ["slime"] = ObjectCategory.Enemy,
            ["spider"] = ObjectCategory.Enemy,
            ["larva"] = ObjectCategory.Enemy,
            ["grub"] = ObjectCategory.Enemy,
            ["mushroom"] = ObjectCategory.Enemy,
            ["scarab"] = ObjectCategory.Enemy,
            ["mold"] = ObjectCategory.Enemy,
            ["boss"] = ObjectCategory.Enemy,
            ["skeleton"] = ObjectCategory.Enemy,
            ["goblin"] = ObjectCategory.Enemy,
            ["zombie"] = ObjectCategory.Enemy,

            // Pickups/Items
            ["pickup"] = ObjectCategory.Pickup,
            ["drop"] = ObjectCategory.Pickup,
            ["item"] = ObjectCategory.Pickup,

            // Plants (basado en archivos del juego)
            ["tree"] = ObjectCategory.Plant,
            ["plant"] = ObjectCategory.Plant,
            ["flower"] = ObjectCategory.Plant,
            ["bush"] = ObjectCategory.Plant,
            ["grass"] = ObjectCategory.Plant,
            ["abysst"] = ObjectCategory.Plant,

            // Resources
            ["ore"] = ObjectCategory.Resource,
            ["crystal"] = ObjectCategory.Resource,
            ["mineral"] = ObjectCategory.Resource,
            ["rock"] = ObjectCategory.Resource,
            ["boulder"] = ObjectCategory.Resource,

            // Structures (basado en archivos del juego)
            ["obelisk"] = ObjectCategory.Structure,
            ["pillar"] = ObjectCategory.Structure,
            ["fountain"] = ObjectCategory.Structure,
            ["monument"] = ObjectCategory.Structure,

            // Furniture (basado en archivos como Banner.cs, etc.)
            ["bed"] = ObjectCategory.Furniture,
            ["chair"] = ObjectCategory.Furniture,
            ["stool"] = ObjectCategory.Furniture,
            ["throne"] = ObjectCategory.Furniture,
            ["banner"] = ObjectCategory.Furniture,
            ["pot"] = ObjectCategory.Furniture,
            ["toilet"] = ObjectCategory.Furniture,

            // Statues
            ["statue"] = ObjectCategory.Statue,
            ["trophy"] = ObjectCategory.Statue,

            // Decorations
            ["decoration"] = ObjectCategory.Decoration,
            ["rug"] = ObjectCategory.Decoration,
        };

        // Exclusiones - objetos que NO deben ser categorizados como enemigos aunque contengan la palabra
        private static readonly HashSet<string> EnemyExclusions = new HashSet<string>
        {
            "statue",
            "trophy",
            "dummy",
            "practice",
            "totem",
            "spawner",
            "decoration"
        };

        /// <summary>
        /// Determina la categoría de un objeto basándose en su GameObject
        /// </summary>
        public static ObjectCategory GetCategory(GameObject gameObject)
        {
            if (gameObject == null) return ObjectCategory.Unknown;

            string name = gameObject.name.ToLower();
            name = name.Replace("(clone)", "").Replace("_", "").Trim();

            // Verificar exclusiones primero (para enemigos falsos)
            foreach (var exclusion in EnemyExclusions)
            {
                if (name.Contains(exclusion))
                {
                    // Si es statue/trophy, devolver esa categoría
                    if (exclusion == "statue" || exclusion == "trophy")
                        return ObjectCategory.Statue;
                    // Otros casos, continuar la búsqueda normal
                    break;
                }
            }

            // Buscar en el diccionario de patterns
            foreach (var kvp in CategoryPatterns)
            {
                if (name.Contains(kvp.Key))
                {
                    // Verificación adicional para enemigos
                    if (kvp.Value == ObjectCategory.Enemy)
                    {
                        // Asegurar que no es una exclusión
                        bool isExcluded = false;
                        foreach (var exclusion in EnemyExclusions)
                        {
                            if (name.Contains(exclusion))
                            {
                                isExcluded = true;
                                break;
                            }
                        }
                        if (isExcluded) continue;
                    }

                    return kvp.Value;
                }
            }

            return ObjectCategory.Unknown;
        }

        /// <summary>
        /// Obtiene la categoría de una entidad
        /// </summary>
        public static ObjectCategory GetCategory(PugOther.EntityMonoBehaviour entity)
        {
            if (entity?.gameObject == null) return ObjectCategory.Unknown;
            return GetCategory(entity.gameObject);
        }

        /// <summary>
        /// Verifica si un objeto es interactuable (estaciones, cofres, etc.)
        /// </summary>
        public static bool IsInteractable(ObjectCategory category)
        {
            return category == ObjectCategory.Chest ||
                   category == ObjectCategory.WorkStation ||
                   category == ObjectCategory.Core;
        }

        /// <summary>
        /// Verifica si un objeto es hostil
        /// </summary>
        public static bool IsHostile(ObjectCategory category)
        {
            return category == ObjectCategory.Enemy;
        }

        /// <summary>
        /// Verifica si un objeto es recolectable
        /// </summary>
        public static bool IsCollectible(ObjectCategory category)
        {
            return category == ObjectCategory.Pickup ||
                   category == ObjectCategory.Resource;
        }

        /// <summary>
        /// Verifica si un objeto es un ser vivo amigable
        /// </summary>
        public static bool IsFriendly(ObjectCategory category)
        {
            return category == ObjectCategory.Animal ||
                   category == ObjectCategory.Critter;
        }

        /// <summary>
        /// Obtiene una descripción amigable de la categoría
        /// </summary>
        public static string GetCategoryDescription(ObjectCategory category)
        {
            return category switch
            {
                ObjectCategory.Core => "Núcleo",
                ObjectCategory.Chest => "Cofre",
                ObjectCategory.WorkStation => "Estación de trabajo",
                ObjectCategory.Enemy => "Enemigo",
                ObjectCategory.Pickup => "Objeto recolectable",
                ObjectCategory.Plant => "Planta",
                ObjectCategory.Resource => "Recurso",
                ObjectCategory.Decoration => "Decoración",
                ObjectCategory.Furniture => "Mueble",
                ObjectCategory.Animal => "Animal",
                ObjectCategory.Critter => "Criatura",
                ObjectCategory.Structure => "Estructura",
                ObjectCategory.Door => "Puerta",
                ObjectCategory.Statue => "Estatua",
                _ => "Objeto desconocido"
            };
        }
    }
}
