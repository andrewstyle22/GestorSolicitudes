# Edición con búfer y modelo encapsulado

El panel de detalle bindeaba sus campos en dos direcciones directamente contra la entidad `Solicitud`:
cualquier tecla modificaba el modelo sin pasar por ninguna regla, y *Cancelar* dependía de que EF
descartara los cambios. Ahora la interfaz escribe en un búfer (`SolicitudEdicionViewModel`) y el
modelo solo cambia al guardar, mediante métodos de negocio. Se hizo por etapas, grupo de campos a grupo
de campos, para que la aplicación compilara y pasara los tests en cada commit.

## Decisiones

**Un búfer, no un wrapper por entidad.** El editor copia los datos al abrir (`CargarDesde`), los
valida mientras se escriben (`ObservableValidator`) y los vuelca al guardar (`AplicarA`). *Cancelar* lo
descarta. No se llama al modelo por cada tecla: escribir «35000» pasaría por estados intermedios
inválidos.

**La tabla no se envuelve.** Sus filas siguen siendo entidades de solo lectura y la lista se recarga
tras cada guardado. El `TwoWay` de `SelectedItem` del `DataGrid` es legítimo.

**El modelo se protege a sí mismo.** `Solicitud` y `Evento` no tienen setters públicos: se crean con los
datos obligatorios, tienen un constructor privado para EF y cambian por métodos por intención
(`ActualizarIdentificacion`, `ActualizarOferta`, `ActualizarCondiciones`, `CambiarEstado`, `Reabrir`,
`AgregarEvento`…). Los métodos aplican las reglas y lanzan `ArgumentException` si algo no vale, de modo
que el editor valida antes y el modelo es la última barrera.

**EF accede por campo.** `Id` de `Solicitud` no tiene setter, `FechaReapertura` tiene campo de respaldo
y `Evento` usa `UsePropertyAccessMode(Field)`. `Eventos` es un `IReadOnlyList` sobre un campo privado.

**El historial sigue el mismo camino.** Los hitos editables son `EventoViewModel` y se emparejan con su
entidad por `Original`, nunca por posición ni por `Id`. Al guardar se crean los nuevos, se actualizan los
existentes y se borran los quitados; el búfer no conoce la base de datos.

**El orden de `AplicarA` importa.** `AplicarRespuesta` va antes que `AplicarEstado`: al salir de «Sin
respuesta» el modelo rellena `FechaPrimeraRespuesta`, y si el editor la escribiera después, la pisaría
con el valor vacío.

**Un guardado fallido no deja el editor desfasado.** Si `SaveChanges` falla, la entidad ya tiene los
efectos de `CambiarEstado` y de `Reabrir` (por ejemplo `FechaCierre` o `FechaPrimeraRespuesta`, que el
modelo solo rellena en la transición); el editor se recarga desde ella para que el reintento no los
repita ni los pise con los valores antiguos del búfer.

**El editor sabe si hay cambios.** `HayCambios` se activa al tocar cualquier campo editable o el
historial y se apaga al cargar desde la entidad. Con él, `ResolverCambiosPendientes()` pregunta
Guardar, Descartar o Cancelar antes de cambiar de fila, abrir *Nueva* o *Duplicar* y cerrar la
ventana. Con Guardar, la acción pedida sigue solo si `TryGuardar()` terminó bien.

**Cambiar `FechaSolicitud` arrastra su hito y el seguimiento propuesto.** El modelo mueve el hito
«Solicitud enviada» y el seguimiento (fecha anterior más 7 días) solo si seguían en la fecha anterior
y no hay reapertura; el editor hace lo mismo sobre el búfer para que el panel lo muestre al instante
y `AplicarEventos` no lo devuelva a su valor anterior.

**Los ficheros adjuntos solo se borran tras guardar bien.** Quitar o sustituir un adjunto no toca el
disco hasta que el guardado funciona; *Cancelar* borra las copias que no se llegaron a guardar.

## Consecuencias

- Cada propiedad nueva se escribe en tres sitios: el modelo (con su método y su constante de longitud),
  el editor (`CargarDesde`, `Aplicar…` y validación) y `EsquemaDb`. El procedimiento está en `AGENTS.md`.
- Un test por reflexión vigila que ningún modelo exponga setters públicos.
- Los hitos que crea el modelo guardan su texto en castellano (el modelo no puede usar `Localizacion`);
  solo se traduce el tipo. Guardar valores únicos y traducirlos al mostrar queda para otra
  refactorización.
