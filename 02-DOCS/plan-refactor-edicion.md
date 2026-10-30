# Plan de refactor: edición con búfer y modelo encapsulado

Objetivo: que la interfaz deje de modificar la entidad `Solicitud` directamente mediante bindings `TwoWay` implícitos, y que el modelo sea quien proteja sus reglas de negocio.

Sugerencia de ubicación en el repo: `02-DOCS/plan-refactor-edicion.md`.

---

## 1. Estado de partida (verificado en el código)

**Ya está hecho**
- `Solicitud.Eventos` es `List<Evento>` y la copia observable (`EventosEdicion`) vive en el ViewModel.
- `Adjunto` es un `record` en `ViewModels`.
- `Cancelar` ya hace `db.ChangeTracker.Clear()`, así que descartar una edición no deja cambios pendientes en EF.
- `Directory.Build.props` activa `TreatWarningsAsErrors`: cada commit debe compilar sin avisos.
- `ArquitecturaTests` ya vigila las capas (`Models` no depende de nada superior).

**Sigue pendiente**
- `Solicitud` y `Evento` tienen todos los `set` públicos.
- El panel de detalle bindea contra la entidad: 26 bindings `Edicion.X` repartidos así: `PestanaOferta` 11, `PestanaSeguimiento` 13 y `MainWindow` 2 (Empresa y Puesto).
- `PestanaAdjuntos` enlaza `EventosEdicion` (entidades `Evento` mutables).
- `RefrescarPanelDetalle` (`Edicion = null; Edicion = actual`) se usa en 3 sitios solo para forzar el repintado.
- Los adjuntos dependen de `EF OriginalValue` (`RutasGuardadas`) para saber qué había guardado: es el punto más delicado.
- Las reglas viven fuera del modelo: `ReglasSolicitud` es estático y el ViewModel asigna campos a mano (`solicitud.ProximoSeguimiento = propuesta`).
- Hay 90 `new Solicitud` en 14 ficheros de test + `MainViewModel`, y 97 referencias a `Edicion` en 5 ficheros de test (`ViewModelTests` 39, `ViewModelAdjuntosTests` 32, `ViewModelDialogosTests` 23, `ViewModelEmbudoTests` 2, `PestanasTests` 1).

---

## 2. Decisiones de diseño

1. **Editor con búfer, no un wrapper por entidad.** Un único `SolicitudEdicionViewModel` (`ViewModels`, `INotifyPropertyChanged`) copia los datos al abrir, valida mientras se escribe y vuelca todo al modelo en **un solo paso** al pulsar Guardar. Cancelar descarta el búfer. No se llama al modelo por cada tecla: escribir "35000" pasaría por estados intermedios inválidos.
2. **La tabla no se envuelve.** Las filas siguen siendo entidades de solo lectura (la paginación ya limita lo que se ve). El `TwoWay` de `SelectedItem` del `DataGrid` es legítimo y se queda.
3. **Coexistencia temporal.** Mientras se migra pestaña a pestaña, `MainViewModel` mantiene `Edicion` (la entidad) y añade `Editor`. `Editor.AplicarA(entidad)` está dividido en **grupos** (identificación, oferta, económico, seguimiento, contacto, adjuntos) y solo se ejecutan los grupos cuyo XAML ya apunta al editor; así nunca se pisan datos editados por un binding aún sin migrar.
4. **Meta final.** `Edicion` deja de ser pública; la UI solo conoce `Editor`. El modelo expone constructor con datos obligatorios, métodos por intención de negocio y `set` privados.
5. **Motivo adicional.** Los bindings a propiedades de un POCO sin `INotifyPropertyChanged` pasan por `PropertyDescriptor`, una fuente conocida de fugas de memoria en WPF. Con el editor desaparece ese caso en la edición.

---

## 3. Reglas de cada commit

Antes de cada commit, en este orden:

