# Gestor de candidaturas

Aplicación de escritorio para llevar el control de las ofertas de empleo a las que te inscribes:
a quién, cuándo, qué te contestaron y cuándo toca volver a insistir.

## Stack

- **.NET 8** + **WPF** (MVVM)
- **Entity Framework Core 8** sobre **SQLite** (fichero local, sin servidor)
- **CommunityToolkit.Mvvm** para `ObservableObject` y `RelayCommand` por generadores de código
- **Hardcodet.NotifyIcon.Wpf** para el icono y los avisos de la bandeja del sistema
- Barras WPF nativas para la gráfica de embudo (mensual, semanal o diaria; sin dependencia de gráficos)

## Cómo ejecutarlo

Desde la raíz del repositorio:

```bash
dotnet restore
dotnet run
```

O abre `GestorSolicitudes.csproj` en Visual Studio y pulsa F5.

La base de datos se crea sola en el primer arranque en:

```
%APPDATA%\GestorSolicitudes\solicitudes.db
```

Está fuera de `bin/` a propósito: puedes recompilar, mover o reinstalar la app sin perder los datos.
Ese fichero `.db` es todo tu histórico, así que cópialo de vez en cuando a OneDrive o a un pendrive.
En el arranque, la app añade con un `ALTER TABLE` las columnas nuevas que no existan en una base
ya creada por una versión anterior (porque `EnsureCreated()` no versiona el esquema).

## Tests

```bash
dotnet test
```

xUnit sobre el código que admite pruebas sin abrir ventanas: validaciones y modelo de `Solicitud`,
helpers (enum → texto, CSV, adjuntos, columnas visibles), la localización (cobertura y paridad de claves
entre los tres idiomas), el `AppDbContext` contra una base SQLite temporal,
la verificación de columnas del arranque y la lógica del ViewModel (filtros, métricas, estadísticas,
embudo, paginación, adjuntos, diálogos y CSV).
Los tests construyen las candidaturas con `SolicitudBuilder` y el helper `RellenarObligatorios`, y
cubren el búfer de edición (`SolicitudEdicionViewModel`) y la ida y vuelta del historial con
`Include` al guardar.
También corre **ArchUnitNET**: reglas de arquitectura que vigilan que las capas solo dependan
"hacia abajo" (lo típico: los ViewModels no tocan las Vistas, las Vistas solo se usan desde el
arranque). Un using de más a una capa prohibida, o un `MessageBox` colado en un ViewModel, se cae
en el CI.
La app se abre a los tests con `[assembly: InternalsVisibleTo("GestorSolicitudes.Tests")]` y el
ViewModel se construye con un contexto apuntando a una base temporal, para no tocar tu histórico.
El CI de GitHub Actions corre en `windows-latest`: compila, exige `dotnet format`, ejecuta
`dotnet test` con umbral del 90 % de líneas y lanza el análisis de SonarCloud sobre la rama del PR.

## Qué se guarda de cada candidatura

**La oferta**: empresa, puesto, enlace directo (con botón para abrirlo en el navegador), portal de
origen, vía de contacto (cómo llegó el contacto: aplicación directa, recruiter, referido, networking,
feria de empleo...), ubicación, modalidad, tecnologías pedidas, horquilla salarial, tu pretensión, un
nivel de interés del 1 al 5 (con un desplegable) y los requisitos del cargo (con botón *Leer*
que los abre en una ventana más grande, con texto seleccionable y scroll, para leerlos con comodidad).

**El proceso**: estado dentro del embudo, fecha de envío, fecha de la primera contestación, día de
la entrevista, fecha de cierre, fecha de próximo seguimiento y el texto de la respuesta de la empresa. Al marcar el
estado como un cierre negativo —*Descartado por la empresa*, *Retirada por mí* u *Oferta rechazada por mí*—
aparece un desplegable **obligatorio** con el motivo, filtrado por estado: motivos de la empresa
(salario, experiencia insuficiente, otro candidato, ajuste cultural, puesto cancelado u otro) o
propios (aceptaste otra oferta, modalidad o ubicación, ya no te interesa, salario u otro). Si no lo
sabes, se preselecciona *No indicado*. Es el dato que con suficientes candidaturas mejor dice qué ajustar.
Si dejas el próximo seguimiento en blanco, al guardar se propone automáticamente una fecha 7 días
después del envío (o de la reapertura, si la hubo), para que el aviso de "seguimiento pendiente" no
dependa de que te acuerdes de rellenarlo a mano.

