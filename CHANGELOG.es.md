# Registro de cambios

Todos los cambios importantes de este fork.
English: [CHANGELOG.md](CHANGELOG.md) · Deutsch: [CHANGELOG.de.md](CHANGELOG.de.md) ·
Italiano: [CHANGELOG.it.md](CHANGELOG.it.md) · Français : [CHANGELOG.fr.md](CHANGELOG.fr.md)

## [2.4.1] - 2026-10-03

### Novedades
- La **búsqueda** encuentra también aplicaciones del menú Inicio y páginas de la configuración de Windows, y calcula
  (`12*7`, `200*15%`; Intro copia).
- Widgets de **tiempo de pantalla** (programas usados hoy / 7 días, se queda en este PC), **sonido** (volumen, silencio,
  micrófono, cambiar dispositivo) y **estado de servicios** (RSI, Discord, Epic Games, GitHub… según sus páginas de estado).
- **Modo concentración**: el temporizador cambia a un perfil elegido durante las rondas de concentración.
- **Atajos de perfil** Ctrl+Alt+F1…F9 (F10: todas las vallas) y un **fondo de pantalla por perfil**.
- **Asistente de escritorio**: ordena los iconos del escritorio en vallas nuevas por tipo (se ofrece en el primer inicio).
- **Regla en pantalla** en píxeles, centímetros o pulgadas (bandeja → Regla en pantalla).

### Cambios
- **Menús agrupados**: Nuevo widget ▸ Tiempo y planificación / Información y noticias / Sistema / Juegos y multimedia;
  Estilo ▸ Básicos / Juegos y tecnología / Trabajo y día a día / Ocio / Pósit / Estilos propios.
- **Noticias** más claras: titulares en negrita en hasta dos líneas, la fuente en el color de acento, líneas separadoras.

### Correcciones
- Los widgets de noticias y cotizaciones decían «Sin conexión con el servicio» cuando fallaba una fuente.
- Una página web introducida como fuente ahora encuentra la fuente que anuncia la página, o indica claramente que no lo es.
- Una actualización fallida ya no borra titulares, cotizaciones ni citas ya mostrados; nuevo intento tras 30 segundos.
- Los problemas de los widgets en línea se escriben en log.txt en la carpeta de datos.

## [2.4.0] - 2026-10-03

### Novedades
- **Widgets**: reloj y calendario, monitor del sistema (CPU, RAM, carga y temperatura de la GPU, FPS), unidades,
  papelera, tiempo de juego y cuenta atrás. Menú de la bandeja o de una valla → Nuevo widget.
- **Tiempo de juego** para cualquier juego: elige su exe y NoFences registra cuánto tiempo se ejecuta (hoy, semana,
  mes, total, en curso).
- **Cuenta atrás** hasta una fecha, con título.
- Widgets de **tiempo** (Open-Meteo, sin cuenta), **reproduciendo** con controles, **red** con gráfico y ping,
  **historial del portapapeles** (solo en memoria, respeta los gestores de contraseñas) y **batería**.
- Widget de **juegos**: los juegos instalados de Steam (con carátulas), Epic, GOG y la app de Xbox; un clic los inicia.
- **Citas** desde enlaces de calendario (.ics: Google, Outlook, iCloud), también periódicas.
- Widgets de **marco de fotos**, **temporizador de concentración (Pomodoro)**, **noticias** (RSS/Atom con fuentes
  listas) y **cotizaciones** (acciones, índices, criptomonedas).
- **Búsqueda en todas las vallas** (Ctrl+Alt+F): accesos directos, contenido de carpetas, pestañas y notas.
- **Perfiles** como «Trabajo» y «Juegos»: se cambian en la bandeja y las vallas se asignan con clic derecho → Mostrar
  en el perfil.
- **Automatización**: cambiar de perfil mientras se ejecuta un programa o a horas fijas; ocultar las vallas en pantalla
  completa; estilo predeterminado claro/oscuro según Windows o la hora.
- **Varios PC**: las vallas en una carpeta compartida como OneDrive.
- Estilo **Color de énfasis de Windows**: 25 estilos en total.
- **Francés y español**; menú **Idioma** con banderas en la bandeja y en cada valla.
- Botón **Donar** (Acerca de y Configuración → Actualizaciones).
- **Medición de FPS** (opcional, desactivada por defecto): un pequeño asistente con permisos de administrador cuenta los
  fotogramas del programa en primer plano; Windows pregunta una vez y la configuración explica por qué.
- Valla **«Archivos recientes»** y **barra de inicio rápido** (solo iconos, nombres como información sobre herramientas).
- **Pestañas** en las vallas de accesos directos.
- **Solo en este escritorio virtual** por valla.
- **Exportar/importar** vallas y estilos propios, p. ej. a otro PC.
- **Ventana de configuración** (bandeja → Configuración) con todo lo de la aplicación; el menú de la bandeja es mucho más corto.
- **Configuración de la valla** rediseñada, con secciones y vista previa en directo.
- **Ventana Acerca de** con versión, créditos y enlaces.
- **8 estilos nuevos**: Documentos, Multimedia, Música, Deporte, Fotos, Viajes, Cocina, Naturaleza; cada estilo tiene
  un color de acento para los widgets.

### Correcciones
- Aceptar en la configuración de una valla de accesos directos quitaba todos sus accesos directos.
- Abrir la configuración de un widget provocaba un cierre inesperado.
- En el pósit, los elementos sobresalían por abajo sobre la sombra del papel.
- El atajo «Traer las vallas al frente» aparecía pegado al texto del menú.
- La entrada seleccionada en la barra lateral de la configuración se volvía ilegible.

## Versiones anteriores

Las versiones 2.0.0 a 2.3.0 se describen en el [registro en inglés](CHANGELOG.md). NoFences se basa en
[Twometer/NoFences](https://github.com/Twometer/NoFences) de Twometer y sus colaboradores.

[2.4.1]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.4.1
[2.4.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.4.0
