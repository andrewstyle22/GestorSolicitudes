# Revisión de clean code (tercera pasada)

Alcance: código de producción (`*.cs`, `*.xaml`, `*.csproj`), tests, CI y documentación
(`README.md`, `AGENTS.md`, `02-DOCS/`). La revisión se hizo sobre `HEAD 907031c`; después se
aplicaron los bloques 1 y 2, y ya está cerrado el bloque 3 (3.1-3.8). El bloque 4 está
dividido en 4.1-4.17 y **cerrado salvo 4.2 y 4.8**, que siguen pendientes.

> **Verificación tras el bloque 1:** `dotnet test` → 232 tests correctos, 0 fallos.
> `dotnet format GestorSolicitudes.slnx --verify-no-changes` → exit 0. Cobertura local con
> coverlet → 92,01 % de líneas y 77,69 % de ramas. `dotnet` necesitó permiso
> `danger-full-access`; con el sandbox normal devolvía «Acceso denegado» al lanzar MSBuild.
>
> **Verificación tras el bloque 2:** `dotnet test` → 252 tests correctos, 0 fallos;
> `dotnet format GestorSolicitudes.slnx --verify-no-changes` → exit 0.
>
> **Verificación tras 3.1-3.5:** `dotnet test` → 258 tests correctos, 0 fallos;
> `dotnet format GestorSolicitudes.slnx --verify-no-changes` → exit 0.
>
> **Verificación tras 3.6-3.8:** `dotnet test` → 258 tests correctos, 0 fallos (el test de
> estilos ahora también instancia las tres pestañas y la cabecera de métricas y comprueba
> que sus enlaces resuelven); `dotnet format GestorSolicitudes.slnx --verify-no-changes` →
> exit 0. La app no se ha arrancado: la verificación es de tests, no visual.
>
> **Verificación tras 4.15-4.17:** `dotnet build GestorSolicitudes.slnx` → 0 errores, 0 avisos
> (los avisos son errores desde `Directory.Build.props`, también en los tests); `dotnet test`
> → 277 correctos, 0 fallos; `dotnet format GestorSolicitudes.slnx --verify-no-changes` →
> exit 0; cobertura de líneas 94,3 % con el umbral de 90 que aplica el CI. `global.json` fija
> el SDK 10.0.403 (`rollForward: latestFeature`) porque `.slnx` no se entiende antes del
> 9.0.200, y los dos workflows instalan ahora `10.0.x`.
>
> **Verificación tras los avisos de Sonar:** `4a38192` (`Listar` devuelve `Dictionary`) y
> `7ac156c` (expresiones de colección y campos `Dictionary`) → `dotnet test` 277 correctos,
> `dotnet format` exit 0. El gate del PR no se ha comprobado: necesita push y análisis.

## Estado verificado (30/10/2026)

> **Convención de identificadores:** los IDs de esta revisión se citan sin prefijo (`A1`,
> `B1`...). Cuando se cita la revisión 2 se escribe `R2-A1`. El commit `907031c` habla de
> «A6/A10 de la fase 3»: son **R2-A6 y R2-A10**, no los `A6`/`A10` de este documento.

La «fase 0» de la revisión 2 está cerrada. El bloque 1 de esta revisión también. Estado real:

| Trabajo | Estado | Evidencia |
|---|---|---|
| R2 fase 0: A1-A4, A7, A9, A11 | Cerrado | `CsvExportador` y `EmbudoHelper` en producción, hito único, `switch` que lanza, miembros `private`/`internal` |
| R2-A8 (`x:Name` muertos) | Cerrado en el bloque 1 | se quitaron los 8 que quedaban; `Buscar_empresas` pasa a `BuscarEmpresas` |
| R2-A10 (sangría de `Grafica.Ver`) | Cerrado en el bloque 1 | `Localizacion.cs:321` ya está sangrado |
| R2-D8 (`NuevaRuta`) | Cerrado en el bloque 1 | `TestDb.cs` usa una expresión equivalente |
| R2-A6 (claves del título) | Corregido en el bloque 1 | `TextoTitulo` elige `TituloMes/Semana/Dia` y el formato ya no duplica la preposición |
| Revisión 3, bloque 1 | **Cerrado y commiteado** (`3be1207`) | A1, A2, A6, A10, C2, C3, C6, C7 y G2; 232 tests verdes; ver «Bloque 1 aplicado» |
| Revisión 3, bloque 2 | **Cerrado y commiteado** (`7fa77c7`) | A3, A4, A5, A7, A8, A9, F1 y F7; 252 tests verdes; ver «Bloque 2 aplicado» |
| Revisión 3, bloque 3 | **Cerrado y commiteado** (`2b10bcf`…`eaab361`) | B1, B4, B5, D3, D5, E1, E2 y E4; 258 tests verdes; ver «Bloque 3 dividido» |
| Revisión 3, bloque 4 | **Cerrado salvo 4.2 y 4.8** (`04f88c2`…`7ac156c`) | B2, C1, C8-C12, D2, D4, E3/E5/E6, F3-F6, F8, G1/G3/G4/G5 y B3b (4.9); B3a (reloj) y D1/C12 siguen abiertos; ver «Bloque 4 dividido» |

`git status` está limpio. HEAD es `7ac156c`, en la rama `andres/improve-code-3`: los bloques
1-3 están cerrados y del bloque 4 solo quedan 4.2 (`DialogosNulos`) y 4.8 (reloj).

Ese era el origen de la confusión: **el commit `9512fac` («Fase 1 A6: usa claves
Grafica.TituloMes/Semana/Dia») es precisamente el que dejó el título roto**, porque
interpoló el número del enum en vez de su nombre. Las claves existían desde entonces,
pero no se usaban.

### Bloque 1 aplicado

| Punto | Cambio | Test |
|---|---|---|
| A1 | `TextoTitulo` elige la clave por valor del enum; `Grafica.Titulo` pierde la preposición que ya llevan `TituloMes/Semana/Dia` | `TituloGrafica_UsaLaTraduccionDeLaGranularidad` (3 casos) y `TituloDeLaGrafica_NoDuplicaLaPreposicion` (3 idiomas) |
| A2 | `Eliminar` borra las rutas guardadas (`RutasGuardadas`), no las que el usuario cambió sin guardar | `Eliminar_ConElCvQuitadoSinGuardar_BorraElFicheroGuardado` y `Eliminar_ConElCvSustituidoSinGuardar_BorraElAnteriorYLaCopia` |
| A6 | `AplicarVisibilidadColumnas` usa `DataGridColumn` no nulo; fuera el `if` inalcanzable | `FuentesTests.CadaColumnaOcultableTieneSuCasoEnElMapaDeLaVentana` |
| A10 | Fuera los 8 `x:Name` muertos; `Buscar_empresas` → `BuscarEmpresas` | `FuentesTests` de columnas y claves |
| C2/C3/C6/C7 | `NuevaRuta` en una línea y sin comentario obsoleto; `TextoNulo` reutilizado; «puras»; sangría inglesa | Tests existentes de esquema y localización |
| G2/F2 | Bloque 1: `charset = utf-8`, comentarios de `PestanasTests` reparados y guardia contra `U+FFFD`. Después (`0c73d46`): `insert_final_newline`, `IDE0005`, `IDE0161` y `SA1101`; `CA1031`, `CA1305` y `CA1307` desactivadas con motivo | `NingunFicheroDeTextoTieneCaracteresDeReemplazo` y build sin avisos |