1. `dotnet test` en verde (recuerda: los avisos son errores).
2. `dotnet format GestorSolicitudes.slnx` y comprobar `--verify-no-changes`.
3. Pasar la skill `sonarcloud-gate` (S2930, S6444, S6966, S1117, S1192, S3267...).
4. Si hay XAML nuevo: `<Page>` declarado en el csproj. Si hay texto de UI nuevo: clave en es/en/de en `Traducciones.cs`.
5. Mensaje de commit en español. Un commit = una idea reversible con `git revert`.

Tamaño estimado: **S** (<50 líneas), **M** (50-200), **L** (>200, vigilar y dividir si crece).

---

## Etapa 0. Red de seguridad (sin cambiar comportamiento)

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| 0.1 | `test: fijar el comportamiento actual de Guardar, Cancelar y Duplicar` | Revisar los tests existentes y añadir solo lo que falte: Cancelar sobre fila existente descarta cambios, Guardar propone seguimiento, Cancelar borra copias de adjuntos no guardadas. | M |
| 0.2 | `test: añadir SolicitudBuilder` | Clase en `GestorSolicitudes.Tests` con valores por defecto y métodos `Con...`. Todavía sin usar. | S |
| 0.3a | `test: usar SolicitudBuilder en los tests de ViewModel` | Sustituye `new Solicitud {...}` en `ViewModelTests`, `ViewModelCsvTests`, `ViewModelPaginacionTests`, `ViewModelDialogosTests`, `ViewModelAdjuntosTests`, `ViewModelEmbudoTests`. Mecánico. | L |
| 0.3b | `test: usar SolicitudBuilder en los tests de modelo y datos` | `SolicitudTests`, `ReglasSolicitudTests`, `AppDbContextTests`. | M |
| 0.3c | `test: usar SolicitudBuilder en el resto de tests` | `PestanasTests`, `CsvExportadorTests`, `EstadisticasConsultaTests`, `GestorAdjuntosTests`, `EmbudoHelperTests`. | M |
| 0.4 | `test: helper RellenarObligatorios para los tests de ViewModel` | Extensión de test `vm.RellenarObligatorios("ACME", "Dev")` que sustituye al par repetido `vm.Edicion.Empresa = ...; vm.Edicion.Puesto = ...`. Más adelante solo cambia el helper, no los 39+ tests. | M |

**Hecho cuando:** ningún test crea `Solicitud` con el inicializador directo ni rellena empresa/puesto a mano.

---

## Etapa 1. Editor con búfer

### 1A. Construir el editor (sin tocar la UI)

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| 1.1 | `feat: SolicitudEdicionViewModel con grupo Identificación` | Clase con `Empresa` y `Puesto` (`[ObservableProperty]`), `CargarDesde(Solicitud)` y `AplicarIdentificacion(Solicitud)`. Tests de ida y vuelta. | S |
| 1.2 | `feat: grupo Oferta en el editor` | `EnlaceOferta`, `Portal`, `Origen`, `Ubicacion`, `Modalidad`, `Tecnologias`, `Requisitos`. | S |
| 1.3 | `feat: grupo Económico e Interés en el editor` | `SalarioMin`, `SalarioMax`, `PretensionSalarial` (`int?`), `Interes`. Tests con valores nulos. | S |
| 1.4 | `feat: grupo Seguimiento en el editor` | `Estado`, `MotivoRechazo`, `FechaSolicitud`, `FechaPrimeraRespuesta`, `FechaEntrevista`, `ProximoSeguimiento`, `FechaCierre`, `RespuestaEmpresa`. | M |
| 1.5 | `feat: grupo Contacto y Notas en el editor` | `ContactoNombre`, `ContactoEmail`, `ContactoTelefono`, `Notas`. | S |

`AplicarA(Solicitud)` llama a los métodos de grupo; los tests prueban cada grupo por separado.

### 1B. Conectar al `MainViewModel`

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| 1.6 | `refactor: MainViewModel expone Editor` | `Editor` se crea en `OnEdicionChanged` y se pone a `null` en `CerrarEdicion` y `Cancelar`. `Guardar` llama a `Editor.AplicarA` con **ningún grupo activo** todavía. Sin cambios visibles. | S |

