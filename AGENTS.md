# AGENTS.md

Aplicación WPF (.NET 8) escrita íntegramente en español: comentarios, mensajes de commit, textos de UI. No escribas en inglés salvo que se pida.

Referencia completa: `README.md` (features, decisiones de diseño, lecciones aprendidas). Léelo antes de tocar código; este fichero solo resume lo que no se adivina.

## Comandos

- Compilar + tests: `dotnet test`
- Formato (lo exige el CI): `dotnet format GestorSolicitudes.slnx --verify-no-changes` — deja el código ya formateado con `dotnet format`
- Ejecutar: `dotnet run` (solo Windows; TFM `net8.0-windows10.0.19041`)
- La solución usa el formato nuevo `.slnx`, no `.sln`

## Arquitectura (obligatoria: la vigila ArchUnitNET)

Los ficheros viven físicamente en la raíz del proyecto, pero el **namespace** marca la capa. Las capas solo dependen "hacia abajo": `Models`, `Data`, `Helpers`, `Converters` → `ViewModels` → `Views` (`GestorSolicitudes.Tests/ArquitecturaTests.cs`). El namespace raíz `GestorSolicitudes` (App, Localizacion) es el "Núcleo": es el único que construye Vistas; los ViewModels tienen prohibido tocar `Views` y `Converters`. Un `MessageBox` nuevo en un ViewModel o un using hacia una capa superior reventará los tests.

Regla práctica: un helper nuevo va en `namespace GestorSolicitudes.Helpers` (no `GestorSolicitudes`). Los estáticos puros de CSV viven en `CsvHelper` (no en el ViewModel): `CsvHelper.NormalizarBusqueda` está ahí —y no en el ViewModel— porque `NormalizarCabecera` la necesita y Helpers no puede depender de ViewModels; el filtro de búsqueda la reutiliza desde `Recargar`.

## Convenciones que rompen el default de .NET

- **`MessageBox` vive deliberadamente en el ViewModel** (MVVM no ortodoxo; ver README «Decisiones»).
- **Entidades POCO sin `INotifyPropertyChanged`**: tras guardar se recarga la lista entera y el panel se repinta con `Edicion = null; Edicion = actual;`. No lo "arregles" sin consultar.
- **Un solo `AppDbContext` durante toda la sesión** (explota el change tracking de EF).
- **Añadir una propiedad a `Solicitud`**: no hay migraciones. Hay que registrar la columna en los arrays de `VerificarColumnasFaltantes` (App.xaml.cs) o la app revienta con bases ya creadas por versiones antiguas. Los enums se guardan como entero.
- **XAML nuevo**: el csproj declara las páginas a mano (`EnableDefaultPageItems=false`): hay que añadir el `<Page>` al csproj o no se compila a BAML y la app falla buscando el recurso.

## UI y localización

- Todo texto visible va por `Localizacion.Texto("Clave")`; la clave hay que añadirla en los tres idiomas (es/en/de) en `Localizacion.cs` o un test de paridad falla.
- Los enums llevan `[Description]` (texto de UI); `EnumHelper` traduce y cae a `[Description]` si falta la clave.
- Los cambios de UI/columnas se acompañan de su test (hay tests de pestañas, columnas, filtros, botones).

## StyleCop

- Reglas de documentación desactivadas en `.editorconfig` (no se exige XML doc ni cabecera de copyright). No añadas documentación obligatoria.

## Tests

- xUnit + ArchUnitNET. Nunca toques `%APPDATA%`: usa los helpers de `TestDb.cs` (`NuevaRuta()`, `NuevaCarpeta()`, `NuevoContexto()`) con SQLite temporal.
- Todo cambio de función (crear/editar/eliminar) obliga a su test: carga la skill `tests-obligatorios` (`.opencode/skills/`) y deja `dotnet test` en verde.
- Tests que dependen del dispatcher WPF usan `[StaFact]` (Xunit.StaFact).
- Los tests ven miembros `internal` vía `InternalsVisibleTo("GestorSolicitudes.Tests")`.
- El CI corre en `windows-latest`; SonarCloud, aparte, en `ubuntu-24.04`.