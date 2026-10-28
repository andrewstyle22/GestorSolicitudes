# Revisión de clean code

Alcance: todo el código de producción (`*.cs`, `*.xaml`, `*.csproj`) y la suite de tests.
No se ha modificado ningún fichero: esto es solo el diagnóstico y la lista de cambios.

> Nota de verificación: en este entorno no se pudo ejecutar `dotnet build` / `dotnet test`
> (el sandbox deniega la ejecución de `dotnet.exe`). Por tanto, los hallazgos son de lectura
> de código; los que dependen de que el formateador o el analizador los detecten van marcados
> como «comprobar».

## Resumen: los cinco problemas que más pesan

1. **`MainViewModel` es una clase Dios**: 1118 líneas y ocho responsabilidades (filtros,
   paginación, estadísticas, embudo, adjuntos, CSV, diálogos y CRUD). Es el mayor incumplimiento
   de responsabilidad única del proyecto.
2. **La E/S de interfaz vive dentro del ViewModel**: `MessageBox`, `OpenFileDialog`,
   `SaveFileDialog` y `Process.Start` están en el ViewModel, así que la lógica que los rodea
   (validar, confirmar, borrar adjuntos, exportar) queda marcada `[ExcludeFromCodeCoverage]`
   y no se prueba. El propio README lo reconoce como la primera mejora que pediría un revisor.
3. **Estado global mutable y evento sin desuscripción**: `Localizacion` es un singleton estático
   con `IdiomaActual`, y `MainViewModel` se suscribe a `IdiomaCambiado` sin darse de baja nunca
   (`MainViewModel.cs:50`). Cada instancia de ViewModel (incluidas las de los tests) queda viva.
4. **Duplicación estructural**: nueve estilos de celda idénticos en el `DataGrid`, dos plantillas
   de botón copiadas, el bloque de CV y el de carta duplicados, la lista de columnas ocultables
   repetida en cuatro sitios y las fechas con formato fijo en seis lugares.
5. **Código muerto y puertas de calidad flojas**: envoltorios públicos que nadie llama, un modo
   «invertir» que ningún XAML usa, y ningún proyecto con `TreatWarningsAsErrors` ni reglas CA
   configuradas; solo StyleCop con varias familias desactivadas.

Prioridad: **A** = hacer ya (barato y alto impacto), **B** = siguiente iteración,
**C** = higiene, se puede posponer.

---

## A. Diseño y responsabilidades (SRP)

