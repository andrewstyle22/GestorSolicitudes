# Estado del proyecto

Plan: `02-DOCS/plan-refactor-edicion.md`.

## Etapas
- ✅ 0: red de seguridad.
- ✅ 1: editor con búfer; la UI no ve la entidad.
- ✅ 2: historial de eventos (2.1–2.10).
- ✅ 3: modelo cerrado con constructor, métodos de negocio y `set` privados (3.1–3.17).
- ✅ R: estados y reapertura (R.1–R.22); detalle en `02-DOCS/decisiones/estados-y-reapertura.md`.
- ✅ 4: cierre de la refactorización: modelos sin setters públicos, documentación al día y gate de Sonar en verde.
- 🔄 5: correcciones tras la revisión final (5.1–5.20); detalle en el plan.

## Siguiente paso concreto
5.19 (`docs: actualizar README, AGENTS.md y estados-y-reapertura.md`): CSV (separador, columnas y apóstrofo), aviso de cambios sin guardar, la fecha de solicitud que mueve el hito y qué estados cuentan como respuesta de la empresa. No hacer push sin que se pida.

## Decisiones vigentes
- Búfer: copia al abrir, vuelca al Guardar; Cancelar descarta. La UI no bindea la entidad.
- `AplicarEventos` empareja por `EventoViewModel.Original` (nulo = nuevo), nunca por posición ni `Id`.
- `MainViewModel.Guardar` borra los `EventosEliminados`; el búfer no conoce la base.
- EF escribe por campo: `Eventos` es `IReadOnlyList` sobre campo privado y ningún modelo expone setters públicos.
- La nota de reapertura vive en `NotaReapertura` (300), no en `Notas`: solo se guarda en el hito.
- Si el guardado falla tras `Reabrir`, el editor se recarga desde la entidad para no repetir la reapertura.
- Los hitos del modelo guardan su texto en castellano con el prefijo `Reabierta. ` compartido; traducirlos al mostrar queda para otra refactorización.
- Respuesta de la empresa (5.8): cuentan los estados abiertos distintos de «Enviada» y «Sin respuesta», y además «Descartado por la empresa», «Oferta aceptada» y «Oferta rechazada por mí». Solo «Retirada por mí» no cuenta. Los datos antiguos no se corrigen automáticamente.
- Cambios sin guardar: aviso con Guardar, Descartar y Cancelar al cambiar de fila, pulsar *Nueva* o *Duplicar* y cerrar la ventana; tras Guardar, la acción pedida solo sigue si el guardado fue bien (5.2 a 5.7).
- CSV: separador a elegir (`;`, `,` o tabulador; `CsvExportador.Construir` usa `;` por defecto) en una ventana con Aceptar y Cancelar (`VentanaSeparadorCsv`, vía `IDialogoService.ElegirSeparadorCsv`), recordado en `%APPDATA%\GestorSolicitudes\csv.txt` (`PreferenciaCsv`, patrón de `PreferenciaIdioma`: tolerante a fallos y solo acepta esos tres; al leer no se hace `Trim` porque el tabulador es válido; `PreferenciaCsv.EsValido` es la única lista de separadores admitidos y la ventana cae a `;` si recibe otro); columnas de motivo de cierre (traducido, no el entero) y fecha de reapertura; apóstrofo delante de los textos que empiezan por `=`, `+`, `-`, `@`, tabulador o retorno de carro; se comprueba el primer carácter original, antes de sustituir saltos de línea, y las fechas y números no pasan por `Escapar` (5.11 a 5.16). `ExportarCsv` pregunta el separador antes del destino (cancelar no exporta), lo guarda en la preferencia y construye el CSV con él; el ViewModel recibe la ruta de la preferencia en el constructor para que los tests no toquen `%APPDATA%`.
- Cambiar `FechaSolicitud` mueve el hito de envío y el seguimiento propuesto solo si coincidían con la fecha anterior; el seguimiento no se mueve si hay reapertura. Se aplica en el modelo (`ActualizarFechas`) y en el búfer (`OnFechaSolicitudChanged`), que recuerda la fecha anterior y si la candidatura se reabrió (5.9 y 5.10).
- La descripción de los eventos se valida en el búfer: `Evento.LongitudMaximaDescripcion` (500) es la única fuente del `[MaxLength]` y de la validación; `EventoViewModel` es `ObservableValidator` y `SolicitudEdicionViewModel.Validar()` devuelve si el búfer y sus eventos son válidos, que es lo que mira `MainViewModel` antes de guardar (5.18).

## Reglas de cada commit
- `dotnet test` en verde (avisos son errores) y `dotnet format --verify-no-changes`.
- Antes de commitear: `sonarcloud-gate` local; el gate llega tras el push.
- XAML nuevo: `<Page>` en el csproj; todo texto visible con clave es/en/de.
- Mensaje en español; un commit, una idea reversible. No hacer push salvo que se pida.
- Cada paso del plan, su commit con el mensaje del plan; no fusiones pasos.
- Al terminar cada paso, actualiza este fichero en el mismo commit: marca el paso como hecho, deja el siguiente en «Siguiente paso concreto» y anota las decisiones o puntos de control nuevos; quita lo que ya no aplique.