**El contacto**: nombre, email y teléfono del recruiter o la persona técnica.

**El historial**: una lista de hitos con fecha y tipo (llamada, prueba técnica, entrevista, oferta,
rechazo...). Esto es lo que hace que funcione cuando un proceso tiene tres entrevistas en vez de una:
el campo "día de la entrevista" te da la próxima de un vistazo y el historial guarda todas.

**Los adjuntos**: el CV y la carta de presentación concretos que enviaste a cada oferta. Al pulsar
*Adjuntar* se **copian** a `%APPDATA%\GestorSolicitudes\adjuntos` con un nombre interno único (no se
enlaza el original, así que sobreviven aunque muevas o borres el fichero de origen); el nombre que
ves en pantalla es el original tal como lo elegiste, no el nombre interno. Se borran junto con la
candidatura, o si cancelas una candidatura nueva sin llegar a guardarla. Al sustituir o quitar uno, el
fichero anterior no se borra hasta que guardas: si cancelas, la candidatura sigue teniendo su adjunto.

## Detalles que quizá no se vean a simple vista

- Las filas con **seguimiento vencido** (la fecha de próximo seguimiento ya pasó y el proceso sigue
  abierto) se pintan en ámbar, y hay un filtro para ver solo esas. Es el disparador para escribir
  el clásico "¿hay alguna novedad sobre el proceso?".
- La cabecera calcula **tasa de respuesta** y **media de días hasta la primera contestación**. Con
  treinta o cuarenta candidaturas esos dos números te dicen bastante sobre si el CV está filtrando
  bien o si estás disparando a ofertas equivocadas.
- La interfaz está en **español, inglés y alemán**, con un selector de banderas en la cabecera. El
  cambio es inmediato —textos, fechas y números incluidos— y no hace falta reiniciar. La preferencia
  se guarda en `%APPDATA%\GestorSolicitudes\idioma.txt`; en el primer arranque, si no hay
  preferencia, se detecta el idioma del sistema y todo lo que no sea inglés o alemán cae a español.
- Las candidaturas cerradas se muestran en gris, y el filtro *Solo abiertas* las esconde.
- **Duplicar** (en el panel de detalle de una candidatura ya guardada) crea una candidatura nueva con
  los mismos datos de la oferta —empresa, puesto, ubicación, tecnologías, salario, interés— pero sin
  fechas de proceso, contacto ni adjuntos. La copia queda enlazada a la candidatura de origen para
  rastrear reaplicaciones: pensado para volver a la misma empresa en otro puesto o a una oferta calcada.
- La columna **Días** de la tabla cuenta desde el envío mientras el proceso sigue abierto; en cuanto
  lo cierras se congela y muestra cuánto duró en total hasta la fecha de cierre. Si la candidatura
  se reabrió, cuenta desde la última reapertura y el tooltip muestra el total desde el envío.
- **Reabrir una candidatura cerrada**: al cambiar el estado de una cerrada a uno abierto, la app pide
  confirmación y una **nota obligatoria** (máximo 300 caracteres), limpia el cierre y el motivo,
  guarda la fecha de reapertura y añade un hito *Reapertura* con el cierre anterior y la nota. Respeta
  el próximo seguimiento que hayas editado en ese guardado (si no, propone +7 días). Desde *Oferta
  aceptada* la confirmación es reforzada, y no se puede reabrir a *Sin respuesta*. Las reabiertas
  llevan la insignia ↻ junto al estado, un borde magenta en la fila, una franja informativa en
  *Seguimiento* y un filtro *Reabiertas*.
- **Cambios sin guardar**: si cambias de fila, pulsas *Nueva* o *Duplicar* o cierras la ventana con
  el editor abierto, la app pregunta si quieres guardar, descartar o cancelar. Si eliges *Guardar*,
  la acción pedida sigue solo cuando el guardado termina bien.
