# Estados, transiciones y reapertura de candidaturas

Qué debe pasar cuando una candidatura se cierra y qué debe pasar cuando una cerrada vuelve a tener actividad (la empresa llama, pide el CV actualizado o vuelve a publicar el puesto). Define los estados, el motivo de cierre, las transiciones permitidas, la acción **Reabrir** y lo que el usuario ve en pantalla.

Ubicación: `02-DOCS/decisiones/estados-y-reapertura.md`. Todas las decisiones que estaban abiertas se han tomado (sección 2).

---

## 1. Punto de partida (verificado en el código)

- `EstadoSolicitud` tiene 12 valores (0 a 11). `EstadoSolicitudExtensiones.Cerrados` considera cerrados **solo cuatro**: Oferta aceptada (7), Oferta rechazada por mí (8), Descartado por la empresa (9) y Retirada por mí (10). "Sin respuesta" (11) cuenta como abierta: puede contestar mañana.
- `MotivoRechazo` es hoy un enum de 6 valores (0 a 5) pensado para rechazos de la empresa. Solo se muestra con el estado "Descartado". Es `MotivoRechazo?` en el modelo.
- `DiasProceso` solo se usa en la columna "Días" de la tabla. Las estadísticas usan `DiasHastaRespuesta`.
- El embudo cuenta únicamente los hitos `Entrevista`, `Oferta` y `SolicitudEnviada`; un hito `Reapertura` no lo altera.
- `Recargar` lee la tabla de `Solicitudes` una sola vez, sin `Include` de `Eventos`. Las filas de la tabla son la entidad.
- `EsquemaDb.VerificarColumnasFaltantes` añade las columnas nuevas de forma idempotente al arrancar.
- Los enums se guardan como entero y sus textos viven en `Traducciones.cs` con claves `Tipo.Valor` (es/en/de).
- `CambiarEstado(estado, motivo, fechaCierre)` solo asigna (commit 3.7). Las reglas llegan en esta etapa.

---

## 2. Decisiones tomadas

| # | Decisión | Resultado |
|---|----------|-----------|
| 1 | ¿"En pausa"? | **No** hasta que haya un caso real. Añadirlo después cuesta lo mismo. |
| 2 | ¿"Oferta en negociación"? | **No**. "Oferta recibida" más las notas lo cubre. |
| 3 | ¿Motivo obligatorio? | **Sí**, en los tres cierres negativos (Descartado, Retirada, Oferta rechazada por mí), con el valor `NoIndicado`. Los valores nuevos son propios del usuario y el desplegable se filtra por estado. Al cambiar a esos estados se preselecciona `NoIndicado`. |
| 4 | ¿`DiasProceso` tras reabrir? | **Se reinicia al reabrir**; el total desde el envío va en un tooltip. La candidatura queda identificada con la columna `FechaReapertura`, insignia "↻" y color de fila. Solo se guarda la última reapertura, sin contador. |
| 5 | ¿Se conserva el CV anterior? | **No ahora**. Se deja para otra refactorización (versionado de adjuntos). |
| 6 | ¿Insignia por consulta o columna? | **Columna**, a raíz de la decisión 4. La consulta de ids con hito de reapertura ya no hace falta. |
| 7 | ¿`RespuestaEmpresa` y `Notas` sin límite? | Aplazada; siguen sin límite. |

---

## 3. Estados

No se añade ningún estado. "Reabierta" no es un estado (es una acción que deja un hito y una fecha) y tampoco lo son "En pausa" ni "Oferta en negociación" por ahora. Un proceso en pausa se representa con "En revisión" o "Sin respuesta", una fecha de seguimiento y una nota.

Grupos, sin cambios:

| Grupo | Estados |
|-------|---------|
| **Abiertos** | Enviada, En revisión, Prueba técnica, Entrevista RR. HH., Entrevista técnica, Entrevista final, Oferta recibida, Sin respuesta |
| **Cerrados** | Oferta aceptada, Oferta rechazada por mí, Descartado por la empresa, Retirada por mí |

