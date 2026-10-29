---
name: sonarcloud-gate
description: 'Aplica cuando se trabaja sobre el análisis de SonarCloud de este repo (GestorSolicitudes): dejar en verde el Quality Gate, subir la cobertura de código nuevo o resolver issues nuevos del análisis. Gatillos: "sonarcloud", "quality gate", "cobertura", "coverage", "gate en rojo", "código nuevo", "issue de sonar", "análisis de sonar". El CI corre SonarCloud aparte (windows-latest para build/tests; SonarCloud sobre la rama del PR).'
---

# Dejar el Quality Gate de SonarCloud en verde

Este repo se analiza en SonarCloud (proyecto `andrewstyle22_GestorSolicitudes`).
El gate que hay que pasar en un PR es **"Sonar way"**, y en el análisis de un
pull request **solo se evalúan las condiciones sobre código nuevo**:

- **New Code Coverage ≥ 80%** ← la condición que suele fallar.
- New Code Duplication ≤ 3%.
- Ratings de reliability, security y maintainability = A (0 bugs/vulnerabilidades
  nuevos; deuda limitada).
- 100% de security hotspots nuevos revisados.
- *Fudge factor*: si el código nuevo tiene menos de 20 líneas, las condiciones de
  cobertura y duplicación se ignoran.

## Cómo mide Sonar la cobertura

`coverage = (CT + LC) / (B + EL)`, donde LC son líneas cubiertas, EL líneas
ejecutables, CT condiciones evaluadas a true al menos una vez y B condiciones
totales. **El gate mezcla líneas y condiciones**: una condición sin cubrir pesa
igual que una línea sin cubrir.

"Código nuevo" = líneas añadidas respecto al rama objetivo del PR (el merge-base).
**Una línea modificada del diff cuenta; una línea de método antiguo que no está en
el diff, no.** La cobertura global (`coverage` a secas) NO la evalúa el gate.

## Qué fracasa antes de empezar: medición local ≠ Sonar

La medición local del diff suele dar ~5-20 puntos por encima de la de Sonar
(CPU/aleatorio), porque Sonar cuenta líneas de regiones reescritas y condiciones
que las herramientas locales ignoran. Nunca des el gate por bueno con la medición
local; úsala solo como estimación rápida.

## Receta (lo que ha funcionado en este repo)

El cuello de botella de este repo es la cobertura de `MainViewModel`: ahí vive el
`MessageBox` deliberadamente (MVVM no ortodoxo; ver README y AGENTS.md), así que
**toda rama de diálogo/aviso es intesteable**. Estrategia en orden:

1. **Extraer la lógica a métodos `internal` o `internal static`** del ViewModel
   (los tests ven `internal` vía `InternalsVisibleTo("GestorSolicitudes.Tests")`,
   no hace falta hacerlos `public`) y **cubrirlos con tests xUnit**. Ejemplos
   hechos: `ConstruirCsv`, `CargarColumnas`/`GuardarColumnas`, `EnviadaEnMes`.
   Los estáticos puros que no necesiten el
   ViewModel van en `TextoHelper` (Helpers no puede depender de ViewModels).
2. **`[ExcludeFromCodeCoverage]` en métodos 100% de diálogo/MessageBox**
   (`OpenFileDialog`, `SaveFileDialog`, `Process.Start`...). Es el mecanismo que
   Sonar documenta para código no cubrible: coverlet lo saca del reporte y Sonar
   lo descuenta del denominador. **Regla dura: solo si TODAS sus líneas están sin
   cubrir.** Si el método tiene línea cubierta (p.ej. un `catch` dentro de un
   método que sí se prueba) NO se excluye: se borrarían líneas cubiertas del
   numerador y la cobertura bajaría.
3. **Comprobar el resultado con la API de SonarCloud** (es pública, sin token):
   `https://sonarcloud.io/api/measures/search?projectKeys=andrewstyle22_GestorSolicitudes&metricKeys=new_coverage,new_uncovered_lines,new_uncovered_conditions,new_lines_to_cover,new_conditions_to_cover&branch=<rama>`.
   Ojo: son métricas de la **rama**; la vista de PR de la UI puede mostrar una
   décima o puntos distintos. La que decide es la del PR.
4. **Después del push, el análisis tarda varios minutos** en materializarse; la
   API sigue mostrando valores antiguos mientras tanto. No hay que dar el gate por
   rojo ni por verde hasta que cambie `new_coverage`.

## Verificación local antes de cada commit

