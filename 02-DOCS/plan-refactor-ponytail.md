# Plan de refactorización Ponytail

Nace de la auditoría de la skill *ponytail* (`02-DOCS/auditoria-ponytail.md`) sobre el proyecto tras las etapas 0 a 5 de `plan-refactor-edicion.md`. Cubre los diez hallazgos de la auditoría, salvo el 6 (CI duplicado, que queda fuera por ahora), y añade tres peticiones: un manejador global de excepciones, un límite de 2 MB para los documentos y guardar el CV y la carta en la base de datos.

Las pruebas que no se pueden automatizar (atajos de teclado, instancia única, ventanas) van en la sección «Pruebas manuales».

---

## 1. Decisiones tomadas

| # | Tema | Decisión |
|---|------|----------|
| 1 | Etapa 6 del plan anterior | Se sustituye por la Etapa A. Primero se escriben los tests; los que fallen se commitean con su arreglo (A.2, A.3 y A.4). |
| 2 | Estados | **Oferta aceptada → otros cierres se permite** (te descartan o retiras después de aceptar; el porqué va en las notas). El desplegable solo deja de ofrecer cerrado → «Oferta aceptada», que lanza una excepción. |
| 3 | Excepciones sin controlar | Manejador global: registra el error en `%APPDATA%\GestorSolicitudes\errores.log`, avisa al usuario y no cierra la aplicación. |
| 4 | Instancia única | Opción B: la segunda copia avisa a la primera, que muestra su ventana, y se cierra. |
| 5 | Fechas | Ninguna fecha (primera respuesta, entrevista, cierre y próximo seguimiento) anterior a `FechaSolicitud`. La regla vive en el **modelo** y el editor la usa para el mensaje. |
| 6 | CI duplicado | Fuera de este plan; se revisará después. |
| 7 | Límite de documentos | CV y carta no pueden pesar más de 2 MB (2 × 1024 × 1024 bytes). Si pesan más, aviso: «El documento no puede ser mayor a 2 megabytes». |
| 8 | Documentos | Van a la **base de datos** en una tabla aparte (`DocumentosSolicitud`), con una columna `Cv` y otra `Carta`, ambas BLOB y anulables. Solo se cargan al pulsar «abrir», no al ver la candidatura. Detalle en `02-DOCS/decisiones/adjuntos-en-base-de-datos.md`. |
| 9 | Migración | Al arrancar se pasan a la base de datos los ficheros de `adjuntos` y después se borran. Se mejorará en otra refactorización. |
| 10 | Atajos de teclado | `UpdateSourceTrigger=PropertyChanged` en los `TextBox` de texto, y una confirmación del control con el foco antes de Ctrl+S, Ctrl+N y cerrar, para cubrir también salarios y fechas. |

---

## 2. Reglas de cada commit

1. `dotnet test` en verde (los avisos son errores).
2. `dotnet format GestorSolicitudes.slnx --verify-no-changes`.
3. `sonarcloud-gate` local antes de commitear.
4. XAML nuevo: `<Page>` en el csproj; todo texto visible con clave en es/en/de.
5. Mensaje en español; un commit, una idea reversible.
6. El push lo hace el autor tras cada commit; el agente no hace push.
7. Al terminar cada paso se actualiza `CONTEXT.md` en el mismo commit.
8. Cada paso lleva su test. Los tests de reproducción se escriben primero para comprobar que fallan sin el arreglo y se commitean **junto con** el arreglo.

Tamaño estimado: **S** (<50 líneas), **M** (50-200), **L** (>200, se subdivide).

---

## Etapa A. Guardado y navegación (hallazgos 1, 2 y 3, y manejador global)

### A-0. Preparación

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| A.0 | `docs: plan de la refactorización Ponytail` | Este plan, `CONTEXT.md`, la auditoría, el documento de decisión de adjuntos y la tabla de transiciones de `estados-y-reapertura.md`. La Etapa 6 de `plan-refactor-edicion.md` queda marcada como sustituida. | S |

