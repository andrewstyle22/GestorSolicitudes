# Estado del proyecto

Plan completo: 02-DOCS/plan-refactor-edicion.md (leer solo la sección en curso).

## Etapas
- ✅ 0: red de seguridad (`SolicitudBuilder`, `RellenarObligatorios`).
- ✅ 1: editor con búfer; la UI ya no ve la entidad.
- ✅ 2: historial de eventos (2.1–2.10).
- 🔄 3 (opcional): cerrar el modelo con constructor, métodos de negocio y `set` privados (3.1–3.17; la 3.7 se reduce y la 3.8 se divide en 3.8a y 3.8b).
- ⬜ R: estados y reapertura (R.1–R.15), entre la 3 y la 4; detalle en `02-DOCS/decisiones/estados-y-reapertura.md`.
- ⬜ 4: test por reflexión, limpiar `ReglasSolicitud` y documentación (4.1–4.6).

## Siguiente paso concreto
Commit 3.8a (`feat: Solicitud.ActualizarFechas`): `FechaSolicitud`, entrevista y seguimiento; absorbe `ProponerSeguimiento`.

## Decisiones vigentes
- Búfer: copia al abrir, vuelca al Guardar; Cancelar descarta.
- El patrón de `Evento` (constructor público, privado para EF, métodos por intención) se replica en `Solicitud` durante la Etapa 3; `Solicitud.RegistrarEnvio` ya existe como primer método de negocio del modelo.
- `AplicarEventos` empareja por `EventoViewModel.Original`, nunca por posición ni por `Id`: un evento con `Original` nulo es siempre nuevo.
- `MainViewModel.Guardar` borra con `db.Eventos.Remove` los `EventosEliminados`; el búfer no conoce la base.
- `Evento` se muta por `Actualizar`; EF accede a sus propiedades por campo (`UsePropertyAccessMode(Field)` en `AppDbContext`).
- `Solicitud.Eventos` es `IReadOnlyList` sobre campo privado (solo `AgregarEvento`/`QuitarEvento`); ida y vuelta con `Include` probada en `Guardar_SolicitudConEventos_SeRecuperaCompleta`.
- Las listas enlazadas a entidades POCO (`MainViewModel.Solicitudes` en `MainWindow`) filtran `PropertyDescriptor` al no implementar `INotifyPropertyChanged`; en la edición lo evita el editor.

- `CambiarEstado` (3.7) no valida transiciones ni motivo. Las reglas, `Reabrir` y el estado `EnPausa` van en la Etapa R (R.3 y R.4), cuando termine la Etapa 3.
- `FechaSolicitud` la escribe `ActualizarFechas` (3.8a); `FechaPrimeraRespuesta` y `RespuestaEmpresa`, `RegistrarRespuesta` (3.8b).

## Reglas de cada commit
- `dotnet test` en verde (avisos son errores) y `dotnet format --verify-no-changes`.
- Antes de commitear: `sonarcloud-gate` local; el gate llega tras el push.
- XAML nuevo: `<Page>` en el csproj; texto, clave es/en/de.
- Mensaje en español; un commit, una idea reversible.
- Cada paso del plan lleva su propio commit, con el mensaje que indica el plan. No fusiones pasos ni los des por cubiertos por otro sin preguntarme.
- No hacer push salvo que se pida.

## Puntos de control pendientes
- 3.14–3.17: ampliar el test de ida y vuelta (`AppDbContextTests.ActualizarEvento_SeGuardaYRecuperaCompleto`) para probar los `set` privados.
- Antes de R.3: resolver las decisiones pendientes de `02-DOCS/decisiones/estados-y-reapertura.md` (¿entra `EnPausa`?, ¿motivo obligatorio?, ¿`DiasProceso` cuenta el tiempo cerrada?, ¿se conserva el CV anterior?, ¿insignia por consulta o columna?).
- `TreatWarningsAsErrors`: no dejar miembros sin usar entre commits.