- **Cambiar la fecha de envío** mueve con ella el hito *Solicitud enviada* y el seguimiento
  propuesto, pero solo si seguían en la fecha anterior; lo que hayas editado a mano no se toca. El
  seguimiento no se mueve si la candidatura se reabrió.
- **Salir de *Sin respuesta***: si el estado nuevo es una respuesta de la empresa (cualquier estado
  abierto salvo *Enviada* y *Sin respuesta*, y también *Descartado por la empresa*, *Oferta aceptada*
  y *Oferta rechazada por mí*), la app rellena con hoy la fecha de la primera contestación, si falta,
  y añade un hito *Respuesta*. *Retirada por mí* no cuenta, porque la decides tú.
- Al guardar una candidatura nueva a una empresa y puesto que ya existen, la app avisa del estado y
  el motivo de la anterior, sin impedir el guardado.
- La tabla muestra además **Seguimiento** (la próxima fecha de contacto) e **Interés** en estrellas,
  y todas sus columnas se pueden ordenar clicando la cabecera.
- El botón **Ver todas** junto al campo *Empresa* filtra la lista a todas las candidaturas de esa
  empresa, reutilizando la búsqueda de texto.
- El panel de detalle está en **tres pestañas** —*Oferta*, *Seguimiento* y *Adjuntos e historial*—
  para no tener que hacer scroll de quince bloques seguidos: *Empresa* y *Puesto* quedan siempre a la
  vista arriba y los botones de *Eliminar/Duplicar/Cancelar/Guardar* abajo. La pestaña activa se
  conserva mientras la app sigue abierta; al arrancar se abre en *Oferta*.
- **Exportar CSV** pregunta el separador (`;`, `,` o tabulador) y lo recuerda en
  `%APPDATA%\GestorSolicitudes\csv.txt`; escribe UTF-8 con BOM, así que Excel en español abre el
  fichero en columnas directamente sin el asistente de importación. Incluye el motivo de cierre
  (traducido) y la fecha de reapertura. Los textos que empiezan por `=`, `+`, `-`, `@`, tabulador o
  retorno de carro llevan un apóstrofo delante para que la hoja de cálculo no los trate como
  fórmulas. No incluye todos los campos: faltan `Requisitos`, `Origen`, `ContactoTelefono` y las
  rutas y nombres de los adjuntos.
- La fila de filtros lleva el **buscador** a la izquierda, con el texto *Buscar...* mientras está
  vacío, y el grupo *Columnas*, *Estados* y las dos casillas pegado al borde derecho. El botón
  *Columnas* abre una lista con casillas para mostrar u ocultar *Días*, *1ª respuesta*, *Entrevista*,
  *Seguimiento*, *Interés* y *Portal*; *Empresa*, *Puesto*, *Estado* y *Enviada* no se pueden
  ocultar. La lista se cierra al pulsar fuera y la elección se recuerda en
  `%APPDATA%\GestorSolicitudes\columnas.txt`.
- **La tabla está paginada**: por defecto enseña las diez primeras filas, con un desplegable para
  poner 5, 10, 20 o 30 por página y cuatro flechas —primera, anterior, siguiente y última— que se
  deshabilitan solas cuando no hay a dónde ir. Al lado, el rango (`1-10 de 47`). WPF no pagina las
  listas por sí solo, así que el recorte se hace en el ViewModel: por eso, si pinchas la cabecera
  de una columna para ordenar, el orden se aplica **solo a las filas de la página actual**, y la
  exportación a CSV sigue sacando la lista entera.
- **Icono en la bandeja del sistema**: cerrar la ventana (la X o Alt+F4) termina la aplicación del
  todo; el menú del icono permite reabrir la ventana, comprobar seguimientos y *Salir*. Mientras la
  app está abierta, cada 5 minutos (y a los pocos segundos de arrancar) revisa si hay seguimientos
  vencidos y avisa con un globo junto al reloj de Windows. Cada candidatura avisa una sola vez por
  sesión.
