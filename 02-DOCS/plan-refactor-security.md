# Plan de refactorización de seguridad

Nace de la auditoría de seguridad de `REPORT.md`, `FINDINGS-DETAIL.md` y `NEEDS-VALIDATION.md`. La auditoría fue de fuente, perfil `quick`, y **no confirmó ninguna vulnerabilidad explotable**: lo que recoge son notas de endurecimiento, más un fallo funcional que descubrió de paso (la segunda copia de la aplicación borra los temporales de la primera). Este plan no trata ninguna nota como vulnerabilidad demostrada.

Se han contrastado las notas con el código antes de planificar. Dos correcciones respecto al informe y a su primer plan:
- `MigradorAdjuntos.EstaDentro` **ya respeta los límites de directorio** (añade el separador final); solo faltan las uniones de directorio, que quedan fuera del modelo de amenaza.
- El informe describe el borrado de temporales en el arranque, pero el mismo fallo existe en `OnExit`, así que la corrección tiene dos puntos.

Las correcciones del propio informe (6 frente a 7 unidades de cobertura, revisión `a8542b0` frente a `acb9712`) no son trabajo del proyecto: son notas sobre el informe y no forman parte de este plan.

---

## 1. Decisiones tomadas

| # | Tema | Decisión |
|---|------|----------|
| 1 | Límite de 2 MB | Se aplica en **todas partes**: al adjuntar (ya hecho), al migrar ficheros antiguos y al abrir un documento guardado. Es una invariante del almacén. |
| 2 | Ficheros antiguos de más de 2 MB | **No se migran**: se quedan pendientes con su ruta intacta (como los ilegibles), sin BLOB y sin borrar el fichero. Un único aviso agrupado por arranque y registro en el fichero de errores. Antes se migraban igualmente; esta decisión lo sustituye. |
| 3 | Lectura en la migración | Se reutiliza `LectorAdjuntos.Leer` (tope de tamaño y `FileShare.Read`) en lugar de la lectura propia del migrador. |
| 4 | Ficheros fuera de la carpeta de adjuntos | La migración no los lee ni los borra: quedan pendientes y registrados. |
| 5 | CSV | Un valor que empieza por salto de línea seguido de una fórmula también se neutraliza. Es defensa en profundidad: el CSV lo teclea el mismo usuario que lo abre. |
| 6 | Enlaces | Se rechazan los enlaces con credenciales embebidas y al shell se pasa la forma canónica (`AbsoluteUri`). |
| 7 | CI | **Fuera de este plan** (opción C). Queda pendiente decidir si se borra `dotnet-desktop.yml`, y el endurecimiento de los workflows se hará después de esa decisión. |

---

## 2. Reglas de cada commit

1. `dotnet test` en verde (los avisos son errores).
2. `dotnet format GestorSolicitudes.slnx --verify-no-changes`.
3. `sonarcloud-gate` local antes de commitear.
4. Todo texto visible con clave en es/en/de; XAML nuevo declarado en el csproj.
5. Mensaje en español; un commit, una idea reversible.
6. El push lo hace el autor tras cada commit; el agente no hace push.
7. Al terminar cada paso se actualiza `CONTEXT.md` en el mismo commit.
8. Los tests de reproducción se commitean junto con su arreglo.
9. Antes de tocar `MigradorAdjuntos`, leer su estado actual: el commit que conserva la ruta de un fichero ilegible ya cambió `LeerFichero` y `ActualizarSolicitud`. Los pasos de la Etapa S2 se adaptan a ese código.

Tamaño estimado: **S** (<50 líneas), **M** (50-200).

---

## Etapa S0. Preparación

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| S0.1 | `docs: plan de la refactorización de seguridad` | Este plan y `CONTEXT.md` (plan en curso, etapas, siguiente paso y decisiones). | S |

---

## Etapa S1. Temporales y arranque (fallo funcional)

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| S1.1 | `fix: una segunda copia no borra los temporales de la primera` | Hoy `App.OnStartup` ejecuta `TemporalesAdjuntos.Limpiar()` **antes** de comprobar la instancia única, y `OnExit` lo ejecuta siempre, incluso cuando la segunda copia llama a `Shutdown()`. En los dos casos se borran los documentos que la primera tiene abiertos en el visor. Hay que mover la limpieza del arranque después de `EsPrimera` y, en `OnExit`, limpiar solo si esta copia era la primera (guardar el dato antes de liberar `instancia`). Prueba manual: abrir un documento en el visor, lanzar una segunda copia y comprobar que el documento sigue abierto y su fichero existe; al cerrar la primera, la carpeta se vacía. | S |

