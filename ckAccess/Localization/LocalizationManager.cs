extern alias PugOther;
using System.Collections.Generic;

namespace ckAccess.Localization
{
    /// <summary>
    /// Sistema de localización centralizado para el mod de accesibilidad.
    /// Proporciona fallback a inglés cuando no hay localización disponible.
    /// </summary>
    public static class LocalizationManager
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
        {
            ["en"] = new()
            {
                // Virtual Cursor Messages
                ["virtual_cursor_initialized"] = "Virtual cursor initialized",
                ["cursor_initialization_error"] = "Error initializing cursor",
                ["cursor_reset"] = "Cursor reset: x={0}, z={1}",
                ["cursor_reset_error"] = "Error resetting cursor",
                ["limit_reached"] = "Limit reached",
                ["primary_action_error"] = "Error in primary action: {0}",
                ["placing_at_position"] = "Placing at position x={0}, z={1}",
                ["secondary_action_error"] = "Error in secondary action: {0}",
                ["interaction_error"] = "Error in interaction: {0}",
                ["cursor_not_initialized"] = "Cursor not initialized",
                ["current_position"] = "Current position: x={0}, z={1}",
                ["position_reading_error"] = "Error reading player position",

                // UI Navigation Messages
                ["no_tabs_available"] = "No tabs available",
                ["no_more_tabs"] = "No more tabs available",
                ["tab_equipment"] = "Tab: Equipment",
                ["tab_skills"] = "Tab: Skills",
                ["tab_souls"] = "Tab: Souls",
                ["tab_number"] = "Tab {0}",
                ["navigation_error"] = "Error navigating tabs: {0}",
                ["selection_error"] = "Error selecting object: {0}",
                ["empty_slot"] = "Empty slot",
                ["slot_selection_error"] = "Error in slot selection: {0}",
                ["skill_error"] = "Error opening skill: {0}",
                ["click_simulation_error"] = "Error simulating click: {0}",
                ["no_element_selected"] = "No element selected",
                ["no_element_selected_secondary"] = "No element selected for secondary action",
                ["secondary_action_error_ui"] = "Error in secondary action: {0}",
                ["empty_slot_no_action"] = "Empty slot, no action available",
                ["secondary_action_performed"] = "Secondary action performed",
                ["secondary_slot_error"] = "Error in slot secondary action: {0}",
                ["right_click_simulation_error"] = "Error simulating right click: {0}",
                ["talent_action"] = "Action on talent: {0}",
                ["talent_selected"] = "Talent selected",
                ["talent_selection_error"] = "Error selecting talent: {0}",
                ["skills"] = "Skills",
                ["preset_selection_error"] = "Error selecting preset: {0}",
                ["preset_selected"] = "Preset selected: {0}",
                ["equipment_preset_selected"] = "Equipment preset selected",
                ["equipment_preset_selected_generic"] = "Equipment preset {0} selected",
                ["equipment_preset_error"] = "Error with equipment preset: {0}",
                ["equipment_preset_not_found"] = "Equipment preset not found",
                ["stats_window_toggled"] = "Stats window toggled",
                ["stats"] = "Stats",
                ["stats_button_activated"] = "Stats button activated",
                ["stats_error"] = "Error opening stats: {0}",
                ["selected_item"] = "Selected: {0}",
                ["talent_action"] = "Action on talent: {0}",

                // Skill and Talent Messages
                ["talent_tree_opened"] = "Talent tree opened: {0}",
                ["talent_tree_opened_generic"] = "Talent tree opened",
                ["talent_tree_closed"] = "Talent tree closed",
                ["talent"] = "Talent",

                // Skill Names
                ["skill_mining"] = "Mining",
                ["skill_running"] = "Running",
                ["skill_melee"] = "Melee",
                ["skill_vitality"] = "Vitality",
                ["skill_crafting"] = "Crafting",
                ["skill_range"] = "Range",
                ["skill_gardening"] = "Gardening",
                ["skill_fishing"] = "Fishing",
                ["skill_cooking"] = "Cooking",
                ["skill_magic"] = "Magic",
                ["skill_summoning"] = "Summoning",
                ["skill_explosives"] = "Explosives",

                // World Reader Messages
                ["empty"] = "Empty",
                ["reading_error"] = "Reading error",
                ["the_core"] = "The Core (interactable)",
                ["work_station"] = "{0} (work station)",
                ["enemy"] = "{0} (enemy)",
                ["object"] = "{0} (object)",
                ["plant"] = "{0} (plant)",
                ["resource"] = "{0} (resource)",
                ["entity"] = "{0} (entity)",
                ["destructible_tool"] = "Destructible {0} (use {1})",
                ["destructible_material_tool"] = "Destructible {1} {0} (use {2})",
                ["blocking"] = "Blocking {0}",
                ["blocking_material"] = "Blocking {1} {0}",
                ["dangerous"] = "Dangerous {0}",
                ["dangerous_material"] = "Dangerous {1} {0}",
                ["area_not_available"] = "Area not available",
                ["unknown_position"] = "Unknown position",
                ["unknown"] = "Unknown",
                ["player_position_error"] = "Could not get player position",
                ["cannot_read_player_position"] = "Cannot read player position",
                ["position_reading_error_generic"] = "Error reading position",
                ["empty_area_around"] = "Empty area around",
                ["area_scan_error"] = "Error scanning area",

                // Item Status
                ["equipped"] = ", equipped",

                // UI Elements
                ["stats_button"] = "Stats button: {0}",
                ["equipment_preset"] = "Equipment preset: {0}",
                ["crafting_button"] = "Crafting button: {0}",
                ["organize_button"] = "Organize button: {0}",
                ["quick_action_button"] = "Quick action button: {0}",
                ["button"] = "Button: {0}",
                ["tab"] = "Tab: {0}",
                ["preset"] = "Preset: {0}",
                ["statistics"] = "Statistics: {0}",
                ["bag"] = "Bag: {0}",
                ["shortcut"] = "Shortcut: {0}",
                ["element"] = "Element: {0}",

                // Pop-up Messages
                ["hold_to_confirm"] = "hold to confirm",

                // Menu Options
                ["more_options"] = "More options",

                // Tile Types
                ["tile_none"] = "Empty",
                ["tile_ground"] = "Ground",
                ["tile_wall"] = "Wall",
                ["tile_water"] = "Water",
                ["tile_pit"] = "Pit",
                ["tile_bridge"] = "Bridge",
                ["tile_floor"] = "Built floor",
                ["tile_roof_hole"] = "Roof hole",
                ["tile_thin_wall"] = "Thin wall",
                ["tile_dug_up_ground"] = "Dug up ground",
                ["tile_watered_ground"] = "Watered ground",
                ["tile_circuit_plate"] = "Circuit plate",
                ["tile_ancient_circuit_plate"] = "Ancient circuit plate",
                ["tile_fence"] = "Fence",
                ["tile_rug"] = "Rug",
                ["tile_small_stones"] = "Small stones",
                ["tile_small_grass"] = "Small grass",
                ["tile_wall_grass"] = "Wall grass",
                ["tile_debris"] = "Debris",
                ["tile_floor_crack"] = "Floor crack",
                ["tile_rail"] = "Rail",
                ["tile_great_wall"] = "Great wall",
                ["tile_lit_floor"] = "Lit floor",
                ["tile_debris2"] = "Debris",
                ["tile_loose_flooring"] = "Loose flooring",
                ["tile_immune"] = "Immune zone",
                ["tile_wall_crack"] = "Wall crack",
                ["tile_ore"] = "Ore vein",
                ["tile_big_root"] = "Big root",
                ["tile_ground_slime"] = "Slime trail",
                ["tile_ancient_crystal"] = "Ancient crystal",
                ["tile_chrysalis"] = "Chrysalis",

                // Tile Categories
                ["category_construction"] = "Construction",
                ["category_liquid"] = "Liquid",
                ["category_mineral"] = "Mineral",
                ["category_organic"] = "Organic",
                ["category_stone"] = "Stone",
                ["category_crystal"] = "Crystal",
                ["category_unknown"] = "Unknown",

                // Tile Material Categories
                ["material_earth"] = "Earth",
                ["material_stone"] = "Stone",
                ["material_mineral"] = "Mineral",
                ["material_liquid"] = "Liquid",
                ["material_construction"] = "Construction",
                ["material_crystal"] = "Crystal",
                ["material_organic"] = "Organic",
                ["material_unknown"] = "Unknown",

                // Footstep Sounds
                ["sound_earth"] = "Earth",
                ["sound_stone"] = "Stone",
                ["sound_metal"] = "Metal",
                ["sound_splash"] = "Splash",
                ["sound_wood"] = "Wood",
                ["sound_gravel"] = "Gravel",
                ["sound_textile"] = "Textile",
                ["sound_neutral"] = "Neutral",

                // Tools
                ["tool_pickaxe"] = "Pickaxe",
                ["tool_high_quality_pickaxe"] = "High quality pickaxe",
                ["tool_axe"] = "Axe",
                ["tool_shovel"] = "Shovel",
                ["tool_pickaxe_or_shovel"] = "Pickaxe or Shovel",
                ["tool_none"] = "None",

                // Tileset Materials
                ["tileset_dirt"] = "dirt",
                ["tileset_stone"] = "stone",
                ["tileset_obsidian"] = "obsidian",
                ["tileset_lava"] = "lava",
                ["tileset_nature"] = "nature",
                ["tileset_mold"] = "mold",
                ["tileset_sea"] = "sea",
                ["tileset_sand"] = "sand",
                ["tileset_desert"] = "desert",
                ["tileset_snow"] = "snow",
                ["tileset_crystal"] = "crystal",
                ["tileset_dark_stone"] = "dark stone",

                // Tile Descriptions
                ["tile_with_material"] = "{1} {0}",

                // Inventory Messages
                ["inventory_opened"] = "Inventory opened",
                ["inventory_closed"] = "Inventory closed",
                ["character_window_opened"] = "Character window opened",
                ["character_window_closed"] = "Character window closed"
            },

            ["es"] = new()
            {
                // Virtual Cursor Messages
                ["virtual_cursor_initialized"] = "Cursor virtual inicializado",
                ["cursor_initialization_error"] = "Error inicializando cursor",
                ["cursor_reset"] = "Cursor reseteado: x={0}, z={1}",
                ["cursor_reset_error"] = "Error reseteando cursor",
                ["limit_reached"] = "Límite alcanzado",
                ["primary_action_error"] = "Error en acción primaria: {0}",
                ["placing_at_position"] = "Colocando en posición x={0}, z={1}",
                ["secondary_action_error"] = "Error en acción secundaria: {0}",
                ["interaction_error"] = "Error en interacción: {0}",
                ["cursor_not_initialized"] = "Cursor no inicializado",
                ["current_position"] = "Posición actual: x={0}, z={1}",
                ["position_reading_error"] = "Error leyendo posición del jugador",

                // UI Navigation Messages
                ["no_tabs_available"] = "No hay pestañas disponibles",
                ["no_more_tabs"] = "No hay más pestañas disponibles",
                ["tab_equipment"] = "Pestaña: Equipamiento",
                ["tab_skills"] = "Pestaña: Habilidades",
                ["tab_souls"] = "Pestaña: Almas",
                ["tab_number"] = "Pestaña {0}",
                ["navigation_error"] = "Error navegando pestañas: {0}",
                ["selection_error"] = "Error seleccionando objeto: {0}",
                ["empty_slot"] = "Slot vacío",
                ["slot_selection_error"] = "Error en selección de slot: {0}",
                ["skill_error"] = "Error abriendo habilidad: {0}",
                ["click_simulation_error"] = "Error simulando click: {0}",
                ["no_element_selected"] = "Ningún elemento seleccionado",
                ["no_element_selected_secondary"] = "Ningún elemento seleccionado para acción secundaria",
                ["secondary_action_error_ui"] = "Error en acción secundaria: {0}",
                ["empty_slot_no_action"] = "Slot vacío, no hay acción disponible",
                ["secondary_action_performed"] = "Acción secundaria realizada",
                ["secondary_slot_error"] = "Error en acción secundaria de slot: {0}",
                ["right_click_simulation_error"] = "Error simulando click derecho: {0}",
                ["talent_action"] = "Acción en talento: {0}",
                ["talent_selected"] = "Talento seleccionado",
                ["talent_selection_error"] = "Error seleccionando talento: {0}",
                ["skills"] = "Habilidades",
                ["preset_selection_error"] = "Error seleccionando preset: {0}",
                ["preset_selected"] = "Preset seleccionado: {0}",
                ["equipment_preset_selected"] = "Preset de equipo seleccionado",
                ["equipment_preset_selected_generic"] = "Preset de equipo {0} seleccionado",
                ["equipment_preset_error"] = "Error con preset de equipo: {0}",
                ["equipment_preset_not_found"] = "Preset de equipo no encontrado",
                ["stats_window_toggled"] = "Ventana de estadísticas alternada",
                ["stats"] = "Estadísticas",
                ["stats_button_activated"] = "Botón de estadísticas activado",
                ["stats_error"] = "Error abriendo estadísticas: {0}",
                ["selected_item"] = "Seleccionado: {0}",
                ["talent_action"] = "Acción en talento: {0}",

                // Skill and Talent Messages
                ["talent_tree_opened"] = "Árbol de talentos abierto: {0}",
                ["talent_tree_opened_generic"] = "Árbol de talentos abierto",
                ["talent_tree_closed"] = "Árbol de talentos cerrado",
                ["talent"] = "Talento",

                // Skill Names
                ["skill_mining"] = "Minería",
                ["skill_running"] = "Correr",
                ["skill_melee"] = "Combate Cuerpo a Cuerpo",
                ["skill_vitality"] = "Vitalidad",
                ["skill_crafting"] = "Fabricación",
                ["skill_range"] = "Combate a Distancia",
                ["skill_gardening"] = "Jardinería",
                ["skill_fishing"] = "Pesca",
                ["skill_cooking"] = "Cocina",
                ["skill_magic"] = "Magia",
                ["skill_summoning"] = "Invocación",
                ["skill_explosives"] = "Explosivos",

                // World Reader Messages
                ["empty"] = "Vacío",
                ["reading_error"] = "Error de lectura",
                ["the_core"] = "El Núcleo (interactuable)",
                ["work_station"] = "{0} (estación de trabajo)",
                ["enemy"] = "{0} (enemigo)",
                ["object"] = "{0} (objeto)",
                ["plant"] = "{0} (planta)",
                ["resource"] = "{0} (recurso)",
                ["entity"] = "{0} (entidad)",
                ["destructible_tool"] = "{0} destructible (usar {1})",
                ["destructible_material_tool"] = "{0} de {1} destructible (usar {2})",
                ["blocking"] = "{0} (bloqueante)",
                ["blocking_material"] = "{0} de {1} (bloqueante)",
                ["dangerous"] = "{0} (peligroso)",
                ["dangerous_material"] = "{0} de {1} (peligroso)",
                ["area_not_available"] = "Área no disponible",
                ["unknown_position"] = "Posición desconocida",
                ["unknown"] = "Desconocido",
                ["player_position_error"] = "No se pudo obtener la posición del jugador",
                ["cannot_read_player_position"] = "No se puede leer la posición del jugador",
                ["position_reading_error_generic"] = "Error leyendo posición",
                ["empty_area_around"] = "Área vacía alrededor",
                ["area_scan_error"] = "Error escaneando área",

                // Item Status
                ["equipped"] = ", equipado",

                // UI Elements
                ["stats_button"] = "Botón de estadísticas: {0}",
                ["equipment_preset"] = "Preset de equipo: {0}",
                ["crafting_button"] = "Botón de crafteo: {0}",
                ["organize_button"] = "Botón de organizar: {0}",
                ["quick_action_button"] = "Botón de acción rápida: {0}",
                ["button"] = "Botón: {0}",
                ["tab"] = "Pestaña: {0}",
                ["preset"] = "Preset: {0}",
                ["statistics"] = "Estadísticas: {0}",
                ["bag"] = "Bolsa: {0}",
                ["shortcut"] = "Atajo: {0}",
                ["element"] = "Elemento: {0}",

                // Pop-up Messages
                ["hold_to_confirm"] = "mantén pulsado para confirmar",

                // Menu Options
                ["more_options"] = "Más opciones",

                // Tile Types
                ["tile_none"] = "Vacío",
                ["tile_ground"] = "Suelo",
                ["tile_wall"] = "Pared",
                ["tile_water"] = "Agua",
                ["tile_pit"] = "Hoyo",
                ["tile_bridge"] = "Puente",
                ["tile_floor"] = "Suelo construido",
                ["tile_roof_hole"] = "Agujero en el techo",
                ["tile_thin_wall"] = "Pared delgada",
                ["tile_dug_up_ground"] = "Tierra excavada",
                ["tile_watered_ground"] = "Tierra regada",
                ["tile_circuit_plate"] = "Placa de circuito",
                ["tile_ancient_circuit_plate"] = "Placa de circuito antigua",
                ["tile_fence"] = "Valla",
                ["tile_rug"] = "Alfombra",
                ["tile_small_stones"] = "Piedras pequeñas",
                ["tile_small_grass"] = "Hierba pequeña",
                ["tile_wall_grass"] = "Hierba de pared",
                ["tile_debris"] = "Escombros",
                ["tile_floor_crack"] = "Grieta en el suelo",
                ["tile_rail"] = "Riel",
                ["tile_great_wall"] = "Gran pared",
                ["tile_lit_floor"] = "Suelo iluminado",
                ["tile_debris2"] = "Escombros",
                ["tile_loose_flooring"] = "Suelo suelto",
                ["tile_immune"] = "Zona inmune",
                ["tile_wall_crack"] = "Grieta en la pared",
                ["tile_ore"] = "Veta de mineral",
                ["tile_big_root"] = "Raíz grande",
                ["tile_ground_slime"] = "Rastro de slime",
                ["tile_ancient_crystal"] = "Cristal antiguo",
                ["tile_chrysalis"] = "Crisálida",

                // Tile Categories
                ["category_construction"] = "Construcción",
                ["category_liquid"] = "Líquido",
                ["category_mineral"] = "Mineral",
                ["category_organic"] = "Orgánico",
                ["category_stone"] = "Piedra",
                ["category_crystal"] = "Cristal",
                ["category_unknown"] = "Desconocido",

                // Tile Material Categories
                ["material_earth"] = "Tierra",
                ["material_stone"] = "Piedra",
                ["material_mineral"] = "Mineral",
                ["material_liquid"] = "Líquido",
                ["material_construction"] = "Construcción",
                ["material_crystal"] = "Cristal",
                ["material_organic"] = "Orgánico",
                ["material_unknown"] = "Desconocido",

                // Footstep Sounds
                ["sound_earth"] = "Tierra",
                ["sound_stone"] = "Piedra",
                ["sound_metal"] = "Metal",
                ["sound_splash"] = "Chapoteo",
                ["sound_wood"] = "Madera",
                ["sound_gravel"] = "Grava",
                ["sound_textile"] = "Textil",
                ["sound_neutral"] = "Neutral",

                // Tools
                ["tool_pickaxe"] = "Pico",
                ["tool_high_quality_pickaxe"] = "Pico de alta calidad",
                ["tool_axe"] = "Hacha",
                ["tool_shovel"] = "Pala",
                ["tool_pickaxe_or_shovel"] = "Pico o Pala",
                ["tool_none"] = "Ninguna",

                // Tileset Materials
                ["tileset_dirt"] = "tierra",
                ["tileset_stone"] = "piedra",
                ["tileset_obsidian"] = "obsidiana",
                ["tileset_lava"] = "lava",
                ["tileset_nature"] = "naturaleza",
                ["tileset_mold"] = "moho",
                ["tileset_sea"] = "mar",
                ["tileset_sand"] = "arena",
                ["tileset_desert"] = "desierto",
                ["tileset_snow"] = "nieve",
                ["tileset_crystal"] = "cristal",
                ["tileset_dark_stone"] = "piedra oscura",

                // Tile Descriptions
                ["tile_with_material"] = "{0} de {1}",

                // Inventory Messages
                ["inventory_opened"] = "Inventario abierto",
                ["inventory_closed"] = "Inventario cerrado",
                ["character_window_opened"] = "Ventana de personaje abierta",
                ["character_window_closed"] = "Ventana de personaje cerrada"
            }
        };

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
            string language = GetCurrentLanguage();

            if (Translations.TryGetValue(language, out var languageDict))
            {
                return languageDict.ContainsKey(key);
            }

            // Verificar en inglés como fallback
            return Translations.TryGetValue("en", out var englishDict) &&
                   englishDict.ContainsKey(key);
        }
    }
}