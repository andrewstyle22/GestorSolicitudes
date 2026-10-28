# Revisión de clean code (segunda pasada)

Alcance: código de producción (`*.cs`, `*.xaml`, `*.csproj`), suite de tests y documentación
(`README.md`, `AGENTS.md`). No se ha modificado ningún fichero de producción: esto es solo el
diagnóstico y la lista de cambios. Criterios: responsabilidad única, DRY y fuente única de verdad,
nombres, código muerto, manejo de errores y estado, testabilidad, puertas de calidad y
documentación coherente con el código.

> **Verificación:** en este entorno no se ha podido ejecutar `dotnet build` ni `dotnet test`.
> `dotnet --version` responde (10.0.402), pero `dotnet build --no-restore` falla al invocar a
> Roslyn con `MSB3883: Acceso denegado`; `dotnet test` falla con el mismo bloqueo. Por tanto, los
> hallazgos son de lectura de código y de historial de git, y no hay evidencia de que la suite
> siga verde. Las comprobaciones que solo puede hacer el compilador o el analizador quedan
> pendientes de ejecutar en un entorno sin sandbox.

## Veredicto rápido

| Dimensión | Estado | Evidencia |
|-----------|--------|-----------|
| Responsabilidad única | Rojo | `MainViewModel` 1075 líneas; `Localizacion` 984 |
| DRY / fuente única | Rojo | CSV y periodos duplicados; nueve estilos de celda; colores repetidos |
| Código muerto | Rojo | `CsvExportador` y la copia de `CalcularPeriodos` sin llamadas; rama imposible en `Guardar`; usings y `x:Name` sin uso |
| Nombres | Amarillo | `CsvHelper` hace normalización de texto; abreviaturas (`cd`, `sb`, `Con25`); `Buscar_empresas` |
| Errores y estado | Amarillo | `catch (Exception)` en los comandos que hacen E/S; métodos que devuelven `Exception?`; `DialogosNulos` silencioso; selección obsoleta al eliminar |
| Tests | Amarillo | Buena red, pero el test de paridad no comprueba paridad; `ViewModelTests` 1229 líneas; ViewModels no se liberan |
| Puertas de calidad | Rojo | Sin `Directory.Build.props`; `.editorconfig` solo desactiva reglas; sin umbral de cobertura |
| Documentación | Amarillo | Bloque de estructura del README roto; afirmaciones que el código no cumple |

## Resumen: los cinco puntos que más pesan

1. **La extracción de los helpers de la primera revisión (A2 y A4) se quedó a medias.**
   `EmbudoHelper.CalcularPeriodos` y `CsvExportador.Construir` existen y tienen tests, pero el
   código de producción sigue usando copias dentro de `MainViewModel` (`CalcularPeriodos` en
   563-583 y `ConstruirCsv` en 1018-1074). `CsvExportador` no tiene ninguna llamada de producción:
   solo lo usan sus tests. El ViewModel bajó de 1118 a 1075 líneas, pero unas 90 de las que quedan
   son código muerto o duplicado; si se eliminan, se queda en unas 985. Es el patrón de «añadir el
   helper y no borrar lo viejo», y es peor que no haber extraído: ahora hay dos fuentes de verdad y
   los tests protegen la que no se ejecuta.
2. **Queda código muerto de la extracción A1 de la primera revisión.** En `Guardar` se llama a
   `ReglasSolicitud.AnadirHitoInicial` (791) y justo después se comprueba `Eventos.Count == 0`
   (793-801). Esa condición ya no puede ser cierta: el helper añade el hito precisamente cuando la
   colección está vacía. El bloque es inalcanzable y duplica el literal del evento.
3. **El test de paridad de idiomas no comprueba la paridad.** `Localizacion.Texto(Idioma, clave)`
   cae al castellano cuando falta una clave (807-815), así que
   `TodosLosIdiomasCubrenLasMismasClaves` (LocalizacionTests 55-70) pasa aunque a alemán o inglés
   les falten claves. Hay casos reales: la pestaña alemana de seguimiento dice «Follow-up»
   (Localizacion 608) y `TituloGrafica` pasa a minúsculas el nombre de la granularidad
   (MainViewModel 442-445), produciendo «nach monat» en alemán.
4. **`MainViewModel` sigue siendo la clase Dios.** 1075 líneas y ocho responsabilidades
   (paginación, filtros, estadísticas, embudo, adjuntos, CRUD, CSV, columnas). A5 desacopló WPF,
   que era lo urgente, pero no dividió la clase. `Localizacion` sigue igual: cinco
   responsabilidades en 984 líneas.
5. **La deuda de UI y de puertas de calidad no se ha tocado.** Nueve estilos de celda idénticos,
   dos plantillas de botón copiadas, los bloques de CV y carta desincronizados, colores
   hexadecimales repetidos, `x:Name` que nadie lee, ningún `Directory.Build.props` y un
   `.editorconfig` que solo desactiva reglas.

## Lo que ya está cerrado desde `revision-clean-code.md`

No hace falta repetirlo como tarea; se deja constancia para que la segunda pasada no lo vuelva a
pedir:

- **A5, servicio de diálogos:** `IDialogoService`, `DialogoService`, `DialogosNulos`,
  `DialogoFalso` y el test de fuentes. Los comandos de `MainViewModel` ya se prueban sin abrir
  ventanas; no queda ningún `MessageBox` en el ViewModel.
- **A1, reglas de `Guardar`:** `ReglasSolicitud` con validación, seguimiento propuesto e hito
  inicial. Falta borrar el bloque viejo (A3 de esta revisión).
- **A2/A3/A4 de la primera revisión, helpers:** `EmbudoHelper`, `EstadisticasSolicitud` y
  `CsvExportador` existen y están probados. Los helpers de A2 y A4 están a medio cablear (ver A1 y
  A2 de esta revisión).
- **C1-C4:** `Eliminar` y `ExportarCsv` capturan fallos y avisan; los `catch` silenciosos de
  `AdjuntosHelper`, `ColumnasHelper`, `Localizacion` y el visor dejan traza en `Debug`;
  `MainViewModel` implementa `IDisposable` y se desengancha del evento de idioma;
  `Cancelar` usa `ChangeTracker.Clear()`.
- **D1/D2/D4/D8:** `ColumnasHelper` expone `CargarDe`/`GuardarEn`, el parámetro «invertir» y el
  `Cursor=""` desaparecieron, y el test del constructor por defecto ya no abre la base real.
- **E9/E11/F4/F6/F7:** el test tautológico de estadísticas se eliminó; el test de `HeaderStyle`
  pasó a ser estructural (`FuentesTests`); el formato se verifica antes de los tests en el CI;
  hay tests de claves usadas y de columnas ocultables.
- **D9 parcial:** `TestDb` limpia la raíz temporal al empezar, así que la basura no se acumula
  entre ejecuciones.

## A. Terminar lo que se empezó (hacer ya)

Las secciones A-E agrupan por tema, no por persona; dentro de cada tabla el orden es temático y
los elementos marcados **Crítico** son los que dejan el código en un estado engañoso (código
muerto que parece vivo, tests que no prueban lo que dicen, o una fuga real). El orden recomendado
del final los distribuye por fases.