---

## Etapa S2. Migración de adjuntos antiguos

Archivos: `MigradorAdjuntos.cs`, `LectorAdjuntos.cs`, `AdjuntosHelper.cs` y `MigradorAdjuntosTests.cs`. En todos los pasos, un fichero que no se migra conserva `RutaCv` o `RutaCarta` y su nombre, no genera BLOB, no se borra y se registra con `RegistroErrores`; la fila sigue pendiente y se reintenta en el siguiente arranque.

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| S2.1 | `refactor: la migración lee los ficheros con LectorAdjuntos` | Sustituye la lectura propia (`File.ReadAllBytes` sin cota) por `LectorAdjuntos.Leer`. El resultado distingue cuatro casos: sin ruta, fichero que no existe (se vacía como hoy), fichero ilegible o bloqueado (pendiente) y **fichero que supera 2 MB** (pendiente, a partir de `DocumentoDemasiadoGrandeException`). Tests: exactamente 2 MB se migra; 2 MB más un byte queda pendiente con la ruta intacta, sin BLOB y con el fichero en disco; un documento grande no impide migrar el otro de la fila; los tests existentes siguen pasando. | M |
| S2.2 | `feat: aviso agrupado de adjuntos antiguos que superan el límite` | El resultado de la migración incluye la lista de ficheros pendientes por tamaño. `App.OnStartup` muestra un único aviso (número de documentos y la carpeta, es/en/de) mientras quede alguno, y lo registra. Se resuelve reduciendo el fichero o moviéndolo. Tests del resultado y del texto. Prueba manual con un fichero de más de 2 MB. | S |
| S2.3 | `fix: la migración solo lee y borra ficheros dentro de la carpeta de adjuntos` | Aplica `EstaDentro` **antes de leer** (hoy solo se comprueba antes de borrar). Una ruta externa no se lee ni se borra, queda pendiente y se registra. Tests: ruta dentro, ruta fuera, un prefijo que solo comparte texto (`adjuntos-otro`, que ya funcionaba: se deja como regresión) y una ruta con `..`. Un fichero externo nunca se elimina. Las uniones de directorio quedan fuera: se anota en la decisión, sin presentar la comprobación textual como protección frente a ellas. | S |

---

## Etapa S3. Apertura de documentos guardados

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| S3.1 | `feat: comprobar el tamaño guardado antes de leer un documento` | `AbrirAdjunto` consulta primero la longitud del BLOB (consulta sin seguimiento que proyecta solo la longitud) y, si supera `AdjuntosHelper.TamanoMaximoBytes`, no lo lee, no escribe el temporal ni abre el visor: avisa con `dialogo.Avisar` (clave nueva en es/en/de) y deja la fila intacta. Hoy el BLOB se materializa entero antes de cualquier comprobación. Tests: un BLOB justo en el límite se abre; otro mayor, sembrado directamente en la base de pruebas, no llega al visor y no se modifica ni se borra. | M |

**Punto de control A.** Ejecutar los tests de migración y de apertura; comprobar que ningún fallo de migración limpia una ruta ni borra el fichero original, y que el tamaño máximo tiene una única fuente: `AdjuntosHelper.TamanoMaximoBytes`.

---

## Etapa S4. Exportación CSV

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| S4.1 | `fix: el CSV neutraliza los textos que empiezan por un salto de línea y una fórmula` | `TextoHelper.Escapar` decide el apóstrofo con el primer carácter visible del valor original; `'\n'` no está entre los disparadores y `\n=1+1` sale como ` =1+1`. Se añade `'\n'` y la regresión `Escapar("\n=1+1")`. Tests para LF, CR, tabulador, espacios iniciales, fórmulas, comillas y los tres separadores; los valores no peligrosos salen igual que antes. Se actualiza la decisión de `AGENTS.md`. | S |

---