La retirada del BOM de 14 `.cs` es el efecto de `charset = utf-8`: no cambia código, solo la
codificación de la primera línea; se ha dejado porque el formateador la exige en cada
verificación.

### Bloque 2 aplicado

| Punto | Cambio | Test |
|---|---|---|
| A3 | `EstadoSolicitudExtensiones` (`EstaCerrada`, `EsEntrevista`, `EsOferta`, `Cerrados`) es la única definición; la usan `Solicitud.EstaAbierta`, la consulta de la bandeja y `EstadisticasSolicitud` | `EstaCerrada_SoloIncluyeLosEstadosTerminales`, `EsEntrevista_…`, `EsOferta_…` |
| A4 | `ColumnaItem` recibe la clave de texto explícita; `ColumnasOcultables` es un array de pares columna/clave y nadie concatena `"Columna." + …` | `Columnas_CadaClaveTieneSuTextoEnElDiccionario`, `LasColumnasOcultablesSonColumnasDelXamlConSuClave` |
| A5 | El ViewModel recibe `Action<Idioma> cambiarIdioma` (por defecto, no-op); la producción pasa `Localizacion.Cambiar` y los tests ya no tocan `%APPDATA%` ni la cultura global | `IdiomaActual_UsaElCambioInyectadoSinTocarElIdiomaGlobal` |
| A7 | `MainWindow` y `VisorAdjuntoWindow` reciben `IDialogoService`; el visor reutiliza `ElegirDestino`; el comentario de `DialogoService` describe lo que hace | Compilación y tests de diálogos; sin test de UI |
| A8 | `EliminarSolicitud`/`EscribirCsv` pasan a `TryEliminarSolicitud`/`TryEscribirCsv` con `bool` + `out Exception?` | `TryEliminarSolicitud_…`, `EscribirCsv_…` |
| A9 | En lugar de un `ResumenSolicitud`, `ActualizarEstadisticas` lee entidades completas con `AsNoTracking`: los cálculos usan propiedades `[NotMapped]` y un DTO las duplicaría | Tests de estadísticas existentes |
| F1 | `ViewModelTests` (1454 líneas) partido en `ViewModelTests` (CRUD y filtros), `ViewModelAdjuntosTests`, `ViewModelCsvTests`, `ViewModelDialogosTests`, `ViewModelEmbudoTests` y `ViewModelPaginacionTests`; todos los ViewModels van en `using` | 252 tests (los 78 de HEAD siguen presentes) |
| F7 | Tests nuevos de A1 (tres granularidades), A2 (adjunto huérfano), A3 (clasificación) y cabeceras CSV traducidas | Ver filas anteriores |

El test de A6 llegó después (`49e44e2`): comprueba que cada columna ocultable tiene su caso
`"Nombre" => this.Nombre` en el `switch` del code-behind. No abre la ventana porque WPF solo
admite una `Application` por AppDomain.

### Bloque 3 dividido

| Sub-bloque | Punto | Cambio | Estado |
|---|---|---|---|
| 3.1 | B1 (adjuntos) | `GestorAdjuntos` (Helpers) lleva las copias, las sustituciones y el descarte; su reglas tienen test propio | Hecho (`2b10bcf`) |
| 3.2 | B1 (columnas) | `ColumnasViewModel` lleva `Columnas`, `PanelColumnasAbierto`, la carga/guardado y el evento; el XAML enlaza `ColumnasVm` | Hecho (`38d39ed`) |
| 3.3 | B4 | `App` abre la base, crea el ViewModel y el servicio, y los libera en `OnExit`; `MainWindow` recibe ambos | Hecho (`2aa4bf3`) |
| 3.4 | B5 | Un único `ResultadoPagina<Solicitud> Pagina`; `RangoPagina` es texto derivado y el XAML enlaza `Pagina.*` | Hecho (`de8161f`) |
| 3.5 | D3 | La carga del historial sale a `Seleccionar`, documentada como E/S deliberada; el setter solo atiende al DataGrid | Hecho (`29db291`) |
| 3.6 | D5 | Visor PDF: páginas bajo demanda, `BitmapImage` congeladas y `Stream` liberado | Hecho (`eaab361`) |
| 3.7 | E1 | Pestañas del detalle a `UserControl` y métricas a `CabeceraMetricas` | Hecho |
| 3.8 | E2/E4 | Enlaces sin `RelativeSource=Window` (`Edicion.*` y comandos directos); paleta/estilos del visor | Hecho (E4 en 3.6) |

En 3.8 se descartó `Source={x:Reference raiz}`: dentro de las pestañas ya no hace falta un
origen alternativo porque los campos cuelgan de `Edicion.*` y los comandos son propiedades
del ViewModel, que es el `DataContext` heredado. Solo el `DataTemplate` del historial sigue
saliendo del árbol (`AncestorType=UserControl`), porque ahí el `DataContext` es el evento.

La separación evita mezclar lógica con XAML: 3.1-3.5 son lógica con test, 3.6 es el visor y
3.7-3.8 son extracción/estilos de XAML.

### Bloque 4 dividido

Criterio: un commit por fila y cada fila deja `dotnet test` y `dotnet format` en verde. El
tamaño (S/M/L) es esfuerzo, no riesgo.

