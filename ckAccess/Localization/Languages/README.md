# Sistema de Localización - Core Keeper Accessibility Mod

Este directorio contiene los archivos de localización para el mod de accesibilidad de Core Keeper.

## Estructura de Archivos

- `es.txt` - Traducciones en español
- `en.txt` - Traducciones en inglés (idioma fallback)

## Formato de Archivos

Los archivos de localización utilizan el formato simple `clave=valor`:

```
# Esto es un comentario
clave_ejemplo=Valor de ejemplo
virtual_cursor_initialized=Cursor virtual inicializado
```

### Reglas:

1. **Comentarios**: Las líneas que empiecen con `#` son comentarios y se ignoran
2. **Formato**: Cada línea debe seguir el formato `clave=valor`
3. **Espacios**: Los espacios al inicio y final de claves y valores se eliminan automáticamente
4. **Parámetros**: Se pueden usar parámetros como `{0}`, `{1}`, etc. para valores dinámicos

## Añadir Nuevos Idiomas

Para añadir soporte para un nuevo idioma:

1. Crea un nuevo archivo con el código del idioma (ej. `fr.txt` para francés)
2. Copia todas las claves de `en.txt` o `es.txt`
3. Traduce los valores manteniendo las claves intactas
4. El mod detectará automáticamente el nuevo idioma

## Modificar Traducciones

1. Edita el archivo del idioma correspondiente
2. Recompila el mod o reinicia el juego para cargar los cambios
3. También puedes usar `LocalizationManager.ReloadTranslations()` en código para recargar

## Añadir Nuevas Claves

1. Añade la nueva clave en `en.txt` (inglés como base)
2. Añade la traducción correspondiente en `es.txt` y otros idiomas
3. Usa `LocalizationManager.GetText("tu_nueva_clave")` en el código

## Códigos de Idioma Soportados

El sistema mapea automáticamente los códigos de idioma del juego:

- `"spanish"`, `"es"`, `"es-ES"` → `es.txt`
- `"english"`, `"en"`, `"en-US"` → `en.txt`
- Cualquier otro idioma → `en.txt` (fallback)

## Ubicación de Archivos

Los archivos de localización se copian automáticamente durante la compilación:

- **Desarrollo**: `ckAccess/Localization/Languages/`
- **Mod compilado**: `ckAccess.dll` directory `/Localization/Languages/`
- **BepInEx plugins**: `BepInEx/plugins/Localization/Languages/`