| # | Dónde | Cambio | Por qué |
|---|-------|--------|---------|
| A1 | `MainViewModel.cs:1018-1074`, `:1005-1016`, `:21`, `:999-1001`; `CsvExportador.cs:15-72`, `:12` | **Crítico.** Borrar `ConstruirCsv` y `FormatoFechaCsv` del ViewModel. En `EscribirCsv`, llamar a `CsvExportador.Construir(db.Solicitudes.OrderByDescending(...).ThenByDescending(...).ToList())` y mantener solo la escritura con BOM. Dentro del helper, unificar cabecera y fila en una lista de descriptores `(clave, valor)` para que no puedan desalinearse. Añadir un test de `EscribirCsv` que compruebe una fila completa, no solo la cabecera. Además, `ExportarCsv` debe informar del número de filas escritas, no de `TotalSolicitudes` (999-1001), que puede estar desfasado. | El helper no se ejecuta nunca en producción; sus tests dan falsa cobertura; hay dos listas de 22 columnas que se pueden desalinear por separado. El mensaje de éxito puede mentir si la base cambió desde la última recarga. |
| A2 | `MainViewModel.cs:557`, `:563-583`; `EmbudoHelper.cs:13`, `:60-80` | **Crítico.** Borrar `PeriodosDelEmbudo` y `CalcularPeriodos` del ViewModel. `PuedeRetrocederUnPeriodoMas` ya usa `EmbudoHelper.CalcularPeriodos` (438); no queda ningún llamador. | El método es una copia literal del helper, sin llamadas; A1b lo dejó muerto al pasar `PuedeRetrocederUnPeriodoMas` al helper. La extracción quitó cuatro métodos pero dejó este. |
| A3 | `MainViewModel.cs:791-801` | **Crítico.** Borrar el bloque `if (this.Edicion.Eventos.Count == 0) { ... }`. Dejar `AnadirHitoInicial` como único sitio que crea el hito. Añadir una aserción de que se guarda exactamente un `SolicitudEnviada`. | La condición es imposible después del helper; el literal del evento está duplicado y puede divergir. |
| A4 | `MainViewModel.cs:782-783`; `ReglasSolicitud.cs:22-25` | Sustituir `... ?? this.Edicion.ProximoSeguimiento` por `if (ReglasSolicitud.ProponerSeguimiento(this.Edicion) is DateTime propuesta) { this.Edicion.ProximoSeguimiento = propuesta; }`. | El `??` no puede activarse nunca: el helper devuelve `null` justo cuando ya hay fecha o cuando el proceso está cerrado, así que no hay ningún fallback real que aplicar. Hoy el lector cree que sí. |
| A5 | `LocalizacionTests.cs:55-70`; `Localizacion.cs:807-815`, `:828`; `AGENTS.md:30` | **Crítico.** Añadir `Tiene(Idioma, clave)` o `Claves(Idioma)` y un test que compare los conjuntos de claves de los tres idiomas y que ninguna esté vacía. Mantener la frase de `AGENTS.md` solo cuando el test compruebe la paridad de verdad. | `Texto(idioma, clave)` cae al castellano, así que el test actual pasa con traducciones ausentes. Es el fallo de test más importante del proyecto. |
| A6 | `MainViewModel.cs:442-445`; `Localizacion.cs:57`, `:305`, `:553`, `:608` | Quitar el `ToLower` y usar una clave de título por granularidad (`Grafica.TituloMes`, `TituloSemana`, `TituloDia`) o una frase completa traducida. Corregir la pestaña alemana «Follow-up» y revisar si quedan anglicismos en alemán. | En alemán los sustantivos van en mayúscula y el régimen correcto es «nach Monaten/Wochen/Tagen»; «nach monat» es incorrecto. El test de paridad no lo detecta. |
| A7 | `MainViewModel.cs:5`; 14 ficheros de `GestorSolicitudes.Tests` | Quitar el `using System.Globalization;` (ya no se usa) y los `using Xunit;` redundantes con el `<Using Include="Xunit" />` del csproj. Activar `IDE0005` para que no vuelvan. | El `System.Globalization` es un residuo de A2; los `using Xunit` son ruido de compilación. El formateador no los está detectando. |
| A8 | `MainWindow.xaml:337,422,469,485,500,519,644,652,656,662,668,674,690` | Quitar los `x:Name` que nadie lee: `TablaCandidaturas`, `SelectorEstados`, `Empresa`, `Puesto`, `Estado`, `Enviadas`, `SelectorFilasPagina`, `RangoTexto`, los cuatro botones de paginación y `NuevaCandidatura`. Conservar los seis de columnas ocultables, `Buscar_empresas` (lo usa un `ElementName`) y `BotonGrafica`. Renombrar `Buscar_empresas` a `BuscarEmpresas`. | Son API muerta; los nombres sugieren un acceso desde code-behind que no existe y `FuentesTests` solo necesita los seis ocultables. |
| A9 | `MainWindow.xaml.cs:64-73` (`_ => null` en 72); `MainViewModel.cs:135-136`; `FuentesTests.cs:30-41` | **Crítico.** Hacer que el `switch` falle de forma ruidosa con una columna desconocida (`throw new InvalidOperationException(...)`) o construir un diccionario una vez. Añadir un test que cruce `ColumnasOcultables` con el mapa del code-behind. | El test actual comprueba que el `x:Name` y la clave existen, pero no que el `switch` tenga el caso: una columna nueva se ignoraría en silencio y su casilla no haría nada. |
| A10 | `ReglasSolicitud.cs:12`; `Localizacion.cs:67`; `EstadisticasSolicitud.cs:6`; `README.md:152-177`, `:107`, `:108` | Corregir «empuesto» por «empresa y puesto», la sangría de `Grafica.Ver`, partir el `record` largo y rehacer el bloque de estructura del README. Aclarar las afirmaciones de CSV y de la pestaña inicial (ver E6). | Es barato y el README quedó roto después de los refactors: `EnumHelper` aparece dos veces y la descripción de `CsvHelper` está partida por `EmbudoHelper`. |
| A11 | `Localizacion.cs:863`; `EnumHelper.cs:30` | Bajar a `private`/`internal` los miembros que solo se usan dentro: `Localizacion.AplicarRecursosDeApplication` (llamado desde `Inicializar` y `Cambiar`) y `EnumHelper.Clave` (llamado desde `Descripcion`). Si un test necesita alguno, usar `InternalsVisibleTo`. | API pública que no tiene consumidores externos; amplía la superficie sin motivo y hace creer que es un punto de extensión. |

## B. Responsabilidad única y estado

