# Estado del proyecto

Sin plan en curso. Los planes completados, las revisiones de clean code y la auditoría Ponytail se retiraron; su historial está en git.

## Etapas Ponytail
- ✅ A: guardado, navegación, errores y atajos (A.0–A.11).
- ✅ B: límite de 2 MB (B.1).
- ✅ C: instancia única y fechas (C.1–C.5).
- ✅ D: detalles menores (D.1–D.3).
- ✅ E: documentos en la base de datos (E.1–E.16).
- ✅ F: documentación y cierre (F.1–F.4).

## Etapas Seguridad
- ✅ S0: preparación (S0.1).
- ✅ S1: temporales y arranque (S1.1).
- ✅ S2: migración de adjuntos antiguos (S2.1–S2.3).
- ✅ S3: apertura de documentos guardados (S3.1).
- ✅ S4: exportación CSV (S4.1).
- ✅ S5.1: enlaces con credenciales embebidas.
- ✅ S5.2: forma canónica al shell.
- ✅ S6: documentación y cierre (S6.1–S6.2).
- 🔄 Fuera del plan: reorganización en carpetas.

## Siguiente paso concreto
Sin etapa en curso. Pendientes: las pruebas manuales S1.1 y S2.2 (ver «Puntos de control pendientes»), y, cuando se decida, el CI y la reorganización en carpetas (ver «Fuera del plan»).

## Decisiones vigentes
- Ningún modelo expone setters públicos ni la UI bindea la entidad: la edición va por el búfer del editor y EF escribe por campo, para proteger las reglas de negocio.
- `AplicarEventos` empareja por `Original`, no por posición ni `Id`, para conservar los hitos al reordenar.
- Cerrado → abierto solo por `Reabrir`; «Oferta aceptada» puede corregirse a otro cierre, pero «Oferta rechazada por mí» no pasa a «Oferta aceptada»; el motivo es obligatorio en los tres cierres negativos, para impedir transiciones inválidas.
- `ResolverCambiosPendientes` mira también `ChangeTracker.HasChanges()`: un guardado fallido deja la entidad sucia y debe avisar.
- `RegistroErrores` nunca lanza y se recorta a 1 MB, para que un fallo al registrar no tumbe la aplicación.
- Los documentos van en `DocumentosSolicitud` como BLOB sin navegación: se leen solo al abrir y el contenido pendiente vive en el editor, para no cargarlos al listar ni perderlos si falla Guardar.
- El límite de 2 MB es invariante del almacén: se aplica al adjuntar, migrar y abrir, para tener un solo tope.
- `VerificarColumnas_CubreTodasLasColumnasDelModelo` contrasta modelo y base tras `VerificarColumnasFaltantes`, para detectar columnas nuevas sin registrar.
- Los temporales se limpian al arrancar y al cerrar solo la copia principal, para no borrar documentos abiertos por otra instancia; el visor no vuelca tipos desconocidos y acota DOCX/ODT a 10 MB.
- `MigradorAdjuntos` es idempotente y solo borra un fichero tras copiarlo a la base, para no perder documentos.
- Lo que no se puede migrar (ilegible, mayor de 2 MB o fuera de la carpeta) conserva su ruta y su nombre, sin BLOB y registrado, para reintentarlo en el próximo arranque; las uniones de directorio quedan fuera del modelo de amenaza.
- La migración reutiliza `LectorAdjuntos.Leer`, para compartir tope de tamaño y `FileShare.Read`.
- `InstanciaUnica` se crea antes de abrir la base: la segunda copia avisa y se cierra, para mantener una sola sesión.
- Ninguna fecha anterior a `FechaSolicitud`; `AplicarFechas` va después de `AplicarRespuesta` y `AplicarEstado`, para no pisar la primera respuesta.
- `Paginar` repone la selección con `SeleccionarSinAviso`, para que la fila resaltada no contradiga la selección.
- La reapertura exige nota (300 caracteres, solo en el hito), no admite «Sin respuesta» como destino y respeta el seguimiento editado; `FechaReapertura` alimenta `DiasProceso`, insignia y borde, para no confundir reapertura con un estado.
- Respuesta de la empresa: cuentan los abiertos salvo Enviada y Sin respuesta, más Descartado y las dos ofertas; «Retirada por mí» no, porque la decide el usuario.
- Cambios sin guardar: Guardar/Descartar/Cancelar al cambiar de fila, Nueva, Duplicar o cerrar; la acción sigue solo si guardó, para no perder ediciones.
- CSV: separador recordado, motivo y reapertura, apóstrofo ante fórmulas o saltos de línea, para neutralizar celdas peligrosas.
- `FechaSolicitud` mueve el hito de envío y el seguimiento solo si coincidían, para no alterar lo editado a mano.
- Atajos: `PropertyChanged` solo en texto y se confirma el control con el foco antes de Ctrl+S/N y cerrar, para no perder la edición en curso.
- `RespuestaEmpresa` y `Notas` siguen siendo texto libre sin límite, porque esa fue la decisión de negocio.
- Los hitos del modelo se guardan en castellano con el prefijo `Reabierta. `; traducirlos al mostrar queda pendiente.
- Enlaces: se rechazan credenciales embebidas y al shell se pasa `AbsoluteUri`, para evitar interpretaciones distintas del URL.
- Modelo de amenaza en `README.md`: app local de un solo usuario, datos en `%APPDATA%` Roaming con ACL heredadas, registro con rutas y SQL, uniones de directorio fuera del modelo y temporales sin cifrar hasta salir.
- El CI queda fuera del plan (opción C): hay que decidir si se borra `dotnet-desktop.yml` antes de endurecer los workflows.
- Graphify: graphify-out/ no se versiona y Sonar la excluye.