### A-1. Fallos de guardado (hallazgo 2 y Etapa 6)

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| A.1 | `test: comprobar si la Etapa 6 corrigió los fallos de guardado` | Tres tests con `DialogoFalso` e interceptor de EF: (a) tras un `SaveChanges` fallido, `HayCambios` sigue en verdadero; (b) escenario del hallazgo 2: rellenar una fila nueva, fallar el guardado, elegir otra fila, **Descartar**, guardar esa fila y comprobar que solo hay una; (c) una candidatura nueva guardada tras *Descartar* avisa de que está repetida. Los que ya pasan se commitean como regresión; el que falle se commitea con su arreglo (A.2, A.3 o A.4). | M |
| A.2 | `fix: el editor conserva HayCambios tras un guardado fallido` | Solo si falla (a). Tras recargar el editor desde la entidad (5.1), vuelve a marcar que hay cambios (por ejemplo con un método interno `ConservarCambios()`), porque la entidad sigue modificada en el contexto. | S |
| A.3 | `fix: la candidatura repetida se busca en la base de datos` | Solo si falla (c). `BuscarCandidaturaRepetida` consulta `db.Solicitudes` sin seguimiento en lugar de `Local`; la comparación normalizada de empresa y puesto se sigue haciendo en memoria. Una fila fantasma ya no produce avisos falsos. | M |
| A.4 | `fix: ResolverCambiosPendientes cuenta el seguimiento de EF` | Se hace siempre. Con un editor abierto, si no hay `HayCambios` pero `db.ChangeTracker.HasChanges()` es verdadero, también se pregunta. *Descartar* ya vacía el seguimiento, así que la fila abandonada desaparece. Test (b). | S |

### A-2. Estado y excepciones (hallazgo 1)

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| A.5 | `refactor: las reglas de transición en un solo sitio` | `EstadoSolicitudExtensiones.AdmiteTransicion(origen, destino)`: falso para cerrado → «Oferta aceptada» (con origen distinto de «Oferta aceptada»); verdadero en el resto, incluido «Oferta aceptada» → otros cierres. `CambiarEstado` la usa para decidir cuándo lanza. Tests por cada celda de la tabla de transiciones. | M |
| A.6 | `fix: el editor valida la transición antes de guardar` | `Validar()` añade un error de transición no permitida (clave `Mensaje.TransicionNoPermitida` en es/en/de) cuando el estado original y el elegido no la admiten. Test de ViewModel: guardar cerrado → «Oferta aceptada» deja el panel abierto, avisa y no lanza. | M |
| A.7 | `feat: el desplegable de estado solo ofrece destinos válidos` | El editor expone `EstadosDisponibles` según `estadoOriginal` (se refresca al cambiar de idioma) y la pestaña Seguimiento enlaza ahí en lugar de a `Estados`. Los estados abiertos siguen disponibles para las candidaturas cerradas: se reabren con la confirmación de siempre. Tests. | M |

### A-3. Red de seguridad

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| A.8 | `feat: registro de errores en fichero` | `RegistroErrores.Registrar(excepcion, contexto)` escribe en `%APPDATA%\GestorSolicitudes\errores.log` (añade al final; si pasa de 1 MB, conserva solo la parte reciente) y nunca lanza. Tests con ruta temporal. | S |
| A.9 | `feat: manejador global de excepciones` | `DispatcherUnhandledException` (registra, avisa con la ruta del fichero y marca `e.Handled`), `AppDomain.UnhandledException` y `TaskScheduler.UnobservedTaskException` (solo registran). Textos es/en/de. Tras un error en mitad de `Guardar`, A.4 hace que el siguiente cambio de fila pregunte. | M |

