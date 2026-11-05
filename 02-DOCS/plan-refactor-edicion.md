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
6. El push lo hace el autor tras cada commit, para que SonarCloud analice la rama remota. El agente no hace push.

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

> **Revisión:** el commit 3.7 se reduce a un método que solo asigna (sin reglas nuevas) y el commit 3.8 se divide en 3.8a y 3.8b. Las reglas de estado y la reapertura pasan a la **Etapa R**, entre la 3 y la 4. Así `Estado`, `MotivoRechazo` y `FechaCierre` pueden cerrarse en 3.16 sin esperar a las decisiones de negocio. Además, `FechaSolicitud` y `RespuestaEmpresa` no tenían método propietario: ahora lo tienen (3.8a y 3.8b).
>
> **Revisión 2:** se añaden 3.10b, 3.10c y 3.13b, se precisan 3.8a, 3.8b y las longitudes máximas, y se incorpora una auditoría de escrituras directas antes de 3.14.

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
| 3.7 | `feat: Solicitud.CambiarEstado` | Versión reducida: `CambiarEstado(estado, motivo, fechaCierre)` **solo asigna** `Estado`, `MotivoRechazo` y `FechaCierre`, conservando el comportamiento actual. Sin transiciones ni validación de motivo; esas reglas van en R.4. Tests de caracterización del comportamiento vigente. | S |
| 3.8a | `feat: Solicitud.ActualizarFechas` | `FechaSolicitud`, entrevista y seguimiento. Absorbe `ProponerSeguimiento` como método de instancia, con las mismas condiciones que la regla actual. `ProximoSeguimiento` tiene dos orígenes: lo edita el usuario en el búfer y lo propone la app al preparar una solicitud nueva; ambos pasan por el modelo. Tests: una solicitud nueva abierta recibe seguimiento propuesto y uno ya informado no se pisa. | M |
| 3.8b | `feat: Solicitud.RegistrarRespuesta` | `RegistrarRespuesta(fecha?, texto?)`: ambos valores opcionales e **independientes**, como hoy (puede haber texto sin fecha y fecha sin texto). `RespuestaEmpresa` no tiene límite hoy, así que no se añade validación salvo que se decida lo contrario (ver "Longitudes máximas"). El editor llama una sola vez con los dos valores (ver 3.12). | S |
| 3.9 | `feat: Solicitud.ActualizarContacto y ActualizarNotas` | Sin reglas de negocio fuertes; valida las longitudes de `ContactoNombre` (120), `ContactoEmail` (150) y `ContactoTelefono` (40). `Notas` no tiene límite hoy (ver "Longitudes máximas"). | S |
| 3.10 | `feat: AsignarCv, AsignarCarta, QuitarCv, QuitarCarta` | Ruta y nombre original van siempre juntos. Valida las longitudes de `RutaCv` y `RutaCarta` (500) y de `NombreOriginalCv` y `NombreOriginalCarta` (260). | S |
| 3.10b | `refactor: Duplicar y PrepararNueva sin asignaciones directas` | `Solicitud.CrearCopia()` para Duplicar. `PrepararNueva` usa el constructor y los métodos de 3.4 a 3.8a, sin asignar campos sueltos. Cubre los inicializadores que sobreviven al constructor. | M |
| 3.10c | `test: el editor no modifica la entidad hasta Guardar` | Abrir una solicitud, cambiar varios campos del editor y comprobar que la entidad conserva sus valores; cancelar y comprobar entidad y base de datos. Protege 3.11 a 3.13. | S |

**Longitudes máximas.** Hoy hay constantes privadas (`LongitudMaximaX`) para identificación, enlace, portal, ubicación, tecnologías y requisitos. El resto de campos usa números literales en `[MaxLength]`: `ContactoNombre` 120, `ContactoEmail` 150, `ContactoTelefono` 40, `RutaCv` y `RutaCarta` 500, `NombreOriginalCv` y `NombreOriginalCarta` 260. `RespuestaEmpresa` y `Notas` no tienen límite. Seguir el patrón existente: una constante por campo, compartida por `[MaxLength]` y por la validación del método. Si el editor necesita reutilizarlas para `INotifyDataErrorInfo`, pasan a `public const`. ☐ Decidir si `RespuestaEmpresa` y `Notas` siguen siendo texto libre (recomendado: sí, porque añadir un límite cambiaría el comportamiento actual).