- `dotnet test` en verde (xUnit; tests del ViewModel con
  `using var db = TestDb.NuevoContexto();`, nunca tocar `%APPDATA%`).
- `dotnet format GestorSolicitudes.slnx --verify-no-changes` (si se tocó formato).
- Cobertura local de código nuevo: `dotnet test /p:CollectCoverage=true
  /p:CoverletOutput="<temp>\coverage" /p:CoverletOutputFormat=opencover` y filtrar
  las líneas `+` del `git diff <merge-base> HEAD` contra el `coverage.opencover.xml`
  (sumatorio de líneas y de `bec`/`bev` por línea nueva).
- **Comprueba que el reporte no está vacío**: si el XML pesa unos cientos de bytes y
  el `Summary` trae `numSequencePoints="0"`, coverlet no ha podido mapear ninguna
  línea. No es que los tests no cubran: es que falta el PDB (ver más abajo).
- Tests que dependen del dispatcher WPF: `[StaFact]` (Xunit.StaFact). `[StaFact]`
  y `[Theory]` no pueden coexistir en el mismo test (xUnit1002).

## Cobertura a 0,0% en todos los ficheros

Si Sonar muestra `0.0%` y las líneas sin cubrir coinciden con el total de cada
fichero, el problema es el reporte, no los tests. En este repo la causa fue
`<DebugType>None</DebugType>` en `GestorSolicitudes.csproj`: sin PDB, coverlet no
puede asociar los puntos de secuencia y genera un OpenCover vacío. `None` solo hace
falta al publicar, así que se condiciona a Release:

```xml
<DebugType Condition="'$(Configuration)' == 'Release'">None</DebugType>
```

`dotnet test` compila en Debug, así que el CI recupera el PDB y la cobertura real
(~89% de líneas en este repo). Después de tocar esto, vuelve a mirar el tamaño del
`coverage.opencover.xml`, no solo el código de salida de `dotnet test`.

## Convenciones de este repo que se deben respetar

- La solución es `.slnx`. Todo texto, comentario y mensaje de commit en español.
- Los diálogos van por `IDialogoService` (inyectado): `MessageBox`, `OpenFileDialog`,
  `SaveFileDialog` y `Process.Start` solo viven en `DialogoService` (Views). No volver a
  meter `MessageBox` en un ViewModel. La lógica de los comandos se prueba con un
  `DialogoFalso`, así que la cobertura se arregla probando el comando, no excluyéndolo.
- Para añadir propiedades a `Solicitud` o XAML nuevo, seguir AGENTS.md
  (`VerificarColumnasFaltantes`, `<Page>` declarado en el csproj).
- Un issue nuevo de Sonar que es falso positivo del diseño (p.ej. S2077 en el
  `App()` antiguo) se resuelve con `[SuppressMessage]` + comentario, no tocando
  la lógica.

## Regla de decisión cuando Sonar marca un fichero sin cobertura

1. **¿Es un lenguaje para el que el CI recoge cobertura?** Antes de nada mira
   qué Extensions tiene el PR, no qué issues marca Sonar:
   `git diff --name-only <merge-base>..HEAD | % { [IO.Path]::GetExtension($_) } | Group-Object`.
   El workflow **solo** recoge cobertura de C# (`coverlet` → OpenCover). Un
   fichero `.mjs`, `.ts` o `.js` en el PR **nunca** tendrá cobertura, y si es el
   único código ejecutable que el PR añade, la cobertura de código nuevo cae a
   **0,0%** y el gate falla. Pasa directamente al punto 3.
2. **¿Ya está en el diff del PR?** (`git log --oneline -<n> -- <ruta>`). Si sí,
   el fichero entró con tu trabajo y un issue de estilo (no de cobertura) se
   arregla **en el sitio**.
3. **Si el fichero es de un lenguaje sin reporte de cobertura, o no está en el
   diff**, pelea con la herramienta y ganarás. Excluye la ruta del análisis
   (`/d:sonar.exclusions="<ruta>"` en `sonarcloud.yml`) en vez de editar el
   fichero. Con el fichero excluido y sin C# nuevo, el código nuevo queda por
   debajo de las 20 líneas y el gate ignora la condición de cobertura.

No inventes un hook pre-push que lance Sonar: el análisis necesita el token de
nube, tarda minutos en materializarse y CI ya lo corre. Sonar no es una
comprobación local decidible al instante como `dotnet test` o `dotnet format`.

