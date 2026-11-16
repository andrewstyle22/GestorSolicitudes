# Estado del proyecto

Plan en curso: `02-DOCS/plan-refactor-ponytail.md` (leer solo la sección en curso). Nace de la auditoría `02-DOCS/auditoria-ponytail.md`.
Plan anterior, ya terminado: `02-DOCS/plan-refactor-edicion.md` (etapas 0 a 5; su Etapa 6 queda sustituida por la Etapa A del plan actual).

## Etapas del plan anterior
- ✅ 0 a 5: red de seguridad, editor con búfer, historial, modelo cerrado, estados y reapertura (R), cierre y correcciones finales.

## Etapas de la refactorización Ponytail
- ✅ A: guardado y navegación (hallazgos 1, 2 y 3, y manejador global de excepciones), A.0–A.11. Hecho entero; pendiente la prueba manual de la sección 4.
- ✅ B: límite de 2 MB para CV y carta, B.1. Hecho; pendiente la prueba manual de la sección 4.
- ✅ C: instancia única y fechas coherentes (hallazgos 4 y 5), C.1–C.5. Hecho.
- ✅ D: detalles menores (hallazgos 7, 9 y 10), D.1–D.3. Hecho.
- ⬜ E: documentos en la base de datos (tabla `DocumentosSolicitud`, migración y limpieza), E.1 hecho; quedan E.2–E.16.
- ⬜ F: documentación y cierre, F.1–F.4.

## Siguiente paso concreto
E.2 (`feat: lector de adjuntos con límite`): `LectorAdjuntos.Leer(ruta)` devuelve los bytes y rechaza lo que supere 2 MB (comparte la constante de B.1). Tests. No hacer push sin que se pida.

## Decisiones vigentes de esta refactorización
- Etapa 6 anterior: se comprueba con tests (A.1). Los tests que ya pasan se commitean como regresión; los que fallen se commitean con su arreglo (A.2, A.3 o A.4). A.4 se hace siempre.
- `ResolverCambiosPendientes` pregunta también si `db.ChangeTracker.HasChanges()` es verdadero con un editor abierto (A.4).
- Estados: **Oferta aceptada → otros cierres se permite** (corrección cerrado → cerrado). Solo cerrado → «Oferta aceptada» lanza en el modelo; el editor lo valida (A.6) y el desplegable no lo ofrece (A.7). Las reglas de transición viven en `EstadoSolicitudExtensiones.AdmiteTransicion` (A.5). `EstadosDisponibles` se arma desde `estadoOriginal` y se recalcula en `RefrescarListasDelEstado` al cambiar de idioma (A.7).
- Manejador global de excepciones: registra en `%APPDATA%\GestorSolicitudes\errores.log`, avisa y no cierra (A.8 y A.9). `ManejadorErrores.Avisar` (Núcleo) es la parte con test; los enganches de `App` son de ciclo de vida y van excluidos de cobertura.
- `RegistroErrores` (A.8): añade al final del fichero y, pasado 1 MB, se queda con la mitad reciente (desde el siguiente salto de línea). Nunca lanza; si no puede escribir, solo deja un `Debug.WriteLine`. Los tests usan `RegistrarEn(ruta, …)`.
- Atajos: `UpdateSourceTrigger=PropertyChanged` en los `TextBox` de texto (A.10) y confirmación del control con el foco antes de Ctrl+S, Ctrl+N y cerrar (A.11).
- Documentos: máximo 2 MB (2 × 1024 × 1024 bytes); más grande, aviso «El documento no puede ser mayor a 2 megabytes» y no se adjunta (B.1).
- Tabla `DocumentosSolicitud` (E.1): entidad sin navegación desde `Solicitud`; `SolicitudId` es PK y FK con borrado en cascada; `Cv` y `Carta` son `byte[]?` y solo cambian por `Asignar…`/`Quitar…`. `EsquemaDb.VerificarColumnasFaltantes` la crea con `CREATE TABLE IF NOT EXISTS` en bases antiguas. La conexión de EF activa `foreign_keys` (lo comprueba un test).
- Instancia única: la segunda copia avisa a la primera y se cierra sin abrir la base ni crear ventana; la primera muestra su ventana al recibir el aviso (C.1 a C.3). `InstanciaUnica` (Helpers) usa un `Mutex` con nombre (`EsPrimera` sale de `createdNew`) y un `EventWaitHandle` con nombre para el aviso; el constructor acepta nombre para los tests. `Dispose` no llama a `ReleaseMutex` (es del hilo que lo creó), solo cierra los manejadores; `EsperarAviso` sale al liberarse (bandera + `Set`). `App.OnStartup` crea la instancia tras instalar los manejadores de errores y antes de la base; `App.OnExit` la libera. La primera copia escucha el aviso en una tarea de fondo (C.3) y, en el hilo de la interfaz, llama a `MainWindow.MostrarVentana` (internal), que ya usan el doble clic y el menú de la bandeja; si el despachador está cerrado, el aviso se ignora.
- Fechas: primera respuesta, entrevista, cierre y próximo seguimiento no pueden ser anteriores a `FechaSolicitud`. Regla en el modelo (`ActualizarFechas`, C.5) y mensaje en el editor (C.4). El modelo exime los `hoy` que rellena él mismo (primera respuesta y cierre). `AplicarFechas` va después de `AplicarRespuesta` y `AplicarEstado`; hay test del orden. En el editor, `ValidarFechaPosteriorALaSolicitud` se aplica a las cuatro fechas, formatea `Mensaje.FechaAnteriorASolicitud` con la etiqueta `Seguimiento.*` del campo y se revalida al cambiar `FechaSolicitud`; las filas antiguas incoherentes se pueden cargar pero no guardar hasta corregirlas (los tests con esas fechas se ajustaron en C.4 y C.5).
- Selección y recarga: tras repaginar, `Paginar` repone `solicitudAbierta` con `SeleccionarSinAviso` si sigue en la página (D.2); el panel abierto y la fila resaltada no se contradicen.
- Documentos en la base de datos: tabla aparte `DocumentosSolicitud` (`SolicitudId`, `Cv` y `Carta` BLOB anulables, borrado en cascada), sin navegación desde `Solicitud`, leída solo al pulsar «abrir». `Solicitud` conserva `NombreOriginalCv` y `NombreOriginalCarta`; `TieneCv` y `TieneCarta` salen de ellos. El contenido nuevo vive en el editor hasta guardar. Abrir escribe un temporal en `%TEMP%\GestorSolicitudes\adjuntos`, limpiado al cerrar y al arrancar. No ahorra espacio; aporta copia de seguridad en un solo fichero.
- Migración al arrancar: con SQL, transacción, comprobación de tamaño, y borrado de los ficheros antiguos tras confirmar. Se mejorará más adelante.
- `RutaCv` y `RutaCarta` no se eliminan del modelo (E.14) hasta que todo use el contenido de la base de datos; las columnas quedan sin usar en las bases antiguas.
- El visor no vuelca tipos desconocidos como texto y acota DOCX y ODT (E.13).