### 3C. El editor llama a los métodos

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| 3.11 | `refactor: AplicarA usa los métodos (identificación, oferta, condiciones)` | Sustituye las asignaciones de `AplicarIdentificacion`, `AplicarOferta` y `AplicarEconomico`. Estos métodos lanzan `ArgumentException` si un texto supera su límite, así que comprobar que el editor valida las longitudes (o que los `TextBox` tienen `MaxLength`) antes de llamar, para que Guardar no falle con una excepción. | S |
| 3.12 | `refactor: AplicarA usa los métodos (estado y fechas)` | Sustituye `AplicarEstado`, `AplicarFechas` y `AplicarRespuesta`. Usa `CambiarEstado` (3.7), `ActualizarFechas` (3.8a) y `RegistrarRespuesta` (3.8b). Como `FechaPrimeraRespuesta` y `RespuestaEmpresa` hoy se aplican en grupos distintos, se reorganizan para llamar una sola vez a `solicitud.RegistrarRespuesta(editor.FechaPrimeraRespuesta, editor.RespuestaEmpresa)`. | M |
| 3.13 | `refactor: AplicarA usa los métodos (contacto y adjuntos)` | Sustituye `AplicarContacto` y `AplicarAdjuntos`. | S |
| 3.13b | `test: un fallo de SaveChanges conserva adjuntos y editor` | Con un interceptor de EF que lance en `SaveChanges`: no se borra el adjunto anterior, el editor sigue abierto, las copias temporales siguen en disco y se puede reintentar el guardado. Si el test destapa un fallo en `Guardar`, se corrige antes en un commit `fix:` propio. | M |

### 3D. Cerrar los setters (guiado por el compilador; cada commit con test de ida y vuelta en `AppDbContextTests`)

| # | Commit | Propiedades | Tam. |
|---|--------|-------------|------|
| 3.14 | `refactor: set privado en identificación y oferta` | `Empresa`, `Puesto`, `EnlaceOferta`, `Portal`, `Origen`, `Ubicacion`, `Modalidad`, `Tecnologias`, `Requisitos` | M |
| 3.15 | `refactor: set privado en económico y fechas` | `Salario*`, `PretensionSalarial`, `Interes` y todas las fechas (`FechaSolicitud` por 3.8a, primera respuesta por 3.8b, `FechaCierre` por 3.7) | M |
| 3.16 | `refactor: set privado en estado, seguimiento y contacto` | `Estado` y `MotivoRechazo` (por 3.7), `RespuestaEmpresa` (por 3.8b), contacto, `Notas` | M |
| 3.17 | `refactor: set privado en adjuntos e Id` | `RutaCv`, `RutaCarta`, `NombreOriginal*`, `Id`. El test de ida y vuelta comprueba que la base asigna el `Id` al guardar; revisar los tests que lo fijen a mano. | S |

El `SolicitudBuilder` pasa a construir mediante los métodos de la Etapa 3B.

**Control previo a 3.14: auditoría de escrituras directas.** El compilador detecta toda asignación a un `set` privado, pero no las escrituras por reflexión ni las de EF. Antes de empezar, buscar y anotar el resultado (ajustar la lista a los nombres reales de las propiedades):

```
rg -n "\.(Empresa|Puesto|EnlaceOferta|Portal|Origen|Ubicacion|Modalidad|Tecnologias|Requisitos|SalarioMin|SalarioMax|PretensionSalarial|Interes|FechaSolicitud|FechaPrimeraRespuesta|FechaEntrevista|ProximoSeguimiento|FechaCierre|Estado|MotivoRechazo|RespuestaEmpresa|ContactoNombre|ContactoEmail|ContactoTelefono|Notas|RutaCv|RutaCarta|NombreOriginalCv|NombreOriginalCarta|Id)\s*=[^=]" --glob "*.cs"
rg -n "SetValues|CurrentValue|GetProperty\(" --glob "*.cs"
```

Debe cubrir producción, tests, `SolicitudBuilder`, inicializadores de objetos, `Duplicar`, `PrepararNueva` y los datos de prueba de `AppDbContext`. Tras 3.13 solo deberían quedar asignaciones dentro de `Solicitud` y del builder.

---

## Etapa R. Estados y reapertura (entre la Etapa 3 y la 4)

Detalle, tablas y decisiones tomadas: `02-DOCS/decisiones/estados-y-reapertura.md`. No se empieza hasta cerrar la Etapa 3 (3.17). La garantía de que ningún camino escribe `Estado` sin pasar por el modelo viene de 3.16. Tras 3.17 los `set` son privados, así que `Reabrir` puede escribir `FechaCierre`, `MotivoRechazo`, `ProximoSeguimiento` y `FechaReapertura` desde dentro de `Solicitud`.