| # | Dónde | Cambio | Por qué |
|---|-------|--------|---------|
| B1 | `MainViewModel.cs:212-295` | Extraer un `Paginador`/`PaginaActual` que encapsule tamaño, página, total, rango y habilitación de flechas; el ViewModel delega y expone el resultado. Probar el paginador puro y mantener los tests de integración actuales. | La paginación es un concepto con estado propio; hoy son ocho propiedades, cuatro comandos y `Paginar` mezclados con el resto. Es la extracción con mejor relación coste/beneficio que queda. |
| B2 | `MainViewModel.cs:597-693` | Sustituir los pares de lambdas de CV y carta por un descriptor (`TituloClave`, `Etiqueta`, `ObtenerRuta`, `Asignar`) y un único método de adjuntar/quitar. | Cuatro comandos casi idénticos; con un descriptor, CV y carta comparten una sola ruta de código y se prueba una vez. |
| B3 | `EstadisticasSolicitud.cs:6`, `:25-26`; `EstadisticasSolicitudTests.cs:71-72` | Devolver números (`double? TasaRespuesta`, `double? MediaDiasRespuesta`) y formatear en el ViewModel o en un converter. Probar porcentajes y medias, no cadenas localizadas. | Un cálculo «puro» que depende de `Localizacion.CulturaActual` y devuelve «3 días» mezcla cálculo y presentación; los tests se acoplan al texto. |
| B4 | `Localizacion.cs` (984 líneas) | Dividir en `Traducciones` (diccionarios y fallback), `CulturaApp` (cultura del hilo, lenguaje de ventanas), `PreferenciaIdioma` (lectura/escritura de `idioma.txt`) y una fachada `Localizacion` que conserve la API pública. | Hoy una clase hace cinco cosas: tabla de textos, cultura, recursos WPF, fichero de preferencia y evento global. Es el mayor fichero `.cs` después del ViewModel y el que más cuesta navegar. |
| B5 | `App.xaml.cs:49-101` | Mover `VerificarColumnasFaltantes` a `EsquemaDb.cs` (namespace `Data`, en la raíz como el resto de ficheros) y actualizar `AGENTS.md:25` y `VerificarColumnasTests`. | Es infraestructura de datos, no arranque de WPF; así se prueba sin instanciar `App` y el arranque queda como orquestación. |
| B6 | `Solicitud.cs:54,109,119,138`; `Evento.cs:16`; `MainViewModel.cs:588,701,733,957,984`; `EmbudoHelper.cs:65`, `:99-109` | Inyectar un reloj (`IClock`/`Func<DateTime>`) o centralizar `DateTime.Today` en un único `Reloj.Hoy`. Empezar por el ViewModel y los helpers; el modelo puede recibir la fecha como parámetro en sus cálculos. Pasar también la cultura a `EmbudoHelper.EtiquetaDePeriodo` o mover la etiqueta al ViewModel. | Los tests dependen del día real y del cambio de medianoche; el embudo y las fechas de «hoy» se calculan con el reloj del sistema en sitios distintos, y una función de fechas depende además de la cultura global. |
| B7 | `MainViewModel.cs:880-914` | Limpiar también `SolicitudSeleccionada` en `Eliminar`, como ya hacen `Guardar` (836) y `Cancelar` (876). Añadir la aserción al test de eliminación. | Hoy el ViewModel conserva una entidad borrada como seleccionada; en la vista el `DataGrid` puede propagar el `null`, pero el código no lo garantiza. |
| B8 | `MainViewModel.cs:205` y `:306` | Evitar la doble recarga al cambiar de idioma: asignar `EstadoFiltroItem` ya dispara `OnEstadoFiltroItemChanged` → `Recargar()`; la llamada explícita de la línea 205 sobra (o hay que silenciar el trigger mientras se reasigna). | Cada cambio de idioma hace dos recargas completas, y cada una lanza su consulta de lista y su consulta de estadísticas. |
| B9 | `MainViewModel.cs:89-95`, `:180-207` | Unificar `IdiomaActual` con `Localizacion.IdiomaActual`: una propiedad calculada con setter que llame a `Localizacion.Cambiar`, y notificar en `CuandoCambiaIdioma`. No ignorar el argumento `idioma`. Cuando se pueda probar sin tocar el estado global, quitar el `[ExcludeFromCodeCoverage]` de `CuandoCambiaIdioma`. | Hay dos fuentes de verdad; si algo llama a `Localizacion.Cambiar` fuera del ViewModel, el `ComboBox` se queda con el valor viejo. Además, el argumento del evento no se usa y el método con más lógica de refresco queda fuera de cobertura. |
| B10 | `MainViewModel.cs:40` | Hacer obligatorio `IDialogoService` en el constructor (los tests pasan `DialogoFalso` o `DialogosNulos`) o, como mínimo, dejar traza cuando se use `DialogosNulos`. | El valor por defecto silencia todos los avisos si alguien olvida inyectar el servicio: un fallback que esconde el invariante. |
| B11 | `MainWindow.xaml.cs:35`; `MainViewModel.cs:58` | Definir quién posee el `AppDbContext`: si lo crea la ventana, que lo libere en `OnClosed`; si lo posee el ViewModel, que lo libere en `Dispose`. Documentarlo en el constructor. | La ventana crea un contexto y no lo libera; el ViewModel implementa `IDisposable` pero solo suelta el evento. |
| B12 | `MainViewModel.cs:686-691` | Documentar que `RutasGuardadas` exige una entidad en seguimiento, o guardar las rutas originales al abrir la edición en vez de leer `OriginalValue`. | Si `Edicion` está `Detached`, `OriginalValue` ya no representa lo que hay en disco; el método asume seguimiento y hoy funciona porque `Edicion` siempre viene de una consulta, pero el invariante no está escrito. |
| B13 | `MainViewModel.cs:765-838`, `:840-878` | Partir `Guardar` en `Validar`, `PrepararNueva`, `Persistir`, `LimpiarAdjuntosSustituidos` y `CerrarEdicion`; compartir con `Cancelar`/`Eliminar` la limpieza de adjuntos. | Son 74 líneas con cinco responsabilidades y tres bloques de limpieza de adjuntos casi iguales. Alarga el método y hace difícil ver qué pasa si falla el guardado. |
| B14 | `MainViewModel.cs:511-522` | Decidir si la cabecera mide el total o la lista filtrada. Hoy consulta `db.Solicitudes.ToList()` (global) mientras la tabla muestra el filtro; si es intencional, documentarlo y probarlo; si no, pasar `lista` a `Calcular`. | El usuario ve métricas que no cuadran con la tabla filtrada y no hay test que fije la semántica. |
| B15 | `MainViewModel.cs:917-929`, `:1005-1016` | Opcional: sustituir `Exception?` como valor de retorno por `bool Try... (out string error)` o dejar que la excepción suba y capturarla en el comando. | Es un patrón poco habitual; los tests comprueban `is Exception` y el lector tiene que recordar que un `null` significa éxito. |

## C. Duplicación, rendimiento y XAML

