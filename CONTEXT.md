# Estado del proyecto

Plan: `02-DOCS/plan-refactor-edicion.md`.

## Etapas
- ✅ 0: red de seguridad.
- ✅ 1: editor con búfer; la UI no ve la entidad.
- ✅ 2: historial de eventos (2.1–2.10).
- ✅ 3: modelo cerrado con constructor, métodos de negocio y `set` privados (3.1–3.17).
- ✅ R: estados y reapertura (R.1–R.22); detalle en `02-DOCS/decisiones/estados-y-reapertura.md`.
- ✅ 5: correcciones tras la revisión final - validación CSV, fecha solicitud, cambios sin guardar y respuesta empresa.

## Siguiente paso concreto
Proyecto completo en rama `andres/Plan-Refactor-5`. Listo para abrir PR y evaluar gate de SonarCloud.

## Decisiones vigentes
- Búfer: copia al abrir, vuelca al Guardar; Cancelar descarta. UI no bindea entidad.
- Setters privados en modelo: `Solicitud` y `Evento` solo por métodos de negocio y EF.
- Decisiones de negocio (estados, reapertura, cierres) definidas en `02-DOCS/decisiones/estados-y-reapertura.md`.
- CSV: separador configurable (`;`, `,`, tab); `Escapar` antepone `'` a `= + - @`; fechas/números sin tocar.
- Respuesta empresa: cuentan estados abiertos (excluyendo "Enviada" y "Sin respuesta") más "Descartado por empresa", "Oferta aceptada", "Oferta rechazada por mí". "Retirada por mí" no cuenta.
- Cambios sin guardar: aviso Guardar/Descartar/Cancelar; tras Guardar solo si el guardado fue bien.
- Diálogo por `IDialogoService`: ViewModel never llama `MessageBox` ni `OpenFileDialog`.
- Setters privados protegidos por tests de reflexión y tests de ida y vuelta con SQLite.

## Reglas de cada commit
- `dotnet test` en verde (avisos son errores) y `dotnet format --verify-no-changes`.
- Antes de commitear: `sonarcloud-gate` local; el gate llega tras el push.
- XAML nuevo: `<Page>` en el csproj; todo texto visible con clave es/en/de.
- Mensaje en español; un commit, una idea reversible. No hacer push salvo que se pida.
- Cada paso del plan, su commit con el mensaje del plan; no fusiones pasos.
- Al terminar cada paso, actualiza este fichero: marca el paso como hecho, deja el siguiente en «Siguiente paso concreto» y anota decisiones nuevas; quita lo que ya no aplique.
