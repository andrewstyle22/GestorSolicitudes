# Estado del proyecto

Plan: `02-DOCS/plan-refactor-edicion.md`.

## Etapas
- ✅ 0: red de seguridad.
- ✅ 1: editor con búfer; la UI no ve la entidad.
- ✅ 2: historial de eventos (2.1–2.10).
- ✅ 3: modelo cerrado con constructor, métodos de negocio y `set` privados (3.1–3.17).
- ✅ R: estados y reapertura (R.1–R.22); detalle en `02-DOCS/decisiones/estados-y-reapertura.md`.
- ✅ 4: cierre de la refactorización: modelos sin setters públicos, documentación al día y gate de Sonar en verde.
- ✅ 5: correcciones tras la revisión final (5.1–5.20): guardado, cambios sin guardar, respuesta de la empresa, fecha de solicitud y CSV.
- ✅ 6: dos fallos hallados al revisar la Etapa 5 (6.1 y 6.2).

## Siguiente paso concreto
Nada pendiente del plan. Queda, cuando se pida, el push y la revisión del gate de SonarCloud del PR.

## Decisiones vigentes
- Búfer: copia al abrir, vuelca al Guardar; Cancelar descarta. La UI no bindea la entidad.
- `AplicarEventos` empareja por `EventoViewModel.Original` (nulo = nuevo), nunca por posición ni `Id`.
- `MainViewModel.Guardar` borra los `EventosEliminados`; el búfer no conoce la base.
- EF escribe por campo: `Eventos` es `IReadOnlyList` sobre campo privado y ningún modelo expone setters públicos.
- La nota de reapertura vive en `NotaReapertura` (300), no en `Notas`: solo se guarda en el hito.
- Si el guardado falla, el editor se recarga desde la entidad (que ya tiene los efectos de `CambiarEstado` y `Reabrir`) para que el reintento no los repita ni los pise. La recarga conserva `HayCambios` (`ConservarCambios`), porque la entidad sigue modificada en el contexto y salir de la candidatura debe avisar (6.1).
- Los hitos del modelo guardan su texto en castellano con el prefijo `Reabierta. ` compartido; traducirlos al mostrar queda para otra refactorización.
- Respuesta de la empresa (5.8): cuentan los estados abiertos distintos de «Enviada» y «Sin respuesta», y además «Descartado por la empresa», «Oferta aceptada» y «Oferta rechazada por mí». Solo «Retirada por mí» no cuenta. Los datos antiguos no se corrigen automáticamente.
- Cambios sin guardar: aviso con Guardar, Descartar y Cancelar al cambiar de fila, pulsar *Nueva* o *Duplicar* y cerrar la ventana; tras Guardar, la acción pedida solo sigue si el guardado fue bien (5.2 a 5.7).
- CSV: separador a elegir (`;`, `,` o tabulador) en una ventana con Aceptar y Cancelar, recordado en `%APPDATA%`; columnas de motivo de cierre y fecha de reapertura; apóstrofo delante de los textos que empiezan por `=`, `+`, `-` o `@` (5.11 a 5.16).
- Cambiar `FechaSolicitud` mueve el hito de envío y el seguimiento propuesto solo si coincidían con la fecha anterior; el seguimiento no se mueve si hay reapertura (5.9 y 5.10).
- El aviso de candidatura repetida consulta la base sin seguimiento (`AsNoTracking`), después de guardar y descartando por `Id` la propia candidatura: no depende de lo que `Recargar` haya dejado en `Local` ni se dispara en guardados con la base rota (6.2).

## Reglas de cada commit
- `dotnet test` en verde (avisos son errores) y `dotnet format --verify-no-changes`.
- Antes de commitear: `sonarcloud-gate` local; el gate llega tras el push.
- XAML nuevo: `<Page>` en el csproj; todo texto visible con clave es/en/de.
- Mensaje en español; un commit, una idea reversible. No hacer push salvo que se pida.
- Cada paso del plan, su commit con el mensaje del plan; no fusiones pasos.
- Al terminar cada paso, actualiza este fichero en el mismo commit: marca el paso como hecho, deja el siguiente en «Siguiente paso concreto» y anota las decisiones o puntos de control nuevos; quita lo que ya no aplique.

## Mejoras futuras (fuera del plan)
- Las copias de adjuntos que quedan en disco si se cierra la app antes de guardar o cancelar.
- Si se vacía el seguimiento mucho después de reabrir, se propone `FechaReapertura + 7`, que puede estar en el pasado.
- Guardar los hitos del modelo como valores únicos y traducirlos al mostrarlos.
- Probar a mano el desplegable de motivos al cambiar de «Descartado» a «Retirada» (WPF puede vaciar `SelectedValue` al reemplazar `MotivosDelEstado`).
- La preferencia del separador del CSV se guarda aunque luego se cancele la elección del destino.
