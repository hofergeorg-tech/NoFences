# Ayuda de NoFences

NoFences coloca en tu escritorio recuadros («vallas») que mantienen tus iconos ordenados, además de notas y widgets.
English: [HELP.md](HELP.md) · Deutsch: [HILFE.md](HILFE.md) · Italiano: [AIUTO.md](AIUTO.md) · Français : [AIDE.md](AIDE.md)

## Primeros pasos

- Tras el primer inicio hay una valla vacía. Arrastra archivos o carpetas sobre ella.
- **Clic derecho en una valla** (título o espacio vacío) para su menú: configuración, estilo, cambiar nombre, nueva valla
  o widget, eliminar.
- **Clic derecho en un elemento** para el menú normal del Explorador. Mayús + clic derecho muestra el menú de la valla.
- El **icono de la bandeja** (abajo a la derecha, quizá tras la flecha ^) crea vallas, las muestra u oculta, cambia de
  perfil y abre la **Configuración** general. El **idioma** está en la bandeja y en el menú de cada valla.

## Tipos de vallas

- **Valla de accesos directos** (predeterminada): enlaces a archivos y carpetas. Los archivos se quedan donde están, así
  que siguen en el escritorio. Si eliminas el original, también desaparece de la valla.
- **Valla de carpeta**: muestra el contenido de una carpeta. Los archivos que sueltas se **mueven** a esa carpeta (con
  Ctrl se copian), así que salen de verdad del escritorio.
- **Nota**: un pósit con texto, ver más abajo.
- **Widget**: contenido en directo – reloj, tiempo, juegos, citas y más, ver más abajo.
- **Archivos recientes**: los 20 últimos archivos abiertos (solo lectura).
- **Barra de inicio rápido**: una valla de accesos directos estrecha, solo con iconos; los nombres aparecen como información sobre herramientas.

Consejo: para un escritorio ordenado, crea una carpeta como `Documentos\Vallas\Trabajo` y úsala como valla de carpeta.

## Trabajar con los elementos

- Doble clic abre un elemento. Arrastra los elementos para reordenarlos, a otra valla para moverlos allí o al Explorador.
- **Ctrl+clic** selecciona varios elementos, **Mayús+clic** un rango; arrastrar en un espacio vacío dibuja un rectángulo de selección.
- Una valla en la que has hecho clic responde al teclado: **Intro** abre, **F2** cambia el nombre, **Supr** quita (valla
  de accesos directos: solo el enlace; valla de carpeta: papelera), **Ctrl+A**, **Ctrl+C**, flechas, **Esc**.
- **Escribe directamente** para buscar en esa valla; el texto aparece arriba a la derecha, Esc termina.
- Menú de la valla → **Ordenar por**: manual, nombre, tipo, fecha de modificación o tamaño.
- **Pestañas** (vallas de accesos directos): menú → Añadir pestaña. Clic para cambiar, doble clic para cambiar el nombre,
  arrastra elementos a una pestaña para moverlos allí.

## Buscar en todas las vallas

**Ctrl+Alt+F** (o clic derecho en el icono de la bandeja o en una valla → Herramientas ▸ Buscar en las vallas…) abre un cuadro de búsqueda. Encuentra todo en tus vallas – accesos
directos, contenido de carpetas, pestañas y textos de notas – incluso letras en orden («ffx» encuentra Firefox) – y
también aplicaciones del menú Inicio y páginas de la configuración de Windows («bluetooth», «sonido»). Escribe un cálculo
como `12*7` o `200*15%` e Intro copia el resultado. **Intro** abre el resultado, **↑↓** para elegir, **Esc** cierra. El
atajo se cambia en Configuración → Escritorio.

## Mover, cambiar el tamaño, cambiar el nombre

- Arrastra la barra de título para mover y los bordes para cambiar el tamaño. **Doble clic en el título** para cambiar el nombre.
- Las vallas se **ajustan** a los bordes de la pantalla y a otras vallas; mantén **Alt** para colocarlas libremente.
- Las posiciones se recuerdan **por configuración de monitores**: desconecta y vuelve a conectar un monitor y las vallas vuelven.
- Las vallas **bloqueadas** no se pueden mover ni cambiar. **Contraer cuando el ratón se aleja** las reduce a la barra de título.
- **Siempre visible** mantiene una valla encima de todas las ventanas (sobre juegos solo en modo «ventana sin bordes»).
- **Solo en este escritorio virtual** muestra una valla solo en el escritorio virtual actual (Win+Ctrl+flechas).
- **Ctrl+Alt+D** trae todas las vallas delante de las ventanas abiertas; Esc o un clic fuera las devuelve atrás.