| Sub-bloque | Puntos | Cambio | Tamaño | Notas |
|---|---|---|---|---|
| 4.1 | C1, C8, C9 | Renombrados: `CsvHelper` → `TextoHelper`, `ViewModelStaticTests` → `TextoYCsvTests`, `cd`/`Con25`/`s` por sus nombres completos | S | Solo nombres; el compilador vigila. Los tests existentes cubren el comportamiento. |
| 4.2 | D1, C12 | Decidir el `DialogosNulos` por defecto y quitar la traza del constructor | S/M | **Decisión 4**. Si el servicio pasa a obligatorio hay que tocar 63 constructores de test; por eso va aparte y después de 4.1. |
| 4.3 | D4 | `AbrirEnlace` avisa cuando la URL no es `http`/`https` (o el botón se deshabilita) | S | Test del servicio con un enlace `mailto:` y uno roto. |
| 4.4 | C10, E5 | `FilaAdjunto`: un `ComandoAbrir` en vez del evento CLR, y una sola propiedad `Adjunto` (ruta + nombre) en vez de `Texto`+`Ruta` | M | Un fichero y sus dos usos; el manejador de la ventana desaparece. Toca `PestanasTests`. |
| 4.5 | C11 | Colores de estado a una tabla con nombre; el pincel `OfertaRechazada` pasa a llamarse por su uso | M | `ConvertidorTests` no es STA: los colores tienen que ser estáticos de C#, no recursos de `App.xaml`. |
| 4.6 | B2a | `Traducciones` (Helpers): los tres diccionarios, `Texto`, `Contiene`, `Claves`, `Listar` y `NombrePropio`; `Localizacion` queda de fachada | L | Movimiento mecánico de ~800 líneas. Rompe de paso la dependencia Helpers→Núcleo (decisión 7). |
| 4.7 | B2b | `CulturaApp` (cultura del hilo, lenguaje de ventanas, `Cambiar`) y `PreferenciaIdioma` (`idioma.txt`) | M | `IdiomaActual` e `IdiomaCambiado` se quedan en la fachada. Si crece: 4.7a cultura / 4.7b fichero. |
| 4.8 | B3a | Reloj inyectable para `DateTime.Today`/`Now` en el modelo (`Solicitud`, `Evento`), `EmbudoHelper` y `MainViewModel` | L | **Decisión 1** (`TimeProvider` o `IClock`). Toca casi todos los tests de fechas: va solo. |
| 4.9 | B3b | `EmbudoHelper.EtiquetaDePeriodo` recibe la cultura en vez de leerla del hilo | S | Independiente de 4.8 aunque salga del mismo punto. |
| 4.10 | D2 | `ActualizarEstadisticas` deja de lanzar una consulta completa por recarga (conteos en SQL o caché invalidada al escribir) | M | Medir con un contador de consultas en el test, no a ojo. |
| 4.11 | E3, E6 | Medidas mágicas del XAML (`1525`, `394*`, `440`, `355`) a recursos o `*`+`MinWidth`, y `ToolTip`/`AutomationProperties` en los dos «✕» | M | Riesgo visual: la pantalla no se puede comprobar desde aquí. E6 entra por ser dos líneas del mismo XAML. |
| 4.12 | F3, F4, F5 | Limpieza de tests: fuera el test de color redundante y `CadaTestUsaSuPropiaBase`, helper del esquema viejo, `TestDb.NuevoContexto` con ruta | M | No toca producción. |
| 4.13 | F6 | Los tests que comparan literales traducidos («50 %», «1-10 de 25») pasan a comparar números o a componer el texto con `Localizacion.Texto` | S | |
| 4.14 | F8, G4 | Test que rechaza `U+FFFD` y exige UTF-8 en `.cs`/`.xaml`; documentar los límites de `FuentesTests` y cambiar el «exactamente 9 columnas» por comportamiento | S/M | El caso de `PestanasTests` demostró que nada detectaba la corrupción. |
| 4.15 | G1 | `Directory.Build.props`: warnings como errores, analizadores y estilo en el build, también en el proyecto de tests | M | Último del código: se pone cuando ya no quedan warnings que arreglar. |
| 4.16 | G3 | `global.json` y CI: fijar SDK, `dotnet build -warnaserror`, umbral de cobertura | M | El CI instalaba `8.0.x`, que no entiende `.slnx` (hace falta 9.0.200 o superior); podía estar en rojo desde antes. |
| 4.17 | G5 | Actualizar `02-DOCS/decisiones/*.md` con lo que dejen hecho 4.4-4.11 | S | Solo documentación. |

Estado: **4.1, 4.3-4.7 y 4.9-4.17 hechos y commiteados**; **4.2 y 4.8 no han empezado**.
4.2 obliga a tocar 63 constructores de test si el servicio pasa a obligatorio (`App` ya lo
pasa siempre). 4.8 tiene 9 usos de `DateTime.Today` en producción (`Solicitud` 4, `Evento` 1,
`EmbudoHelper` 1, `MainViewModel` 5) y 49 referencias en tests.

Orden propuesto: 4.1 → 4.3 → 4.2 → 4.6 → 4.7 → 4.5 → 4.4 → 4.9 → 4.8 → 4.10 → 4.11 →
4.12 → 4.13 → 4.14 → 4.15 → 4.16 → 4.17.

Primero lo barato y sin riesgo, luego las dos extracciones grandes con la suite en verde,
después lo que cambia comportamiento (reloj, consultas) y las puertas de calidad al final,
cuando el árbol ya no tenga warnings. 4.16 se mira antes de nada porque puede condicionar el
resto.

Decisiones dentro del bloque: solo quedan **4.2** (`DialogosNulos` por defecto) y **4.8**
(reloj). Fuera de los cuatro bloques sigue B6 (`ObservableCollection` en el POCO); D6 (clave
`Error` duplicada) se corrigió en `d4dd9f9`. La decisión 7 (Helpers→Núcleo) la resolvió 4.6.

## Veredicto rápido

| Dimensión | Estado | Evidencia |
|-----------|--------|-----------|
| Corrección | Verde | A1 y A2 corregidos con test: el título usa las claves de la granularidad y eliminar borra los adjuntos guardados |
| Responsabilidad única | Amarillo | `MainViewModel` 1085 líneas; `Localizacion` queda en fachada de 73 líneas sobre `Traducciones`, `CulturaApp` y `PreferenciaIdioma` |
| DRY / fuente única | Verde | Clasificación de estados única, columnas y adjuntos extraídos, sin `x:Name` muertos |
| Nombres | Verde | `TextoHelper`, `TextoYCsvTests` y los nombres completos en tests (4.1) |
| Errores y estado | Amarillo | `Exception?` fuera (`Try…` con `bool` + `out`); siguen el `DialogosNulos` por defecto (4.2) y la ausencia de reloj (4.8) |
| Tests | Verde | 278 tests, cobertura 94,3 % de líneas y 78,71 % de ramas; `ViewModelTests` 552 líneas |
| Puertas de calidad | Verde | `Directory.Build.props` con avisos como errores, `global.json`, StyleCop en tests y umbral de cobertura 90 % en CI |
| Documentación | Verde | `02-DOCS/decisiones` y este documento describen el código; falta cerrar 4.2/4.8 |