Y no confíes en `api/qualitygates/project_status` sin `branch`: devuelve el
estado de la rama principal (`master`), que puede estar en verde mientras el PR
está en rojo. Para el PR, la fuente es el check de GitHub
`SonarQubeCloud / SonarCloud Code Analysis` (API pública de GitHub,
`/repos/<owner>/<repo>/commits/<rama>/check-runs`, sin token), que es el que
reporta "Quality Gate failed" con la condición concreta.

## Reglas del analizador que ya nos han mordido

Cada vez que Sonar marca un issue de código (no de cobertura), apunta aquí la regla
y su arreglo para no repetirlo:

- **S6966 (`Await ReadAllTextAsync instead`)**: en un método `async`, la E/S de
  fichero síncrona (`File.ReadAllText`, `File.ReadAllBytes`, `StreamReader.ReadToEnd`
  sobre un `Stream` de fichero...) debe ser la versión `...Async` y con `await`
  (`await File.ReadAllTextAsync(ruta)`). Ya pasó en `Views/VisorAdjuntoWindow.xaml.cs`.
  Regla general: si el método es `async`, no llames a APIs de E/S bloqueantes.
- **S6444 (`Pass a timeout to limit the execution time`)**: todo `Regex` debe llevar
  timeout para no ser vulnerable a ReDoS. Usa la sobrecarga con timeout
  (`Regex.Replace(entrada, patron, reemplazo, RegexOptions.None, TimeSpan.FromSeconds(1))`)
  o un `Regex` estático construido con timeout. Ya pasó en `DocumentoHelper.cs`.
  Regla general: nunca uses los atajos `Regex.IsMatch/Match/Replace/Split` sin timeout
  sobre texto que viene de un fichero.
- **S3881 (`Fix this implementation of 'IDisposable' to conform to the dispose pattern`)
  y CA1816 (`Change Dispose() to call GC.SuppressFinalize`)**: un `IDisposable` sin
  finalizador se resuelve **sellando la clase** (`public sealed partial class ...`) y
  llamando a `GC.SuppressFinalize(this)` al final de `Dispose()`. Si la clase no puede
  sellarse (se hereda), hace falta `protected virtual void Dispose(bool disposing)` y
  `Dispose()` llama a `Dispose(true)` + `GC.SuppressFinalize(this)`. Ya pasó en
  `MainViewModel`. No añadas un finalizador solo para callar la regla.
- **S1117 (`Rename 'x' which hides the field with the same name`)**: una variable local
  o de patrón (`if (this.Edicion is not Solicitud edicion)`) que se llama igual que un
  campo generado (`edicion`) oculta el campo. Renombra la local/parámetro (`solicitud`)
  y deja el campo con su nombre. Cuidado con `[ObservableProperty]`: el campo privado y
  la propiedad pública comparten nombre en PascalCase. Ya pasó también en `App.xaml.cs`
  con `AppDbContext db = new();` tapando el campo `db`: el local pasó a `contexto`.
- **S2930 (`Dispose 'x' when it is no longer needed`)**: es un **bug**, no un estilo, y
  tumba el rating de fiabilidad: es el único de los seis que puso el gate en rojo. Todo
  `IDisposable` guardado en un campo se libera. Una `Window` no tiene `Dispose`, así que
  se cancela y se libera en `Closed`:
  `this.Closed += (_, _) => { this.cancelacion.Cancel(); this.cancelacion.Dispose(); };`.
  Es seguro liberar tras cancelar: los `CancellationToken` ya capturados siguen
  respondiendo a `IsCancellationRequested`. Ya pasó en `Views/VisorAdjuntoWindow.xaml.cs`.
- **S1192 (`Define a constant instead of using this literal 'X' N times`)**: un literal
  repetido 3 o más veces en el mismo fichero va a `private const string`. Ya pasó en
  `VisorAdjuntoWindow` con `"Mensaje.NoSePuedePrevisualizar"` (4 veces). Las claves
  repetidas en los tres diccionarios de `Traducciones.cs` no cuentan, y `MainViewModel`
  ya tiene `ClaveError` para `"Error"`.
- **S3267 (`Loops should be simplified using the 'Where' LINQ method`)**: un `foreach`
  cuya única lógica es un `if`, o un `if (...) continue;`, se sustituye por `Where`.
  Ya pasó en `GestorAdjuntos.DescartarCopias`, `MainWindow.ComprobarSeguimientosVencidos`,
  `EsquemaDb.VerificarColumnasFaltantes` y `TextoHelper.NormalizarBusqueda`. Cuidado con
  los efectos secundarios: en `MainWindow` el arreglo es
  `Where(v => this.idsVencidosAvisados.Add(v.Id))`, que filtra y marca en la misma
  pasada; deja un comentario explicándolo.