| # | Dónde | Cambio | Por qué |
|---|-------|--------|---------|
| A1 | `MainViewModel.cs:849-906` | Extraer las reglas de `Guardar` (campos obligatorios en 856, seguimiento por defecto en 868-871, hito inicial en 879-887) a `ReglasSolicitud` (Models) o `ValidadorSolicitud` (Helpers); el ViewModel solo orquesta. | Son reglas de negocio, no de presentación: así se prueban sin `MessageBox` y sin base. |
| A2 | `MainViewModel.cs:561-666` y `1100-1117` | Extraer el embudo completo (`ActualizarEmbudo`, `CalcularPeriodos`, `InicioDePeriodo`, `SiguientePeriodo`, `EtiquetaDePeriodo`, `EnviadaEnRango` y el record `EmbudoPeriodo`) a `Helpers/EmbudoHelper.cs` (el record, a `Models`). | Funciones puras de fecha con cero dependencia del ViewModel; hoy viven mezcladas con comandos y consultas. |
| A3 | `MainViewModel.cs:506-534` | Extraer `ActualizarEstadisticas` a un `EstadisticasSolicitud.Calcular(IEnumerable<Solicitud>)` que devuelva un record. | Es cálculo puro; además hoy recarga toda la tabla y vuelve a formatear textos de UI. |
| A4 | `MainViewModel.cs:1041-1097` | Extraer `ConstruirCsv` a `Helpers/CsvExportador.cs`. | 57 líneas de formateo con dos listas paralelas (cabecera y fila) dentro del ViewModel; es el sitio donde es más fácil que una columna se desalinee. |
| A5 | `MainViewModel.cs:733, 858, 896, 947, 986, 1022, 1036`; `Views/MainWindow.xaml.cs:184-200` | Introducir `IDialogoService` (mostrar error/aviso/confirmación, abrir/guardar fichero, abrir URL) con implementación en `Views` o en el núcleo, e inyectarlo. | Desacopla WPF del ViewModel y permite probar `Guardar` inválido, `Eliminar` y `ExportarCsv`. Reemplaza la decisión documentada de «MessageBox en el ViewModel» (README:197-199). |
| A6 | `MainViewModel.cs:207-290` (ocho propiedades + `Paginar` + `IrAPagina`) | Extraer a un `Paginador`/`PaginaActual` que encapsule página, tamaño, total, rango y habilitación de flechas. | La paginación es un concepto propio con su estado; el ViewModel solo debería exponerlo. |
| A7 | `MainViewModel.cs:682-706` | Sustituir el par de lambdas `obtenerRuta` / `asignar` por un descriptor `Adjunto` (ruta, nombre original, etiqueta, título del diálogo) y un único método. | Hoy hay dos lambdas por adjunto y dos comandos casi idénticos; con un descriptor, CV y carta comparten una sola ruta de código. |
| A8 | `App.xaml.cs:49-101` | Mover `VerificarColumnasFaltantes` a `Data/EsquemaDb.cs` (o `Data/InicializadorEsquema`). | Es infraestructura de datos, no arranque de la aplicación; además así se prueba sin instanciar `App`. |
| A9 | `Localizacion.cs` (972 líneas) | Separar en tres piezas: datos de traducción (`Traducciones.cs`), cultura (`CulturaApp`) y persistencia del idioma (`PreferenciaIdioma`). | Una clase hace hoy cinco cosas: tabla de textos, cultura del hilo, recursos WPF, fichero de preferencia y evento global. |
| A10 | `Solicitud.cs:105` | Cambiar `ObservableCollection<Evento>` por `List<Evento>` (o exponerla `IReadOnlyList`) y refrescar el panel al añadir/borrar. | El modelo deja de depender de una colección de UI, coherente con «POCO sin notificación»; hoy el modelo ya arrastra `System.Collections.ObjectModel`. |
| A11 | `Solicitud.cs:109,117,122,127,136`; `MainViewModel.cs:610,671,785,817,997` | Inyectar la fecha de hoy (`IClock`/`Func<DateTime>`) en vez de llamar a `DateTime.Today`. | `DateTime.Today` repartido por modelo y ViewModel hace los tests dependientes del día real y del cambio de medianoche. |

## B. Duplicación y fuente única de verdad (DRY)