## Decisiones vigentes heredadas
- Búfer: copia al abrir, vuelca al Guardar; Cancelar descarta. La UI no bindea la entidad.
- `AplicarEventos` empareja por `EventoViewModel.Original` (nulo = nuevo), nunca por posición ni `Id`. `MainViewModel.Guardar` borra los `EventosEliminados`; el búfer no conoce la base.
- Ningún modelo expone setters públicos; EF escribe por campo. `AplicarRespuesta` va antes que `AplicarEstado`.
- Reapertura: solo con `Solicitud.Reabrir`, con nota obligatoria `NotaReapertura` (300 caracteres) que solo se guarda en el hito, no a «Sin respuesta», y respetando el seguimiento editado. `FechaReapertura` (solo la última) alimenta `DiasProceso`, la insignia ↻ y el borde magenta.
- Motivo de cierre obligatorio en Descartado, Retirada y Oferta rechazada por mí, filtrado por estado, con `NoIndicado` preseleccionado. `RespuestaEmpresa` y `Notas` son texto libre sin límite.
- Respuesta de la empresa (5.8): cuentan los estados abiertos salvo «Enviada» y «Sin respuesta», más «Descartado por la empresa», «Oferta aceptada» y «Oferta rechazada por mí». Solo «Retirada por mí» no cuenta.
- Cambios sin guardar: aviso con Guardar, Descartar y Cancelar al cambiar de fila, *Nueva*, *Duplicar* y cerrar la ventana; tras Guardar, la acción solo sigue si el guardado fue bien.
- CSV: separador a elegir (`;`, `,` o tabulador) y recordado; columnas de motivo de cierre y fecha de reapertura; apóstrofo delante de los textos cuyo primer carácter visible es `=`, `+`, `-`, `@`, tabulador o retorno de carro; los espacios iniciales se ignoran al decidirlo (D.3) y el apóstrofo se antepone a la celda completa.
- Cambiar `FechaSolicitud` mueve el hito de envío y el seguimiento propuesto solo si coincidían con la fecha anterior.
- Los hitos que crea el modelo guardan su texto en castellano con el prefijo `Reabierta. ` compartido; traducirlos al mostrar queda para otra refactorización.

## Reglas de cada commit
- `dotnet test` en verde (avisos son errores) y `dotnet format --verify-no-changes`.
- Antes de commitear: `sonarcloud-gate` local; el gate llega tras el push.
- XAML nuevo: `<Page>` en el csproj; todo texto visible con clave es/en/de.
- Mensaje en español; un commit, una idea reversible. No hacer push salvo que se pida.
- Cada paso del plan, su commit con el mensaje del plan; no fusiones pasos.
- Los tests de reproducción se commitean junto con su arreglo.
- Al terminar cada paso, actualiza este fichero en el mismo commit: marca el paso como hecho, deja el siguiente en «Siguiente paso concreto» y anota las decisiones o puntos de control nuevos; quita lo que ya no aplique.

## Puntos de control
- Pruebas manuales (sección 4 del plan): A.7, A.9, A.10 y A.11, C.3, E.6, E.12 y E.16.
- `TreatWarningsAsErrors`: no dejar miembros sin usar entre commits.

## Fuera del plan
- Hallazgo 6 de la auditoría (CI duplicado): se revisará después. Si hay protección de rama con una comprobación obligatoria, habrá que cambiarla en GitHub a mano.
- Mejora de la migración de adjuntos y vista previa de imágenes.
- Si se vacía el seguimiento mucho después de reabrir, se propone `FechaReapertura + 7`, que puede estar en el pasado.
- Guardar los hitos del modelo como valores únicos y traducirlos al mostrarlos.
- La preferencia del separador del CSV se guarda aunque luego se cancele el destino.
