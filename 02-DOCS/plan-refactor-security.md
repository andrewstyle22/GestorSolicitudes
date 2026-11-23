# Plan de refactorización de seguridad

## Evaluación de los documentos de auditoría

**Conclusión:** no se confirmó ninguna vulnerabilidad explotable. La conclusión principal es razonable para una aplicación WPF local y mono-usuario; las recomendaciones del informe son endurecimiento, no hallazgos con severidad. El informe sí necesita correcciones antes de considerarse una descripción exacta y reproducible de la revisión.

- **`FINDINGS-DETAIL.md`: correcto con el alcance declarado.** Indica cero hallazgos confirmados, coherente con `REPORT.md`. No aporta trazas porque no hubo candidatos ni ejecución del objetivo.
- **`NEEDS-VALIDATION.md`: correcto respecto a los hallazgos.** No hay hipótesis de vulnerabilidad bloqueadas en los documentos. La nota del informe sobre perfiles móviles debe mantenerse como comprobación de despliegue/privacidad, no presentarse como una vulnerabilidad confirmada.
- **`REPORT.md`: correcto en su conclusión principal, pero requiere correcciones.** Las observaciones de migración sin límite, previsualización sin límite, contención textual y fórmula CSV siguen presentes en el código consultado. Sin embargo:
  1. La sección de método dice `PASS: 6 coverage units valid`, mientras que Cobertura afirma que hay 7 unidades cubiertas. Hay que reconciliar el libro de cobertura y el texto.
  2. El informe identifica la revisión como `a8542b0…`; el repositorio está ahora en `acb9712…`. Hay cambios posteriores en `MigradorAdjuntos.cs` y `MainViewModel.cs`, entre otros. Por ello, no se debe atribuir al código actual una cobertura completa basada solo en esa revisión.
  3. La falta de `permissions:` en los workflows sí es comprobable; los permisos efectivos del `GITHUB_TOKEN` dependen también de la configuración de GitHub, que el repositorio no muestra. El informe debe presentar el mínimo privilegio como recomendación, no afirmar una autoridad efectiva no verificada.

La auditoría fue de fuente, perfil `quick`; no ejecutó la aplicación. Este plan no trata ninguna nota de endurecimiento como una vulnerabilidad demostrada.

## Objetivo y límites

Cerrar primero los riesgos verificables de tamaño y ruta de adjuntos y endurecer el flujo de CI. Después, abordar mejoras locales de menor prioridad. Mantener el comportamiento mono-usuario previsto, la compatibilidad con bases heredadas y las reglas de arquitectura del proyecto. No cambiar ACL, política de datos, ni el comportamiento de temporales sin un requisito de despliegue que lo justifique.

## Etapa 0: fijar la base de trabajo

### 0.1 Alinear el informe con el código actual

- Reconciliar el total de unidades de cobertura con el validador y corregir todas las referencias al número.
- Registrar la revisión actual y repetir o acotar la comprobación de las rutas que cambiaron desde `a8542b0…`, en particular `MigradorAdjuntos.cs` y `MainViewModel.cs`.
- Mantener separados los estados `confirmed`, `needs_validation` y las notas de endurecimiento; no elevar estas últimas a vulnerabilidades sin evidencia de una frontera cruzada.

**Aceptación:** la revisión declara una sola referencia, el alcance coincide con esa referencia y las cifras de cobertura concuerdan con el libro validado.

**Dependencia:** ninguna. Es requisito para interpretar los resultados de las etapas posteriores.

## Etapa 1: limitar y contener la migración de adjuntos

### 1.1 Aplicar el límite antes de leer el fichero

Archivos principales: `MigradorAdjuntos.cs`, `AdjuntosHelper.cs` y `GestorSolicitudes.Tests/MigradorAdjuntosTests.cs`.

- Aplicar `AdjuntosHelper.TamanoMaximoBytes` dentro de la ruta de migración, antes de `File.ReadAllBytes` o de cualquier reserva proporcional al tamaño.
- Si el fichero excede el máximo, registrar el caso, conservar la ruta pendiente para que no se pierda el documento ni se marque como migrado, y no guardar ningún BLOB parcial.
- Revisar la compatibilidad con las bases heredadas y el flujo de transacción antes de cambiar el estado de la fila.

**Aceptación y verificación:** casos en el límite exacto, un byte por encima, fichero ausente y fichero ilegible; en el caso excesivo, la ruta permanece pendiente, no se crea un BLOB y el resto de la migración conserva su comportamiento.

### 1.2 Validar la ubicación y el borrado de rutas heredadas

Archivos principales: `MigradorAdjuntos.cs` y `GestorSolicitudes.Tests/MigradorAdjuntosTests.cs`.

