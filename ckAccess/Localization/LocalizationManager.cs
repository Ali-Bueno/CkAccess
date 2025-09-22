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
                ["skills"] = "Skills"
            };

            Translations["es"] = new Dictionary<string, string>
            {
                ["empty"] = "Vacío",
                ["unknown"] = "Desconocido",
                ["error"] = "Error",
                ["empty_slot"] = "Slot vacío",
                ["talent"] = "Talento",
                ["skills"] = "Habilidades"
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