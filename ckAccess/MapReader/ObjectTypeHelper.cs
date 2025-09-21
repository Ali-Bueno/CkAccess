extern alias PugOther;

using System.Collections.Generic;

namespace ckAccess.MapReader
{
    /// <summary>
    /// Helper OPTIMIZADO para obtener nombres de objetos con caché de rendimiento.
    /// </summary>
    public static class ObjectTypeHelper
    {
        /// <summary>
        /// Caché de nombres de objetos para optimizar rendimiento.
        /// </summary>
        private static readonly Dictionary<ObjectID, string> _nameCache = new Dictionary<ObjectID, string>();
        /// <summary>
        /// Diccionario con nombres en español para ObjectIDs más comunes.
        /// </summary>
        private static readonly Dictionary<ObjectID, string> SpanishNames = new Dictionary<ObjectID, string>
        {
            // Herramientas básicas - solo las que sabemos que existen
            { ObjectID.CopperMiningPick, "Pico de cobre" },
            { ObjectID.WoodMiningPick, "Pico de madera" },
            { ObjectID.TinMiningPick, "Pico de estaño" },
            { ObjectID.IronMiningPick, "Pico de hierro" },
            { ObjectID.ScarletMiningPick, "Pico escarlata" },
            { ObjectID.OctarineMiningPick, "Pico de octarina" },
            { ObjectID.GalaxiteMiningPick, "Pico de galaxita" },

            // Palas
            { ObjectID.CopperShovel, "Pala de cobre" },
            { ObjectID.WoodShovel, "Pala de madera" },
            { ObjectID.TinShovel, "Pala de estaño" },
            { ObjectID.IronShovel, "Pala de hierro" },
            { ObjectID.ScarletShovel, "Pala escarlata" },

            // Antorchas e iluminación
            { ObjectID.Torch, "Antorcha" },
            { ObjectID.Campfire, "Fogata" },
            { ObjectID.DecorativeTorch1, "Antorcha decorativa" },
            { ObjectID.FishoilTorch, "Antorcha de aceite de pescado" },
            { ObjectID.CrystalLamp, "Lámpara de cristal" },
            { ObjectID.Lamp, "Lámpara" },

            // Cofres y almacenamiento
            { ObjectID.InventoryChest, "Cofre" },
            { ObjectID.InventoryLarvaHiveChest, "Cofre de panal de larvas" },
            { ObjectID.InventoryMoldDungeonChest, "Cofre de mazmorra de moho" },
            { ObjectID.InventoryAncientChest, "Cofre antiguo" },
            { ObjectID.InventorySeaBiomeChest, "Cofre del bioma marino" },
            { ObjectID.InventoryDesertBiomeChest, "Cofre del bioma desértico" },

            // Decoración básica
            { ObjectID.DecorativePot, "Maceta decorativa" },
            { ObjectID.PlanterBox, "Jardinera" },
            { ObjectID.Pedestal, "Pedestal" },
            { ObjectID.StonePedestal, "Pedestal de piedra" },
            { ObjectID.WoodStool, "Taburete de madera" },
            { ObjectID.WoodTable, "Mesa de madera" },
            { ObjectID.Painting, "Pintura" },

            // Herramientas especiales
            { ObjectID.WaterCan, "Regadera" },
            { ObjectID.LargeWaterCan, "Regadera grande" },
            { ObjectID.BugNet, "Red para insectos" },
            { ObjectID.Bucket, "Cubo" },
            { ObjectID.DrillTool, "Taladro" },

            // Recursos comunes
            { ObjectID.CopperOre, "Mineral de cobre" },
            { ObjectID.TinOre, "Mineral de estaño" },
            { ObjectID.IronOre, "Mineral de hierro" },
            { ObjectID.ScarletOre, "Mineral escarlata" },
            { ObjectID.OctarineOre, "Mineral de octarina" },
            { ObjectID.GalaxiteOre, "Mineral de galaxita" },

            // Madera y materiales básicos
            { ObjectID.Wood, "Madera" },

            // Especiales
            { ObjectID.LargeShinyGlimmeringObject, "Objeto brillante grande" },
            { ObjectID.Thumper, "Golpeador" },

            // Ninguno
            { ObjectID.None, "Ninguno" }
        };

        /// <summary>
        /// Obtiene el nombre localizado de un ObjectID usando el sistema del juego.
        /// </summary>
        /// <param name="objectID">ID del objeto</param>
        /// <returns>Nombre localizado</returns>
        public static string GetLocalizedName(ObjectID objectID)
        {
            // OPTIMIZACIÓN: Verificar caché primero
            if (_nameCache.TryGetValue(objectID, out var cachedName))
            {
                return cachedName;
            }

            string result;
            try
            {
                // PRIORIDAD 1: Usar sistema de propiedades del juego
                var propertyName = GetObjectPropertyName(objectID);
                if (!string.IsNullOrEmpty(propertyName))
                {
                    result = propertyName;
                }
                else
                {
                    // PRIORIDAD 2: Usar sistema de localización del juego
                    var localizedName = GetLocalizedObjectName(objectID);
                    if (!string.IsNullOrEmpty(localizedName))
                    {
                        result = localizedName;
                    }
                    else
                    {
                        // Fallback al sistema español manual
                        result = SpanishNames.TryGetValue(objectID, out var name) ? name : FormatEnumName(objectID.ToString());
                    }
                }
            }
            catch (System.Exception)
            {
                // Fallback silencioso
                result = SpanishNames.TryGetValue(objectID, out var name) ? name : FormatEnumName(objectID.ToString());
            }

            // OPTIMIZACIÓN: Cachear resultado para futuras consultas
            _nameCache[objectID] = result;
            return result;
        }