- Antes de leer o borrar, validar que la ruta corresponde a la carpeta de adjuntos heredados esperada; ante una ruta externa, no leerla ni borrarla y conservarla para revisión.
- Revisar el uso de `EstaDentro`, que hoy compara rutas completas mediante `StartsWith`. La comparación debe respetar límites de directorio y, si el modelo de amenaza incluye escrituras por otro principal o puntos de reanálisis, comprobar también estos últimos de forma segura para Windows. Una comprobación textual sola no debe describirse como protección contra uniones de directorios.
- Mantener una prueba de regresión que confirme que un fichero externo nunca se borra.

**Aceptación y verificación:** rutas dentro/fuera, prefijos que solo comparten texto con la carpeta, y puntos de reanálisis según el mecanismo elegido; nunca se elimina el destino externo. La migración de datos heredados válidos sigue funcionando.

**Dependencia:** 1.1. No adoptar comprobaciones de puntos de reanálisis que sigan dejando una carrera de tiempo entre validación y borrado; si no se puede garantizar el requisito, documentar el límite y mantener la operación segura por defecto.

## Etapa 2: hacer cumplir el tamaño al abrir documentos guardados

Archivos principales: `MainViewModel.cs`, `AdjuntosHelper.cs` y `GestorSolicitudes.Tests/ViewModelAdjuntosTests.cs`.

- Hacer que la cota de 2 MB se aplique también a documentos leídos desde SQLite, no solo a ficheros seleccionados para adjuntar.
- Comprobar el tamaño almacenado antes de materializar el BLOB en memoria cuando sea posible; no basta con validar después de obtener el `byte[]`.
- Si el BLOB supera el máximo, no escribirlo a `%TEMP%` ni pasarlo a los parsers. Informar al usuario mediante el servicio de diálogo y conservar intacta la fila almacenada.

**Aceptación y verificación:** prueba de un BLOB válido en el límite y otro mayor creado directamente en la base de pruebas; el segundo no llega al visor y no se modifica ni elimina.

**Dependencia:** 1.1, para que migración y lectura compartan el mismo límite.

## Punto de control A: adjuntos

- Ejecutar las pruebas específicas de migración y apertura de adjuntos.
- Revisar que los fallos de migración no limpien la ruta ni borren el fichero original.
- Confirmar que el tamaño máximo sigue teniendo una única fuente: `AdjuntosHelper.TamanoMaximoBytes`.

## Etapa 3: endurecer la exportación CSV

Archivos principales: `TextoHelper.cs` y `GestorSolicitudes.Tests/TextoYCsvTests.cs`.

- Añadir una regresión para un valor que empieza por salto de línea y continúa con una fórmula, por ejemplo `\n=1+1`; la salida no debe poder convertirse en fórmula al abrirse en una hoja de cálculo.
- Revisar el escapado después de la normalización de CR/LF y preservar las reglas existentes para comillas, separadores y apóstrofos.
- Esta es una defensa local de profundidad: el contenido lo controla el mismo usuario y no se ha demostrado una frontera entre principales. Documentar la ampliación respecto a la decisión actual de `TextoHelper.Escapar`.

**Aceptación y verificación:** pruebas para LF, CR, tabulador, espacios iniciales, fórmulas, comillas y los tres separadores; exportación normal sin cambios para valores no peligrosos.

**Dependencia:** ninguna tras la etapa 0.

## Etapa 4: reducir la exposición del CI y fijar sus entradas

Archivos principales: `.github/workflows/dotnet-desktop.yml`, `.github/workflows/sonarcloud.yml` y, si procede, el manifiesto de herramientas.

### 4.1 Permisos mínimos y credenciales de `checkout`

- Declarar permisos explícitos mínimos por workflow/job, empezando por `contents: read` cuando sea suficiente.
- Desactivar `persist-credentials` en cada `actions/checkout` si los pasos posteriores no necesitan autenticarse con Git.
- Comprobar por separado los eventos `push` y `pull_request`, incluidos PR de forks; no asumir permisos efectivos sin revisar la configuración del repositorio/organización.

**Aceptación:** el CI sigue compilando y ejecutando tests con el token de menor alcance necesario, y el checkout no deja credenciales persistidas si no se requieren.

### 4.2 Fijar acciones, herramientas y SDK

- Fijar las acciones de terceros a SHA completo revisado y documentar cómo se actualizan.
- Fijar una versión aprobada de `dotnet-sonarscanner` o incorporarla al mecanismo de herramientas del proyecto.
- Alinear el SDK resuelto por `global.json` con el instalado en ambos workflows; eliminar diferencias implícitas que reduzcan la reproducibilidad.