---

## 4. Motivo de cierre

### 4.1 Valores

Se amplía el mismo enum, al final, sin tocar los valores existentes (se guarda como entero, así que no cambia el esquema):

| Valor | Nombre | es | Uso |
|-------|--------|----|-----|
| 0 | `Salario` | Salario / pretensión | empresa y propio |
| 1 | `ExperienciaInsuficiente` | Experiencia insuficiente | empresa |
| 2 | `SeleccionaronOtroCandidato` | Seleccionaron a otro candidato | empresa |
| 3 | `CulturalFit` | No encajaba con el equipo/cultura | empresa |
| 4 | `PuestoCanceladoOPausado` | Puesto cancelado o pausado | empresa |
| 5 | `Otro` | Otro motivo | ambos |
| 6 | `NoIndicado` | No indicado | ambos |
| 7 | `AceptoOtraOferta` | Acepté otra oferta | propio |
| 8 | `ModalidadOUbicacion` | Modalidad o ubicación | propio |
| 9 | `YaNoMeInteresa` | Ya no me interesa | propio |

Cada valor nuevo lleva sus claves en es/en/de (`MotivoRechazo.NoIndicado`...). El nombre del enum y de la propiedad (`MotivoRechazo`) **no se cambia** en esta etapa; renombrarlo a `MotivoCierre` es un refactor aparte. La etiqueta visible `Seguimiento.MotivoRechazo` pasa a "Motivo del cierre" / "Reason for closing" / "Grund des Abschlusses".

### 4.2 Motivos permitidos por estado

La tabla vive en el modelo y la usan a la vez el desplegable y `CambiarEstado`, para que no puedan divergir.

| Estado | Motivos permitidos |
|--------|--------------------|
| Descartado por la empresa | Salario, Experiencia insuficiente, Seleccionaron a otro candidato, Cultural fit, Puesto cancelado o pausado, Otro, No indicado |
| Retirada por mí / Oferta rechazada por mí | Salario, Acepté otra oferta, Modalidad o ubicación, Ya no me interesa, Otro, No indicado |
| Cualquier otro estado (incluida Oferta aceptada) | Ninguno (`null`) |

### 4.3 Reglas

- En los tres estados con motivo, el motivo es **obligatorio** y debe pertenecer a su lista. `CambiarEstado` lanza `ArgumentException` si no.
- En cualquier otro estado, `CambiarEstado` deja el motivo en `null` (no lanza): así una fila con un motivo antiguo no impide guardar.
- **Editor:** el desplegable muestra solo los motivos del estado, sin la entrada "Sin especificar". Al pasar a uno de los tres estados con el motivo vacío o no permitido, se preselecciona `NoIndicado`; al salir a otro estado, se limpia. Así Guardar nunca queda bloqueado por este motivo y el modelo sigue exigiéndolo.
- El panel del motivo se muestra en los tres estados (hoy solo en "Descartado").

### 4.4 Datos existentes

Al arrancar, después de `VerificarColumnasFaltantes`, `EsquemaDb` normaliza las filas antiguas. Es idempotente: tras la primera vez no coincide ninguna fila, y la regla impide crear nuevas:

```sql
UPDATE Solicitudes SET MotivoRechazo = 6 WHERE Estado IN (8, 9, 10) AND MotivoRechazo IS NULL;
UPDATE Solicitudes SET MotivoRechazo = 6 WHERE Estado IN (8, 10) AND MotivoRechazo IN (1, 2, 3, 4);
```

La segunda sentencia cubre filas de "Retirada" u "Oferta rechazada" que conservaron un motivo de empresa de cuando eran "Descartado". Debe estar antes de que `CambiarEstado` exija el motivo, para que esas filas se puedan guardar.

---

## 5. Tabla de transiciones

