# Estado del proyecto

Plan: `02-DOCS/plan-refactor-edicion.md` (leer la sección en curso).

## Etapas
- ✅ 0: red de seguridad.
- ✅ 1: editor con búfer; la UI no ve la entidad.
- ✅ 2: historial de eventos (2.1–2.10).
- ✅ 3: modelo cerrado con constructor, métodos de negocio y `set` privados (3.1–3.17).
- ✅ R: estados y reapertura (R.1–R.22 completos; correcciones R.12a–R.12g tras la R.12); detalle en `02-DOCS/decisiones/estados-y-reapertura.md`.
- 🔄 4: `Evento` sin setters públicos (4.1), test por reflexión (4.2), prefijo compartido del hito de reapertura y documentación (4.3–4.8).

## Siguiente paso concreto
4.3 (`refactor: prefijo del hito de reapertura compartido`); el resto es documentación. El detalle está en el plan.

## Decisiones vigentes
- Búfer: copia al abrir, vuelca al Guardar; Cancelar descarta. La UI no bindea la entidad.
- `AplicarEventos` empareja por `EventoViewModel.Original`, nunca por posición ni `Id`: `Original` nulo es nuevo.
- `MainViewModel.Guardar` borra los `EventosEliminados`; el búfer no conoce la base.
- EF accede por campo a `Evento` e `Id` de `Solicitud`; `Eventos` es `IReadOnlyList` sobre campo privado.
- Un proceso cerrado solo vuelve a abrirse con `Reabrir`; el combo de estado no lo permite. `CambiarEstado` exige motivo válido en los tres cierres negativos y lo limpia fuera de ellos; al cerrar sin fecha pone hoy y, al pasar a Descartado, añade un hito `Rechazo` solo en la transición para no duplicarlo al reguardar.
- Estados: no se añade ninguno (ni `EnPausa` ni negociación); "Reabierta" es una acción, no un estado. El enum y la propiedad siguen llamándose `MotivoRechazo`, aunque el desplegable se filtre por estado y preseleccione `NoIndicado`.
- Reapertura: `Reabrir` solo va a un estado abierto distinto de "Sin respuesta"; solo se guarda la última `FechaReapertura` y `DiasProceso` cuenta desde ella, con insignia ↻ y borde magenta en la fila. El versionado del CV anterior queda fuera de alcance.
- Longitudes: una constante por campo para `[MaxLength]` y la validación. `RespuestaEmpresa` y `Notas` son texto libre sin límite (decidido; se anota en el README).
- El editor valida longitudes, rango salarial, motivo de cierre y nota de reapertura antes de `AplicarA`; el modelo lanzaría `ArgumentException`.
- La nota de la reapertura es **obligatoria** y vive en `NotaReapertura` (300), no en `Notas`: solo se guarda en el texto del hito.
- Al reabrir se respeta el `ProximoSeguimiento` editado; si no lo edita o lo deja vacío, se propone +7 días. Si el guardado falla tras `Reabrir`, el editor se recarga desde la entidad para no repetir la reapertura ni pisar datos.
- "Oferta rechazada por mí" → "Oferta aceptada" está prohibida: se reabre a "Oferta recibida" y luego se acepta.
- Salir de "Sin respuesta" rellena `FechaPrimeraRespuesta` y añade un hito `Respuesta`; `AplicarA` aplica la respuesta **antes** que el estado para que el editor no la pise.
- Desde cualquier estado abierto se puede pasar a Oferta aceptada u Oferta rechazada por mí: no se exige pasar por «Oferta recibida».
- `Evento` pasa a no tener setters públicos en 4.1 (`Id`, `SolicitudId` y `Solicitud` los escribe EF por campo); el test de 4.2 no admite excepciones.
- Los hitos que crea el modelo (`Reapertura`, `Respuesta`, `Rechazo`) guardan su texto en castellano; solo se traduce el tipo. El prefijo `Reabierta. ` es una constante compartida con la franja de Seguimiento (4.3). Guardar valores únicos y traducirlos al mostrar queda para otra refactorización.

## Reglas de cada commit
- `dotnet test` en verde (avisos son errores) y `dotnet format --verify-no-changes`.
- Antes de commitear: `sonarcloud-gate` local; el gate llega tras el push.
- XAML nuevo: `<Page>` en el csproj; texto, clave es/en/de.
- Mensaje en español; un commit, una idea reversible.
- Cada paso del plan lleva su propio commit, con el mensaje que indica el plan. No fusiones pasos ni los des por cubiertos por otro sin preguntarme.
- No hacer push salvo que se pida.