- **S3242 (`Change return type of method 'X' ... for improved performance`)**: un método
  privado que solo devuelve un tipo concreto debe declararlo
  (`SolidColorBrush` en vez de `Brush` en `EstadoAColorConverter.Crear`). Los campos
  pueden seguir siendo del tipo base.
- **S3878 (`Prefer 'static readonly' fields over constant array arguments...`)**: no
  crees arrays literales en cada llamada (`Assert.Equal(new[] { ... }, ...)`). Usa un
  `private static readonly` en la clase del test o evita el array (`lista.Take(n)`,
  `lista.Skip(n)`). Ya pasó en `PaginadorTests` y `LocalizacionTests`.
- **S1135 (`Complete the task associated to this 'TODO' comment`)**: la regla busca la
  etiqueta `TODO`, pero también muerde el **"todo" español** de los comentarios
  ("de que todo fue bien"). Si aparece en un comentario que no es una tarea pendiente,
  reescribe la frase ("la operación terminó bien") en vez de añadir un `NOSONAR`.
  Ya pasó en `IDialogoService.cs` y `ArquitecturaTests.cs`.
- **Duplicación (`Duplicated Lines`)**: una lógica extraída a un helper pero sin
  conectar deja dos copias y Sonar marca la nueva. Conecta el helper y borra la
  copia vieja: `MainViewModel.ConstruirCsv` delegó en `CsvExportador.Construir` y
  desaparecieron las ~50 líneas duplicadas. Antes de extraer, busca el cuerpo que ya
  existe (`Select-String -Pattern 'Csv\.Empresa|TextoHelper\.Escapar'`) y no copies el
  bloque.

## Barrido antes de dar por cerrados los issues

Sonar no corre en local, pero estos patrones se encuentran con `grep`/`Select-String`:

- `IDisposable|Dispose\(` en `*.cs` (¿hay otra clase que lo implemente?).
- `CancellationTokenSource`, `MemoryStream`, `StreamReader`... en campos sin `using` ni
  `Dispose`: S2930 (bug, tumba el gate).
- `\bedicion\b` (o el nombre del campo) en el fichero tocado: locales/parámetros que lo
  ocultan, incluidos los `Tipo nombre = new();`.
- `foreach` seguido en la línea siguiente de un `if` o un `continue`: S3267.
- `'"([^"]{3,})"'` contado por fichero: si un literal sale 3+ veces, S1192.
- `Assert\.(Equal|Contains)\(new` y `new\[\]` dentro de llamadas: arrays constantes.
- `\btodo\b` (sin distinguir mayúsculas) en comentarios: falsos TODO en español.

## Errores que ya hemos puesto

- **Fiarse de la cobertura global o de `master` y decir que el gate del PR pasa.**
  En el PR #13 `project_status` (sin `branch`) daba `OK` con 91,8% y el check de
  GitHub fallaba con `0.0% Coverage on New Code`. Eran dos analyses distintos.
  Lee siempre el check del PR.
- **Arreglar el issue de un fichero sin cobertura y creer que eso arregla el
  gate.** Ya pasó: un issue de estilo se corrigió en el sitio y el issue
  desapareció, pero el gate seguía en rojo. El bloqueo era la cobertura, que un
  arreglo de estilo no toca. Si el PR no añade ni una línea de C#, no esperes
  arreglar el gate editando ficheros: excluye la ruta del análisis.
- Creer que testear métodos de código viejo (p.ej. `QuitarCv`,
  `RefrescarPanelDetalle`) sube el gate: no lo sube ni 0,1 pp; solo la cobertura
  global, que el gate de PR no evalúa.
- Medir solo líneas (ignorar condiciones) y dar el gate por superado.
- Excluir un método completo que contenía líneas cubiertas.
- Subestimar el tiempo de re-análisis de SonarCloud tras el push.
- **Confundir un aviso con un fallo del gate.** Un issue nuevo de tipo *bug* (p.ej.
  S2930) tumba la fiabilidad y el gate; un *code smell* nuevo solo baja la
  mantenibilidad y aguanta una deuda pequeña. Arregla todos los del código nuevo,
  pero prioriza los bugs.