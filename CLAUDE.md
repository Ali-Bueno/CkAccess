* esto es un proyecto llamado Core Keeper Accessibility Mod, un mod con objetivo para poder accesibilizar el juego core keeper para personas ciegas: El código debe ser modular. No crear archivos de parche grandes. Al añadir una nueva funcionalidad, está prohibido romper algo que ya funcione.
* antes de ejecutar una tarea, siempre mirar el código actual que ya tenemos, en la carpeta ckAcces y sus subcarpetas,  para no implementar algo nuevo y que acabe siendo peor que la implementación que ya teníamos antes.
* al ejecutar una tarea que el usuario pida, siempre estudiar la mejor forma de implementar, que sea menos invasiva para el juego y estudiar el código que está en la carpeta `ck code`. En caso de no poder accederse a campos y métodos privados, usar reflexión. también no olvidar/tocar los alias en el csproj, que ya están puestos aí.
* **Ubicación de Referencias:** Las DLLs del juego se encuentran en `D:\games\steam\steamapps\common\Core Keeper\CoreKeeper_Data\Managed`. La única excepción es Tolk, que permanece en la carpeta `references` del proyecto.
* Siempre procurar hacer el código por parches pequeños, analizando bien la estructura de las carpetas y de los parches anteriores para tomarse como ejemplo.
* Los parches de menús deben ser lo menos invasivo posible para evitar cambios en el comportamiento del juego.
* Al buscar en el código del juego, ignorar siempre los patrones de .gitignore para asegurar un análisis completo. **Recordatorio: Forzar siempre la opción para ignorar .gitignore en las herramientas de búsqueda de archivos, ya que a veces se olvida.**
* **Advertencias de Compilación:** Siempre corregir TODAS las advertencias del compilador. Para warnings de Harmony003 (acceso a miembros de structs en parámetros), copiar el parámetro a una variable local antes de acceder a sus miembros. Para falsos positivos de analizadores, usar `#pragma warning disable` con comentario explicativo.
* **Convención de Referencias:** Todas las clases de la interfaz de usuario (UI) del juego se encuentran en el ensamblado `Pug.Other.dll`. Para acceder a ellas, se debe utilizar el alias `PugOther` (ej. `PugOther.InventorySlotUI`). No existe un `Pug.UI.dll` separado.
* **Convención de Git:** TODOS los commits deben ser escritos en inglés, siguiendo las mejores prácticas de commits convencionales.

---
### Información del Repositorio
*   **URL:** git@github.com:Ali-Bueno/CkAccess.git
*   **Rama Principal:** main
---
### Principios de Accesibilidad

*   **Librería de Salida:** Se utiliza **Tolk** como única librería para la comunicación con lectores de pantalla.
*   **Implementación:** No es necesario implementar nuevos sistemas de accesibilidad. La verbalización de los elementos de la interfaz se gestiona través de la clase `UIManager`, que centraliza las llamadas a Tolk. Para añadir accesibilidad a un nuevo menú, se debe seguir el patrón de los parches existentes.

---
### Arquitectura de Parches de UI

Se ha implementado una arquitectura de parches centralizada y robusta para gestionar la accesibilidad de los menús del juego. Este enfoque se divide en dos responsabilidades principales: la **lectura de opciones**, la **gestión del foco inicial** y la **corrección de anuncios duplicados**.

#### 1. Lectura de Opciones (`RadicalMenuPatch.cs`)

Este es el parche universal que se encarga de verbalizar las opciones de los menús.

*   **Objetivo:** `PugOther.RadicalMenu`
*   **Métodos Parcheados:** `OnSelectedOptionChanged`, `SkimLeft`, `SkimRight`.
*   **Funcionamiento:**
    1.  **Manejador General:** La lógica principal busca dinámicamente todos los componentes de texto (`PugText`) dentro de la opción de menú seleccionada. Utiliza el método `ProcessText()` de cada componente para obtener el texto final (con placeholders resueltos) y los anuncia. Esto proporciona una cobertura base para la mayoría de los menús.
    2.  **Manejadores Específicos:** Para menús con controles complejos o sin texto visible, el parche implementa manejadores específicos basados en el tipo de la opción seleccionada:
        *   `CharacterCustomizationOption_Selection`: Distingue entre opciones de **apariencia** (construyendo un texto como "Estilo X de Y") y opciones de **ocupación** (leyendo y formateando los múltiples campos de texto del tooltip).
        *   `CharacterTypeOption_Selection`: Lee tanto el tipo de personaje como su descripción detallada.
        *   `WorldSlotMoreOption` y `WorldSlotDeleteOption`: Anuncia un texto localizado o manual para estos botones, que carecen de texto visible.
        *   **`RadicalJoinGameMenu_JoinMethodDropdown`:** Se ha añadido un parche específico para este componente. En lugar de parchear el menú principal, se parchea el método `NavigateInternally` del propio dropdown. Esto permite interceptar la navegación con flechas *dentro* de la lista de opciones (ID o IP) y anunciar el texto del elemento recién seleccionado, solucionando el problema de la falta de verbalización.

#### 2. Gestión del Foco Inicial

Este apartado describe los parches responsables de asegurar que la navegación por teclado funcione correctamente al entrar o regresar a un menú.

*   **`MenuActivatePatches.cs`:**
    *   **Objetivo:** Varios menús (`WorldSettingsMenu`, `CharacterCustomizationMenu`, `SelectWorldMenu`, `CharacterTypeSelectionMenu`, `ChooseCharacterMenu`, `SelectWorldMenu`).
    *   **Método Parcheado:** `Activate` en cada menú objetivo.
    *   **Funcionamiento:** Utiliza una corrutina (`ForceSafeSelectionCoroutine`) para ejecutar la lógica de enfoque después de que el menú se haya inicializado completamente. Desactiva a la fuerza el "modo de escritura" del juego (`Manager.input.SetActiveInputField(null)`) y selecciona la primera opción interactuable que no sea un campo de texto, o la primera opción disponible como fallback. Esto es crucial para evitar el bloqueo del teclado.

*   **`WorldSettingsMenuPatches.cs`:**
    *   **Objetivo:** `PugOther.WorldSettingsMenu`
    *   **Método Parcheado:** `ActivateMenuIndex`
    *   **Funcionamiento:** Utiliza una corrutina (`DelayedFocusReset`) lanzada desde un `Postfix` para asegurar que, después de cambiar de pestaña en el menú de ajustes del mundo, el foco se restablezca correctamente. Desactiva cualquier campo de texto activo y selecciona la primera opción interactuable en la nueva pestaña, previniendo bloqueos de input.

#### 3. Corrección de Anuncios Duplicados (Debounce)

Se ha detectado que en ciertos menús, como los slots de mundo y personaje, el evento `OnSelectedOptionChanged` se dispara dos veces consecutivas, provocando que el lector de pantalla anuncie la misma opción dos veces.

*   **Solución:** Se ha implementado una técnica de "debounce" en `RadicalMenuPatch.cs`.
*   **Funcionamiento:**
    1.  Se guarda una marca de tiempo (`lastAnnounceTime`) y el texto del último anuncio (`lastAnnouncedText`).
    2.  Antes de verbalizar una nueva opción, se comprueba si el texto es idéntico al anterior y si ha transcurrido un tiempo mínimo (50 milisegundos).
    3.  Si ambas condiciones se cumplen, el segundo anuncio se ignora. Esto filtra eficazmente las llamadas duplicadas sin afectar la capacidad de respuesta del usuario.