| # | Dónde | Cambio | Por qué |
|---|-------|--------|---------|
| B1 | `MainViewModel.cs:130-131`, `Views/MainWindow.xaml` (`x:Name` de columnas), `Views/MainWindow.xaml.cs:63-72`, `Localizacion.cs` (`Columna.*`) | Una sola lista de descriptores de columna (clave, encabezado, binding, ocultable) y que ViewModel y vista la consuman. Añadir un test que cruce `x:Name` del XAML con la clave del ViewModel. | La misma lista está en cuatro sitios; el `switch` de la vista tiene `_ => null`, así que un nombre nuevo se ignora **en silencio** (fallback que esconde el invariante). |
| B2 | `Views/MainWindow.xaml:475-482, 491-497, 526-531, 545-550, 560-565, 575-580, 590-595, 605-611, 620-626` | Un recurso `EstiloCelda` compartido (`VerticalAlignment`, `Margin`, `TextTrimming`) y usarlo en las nueve columnas. | Nueve copias del mismo `Style`; un ajuste de alineación exige tocar nueve bloques. |
| B3 | `App.xaml:75-104` y `127-159` | `BotonDesplegable` debe derivar de `BotonBase` (`BasedOn`) y añadir solo sus triggers. | La plantilla `Border` + `ContentPresenter` + triggers está copiada palabra por palabra. |
| B4 | `Views/MainWindow.xaml:944-1001` | Extraer el bloque de CV y el de carta a un `UserControl`/`DataTemplate` parametrizado. | Dos bloques casi iguales, pero con rejillas distintas (5 columnas vs 7 y `ColumnSpan` cruzados en 981-989): la copia ya se ha desincronizado. |
| B5 | `Views/MainWindow.xaml:519,553,568,583`; `MainViewModel.cs:821`; `Views/MainWindow.xaml.cs:119` | Formato de fecha dependiente de la cultura (`StringFormat=d` o `ToString("d", Localizacion.CulturaActual)`), y una sola constante de formato para el CSV. | Hoy la UI en inglés/alemán sigue mostrando `dd/MM/yyyy`: es una fuga de localización real, no solo estética. |
| B6 | `Convertidores.cs:41-60`, `Views/MainWindow.xaml:237-249`, `App.xaml:19-31` | Una sola fuente de colores: pinceles con nombre en `App.xaml` (o un `ColoresEstado` con pinceles congelados) y que el convertidor y la leyenda los reutilicen. | Los hexadecimales están duplicados entre C# y XAML; cambiar la paleta obliga a buscar cadenas `#`. |
| B7 | `CsvHelper.cs:17-33` vs `35-44` | Separar `TextoHelper.NormalizarBusqueda` de `CsvHelper.Escapar` (o renombrar la clase a `TextoHelper`). | La clase se llama Csv pero hace normalización Unicode para el buscador; el nombre miente sobre su responsabilidad. Mantener ambos en `Helpers` respeta la regla de arquitectura. |
| B8 | `MainViewModel.cs:104,108,820,885,948`; `Views/MainWindow.xaml.cs:199` | Constantes para las claves de traducción (`ClaveError`, `ClaveEmpresaDuplicada`, etc.) y un test que verifique que toda clave usada en `.cs`/`.xaml` existe en los tres idiomas. | Las claves son literales sueltos: una errata no falla en ningún sitio, solo muestra la clave en crudo en la UI. |
| B9 | `GestorSolicitudes.Tests/TestDb.cs:12-24` | `NuevaRuta()` y `NuevaCarpeta()` son el mismo método; unificar en `NuevaCarpeta()` + `RutaDe(carpeta, "test.db")`. | Duplicación literal. |
| B10 | `VerificarColumnasTests.cs:15-30, 56-69, 81-96` | Helper `CrearTablaSolicitudesVieja(ruta, columnasExtra)` para las tres copias del esquema SQL. | El mismo `CREATE TABLE` aparece tres veces; si el esquema viejo cambia, hay que tocar tres bloques. |
| B11 | `ConvertidorTests.cs:57-74` y `84-109` | Unificar en un solo test por tabla de colores (el primero es un subconjunto del tercero). | Dos tests comprueban lo mismo con distinto nivel de detalle. |
| B12 | `SolicitudTests.cs:116-117` | Quitar el `Assert.True(s.SeguimientoPendiente);` duplicado. | Copia-pega evidente. |
| B13 | `ViewModelTests.cs` (Arrange repetido en ~15 tests) | Helper `NuevaSolicitud(empresa, puesto, …)` / `GuardarSolicitud(vm, …)`. | El mismo bloque de `Add` + `SaveChanges` + `Recargar` se repite en casi todos los tests. |

## C. Errores, estado y ciclo de vida

