# Estado del proyecto

Plan completo: 02-DOCS/plan-refactor-edicion.md (leer solo la sección en curso).

## Etapas
- ✅ 0: red de seguridad (`SolicitudBuilder`, `RellenarObligatorios`).
- ✅ 1: editor con búfer; la UI ya no ve la entidad.
- ✅ 2: historial de eventos (2.1–2.10).
- 🔄 3 (opcional): cerrar el modelo con constructor, métodos de negocio y `set` privados (3.1–3.17; la 3.7 se reduce, la 3.8 se divide en 3.8a y 3.8b, y se añaden 3.10b, 3.10c y 3.13b). Hechas: 3.1 a 3.8a.
- ⬜ R: estados y reapertura (R.1–R.15), entre la 3 y la 4; no empezar hasta cerrar la 3 (3.17); detalle en `02-DOCS/decisiones/estados-y-reapertura.md`.
- ⬜ 4: test por reflexión y documentación (4.1–4.6; la limpieza de `ReglasSolicitud` ya se hizo en 3.8a).

## Siguiente paso concreto
Commit 3.8b (feat: Solicitud.RegistrarRespuesta): `FechaPrimeraRespuesta` y `RespuestaEmpresa`, ambos opcionales e independientes. Después, 3.9.

## Decisiones vigentes
- Búfer: copia al abrir, vuelca al Guardar; Cancelar descarta.
- El patrón de `Evento` (constructor público, privado para EF, métodos por intención) se replica en `Solicitud` durante la Etapa 3; `Solicitud.RegistrarEnvio` ya existe como primer método de negocio del modelo.
- `AplicarEventos` empareja por `EventoViewModel.Original`, nunca por posición ni por `Id`: un evento con `Original` nulo es siempre nuevo.
- `MainViewModel.Guardar` borra con `db.Eventos.Remove` los `EventosEliminados`; el búfer no conoce la base.
- `Evento` se muta por `Actualizar`; EF accede a sus propiedades por campo (`UsePropertyAccessMode(Field)` en `AppDbContext`).
- `Solicitud.Eventos` es `IReadOnlyList` sobre campo privado (solo `AgregarEvento`/`QuitarEvento`); ida y vuelta con `Include` probada en `Guardar_SolicitudConEventos_SeRecuperaCompleta`.
- Las listas enlazadas a entidades POCO (`MainViewModel.Solicitudes` en `MainWindow`) filtran `PropertyDescriptor` al no implementar `INotifyPropertyChanged`; en la edición lo evita el editor.
- `CambiarEstado` (3.7) no valida transiciones ni motivo. Las reglas, `Reabrir` y el estado `EnPausa` van en la Etapa R (R.3 y R.4), cuando termine la Etapa 3. El test `CambiarEstado_SinMotivoNiFecha_LosDejaNulos` documenta el paso cerrado → abierto y se reescribe en R.4.
- `FechaSolicitud` la escribe `ActualizarFechas` (3.8a); `FechaPrimeraRespuesta` y `RespuestaEmpresa`, `RegistrarRespuesta` (3.8b), con ambos valores opcionales e independientes.
- `ProponerSeguimiento` es método de instancia de `Solicitud` (3.8a) y asigna él mismo la fecha; `PrepararNueva` lo llama. `ReglasSolicitud` desaparece (era su única regla). `Duplicar` y `PrepararNueva` dejan de asignar campos sueltos en 3.10b.
- Longitudes máximas: una constante `LongitudMaximaX` por campo en el modelo, compartida por `[MaxLength]` y la validación de los métodos; `public const` si el editor las reutiliza. `RespuestaEmpresa` y `Notas` no tienen límite hoy: no añadir validación en 3.8b ni 3.9 sin decidirlo.

## Reglas de cada commit
- `dotnet test` en verde (avisos son errores) y `dotnet format --verify-no-changes`.
- Antes de commitear: `sonarcloud-gate` local; el gate llega tras el push.
- XAML nuevo: `<Page>` en el csproj; texto, clave es/en/de.
- Mensaje en español; un commit, una idea reversible.
- Cada paso del plan lleva su propio commit, con el mensaje que indica el plan. No fusiones pasos ni los des por cubiertos por otro sin preguntarme.
- No hacer push salvo que se pida.

## Puntos de control pendientes
- 3.11: comprobar que el editor valida las longitudes (o que los `TextBox` tienen `MaxLength`) antes de llamar a los métodos, porque estos lanzan `ArgumentException`.
- Antes de 3.14: auditoría con `rg` de escrituras directas (producción, tests, builder, `Duplicar`, `PrepararNueva`), incluidas las de reflexión y `SetValues`. Comando en el plan.
- 3.10c: test de que el editor no modifica la entidad hasta Guardar. 3.13b: test de fallo de `SaveChanges` con interceptor de EF (si destapa un fallo, se corrige antes en un commit `fix:` propio).
- 3.17: el test de ida y vuelta debe comprobar que la base asigna el `Id`.
- Decidir si RespuestaEmpresa y Notas siguen siendo texto libre (hoy no tienen [MaxLength]).
- 3.14–3.17: ampliar el test de ida y vuelta (`AppDbContextTests.ActualizarEvento_SeGuardaYRecuperaCompleto`) para probar los `set` privados.
- Antes de R.3: resolver las decisiones pendientes de `02-DOCS/decisiones/estados-y-reapertura.md` (¿entra `EnPausa`?, ¿motivo obligatorio?, ¿`DiasProceso` cuenta el tiempo cerrada?, ¿se conserva el CV anterior?, ¿insignia por consulta o columna?).
- `TreatWarningsAsErrors`: no dejar miembros sin usar entre commits.
