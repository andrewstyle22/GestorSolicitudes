---
name: sonarcloud-gate
description: 'Aplica a cualquier cambio de código de este repo (GestorSolicitudes) antes de commitear, subir o abrir un PR, y cuando se trabaje con SonarCloud: dejar en verde el Quality Gate, subir la cobertura de código nuevo o resolver issues nuevos del análisis. Gatillos: "sonarcloud", "quality gate", "cobertura", "coverage", "gate en rojo", "código nuevo", "issue de sonar", "análisis de sonar", "commit", "PR", "push". El CI corre SonarCloud aparte (windows-latest para build/tests; SonarCloud sobre la rama del PR).'
---

# Dejar el Quality Gate de SonarCloud en verde

Repo: `andrewstyle22_GestorSolicitudes`. El gate del PR es **"Sonar way"** y solo evalúa el código nuevo:

- **New Code Coverage ≥ 80%**: la condición que suele fallar. Se mide `(CT + LC) / (B + EL)`, así que una condición sin cubrir pesa como una línea sin cubrir.
- **New Code Duplication ≤ 3%**.
- Ratings de reliability, security y maintainability = A (0 bugs/vulnerabilidades nuevos; deuda limitada).
- 100% de security hotspots nuevos revisados.
- Con menos de 20 líneas de código nuevo, cobertura y duplicación se ignoran.

## Verificación local antes de cada commit

- `dotnet test` en verde (xUnit; tests del ViewModel con `using var db = TestDb.NuevoContexto();`, nunca tocar `%APPDATA%`).
- `dotnet format GestorSolicitudes.slnx --verify-no-changes`.
- El código nuevo lleva sus tests en el mismo commit.
- Tests que dependen del dispatcher WPF: `[StaFact]` (Xunit.StaFact).

### Barrido antes de dar por cerrados los issues

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
- `\.(First|Last|ElementAt)\(\)` en colecciones indexables (`List`, array, `IReadOnlyList`): S6602.

La cobertura local por diff solo se mide al cerrar etapa o antes de abrir un PR. La medición local sobrestima 5-20 puntos y no sustituye al check del PR. (cómo medirla: ver «Medir la cobertura» en referencia.md)

El veredicto del gate solo llega tras el push, con el check de GitHub del PR (SonarQubeCloud / SonarCloud Code Analysis). No montes hooks que lancen Sonar ni hagas push sin que te lo pida.

Si Sonar marca un issue, el gate falla o hay que medir cobertura, lee .opencode/skills/sonarcloud-gate/referencia.md.

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
