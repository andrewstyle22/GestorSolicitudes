# Estado del proyecto

Plan completo: 02-DOCS/plan-refactor-edicion.md (leer solo la sección de la etapa en curso).

## Etapas
- ✅ Etapa 0: red de seguridad; tests con `SolicitudBuilder` y `RellenarObligatorios`.
- ✅ Etapa 1: editor con búfer conectado al `MainViewModel`; la UI ya no ve la entidad.
- 🔄 Etapa 2 (en curso): historial de eventos en el editor y encapsulado en el modelo (2.1–2.10); 2.1–2.7 hechos.
- ⬜ Etapa 3 (opcional): cerrar el modelo con constructor, métodos de negocio y `set` privados (3.1–3.17).
- ⬜ Etapa 4: cierre; test por reflexión, limpiar `ReglasSolicitud` y documentación (4.1–4.6).

## Siguiente paso concreto
Commit 2.8 (`refactor: configurar EF con acceso por campo para Eventos`). `UsePropertyAccessMode` en `AppDbContext`; antes de seguir, test de ida y vuelta con `Include` (guardar, nuevo contexto, leer).

## Decisiones vigentes
- Búfer: copia al abrir y vuelca al modelo solo al Guardar; Cancelar descarta.
- `EventoViewModel.Original` enlaza cada copia con su entidad; los eventos nuevos quedan con `Original` a `null` hasta Guardar.
- Sincronía del historial: `SolicitudEdicionViewModel.AplicarEventos` crea, actualiza y quita del modelo; `MainViewModel.Guardar` marca los de `EventosEliminados` con `db.Eventos.Remove` (el búfer no conoce la base).
- `Evento` se crea con `Evento(fecha, tipo, descripcion)` y muta por `Actualizar`; EF usa el constructor privado. `EventoViewModel.AplicarA` delega en `Actualizar`.
- `Solicitud` guarda el historial en el campo privado `eventos`: se añade y se quita por `AgregarEvento`/`QuitarEvento`, no con `Eventos.Add`.
- Coexistencia: `MainViewModel` mantiene `Edicion` (privada) y `Editor` mientras migra pestaña a pestaña; cada grupo de `AplicarA` se activa con su XAML.
- Meta final: la UI solo conoce `Editor`; el modelo expone constructor, métodos por intención y `set` privados.
- POCO sin `INotifyPropertyChanged` filtra `PropertyDescriptor`; el editor lo evita.

## Reglas de cada commit
- `dotnet test` en verde (avisos son errores).
- `dotnet format GestorSolicitudes.slnx` y `--verify-no-changes`.
- Antes de commitear: verificación local de `sonarcloud-gate` (test, format, barrido). El veredicto del gate llega tras el push, en el PR.
- XAML nuevo: `<Page>` en el csproj; texto nuevo: clave en es/en/de.
- Mensaje en español; un commit, una idea revertible.
- Cada paso del plan lleva su propio commit, con el mensaje que indica el plan. No fusiones pasos ni los des por cubiertos por otro sin preguntarme.
- No hacer push salvo que se pida.

## Puntos de control pendientes
- 2.5: escribir antes los tests de AplicarEventos y comprobar que fallan con el código viejo: (a) quitar A y añadir N, (b) insertar N antes de A, (c) borrado real en base de datos con contexto real. Regla de diseño: todo evento con Id == 0 se crea siempre como nuevo; nunca se empareja por posición con eventos existentes.
- 2.8: EF puede no materializar la colección por campo; test de ida y vuelta con `Include` (guardar, nuevo contexto, leer).
- 3.14–3.17: el mismo test prueba los `set` privados.
- 3.7: decidir `CambiarEstado` (motivo solo con rechazo, `FechaCierre` al cerrar, reapertura) y anotarlo en `02-DOCS/decisiones/`.
- `TreatWarningsAsErrors` rompe commits intermedios: no dejar miembros sin usar.
