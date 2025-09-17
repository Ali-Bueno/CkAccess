* esto es un proyecto llamado Core Keeper Accessibility Mod, un mod con objetivo para poder accesibilizar el juego core keeper para personas ciegas: El código debe ser modular. No crear archivos de parche grandes. Al añadir una nueva funcionalidad, está prohibido romper algo que ya funcione.
* antes de ejecutar una tarea, siempre mirar el código actual que ya tenemos, en la carpeta ckAcces y sus subcarpetas,  para no implementar algo nuevo y que acabe siendo peor que la implementación que ya teníamos antes.
* al ejecutar una tarea que el usuario pida, siempre estudiar la mejor forma de implementar, que sea menos invasiva para el juego y estudiar el código que están en las carpetas ck code 1 y ck code 2. En caso de no poder accederse a campos y métodos privados, usar reflexión. también no olvidar/tocar los alias en el csproj, que ya están puestos aí.
* Siempre procurar hacer el código por parches pequeños, analizando bien la estructura de las carpetas y de los parches anteriores para tomarse como ejemplo.
* Los parches de menús deben ser lo menos invasivo posible para evitar cambios en el comportamiento del juego.
* Al buscar en el código del juego, ignorar siempre los patrones de .gitignore para asegurar un análisis completo.
* Al compilar, ignorar las advertencias de BepInEx si la compilación se completa correctamente.

---
### Información del Repositorio
*   **URL:** git@github.com:Ali-Bueno/CkAccess.git
*   **Rama Principal:** main
---
### Principios de Accesibilidad

*   **Librería de Salida:** Se utiliza **Tolk** como única librería para la comunicación con lectores de pantalla.
*   **Implementación:** No es necesario implementar nuevos sistemas de accesibilidad. La verbalización de los elementos de la interfaz se gestiona a través de la clase `UIManager`, que centraliza las llamadas a Tolk. Para añadir accesibilidad a un nuevo menú, se debe seguir el patrón de los parches existentes.

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

### Plan de Desarrollo

- **Completado:** Refactorización y centralización de todos los parches de menús.
- **Completado:** Accesibilidad del menú principal, menú de ajustes, slots de mundo, selección de tipo de personaje y menú de personalización de personaje.
- **Completado:** Accesibilidad del menú de unirse a partida, incluyendo la navegación por teclado y la lectura de opciones del dropdown.
- **Completado:** Accesibilizar los slots de personajes.
- **Completado:** Refactorización del código para eliminar duplicados en la gestión del foco de los menús.

### Próximos Pasos

1.  **Accesibilizar el inventario del jugador:** Analizar la estructura del inventario y aplicar parches para leer la información de los objetos (nombre, cantidad, descripción).
2.  **Mejorar personalización de personaje:** Investigar cómo obtener los nombres o descripciones de las opciones de apariencia (ej. "Pelo largo", "Rojo") en lugar de solo "Estilo X de Y".
3.  **Verificar y pulir:** Probar exhaustivamente todos los menús para asegurar que la lectura sea fluida y no haya regresiones.

---
### Workaround Temporal

*   **Menú "Unirse a la Partida":** Actualmente, para que la navegación por teclado funcione correctamente en el menú "Unirse a la Partida", es necesario mantener el cursor del ratón en el centro de la pantalla. Si el ratón se encuentra en los bordes superior o inferior de la ventana del juego, la navegación con las flechas del teclado puede bloquearse. Este es un comportamiento temporal mientras se investiga una solución más permanente.