Leyenda: ✅ libre · 🔁 solo con **Reabrir** · ⚠️ con reglas · ⛔ no permitido.

| Desde ↓ / Hacia → | Abierto (cualquiera) | Descartado | Retirada | Oferta rechazada por mí | Oferta aceptada |
|---|---|---|---|---|---|
| **Abierto** | ✅ (avanzar o corregir) | ⚠️ A | ⚠️ A | ⚠️ A (solo desde Oferta recibida) | ⚠️ B (solo desde Oferta recibida) |
| **Descartado** | 🔁 | ✅ (cambiar motivo) | ⚠️ C | ⚠️ C | ⛔ |
| **Retirada** | 🔁 | ⚠️ C | ✅ | ⚠️ C | ⛔ |
| **Oferta rechazada por mí** | 🔁 | ⚠️ C | ⚠️ C | ✅ | 🔁 (confirmación reforzada) |
| **Oferta aceptada** | 🔁 (confirmación reforzada) | ⛔ | ⛔ | ⛔ | ✅ |

Reglas:

- **A. Hacia un cierre negativo:** motivo obligatorio y permitido (sección 4) y `FechaCierre` = hoy si está vacía. Para "Descartado" se añade además el hito `Rechazo`.
- **B. Hacia Oferta aceptada:** `FechaCierre` = hoy si está vacía; sin motivo.
- **C. Cerrado → cerrado:** es una corrección, no una reapertura. Si el motivo anterior no pertenece a la lista del nuevo estado, se preselecciona `NoIndicado`. No pide confirmación.
- **"Sin respuesta" → otro estado** no es reapertura (ya estaba abierta). Si no hay `FechaPrimeraRespuesta`, se rellena con hoy y se añade un hito `Respuesta`.
- **Cerrado → abierto nunca va por el combo de estado.** Si el usuario lo intenta, la app lo redirige a la acción Reabrir.
- **Oferta aceptada → cualquier otro:** confirmación reforzada ("Habías aceptado esta oferta. ¿Seguro?").

---

## 6. La acción Reabrir

Método explícito del modelo, `Solicitud.Reabrir(nuevoEstado, nota)`, separado de `CambiarEstado` para que este pueda seguir rechazando saltos incoherentes.

**Precondiciones**
- La candidatura está cerrada.
- `nuevoEstado` es un estado abierto distinto de "Sin respuesta" (se reabre a una fase real).

**Efectos**
1. `Estado = nuevoEstado`.
2. `FechaCierre = null` y `MotivoRechazo = null`.
3. **`FechaReapertura = hoy`.** Si ya había una, se sustituye: solo se guarda la última, no hay contador. Las anteriores quedan en los hitos del historial.
4. `FechaSolicitud` **no cambia**.
5. Si `FechaPrimeraRespuesta` está vacía, se rellena con hoy.
6. `ProximoSeguimiento` se pone a `null` y la regla existente propone uno nuevo (+7 días).
7. Se añade un hito `TipoEvento.Reapertura` con texto del estilo *"Reabierta. Antes: Descartado por la empresa (10/10, perfil no adecuado)"* más la nota opcional. Es la única copia del cierre anterior.

La nota opcional del hito se toma del campo `Notas` del editor. Antes de guardar, la confirmación muestra el estado, fecha y motivo del cierre, el nuevo estado y esa nota; al reabrir una candidatura con oferta aceptada, el mensaje advierte expresamente que la oferta ya se había aceptado.

**Modelo**
- Propiedad `DateTime? FechaReapertura` con `set` privado, columna `TEXT NULL` añadida en `EsquemaDb`.
- `[NotMapped] bool FueReabierta => FechaReapertura.HasValue`.

**Días.** `DiasProceso` usa como referencia `FechaReapertura ?? FechaSolicitud`: abierta, `hoy − referencia`; cerrada, `(FechaCierre ?? hoy) − referencia`. Tras reabrir empieza en 0. `DiasDesdeSolicitud` (total desde el envío) no cambia y se enseña en el tooltip. `DiasHastaRespuesta` y las estadísticas no cambian.