- **Gráfica de embudo** (botón *Gráfico* de la cabecera): enviadas → respondidas → entrevistas →
  ofertas de los últimos 12 periodos, contadas por el historial (hito "Solicitud enviada", primera
  contestación, hitos de entrevista y de oferta), así cada barra cuenta lo que pasó en ese periodo. El
  desplegable *Agrupar por* cambia la granularidad entre mes, semana (lunes a domingo) y día, y las
  flechas ◀ ▶ desplazan la ventana de doce periodos hacia atrás y hacia delante, con el botón *Hoy*
  para volver al periodo actual de un clic. El rango va de la primera solicitud a hoy: no se puede
  avanzar al futuro (no hay datos) ni retroceder más allá de la primera candidatura. Se refresca
  sola si guardas o borras candidaturas mientras está abierta.
- Atajos: `Ctrl+N` nueva candidatura, `Ctrl+S` guardar.

## Qué no hacer una vez (lección aprendida)

El icono de la bandeja usa `System.Drawing`, por eso el proyecto tiene `UseWindowsForms=true` además
de WPF. Para no arrastrar el infierno de ambigüedades (`Application`, `MessageBox`, `Color`...),
el `.csproj` quita los usings implícitos de WinForms y de `System.Drawing` con `<Using Remove=...>`.

## Estructura

Los ficheros `.cs` viven en la raíz del proyecto; **la capa la marca el namespace**, no la carpeta:
`GestorSolicitudes.Models`, `.Data`, `.Helpers`, `.Converters` → `.ViewModels` → `.Views`. Las carpetas
con contenido son `Views/` (las XAML), `Resources/` (el icono), `GestorSolicitudes.Tests/`, `02-DOCS/`
(revisiones y decisiones de diseño) y `.opencode/skills/` (instrucciones para los agentes).
Lo vigila ArchUnitNET: las capas solo dependen hacia abajo, así que un helper nuevo va en `.Helpers` y
un `using` a `Views` dentro de un ViewModel tumba el CI.

```
Solicitud.cs, Evento.cs, DatosOferta.cs, Adjunto.cs, Enumeraciones.cs → modelo, datos de oferta, enums con [Description] y el adjunto que pinta la vista
EstadoSolicitudExtensiones.cs → estados cerrados y motivos de cierre permitidos por estado
AppDbContext.cs → SQLite: EnsureCreated, DbSets y mapeo del modelo
EsquemaDb.cs → ALTER TABLE de las columnas nuevas en bases creadas por versiones antiguas
Traducciones.cs → diccionarios es/en/de y Texto (en Helpers, para que los helpers traduzcan)
Localizacion.cs → fachada del idioma: idioma activo, textos y cambio de idioma
CulturaApp.cs → cultura del hilo, lenguaje de WPF y recursos de texto al aplicar un idioma
PreferenciaIdioma.cs → idioma guardado en %APPDATA%\...\idioma.txt (o el del sistema)
PreferenciaCsv.cs → separador del CSV guardado en %APPDATA%\...\csv.txt
EnumHelper.cs → enum → texto traducido (cae a [Description] si falta la clave)
Convertidores.cs, ColoresEstado.cs → enum → texto, null → Visibility, estado → color, interés → estrellas

AdjuntosHelper.cs → copia un fichero suelto a %APPDATA%\...\adjuntos y borra solo lo que vive ahí
GestorAdjuntos.cs → ciclo de vida de los adjuntos de una edición (copias, sustituciones y descartes)
DocumentoHelper.cs → extrae el texto de DOCX/ODT para el visor de adjuntos
ColumnasHelper.cs → lee/escribe las columnas visibles en %APPDATA%\...\columnas.txt
Paginador.cs → recorte puro de una lista a la página actual (rango y flechas)
TextoHelper.cs → escapa los campos del CSV y normaliza el texto de la búsqueda (minúsculas sin tildes)
CsvExportador.cs → exportación CSV pura (cabecera + filas)
EmbudoHelper.cs, EmbudoPeriodo.cs → lógica del embudo (máximo, periodos, rangos, conteos)
EstadisticasSolicitud.cs → cabecera de estadísticas (total, abiertas, tasa, media)
Enlaces.cs → validación de los enlaces que se abren (solo http y https)

ColumnaItem.cs, ColumnasViewModel.cs → columna ocultable y su preferencia guardada (namespace ViewModels)
IDialogoService.cs → lo que el ViewModel le pide al usuario: avisos, confirmar, ficheros, enlaces
SolicitudEdicionViewModel.cs → búfer de la solicitud abierta: copia al abrir, valida y vuelca con AplicarA
EventoViewModel.cs → hito editable del búfer (fecha, tipo y descripción)
MainViewModel.cs → filtros, CRUD, métricas, embudo, adjuntos, historial, duplicar, exportación y columnas
                 (sin WPF: habla con IDialogoService)

DialogoService.cs, DialogosNulos.cs → implementación WPF y versión silenciosa de IDialogoService
App.xaml, App.xaml.cs → tema, estilos compartidos, arranque, cultura y preferencias
Views/MainWindow.xaml(.cs) → lista maestra + panel de detalle + gráfica + bandeja
Views/CabeceraMetricas.xaml(.cs) → cabecera de métricas
Views/PestanaOferta.xaml(.cs), PestanaSeguimiento.xaml(.cs), PestanaAdjuntos.xaml(.cs) → pestañas del detalle
Views/FilaAdjunto.xaml(.cs) → fila de un adjunto (CV o carta): etiqueta, nombre y botones
Views/VentanaRequisitos.xaml(.cs) → ventana de lectura de los requisitos
Views/VentanaSeparadorCsv.xaml(.cs) → ventana modal para elegir el separador del CSV
Views/VisorAdjuntoWindow.xaml(.cs) → visor de PDF/DOCX/ODT sin abrir el programa externo
Resources/appicon.ico → icono de la ventana, del ejecutable y de la bandeja
GestorSolicitudes.Tests/ → xUnit: modelo, helpers, localización, contexto SQLite, estilos de las
                           ventanas y lógica del ViewModel
02-DOCS/ → revisiones y decisiones de diseño
.opencode/skills/ → skills del repo: tests-obligatorios y sonarcloud-gate
```