### 1C. Migrar la UI grupo a grupo

Cada commit: los bindings pasan de `Edicion.X` a `Editor.X`, se añade el grupo a `AplicarA`, y se ajustan los tests que escriben esos campos.

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| 1.7 | `refactor: PestanaOferta bindea contra Editor (oferta)` | Enlace, Portal, Origen, Ubicación, Modalidad, Tecnologías, Requisitos. | M |
| 1.8 | `refactor: PestanaOferta bindea contra Editor (económico)` | Salarios, pretensión e Interés (revisar `TargetNullValue` y los convertidores). | S |
| 1.9 | `refactor: PestanaSeguimiento bindea Estado y Motivo` | El convertidor `EstadoEsRechazadaAVisibilidad` ahora lee `Editor.Estado`. Comprobar a mano que el motivo aparece al elegir un rechazo. | S |
| 1.10 | `refactor: PestanaSeguimiento bindea las fechas` | Cinco `DatePicker`. | S |
| 1.11 | `refactor: PestanaSeguimiento bindea respuesta, contacto y notas` | `RespuestaEmpresa`, contacto y `Notas`. | S |
| 1.12 | `refactor: cabecera bindea Empresa y Puesto contra Editor` | Dos bindings de `MainWindow`. Se activa el grupo Identificación. Solo cambia `RellenarObligatorios` en los tests (gracias a 0.4). | M |

### 1D. Validación en el editor

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| 1.13 | `feat: validación de Empresa y Puesto en el editor` | `INotifyDataErrorInfo` / `HasErrors`. Reutiliza el mensaje de `Mensaje.FaltanDatos`. | M |
| 1.14 | `refactor: Guardar usa la validación del editor` | `Validar` pasa a leer el editor; `ReglasSolicitud.TieneDatosObligatorios` queda sin uso. | S |
| 1.15 | `feat: reglas de coherencia en el editor` | Opcional: `SalarioMin <= SalarioMax`, `Interes` entre 1 y 5, motivo solo con rechazo. Claves nuevas en es/en/de. | M |

### 1E. Adjuntos (el punto delicado: hoy dependen de `OriginalValue` de EF)

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| 1.16 | `feat: adjuntos en el editor` | `RutaCv`, `RutaCarta`, `NombreOriginalCv/Carta`, y `AdjuntoCv` / `AdjuntoCarta` con notificación propia (ya no hace falta repintar todo el panel). | M |
| 1.17 | `refactor: AdjuntarAdjunto y QuitarAdjunto operan sobre el editor` | Los dos métodos y los `DescriptorAdjunto` pasan a escribir en el editor. Se elimina `RefrescarPanelDetalle`. | M |
| 1.18 | `refactor: GestorAdjuntos compara rutas del editor con las de la entidad` | `DescartarSustituidas` y `DescartarNoGuardadas` reciben rutas en vez de `Solicitud`. Como la entidad ya no se muta hasta Guardar, `RutasGuardadas` deja de necesitar `EF OriginalValue` y se simplifica. | M |
| 1.19 | `test: ampliar tests de adjuntos` | Casos: adjuntar y cancelar, adjuntar y guardar, quitar y cancelar, reemplazar (los 32 usos de `Edicion` en `ViewModelAdjuntosTests`). | M |

Atención: 1.17 y 1.18 modifican comportamiento; los tests afectados se ajustan **en el mismo commit** para no dejar el árbol en rojo, y 1.19 solo añade cobertura nueva.

### 1F. Retirar `Edicion` y rematar

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| 1.20 | `refactor: Nueva y Duplicar construyen el editor` | Siguen creando la entidad, pero la carga la hace `CargarDesde`. | S |
| 1.21 | `test: leer de Editor en lugar de Edicion` | Los `Assert` de `ViewModelTests`, `ViewModelDialogosTests`, `ViewModelEmbudoTests` y `PestanasTests` pasan a `vm.Editor`. | M |
| 1.22 | `refactor: Edicion deja de ser pública` | Pasa a campo privado (`solicitudAbierta`). Se retira el código muerto. | S |

