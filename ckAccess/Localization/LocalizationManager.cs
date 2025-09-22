extern alias PugOther;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ckAccess.Localization
{
    /// <summary>
    /// Sistema de localización centralizado para el mod de accesibilidad.
    /// Carga las traducciones desde archivos de texto externos.
    /// Proporciona fallback a inglés cuando no hay localización disponible.
    /// </summary>
    public static class LocalizationManager
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new();
        private static bool _isInitialized = false;
        private static readonly object _initLock = new object();

        /// <summary>
        /// Inicializa el sistema de localización cargando los archivos de idioma
        /// </summary>
        private static void Initialize()
        {
            if (_isInitialized) return;

            lock (_initLock)
            {
                if (_isInitialized) return;

                try
                {
                    // Obtener la carpeta del mod (donde está el DLL)
                    string modDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string languagesPath = Path.Combine(modDirectory, "Localization", "Languages");

                    UnityEngine.Debug.Log($"[LocalizationManager] Loading translations from: {languagesPath}");

                    if (Directory.Exists(languagesPath))
                    {
                        // Cargar todos los archivos .txt en la carpeta Languages
                        string[] languageFiles = Directory.GetFiles(languagesPath, "*.txt");

                        foreach (string filePath in languageFiles)
                        {
                            string fileName = Path.GetFileNameWithoutExtension(filePath);
                            LoadLanguageFile(fileName, filePath);
                        }

                        UnityEngine.Debug.Log($"[LocalizationManager] Loaded {Translations.Count} language(s)");
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"[LocalizationManager] Languages directory not found: {languagesPath}");
                        LoadFallbackTranslations();
                    }
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"[LocalizationManager] Error initializing translations: {ex.Message}");
                    LoadFallbackTranslations();
                }

                _isInitialized = true;
            }
        }

        /// <summary>
        /// Carga un archivo de idioma específico
        /// </summary>
        private static void LoadLanguageFile(string languageCode, string filePath)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                var translations = new Dictionary<string, string>();

                foreach (string line in lines)
                {
                    // Ignorar líneas vacías y comentarios
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                        continue;

                    // Buscar el primer '=' para separar clave y valor
                    int equalIndex = line.IndexOf('=');
                    if (equalIndex > 0 && equalIndex < line.Length - 1)
                    {
                        string key = line.Substring(0, equalIndex).Trim();
                        string value = line.Substring(equalIndex + 1).Trim();

                        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                        {
                            translations[key] = value;
                        }
                    }
                }

                Translations[languageCode] = translations;
                UnityEngine.Debug.Log($"[LocalizationManager] Loaded {translations.Count} translations for '{languageCode}'");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[LocalizationManager] Error loading language file '{filePath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Carga traducciones básicas en caso de que no se puedan cargar los archivos
        /// </summary>
        private static void LoadFallbackTranslations()
        {
            UnityEngine.Debug.Log("[LocalizationManager] Loading fallback translations");

            // Solo cargar unas traducciones básicas de emergencia
            Translations["en"] = new Dictionary<string, string>
            {
                ["empty"] = "Empty",
                ["unknown"] = "Unknown",
                ["error"] = "Error",
                ["empty_slot"] = "Empty slot",
                ["talent"] = "Talent",
                ["skills"] = "Skills",
                // Crafting translations
                ["recipe"] = "Recipe: {0}",
                ["recipe_with_amount"] = "Recipe: {0} x{1}",
                ["can_craft"] = "Can craft",
                ["cannot_craft"] = "Cannot craft",
                ["required_materials"] = "Required materials",
                ["missing_material"] = "{0}: need {1}",
                ["available_material"] = "{0}: have {1}",
                ["and_more_materials"] = "and more materials",
                ["invalid_recipe"] = "Invalid recipe",
                ["cannot_craft_materials"] = "Cannot craft: missing materials",
                ["inventory_full"] = "Inventory full",
                ["crafted_item"] = "Crafted {0}",
                ["crafting_error"] = "Crafting error",
                // Direct actions
                ["empty_slot_cannot_equip"] = "Empty slot, cannot equip",
                ["item_not_equippable"] = "{0} cannot be equipped",
                ["equipment_window_not_open"] = "Equipment window not open",
                ["item_equipped"] = "{0} equipped",
                ["item_cannot_equip"] = "Cannot equip {0}",
                ["equip_error"] = "Error equipping",
                ["empty_slot_cannot_use"] = "Empty slot, cannot use",
                ["item_not_usable"] = "{0} cannot be used",
                ["item_used"] = "{0} used",
                ["item_cannot_use"] = "Cannot use {0}",
                ["use_error"] = "Error using",
                ["no_crafting_available"] = "No crafting available",
                ["crafting_station_available"] = "Crafting station available",
                ["select_recipe_item"] = "Select a recipe item",
                ["recipe_item_detected"] = "Recipe detected: {0}",
                ["not_a_recipe"] = "This item is not a recipe",
                ["craft_error"] = "Error crafting",
                // Hotbar messages
                ["hotbar_changed"] = "Hotbar {0} selected",
                ["no_weapon_equipped"] = "No weapon equipped",
                ["empty_hotbar_slot"] = "Empty hotbar slot",
                ["current_hotbar_item"] = "Hotbar {0}, slot {1}: {2}",
                ["hotbar_error"] = "Hotbar error",
                ["durability_critical"] = "critical durability {0}%",
                ["durability_low"] = "low durability {0}%",
                ["durability_normal"] = "durability {0}%",
                ["durability_full"] = "full durability",
                ["hotbar_info_available"] = "Hotbar information available"
            };

            Translations["es"] = new Dictionary<string, string>
            {
                ["empty"] = "Vacío",
                ["unknown"] = "Desconocido",
                ["error"] = "Error",
                ["empty_slot"] = "Slot vacío",
                ["talent"] = "Talento",
                ["skills"] = "Habilidades",
                // Crafting translations
                ["recipe"] = "Receta: {0}",
                ["recipe_with_amount"] = "Receta: {0} x{1}",
                ["can_craft"] = "Se puede crear",
                ["cannot_craft"] = "No se puede crear",
                ["required_materials"] = "Materiales necesarios",
                ["missing_material"] = "{0}: faltan {1}",
                ["available_material"] = "{0}: tienes {1}",
                ["and_more_materials"] = "y más materiales",
                ["invalid_recipe"] = "Receta inválida",
                ["cannot_craft_materials"] = "No se puede crear: faltan materiales",
                ["inventory_full"] = "Inventario lleno",
                ["crafted_item"] = "Creado {0}",
                ["crafting_error"] = "Error al crear",
                // Direct actions
                ["empty_slot_cannot_equip"] = "Slot vacío, no se puede equipar",
                ["item_not_equippable"] = "{0} no se puede equipar",
                ["equipment_window_not_open"] = "Ventana de equipamiento no está abierta",
                ["item_equipped"] = "{0} equipado",
                ["item_cannot_equip"] = "No se puede equipar {0}",
                ["equip_error"] = "Error al equipar",
                ["empty_slot_cannot_use"] = "Slot vacío, no se puede usar",
                ["item_not_usable"] = "{0} no se puede usar",
                ["item_used"] = "{0} usado",
                ["item_cannot_use"] = "No se puede usar {0}",
                ["use_error"] = "Error al usar",
                ["no_crafting_available"] = "No hay crafting disponible",
                ["crafting_station_available"] = "Estación de crafting disponible",
                ["select_recipe_item"] = "Selecciona un item de receta",
                ["recipe_item_detected"] = "Receta detectada: {0}",
                ["not_a_recipe"] = "Este item no es una receta",
                ["craft_error"] = "Error al craftear",
                // Hotbar messages
                ["hotbar_changed"] = "Hotbar {0} seleccionado",
                ["no_weapon_equipped"] = "Sin arma equipada",
                ["empty_hotbar_slot"] = "Slot de hotbar vacío",
                ["current_hotbar_item"] = "Hotbar {0}, slot {1}: {2}",
                ["hotbar_error"] = "Error en hotbar",
                ["durability_critical"] = "durabilidad crítica {0}%",
                ["durability_low"] = "durabilidad baja {0}%",
                ["durability_normal"] = "durabilidad {0}%",
                ["durability_full"] = "durabilidad completa",
                ["hotbar_info_available"] = "Información del hotbar disponible"
            };
        }

        /// <summary>
        /// Obtiene el texto localizado para la clave especificada.
        /// Utiliza el idioma actual del juego, con fallback a inglés.
        /// </summary>
        /// <param name="key">Clave de localización</param>
        /// <param name="args">Parámetros para formatear el texto</param>
        /// <returns>Texto localizado o clave si no se encuentra</returns>
        public static string GetText(string key, params object[] args)
        {
            try
            {
                // Inicializar si es la primera vez
                if (!_isInitialized)
                {
                    Initialize();
                }

                // Intentar obtener el idioma del juego
                string language = GetCurrentLanguage();

                // Buscar en el idioma actual
                if (Translations.TryGetValue(language, out var languageDict) &&
                    languageDict.TryGetValue(key, out var translation))
                {
                    return args.Length > 0 ? string.Format(translation, args) : translation;
                }

                // Fallback a inglés
                if (language != "en" &&
                    Translations.TryGetValue("en", out var englishDict) &&
                    englishDict.TryGetValue(key, out var englishTranslation))
                {
                    return args.Length > 0 ? string.Format(englishTranslation, args) : englishTranslation;
                }

                // Último fallback - devolver la clave
                return key;
            }
            catch
            {
                // En caso de error, devolver la clave
                return key;
            }
        }

        /// <summary>
        /// Obtiene el idioma actual del juego de forma segura
        /// </summary>
        private static string GetCurrentLanguage()
        {
            try
            {
                // Intentar obtener el idioma de las preferencias del juego
                var prefs = PugOther.Manager.prefs;
                if (prefs != null)
                {
                    // El sistema de Core Keeper usa códigos de idioma estándar
                    string gameLanguage = prefs.language;
                    UnityEngine.Debug.Log($"[LocalizationManager] Game language: '{gameLanguage}'");

                    // Mapear códigos de idioma conocidos
                    string mappedLanguage = gameLanguage switch
                    {
                        "spanish" or "es" or "es-ES" => "es",
                        "english" or "en" or "en-US" => "en",
                        _ => "en" // Fallback a inglés para idiomas no soportados
                    };

                    UnityEngine.Debug.Log($"[LocalizationManager] Mapped language: '{mappedLanguage}'");
                    return mappedLanguage;
                }
                else
                {
                    UnityEngine.Debug.Log($"[LocalizationManager] Manager.prefs is null, defaulting to 'en'");
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.Log($"[LocalizationManager] Exception getting language: {ex.Message}");
            }

            // Fallback por defecto a inglés
            UnityEngine.Debug.Log($"[LocalizationManager] Using fallback language: 'en'");
            return "en";
        }

        /// <summary>
        /// Verifica si una clave de localización existe
        /// </summary>
        public static bool HasKey(string key)
        {
            // Inicializar si es la primera vez
            if (!_isInitialized)
            {
                Initialize();
            }

            string language = GetCurrentLanguage();

            if (Translations.TryGetValue(language, out var languageDict))
            {
                return languageDict.ContainsKey(key);
            }

            // Verificar en inglés como fallback
            return Translations.TryGetValue("en", out var englishDict) &&
                   englishDict.ContainsKey(key);
        }

        /// <summary>
        /// Método público para forzar la recarga de traducciones
        /// </summary>
        public static void ReloadTranslations()
        {
            lock (_initLock)
            {
                _isInitialized = false;
                Translations.Clear();
                Initialize();
            }
        }
    }
}