#### 4. Navegación por Teclado y D-Pad en Inventarios (`UIMouseInputPatch.cs`) ✅ **CORREGIDO**

Se ha implementado un sistema robusto y sin conflictos para permitir la navegación por los slots del inventario, crafteo y menús similares utilizando el teclado (WASD/Flechas) y el D-Pad del mando de forma independiente.

*   **Objetivo:** `PugOther.UIMouse`
*   **Método Parcheado:** `UpdateMouseUIInput` (con un parche `Prefix`).
*   **Funcionamiento:**
    1.  **Estrategia `Prefix`:** Se utiliza un parche `Prefix` para interceptar el input *antes* de que el método original del juego se ejecute. Esto es crucial para evitar conflictos, ya que el juego consume el input del mando, impidiendo que un parche `Postfix` lo detecte.
    2.  **Condición de Alcance:** El parche se desactiva inmediatamente si no hay ninguna ventana de inventario abierta (`Manager.ui.isAnyInventoryShowing`). Esto asegura que la navegación solo afecte a los menús relevantes y no al juego normal.
    3.  **Detección de Input:**
        *   **Teclado:** Se utiliza `UnityEngine.Input.GetKeyDown()` para detectar pulsaciones únicas de las teclas WASD y las flechas.
        *   **D-Pad:** Tras un proceso de depuración, se descubrió que el D-Pad no está vinculado a las acciones de navegación del joystick (`MENU_UP`, etc.). Para identificar las acciones correctas, se implementó un "escucha" de eventos universal de Rewired (`player.AddInputEventDelegate`). El análisis del log reveló que, en el inventario, el D-Pad está mapeado a acciones no intuitivas:
            *   **Arriba:** `SwapNextHotbar`
            *   **Abajo:** `SwapPreviousHotbar`
            *   **Izquierda:** `QuickStack`
            *   **Derecha:** `Sort`
        El parche utiliza `ReInput.players.GetPlayer(0).GetButtonDown()` con estos nombres de acción para detectar pulsaciones únicas del D-Pad.
    4.  **Lógica de Navegación:**
        *   El método `DetectNavigationInput()` unifica la detección de WASD/Flechas y D-Pad, retornando una dirección (`Direction.Id`).
        *   Si se detecta una pulsación, se llama a `HandleKeyboardNavigation()` que utiliza el método nativo del juego `currentSelectedUIElement.GetAdjacentUIElement()` para encontrar el siguiente slot en la dirección deseada.
        *   Se actualiza **directamente** la posición del puntero del ratón (`uiMouse.pointer.position`) al nuevo elemento, sin variables intermedias.
        *   Se invoca el método privado `TrySelectNewElement()` mediante reflexión para seleccionar oficialmente el nuevo slot y disparar todos los eventos asociados (como la verbalización del contenido del slot).
    5.  **Prevención de Interferencias del Ratón:** El parche `GetMouseUIViewPosition_Prefix` intercepta la posición del ratón cuando hay inventario abierto y retorna la posición del elemento seleccionado en lugar de la posición física del ratón. Esto previene que el movimiento del ratón físico interfiera con la navegación por teclado/D-Pad.
    6.  **Sin Conflictos entre Métodos de Input:** La implementación actualiza directamente el puntero sin usar variables estáticas que puedan causar conflictos. Esto permite cambiar libremente entre teclado y D-Pad sin que uno bloquee al otro.

*   **Corrección de Bug Importante (2025-01-03):**
    *   **Problema:** La navegación con D-Pad se quedaba atascada en el último slot seleccionado con teclado. Esto ocurría porque se usaba una variable estática `_forcedPointerPosition` que nunca se limpiaba, interfiriendo con la navegación nativa del D-Pad.
    *   **Solución:** Eliminada la variable `_forcedPointerPosition`. En su lugar, se actualiza directamente `uiMouse.pointer.position` en el momento de la navegación. Esto hace que ambos métodos de input (teclado y D-Pad) funcionen de forma independiente y sin conflictos.

#### 5. Accesibilidad de Habilidades y Talentos

Se ha implementado un sistema completo para la accesibilidad de habilidades (Skills) y sus árboles de talento correspondientes.

##### 5.1. Accesibilidad de Habilidades (`SkillAccessibilityPatch.cs`)

*   **Objetivo:** `PugOther.ButtonUIElement` (componente base de las habilidades)
*   **Método Parcheado:** `OnSelected`, `OnDeselected`
*   **Funcionamiento:**
    1.  **Detección Específica:** El parche detecta cuando un `ButtonUIElement` tiene un componente `SkillUIElement` asociado y está en contexto de inventario.
    2.  **Información Completa:** Al seleccionar una habilidad, se anuncia:
        *   Nombre de la habilidad con nivel actual
        *   Puntos de talento disponibles para esa habilidad
        *   Estadísticas y efectos actuales
        *   Descripción de la habilidad (filtrando instrucciones de click)
    3.  **Cache Inteligente:** Previene anuncios duplicados usando un identificador único por habilidad.

##### 5.2. Árboles de Talento (`SkillTalentTreePatch.cs`)

*   **Objetivo:** `PugOther.SkillTalentTreeUI` y `PugOther.SkillTalentUIElement`
*   **Métodos Parcheados:**
    *   `ShowTalentTree`, `HideTalentTree` (para el árbol completo)
    *   `OnSelected`, `OnDeselected`, `OnLeftClicked` (para elementos individuales)
*   **Funcionamiento:**
    1.  **Apertura/Cierre de Árboles:** Anuncia cuando se abre o cierra un árbol de talentos, incluyendo el nombre de la habilidad.
    2.  **Información de Talentos:** Al navegar por los talentos individuales, se anuncia:
        *   Nombre del talento
        *   Efectos/estadísticas que proporciona
        *   Descripción detallada del talento
    3.  **Confirmación de Acciones:** Al usar U en un talento, se confirma la acción realizada (invertir punto de talento).

##### 5.3. Integración con Sistema de Interacción (`InventoryUIInputPatch.cs`)

*   **Integración U/O:** Las teclas U y O se integran perfectamente con el sistema de habilidades:
    *   **U en Habilidad:** Abre el árbol de talentos correspondiente
    *   **U en Talento:** Invierte un punto de talento (si es posible)
    *   **O:** Acciones secundarias contextuales
*   **Detección Automática:** El sistema detecta automáticamente el tipo de elemento seleccionado y ejecuta la acción apropiada.
*   **Feedback Inmediato:** Cada acción proporciona confirmación por TTS.

### Plan de Desarrollo

- **Completado:** Refactorización y centralización de todos los parches de menús.
- **Completado:** Accesibilidad del menú principal, menú de ajustes, slots de mundo, selección de tipo de personaje y menú de personalización de personaje.
- **Completado:** Accesibilidad del menú de unirse a partida, incluyendo la navegación por teclado y la lectura de opciones del dropdown.
- **Completado:** Accesibilizar los slots de personajes.
- **Completado:** Refactorización del código para eliminar duplicados en la gestión del foco de los menús.
- **Completado:** Accesibilidad del inventario y pestañas de personaje.
  - **Completado:** Lectura de nombre, cantidad, durabilidad, atributos y tooltip de los objetos en los slots.
  - **Completado:** Accesibilidad de las pestañas de personaje (Equipamiento, Habilidades) y su contenido para mando y ratón.
