# AGENTS.md

Aplicación WPF en .NET 8 (C#) escrita íntegramente en español: comentarios, mensajes de commit, textos de UI. No escribas en inglés salvo que se pida. Stack: MVVM con EF Core + SQLite y xUnit, nada más — no hay ASP.NET, ni TypeScript, ni Node.js; no los propongas.

Referencia completa: `README.md` (features, decisiones de diseño, lecciones aprendidas). Léelo antes de tocar código; este fichero solo resume lo que no se adivina.

## Comandos

- Compilar + tests: `dotnet test`
- Formato (lo exige el CI): `dotnet format GestorSolicitudes.slnx --verify-no-changes` — deja el código ya formateado con `dotnet format`
- Ejecutar: `dotnet run` (solo Windows; TFM `net8.0-windows10.0.19041`)
- La solución usa el formato nuevo `.slnx`, no `.sln`

## Arquitectura (obligatoria: la vigila ArchUnitNET)

Los ficheros viven físicamente en la raíz del proyecto, pero el **namespace** marca la capa. Las capas solo dependen "hacia abajo": `Models`, `Data`, `Helpers`, `Converters` → `ViewModels` → `Views` (`GestorSolicitudes.Tests/ArquitecturaTests.cs`). El namespace raíz `GestorSolicitudes` (App, Localizacion, ManejadorErrores, MigradorAdjuntos) es el "Núcleo": es el único que construye Vistas; los ViewModels tienen prohibido tocar `Views` y `Converters`. Los diálogos/avisos van por `IDialogoService` (ver Convenciones): un `MessageBox` en un ViewModel, o un using hacia una capa superior, reventará los tests. Lo que combina base de datos con registro o diálogos (`ManejadorErrores`, `MigradorAdjuntos`) vive en el Núcleo, no en `Helpers`.

Regla práctica: un helper nuevo va en `namespace GestorSolicitudes.Helpers` (no `GestorSolicitudes`). Los estáticos puros viven en el helper que les toca (Helpers no puede depender de ViewModels): `TextoHelper.NormalizarBusqueda` —minúsculas sin tildes— está ahí, y no en el ViewModel, porque es lógica de texto reutilizable; el filtro de búsqueda la llama desde `Recargar` y `TextoHelper.Escapar` la usa la exportación.

## Convenciones que rompen el default de .NET

- **Los diálogos van por `IDialogoService`** (`ViewModels`), que implementa `DialogoService` (`Views`). El ViewModel nunca llama a `MessageBox`, `OpenFileDialog`, `SaveFileDialog` ni `Process.Start`: se le inyecta el servicio (en los tests, un falso). Un test de fuentes lo vigila.
- **Entidades POCO sin `INotifyPropertyChanged`**: el panel de detalle edita un búfer (`Editor`, `SolicitudEdicionViewModel`) y la entidad solo se escribe al pulsar Guardar (`Editor.AplicarA`). La UI no bindea contra la entidad: no expongas `Solicitud` para editar.
- **Un solo `AppDbContext` durante toda la sesión** (explota el change tracking de EF).
- **Los modelos no exponen setters públicos** (lo vigila un test por reflexión): `Solicitud` y `Evento` solo cambian por métodos de negocio (`Actualizar…`, `CambiarEstado`, `Reabrir`, `AgregarEvento`…) y EF escribe por campo.
- **Añadir una propiedad a `Solicitud`**: no hay migraciones. Su `set` es privado, así que se escribe desde un método de negocio de `Solicitud` (con su constante `LongitudMaximaX` si es texto, compartida por `[MaxLength]` y la validación). Hay que registrarla en los arrays de `VerificarColumnasFaltantes` (`EsquemaDb.cs`, namespace `Data`); si se olvida, el test `VerificarColumnas_CubreTodasLasColumnasDelModelo` falla. Añadirla al editor (`CargarDesde`, su `Aplicar…` y la validación de longitud) y darle test y traducción si se ve. Los enums se guardan como entero. Un BLOB no va en `Solicitud`: va en `DocumentosSolicitud` y se lee solo al abrir.
- **XAML nuevo**: el csproj declara las páginas a mano (`EnableDefaultPageItems=false`): hay que añadir el `<Page>` al csproj o no se compila a BAML y la app falla buscando el recurso.

## Documentos, errores e instancia única

- **Documentos en la base de datos:** `DocumentosSolicitud` (namespace `Models`) guarda el CV y la carta como BLOB, una fila por candidatura, sin navegación desde `Solicitud`. `Solicitud` solo conserva el nombre original, y `TieneCv`/`TieneCarta` salen de él. La fila se lee solo al abrir el documento y el contenido pendiente vive en el editor (`CvContenido`, `CartaContenido`) hasta que `Guardar` lo escribe; si el guardado falla, el búfer lo conserva. La fila de `DocumentosSolicitud` se crea después del primer `SaveChanges` (hace falta el `Id`).
- **Límite de 2 MB:** `AdjuntosHelper.TamanoMaximoBytes` es la única fuente; `LectorAdjuntos.Leer` mide antes de reservar memoria y el ViewModel muestra el aviso.
- **Abrir un documento:** `TemporalesAdjuntos` lo copia a `%TEMP%\GestorSolicitudes\adjuntos` y llama al visor; la carpeta se limpia al arrancar y al cerrar solo por la copia principal (`LimpiarSiEsPrimera`), para no borrar lo que otra instancia tiene abierto. El visor no vuelca a texto los tipos desconocidos y acota DOCX/ODT a 10 MB.
- **Migración:** `MigradorAdjuntos` corre al arrancar tras `EsquemaDb`, es idempotente, ignora bases sin `RutaCv`/`RutaCarta`, registra los fallos y borra del disco solo los ficheros ya copiados a la base. Solo migra ficheros dentro de la carpeta de adjuntos y de hasta 2 MB; el resto queda pendiente con su ruta intacta, se registra y se reintenta en el siguiente arranque (los grandes, con un aviso agrupado por arranque).
- **Instancia única:** `InstanciaUnica` se crea antes de abrir la base; la segunda copia avisa a la primera, se cierra sin abrir base ni ventana, y se libera en `OnExit`.
- **Errores:** `RegistroErrores` escribe en `%APPDATA%\GestorSolicitudes\errores.log`, nunca lanza y se recorta a 1 MB; `ManejadorErrores.Avisar` registra y avisa por `IDialogoService`. El registro incluye rutas absolutas y, en los fallos de EF, el SQL.

## Decisiones vigentes (etapa R: estados y reapertura)

Resumen de `02-DOCS/decisiones/estados-y-reapertura.md`:

- Estados: no se añade ninguno. Abiertos: Enviada, En revisión, Prueba técnica, Entrevista RR. HH., Entrevista técnica, Entrevista final, Oferta recibida, Sin respuesta. Cerrados: Oferta aceptada, Oferta rechazada por mí, Descartado por la empresa, Retirada por mí. "Reabierta" es una acción, no un estado.
- Transiciones: cerrado → abierto solo por `Solicitud.Reabrir` (nunca por el combo de estado). Hacia un cierre negativo (Descartado, Retirada, Oferta rechazada por mí): motivo obligatorio y permitido; `FechaCierre` = hoy si está vacía; al pasar a Descartado se añade hito `Rechazo`. Hacia Oferta aceptada: `FechaCierre` = hoy si está vacía, sin motivo. Cerrado → cerrado es corrección (motivo no permitido se reemplaza por `NoIndicado`). "Sin respuesta" → otro estado no es reapertura: si el nuevo estado cuenta como respuesta (ver etapa 5), se rellena `FechaPrimeraRespuesta` y se añade hito `Respuesta`. "Oferta rechazada por mí" → "Oferta aceptada" prohibida: reabrir a "Oferta recibida" y luego aceptar. Oferta aceptada → cualquier otro: confirmación reforzada.
- Motivo de cierre: obligatorio en los tres cierres negativos. `MotivoRechazo` se amplía al final con `NoIndicado` (6), `AceptoOtraOferta` (7), `ModalidadOUbicacion` (8), `YaNoMeInteresa` (9); el nombre del enum y la propiedad no cambian. El desplegable se filtra por estado y el editor preselecciona `NoIndicado`; al salir de esos estados se limpia. Bases existentes: `EsquemaDb` rellena `MotivoRechazo = 6` donde falte (idempotente). `CambiarEstado` lanza `ArgumentException` si el motivo no es válido para el estado; fuera de los tres cierres negativos lo deja en `null`.
- Reapertura: `Solicitud.Reabrir(nuevoEstado, nota, proximoSeguimiento)`. `FechaReapertura = hoy` (solo la última, sin contador), `FechaCierre = null`, `MotivoRechazo = null`, `FechaSolicitud` no cambia. `nota` obligatoria (máx. 300, campo `NotaReapertura` del editor, guardada solo en el hito `Reapertura`). `ProximoSeguimiento`: respeta lo editado en el mismo guardado; si no, `null` y la regla propone +7 días. `Reabrir` no admite "Sin respuesta". Si el guardado falla, el editor se recarga desde la entidad (no solo tras reabrir).
- `FechaReapertura` columna `TEXT NULL` + `[NotMapped] FueReabierta`. `DiasProceso` cuenta desde `FechaReapertura ?? FechaSolicitud`. Insignia ↻ y borde magenta `#DB2777` en la fila.
- Desde cualquier estado abierto se puede pasar a Oferta aceptada u Oferta rechazada por mí: no se exige pasar por «Oferta recibida».
- `RespuestaEmpresa` y `Notas` son texto libre sin límite.
- Los hitos que crea el modelo (`Reapertura`, `Respuesta`, `Rechazo`) guardan su texto en castellano: solo se traduce el tipo. El prefijo del hito de reapertura es una constante compartida con la franja de Seguimiento. Guardar valores únicos y traducir al mostrar es otra refactorización.

## Decisiones vigentes (etapa 5: correcciones)

- **Cambios sin guardar:** `ResolverCambiosPendientes()` pregunta Guardar/Descartar/Cancelar al cambiar de fila, pulsar *Nueva* o *Duplicar* y cerrar la ventana. Con Guardar, la acción pedida sigue solo si `TryGuardar()` fue bien; Descartar hace la misma limpieza que *Cancelar*. Todo `Persistir()` fallido recarga el editor desde la entidad.
- **CSV:** `ExportarCsv` pregunta el separador (`;`, `,` o tabulador) antes del destino y lo recuerda en `%APPDATA%\GestorSolicitudes\csv.txt` (`PreferenciaCsv`; `EsValido` es la única lista y el patrón es el de `PreferenciaIdioma`). `CsvExportador.Construir` recibe el separador (`;` por defecto) e incluye motivo de cierre y reapertura. `TextoHelper.Escapar` antepone `'` a los textos que empiezan por `=`, `+`, `-`, `@`, tabulador, retorno de carro o salto de línea; fechas y números no pasan por él. El ViewModel recibe la ruta de la preferencia en el constructor para no tocar `%APPDATA%` en los tests.
- **Fecha de solicitud:** cambiarla mueve el hito `SolicitudEnviada` y el seguimiento propuesto solo si coincidían con la fecha anterior; el seguimiento no se mueve con reapertura. Se aplica en `ActualizarFechas` y en `OnFechaSolicitudChanged`.
- **Respuesta de la empresa:** `EsRespuestaDeLaEmpresa` cuenta los estados abiertos salvo «Enviada» y «Sin respuesta», más «Descartado por la empresa», «Oferta aceptada» y «Oferta rechazada por mí». Solo «Retirada por mí» no cuenta. Los datos antiguos no se corrigen.
- **Descripción de eventos:** `Evento.LongitudMaximaDescripcion` (500) es la única fuente del `[MaxLength]` y de la validación del búfer; `Validar()` incluye los eventos.
- **Hito `Rechazo`:** se fecha con la `FechaCierre` resultante, no con hoy.

## Decisiones vigentes (deliberadas)

Ninguna auditoría debe «arreglar» lo siguiente sin una razón nueva:

- **`TextoHelper.Escapar`:** decide el apóstrofo con el primer carácter visible del valor original, y `\r` y `\n` ya se sustituyen en la salida. El salto de línea (`\r` o `\n`) también dispara el apóstrofo: tras sustituirse por un espacio dejaría la fórmula como primer carácter visible de la celda.
- **`PreferenciaCsv.LeerDe`:** no recorta la cadena; el tabulador es un separador válido.
- **Duplicar:** no copia los adjuntos ni sus nombres; copiar el nombre dejaría `TieneCv` en verdadero sin fila en `DocumentosSolicitud`.
- **`Recargar`:** lee toda la tabla y los filtros son en memoria; con cientos de filas es suficiente y conserva la identidad de las entidades rastreadas.
- **`ColumnasHelper.CargarDe`:** ignora las claves desconocidas a propósito.
- **`TryGuardar`:** recarga el editor desde la entidad tras un fallo; así conserva las ediciones y el contenido pendiente de los documentos.
- **XAML:** se declaran a mano en el csproj; `FuentesTests` falla si falta alguno.
- **Esquema:** se mantiene con `EsquemaDb` y no con migraciones; el test de esquema evita olvidos.
- **Enlaces:** solo se abren `http`/`https` sin credenciales embebidas; al shell se le pasa `AbsoluteUri`.
- **Modelo de amenaza:** el de `README.md` (app local de un solo usuario, datos en `%APPDATA%` Roaming con ACL heredadas, registro con rutas y SQL, uniones de directorio fuera del modelo, temporales hasta salir). No añadas defensas para lo que ese modelo deja fuera sin una razón nueva.

## UI y localización

- Todo texto visible va por `Localizacion.Texto("Clave")`; la clave hay que añadirla en los tres idiomas (es/en/de) en `Traducciones.cs` (`Helpers`) o un test de paridad falla.
- Los enums llevan `[Description]` (texto de UI); `EnumHelper` traduce y cae a `[Description]` si falta la clave.
- Los cambios de UI/columnas se acompañan de su test (hay tests de pestañas, columnas, filtros, botones).

## StyleCop

- Reglas de documentación desactivadas en `.editorconfig` (no se exige XML doc ni cabecera de copyright). No añadas documentación obligatoria.

## Tests

- xUnit + ArchUnitNET. Nunca toques `%APPDATA%` ni `%TEMP%`: usa los helpers de `TestDb.cs` (`NuevaRuta()`, `NuevaCarpeta()`, `NuevoContexto()`) con SQLite temporal, y las sobrecargas con ruta de `RegistroErrores` y `TemporalesAdjuntos`.
- Todo cambio de función (crear/editar/eliminar) obliga a su test: carga la skill `tests-obligatorios` (`.opencode/skills/`) y deja `dotnet test` en verde.
- Tests que dependen del dispatcher WPF usan `[StaFact]` (Xunit.StaFact).
- Los tests ven miembros `internal` vía `InternalsVisibleTo("GestorSolicitudes.Tests")`.
- El CI corre en `windows-latest`, incluido el análisis de SonarCloud.

## Skills (`.opencode/skills/`)

- **`tests-obligatorios`**: obligatoria al crear, editar o borrar una función. Deja `dotnet test` en verde.
- **`sonarcloud-gate`**: obligatoria antes de commitear, subir o abrir un PR con cambios de código, aunque la tarea no mencione Sonar. Resume las condiciones del gate y los patrones que ya lo han tumbado (S2930, S6444, S6966, S1117, S1192, S3267...). No esperes a que SonarCloud marque el issue en el PR.
- **`security-audit`**: la de Cloudflare (https://github.com/cloudflare/security-audit-skill), copiada en `.opencode/skills/security-audit`. Solo para auditorías de seguridad explícitas o preguntas de seguridad; no la ejecutes por tu cuenta. Necesita `node` (solo para sus validadores `.cjs`, no es stack del proyecto). Los dos validadores están parcheados en local para Windows (no tienen `O_NOFOLLOW` ni `O_NONBLOCK`): si actualizas con `npx skills add`, vuelve a aplicar el respaldo `win32` de `readFileWithinLimit` o los validadores rechazarán toda entrada.
