# Problemas Conocidos / Known Issues

## 🔴 CRÍTICO: Input bloqueado después de la intro

**Estado:** Pendiente de corrección

**Descripción:**
Después de que la introducción narrativa termina (al crear un nuevo personaje), el input del jugador queda bloqueado:
- ❌ No se puede mover el personaje con WASD
- ❌ No se puede abrir el inventario
- ❌ Los clicks no funcionan
- ✅ El cursor virtual SÍ funciona (I/J/K/L)

**Workaround temporal:**
1. Presionar ESC para ir al menú principal
2. Volver a cargar el personaje
3. El input funciona correctamente después de recargar

**Causa probable:**
El campo `InputManager.activeInputField` no se limpia correctamente después de la intro, dejando el sistema de input en estado "escritura" aunque no haya ningún campo de texto activo.

**Intentos de solución:**
1. ✅ Patch en `IntroHandler.LoadNextScene()` - No suficiente
2. ✅ Patch en `IntroHandler.Update()` cuando `slidesDone=true` - No suficiente
3. ✅ Patch agresivo en `PlayerController.ManagedUpdate()` cada 0.5s - No funciona

**Archivos relacionados:**
- `ckAccess/Patches/UI/IntroSlideTextPatch.cs`
  - `IntroStartPatch` - Anuncia mensaje de skip
  - `IntroSlideTextPatch` - Lee texto de slides
  - `IntroEndCleanupPatch` - Limpia input en LoadNextScene
  - `IntroUpdateCleanupPatch` - Limpia input cuando slidesDone=true
- `ckAccess/Patches/Player/ForceInputUnlockPatch.cs` - Desbloqueo agresivo (no funciona)

**Siguiente paso sugerido:**
Investigar si hay otros campos o estados del `InputManager` que deban resetearse, o si el problema está en otro sistema (como `PlayerController` o el sistema de input de Rewired).

---

## ✅ Funcionalidades completadas en esta sesión:

### Sistema de mensajes y diálogos accesibles
- ✅ **ChatWindow**: Lectura de mensajes del sistema (items recogidos, level ups, etc.)
- ✅ **Emote**: Lectura de mensajes contextuales (tutoriales, herramientas, etc.)
- ✅ **Intro narrativa**: Lectura de diálogos de la introducción del juego
- ✅ **Aviso de skip**: Anuncia "Mantén espacio para saltar la introducción"

### Sistema de buffer de tutoriales
- ✅ Detección automática de mensajes de tutorial
- ✅ Navegación con teclas ' y , (como sistema de notificaciones)
- ✅ Saltos rápidos con Shift+' y Shift+,
- ✅ Anuncios con posición: "X de Y. [mensaje]"

### Mejoras al cursor virtual
- ✅ No interfiere al escribir nombres en menús
- ✅ Detección mejorada de campos de texto activos
- ✅ Solo se activa en gameplay real
- ✅ Verificación mediante `InputManager.textInputIsActive`

### Localización
- ✅ Nuevas claves en 2 idiomas (es, en):
  - `intro_skip_message` - Mensaje de cómo saltar la intro
  - `no_tutorials` - Mensaje cuando no hay tutoriales en buffer

**Archivos nuevos creados:**
- `ckAccess/Patches/UI/ChatWindowAccessibilityPatch.cs`
- `ckAccess/Patches/UI/EmoteAccessibilityPatch.cs`
- `ckAccess/Patches/UI/IntroSlideTextPatch.cs`
- `ckAccess/Patches/Player/ForceInputUnlockPatch.cs`
- `ckAccess/Tutorials/TutorialBuffer.cs`
- `ckAccess/Tutorials/TutorialNavigationPatch.cs`

---

## Contacto
Para reportar problemas o sugerir soluciones, abrir un issue en GitHub.