- **Completado:** Implementada navegación por teclado (WASD/Flechas) y D-Pad en todos los menús de inventario.
- **Completado:** Sistema de interacción con inventario mediante teclas U y O.
  - **Completado:** U para seleccionar objetos, cambiar pestañas y abrir árboles de talento.
  - **Completado:** O para acciones secundarias (click derecho en slots).
- **Completado:** Accesibilidad completa de las habilidades (Skills).
  - **Completado:** Lectura detallada de información de habilidades con niveles y estadísticas.
  - **Completado:** Apertura de árboles de talento con U en las habilidades.
- **Completado:** Accesibilidad de árboles de talento (Skill Talent Trees).
  - **Completado:** Anuncios de apertura/cierre de árboles de talento.
  - **Completado:** Lectura completa de talentos incluyendo nombre, efectos y descripción.
  - **Completado:** Interacción con talentos mediante tecla U para invertir puntos.
- **Completado:** Sistema completo de cursor virtual para gameplay.
  - **Completado:** Cursor virtual con movimiento I/J/K/L y detección de mundo.
  - **Completado:** Inicialización automática al entrar al mundo (sin necesidad de activación manual).
  - **Completado:** Acciones U/O en el mundo (minar, atacar, colocar) - tecla E eliminada, el juego maneja interacciones automáticamente.
  - **Completado:** Integración completa con sistema de inventario y UI mediante U/O.
  - **Completado:** Soporte para teclas mantenidas emulando comportamiento de ratón.
  - **Completado:** Soporte completo de mando (gamepad) para cursor virtual.
    - **Completado:** Stick derecho para movimiento discreto del cursor (tile por tile).
    - **Completado:** Comportamiento idéntico entre teclado (I/J/K/L) y stick derecho del mando.
    - **Completado:** R2/L2 (triggers) integrados para acciones U/O.
    - **Completado:** Sistema unificado que comparte variables de estado entre teclado y mando.
    - **Nota técnica:** Botones individuales del mando no detectables por limitaciones de Core Keeper/Rewired.
- **Completado:** Sistema de Auto-Targeting inteligente.
  - **Completado:** Detección automática de enemigos cercanos con filtrado mejorado (excluye estatuas y decoraciones).
  - **Completado:** Rango adaptativo según el arma equipada (melee: 3 tiles, ranged: 10 tiles, magic: 8 tiles).
  - **Completado:** Integración con cursor virtual - la tecla U apunta automáticamente al enemigo más cercano.
  - **Completado:** Anuncios por TTS de enemigos que entran/salen del rango con dirección y distancia.
  - **Completado:** Sistema siempre activo (sin toggles).
- **Completado:** Sistema de Proximidad Sonora.
  - **Completado:** Sonidos de proximidad para objetos interactuables (cofres, puertas, altares, etc.).
  - **Completado:** Sonidos de proximidad para enemigos con detección de movimiento.
  - **Completado:** Sistema de pitch dinámico: grave = lejos (10 tiles), agudo = cerca (1 tile).
  - **Completado:** Audio 3D espacializado para indicar dirección.
  - **Completado:** Diferenciación sutil de tipos de enemigos mediante variaciones de pitch.
  - **Completado:** Actualización en tiempo real de posición de enemigos aunque el jugador esté parado.
- **Completado:** Accesibilidad del Hotbar.
  - **Completado:** Anuncios de items al seleccionar slots del hotbar con teclas 1-0.
  - **Completado:** Lectura del nombre del item o "Slot vacío" si no hay nada equipado.
- **Completado:** Correcciones finales de accesibilidad y estabilidad.
  - **Completado:** Sistema de localización unificado con LocalizationManager.
  - **Completado:** Detección correcta de presets de equipo con navegación por teclado.
  - **Completado:** Anuncios correctos de apertura/cierre de árboles de talento.
  - **Completado:** Nombres de skills localizados (eliminados errores "missing:").
  - **Completado:** Limpieza de código: eliminado debug logging y código duplicado.
- **Completado:** Accesibilidad de botones de inventario (2025).
  - **Completado:** Soporte para botones Sort, Quick Stack, Lock/Unlock Items y Quick Stack Nearby.
  - **Completado:** Detección automática de tipo de botón mediante `ButtonUIElement`.
  - **Completado:** Integración completa con sistema de clicks simulados existente.
  - **Completado:** Anuncios por TTS del nombre del botón al presionar U/R2.
- **Completado:** Sistema de Anuncio de Biomas (2025).
  - **Completado:** Detección automática de cambios de bioma/tileset.
  - **Completado:** Anuncios por TTS al entrar en un nuevo bioma.
  - **Completado:** Soporte para 60+ biomas del juego.
  - **Completado:** Localización en 14 idiomas.

---
### Últimas Correcciones Técnicas (2024)

#### Corrección de Presets de Equipo
- **Problema**: Los presets de equipo saltaban a la pestaña de skills en lugar de cambiar el equipamiento.
- **Causa**: La detección de presets se realizaba después de la detección de SkillUIElement.
- **Solución**: Reordenadas las prioridades de detección en `HandleUInput()` para verificar presets ANTES que skills.
- **Archivos modificados**: `InventoryUIInputPatch.cs`

#### Corrección de Anuncios de Árboles de Talento
- **Problema**: Solo anunciaba "Abriendo árbol de talentos", nunca el cierre.
- **Causa**: Verificación incorrecta de `isAnyInventoryShowing` impedía anuncios de cierre.
- **Solución**: Eliminada la verificación incorrecta en `HideTalentTree_Postfix()`.
- **Archivos modificados**: `SkillTalentTreePatch.cs`

#### Corrección de Nombres de Skills
- **Problema**: Nombres aparecían como "missing: skill_Summoning".
- **Causa**: Uso incorrecto de `UIManager.GetLocalizedText()` y mapeo incorrecto de enum SkillID.
- **Solución**:
  - Migrado a `LocalizationManager.GetText()`
  - Corregidos valores de SkillID (Range vs Ranged)
  - Agregadas todas las skills al sistema de localización
- **Archivos modificados**: `SkillTalentTreePatch.cs`, `LocalizationManager.cs`

#### Limpieza de Código
- **Eliminado**: Debug logging excesivo de producción
- **Eliminado**: Claves de localización sin usar (`opening_talent_tree_*`)
- **Mejorado**: Manejo de errores y logging de exceptions

#### Corrección de Línea de Visión para Sistemas de Proximidad (2025)
- **Problema**: Los sonidos de proximidad (interactuables y enemigos) se reproducían a través de paredes sólidas.
- **Solución**: Implementado algoritmo de Bresenham para verificar línea de visión entre jugador y objeto/enemigo.
- **Funcionalidad**:
  - Verifica cada tile entre dos puntos para detectar paredes bloqueantes
  - Permite ver/escuchar a través de materiales transparentes (cristales - tileset 34, vallas)
  - Bloquea sonidos cuando hay paredes sólidas en el camino
  - Distancias menores a 2 tiles siempre tienen línea de visión directa
- **Archivos modificados**: `ProximityAudioPatch.cs`, `EnemyProximityAudioPatch.cs`

#### Corrección de Detección de Minions del Jugador (2025)
- **Problema**: Los minions invocados por el jugador se detectaban como enemigos durante combate en sistemas de auto-targeting y proximidad de audio.
- **Solución**: Implementado sistema triple de verificación con redundancia completa:
  1. **Verificación de componentes ECS**: `MinionCD`, `PetCD`, `MinionOwnerCD`
  2. **Filtro por patrones de nombre**: minion, summon, companion, familiar, pet, ally
  3. **Orden de verificación optimizado**: Las verificaciones de minions se ejecutan ANTES de marcar como enemigo