Decisiones que condicionan la etapa: no se añaden estados nuevos; el motivo de cierre es obligatorio en Descartado, Retirada y Oferta rechazada por mí, con `NoIndicado`, valores propios y desplegable filtrado por estado; `DiasProceso` se reinicia al reabrir; la reapertura se identifica con la columna `FechaReapertura` (solo la última, sin contador), insignia y color de fila. El versionado de CV queda fuera de alcance.

Mismas reglas de siempre: un commit por paso, con el mensaje indicado. **El orden importa:** varios commits van antes de otros para no romper la interfaz ni los datos existentes (ver las notas de cada bloque).

### R-A. Hito y motivos

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| R.1 | `feat: TipoEvento.Reapertura` | Valor 9, traducciones es/en/de, test de paridad. | S |
| R.2 | `feat: nuevos valores de MotivoRechazo` | Al final del enum: `NoIndicado` (6), `AceptoOtraOferta` (7), `ModalidadOUbicacion` (8), `YaNoMeInteresa` (9). Claves es/en/de, test de paridad. Comprobar que el selector de columnas y el CSV no dependen de la lista cerrada. | S |
| R.3 | `feat: motivos permitidos por estado` | Tabla en el modelo (`EstadoSolicitudExtensiones`): `AdmiteMotivo(estado)` y `MotivosPermitidos(estado)`. Tests por estado. | S |
| R.4 | `feat: rellenar el motivo de cierre en bases existentes` | En `EsquemaDb`, tras `VerificarColumnasFaltantes`, los dos `UPDATE` de la sección 4.4 del documento de decisión. Idempotente. Tests con SQLite temporal. **Debe ir antes de R.10.** | M |

### R-B. Reapertura en el modelo

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| R.5 | `feat: Solicitud.FechaReapertura` | `DateTime?` con `set` privado, `[NotMapped] FueReabierta`, columna `TEXT NULL` en `EsquemaDb` y test de ida y vuelta. Todavía nada la escribe. | M |
| R.6 | `feat: DiasProceso cuenta desde la reapertura` | Referencia `FechaReapertura ?? FechaSolicitud`. Tests: abierta, cerrada y reabierta. | S |
| R.7 | `feat: Solicitud.Reabrir(nuevoEstado, nota)` | Efectos y precondiciones de la sección 6 del documento (estado, limpia cierre y motivo, `FechaReapertura = hoy`, primera respuesta, seguimiento, hito). Tests. | M |

### R-C. Motivo de cierre en la interfaz y en el modelo

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| R.8 | `feat: desplegable de motivo filtrado por estado` | El editor expone los motivos del estado, sin la entrada "Sin especificar". El panel se muestra en los tres estados (sustituye a `EstadoEsRechazadaAVisibilidad`). La etiqueta `Seguimiento.MotivoRechazo` pasa a "Motivo del cierre" en es/en/de. | M |
| R.9 | `feat: el editor preselecciona NoIndicado` | Al pasar a uno de los tres estados con el motivo vacío o no permitido; lo limpia al salir. Tests. | S |
| R.10 | `feat: CambiarEstado exige y valida el motivo` | Obligatorio y permitido en los tres estados; en el resto lo deja en `null`. `SolicitudBuilder` pone `NoIndicado` por defecto en esos estados. **Solo después de R.4, R.8 y R.9.** | M |

### R-D. Reapertura en la interfaz

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| R.11 | `feat: el editor detecta cerrado → abierto` | Expone que hay una reapertura pendiente. | S |
| R.12 | `feat: confirmación al reabrir` | Diálogo con estado, fecha y motivo del cierre, nuevo estado y nota opcional; Guardar llama a `Reabrir`. Confirmación reforzada desde Oferta aceptada. | M |
| R.13 | `feat: CambiarEstado valida las transiciones` | Cerrado → abierto solo por `Reabrir`; `FechaCierre` = hoy al cerrar si está vacía; reglas A, B y C del documento. Reescribir `CambiarEstado_SinMotivoNiFecha_LosDejaNulos`. **Solo después de R.11 y R.12.** | M |
| R.14 | `feat: insignia ↻ en la tabla` | Junto al estado, sobre `FueReabierta`; tooltip con la fecha. XAML y traducciones. | M |
| R.15 | `feat: color de fila para reabiertas` | Borde izquierdo magenta `#DB2777` con `DataTrigger` sobre `FueReabierta` en el `RowStyle`; no pisa el ámbar de seguimiento pendiente. | S |
| R.16 | `feat: tooltip de Días con el total desde el envío` | Total desde el envío y fecha de reapertura. | S |
| R.17 | `feat: franja informativa en Seguimiento` | Visible solo si hay reapertura; lee el último hito `Reapertura`. | M |