| # | Dónde | Cambio | Por qué |
|---|-------|--------|---------|
| C1 | `MainViewModel.cs:959-963` (`Eliminar`) y `1034` (`ExportarCsv`) | Envolver `SaveChanges` y `File.WriteAllText` igual que `Guardar`/`Adjuntar` (hoy sí capturan) y avisar por el servicio de diálogos. | Inconsistencia peligrosa: un fallo de E/S o de FK en «Eliminar» o «Exportar» tumba la app sin mensaje. |
| C2 | `AdjuntosHelper.cs:62-65`, `ColumnasHelper.cs:37-41, 55-58`, `Localizacion.cs:938-941, 959-962`, `Views/VisorAdjuntoWindow.xaml.cs:52-55` | No tragar excepciones en silencio: registrar al menos en `Debug`/un log de fichero y mantener el fallback. | Hoy un fallo de permisos o un adjunto corrupto es invisible; no hay ningún diagnóstico posible. |
| C3 | `MainViewModel.cs:50` | Desuscribirse de `Localizacion.IdiomaCambiado` (implementar `IDisposable`) o usar un evento débil. | `Localizacion` es estático: retiene para siempre cada ViewModel construido (los tests crean decenas). |
| C4 | `MainViewModel.cs:928-931` | Sustituir el bucle `Entries()` + `State = Detached` por `this.db.ChangeTracker.Clear()`. | Misma semántica, una línea, sin `using Microsoft.EntityFrameworkCore.ChangeTracking`. |
| C5 | `MainViewModel.cs:431-434` | `PuedeRetrocederUnPeriodoMas` lanza una consulta (`Min`) y calcula periodos cada vez que se lee; convertirla en un método `ActualizarLimitesEmbudo()` que se llama al cambiar granularidad/desplazamiento y guardar los booleanos. | Un getter con E/S y asignaciones sorprende al lector y se evalúa desde triggers de UI. |
| C6 | `MainViewModel.cs:466-476` y `508` | Paginar en la base (`Skip/Take` + `Count`) en lugar de traer todas las filas y recortar en memoria; `ActualizarEstadisticas` puede usar `Count()` en vez de `ToList()`. | Hoy cada tecla del buscador materializa la tabla entera dos veces (búsqueda y estadísticas). Con 200 filas se nota poco; el patrón escala mal. |
| C7 | `MainViewModel.cs:482-488` | Normalizar los campos una sola vez (o precalcular una columna de búsqueda) en lugar de cinco `NormalizarBusqueda` por fila y pulsación. | Trabajo repetido en el camino caliente del filtro. |
| C8 | `Convertidores.cs:62` | Devolver pinceles congelados y cacheados (`SolidColorBrush` estáticos o recursos), no crear uno nuevo por celda y por `Convert`. | Se instancia un `SolidColorBrush` por cada celda visible en cada repintado. |
| C9 | `EnumHelper.cs:16-27` | Cachear la descripción por valor de enum (el `GetField` por reflexión se ejecuta en cada celda y cada combo). | Reflexión en un binding de tabla. |
| C10 | `Views/MainWindow.xaml.cs:19` | `idsVencidosAvisados` no se poda nunca (y un id borrado sigue ahí). Acotar o limpiar al recargar. | Crecimiento sin límite, pequeño pero gratuito de arreglar. |
| C11 | `Views/MainWindow.xaml.cs:21, 89-95` | Constantes para `20 s` y `5 min`; liberar `flujoIcono` en `OnClosed` (o `IDisposable`). | Números mágicos y un `Stream` que no se cierra nunca. |
| C12 | `Views/VisorAdjuntoWindow.xaml.cs:63-81` | Renderizar las páginas del PDF bajo demanda (o acotar), no todas de golpe en memoria. | Un PDF de 200 páginas se carga entero como imágenes. |
| C13 | `Views/VisorAdjuntoWindow.xaml.cs:26` | `Title` se fija en el constructor, pero el XAML declara `Title="Visor"` sin localizar (`VisorAdjuntoWindow.xaml:4`). | Texto visible fuera de `Localizacion`, en contra de la convención del proyecto. |
| C14 | `Views/VisorAdjuntoWindow.xaml` | Usar los estilos y la paleta compartidos (`BotonSecundario`, `Fondo`, `Texto`, `FontFamily`) como el resto de ventanas. | Es la única ventana que no sigue el tema: se ve distinta y duplica paddings a mano. |

## D. Código muerto y API sobrante

| # | Dónde | Cambio |
|---|-------|--------|
| D1 | `ColumnasHelper.cs:20` y `:45` | `Cargar()` y `Guardar()` (envoltorios sobre `Ruta`) no se llaman en ningún sitio: o se usan desde el ViewModel o se eliminan. |
| D2 | `Convertidores.cs:26-29` | El parámetro `"invertir"` de `NuloAVisibilidadConverter` no aparecen en ningún XAML; solo lo usa un test. Quitarlo (y su test) o darle uso. |
| D3 | `Views/MainWindow.xaml` | `x:Name` que nadie lee: `TablaCandidaturas` (337), `NuevaCandidatura` (690), `Buscar_empresas` (360), `SelectorEstados` (422), `SelectorFilasPagina` (644), `RangoTexto` (652), `BotonPrimeraPagina`/`BotonPaginaAnterior`/`BotonPaginaSiguiente`/`BotonUltimaPagina` (656-674), y las columnas `Empresa` (469), `Puesto` (485), `Estado` (500), `Enviadas` (519). |
| D4 | `Views/MainWindow.xaml:728` | Atributo vacío `Cursor=""`. |
| D5 | `Views/MainWindow.xaml:7` | `Icon="/GestorSolicitudes;component/..."` vuelve a codificar el nombre del ensamblado que `MainWindow.xaml.cs:27-28` evita a propósito con `RutaIcono`. Unificar. |
| D6 | `Localizacion.cs:853` y `EnumHelper.cs:30` | Miembros públicos que solo se usan dentro (`AplicarRecursosDeApplication`, `Clave`): bajar a `private`/`internal`. |
| D7 | `GestorSolicitudes.Tests/*.cs` | `using Xunit;` es redundante con el `<Using Include="Xunit" />` del `.csproj`: eliminarlo de los ocho ficheros. |
| D8 | `GestorSolicitudes.Tests/AppDbContextTests.cs:86-90` | El test del constructor por defecto crea un contexto contra la base **real** de `%APPDATA%`; hoy solo comprueba que no es null, pero cualquier uso futuro escribiría en los datos del usuario. Borrarlo o construirlo con ruta temporal. |
| D9 | `GestorSolicitudes.Tests/TestDb.cs` | Las carpetas temporales no se borran nunca: la suite deja basura en `%TEMP%`. Añadir limpieza (`IDisposable` o `try/finally`). |