- **Resultado**: Los minions NUNCA se detectan como enemigos, independientemente del estado de combate
- **Archivos modificados**: `AutoTargetingPatch.cs` (que también afecta a `EnemyProximityAudioPatch.cs`)

#### Corrección de Tile-Ahead Announcer con Gamepad (2025)
- **Problema**: El sistema de anuncio de tiles adelante solo funcionaba con teclado (WASD), no con gamepad.
- **Causa**: El sistema intentaba detectar el input del stick del mando, pero el juego consumía el input antes de que el parche pudiera leerlo.
- **Solución**: Cambio completo de enfoque para detectar **movimiento real** en lugar de input:
  - Compara la posición del jugador entre frames para calcular la dirección de movimiento
  - Funciona automáticamente con cualquier método de control (teclado, gamepad, o futuro)
  - Inicialización correcta de posición previa para evitar falsos positivos
  - Actualización de posición en un solo punto del código para evitar inconsistencias
- **Ventajas**: Más robusto, más simple, funciona con cualquier input sin necesidad de detectar cada tipo
- **Archivos modificados**: `TileAheadAnnouncerPatch.cs`

#### Accesibilidad del Hotbar (2024)
- **Problema**: El usuario solicitó anuncios de items al seleccionar slots del hotbar con teclas 1-0.
- **Implementación**: Creado `HotbarSelectionAccessibilityPatch.cs` que intercepta `PlayerController.EquipSlot`.
- **Funcionalidad**:
  - Detecta automáticamente cuando se selecciona un slot del hotbar (teclas 1-0)
  - Accede al inventario del jugador (`player.playerInventoryHandler.GetContainedObjectData()`)
  - Anuncia el nombre localizado del item o "Slot vacío" si está vacío
  - Fallback a número de slot si hay errores de acceso
- **Localización**: Agregadas claves `hotbar_slot_selected` y `empty_hotbar_slot` en español e inglés
- **Archivos modificados**: `HotbarSelectionAccessibilityPatch.cs`, `es.txt`, `en.txt`

#### Corrección Crítica del Cursor Virtual - Colocación con Teclado (2025)
- **Problema**: La acción secundaria (tecla O) no colocaba objetos en la posición del cursor virtual, solo funcionaba con mando (L2).
- **Causa Raíz**: Core Keeper tiene dos caminos diferentes para calcular la posición de acción:
  - **PATH 1 (Teclado/Mouse)**: Usa la posición física del mouse directamente
  - **PATH 2 (Mando)**: Calcula posición basándose en el stick derecho (que el mod intercepta)
  - Cuando presionabas O (teclado), el juego detectaba "modo mouse" y usaba PATH 1, ignorando el cursor virtual
- **Solución**: Implementado parche crítico en `SendClientInputSystemPatch.cs`:
  - Intercepta `CalculateMouseOrJoystickWorldPoint` - el método que calcula dónde colocar/atacar
  - Sobrescribe la posición calculada con la del cursor virtual cuando está activo
  - Sistema de prioridades: 1) Auto-targeting, 2) Cursor virtual, 3) Comportamiento original
- **Resultado**: Ahora TODAS las acciones funcionan correctamente con cursor virtual:
  - ✅ U/R2: Atacar, minar, destruir objetos, talar árboles
  - ✅ O/L2: Colocar objetos, usar herramientas (pala, azada), colocar bloques
  - ✅ Funciona idénticamente con teclado y mando
- **Archivos modificados**: `SendClientInputSystemPatch.cs`, `PlayerInputPatch.cs`
- **Métodos añadidos**: `HasActiveCursor()`, `GetCursorOffsetMagnitude()` para verificación de estado del cursor

#### Mejoras en Detección de Objetos del Cursor Virtual (2025)
- **Problema**: El cursor virtual detectaba objetos de forma inconsistente, sin distinguir entre objetos naturales y colocados por jugador.
- **Investigación**: Análisis exhaustivo del código del juego (PlacementHandler, TileAccessor, ObjectType) para entender el sistema de tiles y objetos.
- **Hallazgos**:
  - Core Keeper NO mantiene flag persistente de "colocado por jugador"
  - Existe `TileCreatedFromEntityCD` pero es temporal (solo durante colocación)
  - Solución: Implementar heurísticas confiables basadas en patrones del juego
- **Implementación**:
  1. **Sistema de Heurísticas** (`PlayerPlacedHelper.cs`):
     - Detecta tiles exclusivos de jugador: fence, rail, bridge, rug, litFloor
     - Detecta tilesets pintados (rangos 15-22, 37-40, 41-52, 61-64) - 100% del jugador
     - Sistema extensible para futuras mejoras
  2. **Priorización Mejorada** (`SimpleWorldReader.cs`):
     - Nueva prioridad: 1) Objetos colocables, 2) Interactuables, 3) Enemigos, 4) Otros
     - Busca el objeto MÁS CERCANO de cada categoría
     - Heurísticas basadas en `ObjectCategory`: WorkStation, Furniture, Decoration
  3. **Indicadores Contextuales**:
     - Añade sufijo "(colocado)" / "(placed)" cuando detecta objeto/tile del jugador
     - Sistema localizado en 19 idiomas
- **Ventajas**:
  - ✅ Detección más consistente y precisa
  - ✅ Información contextual útil para el usuario
  - ✅ Sin cambios en comportamiento actual - solo mejoras
  - ✅ Usa APIs nativas del juego para mejor estabilidad
- **Archivos creados**: `PlayerPlacedHelper.cs`
- **Archivos modificados**: `SimpleWorldReader.cs`, `es.txt`, `en.txt` (clave `player_placed`)

### Controles del Cursor Virtual y Sistemas de Accesibilidad

#### Cursor Virtual (se activa automáticamente al entrar al mundo):

**Controles con Teclado:**
- **I/J/K/L:** Mover cursor discreto (arriba/izquierda/abajo/derecha) - 1 tile por pulsación
- **R:** Resetear cursor a la posición del jugador
- **U:** Acción primaria (atacar/minar) - con auto-target activo apunta automáticamente a enemigos
- **O:** Acción secundaria (usar/colocar objetos)
- **P:** Información de posición del cursor
- **M:** Posición detallada del jugador
- **T:** Test de coordenadas

**Controles con Mando (Gamepad):**
- **Stick Derecho:** Mover cursor discreto (tile por tile) - comportamiento idéntico a I/J/K/L
  - Movimiento en un solo eje a la vez (prioriza el eje con mayor magnitud)
  - Debounce de 200ms para control preciso
  - Zona muerta (deadzone) de 0.5 para evitar drift
- **R2 (Trigger Derecho):** Acción primaria - equivalente a tecla U
- **L2 (Trigger Izquierdo):** Acción secundaria - equivalente a tecla O
- **Tecla R (teclado):** Reset del cursor (botones del mando no disponibles por limitaciones técnicas)

**Implementación Técnica del Soporte de Mando:**
- **Archivo principal:** `PlayerInputPatch.cs`
- **Detección de stick:** Usa acciones de Rewired (`RightJoyStickX`, `RightJoyStickY` - constantes 59 y 60)
- **Sistema unificado:** Teclado y mando comparten las mismas variables de estado (`_cursorOffsetX`, `_cursorOffsetZ`)
- **Comportamiento idéntico:** Ambos métodos de input producen exactamente el mismo resultado
- **Limitación conocida:** Los botones individuales del mando (L3, R3, Select, Start) no se pueden detectar porque Core Keeper/Rewired consume el input antes de que los parches puedan interceptarlo. Esto es una limitación del motor del juego, no del mod.