### A-4. Atajos de teclado (hallazgo 3)

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| A.10 | `fix: los TextBox de texto aplican cada pulsación al búfer` | `UpdateSourceTrigger=PropertyChanged` en los `TextBox` de texto de la cabecera y las pestañas Oferta y Seguimiento. Los numéricos y de fecha no cambian, para evitar errores mientras se escribe. | M |
| A.11 | `fix: confirmar la edición pendiente antes de Ctrl+S, Ctrl+N y cerrar` | Un método que mueve el foco a la ventana para que el control activo vuelque su valor (salarios y fechas incluidos). Lo llaman los atajos (que pasan de `KeyBinding` a un manejador de teclado) y `OnClosing`, antes de `ResolverCambiosPendientes()`. Se comprueba a mano (sección 4). | M |

---

## Etapa B. Límite de 2 MB

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| B.1 | `feat: límite de 2 MB al adjuntar CV y carta` | Constante `AdjuntosHelper.TamanoMaximoBytes` (2 × 1024 × 1024). `AdjuntarAdjunto` mide el fichero antes de copiarlo; si pesa más, `dialogo.Avisar` con `Mensaje.AdjuntoDemasiadoGrande` («El documento no puede ser mayor a 2 megabytes.» en es/en/de) y no se adjunta nada. Un fichero de exactamente 2 MB sí se admite. Tests con `DialogoFalso`: justo en el límite y un byte por encima, para CV y para carta. | S |

---

## Etapa C. Instancia única y fechas (hallazgos 4 y 5)

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| C.1 | `feat: detector de instancia única` | Clase `InstanciaUnica` con un `Mutex` y un `EventWaitHandle` con nombre: `EsPrimera`, `AvisarALaPrimera()` y `EsperarAviso(Action)`. Los nombres se pueden cambiar para los tests. Tests con nombres únicos. | M |
| C.2 | `feat: la segunda copia avisa a la primera y se cierra` | `App.OnStartup` crea la `InstanciaUnica` antes de abrir la base de datos; si no es la primera, avisa y se cierra sin crear ventana. | S |
| C.3 | `feat: la primera copia muestra su ventana al recibir el aviso` | La primera espera el aviso en segundo plano y, en el hilo de la interfaz, restaura la ventana (también si estaba en la bandeja), reutilizando la acción «Mostrar» del icono. Libera los manejadores al salir. | M |
| C.4 | `feat: el editor valida que las fechas no sean anteriores a FechaSolicitud` | Primera respuesta, entrevista, cierre y próximo seguimiento no pueden ser anteriores a `FechaSolicitud`. Mensaje `Mensaje.FechaAnteriorASolicitud` en es/en/de, que nombra el campo. Se revalida al cambiar `FechaSolicitud`. Tests por campo, con el límite exacto y con nulos. | M |
| C.5 | `feat: ActualizarFechas exige fechas coherentes` | Antes de asignar, `ActualizarFechas` comprueba la nueva `FechaSolicitud` contra la entrevista y el seguimiento recibidos y contra `FechaPrimeraRespuesta` y `FechaCierre` ya guardados (por eso `AplicarFechas` debe seguir después de `AplicarRespuesta` y `AplicarEstado`, con un test del orden). Lanza `ArgumentException`. Los valores por defecto que rellena el modelo (`hoy`) no se validan. Se ajustan los tests con fechas incoherentes. | M |

---

## Etapa D. Detalles menores (hallazgos 7, 9 y 10)

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| D.1 | `refactor: cadena de conexión de SQLite en un solo sitio` | `CadenaConexion(ruta)` junto a `RutaBaseDatos`, usada por `AppDbContext` y por los dos puntos de `EsquemaDb`. | S |
| D.2 | `fix: la tabla repone la fila seleccionada tras recargar` | `Paginar` vuelve a seleccionar `solicitudAbierta` si sigue en la página, con el mecanismo de `SeleccionarSinAviso`. Evita el aviso de cambios sin guardar espurio tras el temporizador de recordatorios. Tests. | S |
| D.3 | `fix: el CSV neutraliza fórmulas precedidas de espacios` | `TextoHelper.Escapar` busca el primer carácter que no sea un espacio antes de decidir si antepone el apóstrofo. Tests con `" =2+2"` y tabuladores. | S |

