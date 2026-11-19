# Estado del proyecto

Plan: `02-DOCS/plan-refactor-ponytail.md` (leer solo la etapa en curso; nace de `02-DOCS/auditoria-ponytail.md`). Anterior: `plan-refactor-edicion.md` (0–5; su 6 la sustituye A).

## Etapas Ponytail
- ✅ A: guardado, navegación, errores y atajos (A.0–A.11).
- ✅ B: límite de 2 MB (B.1).
- ✅ C: instancia única y fechas (C.1–C.5).
- ✅ D: detalles menores (D.1–D.3).
- ✅ E: documentos en la base de datos (E.1–E.16).
- ✅ F: documentación y cierre (F.1–F.4).

## Siguiente paso concreto
Plan Ponytail terminado, con las pruebas manuales hechas. Mergear el PR #32 (Quality Gate en verde). Después, en una rama aparte, la reorganización en carpetas (ver «Fuera del plan»).

## Decisiones vigentes
- Ningún modelo expone setters públicos ni la UI bindea la entidad: la edición va por el búfer del editor y EF escribe por campo.
- `AplicarEventos` empareja por `Original`, nunca por posición ni `Id`; `Guardar` borra los `EventosEliminados`.
- Solo se prohíbe cerrado → «Oferta aceptada» (`AdmiteTransicion`); «Oferta aceptada» → otros cierres se permite como corrección; `EstadosDisponibles` sale de `estadoOriginal` y se recalcula al cambiar de idioma.
- `ResolverCambiosPendientes` también mira `ChangeTracker.HasChanges()`: un guardado fallido deja la entidad sucia.
- `RegistroErrores` nunca lanza y se recorta a 1 MB; solo `ManejadorErrores.Avisar` tiene test y los enganches de `App` van excluidos de cobertura.
- Documentos: máximo 2 MB y `LectorAdjuntos` mide antes de reservar; el aviso lo pone el ViewModel.
- `DocumentosSolicitud` (BLOB, sin navegación) se lee solo al abrir; `Solicitud` guarda el nombre y `TieneCv`/`TieneCarta` salen de él; el contenido pendiente vive en el editor y `Guardar` lo escribe tras el primer `SaveChanges`, conservándolo si falla.
- `VerificarColumnas_CubreTodasLasColumnasDelModelo` contrasta el modelo de EF con la base tras `VerificarColumnasFaltantes`; el helper de la base vieja reproduce la primera versión real, para que una propiedad nueva sin registrar en `nuevas` rompa el test.
- Abrir usa `%TEMP%\GestorSolicitudes\adjuntos`, limpiado al arrancar y al cerrar; el visor no vuelca tipos desconocidos y acota DOCX/ODT a 10 MB.
- `MigradorAdjuntos` corre al arrancar, es idempotente, ignora bases sin columnas de ruta, registra los fallos y borra los ficheros solo tras confirmar. Un fichero que existe pero no se puede leer (bloqueado, tamaño cambiado) conserva su ruta y su nombre y se reintenta en el siguiente arranque; CV y carta se resuelven por separado. Un fichero que ya no existe sí deja el documento vacío.
- `InstanciaUnica`: la segunda copia avisa y se cierra sin abrir base ni ventana; `App` la crea antes de la base y la libera en `OnExit`.
- Ninguna fecha anterior a `FechaSolicitud`; `AplicarFechas` va después de `AplicarRespuesta` y `AplicarEstado`.
- `Paginar` repone la selección con `SeleccionarSinAviso` para no contradecir la fila resaltada.
- Reapertura solo con `Reabrir`, con nota obligatoria y respetando el seguimiento editado; motivo de cierre obligatorio y filtrado en los tres cierres negativos.
- Respuesta de la empresa: cuentan los abiertos salvo Enviada y Sin respuesta, más Descartado por la empresa y las dos ofertas.
- Cambios sin guardar: Guardar/Descartar/Cancelar al cambiar de fila, Nueva, Duplicar y cerrar; tras Guardar la acción sigue solo si guardó.
- CSV: separador recordado, motivo y reapertura, apóstrofo ante `= + - @`, tabulador o retorno, aun tras espacios.
- `FechaSolicitud` mueve el hito de envío y el seguimiento solo si coincidían.
- Atajos: `PropertyChanged` solo en texto; se confirma el control con el foco antes de Ctrl+S/N y cerrar.
- `RespuestaEmpresa` y `Notas` son texto libre sin límite.
- La reapertura guarda la nota (`NotaReapertura`, 300 caracteres) solo en el hito, no admite «Sin respuesta» como destino y `FechaReapertura` (solo la última) alimenta `DiasProceso`, la insignia ↻ y el borde `#DB2777`.
- Los hitos que crea el modelo guardan texto en castellano con el prefijo compartido `Reabierta. `; traducirlos al mostrar queda pendiente.

## Reglas de cada commit
- `dotnet test` en verde (avisos son errores) y `dotnet format --verify-no-changes`.
- `sonarcloud-gate` local antes de commitear; el gate llega tras el push.
- XAML nuevo: `<Page>` en el csproj; todo texto visible con clave es/en/de.
- Un commit, una idea reversible, mensaje en español; sin push salvo que se pida.
- Cada paso del plan, su commit con su mensaje y su test; no fusiones pasos.
- Al terminar cada paso, este fichero se actualiza en el mismo commit.

## Puntos de control
- Pruebas manuales de la sección 4 del plan Ponytail: hechas y superadas.
- `TreatWarningsAsErrors`: no dejar miembros sin usar entre commits.

## Fuera del plan
- Reorganización en carpetas, en una rama aparte tras mergear Ponytail, con los namespaces siguiendo las carpetas. Antes de mover nada hay que hacer `FuentesTests` independiente de la estructura (rutas fijas y `TopDirectoryOnly`) y actualizar los `<Page>` del csproj; `IDialogoService` y `DialogosNulos` se quedan en `ViewModels` y `DialogoService` en `Views`. Se activará IDE0130 como aviso al terminar.
- Hallazgo 6 (CI duplicado) y protección de rama en GitHub.
- Migración de adjuntos y vista previa de imágenes.
- Seguimiento `FechaReapertura + 7` puede caer en el pasado.
- Hitos como valores únicos traducidos al mostrar.
- La preferencia del separador CSV se guarda aunque se cancele.