## Los cinco puntos que más pesan

Hallazgos originales; los cuatro primeros ya están corregidos:

1. **El título de la gráfica muestra una clave en crudo.** `MainViewModel.TituloGrafica`
   construye `$"Grafica.Titulo{(int)GranularidadGrafica}"`, es decir, `Grafica.Titulo0`,
   `Titulo1` y `Titulo2`; el diccionario define `Grafica.TituloMes`, `TituloSemana` y
   `TituloDia`. `Localizacion.Texto` devuelve la propia clave cuando falta, así que el
   título enseña «Grafica.Titulo0». Es una regresión introducida al corregir el `ToLower`
   de la revisión 2 (R2-A6, commit `9512fac`) y ningún test la ve.
2. **El test que vigila las claves no puede verlas.** `FuentesTests` extrae claves con la
   expresión `Localizacion.Texto\("([^"]+)"` (`FuentesTests.cs:171-172`), que no casa con
   interpolaciones (`$"..."`) ni con concatenaciones. Por eso el proyecto parece protegido
   contra claves mal escritas y no lo está. La solución no es ampliar el regex: es dejar de
   construir claves a mano.
3. **Al eliminar una fila se puede quedar un adjunto huérfano.** Si en una candidatura
   guardada se pulsa *Quitar* (o se sustituye el CV) y después *Eliminar* sin guardar,
   `Eliminar` lee `Edicion.RutaCv` ya modificado (`MainViewModel.cs:998-999`) y borra el
   fichero nuevo o nada; el que apuntaba la base de datos se queda en disco para siempre.
   `Guardar` sí lo resuelve con `RutasGuardadas` (`:736-741`), pero `Eliminar` no usa ese
   camino.
4. **La clasificación de estados está duplicada.** «Cerrada» se define en
   `Solicitud.EstaAbierta` (`Solicitud.cs:127-130`), otra vez en la consulta de la bandeja
   (`MainViewModel.cs:635-638`) y los grupos de entrevista/oferta en
   `EstadisticasSolicitud.Calcular` (`:31-34`). Añadir un estado terminal obliga a tocar
   tres sitios y uno de ellos fallará en silencio.
5. **La deuda estructural y las puertas siguen igual.** `MainViewModel` no bajó (creció con
   el flujo de adjuntos), `Localizacion` sigue monolítica, y no hay ninguna puerta que
   impida meter warnings, claves rotas o ficheros con la codificación corrupta. La prueba
   más visible: `PestanasTests.cs` contiene caracteres de reemplazo (`U+FFFD`) en los
   comentarios.

## Lo cerrado desde `revision-clean-code-2.md`

No se repite como tarea; se deja constancia para que la tercera pasada no lo vuelva a pedir.

- **R2-A1 a R2-A4:** `CsvExportador` y `EmbudoHelper` ya se usan en producción; el bloque
  viejo del hito desapareció; `ProponerSeguimiento` se aplica con `if`.
- **R2-A5:** `Localizacion.Claves(Idioma)` y el test que compara los conjuntos de los tres
  idiomas.
- **R2-A7, R2-A9, R2-A11:** usings redundantes fuera, `switch` de columnas que falla con una
  desconocida, «empresa y puesto» corregido, miembros públicos bajados a
  `private`/`internal`.
- **R2-B1 a R2-B3, R2-B5, R2-B7 a R2-B9, R2-B11 a R2-B14:** `Paginador`, `DescriptorAdjunto`,
  estadísticas numéricas, `EsquemaDb` en `Data`, selección limpiada al eliminar, guardas de
  recarga al cambiar de idioma, propiedad de idioma unificada, `db` liberado en `OnClosed`,
  `RutasGuardadas` documentado, `Guardar` partido y semántica de estadísticas fijada con test.
- **R2-C1, R2-C2, R2-C4 a R2-C8, R2-C10 a R2-C18, R2-C20, R2-C21:** límites del embudo como
  método, historial cargado al seleccionar, búsqueda en una sola cadena normalizada, proyección
  de la bandeja, pinceles congelados, caché de `EnumHelper`, poda de `idsVencidosAvisados`,
  constantes del temporizador, `Enlaces`, `SqliteConnectionStringBuilder`, canonicalización de
  adjuntos, `EstiloCelda`, `BotonPlano`, `FilaAdjunto` y paleta `Color.*`, icono unificado y
  lista de copias de adjuntos para no dejar huérfanos al sustituir.
- **R2-D9 (parcial):** tests de `EscribirCsv`, diálogos, adjuntos y exportación.
- **R2-E5 a R2-E7:** AGENTS/README actualizados (SonarCloud en Windows, `IDialogoService`,
  `EsquemaDb`, estructura).

**Pendiente de la revisión 2:** R2-A8 (quedan 8 `x:Name`), R2-A10 (sangría inglesa de
`Grafica.Ver`) y R2-D8 (`NuevaRuta` duplicada). R2-A6 está **mal cableada**: las claves
existen, pero el título no las usa (es el A1 de este documento).

## A. Corrección — hallazgos de esta revisión

> Estos `A1`-`A10` son de la revisión 3. El commit `907031c` cita `R2-A6` y `R2-A10`, que
> son otros. `9512fac` ya había añadido `Grafica.TituloMes/Semana/Dia`; el bloque 1 ha
> corregido su uso. **A1-A10 están aplicados** (A3-A5, A7-A9 en el bloque 2); las filas se
> conservan como registro del hallazgo.

