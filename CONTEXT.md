# Estado del proyecto

Plan: `02-DOCS/plan-refactor-edicion.md` (leer la sección en curso).

## Etapas
- ✅ 0: red de seguridad.
- ✅ 1: editor con búfer; la UI no ve la entidad.
- ✅ 2: historial de eventos (2.1–2.10).
- ✅ 3: modelo cerrado con constructor, métodos de negocio y `set` privados (3.1–3.17).
- 🔄 R: estados y reapertura (R.1–R.22, con las correcciones R.12a–R.12g tras la R.12); detalle en `02-DOCS/decisiones/estados-y-reapertura.md`.
- ⬜ 4: test por reflexión y documentación (4.1–4.6).

## Siguiente paso concreto
R.13 (`feat: CambiarEstado valida las transiciones`). R.12g ya quedó completado. El detalle de cada una está en el plan.

## Decisiones vigentes
- Búfer: copia al abrir, vuelca al Guardar; Cancelar descarta.
- `AplicarEventos` empareja por `EventoViewModel.Original`, nunca por posición ni `Id`: `Original` nulo es nuevo.
- `MainViewModel.Guardar` borra los `EventosEliminados`; el búfer no conoce la base.
- EF accede por campo a `Evento` e `Id` de `Solicitud`; `Eventos` es `IReadOnlyList` sobre campo privado.
- `CambiarEstado` exige un motivo válido en los tres cierres negativos y lo limpia fuera de ellos; las transiciones llegan en R.13.
- Estados: no se añade ninguno (ni `EnPausa` ni negociación). Reabrir es una acción, no un estado.
- Motivo de cierre: obligatorio en Descartado, Retirada y Oferta rechazada por mí. Se amplía el enum `MotivoRechazo` con `NoIndicado`, `AceptoOtraOferta`, `ModalidadOUbicacion` y `YaNoMeInteresa`. El desplegable se filtra por estado y el editor preselecciona `NoIndicado`. El nombre `MotivoRechazo` no se cambia.
- Reapertura: columna `FechaReapertura` (solo la última, sin contador) y `FueReabierta` [NotMapped]. `DiasProceso` cuenta desde `FechaReapertura ?? FechaSolicitud`. Insignia ↻ y borde magenta `#DB2777` en la fila.
- El versionado de CV tras una reapertura queda fuera de alcance (otra refactorización).
- Longitudes: una constante por campo para `[MaxLength]` y la validación; `RespuestaEmpresa` y `Notas` sin límite hasta decidirlo.
- El editor valida longitudes, rango salarial y motivo de cierre (obligatorio y permitido) antes de `AplicarA`; el modelo lanzaría `ArgumentException`.
- La nota de la reapertura es **obligatoria**, en un campo propio del editor (`NotaReapertura`, 300 caracteres) visible en Seguimiento solo con reapertura pendiente; solo se guarda en el texto del hito. El editor y `Reabrir` la exigen (R.12f).
- Al reabrir se respeta el `ProximoSeguimiento` que edite el usuario en el mismo guardado; si no lo edita o lo deja vacío, se propone +7 días (R.12g).
- `Reabrir` no admite "Sin respuesta" como destino: el editor lo valida (R.12b). La reapertura se decide con el estado de la entidad y, si el guardado falla, el editor se recarga desde ella (R.12a).
- "Oferta rechazada por mí" → "Oferta aceptada" está prohibida: se reabre a "Oferta recibida" y luego se acepta (R.13).
- Salida de "Sin respuesta" (R.13): rellena `FechaPrimeraRespuesta` y añade un hito `Respuesta`; `AplicarA` aplica la respuesta **antes** que el estado para que el editor no la pise.

## Reglas de cada commit
- `dotnet test` en verde (avisos son errores) y `dotnet format --verify-no-changes`.
- Antes de commitear: `sonarcloud-gate` local; el gate llega tras el push.
- XAML nuevo: `<Page>` en el csproj; texto, clave es/en/de.
- Mensaje en español; un commit, una idea reversible.
- Cada paso del plan lleva su propio commit, con el mensaje que indica el plan. No fusiones pasos ni los des por cubiertos por otro sin preguntarme.
- No hacer push salvo que se pida.

## Puntos de control pendientes
- Orden en la Etapa R: R.11, R.12 y R.12a a R.12g antes de R.13. Dentro de las correcciones, el campo de la nota (R.12d y R.12e) va antes de exigirla (R.12f); si no, el usuario no podría guardar una reapertura.
- R.12a: test de fallo de `SaveChanges` seguido de reintento tras una reapertura (un solo hito, `FechaCierre` nula, seguimiento propuesto, primera respuesta rellena).
- R.12c: prueba manual del combo de motivos al cambiar de Descartado a Retirada; WPF puede vaciar `SelectedValue` al reemplazar `MotivosDelEstado`.
- R.13: test del orden de `AplicarA` (respuesta antes que estado).
- `TreatWarningsAsErrors`: no dejar miembros sin usar entre commits.