## E. Formato, nombres y organización

| # | Dónde | Cambio |
|---|-------|--------|
| E1 | `GestorSolicitudes.csproj` | Limpiar la indentación (línea 6 `<UseWPF>` sin sangrar, 16 y 33-35 con tabuladores y líneas vacías), sacar `SelfContained`/`PublishSingleFile` a un perfil de publicación y justificar `DebugType=None` (hoy no hay símbolos ni números de línea en las trazas). |
| E2 | `MainViewModel.cs:296,301,306,311,321` | Falta `this.` en las parciales `On*Changed`, a diferencia del resto del archivo. |
| E3 | `MainViewModel.cs:1071-1073` | Argumentos desalineados en `ConstruirCsv` (el `'~;'` y los dos primeros campos rompen la columna del resto). |
| E4 | `Localizacion.cs:66-67` | Dos tuplas sin sangrar respecto a sus vecinas. Comprobar si `dotnet format --verify-no-changes` lo tolera. |
| E5 | `MainViewModel.cs:599` | `PeriodosDelEmbudo` está declarado en mitad de la sección del embudo, lejos de `ClaveError`/`FormatoFecha` (23-24); subirlo con las constantes. Renombrar `FormatoFecha` a `FormatoFechaCsv`. |
| E6 | `GestorSolicitudes.Tests` | Abreviaturas y nombres poco dicientes: `Con25` (721), `s`/`db`/`vm` en tests largos, `cd`, `sb`. En el código de producción `sb` (MainViewModel.cs:1043) y `cd` (MainWindow.xaml.cs:63). |
| E7 | `GestorSolicitudes.Tests/PestanasTests.cs` | El nombre dice «pestañas» pero el test comprueba estilos, icono de bandeja y ausencia de `HeaderStyle` en el XAML; dura 85 líneas con 12 asserts. Dividir por asunto y renombrar (`EstilosAppTests`, `IconoBandejaTests`). |
| E8 | `GestorSolicitudes.Tests/ViewModelTests.cs` (882 líneas) | Partir por asunto (`CrudTests`, `FiltrosTests`, `PaginacionTests`, `EmbudoTests`, `CsvTests`). |
| E9 | `GestorSolicitudes.Tests/LocalizacionTests.cs:93-98` | `TextoDeLasEstadisticasDependenDelIdioma` es tautológico: solo interpola un literal con el propio texto que dice comprobar. Reescribirlo contra el código real de estadísticas o borrarlo. |
| E10 | `GestorSolicitudes.Tests/ViewModelTests.cs:242-244, 751, 764…` | Asserts contra textos localizados de la UI (`"50 %"`, `"3 días"`, `"1-10 de 25"`, `"Todos los estados"`): acoplan el test a las traducciones. Usar `Localizacion.Texto(...)` o comprobar el dato, no el texto. |
| E11 | `GestorSolicitudes.Tests/PestanasTests.cs:95-96` | Un test que lee el XAML como texto y busca la palabra `HeaderStyle` es frágil (comentarios, mayúsculas, reformateo). Sustituir por una comprobación estructural o por la regla real (que la columna «Días» tenga tooltip). |
| E12 | `GestorSolicitudes.Tests/AppDbContextTests.cs:17-30, 48-56` | Duplica `TestDb.NuevoContexto()` (`EnsureCreated` + ruta). Reutilizar el helper. |

## F. Puertas de calidad (barato y alto impacto)