| # | Dónde | Cambio | Por qué |
|---|-------|--------|---------|
| A1 | `MainViewModel.cs:479-482`; `Localizacion.cs:58-60, 309-311, 560-562` | **Crítico.** `9512fac` ya añadió las claves; falta usarlas. Construir la clave con el nombre del valor, no con el número: `Localizacion.Texto($"Grafica.Titulo{this.GranularidadGrafica}")` (o un `switch` explícito a `TituloMes`/`TituloSemana`/`TituloDia`). Añadir un test que recorra las tres granularidades y compruebe que `TituloGrafica` no contiene `"Grafica.Titulo"` y sí el texto traducido. | Hoy devuelve `Grafica.Titulo0`, que no existe; `Localizacion.Texto` cae a la propia clave y la UI la muestra en crudo. |
| A2 | `MainViewModel.cs:981-1011`; `:736-741`; `:917-928` | **Alta.** En `Eliminar`, obtener las rutas guardadas antes de borrar (`RutasGuardadas(Edicion)` si la entidad está en seguimiento) y borrar esas, no las de `Edicion`. Añadir test: abrir una candidatura con CV, `QuitarCv`, `Eliminar` → el fichero original desaparece; y variante con CV sustituido sin guardar. | Fuga de disco: la base deja de apuntar a un fichero que sigue existiendo. |
| A3 | `Solicitud.cs:126-130`; `MainViewModel.cs:635-638`; `EstadisticasSolicitud.cs:31-34` | **Alta.** Una sola definición de la clasificación: extensiones de `EstadoSolicitud` (`EstaCerrada`, `EsEntrevista`, `EsOferta`) usadas por `Solicitud`, la consulta de la bandeja y las estadísticas. En la consulta, filtrar con la lista de estados cerrados generada desde esa definición. | Tres listas paralelas: un estado terminal nuevo se olvida en una y la bandeja avisa de procesos cerrados o las métricas dejan de cuadrar. |
| A4 | `FuentesTests.cs:104-128, 171-178`; `MainViewModel.cs:482` | **Alta.** No ampliar el regex: sustituir las claves construidas por constantes tipadas (`ClavesTexto.GraficaTituloMes`, etc.) o por una función `ClaveTitulo(granularidad)`, y añadir tests de comportamiento de los textos generados. | El test de claves no ve interpolaciones ni concatenaciones; da falsa confianza justo en el punto donde se rompió el título. |
| A5 | `MainViewModel.cs:101`; `Localizacion.cs:853-866, 971-984` | **Alta.** Quitar el efecto de disco del setter: `IdiomaActual` no debe llamar a `Localizacion.Cambiar` (que escribe `idioma.txt`) desde una propiedad observable. Exponer un comando `CambiarIdioma` explícito o mover la persistencia a un servicio inyectado, y hacer que los tests no puedan escribir en `%APPDATA%`. | Un test que asigne `vm.IdiomaActual = Idioma.Ingles` cambia la cultura global y escribe en `%APPDATA%`, en contra de la regla del proyecto; además es un efecto lateral oculto en un setter. |
| A6 | `MainWindow.xaml.cs:72-87` | **Media.** Quitar el `if (cd is not null)`: el `switch` ya lanza con una columna desconocida, así que la comprobación es inalcanzable. Mejor aún, construir el mapa `columna → DataGridColumn` una vez y recorrerlo. | Rama muerta que sugiere un caso nulo que ya no existe. |
| A7 | `Views/MainWindow.xaml.cs:199-215`; `Views/VisorAdjuntoWindow.xaml.cs:101-136`; `DialogoService.cs:10-13`; `App.xaml.cs:32-35` | **Media.** Unificar los avisos de las vistas: o pasan por `IDialogoService` (una instancia creada en el arranque) o `DialogoService` deja de afirmar que es «el único sitio del programa que muestra un MessageBox». El `SaveFileDialog` del visor debe reutilizar `ElegirDestino`. | El comentario de `DialogoService` ya es falso: hay cinco `MessageBox` y un `SaveFileDialog` fuera de él, con mensajes que se pueden desincronizar. |
| A8 | `MainViewModel.cs:1023-1035, 1114-1125`; tests `:585-600, 942-946` | **Media.** Sustituir `Exception?` como valor de retorno por `bool` + `out string? error`, por un `ResultadoOperacion`, o dejar subir la excepción y capturarla en el comando. Si se mantiene, documentarlo en los dos métodos y tipar el resultado. | `null` = éxito y `is Exception` = fallo es un patrón que sorprende; los tests se acoplan a él y el lector tiene que recordarlo. |
| A9 | `MainViewModel.cs:561-584` | **Media.** No proyectar entidades: `Select(s => new Solicitud { ... })` usa el modelo como DTO y crea objetos de dominio incompletos. Proyectar a un record `ResumenSolicitud` (o hacer los conteos en SQL) y que `EstadisticasSolicitud.Calcular` reciba esa forma. | Modelo de dominio como contenedor de columnas parciales; además invita a usar propiedades calculadas sobre datos que no se han cargado. |
| A10 | `MainWindow.xaml:504, 606, 614, 618-641, 652` | **Media.** Quitar los `x:Name` que nadie lee: `Estado`, `SelectorFilasPagina`, `RangoTexto`, los cuatro botones de paginación y `NuevaCandidatura`. Renombrar `Buscar_empresas` a `BuscarEmpresas` (solo lo usa un `ElementName`). | API muerta que sugiere un acceso desde code-behind que no existe. |

## B. Responsabilidad única y diseño

| # | Dónde | Cambio | Por qué |
|---|-------|--------|---------|
| B1 | `MainViewModel.cs` (1132 líneas, 8 responsabilidades) | Extraer el flujo de adjuntos (estado `adjuntosCopiados`, `RutasGuardadas`, `AdjuntarAdjunto`, `QuitarAdjunto`, `EliminarAdjuntosSustituidos`, `EliminarAdjuntosNoGuardados`, `EliminarCopiasDeLaEdicion`; ~200 líneas) a un `GestorAdjuntos` con su propio test. Extraer también el estado de columnas (`Columnas`, `PanelColumnasAbierto`, `CargarColumnas`, `GuardarColumnas`) a un `ColumnasViewModel`. | Es la extracción con mejor relación coste/beneficio que queda: el flujo de adjuntos es el que más reglas tiene y el que más se ha tocado; las columnas son preferencia + UI, no CRUD. |
| B2 | `Localizacion.cs` (993 líneas) | Dividir en `Traducciones` (diccionarios y fallback), `CulturaApp` (cultura del hilo y lenguaje de ventanas), `PreferenciaIdioma` (lectura/escritura de `idioma.txt`) y una fachada `Localizacion` que conserve la API pública. | Cinco responsabilidades en un fichero; los textos son datos y el resto es infraestructura. Es el mayor `.cs` después del ViewModel. |
| B3 | `Solicitud.cs:54, 109, 117-119, 122-124, 136-138`; `Evento.cs:16`; `MainViewModel.cs:631, 751, 783, 1063`; `EmbudoHelper.cs:65`; `ReglasSolicitud.cs:24` | Inyectar un reloj (`TimeProvider`/`IClock`) o centralizar `DateTime.Today` en un `Reloj.Hoy`; el modelo puede recibir la fecha en sus cálculos. Pasar la cultura a `EmbudoHelper.EtiquetaDePeriodo`. | Tests dependientes del día real y del cambio de medianoche, y una función de fechas que además depende de la cultura global. |
| B4 | `Views/MainWindow.xaml.cs:39-41`; `App.xaml.cs:43-45` | Mover la composición al arranque: `App` crea `AppDbContext`, `DialogoService` y `MainViewModel`, y la ventana recibe el ViewModel. Hoy la ventana es la raíz de composición de facto y la que libera la base. | La vista no debería decidir qué base de datos se abre; además hoy hay dos contextos en el arranque (uno para `EnsureCreated` y otro para la sesión). |
| B5 | `MainViewModel.cs:232-317` | `Paginador` ya calcula todo, pero el ViewModel sigue exponiendo ocho propiedades (`PaginaActual`, `TotalPaginas`, `RangoPagina`, cuatro booleanos). Exponer un único `ResultadoPagina` o una propiedad `Pagina` y enlazar el XAML a ella. | Se ha movido el cálculo pero no el estado; quedan ocho setters que nadie debería tocar desde fuera. |
| B6 | `Solicitud.cs:105` | Decisión: `ObservableCollection<Evento>` en un POCO. Si se mantiene, documentarlo como excepción consciente; si se cambia a `List`, refrescar el panel al añadir/borrar. | El modelo depende de una colección de UI, en contra de «POCO sin notificación». |