| # | Dónde | Cambio | Por qué |
|---|-------|--------|---------|
| C1 | `MainViewModel.cs:436-439` | Convertir `PuedeRetrocederUnPeriodoMas` en un método `ActualizarLimitesEmbudo()` que se llame al cambiar granularidad, desplazamiento o recargar, y guardar el booleano. | Hoy es un getter con una consulta `Min` a la base y un cálculo de periodos; se evalúa desde triggers y comandos, a veces dos veces por clic. |
| C2 | `MainViewModel.cs:471` | Quitar `Include(s => s.Eventos)` de `Recargar` y cargar el historial al seleccionar la fila (`Entry(edicion).Collection(s => s.Eventos).Load()`). | Se traen los eventos de todas las filas en cada pulsación del buscador, aunque la tabla no los usa; solo el panel de detalle los necesita. |
| C3 | `MainViewModel.cs:469-509` | Decisión: paginar en SQL (`Skip/Take/Count`) o aceptar y documentar el recorte en memoria. Para SQL hace falta una columna de búsqueda normalizada, registrada en `VerificarColumnasFaltantes`, porque SQLite no ignora tildes. | Hoy cada tecla materializa la tabla entera y la recorta en memoria. Es el trade-off documentado en `paginacion-tabla.md`; conviene decidirlo explícitamente antes de que la tabla crezca. |
| C4 | `MainViewModel.cs:487-493` | Precalcular una sola cadena normalizada por fila (empresa + puesto + tecnologías + ubicación + portal) y buscar una vez, en lugar de cinco `NormalizarBusqueda` por fila y pulsación. | Trabajo repetido en el camino caliente del filtro; con el `Delay=250` aún se nota al escribir rápido. |
| C5 | `MainViewModel.cs:513` | Evitar el `ToList()` completo de estadísticas: proyectar solo las columnas que necesita el cálculo o hacer los conteos en SQL. | `Recargar` ya hace una consulta de lista y `ActualizarEstadisticas` hace otra completa, sin `Include`, solo para contar. |
| C6 | `MainViewModel.cs:586-594` | Devolver una proyección (`Id`, `Empresa`, `Puesto`, `ProximoSeguimiento`) en lugar de entidades completas, filtrar los estados cerrados en SQL y usar `AsNoTracking`. | El recordatorio de la bandeja solo necesita cuatro campos; hoy arrastra entidades y filtra `EstaAbierta` en memoria cada cinco minutos. |
| C7 | `Convertidores.cs:44-68` | Usar pinceles estáticos congelados (`SolidColorBrush` + `Freeze`) o recursos; sacar los hexadecimales a la paleta. | Se crea un `SolidColorBrush` por celda y por repintado, y los colores están duplicados con `App.xaml` y la leyenda de la gráfica. |
| C8 | `EnumHelper.cs:15-27` | Cachear la descripción por `(Idioma, Type, valor)` o limpiar la caché en `Localizacion.Cambiar`; no cachear solo por valor. | `GetField`/`GetCustomAttribute` por reflexión se ejecutan en cada celda y cada combo. La caché debe invalidarse al cambiar de idioma. |
| C9 | `VisorAdjuntoWindow.xaml.cs:27`, `:61-84` | Renderizar las páginas del PDF bajo demanda (o con un botón «cargar más»), congelar los `BitmapImage`, liberar el stream de `AsStreamForRead` y cancelar la carga al cerrar. | Un PDF de 200 páginas se carga entero como imágenes; el `Loaded += async` no se espera ni se cancela y puede tocar controles cerrados. |
| C10 | `MainWindow.xaml.cs:103` | Sacar el efecto secundario del `Where` a un bucle explícito y podar `idsVencidosAvisados` al recargar o borrar. | `idsVencidosAvisados.Add` dentro del predicado funciona porque se materializa con `ToList`, pero es frágil; el `HashSet` crece sin límite. |
| C11 | `MainWindow.xaml.cs:90`, `:93`, `:204-211` | Constantes para 20 s y 5 min; en `OnClosed`, parar el temporizador, liberar `flujoIcono` y el icono de la bandeja, y disponer el contexto (B11). | Números mágicos y recursos GDI/stream que no se cierran. |
| C12 | `DialogoService.cs:50-51` | Validar con `Uri.TryCreate` que el enlace es `http` o `https` antes de `Process.Start`. Mover la validación a un helper puro para poder probarla. | Hoy se puede lanzar un `file:` o un protocolo registrado desde un campo de texto; el riesgo es local, pero la comprobación es barata. |
| C13 | `AppDbContext.cs:43`; `App.xaml.cs:59` | Construir la cadena con `SqliteConnectionStringBuilder` en vez de interpolar `Data Source={ruta}`. | Una ruta con `;` o comillas rompe la cadena o inyecta opciones; el builder lo evita. |
| C14 | `AdjuntosHelper.cs:41-43` | Canonicalizar con `Path.GetFullPath` y comparar la carpeta completa, no solo el prefijo. | La comprobación actual acepta `carpeta\..\otro`; los adjuntos se guardan con GUID, pero la defensa debe ser correcta. |
| C15 | `MainWindow.xaml:475-482,491-497,526-531,545-550,560-565,575-580,590-595,605-611,620-626` | Un `EstiloCelda` común (`VerticalAlignment`, `Margin`, `TextTrimming`) y dos derivados con `BasedOn` para Empresa (`FontWeight`) e Interés (`Foreground`). | Nueve copias del mismo `Style`; un ajuste de alineación exige tocar nueve bloques. |
| C16 | `App.xaml:76-105`, `:128-160` | Extraer el `ControlTemplate` del botón a un recurso compartido (`BotonPlano`, `TargetType="ButtonBase"`) y referenciarlo desde `BotonBase` y `BotonDesplegable`; mover el trigger de `IsChecked` al estilo del `ToggleButton`. **No** usar `BasedOn` entre ambos estilos: `Button` y `ToggleButton` son hermanos, no padre/hijo. | La plantilla está copiada palabra por palabra. La recomendación de la primera revisión («derivar de `BotonBase`») no era viable tal cual; el recurso compartido con un `TargetType` común sí, moviendo el trigger específico al estilo. |
| C17 | `MainWindow.xaml:944-1001` | Extraer el bloque de CV y el de carta a un `UserControl` o `DataTemplate` parametrizado por etiqueta, nombre, ruta, comandos y apertura. Ahora el de carta tiene siete columnas con `32*`, `85*`, `167*` y `ColumnSpan` cruzados (985, 989). | Dos bloques casi iguales que ya se han desincronizado; es la deuda de XAML más visible. |
| C18 | `MainWindow.xaml:237-250`, `:281-310`; `Convertidores.cs:44-68` | Pinceles con nombre en `App.xaml` (`Grafica.Enviadas`, `Respondidas`, `Entrevistas`, `Ofertas`) y reutilizarlos en la leyenda, las barras y el converter. | Los mismos hexadecimales están en tres sitios; cambiar la paleta obliga a buscar cadenas `#`. |
| C19 | `MainWindow.xaml:9`, `:328-331`, `:692`, `:725` | Opcional: sacar `Width="1525"`, `Width="440"` y `Height="355"` a recursos o usar `*`/`Auto`; valorar el `394*`/`75*` del cuerpo. | Medidas mágicas que no se adaptan a pantallas pequeñas y que obligan a tocar el XAML para cambiar el layout. |
| C20 | `MainWindow.xaml:7`; `MainWindow.xaml.cs:28-29` | Unificar el icono: usar la misma ruta construida desde el ensamblado (o fijar `Icon` en code-behind) y eliminar el `Icon="/GestorSolicitudes;component/..."`. | El XAML vuelve a codificar el nombre del ensamblado que el code-behind evita a propósito; al renombrar el proyecto, una de las dos rutas se rompe. |
| C21 | `MainViewModel.cs:615-647`, `:823` | **Crítico.** Resolver el `shortcut` confesado en 823: si se sustituye el mismo adjunto dos veces antes de guardar, el fichero intermedio queda huérfano. Llevar una lista de las rutas copiadas durante la edición y borrar todas menos la final al guardar, o borrar la copia anterior al reemplazarla si no es la de la base. Quitar el comentario `shortcut:` o enlazarlo a un issue. | Es una fuga de disco real y un comentario que documenta deuda sin dueño; el flujo de adjuntos es justo el que más pruebas tiene y este caso no está cubierto. |
| C22 | `Views/VisorAdjuntoWindow.xaml:4`; `Views/VisorAdjuntoWindow.xaml.cs:105-134` | Poner un `Title` localizado o vacío (el constructor ya lo sustituye por el nombre del fichero), usar los estilos y la paleta compartidos como el resto de ventanas y reutilizar `IDialogoService.ElegirDestino`/`Error` en vez de otro `SaveFileDialog`/`MessageBox`. | Es la única ventana que no sigue el tema y duplica el diálogo de guardado; el `Title="Visor"` de respaldo incumple la convención de textos localizados aunque el constructor lo sustituya. |

