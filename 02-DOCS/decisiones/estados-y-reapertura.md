# Estados, transiciones y reapertura de candidaturas

Qué debe pasar cuando una candidatura cerrada vuelve a tener actividad: la empresa llama, pide el CV actualizado o vuelve a publicar el puesto. Este documento define los estados, las transiciones permitidas, la acción **Reabrir** y lo que el usuario ve en pantalla.

Ubicación sugerida: `02-DOCS/decisiones/estados-y-reapertura.md`.

Las decisiones que aún necesitan tu confirmación están marcadas con ☐.

---

## 1. Punto de partida (verificado en el código)

`EstadoSolicitud` tiene 12 valores (0 a 11). `EstadoSolicitudExtensiones.Cerrados` considera **cerrados solo cuatro**:

| Grupo | Estados |
|-------|---------|
| **Abiertos** | Enviada, En revisión, Prueba técnica, Entrevista RR. HH., Entrevista técnica, Entrevista final, Oferta recibida, **Sin respuesta** |
| **Cerrados** | Oferta aceptada, Oferta rechazada por mí, Descartado por la empresa, Retirada por mí |

Dos consecuencias importantes:

- **"Sin respuesta" cuenta como abierta.** `EstaAbierta` es verdadera, `DiasProceso` sigue contando hasta hoy y se le propone seguimiento. Es coherente: puede contestar mañana. Se queda así.
- **`MotivoRechazo` ya incluye "Puesto cancelado o pausado".** El caso de la vacante cancelada ya tiene dónde registrarse (ver §2).

`FechaCierre` es hoy un campo editable a mano; ninguna regla la rellena. `TipoEvento` ya tiene `Rechazo` y `Nota`, pero no `Reapertura`.

---

## 2. Estados nuevos

El criterio: un estado dice **dónde está el proceso ahora**. Lo que pasó, o por qué, va al historial o al motivo.

| Propuesta | Decisión recomendada | Razón |
|-----------|----------------------|-------|
| **En pausa** (abierto) | ✅ Añadir | "Me avisan en un mes" hoy no tiene reflejo. Es un estado real del proceso. |
| **Proceso cancelado** (cerrado) | ❌ No añadir | Ya existe `MotivoRechazo.PuestoCanceladoOPausado`. Si preocupa la tasa de rechazo inflada, las estadísticas pueden excluir ese motivo. |
| **Oferta en negociación** (abierto) | ☐ Opcional | Solo si negocias con frecuencia; si no, "Oferta recibida" basta. |
| **Pendiente de enviar** (borrador) | ❌ Más adelante | Obliga a que `FechaSolicitud` sea opcional y distorsiona los días desde el envío. |
| **Reabierta** | ❌ Nunca | Es una acción que deja un hito, no un estado (ver §4). |

☐ Decidir si "En pausa" entra en esta iteración.

Si entra "En pausa":
- Valor `12` al final del enum (el valor se guarda como entero; no reordenar los existentes).
- Es **abierto**. Al pasar a este estado se pide una fecha de seguimiento, y la regla de seguimiento (`ProponerSeguimiento`) la proporciona si no hay una.
- Además, renombrar el motivo a "Puesto cancelado" para que no solape con el estado nuevo (cambio aparte y opcional).

---

## 3. Tabla de transiciones

Leyenda: ✅ libre · 🔁 solo con **Reabrir** · ⚠️ con reglas · ⛔ no permitido.

| Desde ↓ / Hacia → | Abierto (cualquiera) | Descartado | Retirada | Oferta rechazada por mí | Oferta aceptada |
|---|---|---|---|---|---|
| **Abierto** | ✅ (avanzar o corregir) | ⚠️ A | ⚠️ B | ⚠️ B (solo desde Oferta recibida) | ⚠️ B (solo desde Oferta recibida) |
| **Descartado** | 🔁 | ✅ (cambiar motivo) | ⚠️ C | ⚠️ C | ⛔ |
| **Retirada** | 🔁 | ⚠️ C | ✅ | ⚠️ C | ⛔ |
| **Oferta rechazada por mí** | 🔁 | ⚠️ C | ⚠️ C | ✅ | 🔁 (confirmación reforzada) |
| **Oferta aceptada** | 🔁 (confirmación reforzada) | ⛔ | ⛔ | ⛔ | ✅ |