## C. Duplicación, nombres y código muerto

| # | Dónde | Cambio | Por qué |
|---|-------|--------|---------|
| C1 | `CsvHelper.cs` | Renombrar a `TextoHelper` o separar `NormalizarBusqueda` de `Escapar`; el nombre actual dice CSV pero la mitad del fichero es normalización Unicode para el buscador. | El nombre miente sobre la responsabilidad (la ubicación en `Helpers` es correcta). |
| C2 | `TestDb.cs:40-45` | Eliminar el comentario `# Método interno consolidado: Devuelve la carpeta temporal` (está obsoleto y no describe el método) y expresar `NuevaRuta` como `Path.Combine(NuevaCarpeta(), "test.db")`. | Residuo de una edición anterior; el método es una línea. |
| C3 | `EsquemaDb.cs:37, 48` | Usar `TextoNulo` también para `ContactoTelefono` (hoy repite `"TEXT NULL"`). | La constante está justo al lado y se usa para las demás columnas. |
| C4 | `EstadisticasSolicitud.cs:31-36` | Partir el `return` en variables con nombre (`abiertas`, `enEntrevista`, `ofertas`) y mover los grupos de estados a las extensiones de A3. | Una línea de 300 caracteres con tres `Count` anidados no se revisa. |
| C5 | `Paginador.cs:54-63` | `PuedeIrAInicio` y `PuedeRetroceder` son la misma expresión; derivar una de la otra o documentar que son alias semánticos. | Duplicación trivial que puede divergir. |
| C6 | `ReglasSolicitud.cs:6` | «Reglas de negocio pura» → «puras». | Concordancia. |
| C7 | `Localizacion.cs:321` | Sangrar `("Grafica.Ver", "Chart")` como sus vecinas. | Formato roto en medio de la tabla inglesa. |
| C8 | `GestorSolicitudes.Tests/ViewModelStaticTests.cs:6` | Renombrar (`TextoYCsvTests`/`EmbudoHelperTests`) o repartir: ya no prueba estáticos del ViewModel. | El nombre guía mal a quien busca un test. |
| C9 | `MainWindow.xaml.cs:72`; `ViewModelTests.cs:1293`; tests en general | `cd` → `columnaVista`; `Con25` → `ConVeinticincoSolicitudes`; `s` → `solicitud` en los tests largos. | Abreviaturas de una letra en ficheros de más de 1000 líneas. |
| C10 | `Views/FilaAdjunto.xaml.cs:31, 63`; `MainWindow.xaml:915, 922` | El tercer botón usa un evento CLR `Abrir` + handler, mientras los otros dos son `ICommand` por `DependencyProperty`. Unificar con un `ComandoAbrir` o un `RoutedEvent`. | Tres botones de la misma fila con dos mecanismos distintos; el evento obliga a cablear cada instancia. |
| C11 | `Convertidores.cs:46-56` | Los hexadecimales de estado no están en la paleta (solo los de la gráfica); `Retirada` reutiliza el pincel llamado `OfertaRechazada`. Sacar los colores a `App.xaml` o a un `ColoresEstado` y nombrar el pincel por su uso. | Cambiar un color obliga a tocar C#; el nombre del pincel no coincide con el estado que pinta. |
| C12 | `DialogosNulos.cs:10-11` | Quitar el `Debug.WriteLine` del constructor (se ejecuta en cada ViewModel de test sin diálogo) o dejar una sola traza en el primer uso. | Ruido en la salida de depuración de toda la suite. |

## D. Errores, E/S y ciclo de vida

| # | Dónde | Cambio | Por qué |
|---|-------|--------|---------|
| D1 | `MainViewModel.cs:42` | Decidir el `DialogosNulos` por defecto: o el servicio es obligatorio (los tests pasan `DialogoFalso`/`DialogosNulos`) o el constructor deja una traza inequívoca. | Un fallback que silencia todos los avisos en producción es un invariante escondido. |
| D2 | `MainViewModel.cs:629-641`; `:506-542` | `SeguimientosVencidosAhora` ya proyecta y usa `AsNoTracking`, pero `ActualizarEstadisticas` sigue lanzando una segunda consulta completa en cada `Recargar`. Valorar conteos en SQL o cachear hasta que cambie la base. | Cada tecla del buscador hace dos consultas de tabla completa (lista + estadísticas), más la de `Min` del embudo. |
| D3 | `MainViewModel.cs:350-359` | `OnSolicitudSeleccionadaChanged` hace `Entry(value).Collection(...).Load()` (E/S) dentro de un setter. Moverlo a un comando `Seleccionar`/método explícito o documentarlo como parte del contrato de la propiedad. | Un setter que consulta la base sorprende y complica el test. |
| D4 | `DialogoService.cs:53-61` | `AbrirEnlace` devuelve sin hacer nada si la URL no es http/https; el usuario no recibe feedback. Devolver `bool`/avisar o deshabilitar el botón cuando `Enlaces.EsWeb` es falso. | El botón *Abrir* parece roto con un enlace `mailto:` o mal escrito. |
| D5 | `Views/VisorAdjuntoWindow.xaml.cs:61-85` | Renderizar las páginas del PDF bajo demanda, congelar los `BitmapImage` y liberar el `Stream` de `AsStreamForRead`. El `Loaded += async` tampoco se espera ni se cancela al cerrar. | Un PDF de 200 páginas se carga entero como imágenes; la ventana puede tocar controles ya cerrados. |
| D6 | `MainWindow.xaml.cs:195-216` | Sustituir `Localizacion.Texto("Error")` por una constante compartida con el ViewModel (`ClaveError`) y unificar el aviso con A7. | La misma clave está escrita a mano en dos sitios. |

