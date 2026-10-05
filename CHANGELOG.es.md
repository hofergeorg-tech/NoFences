# Registro de cambios

Todos los cambios importantes de este fork.
English: [CHANGELOG.md](CHANGELOG.md) · Deutsch: [CHANGELOG.de.md](CHANGELOG.de.md) ·
Italiano: [CHANGELOG.it.md](CHANGELOG.it.md) · Français : [CHANGELOG.fr.md](CHANGELOG.fr.md)

## [2.10.0] - sin publicar

### Novedades
- **Notas**: progreso de la lista en el título, lo hecho al final u oculto, listas que se desmarcan cada día/semana/mes, subelementos con flechas, contadores `[3/8]`, cálculos (`650 + 80 =` → 730), `#etiquetas` de color (un clic busca), tablas, tamaño del texto con Ctrl+rueda, **plantillas** (también propias), las últimas 20 **versiones** para restaurar, **exportar** a Markdown o PDF e imprimir.

## [2.9.0] - 2026-10-05

### Novedades
- **Formato en notas**: subrayado, tachado, resaltado en colores y banderitas de prioridad (!!! / !! / !) además de negrita y cursiva – con una barra de formato sobre el editor y Ctrl+B / I / U / H.

### Cambios
- El estilo **Tabla** usa ahora fotos reales (speck, queso, pan, salchicha, pepinillos, rábanos sobre madera) en lugar de formas dibujadas.

### Correcciones
- Una valla que se despliega (contraer cuando el ratón se aleja) se abre ahora siempre por encima de las vallas vecinas.

## [2.8.0] - 2026-10-04

### Novedades
- Nuevo estilo **Tabla (speck y queso)**: una tabla de madera con speck, queso, pan y rábanos. Las barras laterales pueden tener un **estilo de fondo** (Grupo → Anclar al borde de la pantalla → Fondo de la barra), p. ej. una mesa de madera con mantel de cuadros y comida entre las vallas.

## [2.7.0] - 2026-10-04

### Novedades
- **Barra lateral**: ancla un grupo de vallas al borde izquierdo, derecho, superior o inferior de un monitor (clic derecho → Grupo → Anclar al borde de la pantalla). Entra cuando el ratón toca el borde o reserva su espacio como la barra de tareas; nunca sobre juegos a pantalla completa.

## [2.6.0] - 2026-10-04

### Novedades
- **Traducciones propias**: Configuración → General → «Traducciones propias…» abre la carpeta `lang` con una plantilla en inglés. Un archivo como `nl.json` añade un idioma, un `es.json` con textos sueltos cambia solo esos. Todos los textos están ahora en un archivo JSON por idioma.
- **Banderas para cada idioma**: el menú de idiomas muestra banderas reales (flag-icons), así los idiomas propios también tienen bandera (por el código de idioma, una región como `pt-BR` o `"_flag": "at"` en el archivo de idioma).
- **Deshacer (Ctrl+Z)** para eliminar, mover, redimensionar y renombrar vallas y para quitar, mover y renombrar elementos; también «Deshacer: …» en el menú de la bandeja y de la valla.
- **Grupos de vallas**: clic derecho → Grupo. Las vallas de un grupo se mueven juntas y se pliegan juntas a su barra de título.
- **Vallas**: abrir carpetas dentro de la valla (flecha en la esquina, contenido sangrado debajo), **notas en elementos** (información emergente), **uso y limpieza** (elementos nunca abiertos como sugerencia), **imagen de fondo** o patrón por valla y **abrir archivos con** un programa elegido.
- Las **notas** reconocen citas («lunes 14:00 Dentista», «mañana 9:30 …», «12.10. 15:00 …») y ofrecen un recordatorio 15 minutos antes; los temporizadores y alarmas que suenan se pueden **posponer** 5 o 10 minutos. Nuevas herramientas: **generador de contraseñas** (copia sin historial del portapapeles) e **información de red** (direcciones, Wi-Fi, router; un clic copia).

### Cambios
- **Todos los datos junto a NoFences.exe**, ordenados en carpetas (`config`, `backups`, `themes`, `media`, `cache`, `logs`, `lang`). Los datos de versiones anteriores se copian una vez; en una carpeta de programa sin permiso de escritura (Archivos de programa, WinGet) se quedan en `%LocalAppData%\NoFences`, ordenados igual.

## [2.5.0] - 2026-10-04

