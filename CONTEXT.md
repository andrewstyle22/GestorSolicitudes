# Estado del proyecto

Plan completo: 02-DOCS/plan-refactor-edicion.md (leer solo la sección de la etapa en curso).

## Etapas
- ✅ Etapa 0: red de seguridad; tests con `SolicitudBuilder` y `RellenarObligatorios`.
- ✅ Etapa 1: editor con búfer conectado al `MainViewModel`; la UI ya no ve la entidad.
- 🔄 Etapa 2 (en curso): historial de eventos en el editor y encapsulado en el modelo (2.1–2.10).
- ⬜ Etapa 3 (opcional): cerrar el modelo con constructor, métodos de negocio y `set` privados (3.1–3.17).
- ⬜ Etapa 4: cierre; test por reflexión, limpiar `ReglasSolicitud` y documentación (4.1–4.6).

## Siguiente paso concreto
Commit 2.1 (`feat: EventoViewModel`). Crear `EventoViewModel` en `ViewModels` con `Fecha`, `Tipo` y `Descripcion` con `INotifyPropertyChanged`, más `CargarDesde` y `AplicarA`, con tests.

## Decisiones vigentes
- Búfer: copia al abrir y vuelca al modelo solo al Guardar; Cancelar descarta.
- Coexistencia: `MainViewModel` mantiene `Edicion` (privada) y `Editor` mientras migra pestaña a pestaña; cada grupo de `AplicarA` se activa con su XAML.
- Meta final: la UI solo conoce `Editor`; el modelo expone constructor, métodos por intención y `set` privados.
- POCO sin `INotifyPropertyChanged` filtra `PropertyDescriptor`; el editor lo evita.

## Reglas de cada commit
- `dotnet test` en verde (avisos son errores).
- `dotnet format GestorSolicitudes.slnx` y `--verify-no-changes`.
- Pasar `sonarcloud-gate`.
- XAML nuevo: `<Page>` en el csproj; texto nuevo: clave en es/en/de.
- Mensaje en español; un commit, una idea revertible.

## Puntos de control pendientes
- 2.8: EF puede no materializar la colección por campo; test de ida y vuelta con `Include` (guardar, nuevo contexto, leer).
- 3.14–3.17: el mismo test prueba los `set` privados.
- 3.7: decidir `CambiarEstado` (motivo solo con rechazo, `FechaCierre` al cerrar, reapertura) y anotarlo en `02-DOCS/decisiones/`.
- `TreatWarningsAsErrors` rompe commits intermedios: no dejar miembros sin usar.
