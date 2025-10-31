# Core Keeper Accessibility Mod

![Version](https://img.shields.io/badge/version-1.0.0-blue)
![BepInEx](https://img.shields.io/badge/BepInEx-5.x-green)
![Core Keeper](https://img.shields.io/badge/Core%20Keeper-Compatible-orange)

**Mod de accesibilidad completo para Core Keeper diseñado para jugadores ciegos y con discapacidad visual.**

Este mod añade compatibilidad total con lectores de pantalla (NVDA, JAWS, etc.) mediante la librería Tolk, permitiendo jugar Core Keeper de manera completamente accesible.

---

## 📋 Tabla de Contenidos

- [Características Principales](#-características-principales)
- [Instalación](#-instalación)
- [Controles y Comandos](#-controles-y-comandos)
  - [Menús e Interfaz](#menús-e-interfaz)
  - [Cursor Virtual](#cursor-virtual)
  - [Inventario y Crafteo](#inventario-y-crafteo)
  - [Sistema de Notificaciones](#sistema-de-notificaciones)
  - [Sistema de Tutoriales](#sistema-de-tutoriales)
- [Sistemas Automáticos](#-sistemas-automáticos)
- [Idiomas Soportados](#-idiomas-soportados)
- [Requisitos](#-requisitos)
- [Problemas Conocidos](#-problemas-conocidos)
- [Contribuir](#-contribuir)

---

## 🎮 Características Principales

### Accesibilidad de Menús
- ✅ **Navegación completa por teclado** en todos los menús del juego
- ✅ **Lectura automática** de opciones, descripciones y tooltips
- ✅ **Soporte para gamepad** (D-Pad para navegación en menús)
- ✅ Menú principal, ajustes, creación de mundos, selección de personajes
- ✅ Menú de unirse a partida con lectura de opciones de conexión
- ✅ Personalización de personaje con lectura detallada de opciones

### Accesibilidad de Gameplay
- ✅ **Cursor Virtual** para navegación tile-por-tile en el mundo
- ✅ **Auto-Targeting** inteligente para combate
- ✅ **Sonidos de Proximidad** espacializados para objetos y enemigos
- ✅ **Anuncios de tiles** al caminar (describe el terreno adelante)
- ✅ Sistema completo de **notificaciones** con historial navegable
- ✅ Sistema de **tutoriales** con buffer separado

### Accesibilidad de Inventario
- ✅ Navegación por teclado y D-Pad en inventarios
- ✅ Lectura completa de items: nombre, cantidad, durabilidad, atributos
- ✅ Accesibilidad de habilidades (Skills) con estadísticas detalladas
- ✅ Árboles de talento totalmente accesibles
- ✅ Hotbar con anuncios al cambiar de slot (teclas 1-0)

---

## 📥 Instalación

### Requisitos Previos
1. **Core Keeper** instalado
2. **BepInEx 5.x** instalado ([Descargar aquí](https://github.com/BepInEx/BepInEx/releases))
3. **Lector de pantalla** (NVDA, JAWS, etc.)

### Pasos de Instalación
1. Descarga la última versión del mod desde [Releases](https://github.com/Ali-Bueno/CkAccess/releases)
2. Extrae el archivo `ckAccess.dll` en la carpeta `BepInEx/plugins/`
3. Asegúrate de que la carpeta `Languages` y el archivo `Select.wav` estén en la misma ubicación
4. Inicia Core Keeper
5. El mod se cargará automáticamente

---

## 🎯 Controles y Comandos

### Menús e Interfaz

#### Navegación General en Menús
- **Flechas / WASD**: Navegar entre opciones
- **Enter / Espacio**: Seleccionar opción
- **Escape**: Retroceder/Cancelar
- **Tab**: Cambiar entre secciones (donde aplique)

#### Navegación en Inventario
- **WASD / Flechas**: Navegar entre slots
- **D-Pad (Gamepad)**:
  - ⬆️ Arriba: Cambiar hotbar siguiente
  - ⬇️ Abajo: Cambiar hotbar anterior
  - ⬅️ Izquierda: Quick Stack
  - ➡️ Derecha: Ordenar
- **U**: Acción primaria (seleccionar, usar, abrir árbol de talento)
- **O**: Acción secundaria (click derecho)
- **1-0**: Seleccionar slot del hotbar (anuncia el item equipado)

### Cursor Virtual

El cursor virtual se activa **automáticamente** al entrar al mundo.

#### Controles con Teclado
| Tecla | Función |
|-------|---------|
| **I** | Mover cursor arriba (1 tile) |
| **J** | Mover cursor izquierda (1 tile) |
| **K** | Mover cursor abajo (1 tile) |
| **L** | Mover cursor derecha (1 tile) |
| **R** | Resetear cursor a posición del jugador |
| **U** | Acción primaria (atacar, minar, interactuar) |
| **O** | Acción secundaria (usar item, colocar objeto) |
| **P** | Información de posición del cursor |
| **M** | Posición detallada del jugador |

#### Controles con Gamepad
| Control | Función |
|---------|---------|
| **Stick Derecho** | Mover cursor (tile por tile, idéntico a I/J/K/L) |
| **R2 (Trigger Derecho)** | Acción primaria (equivalente a U) |
| **L2 (Trigger Izquierdo)** | Acción secundaria (equivalente a O) |

**Nota**: El stick derecho tiene una zona muerta de 0.5 y debounce de 200ms para control preciso.

### Inventario y Crafteo

#### Habilidades (Skills)
- **Navegar a una habilidad**: Anuncia nombre, nivel, puntos de talento y estadísticas
- **Presionar U en habilidad**: Abre el árbol de talento
- **Navegar en árbol de talento**: Anuncia nombre, efectos y descripción de cada talento
- **Presionar U en talento**: Invierte un punto de talento (si está disponible)

#### Interacción con Items
- **U en slot de inventario**: Selecciona/usa el item
- **O en slot de inventario**: Acción secundaria (menú contextual)
- **U en pestaña**: Cambia a esa pestaña (Equipamiento, Habilidades, Almas)

### Sistema de Notificaciones

El mod incluye un buffer de hasta **100 notificaciones** con navegación:

| Tecla | Función |
|-------|---------|
| **Punto (.)** | Siguiente notificación (más reciente) |
| **Coma (,)** | Notificación anterior (más antigua) |
| **Shift + Punto** | Saltar a la última notificación |
| **Shift + Coma** | Saltar a la primera notificación |

**Tipos de notificaciones**:
- Items recogidos
- Subidas de nivel (suma de skills)
- Mejoras de skills individuales
- Mensajes importantes del juego

### Sistema de Tutoriales

Buffer separado para mensajes de tutorial con navegación independiente:

| Tecla | Función |
|-------|---------|
| **' (Apóstrofe)** | Siguiente tutorial (más reciente) |
| **Ñ (Semicolon en teclado inglés)** | Tutorial anterior (más antiguo) |
| **Shift + '** | Saltar al último tutorial |
| **Shift + Ñ** | Saltar al primer tutorial |

Buffer de hasta **50 tutoriales** en memoria.

---

## 🤖 Sistemas Automáticos

Estos sistemas funcionan automáticamente en segundo plano:

### Auto-Targeting Inteligente
- **Detección automática** de enemigos cercanos
- **Rango adaptativo** según el arma equipada:
  - Melee: 3 tiles
  - Ranged: 10 tiles
  - Magic: 8 tiles
- Al presionar **U/R2**, apunta automáticamente al enemigo más cercano
- Anuncia enemigos que entran/salen del rango con dirección y distancia
- **Excluye**: Estatuas, decoraciones, tus minions y mascotas

### Sonidos de Proximidad

#### Para Objetos Interactuables
- Se activan al caminar cerca de:
  - Cofres
  - Puertas
  - Altares
  - Estaciones de trabajo
  - Otros objetos importantes
- **Pitch dinámico**: grave (lejos) → agudo (cerca)
- **Audio espacializado**: indica dirección (izquierda/derecha)
- **Línea de visión**: No suenan a través de paredes sólidas
- **Excepciones**: Cristales y vallas permiten escuchar a través

#### Para Enemigos
- Actualización en tiempo real de posición
- Detección de movimiento
- Diferenciación sutil por tipo de enemigo
- **No detecta**: Tus minions, mascotas o aliados

### Anuncios de Tiles al Frente
- Anuncia automáticamente el tile 1 unidad adelante según dirección de movimiento
- Funciona con **teclado y gamepad**
- Describe: tipo de tile, material, si es destructible, herramienta recomendada
- Solo anuncia cuando el tile cambia (evita repeticiones)

---

## 🌍 Idiomas Soportados

El mod soporta **19 idiomas** con fallback automático a inglés:

- 🇪🇸 Español
- 🇬🇧 Inglés
- 🇫🇷 Francés
- 🇩🇪 Alemán
- 🇮🇹 Italiano
- 🇧🇷 Portugués Brasileño
- 🇳🇱 Holandés
- 🇷🇺 Ruso
- 🇵🇱 Polaco
- 🇹🇷 Turco
- 🇺🇦 Ucraniano
- 🇨🇿 Checo
- 🇸🇪 Sueco
- 🇯🇵 Japonés
- 🇨🇳 Chino Simplificado
- 🇹🇼 Chino Tradicional
- 🇰🇷 Coreano
- 🇸🇦 Árabe
- 🇹🇭 Tailandés

El idioma se detecta automáticamente según la configuración del juego.

---

## ⚙️ Requisitos

### Requisitos del Sistema
- **Core Keeper**: Versión actual
- **BepInEx**: 5.4.x o superior
- **Framework**: .NET Framework 4.7.2

### Requisitos de Accesibilidad
- **Lector de pantalla** compatible con Tolk:
  - NVDA (recomendado)
  - JAWS
  - Window-Eyes
  - System Access
  - ZoomText
  - Otros compatibles con SAPI

---

## ⚠️ Problemas Conocidos

### Limitaciones Técnicas

1. **Botones individuales del gamepad** (L3, R3, Select, Start)
   - **Problema**: No se pueden detectar en ciertos contextos
   - **Causa**: Core Keeper/Rewired consume el input antes de que el mod pueda leerlo
   - **Solución actual**: Usar teclado para funciones como resetear cursor (R)

2. **Menú "Unirse a Partida"**
   - **Workaround temporal**: Mantener el cursor del ratón en el centro de la pantalla
   - **Causa**: Si el ratón está en los bordes, la navegación puede bloquearse
   - **Estado**: Se está investigando una solución permanente

### Rendimiento
- El mod está optimizado y no debería afectar el rendimiento del juego
- Los sonidos de proximidad tienen cooldowns para evitar spam de audio
- El sistema de notificaciones limpia automáticamente mensajes antiguos

---

## 🤝 Contribuir

¡Las contribuciones son bienvenidas! Si quieres ayudar:

1. **Reportar Bugs**: Abre un [Issue](https://github.com/Ali-Bueno/CkAccess/issues)
2. **Sugerencias**: Comparte tus ideas para mejorar la accesibilidad
3. **Traducciones**: Ayuda a mejorar o agregar traducciones
4. **Código**: Fork el repo y envía Pull Requests

### Guías de Contribución
- Lee `CLAUDE.md` para entender la arquitectura del mod
- Sigue las convenciones de código existentes
- Todos los commits deben estar en inglés
- Prueba exhaustivamente antes de enviar cambios

---

## 🙏 Agradecimientos

- **Pugstorm** - Desarrolladores de Core Keeper
- **BepInEx Team** - Framework de modding
- **Davy Kager** - Librería Tolk para lectores de pantalla
- **Comunidad de jugadores ciegos** - Por feedback y pruebas

---

## 📞 Contacto y Soporte

- **GitHub Issues**: [Reportar problema](https://github.com/Ali-Bueno/CkAccess/issues)
- **Repositorio**: [Ali-Bueno/CkAccess](https://github.com/Ali-Bueno/CkAccess)

---

## 🔄 Historial de Versiones

### Versión Actual (2025)
- ✅ Sistema completo de cursor virtual con soporte de gamepad
- ✅ Auto-targeting inteligente
- ✅ Sonidos de proximidad con línea de visión
- ✅ Sistema de notificaciones y tutoriales
- ✅ Accesibilidad completa de menús e inventarios
- ✅ Soporte para 19 idiomas
- ✅ Corrección de detección de minions del jugador
- ✅ Tile-ahead announcer con soporte de gamepad

---

**¡Disfruta de Core Keeper de manera totalmente accesible!** 🎮✨
