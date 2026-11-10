# Estado del proyecto

Plan: `02-DOCS/plan-refactor-edicion.md`.

## Etapas
- ✅ 0: red de seguridad.
- ✅ 1: editor con búfer; la UI no ve la entidad.
- ✅ 2: historial de eventos (2.1–2.10).
- ✅ 3: modelo cerrado con constructor, métodos de negocio y `set` privados (3.1–3.17).
- ✅ R: estados y reapertura (R.1–R.22); detalle en `02-DOCS/decisiones/estados-y-reapertura.md`.
- ✅ 4: cierre de la refactorización: modelos sin setters públicos, documentación al día y gate de Sonar en verde.

## Siguiente paso concreto
Abrir el PR de `andres/Plan-Refactor-4`. No hacer push sin que se pida.

## Decisiones vigentes
- Búfer: copia al abrir, vuelca al Guardar; Cancelar descarta. La UI no bindea la entidad.
- `AplicarEventos` empareja por `EventoViewModel.Original` (nulo = nuevo), nunca por posición ni `Id`.
- `MainViewModel.Guardar` borra los `EventosEliminados`; el búfer no conoce la base.
- EF escribe por campo: `Eventos` es `IReadOnlyList` sobre campo privado y ningún modelo expone setters públicos.
- La nota de reapertura vive en `NotaReapertura` (300), no en `Notas`: solo se guarda en el hito.
- Si el guardado falla tras `Reabrir`, el editor se recarga desde la entidad para no repetir la reapertura.
- Los hitos del modelo guardan su texto en castellano con el prefijo `Reabierta. ` compartido; traducirlos al mostrar queda para otra refactorización.

## Reglas de cada commit
- `dotnet test` en verde (avisos son errores) y `dotnet format --verify-no-changes`.
- Antes de commitear: `sonarcloud-gate` local; el gate llega tras el push.
- XAML nuevo: `<Page>` en el csproj; todo texto visible con clave es/en/de.
- Mensaje en español; un commit, una idea reversible. No hacer push salvo que se pida.
- Cada paso del plan, su commit con el mensaje del plan; no fusiones pasos.
