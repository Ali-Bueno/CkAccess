* esto es un proyecto llamado Core Keeper Accessibility Mod, un mod con objetivo para poder accesibilizar el juego core keeper para personas ciegas: El código debe ser modular. No crear archivos de parche grandes. Al añadir una nueva funcionalidad, está prohibido romper algo que ya funcione.
* antes de ejecutar una tarea, siempre mirar el código actual que ya tenemos, en la carpeta ckAcces y sus subcarpetas,  para no implementar algo nuevo y que acabe siendo peor que la implementación que ya teníamos antes.
* al ejecutar una tarea que el usuario pida, siempre estudiar la mejor forma de implementar, que sea menos invasiva para el juego y estudiar el código que están en las carpetas ck code 1 y ck code 2. En caso de no poder accederse a campos y métodos privados, usar reflexión. también no olvidar/tocar los alias en el csproj, que ya están puestos aí.
* Siempre procurar hacer el código por parches pequeños, analizando bien la estructura de las carpetas y de los parches anteriores para tomarse como ejemplo.
* Los parches de menús deben ser lo menos invasivo posible para evitar cambios en el comportamiento del juego.
* Al buscar en el código del juego, ignorar siempre los patrones de .gitignore para asegurar un análisis completo. **Recordatorio: Forzar siempre la opción para ignorar .gitignore en las herramientas de búsqueda de archivos, ya que a veces se olvida.**
* Al compilar, ignorar las advertencias de BepInEx si la compilación se completa correctamente.
*   **Convención de Referencias:** Todas las clases de la interfaz de usuario (UI) del juego se encuentran en el ensamblado `Pug.Other.dll`. Para acceder a ellas, se debe utilizar el alias `PugOther` (ej. `PugOther.InventorySlotUI`). No existe un `Pug.UI.dll` separado.

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

#### 4. Navegación por Teclado y D-Pad en Inventarios (`UIMouseInputPatch.cs`)

Se ha implementado un sistema para permitir la navegación por los slots del inventario, crafteo y menús similares utilizando el teclado (WASD/Flechas) y el D-Pad del mando.

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
        *   Si se detecta una pulsación (de teclado o D-Pad), se utiliza el método nativo del juego `currentSelectedUIElement.GetAdjacentUIElement()` para encontrar el siguiente slot en la dirección deseada.
        *   Se actualiza la posición del puntero del ratón (`__instance.pointer.position`) para que coincida con la del nuevo slot.
        *   Se invoca el método privado `TrySelectNewElement()` mediante reflexión para seleccionar oficialmente el nuevo slot y disparar todos los eventos asociados (como la verbalización del contenido del slot).
    5.  **Control de Flujo:** Si el parche maneja una entrada de navegación, devuelve `false`, lo que **impide que el método original `UpdateMouseUIInput` se ejecute**. Esto evita que el juego procese el input dos veces o que el ratón interfiera con la selección. Si no se detecta ninguna entrada de navegación, el parche devuelve `true`, permitiendo que el juego funcione con normalidad.

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

### Próximos Pasos

1.  **Accesibilizar la mesa de crafteo.**
2.  **Mejorar personalización de personaje:** Investigar cómo obtener los nombres o descripciones de las opciones de apariencia (ej. "Pelo largo", "Rojo") en lugar de solo "Estilo X de Y".
3.  **Verificar y pulir:** Probar exhaustivamente todos los menús para asegurar que la lectura sea fluida y no haya regresiones.

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

#### Módulo 3 – Integración con Inventario y UI de Crafting 🔄 **PRÓXIMO PASO**
**Objetivo:** Que U y O sirvan dentro del inventario/estaciones de trabajo.
- Mapear el cursor virtual a la cuadrícula de slots de inventario.
- U → selección primaria:
  - Seleccionar ítems.
  - Confirmar acciones (equipar, mover, navegar entre pestañas).
- O → acciones secundarias:
  - Dividir stacks.
  - Acciones rápidas contextuales.
- Validar que el cursor cambia su comportamiento al detectar UI activa.

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