## Decisiones que conviene conocer antes de tocarlo

**Un solo `DbContext` durante toda la sesión.** Es una app monousuario, así que se aprovecha el
change tracking de EF: el panel de detalle edita un búfer y *Guardar* lo vuelca sobre la entidad que
ya está en seguimiento, con un `SaveChanges()` limpio. *Cancelar* descarta el búfer, desengancha
todo el `ChangeTracker` y recarga de disco. Si algún día esto se convierte en multiusuario o
multiventana, el patrón correcto pasa a ser un contexto por operación con `IDbContextFactory`.

**La edición va por un búfer, no por la entidad.** `SolicitudEdicionViewModel` (`Editor`) copia los
datos al abrir y los valida mientras se escriben. La entidad no cambia hasta *Guardar*; *Cancelar*
descarta el búfer. La validación se realiza antes de aplicar los datos al modelo. Las filas de la tabla
siguen siendo entidades POCO de solo lectura y la lista se recarga después de cada guardado.

El historial sigue el mismo camino: los hitos editables viven como `EventoViewModel` en
`SolicitudEdicionViewModel.EventosEdicion`. Se emparejan con su entidad mediante `Original`, nunca
por posición ni por `Id`; al guardar se crean los nuevos, se actualizan los existentes y se eliminan
los quitados. Las bajas se conservan en el búfer hasta guardar, así que cancelar no las aplica.
`Solicitud.Eventos` es un `IReadOnlyList<Evento>` sobre un campo privado y se modifica mediante
`AgregarEvento` y `QuitarEvento`. `Evento` se crea con constructor y se actualiza con `Actualizar`;
EF accede a sus propiedades por campo (`UsePropertyAccessMode(Field)`).

**El modelo está encapsulado.** `Solicitud` se crea con empresa y puesto; su constructor sin
parámetros es privado para EF. Los setters de sus propiedades son privados y los cambios pasan por
métodos de negocio (`ActualizarOferta`, `ActualizarCondiciones`, `CambiarEstado`, entre otros), que
aplican las validaciones y longitudes admitidas. `Solicitud.CrearCopia()` centraliza la duplicación de
la parte reutilizable de una oferta. El plan completo y el estado de cada etapa están en
[`02-DOCS/plan-refactor-edicion.md`](02-DOCS/plan-refactor-edicion.md).