## D. Tests

| # | Dónde | Cambio | Por qué |
|---|-------|--------|---------|
| D1 | `ViewModelTests.cs` (1229 líneas) | Partir por asunto (`CrudTests`, `FiltrosTests`, `PaginacionTests`, `EmbudoTests`, `CsvTests`, `DialogosTests`) y añadir helpers `NuevaSolicitud(...)`, `GuardarSolicitud(...)` y `NuevoViewModel(...)` que devuelvan un `IDisposable` o usen `using`. | Es el fichero más largo del proyecto; el Arrange se repite en casi todos los tests y los ViewModels no se liberan del evento estático. |
| D2 | `PestanasTests.cs:21-99` | Renombrar a `EstilosAppTests` y extraer métodos por asunto. Si se separa el icono, mantenerlo en el mismo `[StaFact]` o en una fixture que garantice que existe `Application`, porque solo puede haber una por AppDomain. Envolver la creación de `App` en `try/finally { app.Shutdown(); }`. | El nombre dice «pestañas» pero prueba pestañas, botón, cabecera e icono; 17 asserts en 100 líneas y una `Application` que queda viva. |
| D3 | `ConvertidorTests.cs:66-118` | Quedarse con el test de tabla completa (`EstadoAColor_CadaEstadoTieneSuColor`) y el caso nulo; borrar `EstadoAColor_PintaCadaEstadoConSuColor`, que es un subconjunto. | Dos tests comprueban lo mismo con distinto detalle; cambiar un color obliga a actualizar dos bloques. |
| D4 | `VerificarColumnasTests.cs:19-28`, `:60-67`, `:85-94` | Helper `CrearTablaSolicitudesVieja(ruta, columnasExtra)` para las tres copias del `CREATE TABLE`. | El mismo esquema aparece tres veces; si cambia el esquema viejo hay que tocar tres bloques. |
| D5 | `AppDbContextTests.cs:17-19`, `:48-50` | Añadir `TestDb.NuevoContexto(out string ruta)` y reutilizarlo en los tests que abren, cierran y reabren la misma base. | Duplica `EnsureCreated` y la gestión de rutas del helper. |
| D6 | `ViewModelStaticTests.cs:1-102` | Renombrar (`TextoYCsvTests`/`EmbudoTests`) o repartir su contenido; hoy el nombre no dice que prueba CSV, normalización y embudo. | El nombre guía mal a quien busca un test. |
| D7 | `ViewModelTests.cs:29,247-248,275-276,1098,1111,1131,1152,1163,1177,1197,1205,1226`; `EstadisticasSolicitudTests.cs:71-72` | Tras B3, comparar números o construir el texto esperado con `Localizacion.Texto(...)`, no literales como «50 %», «3 días» o «1-10 de 25». | Los tests se acoplan a las traducciones y a la cultura; un cambio de texto rompe tests que no cambiaron de comportamiento. |
| D8 | `TestDb.cs:33-45` | Unificar `NuevaRuta` y `NuevaCarpeta` en `NuevaCarpeta()` + `RutaDe(carpeta, "test.db")`. | Duplicación literal; además `NuevoContexto` puede exponer la ruta para D5. |
| D9 | Nuevos tests | Cubrir A1 (fila completa de `EscribirCsv`), A3 (un solo hito), A5 (paridad real), A9 (mapa de columnas) y B7 (`SolicitudSeleccionada` nula al eliminar). | Son los cambios de esta revisión que hoy no tienen red; sin ellos, el refactor puede volver a quedar a medias sin que nada falle. |
| D10 | `TestDb.cs:12-31` | Opcional: limpiar también al terminar (fixture de colección con `Dispose`) en vez de solo al empezar. | La basura de la ejecución actual queda en `%TEMP%` hasta la siguiente; no es grave, pero el comentario reconoce la limitación. |
| D11 | `ViewModelTests.cs:1068`; `MainWindow.xaml.cs:64`; `SolicitudTests.cs` (varios `s`) | Renombrar abreviaturas: `Con25` → `ConVeinticincoSolicitudes`, `cd` → `columnaVista`, `s` → `solicitud`. El `sb` de `MainViewModel.cs:1020` desaparece con A1. | Los nombres de una letra o con números no dicen qué contienen; en un test de 1229 líneas, el coste de leerlos se multiplica. |

