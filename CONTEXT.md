# Estado del proyecto

Plan: `02-DOCS/plan-refactor-edicion.md` (leer la sección en curso).

## Etapas
- ✅ 0: red de seguridad.
- ✅ 1: editor con búfer; la UI no ve la entidad.
- ✅ 2: historial de eventos (2.1–2.10).
- ✅ 3: modelo cerrado con constructor, métodos de negocio y `set` privados (3.1–3.17).
- 🔄 R: estados y reapertura (R.1–R.15); detalle en `02-DOCS/decisiones/estados-y-reapertura.md`.
- ⬜ 4: test por reflexión y documentación (4.1–4.6).

## Siguiente paso concreto
R.1 (`feat: TipoEvento.Reapertura`).

## Decisiones vigentes
- Búfer: copia al abrir, vuelca al Guardar; Cancelar descarta.
- `AplicarEventos` empareja por `EventoViewModel.Original`, nunca por posición ni `Id`: `Original` nulo es nuevo.
- `MainViewModel.Guardar` borra los `EventosEliminados`; el búfer no conoce la base.
- EF accede por campo a `Evento` e `Id` de `Solicitud`; `Eventos` es `IReadOnlyList` sobre campo privado.
- `CambiarEstado` no valida transiciones ni motivo: eso llega en R.3/R.4.
- Longitudes: una constante por campo para `[MaxLength]` y la validación; `RespuestaEmpresa` y `Notas` sin límite hasta decidirlo.
- El editor valida longitudes y rango salarial antes de `AplicarA`; el modelo lanzaría `ArgumentException`.

## Reglas de cada commit
- `dotnet test` en verde (avisos son errores) y `dotnet format --verify-no-changes`.
- Antes de commitear: `sonarcloud-gate` local; el gate llega tras el push.
- XAML nuevo: `<Page>` en el csproj; texto, clave es/en/de.
- Mensaje en español; un commit, una idea reversible.
- Cada paso del plan lleva su propio commit, con el mensaje que indica el plan. No fusiones pasos ni los des por cubiertos por otro sin preguntarme.
- No hacer push salvo que se pida.

## Puntos de control pendientes
- Antes de R.2: decidir si entra `EnPausa`.
- Antes de R.3: decidir motivo obligatorio, `DiasProceso` con tiempo cerrada, CV anterior e insignia.
- `TreatWarningsAsErrors`: no dejar miembros sin usar entre commits.