**Estados y reapertura.** Una candidatura cerrada solo vuelve a abrirse con `Solicitud.Reabrir`,
nunca por el combo de estado. `CambiarEstado` rechaza el paso de cerrado a abierto, exige un motivo
válido en los tres cierres negativos, lo limpia en el resto y fija hoy como fecha de cierre si
falta. No se añaden estados nuevos: «Reabierta» es una acción, no un estado. Desde cualquier estado
abierto se puede pasar a *Oferta aceptada* u *Oferta rechazada por mí*; una oferta ya cerrada se
acepta reabriéndola antes a *Oferta recibida*. Las tablas de transiciones y las decisiones están en
[`02-DOCS/decisiones/estados-y-reapertura.md`](02-DOCS/decisiones/estados-y-reapertura.md).

**El motivo de cierre sigue llamándose `MotivoRechazo`.** El enum y la propiedad no se renombraron
aunque ahora sirvan también a *Retirada por mí* y *Oferta rechazada por mí*; cambiarlo a
`MotivoCierre` queda para otra refactorización.

**`RespuestaEmpresa` y `Notas` son texto libre sin límite.** El resto de campos de texto tiene una
constante `LongitudMaximaX` que comparten `[MaxLength]` y la validación del modelo.

**Los hitos que crea el modelo están en castellano.** `Reapertura`, `Respuesta` y `Rechazo` guardan
su descripción en castellano aunque la interfaz esté en inglés o alemán: el modelo no puede usar
`Localizacion` (lo prohíbe la regla de capas), así que solo se traduce el tipo del hito. El prefijo
del hito de reapertura es una constante compartida con la franja de *Seguimiento*. Queda pendiente
otra refactorización: guardar valores únicos y traducirlos al mostrarlos.

**Ningún modelo expone setters públicos.** Un test por reflexión lo vigila; `Solicitud` y `Evento`
solo cambian mediante métodos de negocio y EF escribe por campo.

**`EnsureCreated()` en lugar de migraciones.** Va bien para empezar, pero no versiona el esquema:
si añades una propiedad nueva al modelo, la tabla existente no se actualiza sola. Cuando quieras
evolucionarlo sin borrar datos:

```bash
dotnet tool install --global dotnet-ef
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet ef migrations add Inicial
dotnet ef database update
```

y cambia `EnsureCreated()` por `db.Database.Migrate()` en `App.xaml.cs`.

**Los diálogos pasan por `IDialogoService`.** El ViewModel no toca WPF: pide avisos,
confirmaciones, elegir fichero y abrir enlaces a la interfaz (`IDialogoService`, en `ViewModels`),
que implementa `DialogoService` en `.Views` con `MessageBox` y los diálogos de WPF. Los tests
inyectan un falso y así se prueban comandos que antes quedaban fuera por
`[ExcludeFromCodeCoverage]` (`Guardar` inválido, `Eliminar`, `ExportarCsv`, `Adjuntar`,
`AbrirEnlace`). Un test de fuentes vigila que ningún ViewModel vuelva a llamar a `MessageBox`,
`OpenFileDialog`, `SaveFileDialog` ni `Process.Start`.

**Estilo guiado por StyleCop.** El analizador `StyleCop.Analyzers` corre sobre la app con un juego
mínimo de reglas activas (ver `.editorconfig` y `stylecop.json`): nada de documentación XML
obligatoria ni cabeceras de copyright. Además están activos `IDE0005` (usings sobrantes), `IDE0161`
(namespace de fichero) y `SA1101` (`this.`), y `Directory.Build.props` trata los avisos como errores.
`dotnet format` deja el código formateado y `dotnet format --verify-no-changes` verifica que no haya
nada pendiente.

## Ideas para seguir

- Recordatorios incluso cuando la app está cerrada del todo (arrancar un proceso en segundo plano con Windows)
- Migrar a Avalonia si algún día quieres ejecutarlo también en macOS o Linux