## Notas

- **Doble clic** para escribir; **Esc** o un clic fuera guarda.
- Las líneas que empiezan con `[ ]` se convierten en casillas; un clic las marca y tacha la línea.
- Las direcciones web y rutas aparecen subrayadas y se abren con un clic. El texto arrastrado a una nota se añade al final.
- **Formato**: `# Título` (también `##`, `###`), `- elemento` o `* elemento` para listas, `> cita`, `---` para una línea,
  `**negrita**` y `*cursiva*`.
- **Ctrl+Alt+N** (se cambia en Configuración → Escritorio) crea desde cualquier sitio una nota junto al ratón, lista para
  escribir.
- Menú de la valla → **Recordatorio…**: a la hora elegida, NoFences suena y muestra una notificación – una vez, a diario,
  entre semana, cada semana o cada mes.
- Estilo pósit en amarillo, rosa, verde, azul y naranja.

## Widgets

Menú de la bandeja o de una valla → **Nuevo widget**. Los widgets con listas se desplazan con la rueda del ratón.

- **Reloj y calendario**.
- **Monitor del sistema**: CPU, RAM, carga y temperatura de la GPU (NVIDIA) y **FPS** si está activado (ver más abajo).
- **Unidades**: nivel de llenado y espacio libre; un clic abre la unidad.
- **Papelera**: suelta archivos en ella para eliminarlos, doble clic la abre, el menú la vacía.
- **Tiempo de juego**: hoy / esta semana / este mes / total para cualquier juego. Haz doble clic y elige el exe del juego;
  NoFences registra cuánto tiempo se ejecuta.
- **Cuenta atrás**: días y horas hasta una fecha; doble clic para configurarla.
- **Tiempo**: el tiempo actual y la previsión de tres días para un lugar que busques (datos: Open-Meteo, sin cuenta).
- **Reproduciendo**: título, artista y carátula de lo que suena en Spotify, un navegador o un reproductor, con
  anterior / reproducir-pausa / siguiente.
- **Red**: velocidad de descarga y subida con un gráfico del último minuto, y el ping.
- **Historial del portapapeles**: los 15 últimos textos copiados; un clic los copia de nuevo. Solo se guardan mientras
  NoFences está abierto; las contraseñas de los gestores de contraseñas se omiten.
- **Batería**: carga, si se está cargando, tiempo restante (portátiles).
- **Juegos**: tus juegos instalados de Steam (con carátulas), Epic, GOG y la app de Xbox; primero los jugados
  recientemente. Un clic inicia el juego. El menú permite ocultar juegos, ordenar por nombre o buscar de nuevo.
- **Citas**: las próximas dos semanas desde enlaces de calendario (.ics). Google: configuración del calendario →
  «Dirección secreta en formato iCal»; Outlook: Configuración → Calendario → Calendarios compartidos → Publicar → ICS;
  iCloud: comparte el calendario públicamente. Varios calendarios: un enlace por línea. Se admiten citas periódicas.
- **Marco de fotos**: presentación de una carpeta de imágenes (también subcarpetas), cada 10 s hasta 15 min. Un clic
  muestra la imagen siguiente, doble clic la abre.
- **Temporizador de concentración (Pomodoro)**: 25 minutos de concentración, 5 de descanso y un descanso largo tras cuatro
  rondas (o 50/10, 15/3). Un sonido y una notificación marcan cada cambio.
- **Noticias**: titulares de fuentes RSS o Atom (botones listos para El País, BBC, heise y otros); un clic abre el artículo.
- **Cotizaciones**: acciones, índices y criptomonedas con la variación desde ayer y un gráfico del día, con símbolos de
  Yahoo Finance como `AAPL`, `^IBEX`, `^GDAXI`, `BTC-EUR`. Se actualizan cada cinco minutos; solo a título informativo.
- **Tiempo de pantalla**: qué programas usaste y cuánto tiempo hoy o en los últimos 7 días (clic en «hoy ⇄» para
  cambiar). Solo se registra mientras existe el widget y estás en el PC; se queda en este PC.
- **Sonido**: volumen del dispositivo de reproducción actual (clic en la barra o rueda del ratón), silenciar altavoces y
  micrófono, y cambiar con un clic a otro dispositivo (auriculares ↔ altavoces).