## Reglas de cada commit
- `dotnet test` en verde (avisos son errores) y `dotnet format --verify-no-changes`.
- `sonarcloud-gate` local antes de commitear; el gate llega tras el push.
- XAML nuevo: `<Page>` en el csproj; todo texto visible con clave es/en/de.
- Un commit, una idea reversible, mensaje en español; sin push salvo que se pida.
- Cada paso del plan, su commit con su mensaje y su test; no fusiones pasos.
- Al terminar cada paso, este fichero se actualiza en el mismo commit.

## Puntos de control pendientes
- S1.1: con un documento abierto en el visor, lanzar una segunda copia; el documento sigue abierto y, al cerrar la primera, la carpeta de temporales se vacía.
- S2.2: con un fichero de más de 2 MB referenciado desde una base antigua, un solo aviso al arrancar, el fichero no se borra y la ruta sigue en la base.
- Punto de control A (migración y apertura, sin pérdida de rutas ni ficheros y un solo tope de 2 MB): sin registro de ejecución.
- `TreatWarningsAsErrors`: no dejar miembros sin usar entre commits.

## Fuera del plan
- Reorganización en carpetas, en una rama aparte, con los namespaces siguiendo las carpetas. Antes de mover nada hay que hacer `FuentesTests` independiente de la estructura (rutas fijas y `TopDirectoryOnly`) y actualizar los `<Page>` del csproj; `IDialogoService` y `DialogosNulos` se quedan en `ViewModels` y `DialogoService` en `Views`. Se activará IDE0130 como aviso al terminar.
- Hallazgo 6 (CI duplicado) y protección de rama en GitHub.
- Vista previa de imágenes en el visor.
- Seguimiento `FechaReapertura + 7` puede caer en el pasado.
- Hitos como valores únicos traducidos al mostrar.
- La preferencia del separador CSV se guarda aunque se cancele.
- Endurecimiento de los workflows del CI (`permissions: contents: read`, `persist-credentials: false`, acciones fijadas por SHA, versión fija de `dotnet-sonarscanner`, `SONAR_TOKEN` solo por entorno, SDK de `global.json` alineado), después de decidir si se borra `dotnet-desktop.yml` (duplicado de `sonarcloud.yml`); si se borra, revisar la protección de rama en GitHub.
- `DialogosNulos` es el valor por defecto del constructor de `MainViewModel` y escribe un `Debug.WriteLine`; lo correcto es hacer el servicio obligatorio y mover `DialogosNulos` a los tests (hay unos 149 `new MainViewModel(` en los tests: usar un helper).
- `DateTime.Today` (20 usos en producción): refactor aparte, solo del reloj (`TimeProvider` o un reloj estático); hay que decidir cómo llega a `DiasProceso` y `SeguimientoPendiente`.
- `Paginador.PuedeIrAInicio` y `PuedeRetroceder` son la misma expresión: derivar una de la otra.
- `MainViewModel` tiene más de 1400 líneas. Una clase `partial` no lo resuelve; candidatos a extraer: `EmbudoViewModel`, paginación, flujo de guardado y reapertura, exportación CSV y documentos.
- Mejora de la migración de adjuntos.
- Probar a mano el desplegable de motivos al pasar de «Descartado» a «Retirada».