| # | Dónde | Cambio |
|---|-------|--------|
| F1 | Nuevo `Directory.Build.props` | `TreatWarningsAsErrors`, `AnalysisLevel=latest`, `EnforceCodeStyleInBuild=true`, `Nullable=enable`, `ImplicitUsings=enable`, `Deterministic=true` para app y tests. |
| F2 | `.editorconfig` | Activar reglas CA relevantes (nullabilidad de argumentos, cultura explícita, no capturar `Exception` genérica, no usar `DateTime.Now` sin motivo), `SA1101`, `IDE0005` (usings sobrantes) y `IDE0161` (namespace de archivo). Hoy solo hay reglas de StyleCop *desactivadas*. |
| F3 | `GestorSolicitudes.csproj:24` | El `NoWarn` global de 1573/1591/1712 se mantiene por StyleCop, pero conviene comentar cada uno y comprobar que no oculta documentación que sí se quiere. |
| F4 | `.github/workflows/dotnet-desktop.yml` | El formato se verifica **después** de los tests: si falla el formato, ya se gastó la suite. Ejecutar antes `dotnet build -warnaserror` y `dotnet format --verify-no-changes`. |
| F5 | `GestorSolicitudes.Tests.csproj` | Umbral de cobertura (coverlet ya está) y excluir de ella solo lo realmente no automatizable, no la lógica que hoy se esconde tras `[ExcludeFromCodeCoverage]` (ver A5). |
| F6 | Nuevo test | Toda clave `Localizacion.Texto("…")` usada en `.cs` y `{DynamicResource …}` del XAML existe en los tres idiomas (hoy la paridad solo cubre las claves definidas, no las usadas). |
| F7 | Nuevo test | Los `x:Name` de las columnas ocultables coinciden con `ColumnasOcultables` y con las claves `Columna.*` (ver B1). |

---

## Código que hoy no se puede probar y debería

`[ExcludeFromCodeCoverage]` está en métodos que contienen lógica, no solo UI:

- `MainViewModel.AdjuntarCv` / `AdjuntarCarta` (681-700): el flujo de copiar/borrar/asignar.
- `MainViewModel.Eliminar` (939-968): borrado + limpieza de adjuntos.
- `MainViewModel.ExportarCsv` (1019-1039): escritura del fichero y mensaje.
- `MainViewModel.CuandoCambiaIdioma` (175-202): refresco de combos, lista, gráfica.

Con A5 (servicio de diálogos) los tres primeros pasan a ser probables; el cuarto necesita poder cambiar el idioma sin tocar el estado global (A9/C3).

## Orden recomendado

**Fase 0 — gratuita (unas horas).** F1-F4, D1-D9, E1-E4. Sin cambios de comportamiento, deja el listón automático alto y quita ruido.

**Fase 1 — extraer lo puro (1 día).** A2, A3, A4, B7, B8 y sus tests (`EmbudoHelper`, estadísticas, exportador CSV). No toca la UI ni el comportamiento; reduce `MainViewModel` en ~350 líneas.

**Fase 2 — desacoplar la UI (1-2 días).** A5, C1-C3, quitar `[ExcludeFromCodeCoverage]` de lo que ya se prueba, tests de `Guardar` inválido, `Eliminar`, `ExportarCsv` y error de guardado.

**Fase 3 — dividir el resto (1-2 días).** A1, A6, A7, A8, A10, B1-B4, C5-C9.

**Fase 4 — gusto (cuando toque).** A9 (Localizacion), A11 (reloj), B5-B6, C10-C14, E5-E12.

## Decisiones documentadas que este plan revisa

No son «errores», son deuda consciente del README/AGENTS; conviene decidirlas explícitamente antes de tocarlas:

- `MessageBox` en el ViewModel (README:197-199) → A5.
- Entidades POCO y `Edicion = null; Edicion = actual;` (README:178-182) → A10 lo aliviaría, pero cambia el patrón.
- Un solo `AppDbContext` durante la sesión (README:172-176) → C6 (paginación en SQL) obliga a revisarlo.
- `EnsureCreated` + `ALTER TABLE` (README:184-195) → A8 solo lo mueve; migrar es otro trabajo.
- `CsvHelper.NormalizarBusqueda` en `Helpers` (AGENTS.md) → B7 mantiene la capa, solo separa responsabilidades.