- **Estado de servicios**: si RSI, Discord, Epic Games, GitHub y otros tienen problemas ahora mismo, según sus páginas
  de estado públicas; un clic en una línea abre la página.
- **Lista de tareas**: doble clic para añadir una tarea, con vencimiento y repetición si quieres; un clic en el círculo la
  marca (las periódicas pasan a su siguiente fecha). Las tareas pendientes se avisan aunque el widget esté oculto.
- **Reloj mundial**: la hora en otros lugares con la diferencia respecto a la tuya; doble clic para elegir zonas horarias.
- **Plan de energía**: cambia con un clic entre Equilibrado, Alto rendimiento y otros.
- **Ofertas de Steam**: las ofertas actuales de Steam; los juegos de tu lista de deseos van primero si es pública (se usa
  la cuenta de Steam iniciada en este PC). Un clic abre la página de la tienda en Steam.

Cada widget tiene sus propios ajustes en su menú. El menú del temporizador de concentración incluye además el **modo
concentración**: durante una ronda cambia a un perfil que elijas (p. ej. «Concentración» solo con vallas de trabajo) y
vuelve en los descansos.

## Asistente de escritorio

Menú de la bandeja o de una valla → Herramientas ▸ **Asistente de escritorio…** (también se ofrece en el primer inicio) ordena lo que hay en tu escritorio en
vallas nuevas – juegos, programas, documentos, imágenes, música y vídeos, archivos comprimidos, carpetas –, cada una con
un estilo adecuado. No se mueve nada: las vallas enlazan a los archivos. Para ocultar los originales: clic derecho en el
escritorio → Ver → Mostrar iconos del escritorio.

## Regla en pantalla

Menú de la bandeja o de una valla → Herramientas ▸ **Regla en pantalla** pone una regla encima de todo: arrastra para moverla, arrastra el extremo para alargarla,
doble clic o espacio la gira, las flechas la ajustan píxel a píxel (Mayús: 10 px), U o el menú cambia entre píxeles,
centímetros y pulgadas (tamaño real, según el tamaño que indica el monitor). Una línea roja sigue al ratón y muestra la
distancia. Esc la cierra.

## Cuentagotas y limpieza

- Herramientas ▸ **Cuentagotas**: la pantalla se congela y una lupa sigue al ratón; un clic copia el color como `#RRGGBB`
  (Mayús+clic: `rgb(…)`), Esc cancela.
- Herramientas ▸ **Limpiar carpetas…**: muestra lo que lleva una semana, un mes, tres meses o un año sin tocarse, primero
  lo más grande; lo elegido va a la papelera (se puede restaurar). Al principio es la carpeta Descargas; **Añadir
  carpeta…** añade más (escritorio, vídeos, una carpeta de juegos…) y la lista se guarda.

## Perfiles

Agrupa las vallas en perfiles como «Trabajo» y «Juegos» y cambia entre ellos en la bandeja (**Perfil ▸**) o en
**Configuración → Escritorio**. Clic derecho en una valla → **Mostrar en el perfil** para asignarla; una valla sin perfil
aparece en todos los perfiles. Las vallas creadas mientras hay un perfil activo pertenecen a él. **Ctrl+Alt+F1…F9**
cambian al perfil 1…9 y **Ctrl+Alt+F10** muestra todas las vallas. Con un perfil activo, bandeja → Perfil ▸ **Fondo de
pantalla para «…»** le da su propio fondo; en los perfiles sin fondo propio vuelve el habitual. **Plan de energía para
«…» ▸** en el mismo menú cambia también el plan de energía con el perfil (p. ej. Alto rendimiento para Juegos).

## Automatización (Configuración → Automatización)

- **Cambiar de perfil automáticamente**: «Juegos» mientras se ejecuta un programa concreto, «Trabajo» entre semana de 8
  a 17, etc. Un programa en ejecución tiene prioridad sobre una regla horaria; cuando ya no se aplica ninguna regla,
  vuelve el perfil anterior. Cambiar a mano termina lo que había iniciado una regla.
- **Pantalla completa**: mientras un juego, un vídeo o una presentación ocupa un monitor, las vallas de ese monitor se ocultan.
- **Estilo claro y oscuro**: el estilo predeterminado sigue el modo claro/oscuro de Windows o cambia a horas fijas – por
  ejemplo, pósit de día y cristal por la noche. Las vallas con estilo propio lo mantienen.