**Hecho cuando:** ningún XAML contiene `Edicion.` y no se usa `RefrescarPanelDetalle`.

---

## Etapa 2. Historial de eventos

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| 2.1 | `feat: EventoViewModel` | `Fecha`, `Tipo`, `Descripcion` con `INotifyPropertyChanged`; `CargarDesde` y `AplicarA`. Tests. | S |
| 2.2 | `refactor: EventosEdicion vive en el editor` | `ObservableCollection<EventoViewModel>` dentro de `SolicitudEdicionViewModel`; el XAML de `PestanaAdjuntos` conserva los nombres de propiedad. | M |
| 2.3 | `refactor: AnadirEvento opera sobre el editor` | No toca la entidad hasta Guardar. | S |
| 2.4 | `refactor: EliminarEvento opera sobre el editor` | Recuerda los eliminados con `Id != 0`. | S |
| 2.5 | `refactor: Guardar sincroniza los eventos` | Alta, modificación y baja (`db.Eventos.Remove`) al guardar; Cancelar no deja nada. Test de persistencia completa. | M |
| 2.6 | `refactor: Evento con constructor y set privados` | `Evento(fecha, tipo, descripcion)` y método `Actualizar`. Constructor privado para EF. | M |
| 2.7 | `refactor: Solicitud.AgregarEvento y QuitarEvento` | Campo privado `eventos`, métodos de negocio, y se actualizan los llamadores. | M |
| 2.8 | `refactor: configurar EF con acceso por campo para Eventos` | `UsePropertyAccessMode` en `AppDbContext`. **Test de ida y vuelta con `Include`** antes de seguir. | S |
| 2.9 | `refactor: Eventos expone IReadOnlyList` | Se retira el `set` y el `List` público. | S |
| 2.10 | `refactor: Solicitud.RegistrarEnvio sustituye a AnadirHitoInicial` | La regla del primer hito pasa al modelo y desaparece de `ReglasSolicitud`. | S |

---

## Etapa 3. Cerrar el modelo (la más cara; opcional)

### 3A. Constructor

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| 3.1 | `feat: constructor Solicitud(empresa, puesto)` | Se añade conservando el constructor sin parámetros público. | S |
| 3.2 | `refactor: Nueva, Duplicar y el builder usan el constructor nuevo` | `MainViewModel` (2 sitios) y `SolicitudBuilder`. | M |
| 3.3 | `refactor: constructor sin parámetros privado (solo EF)` | Corregir lo que quede. | S |

### 3B. Métodos de negocio (un commit cada uno, con tests de la regla)

| # | Commit | Regla que protege | Tam. |
|---|--------|-------------------|------|
| 3.4 | `feat: Solicitud.ActualizarIdentificacion` | Empresa y puesto obligatorios; recorte de espacios y `MaxLength`. | S |
| 3.5 | `feat: Solicitud.ActualizarOferta` | Recibe un `record DatosOferta` (evita un método de 8+ parámetros). | M |
| 3.6 | `feat: Solicitud.ActualizarCondiciones` | `SalarioMin <= SalarioMax`, `Interes` entre 1 y 5. | S |
| 3.7 | `feat: Solicitud.CambiarEstado` | **Decidir antes**: motivo solo con rechazo; `FechaCierre` al cerrar; qué pasa al reabrir. | M |
| 3.8 | `feat: Solicitud.ActualizarFechas` | Primera respuesta, entrevista, seguimiento; absorbe `ProponerSeguimiento`. | M |
| 3.9 | `feat: Solicitud.ActualizarContacto y ActualizarNotas` | Sin reglas fuertes; valida longitudes. | S |
| 3.10 | `feat: AsignarCv, AsignarCarta, QuitarCv, QuitarCarta` | Ruta y nombre original van siempre juntos. | S |