### R-E. Opcionales

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| R.18 | `feat: filtro Reabiertas` | Sobre `FueReabierta`. | M |
| R.19 | `feat: SolicitudAnteriorId` | Columna nullable, `EsquemaDb`, prueba de migración. | M |
| R.20 | `feat: Duplicar enlaza con la candidatura anterior` | | S |
| R.21 | `feat: aviso al guardar una candidatura repetida` | | M |

### Cierre de la etapa

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| R.22 | `docs: registrar la decisión de estados y reapertura` | Actualizar `02-DOCS/decisiones/` y `AGENTS.md` con las transiciones, el motivo de cierre y `FechaReapertura`. | S |

---

## Etapa 4. Cierre

| # | Commit | Qué cambia | Tam. |
|---|--------|-----------|------|
| 4.1 | `test: Models no expone setters públicos` | Test por reflexión en `ArquitecturaTests` (o en un test de modelo). Sin excepciones: tras 3.17 todo es privado. | S |
| 4.2 | `refactor: limpiar ReglasSolicitud` | Eliminar lo que haya pasado al modelo. | S |
| 4.3 | `docs: actualizar AGENTS.md` | Quitar "no lo arregles sin consultar"; describir el editor; el procedimiento de "añadir una propiedad" pasa a ser: modelo, editor, `EsquemaDb`, test y traducción. | S |
| 4.4 | `docs: actualizar README` | Sección de decisiones y lecciones aprendidas. | S |
| 4.5 | `docs: decisión sobre el editor con búfer` | `02-DOCS/decisiones/edicion-con-bufer.md`, con el mismo formato que `selector-columnas.md`. | S |
| 4.6 | `chore: pasada final de sonarcloud-gate` | Verificar antes de abrir el PR. | S |

---

## 4. Orden mínimo viable

- **Etapas 0 + 1 + 2** resuelven el problema real de edición (cancelar, validación, repintado, `PropertyDescriptor`) y dejan el historial protegido.
- **La Etapa 3** aporta encapsulación completa del modelo. Es la más costosa; hazla si quieres que el repositorio demuestre arquitectura limpia de principio a fin.
- **La Etapa R** (estados y reapertura) añade comportamiento nuevo, no refactor. Puede hacerse más adelante, pero siempre después de la Etapa 3.
- Si te detienes tras la Etapa 2, actualiza igualmente `AGENTS.md` (4.3) para que cualquier agente sepa que la edición ya va por el editor.

## 5. Riesgos y puntos de control

| Riesgo | Cuándo | Cómo se controla |
|--------|--------|------------------|
| EF no materializa `private set` o la colección por campo | 2.8, 3.14-3.17 | Test de ida y vuelta (guardar, nuevo contexto, leer) antes de continuar. |
| Los adjuntos dejan ficheros huérfanos en `%APPDATA%` | 1.17-1.19 | Tests de adjuntar/cancelar/guardar/reemplazar con carpeta temporal (`TestDb.NuevaCarpeta()`). |
| `AplicarA` pisa datos de una pestaña aún no migrada | 1.6-1.12 | Solo se activan los grupos cuyo XAML ya apunta al editor. |
| `TreatWarningsAsErrors` rompe commits intermedios | Siempre | Compilar con `dotnet build` antes de cada commit; no dejar miembros sin usar. |
| Exigir el motivo de cierre antes de tener datos y editor listos | R.10 | R.4 (relleno de bases existentes), R.8 y R.9 (desplegable filtrado y preselección) van antes; si no, Guardar fallaría en filas existentes. |
| Prohibir cerrado → abierto antes de que la interfaz use `Reabrir` | R.13 | R.11 y R.12 van antes de R.13; si no, reabrir desde la interfaz fallaría. |
| Escrituras que el compilador no ve (reflexión, `SetValues`) | 3.14 | Auditoría con `rg` antes de empezar. |
| Un fallo de `SaveChanges` pierde el adjunto anterior o cierra el editor | 3.13b | Test con interceptor de EF; corrección en un `fix:` previo si falla. |
| Un test fija el `Id` a mano y deja de compilar | 3.17 | Revisarlo en la auditoría y usar la base para asignarlo. |
| Las longitudes de `[MaxLength]` y las del modelo divergen | 3.9, 3.10 | Una constante por campo compartida por el atributo y la validación. |
| `CambiarEstado` queda sin reglas hasta la Etapa R | 3.7 a R.13 | Es temporal y conserva el comportamiento actual; los tests de caracterización de 3.7 lo fijan. |