#### Sistemas Automáticos (siempre activos):
- **Auto-Targeting:** Apunta automáticamente al enemigo más cercano al usar U/R2
- **Sonidos de Proximidad:** Emite tonos que varían en pitch según la distancia (grave=lejos, agudo=cerca)
  - Para objetos interactuables: se activa al caminar
  - Para enemigos: se actualiza constantemente con su posición
  - **Audio 2D:** Paneo estéreo manual para posicionamiento correcto (izquierda/derecha)

### Sistema de Notificaciones ✅ **COMPLETADO**

Se ha implementado un sistema completo de notificaciones con historial navegable que anuncia eventos importantes del juego.

#### Funcionalidades Principales:

1. **Detección Automática de Eventos:**
   - **Items Recogidos**: Monitorea el inventario del jugador cada 500ms y detecta cuando se recogen items
   - **Nivel Total**: Rastrea la suma de todos los niveles de skills (Core Keeper usa skills individuales, no nivel de personaje)
   - **Skills Mejoradas**: Detecta cuando cualquier skill individual sube de nivel
   - **Inicialización Inteligente**: No anuncia el inventario inicial al cargar el juego, solo items realmente recogidos

2. **Buffer Inteligente:**
   - Almacena hasta **100 notificaciones** en memoria
   - Limpieza automática: elimina las más antiguas cuando se supera el límite
   - Sistema eficiente que no impacta el rendimiento

3. **Navegación del Historial:**
   - **Punto (.)**: Siguiente notificación (más reciente)
   - **Coma (,)**: Anterior notificación (más antigua)
   - **Shift + Punto**: Saltar directamente a la última notificación
   - **Shift + Coma**: Saltar directamente a la primera notificación
   - Cada notificación se anuncia con su posición: "X de Y. [mensaje]"
   - Cooldown de 200ms entre navegaciones

4. **Control de Anuncios:**
   - Intervalo mínimo de 500ms entre notificaciones automáticas
   - Debounce de 300ms para evitar anuncios duplicados del mismo item
   - No interrumpe si el usuario está navegando el historial

#### Archivos del Sistema:

- **`NotificationSystem.cs`**: Sistema central con historial y gestión de notificaciones
- **`NotificationNavigationPatch.cs`**: Controles de navegación por teclado (Punto/Coma con/sin Shift)
- **`ItemPickupNotificationPatch.cs`**: Detección de items recogidos mediante monitoreo de inventario
- **`LevelUpNotificationPatch.cs`**: Detección de cambios en nivel total (suma de skills)
- **`SkillUpNotificationPatch.cs`**: Detección de mejoras en skills individuales

#### Tipos de Notificaciones:

```csharp
public enum NotificationType
{
    ItemPickup,     // Item recogido
    LevelUp,        // Subida de nivel total
    SkillUp,        // Mejora de skill
    Achievement,    // Logro desbloqueado (futuro)
    Info,           // Información general
    Warning,        // Advertencia
    Error           // Error
}
```

#### Claves de Localización (19 idiomas soportados):

```
item_picked_single=Picked up: {0}
item_picked_multiple=Picked up: {0} x{1}
level_up=Leveled up to {0}!
skill_up={0} increased to level {1}
```

### Sistema de Buffer de Tutoriales ✅ **COMPLETADO**

Se ha implementado un sistema separado y dedicado para mensajes de tutorial con navegación independiente del buffer de notificaciones.

#### Detección Universal Multiidioma:

**Método basado en Call Stack (Pila de Llamadas):**
- El sistema analiza la pila de llamadas cuando se muestra un mensaje (Emote o PopUpText)
- Detecta si el origen es una clase del juego que contiene "Tutorial" en su nombre
- **Funciona en todos los 19 idiomas soportados** sin necesidad de listas de palabras clave
- No requiere hardcoding ni mantenimiento por idioma

#### Navegación del Buffer de Tutoriales:
- **' (apóstrofe)**: Siguiente tutorial (más reciente)
- **Ñ (tecla Semicolon)**: Tutorial anterior (más antiguo)
- **Shift + '**: Saltar al último tutorial
- **Shift + Ñ**: Saltar al primer tutorial
- Buffer de hasta **50 tutoriales** en memoria

#### Clasificación Automática de Mensajes:

1. **Buffer de Tutoriales** (teclas '/Ñ):
   - Detectados por call stack (vienen de clases `*Tutorial`)
   - Ejemplos: "Esa madera me podría ser útil", "Ojalá tuviera algo para iluminarme"
   - Se anuncian automáticamente cuando aparecen

2. **Buffer de Notificaciones** (teclas ,/punto):
   - Items recogidos, subidas de nivel, mejoras de skills
   - Comentarios generales del personaje (no tutoriales)

3. **Anuncios Directos** (sin buffer):
   - Feedback crítico inmediato: "No hay energía", "Skill insuficiente"

#### Archivos del Sistema:

- **`TutorialBufferSystem.cs`**: Buffer dedicado con capacidad de 50 tutoriales
- **`TutorialNavigationPatch.cs`**: Controles de navegación ('/Ñ con/sin Shift)
- **`EmoteTextAccessibilityPatch.cs`**: Detección de tutoriales en emotes usando call stack
- **`PopUpTextPatch.cs`**: Detección de tutoriales en popups usando call stack

#### Prevención de Bloqueo de Input:

Todos los parches de mensajes (Chat, PopUp, Emotes) incluyen verificación de cutscenes activas:
- Detecta `CutsceneHandler.isPlaying` e `IntroHandler.showing`
- Previene anuncios durante cutscenes de intro y spawn
- Soluciona el problema de input bloqueado tras la cutscene inicial

### Sistema de Anunciador de Tiles al Frente ✅ **COMPLETADO**

Anuncia automáticamente el tile que está en frente del jugador según la dirección de movimiento.

#### Funcionamiento:
- **Detección de Dirección**: Monitorea WASD/flechas para determinar hacia dónde mira el jugador
- **Cálculo Inteligente**: Determina el tile 1 unidad adelante en la dirección de movimiento
- **Cache Eficiente**: Solo anuncia cuando el tile cambia, no repeticiones innecesarias
- **Cooldown**: 300ms entre anuncios para evitar spam
- **Integración con Menús**: Se desactiva automáticamente cuando hay menús abiertos

#### Archivo:
- **`TileAheadAnnouncerPatch.cs`**: Parche en `PlayerController.ManagedUpdate`

### Sistema de Anunciador de Biomas ✅ **COMPLETADO**

Anuncia automáticamente cuando el jugador entra en un nuevo bioma/zona del mapa.

#### Funcionamiento:
- **Detección de Tileset**: Lee el tileset del tile bajo el jugador en cada frame
- **Comparación Inteligente**: Solo anuncia cuando el bioma cambia, no en cada movimiento
- **Inicialización Silenciosa**: Al cargar el mundo, detecta el bioma inicial sin anunciarlo
- **Cooldown**: 500ms entre anuncios para evitar spam
- **Umbral de Movimiento**: Solo verifica cambios cuando el jugador se mueve 0.3+ tiles
- **Filtrado de Genéricos**: No anuncia biomas desconocidos ("Material X")
- **Multijugador Safe**: Solo procesa el jugador local

#### Biomas Soportados (60+):
- **Básicos**: Tierra, Piedra, Obsidiana, Lava, Naturaleza, Moho, Mar, Arena, Arcilla
- **Especiales**: Desierto, Nieve, Cristal, Piedra Oscura, Alienígena, Oasis, Pradera
- **Construcciones**: Madera, Piedra, y todas las variantes de colores (amarillo, verde, rojo, púrpura, azul, etc.)
- **Templos**: Templo del Desierto, Templo Antiguo del Desierto
- **Eventos**: San Valentín, Pascua

#### Localización:
- Clave: `biome_entered` - "Entraste a {0}" / "Entered {0}"
- Soporta los 14 idiomas del mod

#### Archivo:
- **`BiomeAnnouncerPatch.cs`**: Parche en `PlayerController.ManagedUpdate`

#### Métodos Públicos:
- `ClearCache()`: Limpia el cache (útil al cambiar de mundo)
- `GetCurrentBiomeName()`: Devuelve el nombre del bioma actual
- `GetCurrentTilesetIndex()`: Devuelve el índice del tileset actual

### Sistema de Categorización de Objetos ✅ **COMPLETADO**

Sistema inteligente para identificar y categorizar objetos del mundo sin hardcoding.

#### Categorías Soportadas:
- Core (núcleo del juego)
- Chest (cofres)
- WorkStation (estaciones de trabajo)
- Enemy (enemigos)
- Pickup (items recogibles)
- Plant (plantas)
- Resource (recursos minables)
- Decoration (decoraciones)
- Furniture (muebles)
- Animal (animales)
- Critter (bichos)
- Structure (estructuras)
- Door (puertas)
- Statue (estatuas)

#### Archivo:
- **`ObjectCategoryHelper.cs`**: 70+ patrones de objetos organizados por categoría

### Sistema de Localización

El mod soporta **19 idiomas**:
- Inglés (en), Español (es), Francés (fr), Alemán (de)
- Italiano (it), Portugués Brasileño (pt-br), Holandés (nl)
- Ruso (ru), Polaco (pl), Turco (tr), Ucraniano (uk)
- Checo (cz), Sueco (sv)
- Japonés (ja), Chino Simplificado (zh-cn), Chino Tradicional (zh-tw)
- Coreano (ko), Árabe (ar), Tailandés (th)

**Sistema de Fallback**: Si falta una traducción, se usa inglés automáticamente.

### Compatibilidad Multijugador (2025) ✅ **COMPLETADO**

#### Problema Identificado
En multijugador, todos los sistemas de accesibilidad (cursor virtual, tile ahead announcer, auto-targeting, proximidad de audio) procesaban TODOS los jugadores, no solo el local. Esto causaba:
- El lector de pantalla anunciaba acciones de otros jugadores
- Los sistemas de proximidad detectaban enemigos cerca de otros jugadores
- El cursor virtual se confundía entre jugadores

#### Solución Implementada
Creado un sistema centralizado de identificación del jugador local:

**Archivo:** `LocalPlayerHelper.cs`
- **Método principal:** `GetLocalPlayer()` - Devuelve el PlayerController del jugador local
- **Verificación:** `IsLocalPlayer(PlayerController)` - Compara si un jugador es el local
- **Cache inteligente:** Cachea el jugador local por frame para optimizar rendimiento
- **Fallback robusto:** Si falla la detección directa, busca entre todos los PlayerControllers activos

#### Sistemas Actualizados para Multijugador
Todos estos sistemas ahora verifican `LocalPlayerHelper.IsLocalPlayer()` antes de procesar:

1. **`VirtualCursor.cs`**: Solo mueve el cursor del jugador local
2. **`TileAheadAnnouncerPatch.cs`**: Solo anuncia tiles del jugador local
3. **`AutoTargetingPatch.cs`**: Solo detecta enemigos para el jugador local
4. **`EnemyProximityAudioPatch.cs`**: Solo emite sonidos de proximidad del jugador local
5. **`ProximityAudioPatch.cs`**: Solo detecta interactuables cerca del jugador local

#### Resultado
✅ Cada cliente ejecuta su propia instancia del mod de forma independiente
✅ No hay interferencia entre jugadores
✅ Cada jugador tiene su propia experiencia de accesibilidad

---

### Sistema de Inventario Simplificado (2025) ✅ **COMPLETADO**

#### Filosofía de Diseño
El mod **NO añade atajos nuevos**. Solo emula clicks del mouse para que funcione con teclado/gamepad. El usuario utiliza todas las teclas nativas del juego.

#### Implementación Actual

**Teclas del Mod:**
- **U / R2 (mando)**: Click izquierdo puro - agarrar/colocar objetos
- **O / L2 (mando)**: Click derecho puro - acciones secundarias
- **WASD/Flechas/D-Pad**: Navegación entre slots

**Teclas Nativas del Juego (el usuario las usa directamente):**
- **Shift + Click**: Transferencia rápida entre inventarios (el juego maneja esto)
- **Teclas de Drop**: El usuario usa las teclas configuradas en su juego
- **Teclas de Quick Stack**: El usuario usa las teclas configuradas en su juego

#### Detección de Secciones de Inventario

El mod detecta automáticamente en qué sección del inventario estás y lo anuncia:

**Archivo:** `UIMouseInputPatch.cs`
- **Función:** `AnnounceInventorySectionIfChanged()`
- **Detección:** Usa reflexión para leer `slotsUIContainer.containerType`
- **Tipos detectados:**
  - `PlayerInventory` → "Inventario del jugador"
  - `ChestInventory` → "Inventario del cofre"
  - `CraftingInventory` → "Inventario de crafteo"
  - `PlayerEquipment` → "Equipamiento"
  - `PouchInventory` → "Bolsa"
- **Cache:** Solo anuncia cuando cambias de sección, no en cada navegación

#### Archivos del Sistema
- **`UIMouseInputPatch.cs`**: Detección de input y anuncios de sección
- **`InventoryUIInputPatch.cs`**: Simplificado a clicks puros, sin atajos custom
- **Localization:** Claves `section_player_inventory`, `section_chest_inventory`, etc.

#### Beneficios
✅ 100% compatible con controles nativos del juego
✅ Sin conflictos con atajos del usuario
✅ Más simple y mantenible
✅ Funciona con cualquier configuración de teclas del juego

#### Accesibilidad de Botones de Inventario (2025) ✅ **COMPLETADO**

Se ha implementado soporte completo para los botones de acción del inventario mediante el sistema de clicks simulados existente.

**Botones Soportados:**
- **Sort** (Ordenar): Ordena automáticamente los items del inventario
- **Quick Stack** (Apilar rápidamente): Apila items del inventario al cofre abierto
- **Quick Stack Nearby** (Apilar en cercanos): Apila items a cofres cercanos
- **Lock/Unlock Items** (Bloquear/Desbloquear objetos): Alterna el bloqueo de items

**Funcionamiento:**
- El usuario navega con **WASD/Flechas/D-Pad** hasta el botón deseado
- Presiona **U** o **R2 (mando)** para ejecutar la acción del botón
- El mod detecta automáticamente el tipo de botón (`ButtonUIElement`)
- Llama al método nativo `OnLeftClicked()` del botón
- Anuncia el nombre del botón por TTS

**Implementación:**
- **Archivo:** `InventoryUIInputPatch.cs`
- **Método:** `HandleButtonSelection()` - Maneja la selección de botones genéricos
- **Método:** `GetButtonName()` - Detecta automáticamente el tipo de botón por nombre o texto
- **Localización:** Claves `button_pressed`, `button_sort`, `button_quick_stack`, `button_lock`, `button_quick_stack_nearby`

**Ventajas:**
✅ Aprovecha el sistema de clicks simulados existente
✅ No requiere parches específicos por botón
✅ Detección automática de tipo de botón
✅ Funciona con cualquier `ButtonUIElement` del inventario

#### Corrección de Rotación y Input del Cursor Virtual (2025) ✅ **COMPLETADO**

Se ha solucionado el problema donde el personaje no giraba hacia el cursor virtual (impidiendo construir correctamente) y el input tenía retraso al estar parado.

**Problema:**
- El juego forzaba la rotación hacia el ratón físico si detectaba teclado (`PrefersKeyboardAndMouse` = true).
- El input del cursor virtual dependía de la actualización de la UI (`UIMouse`), que podía pausarse o cambiar en ciertos estados.

**Solución Implementada:**
1.  **Simulación de Mando (`PlayerInputPatch`):** Se intercepta `PrefersKeyboardAndMouse` para devolver `false` cuando el cursor virtual está activo. Esto engaña al juego para que use la lógica de "Stick Derecho", permitiendo la rotación correcta del personaje.
2.  **Parche de Vector de Mirada (`PlayerControllerPatch`):** Se intercepta `UpdateAim` para asegurar que el vector de ataque apunte exactamente al cursor virtual.
3.  **Actualización de Input Robusta:** Se movió la lógica de lectura de teclas (I/J/K/L) a `PlayerInput.UpdateState`. Esto garantiza que el cursor se mueva en cada frame del sistema de input, eliminando el lag al estar parado.
4.  **Seguridad de UI:** El sistema detecta automáticamente si se abre un inventario y restaura el "modo teclado" para evitar conflictos en la navegación de menús.

**Archivos Modificados:**
- `PlayerInputPatch.cs`: `PrefersKeyboardAndMouse_Postfix`, `UpdateState_Prefix`.
- `PlayerControllerPatch.cs`: Nuevo parche para `UpdateAim`.
- `VirtualCursorInputPatch.cs`: Eliminada lógica redundante.

#### Mejoras en Auto-Targeting (2025) ✅ **COMPLETADO**

Se han realizado mejoras significativas en la fiabilidad y precisión del sistema de auto-targeting.

**Problemas Resueltos:**
- **Bloqueo a través de paredes:** El sistema apuntaba a enemigos visibles en el minimapa pero bloqueados por muros.
- **Conflicto con Cursor Virtual:** El cursor virtual sobrescribía la dirección de ataque incluso cuando había un enemigo seleccionado.
- **Lentitud:** El escaneo era demasiado lento (cada 15 frames).

**Soluciones Implementadas:**
1.  **Verificación de Línea de Visión (LOS):** Implementado algoritmo de Bresenham para verificar si hay muros entre el jugador y el enemigo antes de seleccionarlo.
2.  **Prioridad de Apuntado:** Modificado `PlayerControllerPatch` para que la dirección del Auto-Targeting tenga prioridad sobre la dirección del cursor virtual.
3.  **Mayor Frecuencia:** Aumentada la frecuencia de escaneo a cada 5 frames (~80ms) para una respuesta más rápida.

**Archivos Modificados:**
- `AutoTargetingPatch.cs`: Añadido `HasLineOfSight`, aumentado scan rate.
- `PlayerControllerPatch.cs`: Añadida prioridad para `AutoTargetingPatch.GetCurrentTargetPosition()`.

---

### Refactorización de Código (2025-01) ✅ **COMPLETADO**

Se realizó una limpieza exhaustiva del código para mejorar mantenibilidad y eliminar redundancias.

#### 1. Arquitectura de Helpers Centralizados

Se han creado helpers centralizados para eliminar código duplicado y mejorar la mantenibilidad:

##### `LineOfSightHelper.cs` - Verificación de Línea de Visión
- **`HasLineOfSight(Vector3 from, Vector3 to)`** - Algoritmo de Bresenham centralizado
- **`IsVisionBlocking(TileType, int tileset)`** - Determina si un tile bloquea visión
- **`IsSeeThrough(TileType, int tileset)`** - Tiles transparentes (cristales, vallas)
- **`GetCardinalDirection(Vector3 from, Vector3 to)`** - Dirección cardinal localizada

**Usado por:** `ProximityAudioPatch`, `EnemyProximityAudioPatch`, `AutoTargetingPatch`

##### `EntityClassificationHelper.cs` - Clasificación de Entidades con ECS
- **`IsInteractable(EntityMonoBehaviour)`** - Detecta interactuables usando componentes ECS
- **`IsEnemy(EntityMonoBehaviour)`** - Detecta enemigos usando estados ECS (ChaseStateCD, MeleeAttackStateCD)
- **`IsPlayerMinion(EntityMonoBehaviour)`** - Detecta minions/pets del jugador (MinionCD, PetCD)
- **`GetWeaponCategory(ObjectID)`** - Clasifica armas usando ObjectType nativo del juego
- **`GetWeaponRange(ObjectID)`** - Rango basado en categoría de arma

**Categorías de Armas (usando ObjectType del juego):**
| Categoría | ObjectType | Rango |
|-----------|------------|-------|
| Melee | MeleeWeapon (500) | 3 tiles |
| Ranged | RangeWeapon (501), ThrowingWeapon (503) | 10 tiles |
| Magic | SummoningWeapon (502), CastingItem (602), BeamWeapon (610) | 8 tiles |
| Tool | Shovel, Hoe, MiningPick, etc. | 2 tiles |

**Usado por:** `AutoTargetingPatch`, `SimpleWorldReader`, `ProximityAudioPatch`

##### `GameplayStateHelper.cs` - Estado del Juego
- **`IsInGameplay()`** - Verificación completa (jugador, texto, pausa, controlador)
- **`IsInExcludedMenu()`** - Inverso de IsInGameplay
- **`IsInGameplayWithoutInventory()`** - Gameplay + sin inventario abierto

**Usado por:** `PlayerInputPatch`, `VirtualCursorInputPatch`, `SendClientInputSystemPatch`, `TileAheadAnnouncerPatch`

##### `LocalPlayerHelper.cs` - Identificación de Jugador Local (Multijugador)
- **`GetLocalPlayer()`** - Devuelve el PlayerController del jugador local
- **`IsLocalPlayer(PlayerController)`** - Verifica si es el jugador local
- **`TryGetLocalPlayerPosition(out Vector3)`** - Posición del jugador local

**Usado por:** Todos los sistemas de accesibilidad

#### 2. Archivos Refactorizados

| Archivo | Cambios |
|---------|---------|
| `ProximityAudioPatch.cs` | Usa `EntityClassificationHelper.IsInteractable()` y `LineOfSightHelper` |
| `AutoTargetingPatch.cs` | Usa `EntityClassificationHelper.IsEnemy()`, `GetWeaponRange()` y `LineOfSightHelper` |
| `EnemyProximityAudioPatch.cs` | Usa `LineOfSightHelper.HasLineOfSight()` |
| `SimpleWorldReader.cs` | Usa `EntityClassificationHelper` para mejor detección |
| `TileAheadAnnouncerPatch.cs` | Usa `GameplayStateHelper` y `LocalPlayerHelper` |

#### 3. Código Eliminado

| Archivo | Código Eliminado |
|---------|------------------|
| `ProximityAudioPatch.cs` | ~70 líneas (LOS duplicado, patrones hardcodeados) |
| `AutoTargetingPatch.cs` | ~150 líneas (patrones de enemigos, clasificación de armas, LOS) |
| `EnemyProximityAudioPatch.cs` | ~100 líneas (LOS duplicado) |
| `TileAheadAnnouncerPatch.cs` | ~40 líneas (IsAnyMenuOpen, TryGetPlayerPosition duplicados) |

#### 4. Beneficios de la Refactorización

- **~360 líneas de código duplicado eliminadas**
- **2 helpers nuevos** (`LineOfSightHelper.cs`, `EntityClassificationHelper.cs`)
- **Detección ECS nativa** en lugar de patrones de strings hardcodeados
- **Clasificación de armas** usando `ObjectType` enum del juego
- **Código más mantenible** - cambios centralizados en helpers
- **0 cambios funcionales** - Todo sigue funcionando igual

---

### Próximos Pasos

1.  **Accesibilizar la mesa de crafteo y otros menús de crafting.**
2.  **Mejorar personalización de personaje:** Investigar cómo obtener los nombres o descripciones de las opciones de apariencia.
3.  **Sistema de lectura de chat y mensajes del juego.**
4.  **Verificar y pulir:** Probar exhaustivamente todos los sistemas para asegurar estabilidad.

---
### Workaround Temporal

*   **Menú "Unirse a la Partida":** Actualmente, para que la navegación por teclado funcione correctamente en el menú "Unirse a la Partida", es necesario mantener el cursor del ratón en el centro de la pantalla. Si el ratón se encuentra en los bordes superior o inferior de la ventana del juego, la navegación con las flechas del teclado puede bloquearse. Este es un comportamiento temporal mientras se investiga una solución más permanente.

---
### Plan de Desarrollo: Cursor Virtual

#### Módulo 1 – Cursor Virtual Básico ✅ **COMPLETADO**
**Objetivo:** Tener un cursor que se mueve alrededor del jugador con I, J, K, L y que puede resetearse con R.
- ✅ Crear una entidad interna que represente el cursor virtual.
- ✅ El cursor se mueve en pasos de tile.
- ✅ El cursor siempre está limitado al Tilemap jugable.
- ✅ R coloca el cursor en la posición exacta del jugador.
- ✅ Probar que al mover el cursor se detecta correctamente qué tile hay debajo (para narrar con TTS).

**Implementación Actual:**
- **Controles:** I/J/K/L para movimiento (estilo vim), R para resetear al jugador
- **Detección:** Sistema `SimpleWorldReader` con prioridades: Interactuables > Enemigos > Paredes/Minerales > Tiles
- **Funciones adicionales:** M (posición detallada del jugador), P (debug de posición del cursor), T (test de coordenadas)
- **Localización:** Nombres en español con fallback a inglés
- **Archivos:** `VirtualCursor.cs`, `VirtualCursorInputPatch.cs`, `SimpleWorldReader.cs`, helpers varios

#### Módulo 2 – Integración de acciones primarias y secundarias (Gameplay) ✅ **COMPLETADO**
**Objetivo:** Que U y O funcionen como clicks en el mundo.
- ✅ U → simular click izquierdo (INTERACT):
  - Minar bloques si es tile destructible.
  - Atacar si es enemigo.
  - Si no hay nada → dejar que el juego haga la animación estándar de golpear en el aire.
- ✅ O → simular click derecho (SECOND_INTERACT):
  - Usar objetos equipados en la posición del cursor.
  - Colocar bloques, usar herramientas, etc.
- ✅ E → simular interacción (INTERACT_WITH_OBJECT):
  - Abrir cofres, puertas, mesas de trabajo, etc.
  - Interactuar con NPCs y objetos del mundo.
- ✅ **Funcionalidad de teclas mantenidas:** Emula el comportamiento original del juego cuando mantienes presionados los botones del ratón.

**Implementación Técnica:**
- **Archivos:** `PlayerInputPatch.cs`, `SendClientInputSystemPatch.cs`
- **Sistema de parches duales:** Intercepta tanto la detección de input (`PlayerInput`) como la posición de ejecución (`SendClientInputSystem`)
- **Gestión de estados:** Sistema robusto que detecta presión inicial (`WasButtonPressedDownThisFrame`) y estado continuo (`IsButtonCurrentlyDown`)
- **Posicionamiento:** Las acciones se ejecutan en la posición del cursor virtual, no del jugador

#### Módulo 3 – Integración con Inventario y UI de Crafting ✅ **COMPLETADO**
**Objetivo:** Que U y O sirvan dentro del inventario/estaciones de trabajo.
- ✅ U → selección primaria:
  - Seleccionar ítems en slots de inventario.
  - Cambiar entre pestañas (Equipamiento, Habilidades, Almas).
  - Abrir árboles de talento desde habilidades.
  - Invertir puntos de talento en árboles.
- ✅ O → acciones secundarias:
  - Click derecho en slots de inventario.
  - Acciones contextuales en elementos UI.
- ✅ El sistema detecta automáticamente el contexto UI activo y adapta el comportamiento.
- ✅ Integración completa con sistema de habilidades y talentos.

**Implementación Técnica:**
- **Archivos:** `InventoryUIInputPatch.cs`, `SkillAccessibilityPatch.cs`, `SkillTalentTreePatch.cs`
- **Detección contextual:** Automática según el tipo de elemento UI seleccionado
- **Soporte para:** Slots de inventario, pestañas, habilidades, talentos
- **Feedback:** Anuncios TTS confirmando cada acción realizada

#### Módulo 4 – Exclusiones (menús que no deben usar este sistema)
**Objetivo:** Asegurar que el sistema no interfiera en menús donde no corresponde.
- Detectar si el jugador está en:
  - Menú principal.
  - Creación de mundos.
  - Selección de personaje/slot.
  - Menú de pausa.
- En estos casos, deshabilitar por completo el cursor virtual.
- Validar que los menús originales se controlan sin interferencia.

#### Módulo 5 – Auto-Lock de Enemigos (opcional)
**Objetivo:** Facilitar el combate sin depender del cursor.
- Detectar enemigos dentro de un radio configurable alrededor del jugador.
- Seleccionar automáticamente el enemigo más cercano como objetivo.
- Mientras el lock está activo:
  - U siempre ataca al enemigo fijado.
  - Si muere o sale del rango → lock se cancela.
- Narrar por TTS cuando un enemigo es fijado o desbloqueado.

#### Módulo 6 – Feedback Sonoro y TTS
**Objetivo:** Dar accesibilidad plena al jugador ciego.
- Narrar tiles y objetos cuando el cursor se mueve.
- Narrar enemigos con distancia relativa (“Slime a 3 tiles a la izquierda”).
- Narrar interacciones exitosas (“Cofre abierto”).
- Narrar estados especiales (lock activado, lock cancelado, cursor reseteado).

#### Módulo 7 – Optimización y Refinamiento
**Objetivo:** Pulir la experiencia.
- Ajustar la velocidad de movimiento del cursor (1 tile por pulsación o movimiento continuo si la tecla se mantiene).
- Configurar prioridades en UI (ejemplo: siempre seleccionar pestañas antes que slots si el cursor está en el borde).
- Revisar posibles conflictos con los inputs originales del juego.
- Dejar opciones de personalización en un archivo de configuración o menú accesible.