### 3C. El editor llama a los métodos

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| 3.11 | `refactor: AplicarA usa los métodos (identificación, oferta, condiciones)` | | S |
| 3.12 | `refactor: AplicarA usa los métodos (estado y fechas)` | | S |
| 3.13 | `refactor: AplicarA usa los métodos (contacto y adjuntos)` | | S |

### 3D. Cerrar los setters (guiado por el compilador; cada commit con test de ida y vuelta en `AppDbContextTests`)

| # | Commit | Propiedades | Tam. |
|---|--------|-------------|------|
| 3.14 | `refactor: set privado en identificación y oferta` | `Empresa`, `Puesto`, `EnlaceOferta`, `Portal`, `Origen`, `Ubicacion`, `Modalidad`, `Tecnologias`, `Requisitos` | M |
| 3.15 | `refactor: set privado en económico y fechas` | `Salario*`, `PretensionSalarial`, `Interes`, todas las fechas | M |
| 3.16 | `refactor: set privado en estado, seguimiento y contacto` | `Estado`, `MotivoRechazo`, `RespuestaEmpresa`, contacto, `Notas` | M |
| 3.17 | `refactor: set privado en adjuntos e Id` | `RutaCv`, `RutaCarta`, `NombreOriginal*`, `Id` | S |

El `SolicitudBuilder` pasa a construir mediante los métodos de la Etapa 3B.

---

## Etapa 4. Cierre

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| 4.1 | `test: Models no expone setters públicos` | Test por reflexión en `ArquitecturaTests` (o en un test de modelo). | S |
| 4.2 | `refactor: limpiar ReglasSolicitud` | Eliminar lo que haya pasado al modelo. | S |
| 4.3 | `docs: actualizar AGENTS.md` | Quitar "no lo arregles sin consultar"; describir el editor; el procedimiento de "añadir una propiedad" pasa a ser: modelo, editor, `EsquemaDb`, test y traducción. | S |
| 4.4 | `docs: actualizar README` | Sección de decisiones y lecciones aprendidas. | S |
| 4.5 | `docs: decisión sobre el editor con búfer` | `02-DOCS/decisiones/edicion-con-bufer.md`, con el mismo formato que `selector-columnas.md`. | S |
| 4.6 | `chore: pasada final de sonarcloud-gate` | Verificar antes de abrir el PR. | S |

---

## 4. Orden mínimo viable

- **Etapas 0 + 1 + 2** resuelven el problema real de edición (cancelar, validación, repintado, `PropertyDescriptor`) y dejan el historial protegido.
- **La Etapa 3** aporta encapsulación completa del modelo. Es la más costosa; hazla si quieres que el repositorio demuestre arquitectura limpia de principio a fin.
- Si te detienes tras la Etapa 2, actualiza igualmente `AGENTS.md` (4.3) para que cualquier agente sepa que la edición ya va por el editor.

## 5. Riesgos y puntos de control

| Riesgo | Cuándo | Cómo se controla |
|--------|--------|------------------|
| EF no materializa `private set` o la colección por campo | 2.8, 3.14-3.17 | Test de ida y vuelta (guardar, nuevo contexto, leer) antes de continuar. |
| Los adjuntos dejan ficheros huérfanos en `%APPDATA%` | 1.17-1.19 | Tests de adjuntar/cancelar/guardar/reemplazar con carpeta temporal (`TestDb.NuevaCarpeta()`). |
| `AplicarA` pisa datos de una pestaña aún no migrada | 1.6-1.12 | Solo se activan los grupos cuyo XAML ya apunta al editor. |
| `TreatWarningsAsErrors` rompe commits intermedios | Siempre | Compilar con `dotnet build` antes de cada commit; no dejar miembros sin usar. |
| Las reglas de `CambiarEstado` (3.7) no están definidas | 3.7 | Decidirlas y anotarlas en `02-DOCS/decisiones/` antes de implementar. |
