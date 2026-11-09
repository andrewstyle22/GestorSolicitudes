# AGENTS.md

Aplicación WPF en .NET 8 (C#) escrita íntegramente en español: comentarios, mensajes de commit, textos de UI. No escribas en inglés salvo que se pida. Stack: MVVM con EF Core + SQLite y xUnit, nada más — no hay ASP.NET, ni TypeScript, ni Node.js; no los propongas.

Referencia completa: `README.md` (features, decisiones de diseño, lecciones aprendidas). Léelo antes de tocar código; este fichero solo resume lo que no se adivina.

## Comandos

- Compilar + tests: `dotnet test`
- Formato (lo exige el CI): `dotnet format GestorSolicitudes.slnx --verify-no-changes` — deja el código ya formateado con `dotnet format`
- Ejecutar: `dotnet run` (solo Windows; TFM `net8.0-windows10.0.19041`)
- La solución usa el formato nuevo `.slnx`, no `.sln`

## Arquitectura (obligatoria: la vigila ArchUnitNET)

Los ficheros viven físicamente en la raíz del proyecto, pero el **namespace** marca la capa. Las capas solo dependen "hacia abajo": `Models`, `Data`, `Helpers`, `Converters` → `ViewModels` → `Views` (`GestorSolicitudes.Tests/ArquitecturaTests.cs`). El namespace raíz `GestorSolicitudes` (App, Localizacion) es el "Núcleo": es el único que construye Vistas; los ViewModels tienen prohibido tocar `Views` y `Converters`. Los diálogos/avisos van por `IDialogoService` (ver Convenciones): un `MessageBox` en un ViewModel, o un using hacia una capa superior, reventará los tests.

Regla práctica: un helper nuevo va en `namespace GestorSolicitudes.Helpers` (no `GestorSolicitudes`). Los estáticos puros viven en el helper que les toca (Helpers no puede depender de ViewModels): `TextoHelper.NormalizarBusqueda` —minúsculas sin tildes— está ahí, y no en el ViewModel, porque es lógica de texto reutilizable; el filtro de búsqueda la llama desde `Recargar` y `TextoHelper.Escapar` la usa la exportación.

## Convenciones que rompen el default de .NET

- **Los diálogos van por `IDialogoService`** (`ViewModels`), que implementa `DialogoService` (`Views`). El ViewModel nunca llama a `MessageBox`, `OpenFileDialog`, `SaveFileDialog` ni `Process.Start`: se le inyecta el servicio (en los tests, un falso). Un test de fuentes lo vigila.
- **Entidades POCO sin `INotifyPropertyChanged`**: el panel de detalle edita un búfer (`Editor`, `SolicitudEdicionViewModel`) y la entidad solo se escribe al pulsar Guardar (`Editor.AplicarA`). La UI no bindea contra la entidad: no expongas `Solicitud` para editar.
- **Un solo `AppDbContext` durante toda la sesión** (explota el change tracking de EF).
- **Los modelos no exponen setters públicos** (lo vigila un test por reflexión): `Solicitud` y `Evento` solo cambian por métodos de negocio (`Actualizar…`, `CambiarEstado`, `Reabrir`, `AgregarEvento`…) y EF escribe por campo.
- **Añadir una propiedad a `Solicitud`**: no hay migraciones. Su `set` es privado, así que se escribe desde un método de negocio de `Solicitud` (con su constante `LongitudMaximaX` si es texto, compartida por `[MaxLength]` y la validación). Hay que registrarla en los arrays de `VerificarColumnasFaltantes` (`EsquemaDb.cs`, namespace `Data`), añadirla al editor (`CargarDesde`, su `Aplicar…` y la validación de longitud) y darle test y traducción si se ve. Los enums se guardan como entero.
- **XAML nuevo**: el csproj declara las páginas a mano (`EnableDefaultPageItems=false`): hay que añadir el `<Page>` al csproj o no se compila a BAML y la app falla buscando el recurso.

## Decisiones vigentes (etapa R: estados y reapertura)

Resumen de `02-DOCS/decisiones/estados-y-reapertura.md`:

- Estados: no se añade ninguno. Abiertos: Enviada, En revisión, Prueba técnica, Entrevista RR. HH., Entrevista técnica, Entrevista final, Oferta recibida, Sin respuesta. Cerrados: Oferta aceptada, Oferta rechazada por mí, Descartado por la empresa, Retirada por mí. "Reabierta" es una acción, no un estado.
- Transiciones: cerrado → abierto solo por `Solicitud.Reabrir` (nunca por el combo de estado). Hacia un cierre negativo (Descartado, Retirada, Oferta rechazada por mí): motivo obligatorio y permitido; `FechaCierre` = hoy si está vacía; al pasar a Descartado se añade hito `Rechazo`. Hacia Oferta aceptada: `FechaCierre` = hoy si está vacía, sin motivo. Cerrado → cerrado es corrección (motivo no permitido se reemplaza por `NoIndicado`). "Sin respuesta" → otro estado no es reapertura: se rellena `FechaPrimeraRespuesta` y se añade hito `Respuesta`. "Oferta rechazada por mí" → "Oferta aceptada" prohibida: reabrir a "Oferta recibida" y luego aceptar. Oferta aceptada → cualquier otro: confirmación reforzada.
- Motivo de cierre: obligatorio en los tres cierres negativos. `MotivoRechazo` se amplía al final con `NoIndicado` (6), `AceptoOtraOferta` (7), `ModalidadOUbicacion` (8), `YaNoMeInteresa` (9); el nombre del enum y la propiedad no cambian. El desplegable se filtra por estado y el editor preselecciona `NoIndicado`; al salir de esos estados se limpia. Bases existentes: `EsquemaDb` rellena `MotivoRechazo = 6` donde falte (idempotente). `CambiarEstado` lanza `ArgumentException` si el motivo no es válido para el estado; fuera de los tres cierres negativos lo deja en `null`.
- Reapertura: `Solicitud.Reabrir(nuevoEstado, nota, proximoSeguimiento)`. `FechaReapertura = hoy` (solo la última, sin contador), `FechaCierre = null`, `MotivoRechazo = null`, `FechaSolicitud` no cambia. `nota` obligatoria (máx. 300, campo `NotaReapertura` del editor, guardada solo en el hito `Reapertura`). `ProximoSeguimiento`: respeta lo editado en el mismo guardado; si no, `null` y la regla propone +7 días. `Reabrir` no admite "Sin respuesta". Si el guardado falla tras `Reabrir`, el editor se recarga desde la entidad.
- `FechaReapertura` columna `TEXT NULL` + `[NotMapped] FueReabierta`. `DiasProceso` cuenta desde `FechaReapertura ?? FechaSolicitud`. Insignia ↻ y borde magenta `#DB2777` en la fila.
- Desde cualquier estado abierto se puede pasar a Oferta aceptada u Oferta rechazada por mí: no se exige pasar por «Oferta recibida».
- `RespuestaEmpresa` y `Notas` son texto libre sin límite.
- Los hitos que crea el modelo (`Reapertura`, `Respuesta`, `Rechazo`) guardan su texto en castellano: solo se traduce el tipo. El prefijo del hito de reapertura es una constante compartida con la franja de Seguimiento. Guardar valores únicos y traducir al mostrar es otra refactorización.

## UI y localización

- Todo texto visible va por `Localizacion.Texto("Clave")`; la clave hay que añadirla en los tres idiomas (es/en/de) en `Traducciones.cs` (`Helpers`) o un test de paridad falla.
- Los enums llevan `[Description]` (texto de UI); `EnumHelper` traduce y cae a `[Description]` si falta la clave.
- Los cambios de UI/columnas se acompañan de su test (hay tests de pestañas, columnas, filtros, botones).

## StyleCop

- Reglas de documentación desactivadas en `.editorconfig` (no se exige XML doc ni cabecera de copyright). No añadas documentación obligatoria.

## Tests

- xUnit + ArchUnitNET. Nunca toques `%APPDATA%`: usa los helpers de `TestDb.cs` (`NuevaRuta()`, `NuevaCarpeta()`, `NuevoContexto()`) con SQLite temporal.
- Todo cambio de función (crear/editar/eliminar) obliga a su test: carga la skill `tests-obligatorios` (`.opencode/skills/`) y deja `dotnet test` en verde.
- Tests que dependen del dispatcher WPF usan `[StaFact]` (Xunit.StaFact).
- Los tests ven miembros `internal` vía `InternalsVisibleTo("GestorSolicitudes.Tests")`.
- El CI corre en `windows-latest`, incluido el análisis de SonarCloud.

## Skills (`.opencode/skills/`)

- **`tests-obligatorios`**: obligatoria al crear, editar o borrar una función. Deja `dotnet test` en verde.
- **`sonarcloud-gate`**: obligatoria antes de commitear, subir o abrir un PR con cambios de código, aunque la tarea no mencione Sonar. Resume las condiciones del gate y los patrones que ya lo han tumbado (S2930, S6444, S6966, S1117, S1192, S3267...). No esperes a que SonarCloud marque el issue en el PR.
