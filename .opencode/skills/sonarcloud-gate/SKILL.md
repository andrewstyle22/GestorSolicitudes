---
name: sonarcloud-gate
description: Aplica cuando se trabaja sobre el análisis de SonarCloud de este repo (GestorSolicitudes): dejar en verde el Quality Gate, subir la cobertura de código nuevo o resolver issues nuevos del análisis. Gatillos: "sonarcloud", "quality gate", "cobertura", "coverage", "gate en rojo", "código nuevo", "issue de sonar", "análisis de sonar". El CI corre SonarCloud aparte (windows-latest para build/tests; SonarCloud sobre la rama del PR).
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
   hechos: `ImportarFila`/`ImportarLineas`, `ConstruirCsv`, `ValidarImportacion`,
   `EnviadaEnMes`, `ResumenImportacion`. Los estáticos puros que no necesiten el
   ViewModel van en `CsvHelper` (Helpers no puede depender de ViewModels).
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
- Tests que dependen del dispatcher WPF: `[StaFact]` (Xunit.StaFact). `[StaFact]`
  y `[Theory]` no pueden coexistir en el mismo test (xUnit1002).

## Convenciones de este repo que se deben respetar

- La solución es `.slnx`. Todo texto, comentario y mensaje de commit en español.
- Los `MessageBox` viven en el ViewModel por decisión de diseño: **no** convertir
  diálogos/`Process.Start` en interfaces inyectables para "subir cobertura" como
  hace un `public`. La cobertura del gate no se arregla así: lo único sin cubrir
  suelen ser líneas de `MessageBox`, que una interfaz de diálogo no ataca.
- Para añadir propiedades a `Solicitud` o XAML nuevo, seguir AGENTS.md
  (`VerificarColumnasFaltantes`, `<Page>` declarado en el csproj).
- Un issue nuevo de Sonar que es falso positivo del diseño (p.ej. S2077 en el
  `App()` antiguo) se resuelve con `[SuppressMessage]` + comentario, no tocando
  la lógica.

## Ficheros que no son tuyos: los gobernados por el harness

`.rsc.json` tiene dos listas que hay que consultar **antes** de tocar nada bajo
`.claude/`, `.rsc/` o `.rsc.json`:

- `governedPaths`: rutas cuyo contenido escribe la herramienta rsc.
- `artifactDigests`: SHA-256 con el que la herramienta verifica cada una.

Un fichero gobernado que se edita a mano **pierde el hash**: el siguiente
`rsc repair`/`sync` lo restaura desde el catálogo (`catalogVersion`) y el cambio
desaparece sin más. Comprobación rápida:

```powershell
Select-String -Path ".rsc.json" -Pattern '"<ruta>"'   # ¿está en governedPaths?
(Get-FileHash "<ruta>" -Algorithm SHA256).Hash.ToLower()  # ¿coincide con artifactDigests?
```

Excepción: `AGENTS.md` sale con provenance `preexisting` — rsc solo reescribe la
región entre `<!-- rsc-suggest:start -->` y `<!-- rsc-suggest:end -->`, el resto
es tuyo y se edita sin miedo.

### Regla de decisión cuando Sonar marca un fichero gobernado

1. **¿Es un lenguaje para el que el CI recoge cobertura?** Antes de nada mira
   qué Extensions tiene el PR, no qué issues marca Sonar:
   `git diff --name-only <merge-base>..HEAD | % { [IO.Path]::GetExtension($_) } | Group-Object`.
   El workflow **solo** recoge cobertura de C# (`coverlet` → OpenCover). Un
   fichero `.mjs`, `.ts` o `.js` en el PR **nunca** tendrá cobertura, y si es el
   único código ejecutable que el PR añade, la cobertura de código nuevo cae a
   **0,0%** y el gate falla. Pasa directamente al punto 3.
2. **¿Ya está en el diff del PR?** (`git log --oneline -<n> -- <ruta>`). Si sí,
   el fichero entró con tu trabajo y un issue de estilo (no de cobertura) se
   arregla **en el sitio**: el arreglo es legítimo aunque el hash registrado en
   `artifactDigests` quede desfasado y rsc avise de deriva después.
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

## Errores que ya hemos puesto

- **Fiarse de la cobertura global o de `master` y decir que el gate del PR pasa.**
  En el PR #13 `project_status` (sin `branch`) daba `OK` con 91,8% y el check de
  GitHub fallaba con `0.0% Coverage on New Code`. Eran dos analyses distintos.
  Lee siempre el check del PR.
- **Arreglar el issue de un fichero sin cobertura y creer que eso arregla el
  gate.** El `.sort()` sin comparador de `.claude/rsc-bootstrap.mjs:112`
  (javascript:S3785) se corrigió en el sitio y el issue desapareció, pero el gate
  seguía en rojo: el bloqueo era la cobertura, que un comparador no toca. El PR
  no tenía ni una línea de C# y su único ejecutable era ese `.mjs`, sin reporte
  de cobertura. La solución fue `sonar.exclusions=".claude/**"`.
- Creer que testear métodos de código viejo (p.ej. `QuitarCv`,
  `RefrescarPanelDetalle`) sube el gate: no lo sube ni 0,1 pp; solo la cobertura
  global, que el gate de PR no evalúa.
- Medir solo líneas (ignorar condiciones) y dar el gate por superado.
- Excluir un método completo que contenía líneas cubiertas.
- Subestimar el tiempo de re-análisis de SonarCloud tras el push.