## E. XAML y UI

| # | Dónde | Cambio | Por qué |
|---|-------|--------|---------|
| E1 | `Views/MainWindow.xaml` (1033 líneas) | Extraer cada pestaña del panel de detalle a un `UserControl` (`PestanaOferta`, `PestanaSeguimiento`, `PestanaAdjuntos`) y la cabecera de métricas a otro. | La ventana mezcla cabecera, gráfica, tabla, filtros, tres formularios y bandeja; encontrar un campo obliga a recorrer mil líneas. |
| E2 | `MainWindow.xaml:679, 703, 720, 741, 750, 808, 816, 913-921, 933, 958, 966` | Repetir `{Binding DataContext.X, RelativeSource={RelativeSource AncestorType=Window}}` en cada comando del detalle. Nombrar la raíz (`x:Name="raiz"`) y usar `Source={x:Reference raiz}`, o exponer los comandos en un ViewModel de detalle. | Veinte bindings largos e idénticos: un error de dedo deja un botón sin comando. |
| E3 | `MainWindow.xaml:9, 345-348, 652-654, 687` | Medidas mágicas (`Width="1525"`, `394*`/`75*`, `Width="440"`, `Height="355"`). Sacarlas a recursos o usar `*`/`MinWidth`. | La ventana no se adapta y el layout se ajusta a base de números. |
| E4 | `Views/VisorAdjuntoWindow.xaml:4`; todo el fichero | `Title="Visor"` sin localizar (el constructor lo sustituye, pero es un texto visible de reserva); no usa la paleta ni los estilos de botón del resto de ventanas. | Es la única ventana que se ve distinta y el único texto que no pasa por `Localizacion`. |
| E5 | `Views/FilaAdjunto.xaml.cs:33-61` | `Texto` y `Ruta` son dos propiedades para el mismo concepto (nombre visible y ruta del fichero); valorar pasar un `Adjunto` (ruta, nombre) y derivar el tooltip. | Menos propiedades de dependencia y menos cableado en el XAML. |
| E6 | `MainWindow.xaml:398, 963` | Los botones «✕» (limpiar búsqueda y borrar evento) no tienen `ToolTip` ni `AutomationProperties.Name`, a diferencia de las flechas de paginación. | Accesibilidad y coherencia con el resto de botones. |

## F. Tests

| # | Dónde | Cambio | Por qué |
|---|-------|--------|---------|
| F1 | `ViewModelTests.cs` (1454 líneas) | Partir por asunto (`CrudTests`, `FiltrosTests`, `PaginacionTests`, `EmbudoTests`, `CsvTests`, `DialogosTests`, `AdjuntosTests`) y usar `using var vm = ...` en todos: hoy solo un test llama a `Dispose` (`:956-957`) de los ~76 que construyen ViewModel. | Fichero inmanejable y ViewModels retenidos por el evento estático; si un test cambia el idioma, los ViewModels de tests anteriores usan un `db` ya liberado. |
| F2 | `PestanasTests.cs` | **Media.** Corregir los caracteres `U+FFFD` de los comentarios (líneas 13-20, 40, 49, 108-110); renombrar a `EstilosAppTests` y extraer métodos por asunto; envolver la creación de `App` en `try/finally { app.Shutdown(); }`. | El fichero está corrupto en su codificación y concentra 17 asserts de pestañas, botón, fila de adjunto, cabecera e icono en un único test; la `Application` queda viva para el resto de la suite. |
| F3 | `ConvertidorTests.cs:65-82` y `:92-117`; `:160-173` | Borrar `EstadoAColor_PintaCadaEstadoConSuColor` (subconjunto de la tabla completa) y añadir `FechaCortaConverter` al test de `ConvertBack`. | Duplicación y convertidor nuevo sin cubrir en esa prueba. |
| F4 | `VerificarColumnasTests.cs:19-28, 60-67, 85-96` | Helper `CrearTablaSolicitudesVieja(ruta, columnasExtra)` para las tres copias del esquema. | El mismo `CREATE TABLE` tres veces. |
| F5 | `AppDbContextTests.cs:12-66`; `:68-73` | Usar `TestDb.NuevaContexto(out ruta)` en los tests que abren, cierran y reabren; borrar `CadaTestUsaSuPropiaBase`, que prueba el helper, no el contexto. | Duplica la gestión de rutas y `EnsureCreated`; un test que no prueba producción. |
| F6 | `ViewModelTests.cs:28, 327-328, 363-364, 1323, 1336`; `ConvertidorTests.cs:57-58` | Comparar números o construir el texto con `Localizacion.Texto(...)` en vez de literales `"50 %"`, `"3 días"`, `"1-10 de 25"`, `"Todos los estados"`. | Los tests se acoplan a las traducciones; un cambio de texto rompe tests que no cambiaron de comportamiento. |
| F7 | Nuevos tests | Cubrir A1 (título por granularidad), A2 (adjunto huérfano al eliminar), A3 (clasificación única de estados) y A6 (mapa de columnas del code-behind). | Son los cambios de esta revisión; sin red, el refactor puede volver a quedar a medias. |
| F8 | `FuentesTests.cs` (todo) | Mantenerlo como red, pero reconocer sus límites: lee el árbol de fuentes, clasifica ViewModels por texto (`:150-152`), exige exactamente 9 columnas de texto (`:80`) y no ve claves dinámicas. Complementarlo con tests de comportamiento en vez de ampliar regex. | Un test que depende de la ruta del repositorio y del texto del código falla por reformateos o por mover un fichero, y no prueba lo que aparenta. |

## G. Puertas de calidad