Reglas:

- **A. Hacia Descartado:** pide `MotivoRechazo`. ☐ Decidir si es obligatorio (recomendado: sí; "Otro motivo" es la salida). Añade el hito `Rechazo`.
- **B. Hacia otro cerrado:** rellena `FechaCierre` con hoy si está vacía.
- **C. Cerrado → cerrado:** es una corrección, no una reapertura. El estado cambia y se actualizan `FechaCierre` y el motivo. No hace falta confirmar.
- **"Sin respuesta" → otro estado** no es reapertura (ya estaba abierta). Si no hay `FechaPrimeraRespuesta`, se rellena con hoy y se añade un hito `Respuesta`.
- **Cerrado → Abierto nunca va por el combo de estado.** Si el usuario lo intenta, la app lo redirige a la acción Reabrir.
- **Oferta aceptada → cualquier otro:** confirmación reforzada ("Habías aceptado esta oferta. ¿Seguro?"), porque es el caso más raro.

---

## 4. La acción Reabrir

Es un método explícito del modelo (`Solicitud.Reabrir(nuevoEstado, nota)`), separado de `CambiarEstado`. Así `CambiarEstado` puede seguir rechazando saltos incoherentes.

**Precondiciones**
- La candidatura está cerrada.
- `nuevoEstado` es un estado abierto distinto de "Sin respuesta" (se reabre a una fase real: "En revisión", "Entrevista técnica", etc.).

**Efectos**
1. `Estado = nuevoEstado`.
2. `FechaCierre = null` y `MotivoRechazo = null`.
3. `FechaSolicitud` **no cambia**: es la fecha original del proceso.
4. Si `FechaPrimeraRespuesta` está vacía, se rellena con hoy (es la primera vez que la empresa vuelve a contactar).
5. `ProximoSeguimiento` se pone a `null` y la regla existente propone uno nuevo (+7 días).
6. Se añade un hito `TipoEvento.Reapertura` con texto del estilo *"Reabierta. Antes: Descartado por la empresa (10/10, perfil no adecuado)"* más la nota opcional del usuario. **Este hito es la única copia del cierre anterior**, así que es obligatorio.

**Consecuencias en métricas**
- Una candidatura reabierta deja de contar como rechazada: manda el estado actual. El rechazo histórico solo vive en el hito.
- `DiasProceso` seguirá contando desde `FechaSolicitud`, de modo que **incluye el tiempo que estuvo cerrada**. ☐ Decidir si se acepta o si se descuenta.

**Adjuntos.** Hoy `DescartarSustituidas` borra el CV antiguo al sustituirlo. Si te piden el CV actualizado tras una reapertura, no debería perderse el que usaste al presentarte. ☐ Decidir si se conserva el anterior (por ejemplo asociándolo al hito) o se acepta la pérdida.

---

## 5. Misma candidatura o candidatura nueva

| Situación | Acción |
|-----------|--------|
| La empresa te llama o te pide el CV (el mismo proceso continúa) | **Reabrir** la misma candidatura |
| Vuelven a publicar el puesto más adelante (proceso nuevo) | **Candidatura nueva**, creada con *Duplicar* y enlazada a la anterior (`SolicitudAnteriorId`) |

Al guardar una candidatura nueva con la misma empresa y puesto, la app avisa sin bloquear: *"Ya te presentaste el 10/10 a este puesto: Descartado por la empresa, motivo X"*. Se apoya en la búsqueda por empresa que ya existe. Esta parte requiere una columna nueva y es independiente de la reapertura, así que va en una fase aparte.

---

## 6. Qué ve el usuario