- El estilo **Color de énfasis de Windows** toma su color de Configuración → Personalización → Colores.

## Varios PC

Configuración → Datos y estilos → **Elegir carpeta compartida…**, p. ej. en OneDrive. Las vallas, notas, tiempo de juego
y estilos propios se guardan entonces allí, y cada PC que use la misma carpeta muestra las mismas vallas. Las posiciones
se guardan por configuración de monitores, así que un portátil y un PC de sobremesa pueden colocarlas de forma distinta.
Cuando otro PC guarda, NoFences recarga en pocos segundos. «Dejar de compartir» vuelve a copiar todo a este PC.

## Medición de FPS (opcional)

Windows solo entrega los eventos de la tasa de fotogramas a programas con permisos de administrador. Por eso NoFences usa
un pequeño proceso asistente que se ejecuta como administrador; NoFences no. Solo cuenta fotogramas, nada del contenido
ni de lo que escribes. Actívala en **Configuración → Medición de FPS**; Windows pregunta una vez y después una tarea del
Programador de tareas inicia el asistente sin preguntar. Al desactivarla, la tarea se elimina.

## Ordenar desde el escritorio

En la configuración de una valla, escribe patrones en «Ordenar desde el escritorio», p. ej. `*.pdf; *.docx`, o añade una
plantilla. Los archivos nuevos del escritorio que coincidan van a esa valla (también las descargas terminadas).
**Ordenar el escritorio ahora** (bandeja o configuración) ordena lo que ya está allí.

## Estilos

Elige el predeterminado en **Configuración → General**, o uno por valla (menú → Estilo, o la configuración de la valla con
vista previa en directo). Hay 25 estilos – cristal, color de énfasis de Windows, HUD de Star Citizen, Retro-Arcade,
Hardware, Friki, Aficiones, Trabajo, Familia, Gaming, Finanzas, Redes sociales, Documentos, Multimedia, Música, Deporte,
Fotos, Viajes, Cocina, Naturaleza y pósit en cinco colores.

**Estilos propios**: Configuración → Datos y estilos → Abrir carpeta de estilos. Copia `beispiel-mocha.json`, cambia los
colores (`#RRGGBB` o `#RRGGBBAA`) y recarga. Los estilos propios llevan una ★.

## Configuración (bandeja → Configuración)

- **General**: idioma (automático, English, Deutsch, Italiano, Français, Español), iniciar con Windows, extensiones,
  estilo predeterminado, animaciones.
- **Escritorio**: doble clic en el escritorio oculta/muestra las vallas; atajo para traerlas al frente (Ctrl+Alt+D);
  perfiles; atajo de búsqueda (Ctrl+Alt+F); ordenación.
- **Automatización**: reglas de perfil, pantalla completa, estilo claro y oscuro (ver arriba).
- **Actualizaciones**: NoFences comprueba GitHub e instala las versiones nuevas con un clic; donaciones.
- **Medición de FPS**: ver arriba.
- **Datos y estilos**: exportar/importar vallas, restaurar una copia de seguridad (cada 12 horas), carpeta compartida, carpetas.

## Preguntas frecuentes

**¿Puedo eliminar el original después de arrastrar un icono a una valla?**
En una valla de accesos directos, no: la valla solo enlaza a él. En una valla de carpeta el archivo se movió, así que no
queda nada que eliminar.

**Windows muestra un aviso de SmartScreen al iniciar NoFences.**
El exe aún no está firmado. Haz clic en «Más información» → «Ejecutar de todas formas».

**¿Qué se conecta a Internet?**
Solo lo que configures: la búsqueda de actualizaciones (GitHub), el tiempo (Open-Meteo), tus enlaces de calendario, las
fuentes de noticias y las cotizaciones (Yahoo Finance). No se envía nada más.

**¿Dónde está mi configuración?**
En `%LocalAppData%\NoFences\fences.json` (copias de seguridad al lado), o en la carpeta compartida si elegiste una. Con un
archivo vacío `portable.txt` junto a `NoFences.exe`, se guarda junto al exe.

**¿Cómo desinstalo NoFences?**
Configuración → General: desmarca «Iniciar con Windows»; desactiva la medición de FPS si la usas; bandeja → Salir; elimina
`NoFences.exe` y la carpeta `%LocalAppData%\NoFences`. Los archivos de las vallas de carpeta se quedan en sus carpetas.