| # | Dónde | Cambio | Por qué |
|---|-------|--------|---------|
| G1 | Nuevo `Directory.Build.props` | `TreatWarningsAsErrors` (empezando por warnings del compilador), `AnalysisLevel=latest`, `EnforceCodeStyleInBuild=true`, `EnableNETAnalyzers=true`, `Deterministic=true`, y aplicarlo también al proyecto de tests (hoy no tiene StyleCop). | No hay ninguna puerta que impida meter warnings; el proyecto de tests está fuera del análisis. |
| G2 | `.editorconfig` | Añadir `charset = utf-8`, `insert_final_newline = true`, `IDE0005` (usings sobrantes), `IDE0161` (namespace de fichero), `SA1101` (`this.`) y decidir `CA1305`/`CA1307`/`CA1031`. | Hoy solo desactiva reglas de StyleCop; no hay nada que impida el `using` de más, el `catch` genérico o el fichero corrupto de F2. |
| G3 | Nuevo `global.json`; `.github/workflows/dotnet-desktop.yml:17-26` | Fijar el SDK (local hay 10.0.403; el CI instala 8.0.x) y **comprobar que ese SDK entiende `.slnx`**: el soporte de `.slnx` en la CLI llegó con la 9.0.200 ([release notes](https://github.com/dotnet/core/blob/main/release-notes/9.0/9.0.0/9.0.200-preview.md)); un SDK 8.0.x no lo entiende. Añadir `dotnet build -warnaserror` antes de los tests y un umbral de cobertura. | Sin `global.json` el build depende de la máquina; si el SDK del runner no entiende `.slnx`, `dotnet format` y `dotnet test` fallan antes de compilar. `coverlet` está instalado sin umbral. |
| G4 | `FuentesTests` o nuevo test | Comprobar que ningún fuente contiene `U+FFFD` y que los `.cs`/`.xaml` están en UTF-8; el caso de `PestanasTests` demuestra que nada lo detecta. | Corrupción de codificación silenciosa en comentarios y textos. |
| G5 | `02-DOCS/decisiones/*.md` | Actualizar `selector-columnas.md` y `cabecera-columnas.md` si se ejecutan A10, E1 o F2 (mencionan `x:Name` en todas las columnas y `PestanasTests` como test único). | Las decisiones deben seguir describiendo el código; hoy mezclan estado actual y pasado. |

## Orden recomendado

**Bloque 1 — regresiones.** Cerrado y commiteado (`3be1207`): A1 + su test, A2 + su test, A6,
A10, C2, C3, C6, C7 y G2/F2 para la codificación.

**Bloque 2 — verdad en el modelo y en los tests.** Cerrado y commiteado (`7fa77c7`): A3
(clasificación única) + test, A5 (idioma sin `%APPDATA%`), A4 (claves tipadas), A7 (diálogos de
las vistas), A8 (resultado de operación), F1 (partir `ViewModelTests` y liberar ViewModels), F7.

**Bloque 3 — responsabilidades.** Cerrado (`2b10bcf`…`eaab361`): 3.1-3.8, con B1 (adjuntos y
columnas), B4, B5, D3, D5 y E1/E2/E4.

**Bloque 4 — limpieza y puertas.** Hecho salvo 4.2 y 4.8: B2 (`Localizacion`), C1, C8-C12,
D2/D4, E3/E5/E6, F3-F6, F8 y G1/G3/G4/G5. Quedan D1/C12 (4.2, `DialogosNulos`) y B3a (4.8,
reloj).

## Decisiones

No son errores, son deuda consciente. Siguen abiertas:

1. **Reloj** (B3, 4.8): `TimeProvider` de .NET 8 (sin paquete) o un `IClock` propio; afecta a tests de fechas y al embudo.
2. **Paginación en memoria o en SQL** (`02-DOCS/decisiones/paginacion-tabla.md`): SQL exige una columna normalizada y un `ALTER TABLE` nuevo; hoy el comportamiento está documentado y probado.
3. **`ObservableCollection<Evento>` en el POCO** (B6): mantener la excepción documentada o cambiar a `List` y refrescar el panel.
4. **`DialogosNulos` por defecto** (D1, 4.2): comodidad para tests frente a un fallback que silencia avisos.

Ya decididas:

- **`Exception?` como valor de retorno** (A8): descartado; `TryEliminarSolicitud` y `TryEscribirCsv` devuelven `bool` + `out Exception?`.
- **Raíz de composición** (B4): `App` crea la base, el servicio y el ViewModel, y los libera en `OnExit`.
- **`Helpers` depende del Núcleo** (decisión 7): resuelta en 4.6; los textos viven en `Helpers.Traducciones` y ningún `*Helper.cs` usa `Localizacion`.

## Checklist de aceptación de la revisión

- [x] El título de la gráfica no muestra ninguna clave en crudo (test de las tres granularidades).
- [x] Eliminar una fila borra todos los adjuntos que apuntaba la base, aunque se hayan quitado o sustituido sin guardar.
- [x] Hay una sola definición de «estado cerrado» y la usan modelo, consultas y estadísticas.
- [x] Ningún test escribe en `%APPDATA%` ni cambia la cultura global.
- [ ] `MainViewModel` por debajo de ~600 líneas y sin E/S de fichero (adjuntos y columnas extraídos; hoy 1085 líneas).
- [x] `Localizacion` dividida en traducciones, cultura y preferencia.
- [x] Ningún `x:Name`, `using` ni miembro público sin consumidor. Barrido del 30/10/2026: los 23 `x:Name` del XAML tienen consumidor (code-behind o `ElementName`/`TargetName`); los `using` sobrantes los vigila `IDE0005` (aviso, y los avisos son errores); los miembros públicos e internos de producción se contrastaron con el texto de `.cs` y `.xaml`: los 11 candidatos del primer barrido tienen consumidor (enlace XAML o test).
- [x] `ViewModelTests` por debajo de ~500 líneas por fichero y todos los ViewModels liberados.
- [x] `dotnet format --verify-no-changes` y `dotnet test` verdes (278 tests); `dotnet build` verde con los avisos como errores.
- [x] Ningún fuente contiene `U+FFFD`; codificación UTF-8 declarada en `.editorconfig`.
- [x] `DialogoService` y las decisiones de `02-DOCS` describen el código real.

## Anexo: comprobaciones ejecutadas

Estado a 30/10/2026, HEAD `a84f67a` en `andres/improve-code-3`:

- `dotnet --version` → `10.0.403`; `global.json` lo fija con `rollForward: latestFeature`.
- `dotnet build GestorSolicitudes.slnx` → 0 errores, 0 avisos (son errores por `Directory.Build.props`).
- `dotnet test GestorSolicitudes.slnx` → 278 correctos, 0 fallos.
- `dotnet format GestorSolicitudes.slnx --verify-no-changes` → exit 0.
- Cobertura local con `coverlet.msbuild` → 94,3 % de líneas, 78,71 % de ramas y 94 % de métodos; el CI exige 90 % de líneas.
- Auditoría de miembros (30/10/2026): barrido de `public`/`internal` en las fuentes de producción y recuento de apariciones en todos los `.cs` y `.xaml`. Los 11 sospechosos (`Altura*` de `EmbudoPeriodo`, `DiasProceso`, `NivelesInteres`, `TamanosPagina` y los cuatro booleanos de `Paginador`) tienen consumidor en un enlace XAML o en un test. No hay código muerto que borrar.
- `git status` limpio, sin ficheros sin seguimiento.
- Los issues de SonarCloud (`4a38192`, `7ac156c`) están corregidos en local; el quality gate del PR no se ha comprobado porque necesita push y análisis.