**Aceptación:** las versiones ejecutadas por CI son deterministas y la compilación, formato, tests y análisis siguen usando versiones compatibles.

### 4.3 Evitar pasar el secreto de Sonar en argumentos literales

- Consumir `SONAR_TOKEN` desde el entorno en los pasos de inicio y cierre del análisis, sin interpolar el secreto directamente en el texto del comando.
- Verificar que la configuración del scanner y el enmascaramiento de logs siguen funcionando; no imprimir el entorno ni el valor del secreto.

**Aceptación:** los análisis de PR autorizado siguen completándose y el token no aparece como literal en el comando del workflow ni en los logs.

**Dependencias:** 4.1 antes de 4.3; 4.2 puede prepararse en paralelo con 4.1, pero validar el workflow después de las tres subetapas.

## Punto de control B: CI

- Revisar permisos y condiciones de cada evento en la configuración de GitHub.
- Validar los workflows en un PR de prueba autorizado sin exponer secretos.
- Confirmar que los pasos de calidad existentes no se han eliminado ni debilitado.

## Etapa 5: endurecimientos locales de prioridad menor

### 5.1 Evitar que una segunda instancia limpie temporales activos

Archivos principales: `App.xaml.cs`, `InstanciaUnica.cs` y tests apropiados para el ciclo de vida.

- Comprobar que la instancia es única antes de limpiar `%TEMP%`; ahora la limpieza ocurre antes de la comprobación y una segunda apertura puede borrar archivos que usa la primera.
- No resolver un fallo del mecanismo de instancia única abriendo otra instancia a ciegas: eso contradiría la decisión de mantener un solo `AppDbContext` y podría causar conflictos de datos.

**Aceptación:** iniciar la ruta de segunda instancia no elimina documentos temporales usados por la instancia activa; la primera sigue pudiendo abrirlos y cerrarse normalmente.

### 5.2 Endurecer el manejo de enlaces web

Archivos principales: `Enlaces.cs`, `DialogoService.cs` y `GestorSolicitudes.Tests/EnlacesTests.cs`.

- Rechazar URI HTTP/HTTPS con credenciales embebidas (`UserInfo`).
- Pasar al shell la forma canónica validada (`AbsoluteUri`) en vez de la cadena original.
- Añadir pruebas negativas para credenciales y esquemas/rutas no web relevantes, y positivas para HTTP/HTTPS con mayúsculas.

**Aceptación:** solo URI absoluta HTTP/HTTPS sin credenciales llega al shell; las entradas inválidas se rechazan antes de `Process.Start`.

**Prioridad:** posterior a adjuntos y CI; estas medidas no acreditan una vulnerabilidad en el escenario mono-usuario actual.

## Mejoras no incluidas como refactor obligatorio

- ACL explícitas para `%APPDATA%`, SQLite y el registro: primero definir un requisito de despliegue y comprobar perfiles móviles/redirección. No sustituir sin motivo las protecciones del perfil de Windows.
- Limpiar temporales huérfanos al siguiente arranque: comportamiento documentado actualmente; no se propone acortar la vida del temporal sin comprobar el ciclo del visor.
- Nombres reservados de Windows, detalles del mensaje de error del lanzador y lockfile de NuGet: son mejoras de robustez o reproducibilidad, no vulnerabilidades demostradas. Revisarlas si se amplía el modelo de amenaza o los requisitos de CI.

## Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Rechazar rutas heredadas válidas y dejar documentos sin migrar | Probar bases heredadas representativas; conservar la ruta y registrar el error en vez de borrar o descartar datos. |
| Comprobar el tamaño después de cargar un BLOB grande | Consultar primero su longitud y probar con un BLOB sobredimensionado sembrado en SQLite. |
| Cambiar el CSV rompe valores o separadores existentes | Mantener pruebas de paridad y sumar solo el caso LF-formula que hoy falta. |
| Endurecer CI rompe PR de forks o análisis Sonar | Revisar permisos externos y validar cada tipo de evento sin imprimir secretos. |
| Tratar hardening como vulnerabilidad confirmada | Mantener el informe sin severidad para notas locales y actualizar la cobertura en la revisión actual. |

## Orden recomendado

1. Etapa 0: fijar referencia y corregir cobertura.
2. Etapas 1.1 y 1.2: migración segura de adjuntos.
3. Etapa 2 y punto de control A.
4. Etapas 4.1–4.3 y punto de control B.
5. Etapa 3.
6. Etapas 5.1 y 5.2, según prioridad y capacidad de prueba.

Cada cambio de código debe incorporar pruebas xUnit focalizadas, respetar la arquitectura MVVM del proyecto y pasar `dotnet test` y `dotnet format GestorSolicitudes.slnx --verify-no-changes` antes de integrarse.