### Novedades
- **Juegos**: tiempo de juego de cada juego detectado, contado automáticamente y mostrado bajo su carátula; orden por más jugados.
- Widgets de **noticias de juegos** (anuncios y notas de parche de tus juegos de Steam) y **Twitch en directo** (quién está en directo, con aviso).
- **Ofertas de Steam** configurables: ofertas, más vendidos, novedades o solo tu lista de deseos; descuento mínimo, precio máximo, número.
- Widgets de **temporizador y alarma** (temporizadores rápidos, alarmas en los días elegidos, suena aunque esté oculto), **hábitos** (marca los últimos 7 días, rachas) y **progreso del tiempo** (día, semana, mes, año).
- **Recordatorio de descanso** tras 30–120 minutos de uso activo del PC (Configuración → Automatización).
- El **calendario del reloj** marca los días con citas.
- **Vallas**: marcas de color para los elementos (Mayús+clic derecho → Marcar, o Ctrl+1…6), orden **más usados primero**, **vista previa al pasar el ratón** (contenido de carpetas, vista previa grande de imágenes/PDF).
- **Más vallas**: **plantillas** (configuración gaming, oficina, mínima), una **bandeja** que se vacía sola, **marcadores del navegador** (Chrome, Edge, Brave, Vivaldi, Opera) y **carpetas abiertas recientemente**.
- **Atajo propio por valla** (Ctrl+Mayús+F1…F12) la trae al frente, incluso desde otro perfil.
- Las vallas pueden **atenuarse** cuando el ratón está lejos (Configuración → Escritorio); se pueden excluir vallas.
- Herramientas: **mostrar/ocultar iconos del escritorio** y **mover todas las vallas a otro monitor**.
- **Perfiles**: iniciar programas con un perfil (y cerrarlos al salir, si se desea); **fondo según la hora del día** (Configuración → Automatización).
- **Notas**: protección con contraseña (cifrada, se bloquea sola tras 2 minutos), pegar **imágenes** con Ctrl+V, grabar **notas de voz**.
- El **historial del portapapeles** guarda también imágenes; **fija** entradas (clic derecho) para que queden arriba, incluso tras reiniciar.
- **Monitor del sistema** con gráfico de dos minutos y **aviso si la tarjeta gráfica se calienta demasiado**; **test de velocidad** en el widget de red.
- El **widget de batería** muestra también **mandos y dispositivos Bluetooth**; nuevo widget **Inicio automático** (activar/desactivar programas de inicio de Windows).
- Herramientas: **código QR** (texto o enlace del portapapeles), **lupa**; "Ordenar carpetas" encuentra **archivos duplicados**.
- **Tiempo**: aviso de lluvia para las próximas dos horas («Lluvia en unos 20 min»), salida y puesta del sol, fase lunar.
- **Diseñador de estilos**: crea tu propio estilo con clics (colores, fuentes, borde, esquinas) con vista previa en vivo; nuevo estilo **Alto contraste** con texto grande.
- Nuevo widget **Página web**: una página pequeña (panel, página de estado …) directamente en la valla, actualizada con regularidad.
- **Ofertas de Steam**: una lista de deseos no pública funciona con su **enlace para compartir**.
- Las **ofertas de Steam** muestran todas las ofertas actuales (no solo las destacadas; al desplazar se cargan más) y cada juego rebajado de tu lista de deseos.

## [2.4.2] - 2026-10-03

### Cambios
- **Limpiar carpetas** (antes: Limpiar Descargas): añade más carpetas además de Descargas; se revisan todas juntas y una
  columna muestra dónde está cada elemento. La lista de carpetas se guarda.

### Correcciones
- Las listas desplazables (ofertas de Steam, noticias, citas, tareas) se quedaban desplazadas y bloqueadas tras agrandar la valla.

## [2.4.1] - 2026-10-03

### Novedades
- La **búsqueda** encuentra también aplicaciones del menú Inicio y páginas de la configuración de Windows, y calcula
  (`12*7`, `200*15%`; Intro copia).
- Widgets de **tiempo de pantalla** (programas usados hoy / 7 días, se queda en este PC), **sonido** (volumen, silencio,
  micrófono, cambiar dispositivo) y **estado de servicios** (RSI, Discord, Epic Games, GitHub… según sus páginas de estado).
- **Modo concentración**: el temporizador cambia a un perfil elegido durante las rondas de concentración.
- **Atajos de perfil** Ctrl+Alt+F1…F9 (F10: todas las vallas) y un **fondo de pantalla por perfil**.
- **Asistente de escritorio**: ordena los iconos del escritorio en vallas nuevas por tipo (se ofrece en el primer inicio).
- **Regla en pantalla** en píxeles, centímetros o pulgadas (Herramientas ▸ Regla en pantalla – en la bandeja y en el menú de cada valla).
- **Cuentagotas** con lupa (copia #RRGGBB) y **Limpiar Descargas** (archivos antiguos, primero los más grandes, a la papelera).
- **Nota rápida** desde cualquier sitio con Ctrl+Alt+N; **formato en las notas** (títulos, listas, citas, líneas, negrita, cursiva).
- **Recordatorios periódicos** (a diario, entre semana, cada semana, cada mes) y un widget de **lista de tareas** con vencimientos.
- Widgets de **reloj mundial**, **plan de energía** (también por perfil) y **ofertas de Steam** (primero la lista de deseos).

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