## E. Puertas de calidad y documentación

| # | Dónde | Cambio | Por qué |
|---|-------|--------|---------|
| E1 | Nuevo `Directory.Build.props` | `TreatWarningsAsErrors`, `AnalysisLevel=latest`, `EnforceCodeStyleInBuild=true`, `Deterministic=true`, `EnableNETAnalyzers=true`; aplicar a app y tests (hoy los tests no tienen StyleCop). Empezar con errores solo de compilador y subir las reglas CA por fases. | No hay ninguna puerta que impida meter warnings; activar todo de golpe rompería el build por los `catch` genéricos y la cultura. |
| E2 | `.editorconfig:8-25` | Activar `IDE0005` (usings sobrantes), `IDE0161` (namespace de archivo), `SA1101` (`this.`, empezando por las parciales de `MainViewModel` 301-326), `CA1305`/`CA1307` (cultura explícita) y decidir `CA1031` (captura genérica). Hoy el fichero solo desactiva reglas de StyleCop. | Es el complemento de E1; sin reglas activas, `dotnet format --verify-no-changes` no detecta los residuos de A7 ni los `catch` genéricos. |
| E3 | Nuevo `global.json`; `.github/workflows/dotnet-desktop.yml` | Fijar la versión de SDK y comprobar que el CI puede construir la solución `.slnx` con el SDK que instala (`8.0.x`). En local solo hay SDK 10 y el build está bloqueado, así que no se ha podido verificar; el soporte de `.slnx` requiere una cadena de herramientas reciente. | Sin `global.json`, el build depende del SDK que haya en la máquina; el CI instala 8.0.x mientras la solución usa un formato nuevo. Si el SDK del runner no lo soporta, el CI falla antes de compilar. |
| E4 | `.github/workflows/dotnet-desktop.yml` | Fijar el orden `dotnet format --verify-no-changes` → `dotnet build -warnaserror` → `dotnet test --no-build`, y valorar un umbral de cobertura con `coverlet.msbuild` para Helpers/ViewModels. | El formato ya va antes de los tests (bien), pero no hay puerta de warnings ni de cobertura; `coverlet` está instalado sin umbral. |
| E5 | `AGENTS.md:44`; `.github/workflows/sonarcloud.yml` | Reconciliar la documentación: `AGENTS.md` dice que SonarCloud corre en `ubuntu-24.04` y el workflow usa `windows-latest`. | La documentación y la realidad del CI no coinciden; quien depure el pipeline perderá tiempo. |
| E6 | `README.md:107`, `:108`, `:152-177` | Reescribir el bloque de estructura (hoy `EnumHelper` aparece dos veces y `CsvHelper` está partido por `EmbudoHelper`). Aclarar que el CSV exporta todas las filas pero no todos los campos (faltan `Requisitos`, `Origen`, `MotivoRechazo`, `ContactoTelefono` y las rutas/nombres de adjuntos), y que la pestaña inicial no se reinicia a *Oferta*. | El README es la referencia del proyecto y ahora mismo describe un estado que el código no tiene; la pestaña se queda en la última que usó el usuario porque no hay `SelectedIndex` que la reinicie. |
| E7 | `AGENTS.md:18`, `:25`, `:30` | Actualizar las referencias si se ejecutan A5 (paridad real), B5 (esquema fuera de `App`) y B7 de la primera revisión (`CsvHelper` dividido). | La documentación de convenciones debe seguir siendo cierta después del refactor. |
| E8 | `02-DOCS/decisiones/paginacion-tabla.md` | Si se implementa C3, actualizar la decisión y el «Pendiente de decidir» sobre la ordenación por página. | La decisión actual describe el recorte en el ViewModel; si se pagina en SQL, el documento queda obsoleto. |