---

## Etapa E. Documentos en la base de datos

Diseño completo en `02-DOCS/decisiones/adjuntos-en-base-de-datos.md`. Resumen: una tabla `DocumentosSolicitud` (clave y FK `SolicitudId`, `Cv` y `Carta` en BLOB anulables) que solo se lee al abrir un documento; `Solicitud` conserva el nombre del documento (`NombreOriginalCv` y `NombreOriginalCarta`) para saber si hay uno; el contenido nuevo vive en el editor, en memoria, hasta guardar. Se hace en ocho pasos pequeños y en este orden: primero se añade lo nuevo (tabla, lector, temporales), luego la migración, después el modelo y el editor, y al final se elimina lo antiguo.

### E-A. Almacén

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| E.1 | `feat: tabla DocumentosSolicitud` | Entidad con `SolicitudId`, `Cv` y `Carta` (`byte[]?`, `set` privado y métodos `AsignarCv`, `QuitarCv`, `AsignarCarta`, `QuitarCarta`), mapeo 1:1 con borrado en cascada y `CREATE TABLE IF NOT EXISTS` en `EsquemaDb` para las bases existentes. Tests: ida y vuelta, borrado en cascada y que cargar `Solicitudes` no lee la tabla. | M |
| E.2 | `feat: lector de adjuntos con límite` | `LectorAdjuntos.Leer(ruta)` devuelve los bytes y rechaza lo que supere 2 MB (comparte la constante de B.1). Tests. | S |
| E.3 | `feat: carpeta temporal para abrir documentos` | `TemporalesAdjuntos.Escribir(nombre, bytes)` guarda en `%TEMP%\GestorSolicitudes\adjuntos\<guid>\` con el nombre saneado y devuelve la ruta; `Limpiar()` borra la carpeta. Tests con carpeta base temporal. | S |

### E-B. Migración

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| E.4 | `feat: migrar los adjuntos de disco a la base de datos` | `MigradorAdjuntos`: lee con SQL las filas con `RutaCv` o `RutaCarta`, copia los bytes a `DocumentosSolicitud` dentro de una transacción, comprueba que el tamaño coincide y deja `NombreOriginalCv` o `NombreOriginalCarta` rellenos (con el nombre del fichero si estaban vacíos) y la ruta a `NULL`. Un fichero que ya no existe deja el campo vacío y se registra. Idempotente. Tests con base y ficheros temporales. | M |
| E.5 | `feat: borrar los ficheros antiguos tras migrar` | Tras confirmar la transacción, borra cada fichero migrado y la carpeta `adjuntos` si queda vacía. Un fallo al borrar se registra y no interrumpe. Tests. | S |
| E.6 | `feat: migrar al arrancar` | `App.OnStartup` ejecuta la migración después de `EsquemaDb`; los errores van a `RegistroErrores`. Prueba manual con una base antigua (sección 4). | S |

### E-C. Modelo y editor

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| E.7 | `feat: TieneCv y TieneCarta en el modelo` | `TieneCv` y `TieneCarta` según el nombre guardado, y `AsignarCv(nombre)`, `QuitarCv()`, `AsignarCarta(nombre)` y `QuitarCarta()`. Conviven con los métodos de ruta, que aún no se tocan. Tests. | S |
| E.8 | `feat: el editor guarda el contenido pendiente del documento` | `CvContenido` y `CartaContenido` (`byte[]?`) en el editor, que activan `HayCambios`; `CargarDesde` los deja nulos (no se leen de la base). `AdjuntoCv` y `AdjuntoCarta` se calculan con el nombre. Tests. | M |
| E.9 | `refactor: adjuntar lee el fichero en memoria` | `AdjuntarAdjunto` usa `LectorAdjuntos` y deja bytes y nombre en el editor, sin copia en disco; `QuitarAdjunto` los limpia. El aviso de B.1 se mantiene. Tests. | M |
| E.10 | `refactor: Guardar escribe los documentos de una candidatura existente` | En el mismo `SaveChanges`, crea o actualiza la fila de `DocumentosSolicitud` con los bytes pendientes, o vacía la columna si se quitó el documento, y actualiza el nombre. Se adapta el test de fallo de guardado (3.13b) para que el contenido pendiente sobreviva a un fallo. | M |
| E.11 | `refactor: Guardar escribe los documentos de una candidatura nueva` | Lo mismo para una candidatura que aún no tiene `Id`. Tests, incluido el fallo de guardado con reintento. | M |
| E.12 | `feat: abrir el documento desde la base de datos` | `AbrirAdjunto(TipoAdjunto)` usa los bytes pendientes del editor o los lee de la base (consulta sin seguimiento que proyecta solo esa columna), los deja en un temporal y llama al visor. El botón de abrir de `FilaAdjunto` pasa el tipo en lugar de la ruta. Tests con `DialogoFalso`. | M |

### E-D. Visor y limpieza

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| E.13 | `fix: el visor no muestra como texto los tipos desconocidos y acota DOCX y ODT` | Hallazgo 8. La rama por defecto del visor muestra el aviso de «no se puede previsualizar» en lugar de leer el fichero como texto. `ExtraerTextoZip` lee con un límite (10 MB de texto) y avisa si se supera. Tests. | S |
| E.14 | `refactor: quitar RutaCv y RutaCarta del modelo` | Se eliminan las dos propiedades, los métodos de ruta, el nombre de reserva `Archivo(ruta)` y su mapeo; `EsquemaDb` deja de verificarlas. En las bases antiguas las columnas quedan sin usar. Se adaptan los tests y el `SolicitudBuilder`. | M |
| E.15 | `refactor: eliminar GestorAdjuntos y las copias en disco` | Se retira `GestorAdjuntos` con su lista de copias, y `Eliminar`, `Cancelar` y *Descartar* dejan de tocar ficheros. `AdjuntosHelper` conserva solo la constante de tamaño y la carpeta que usa la migración. | M |
| E.16 | `feat: limpiar los temporales al cerrar y al arrancar` | `TemporalesAdjuntos.Limpiar()` en `OnExit` y en `OnStartup` (por si la aplicación se cerró mal). Prueba manual. | S |

---

## Etapa F. Cierre

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| F.1 | `docs: actualizar el README` | Documentos en la base de datos con el límite de 2 MB, instancia única, manejador de errores y fichero de registro, fechas coherentes, atajos y estados. | M |
| F.2 | `docs: actualizar AGENTS.md` | Estructura nueva (`InstanciaUnica`, `RegistroErrores`, `MigradorAdjuntos`, `TemporalesAdjuntos`, `DocumentosSolicitud`) y decisiones. | S |
| F.3 | `docs: alinear edicion-con-bufer.md y adjuntos-en-base-de-datos.md con el código` | Ajustar lo que haya cambiado durante la implementación. | S |
| F.4 | `chore: pasada final de sonarcloud-gate` | Verificar antes de abrir el PR. | S |

---

## 3. Orden y dependencias

- La Etapa A va primero: son los tres hallazgos obligatorios de la auditoría. Dentro de ella, A.5 antes de A.6 y A.7, y A.8 antes de A.9.
- B.1 antes de E.2 y E.9, que reutilizan la constante y el aviso.
- C.4 antes de C.5, para que el editor avise antes de que el modelo lance.
- La Etapa E va al final por ser la más grande. Orden interno: E.1 a E.3 (lo nuevo), E.4 a E.6 (migración), E.7 a E.12 (modelo y editor) y E.13 a E.16 (limpieza). `RutaCv` y `RutaCarta` no se eliminan (E.14) hasta que todo lo demás usa el contenido en la base de datos.
- E.10 y E.11 llevan la prueba de fallo de guardado: es el punto más delicado de la etapa.

## 4. Pruebas manuales

Los siguientes puntos no tienen test automático y hay que comprobarlos a mano en cada paso indicado:

| Paso | Qué comprobar |
|------|---------------|
| A.7 | Con una candidatura en «Descartado por la empresa», el desplegable no ofrece «Oferta aceptada». Con una en «Oferta aceptada», sí ofrece los demás cierres. |
| A.9 | Provocar un error (por ejemplo con un fichero de base de datos de solo lectura) y comprobar que sale el aviso, se escribe `errores.log` y la aplicación sigue abierta. |
| A.10 y A.11 | Escribir un nombre de empresa y pulsar Ctrl+S; escribir un salario y pulsar Ctrl+S; escribir una fecha y pulsar Ctrl+N; escribir texto y cerrar con Alt+F4. En todos los casos debe conservarse lo escrito o preguntar. |
| C.3 | Abrir una segunda copia con la primera minimizada y con la primera en la bandeja: debe aparecer la primera y la segunda no debe quedarse abierta. |
| E.6 | Arrancar con una base de datos y una carpeta `adjuntos` antiguas: los documentos deben abrirse desde la base y la carpeta debe desaparecer. |
| E.12 y E.16 | Abrir un CV y una carta, cerrar la aplicación y comprobar que `%TEMP%\GestorSolicitudes\adjuntos` queda vacío. |

## 5. Riesgos y puntos de control

| Riesgo | Cuándo | Cómo se controla |
|--------|--------|------------------|
| La Etapa 6 ya estaba hecha y A.2 o A.3 duplican trabajo | A.1 | Los tests se escriben primero; solo se arregla lo que falla. |
| `ChangeTracker.HasChanges()` verdadero sin cambios del usuario | A.4 | Solo se consulta con un editor abierto; test de apertura sin tocar nada. |
| `PropertyChanged` en cada pulsación provoca errores de validación mientras se escribe | A.10 | Solo en `TextBox` de texto. Los numéricos y las fechas se confirman con A.11. |
| El orden de `AplicarA` cambia y la regla de fechas da falsos errores | C.5 | Test del orden: `AplicarFechas` después de `AplicarRespuesta` y `AplicarEstado`. |
| Filas antiguas con fechas incoherentes dejan de poder guardarse | C.4, C.5 | El mensaje nombra el campo que hay que corregir. |
| Se pierde un adjunto al migrar | E.4, E.5 | Comprobación de tamaño y transacción antes de borrar el fichero; los ficheros se borran solo tras confirmar. |
| La tabla nueva no se crea en bases existentes | E.1 | `CREATE TABLE IF NOT EXISTS` en `EsquemaDb`, con test sobre una base creada con el esquema anterior. |
| La clave foránea no borra en cascada en SQLite | E.1 | Test de borrado de una candidatura con documentos; comprobar que la conexión activa `foreign_keys`. |
| El contenido pendiente se pierde tras un guardado fallido | E.10, E.11 | Test de fallo y reintento. |
| Quitar `RutaCv` rompe la lectura de la migración | E.14 | La migración lee las rutas con SQL, no con el modelo. |

## 6. Fuera de este plan

- **Hallazgo 6 de la auditoría:** el CI duplicado (`dotnet-desktop.yml` y `sonarcloud.yml`). Si hay protección de rama con una comprobación obligatoria, habrá que cambiarla en GitHub a mano.
- **Mejora de la migración de adjuntos**, que se revisará más adelante.
- **Vista previa de imágenes** en el visor.
- **Mejoras futuras anteriores:** el seguimiento propuesto en el pasado tras una reapertura, la traducción de los hitos del modelo, la prueba manual del desplegable de motivos y la preferencia del separador del CSV que se guarda aunque se cancele el destino.
