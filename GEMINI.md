* esto es un proyecto llamado Core Keeper Accessibility Mod, un mod con objetivo para poder accesibilizar el juego core keeper para personas ciegas: El código debe ser modular. No crear archivos de parche grandes. Al añadir una nueva funcionalidad, está prohibido romper algo que ya funcione.
* antes de ejecutar una tarea, siempre mirar el código actual que ya tenemos, en la carpeta ckAcces y sus subcarpetas,  para no implementar algo nuevo y que acabe siendo peor que la implementación que ya teníamos antes.
* al ejecutar una tarea que el usuario pida, siempre estudiar la mejor forma de implementar, que sea menos invasiva para el juego y estudiar el código que están en las carpetas ck code 1 y ck code 2. En caso de no poder accederse a campos y métodos privados, usar reflexión. también no olvidar/tocar los alias en el csproj, que ya están puestos aí.
* Siempre procurar hacer el código por parches pequeños, analizando bien la estructura de las carpetas y de los parches anteriores para tomarse como ejemplo.
* Los parches de menús deben ser lo menos invasivo posible para evitar cambios en el comportamiento del juego.

---
### Principios de Accesibilidad

*   **Librería de Salida:** Se utiliza **Tolk** como única librería para la comunicación con lectores de pantalla.
*   **Implementación:** No es necesario implementar nuevos sistemas de accesibilidad. La verbalización de los elementos de la interfaz se gestiona a través de la clase `UIManager`, que centraliza las llamadas a Tolk. Para añadir accesibilidad a un nuevo menú, se debe seguir el patrón de los parches existentes.

---
### Arquitectura de Parches de UI

Se ha implementado una arquitectura de parches centralizada y robusta para gestionar la accesibilidad de los menús del juego. Este enfoque se divide en dos responsabilidades principales: la **lectura de opciones** y la **gestión del foco inicial**.

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

#### 2. Gestión del Foco Inicial (`MenuActivatePatches.cs`)

Este archivo centraliza todos los parches responsables de asegurar que la navegación por teclado funcione correctamente al entrar o regresar a un menú.

*   **Objetivo:** Varios menús (`WorldSettingsMenu`, `CharacterCustomizationMenu`, `SelectWorldMenu`, `RadicalJoinGameMenu`, etc.).
*   **Método Parcheado:** `Activate` en cada menú objetivo.
*   **Funcionamiento:** Utiliza corrutinas para ejecutar la lógica de enfoque después de que el menú se haya inicializado completamente. Se emplean diferentes estrategias según las necesidades de cada menú:
    *   **`ForceSelectionCoroutine`:** La corrutina estándar. Deselecciona cualquier elemento que el juego haya podido enfocar automáticamente y luego selecciona la primera opción del menú. Se usa en la mayoría de los menús para garantizar un punto de partida limpio.
    *   **`UndoAndForceSelectionCoroutine`:** Una corrutina más agresiva, diseñada para menús con campos de texto como `CharacterCustomizationMenu` y `RadicalJoinGameMenu`. Desactiva a la fuerza el "modo de escritura" del juego (`Manager.input.SetActiveInputField(null)`) antes de seleccionar una opción segura (la primera que no sea un campo de texto). Esto es crucial para evitar el bloqueo del teclado.

### Plan de Desarrollo

- **Completado:** Refactorización y centralización de todos los parches de menús.
- **Completado:** Accesibilidad del menú principal, menú de ajustes, slots de mundo, selección de tipo de personaje y menú de personalización de personaje.
- **Completado:** Accesibilidad del menú de unirse a partida, incluyendo la navegación por teclado y la lectura de opciones del dropdown.

### Próximos Pasos

1.  **Accesibilizar el inventario del jugador:** Analizar la estructura del inventario y aplicar parches para leer la información de los objetos (nombre, cantidad, descripción).
2.  **Mejorar personalización de personaje:** Investigar cómo obtener los nombres o descripciones de las opciones de apariencia (ej. "Pelo largo", "Rojo") en lugar de solo "Estilo X de Y".
3.  **Accesibilizar los slots de personajes:** Aplicar el mismo enfoque centralizado para leer la información de cada slot de personaje.
4.  **Verificar y pulir:** Probar exhaustivamente todos los menús para asegurar que la lectura sea fluida y no haya regresiones.