## Decisiones que hay que tomar antes de tocar

No son «errores», son deuda consciente; conviene decidirlas explícitamente para no pelearse con
los tests de arquitectura o con el README:

1. **Paginación en memoria o en SQL** (C3): SQL exige una columna normalizada y un `ALTER TABLE`
   nuevo; memoria es el comportamiento documentado y suficiente para cientos de filas.
2. **Entidades POCO con `ObservableCollection<Evento>`** (A10 de la primera revisión): cambiarlo a
   `List<Evento>` alivia la dependencia de UI, pero obliga a refrescar el panel al añadir/borrar.
3. **Un solo `AppDbContext`** (convención de `AGENTS.md`; ver B11): si se pagina en SQL y se
   añaden consultas de solo lectura, valorar `AsNoTracking` para ellas sin romper el seguimiento
   de la edición.
4. **`EnsureCreated` + `ALTER TABLE`** (B5): mover el esquema a `Data` no es migrar; migrar es otro
   trabajo. No mezclarlo con esta revisión.
5. **`DialogosNulos` por defecto** (B10): comodidad para tests frente a un fallback que silencia
   avisos en producción.
6. **Reloj inyectado** (B6): `IClock` propio o `TimeProvider` de .NET 8; el segundo no necesita
   paquete, pero los tests tendrían que construir un proveedor falso.
7. **Estadísticas globales o filtradas** (B14): decidir qué significa la cabecera cuando hay un
   filtro activo y reflejarlo en el README y en un test.
8. **`Exception?` como retorno** (B15): es un patrón interno y probado; si no se cambia, al menos
   documentarlo en los dos métodos.

## Orden recomendado

**Fase 0 — cerrar la herida (2-3 horas).** A1, A2, A3, A4, A7, A8, A9, A10, A11 y D8. No cambia
comportamiento salvo que la exportación pasa a usar el helper probado; quita unas 90 líneas
muertas y deja el árbol coherente. Hacerlo en un commit aparte, sin mezclar con mejoras.

**Fase 1 — verdad en los tests y en el idioma (1 día).** A5, A6, D7, D9; B7, B8, B9; C21. Aquí se
arregla el test de paridad, se fija el comportamiento de la selección y del cambio de idioma y se
cierra la fuga de adjuntos intermedios.

**Fase 2 — responsabilidades (2 días).** B1 (paginador), B2 (adjuntos), B3 (estadísticas
numéricas), B13 (partir `Guardar`), C15, C16, C17 y C18. Son cambios con tests; no mezclar la
extracción de XAML con la de lógica en el mismo commit.

**Fase 3 — cuando toque.** B4 (`Localizacion`), B5 (esquema), B6 (reloj), C1-C14, C19, C20, C22,
E1-E5 y el resto de D. Son mejoras estructurales o de rendimiento que no bloquean lo anterior.

## Anexo: comprobaciones que no se pudieron ejecutar

- `dotnet --version` → `10.0.402`. Solo hay SDK 10 instalado; el CI instala `8.0.x`.
- `dotnet build GestorSolicitudes.slnx --no-restore` → `MSB3883: Excepción inesperada` +
  `Acceso denegado` al invocar a Roslyn. El primer intento, con restore, se quedó en 5 minutos y
  terminó con error sin errores de compilación listados.
- `dotnet test` → mismo bloqueo de `dotnet.exe`; no se ejecutó la suite, así que no hay evidencia
  de que los tests pasen. Las afirmaciones de este documento sobre comportamiento se basan en la
  lectura del código, no en resultados de la suite.
- `git status` muestra los dos documentos de revisión como no rastreados (`revision-clean-code.md`
  y este); no hay cambios de producción sin commitear. El historial confirma que A2
  (`ea43271`), A3 (`1905ee1`), A4 (`a2c9a7b`) y A1 (`b81158e`) se aplicaron por separado y que A4
  no tocó `MainViewModel` y A1 dejó el bloque viejo del hito.