        /// <summary>
        /// OBSOLETO: Usar GetLocalizedName() en su lugar.
        /// </summary>
        public static string GetSpanishName(ObjectID objectID)
        {
            return GetLocalizedName(objectID);
        }

        /// <summary>
        /// Caché estático para reflexión costosa.
        /// </summary>
        private static System.Type _objectPropertiesType;
        private static System.Reflection.MethodInfo _tryGetPropertyStringMethod;
        private static bool _objectReflectionInitialized = false;

        /// <summary>
        /// Obtiene el nombre usando API.Authoring.ObjectProperties (sistema preferido del juego).
        /// ULTRA OPTIMIZADO: Caché de reflexión para evitar llamadas costosas.
        /// </summary>
        private static string GetObjectPropertyName(ObjectID objectID)
        {
            try
            {
                // OPTIMIZACIÓN: Inicializar reflexión solo una vez
                if (!_objectReflectionInitialized)
                {
                    _objectPropertiesType = System.Type.GetType("API.Authoring.ObjectProperties, Assembly-CSharp");
                    if (_objectPropertiesType != null)
                    {
                        _tryGetPropertyStringMethod = _objectPropertiesType.GetMethod("TryGetPropertyString",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    }
                    _objectReflectionInitialized = true;
                }

                if (_tryGetPropertyStringMethod == null) return null;

                string result = "";
                var parameters = new object[] { objectID, "name", result };
                var success = (bool)_tryGetPropertyStringMethod.Invoke(null, parameters);
                if (success)
                {
                    return (string)parameters[2];
                }
            }
            catch (System.Exception)
            {
                // Fallback silencioso
            }
            return null;
        }

        /// <summary>
        /// Obtiene el nombre localizado usando PugGlossary.
        /// </summary>
        private static string GetLocalizedObjectName(ObjectID objectID)
        {
            try
            {
                // Usar reflexión para acceder al PugGlossary del juego
                var glossaryType = System.Type.GetType("PugGlossary, Pug.Other");
                if (glossaryType == null) return null;

                var glossaryInstance = UnityEngine.Object.FindObjectOfType(glossaryType);
                if (glossaryInstance == null) return null;

                var getMethod = glossaryType.GetMethod("Get", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (getMethod == null) return null;

                // Intentar diferentes patrones de claves de localización
                string[] possibleKeys = {
                    $"Items/{objectID}",
                    $"Item/{objectID}",
                    $"items/{objectID}",
                    $"item/{objectID}",
                    $"Objects/{objectID}",
                    $"Object/{objectID}",
                    objectID.ToString()
                };

                foreach (var key in possibleKeys)
                {
                    try
                    {
                        var localizedText = (string)getMethod.Invoke(glossaryInstance, new object[] { key });
                        if (!string.IsNullOrEmpty(localizedText) && !localizedText.StartsWith("M`i`s`s`i`n`g"))
                            return localizedText;
                    }
                    catch (System.Exception)
                    {
                        continue;
                    }
                }
            }
            catch (System.Exception)
            {
                // Fallback silencioso
            }

            return null;
        }

        /// <summary>
        /// Formatea el nombre de un enum para que sea más legible.
        /// </summary>
        private static string FormatEnumName(string enumName)
        {
            if (string.IsNullOrEmpty(enumName)) return "Desconocido";

            var result = "";
            for (int i = 0; i < enumName.Length; i++)
            {
                if (i > 0 && char.IsUpper(enumName[i]))
                {
                    result += " ";
                }
                result += enumName[i];
            }
            return result;
        }

        /// <summary>
        /// Verifica si un objeto es una herramienta.
        /// </summary>
        public static bool IsTool(ObjectID objectID)
        {
            var name = objectID.ToString().ToLower();
            return name.Contains("pick") || name.Contains("shovel") || name.Contains("hoe") ||
                   name.Contains("drill") || name.Contains("hammer") || name.Contains("sledge");
        }

        /// <summary>
        /// Verifica si un objeto es un recurso/material.
        /// </summary>
        public static bool IsResource(ObjectID objectID)
        {
            var name = objectID.ToString().ToLower();
            return name.Contains("ore") || name.Contains("wood") || name.Contains("stone") ||
                   name.Contains("clay") || name.Contains("fiber");
        }

        /// <summary>
        /// Verifica si un objeto es comida.
        /// </summary>
        public static bool IsFood(ObjectID objectID)
        {
            var name = objectID.ToString().ToLower();
            return name.Contains("carrot") || name.Contains("turnip") || name.Contains("wheat") ||
                   name.Contains("apple") || name.Contains("orange") || name.Contains("fish") ||
                   name.Contains("bread") || name.Contains("pie");
        }

        /// <summary>
        /// Verifica si un objeto es almacenamiento.
        /// </summary>
        public static bool IsStorage(ObjectID objectID)
        {
            var name = objectID.ToString().ToLower();
            return name.Contains("chest") || name.Contains("inventory");
        }

        /// <summary>
        /// Obtiene la categoría del objeto en español.
        /// </summary>
        public static string GetCategory(ObjectID objectID)
        {
            if (IsTool(objectID)) return "Herramienta";
            if (IsResource(objectID)) return "Recurso";
            if (IsFood(objectID)) return "Alimento";
            if (IsStorage(objectID)) return "Almacenamiento";

            var name = objectID.ToString().ToLower();
            if (name.Contains("torch") || name.Contains("lamp")) return "Iluminación";
            if (name.Contains("decorative") || name.Contains("pot") || name.Contains("painting")) return "Decoración";
            if (name.Contains("seeds")) return "Semilla";

            return "Objeto";
        }
    }
}