| # | Elemento | Cuándo aparece | Prioridad |
|---|----------|----------------|-----------|
| 1 | **Confirmación al reabrir** con el estado, la fecha y el motivo del cierre, el nuevo estado y una nota opcional | Al pasar de cerrado a abierto | Primera versión |
| 2 | **Hito "Reabierta"** en la línea temporal del historial | Siempre que se reabre | Primera versión |
| 3 | **Insignia "↻"** junto al estado en la tabla | Si la candidatura tiene algún hito de reapertura | Primera versión |
| 4 | **Franja informativa** en la pestaña Seguimiento (*"↻ Reabierta el 03/11. Antes: Descartado el 10/10 · Motivo: perfil no adecuado"*) | Si hay reapertura | Después |
| 5 | **Filtro "Reabiertas"** | Siempre disponible | Opcional |
| 6 | **Contador "Reabierta (2)"** | Si se reabre más de una vez | Opcional |

Notas técnicas:

- **La insignia necesita una consulta.** Las filas de la tabla no traen el historial (`Eventos` se carga solo al abrir el detalle en `Seleccionar`). Opciones: una consulta que devuelva los ids con hito de reapertura al recargar la lista (sin tocar el esquema), o guardar la fecha de la última reapertura en una columna. Recomendada: la consulta.
- **La franja sí tiene los datos**, porque se pinta con la candidatura ya abierta y su historial cargado.
- Todo texto nuevo necesita clave en es/en/de en `Traducciones.cs` (el test de paridad lo exige).
- Los cambios de estado de una candidatura **no** modifican el combo: sigue mostrando los estados reales.

---

## 7. Plan de commits

Depende del refactor del editor: el hito se añade a través de `AgregarEvento` (commit 2.7) y la confirmación vive en el editor. Se implementa tras la Etapa 2 del plan, y la lógica de `Reabrir` y `CambiarEstado` se integra con el commit 3.7.

Mismas reglas de siempre antes de cada commit: `dotnet test`, `dotnet format`, `sonarcloud-gate`, avisos como errores.

### Bloque R-A. Modelo
| # | Commit | Tam. |
|---|--------|------|
| R.1 | `feat: TipoEvento.Reapertura` (valor 9, traducciones es/en/de, test de paridad) | S |
| R.2 | `feat: EstadoSolicitud.EnPausa` (valor 12, grupo abierto, colores, traducciones, revisar embudo y estadísticas). Solo si se decide en §2 | M |
| R.3 | `feat: Solicitud.Reabrir(nuevoEstado, nota)` con tests de efectos y de precondiciones | M |
| R.4 | `feat: CambiarEstado aplica la tabla de transiciones` (reglas A, B, C; cerrado→abierto se rechaza) | M |

### Bloque R-B. Interfaz
| # | Commit | Tam. |
|---|--------|------|
| R.5 | `feat: el editor detecta cerrado → abierto` (expone que hay una reapertura pendiente) | S |
| R.6 | `feat: confirmación al reabrir` (diálogo con los datos del cierre y nota opcional; Guardar llama a `Reabrir`) | M |
| R.7 | `feat: consulta de candidaturas reabiertas` (ids con hito de reapertura, tests) | S |
| R.8 | `feat: insignia ↻ en la tabla` (propiedad en la fila + XAML + traducciones) | M |
| R.9 | `feat: franja informativa en Seguimiento` | M |

### Bloque R-C. Opcionales
| # | Commit | Tam. |
|---|--------|------|
| R.10 | `feat: filtro Reabiertas` | M |
| R.11 | `feat: contador de reaperturas` | S |
| R.12 | `feat: SolicitudAnteriorId` (columna nullable, `EsquemaDb`, prueba de migración) | M |
| R.13 | `feat: Duplicar enlaza con la candidatura anterior` | S |
| R.14 | `feat: aviso al guardar una candidatura repetida` | M |

### Cierre
| # | Commit | Tam. |
|---|--------|------|
| R.15 | `docs: registrar la decisión en 02-DOCS/decisiones` y actualizar `AGENTS.md` (nueva transición de estados) | S |

---

## 8. Decisiones pendientes

☐ ¿Entra "En pausa"? ¿Y "Oferta en negociación"?
☐ ¿`MotivoRechazo` obligatorio al descartar?
☐ ¿`DiasProceso` incluye el tiempo cerrada?
☐ ¿Se conserva el CV anterior al sustituirlo tras una reapertura?
☐ ¿Insignia por consulta o por columna?