## Etapa S5. Enlaces de oferta

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| S5.1 | `fix: no se abren enlaces con credenciales embebidas` | `Enlaces.EsWeb` rechaza una URI HTTP o HTTPS con `UserInfo` no vacío. Tests: `https://usuario:clave@host` se rechaza; los enlaces normales siguen pasando. | S |
| S5.2 | `fix: al abrir un enlace se pasa al shell la forma canónica` | `DialogoService.AbrirEnlace` llama a `Process.Start` con `uri.AbsoluteUri`, resultado de un único análisis, en lugar de la cadena original. Tests: positivo con `HTTPS://` en mayúsculas; negativos con `ms-msdt:`, `C:\Windows\System32\calc.exe`, una ruta UNC, `//host` y un esquema codificado con porcentajes. | S |

---

## Etapa S6. Documentación y cierre

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| S6.1 | `docs: modelo de amenaza y decisiones de seguridad` | Sección «Modelo de amenaza» en `README.md` o `AGENTS.md`: aplicación local de un solo usuario; base, documentos en BLOB y registro viven en `%APPDATA%` (Roaming) con las ACL heredadas, de modo que con un perfil móvil el CV se replicaría fuera del equipo; el registro incluye rutas absolutas y SQL; las uniones de directorio están fuera del modelo; los temporales viven en el `%TEMP%` del usuario hasta salir. Actualiza `adjuntos-en-base-de-datos.md` (los ficheros de más de 2 MB y los externos quedan pendientes) y las decisiones de `AGENTS.md`. | S |
| S6.2 | `chore: pasada final de sonarcloud-gate` | Verificar antes de abrir el PR. | S |

---

## 3. Orden y dependencias

1. S0.1, y después S1.1 (el fallo funcional va primero).
2. S2.1 antes de S2.2 y S2.3: los tres comparten el nuevo resultado de lectura.
3. S3.1 después de S2.1, para que migración y apertura compartan el mismo límite; después, el punto de control A.
4. S4.1, S5.1 y S5.2 son independientes entre sí.
5. S6 al final.

## 4. Pruebas manuales

| Paso | Qué comprobar |
|------|---------------|
| S1.1 | Con un documento abierto en el visor, lanzar una segunda copia: el documento sigue abierto y su temporal existe. Al cerrar la primera, la carpeta de temporales se vacía. |
| S2.2 | Colocar un fichero de más de 2 MB en `adjuntos`, referenciado desde una base antigua, y arrancar: un solo aviso, el fichero no se borra y la ruta sigue en la base. |

## 5. Riesgos

| Riesgo | Cuándo | Mitigación |
|--------|--------|------------|
| Un documento antiguo válido queda sin migrar | S2.1, S2.3 | Se conserva la ruta y el fichero, se registra y se avisa; nunca se borra ni se vacía. |
| El aviso de ficheros grandes aparece en cada arranque | S2.2 | Es un único aviso agrupado y se resuelve reduciendo o moviendo el fichero. |
| La consulta de longitud del BLOB no se traduce a SQL y lo carga entero | S3.1 | Comprobarlo con el test del BLOB sobredimensionado y revisar el SQL generado. |
| El cambio del CSV altera valores o separadores existentes | S4.1 | Se mantienen los tests de paridad y solo se suma el caso del salto de línea. |

## 6. Fuera de este plan

- **CI (opción C).** Endurecer los workflows (`permissions: contents: read`, `persist-credentials: false`, acciones fijadas por SHA, versión fija de `dotnet-sonarscanner`, `SONAR_TOKEN` solo por entorno, SDK de `global.json` alineado) y decidir si se borra `dotnet-desktop.yml`, duplicado de `sonarcloud.yml`. Se hará después de esa decisión; si se borra, hay que revisar la protección de rama en GitHub.
- **ACL explícitas** para `%APPDATA%`, la base y el registro: solo si aparece un requisito de despliegue.
- **Nombres reservados de Windows** en los temporales: Windows no permite crear ficheros con esos nombres, y no hay ningún productor no confiable.
- **`packages.lock.json`:** solo si el build pasa a un runner compartido.
- **Instancia única pre-creable:** un proceso del mismo usuario puede degradar la disponibilidad; el manejador global ya evita que sea un cierre sin aviso. No se abre una segunda instancia a ciegas: contradice el contexto único.
- **Temporales tras un cierre anómalo:** ya se limpian en el siguiente arranque.
- **Mensaje del lanzador de enlaces** (`ex.Message`): solo lo ve el mismo usuario.