**Adjuntos.** No cambia nada: si adjuntas un CV nuevo tras reabrir, se sustituye el anterior, como hoy. El versionado de adjuntos queda fuera de alcance (otra refactorización).

---

## 7. Misma candidatura o candidatura nueva

| Situación | Acción |
|-----------|--------|
| La empresa te llama o te pide el CV (el mismo proceso continúa) | **Reabrir** la misma candidatura |
| Vuelven a publicar el puesto más adelante (proceso nuevo) | **Candidatura nueva**, creada con *Duplicar* y enlazada a la anterior (`SolicitudAnteriorId`, opcional) |

Al guardar una candidatura nueva con la misma empresa y puesto, la app avisa sin bloquear: *"Ya te presentaste el 10/10 a este puesto: Descartado por la empresa, motivo X"*. Requiere una columna nueva y es independiente de la reapertura (commits opcionales).

---

## 8. Qué ve el usuario

| # | Elemento | Cuándo aparece | Prioridad |
|---|----------|----------------|-----------|
| 1 | **Confirmación al reabrir** con estado, fecha y motivo del cierre, nuevo estado y nota opcional | Al pasar de cerrado a abierto | Primera versión |
| 2 | **Hito "Reabierta"** en el historial | Siempre que se reabre | Primera versión |
| 3 | **Insignia "↻"** junto al estado en la tabla, con tooltip (fecha de la reapertura) | Si `FueReabierta` | Primera versión |
| 4 | **Color de fila:** borde izquierdo magenta (`#DB2777`) mediante un `DataTrigger` sobre `FueReabierta` en el `RowStyle`. No pisa el fondo ámbar de seguimiento pendiente. Siempre junto a la insignia, nunca solo el color | Si `FueReabierta` | Primera versión |
| 5 | **Columna "Días"** con el contador reiniciado y tooltip con el total desde el envío | Siempre | Primera versión |
| 6 | **Franja informativa** en Seguimiento: *"↻ Reabierta el 03/11. Antes: Descartado el 10/10 · Motivo: …"* (se lee del último hito `Reapertura`, ya cargado al abrir el detalle) | Si hay reapertura | Después |
| 7 | **Filtro "Reabiertas"** | Siempre disponible | Opcional |

Todos los textos nuevos necesitan clave en es/en/de en `Traducciones.cs` (el test de paridad lo exige).

---

## 9. Plan de commits

Detalle completo en `02-DOCS/plan-refactor-edicion.md` (Etapa R, R.1 a R.22). Resumen del orden y por qué:

1. **R.1 a R.4:** hito `Reapertura`, valores nuevos del motivo, tabla de motivos permitidos y relleno de datos antiguos.
2. **R.5 a R.7:** `FechaReapertura`, `DiasProceso` y `Reabrir` en el modelo.
3. **R.8 a R.10:** el desplegable filtrado y la preselección van **antes** de que `CambiarEstado` exija el motivo (R.10), y el relleno de datos antiguos (R.4) también; si no, Guardar fallaría en filas existentes.
4. **R.11 a R.13:** el editor detecta la reapertura y la confirma **antes** de que `CambiarEstado` prohíba cerrado → abierto (R.13); si no, reabrir desde la interfaz fallaría.
5. **R.14 a R.17:** insignia, color de fila, tooltip de "Días" y franja.
6. **R.18 a R.21:** opcionales. **R.22:** documentación.

---

## 10. Fuera de alcance

- Versionado de adjuntos (CV anterior tras una reapertura): otra refactorización.
- Renombrar `MotivoRechazo` a `MotivoCierre`.
- Contador de reaperturas.
- Estados "En pausa" y "Oferta en negociación": revisar si aparece un caso real.
- Límite de longitud de `RespuestaEmpresa` y `Notas`.
