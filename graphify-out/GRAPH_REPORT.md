# Graph Report - GestorSolicitudes  (2026-11-26)

## Corpus Check
- 137 files · ~101,776 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 5 file(s) not represented in the graph (top: (none) 3, .props 1, .ico 1)

## Summary
- 2017 nodes · 6052 edges · 109 communities (68 shown, 41 thin omitted)
- Extraction: 71% EXTRACTED · 29% INFERRED · 0% AMBIGUOUS · INFERRED: 1780 edges (avg confidence: 0.82)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- Main table and filters
- EF Core database context
- xUnit test infrastructure
- Dialog and save tests
- CSV export tests and builder
- Editor buffer tests
- Fake dialog recorder
- Application status enum
- Solicitud entity model
- Document text extraction
- Event history model
- Solicitud validation tests
- Editor state and dates
- WPF value converters
- Attachment opening tests
- SonarCloud quality gate
- Column configuration
- Helper layer overview
- Funnel aggregation helper
- Application core and startup
- Localisation tests
- Dialog service abstraction
- Models and data layer
- Editor buffer fields
- Editor validation tests
- WPF control tests
- Main window shell
- Attachment viewer window
- Legacy attachment migration
- XAML source invariants
- Application statistics
- SonarCloud CI workflow
- Failing DbContext test doubles
- Pagination helper
- Offer editor fields
- Single instance guard
- CSV preference tests
- Event editor commands
- Status colour palette
- Test and ViewModel namespaces
- Event editor view model
- Desktop build workflow
- Dates and interest fields
- Offer data model
- Null dialog implementations
- Ponytail audit findings
- Database schema management
- CSV separator dialog
- CSV escaping tests
- Attachment viewer layout
- Architecture layer tests
- Attachment row control
- Rejection reason enum
- Solution and dependencies
- Language preference
- Security refactor decisions
- State and reopening decisions
- Event type enum
- Attachment row XAML
- Filter panel toggles
- EF Core interceptors
- Enum description helper
- Pagination view model tests
- Editing refactor plan
- Clean code review notes
- Application lifecycle
- Link opening helper
- Requirements window
- Helper refactor notes
- Metric header cards
- Work modality enum
- CSV exporter
- Culture and language switching
- Save changes prompt
- Application source enum
- Table UX decisions
- Funnel chart command
- Enum caching helper
- Project file packages
- CSV view model tests
- Attachment state enum
- Converter layer
- OpenCode configuration
- Application resources
- StyleCop configuration
- Language flag icons
- Buffer editing decision
- Search text box
- Attachment type enum
- Column header styling
- Overdue follow-up
- Filter selectors panel
- OpenCode step commands
- Solicitud builder tests
- Days column
- Caveman persona
- Apache license

## God Nodes (most connected - your core abstractions)
1. `MainViewModel` - 329 edges
2. `SolicitudBuilder` - 302 edges
3. `SolicitudEdicionViewModel` - 155 edges
4. `Window` - 152 edges
5. `Solicitud` - 127 edges
6. `SolicitudTests` - 114 edges
7. `DialogoFalso` - 108 edges
8. `SolicitudEdicionTests` - 91 edges
9. `EstadoSolicitud` - 63 edges
10. `Evento` - 62 edges

## Surprising Connections (you probably didn't know these)
- `Modelo encapsulado con métodos de negocio` --semantically_similar_to--> `Modelos sin setters públicos`  [INFERRED] [semantically similar]
  02-DOCS/plan-refactor-edicion.md → AGENTS.md
- `IDialogoService y DialogoService` --semantically_similar_to--> `Diálogos siempre por IDialogoService`  [INFERRED] [semantically similar]
  02-DOCS/revision-clean-code.md → AGENTS.md
- `Gráfica de embudo` --semantically_similar_to--> `EmbudoHelper`  [INFERRED] [semantically similar]
  README.md → 02-DOCS/revision-clean-code.md
- `Window` --references--> `MainViewModel`  [INFERRED]
  Views/MainWindow.xaml → MainViewModel.cs
- `Window` --references--> `BuscarFechasCommand`  [INFERRED]
  Views/MainWindow.xaml → MainViewModel.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Hallazgos de la auditoría atendidos por el plan Ponytail** — 02_docs_auditoria_ponytail, 02_docs_plan_refactor_ponytail, 02_docs_auditoria_ponytail_hallazgo1_transicion_cerrada, 02_docs_auditoria_ponytail_hallazgo2_change_tracker, 02_docs_auditoria_ponytail_hallazgo3_atajos_teclado, 02_docs_auditoria_ponytail_hallazgo4_instancia_unica, 02_docs_auditoria_ponytail_hallazgo5_fechas_incoherentes [EXTRACTED 1.00]
- **Edición con búfer y modelo encapsulado** — 02_docs_decisiones_edicion_con_bufer, 02_docs_decisiones_edicion_con_bufer_solicitud_edicion_viewmodel, 02_docs_plan_refactor_edicion, agents_modelos_sin_setters, agents_arquitectura_capas [INFERRED 0.85]
- **Endurecimiento de seguridad: 2 MB, migración, temporales, CSV y enlaces** — 02_docs_plan_refactor_security, 02_docs_decisiones_adjuntos_en_base_de_datos_limite_2_mb, 02_docs_decisiones_adjuntos_en_base_de_datos_migracion, 02_docs_decisiones_adjuntos_en_base_de_datos_temporales, 02_docs_plan_refactor_security_enlaces, 02_docs_plan_refactor_security_texto_helper_escapar, readme_modelo_amenaza [INFERRED 0.85]
- **Condiciones del Quality Gate Sonar way** — _opencode_skills_sonarcloud_gate_skill_quality_gate_sonar_way, _opencode_skills_sonarcloud_gate_skill_new_code_coverage, _opencode_skills_sonarcloud_gate_skill_new_code_duplication, _opencode_skills_sonarcloud_gate_skill_umbral_20_lineas [EXTRACTED 1.00]
- **Flujo de verificacion previo al commit** — _opencode_commands_commit_paso_commit_paso, _opencode_skills_sonarcloud_gate_skill_sonarcloud_gate, _opencode_skills_tests_obligatorios_skill_tests_obligatorios, _opencode_skills_sonarcloud_gate_skill_barrido_patrones [INFERRED 0.85]
- **Reglas del analizador que ya han mordido** — _opencode_skills_sonarcloud_gate_referencia_s2930, _opencode_skills_sonarcloud_gate_referencia_s3267, _opencode_skills_sonarcloud_gate_referencia_s1192, _opencode_skills_sonarcloud_gate_referencia_s1117, _opencode_skills_sonarcloud_gate_referencia_s6444, _opencode_skills_sonarcloud_gate_referencia_s6966, _opencode_skills_sonarcloud_gate_referencia_s6602 [EXTRACTED 1.00]

## Communities (109 total, 41 thin omitted)

### Community 0 - "Main table and filters"
Cohesion: 0.02
Nodes (114): ActualHeight, AlturaEntrevistas, AlturaEnviadas, AlturaOfertas, AlturaRespondidas, BuscarFechasCommand, CancelarCommand, ColumnasVm.Columnas (+106 more)

### Community 1 - "EF Core database context"
Cohesion: 0.07
Nodes (17): AppDbContext, RutaBaseDatos, DocumentosSolicitud, Carta, Cv, SolicitudId, AppDbContextTests, DocumentosSolicitudTests (+9 more)

### Community 3 - "Dialog and save tests"
Cohesion: 0.05
Nodes (29): ViewModelEmbudoTests, MainViewModel, AbrirAdjuntoCommand, AbrirEnlaceCommand, AdjuntarCartaCommand, AdjuntarCvCommand, AnadirEventoCommand, ColumnasVm (+21 more)

### Community 5 - "Editor buffer tests"
Cohesion: 0.07
Nodes (9): SolicitudEdicionViewModel, AdmiteMotivo, EstadosDisponibles, EventosEdicion, EventosEliminados, HayCambios, MotivosDelEstado, ReaperturaPendiente (+1 more)

### Community 6 - "Fake dialog recorder"
Cohesion: 0.06
Nodes (18): DialogoFalso, AbrirEnlaceLanza, AdjuntosAbiertos, Avisos, Confirmaciones, DestinoElegido, DestinosCsvPedidos, EnlacesAbiertos (+10 more)

### Community 7 - "Application status enum"
Cohesion: 0.08
Nodes (16): EstadoSolicitud, EnRevision, EntrevistaFinal, EntrevistaRrhh, EntrevistaTecnica, Enviada, OfertaAceptada, OfertaRechazada (+8 more)

### Community 8 - "Solicitud entity model"
Cohesion: 0.04
Nodes (42): Solicitudes, Solicitud, ContactoEmail, ContactoNombre, ContactoTelefono, DiasDesdeSolicitud, DiasHastaRespuesta, DiasProceso (+34 more)

### Community 9 - "Document text extraction"
Cohesion: 0.09
Nodes (10): DocumentoDemasiadoGrandeException, DocumentoHelper, DocumentoHelperTests, LectorAdjuntosTests, ManejadorErroresTests, RegistroErroresTests, LectorAdjuntos, ManejadorErrores (+2 more)

### Community 10 - "Event history model"
Cohesion: 0.10
Nodes (8): Eventos, Evento, Descripcion, Fecha, Id, Solicitud, SolicitudId, Tipo

### Community 13 - "WPF value converters"
Cohesion: 0.14
Nodes (7): EnumDescripcionConverter, EstadoAColorConverter, FechaCortaConverter, InteresATextoEstrellasConverter, NuloAVisibilidadConverter, VerGraficaATextoConverter, ConvertidorTests

### Community 15 - "SonarCloud quality gate"
Cohesion: 0.08
Nodes (32): Comando /commit-paso, Una sola idea por commit, Cobertura 0,0% por DebugType None, Estrategia de cobertura de MainViewModel, Regla de [ExcludeFromCodeCoverage], Formula de cobertura (CT + LC) / (B + EL), Medicion local != Sonar, Referencia de SonarCloud (+24 more)

### Community 16 - "Column configuration"
Cohesion: 0.11
Nodes (11): ColumnaItem, Clave, Columna, Nombre, Visible, ColumnasHelper, Ruta, ColumnasViewModel (+3 more)

### Community 17 - "Helper layer overview"
Cohesion: 0.11
Nodes (3): AdjuntosHelper, Carpeta, GestorSolicitudes.Helpers

### Community 18 - "Funnel aggregation helper"
Cohesion: 0.11
Nodes (12): EmbudoHelper, EmbudoPeriodo, AlturaEntrevistas, AlturaEnviadas, AlturaOfertas, AlturaRespondidas, Maximo, GranularidadEmbudo (+4 more)

### Community 20 - "Localisation tests"
Cohesion: 0.14
Nodes (9): LocalizacionTests, IdiomaItem, Localizacion, CulturaActual, IdiomaActual, Idioma, Aleman, Castellano (+1 more)

### Community 23 - "Editor buffer fields"
Cohesion: 0.08
Nodes (25): NuloAVisibilidad, Editor.AdmiteMotivo, Editor.ContactoEmail, Editor.ContactoNombre, Editor.ContactoTelefono, Editor.Estado, Editor.EstadosDisponibles, Editor.FechaCierre (+17 more)

### Community 24 - "Editor validation tests"
Cohesion: 0.13
Nodes (5): RangoPagina, TituloGrafica, AdjuntoCarta, AdjuntoCv, ReaperturaResumen

### Community 25 - "WPF control tests"
Cohesion: 0.12
Nodes (6): Adjunto, PestanasTests, CabeceraMetricas, PestanaAdjuntos, PestanaOferta, PestanaSeguimiento

### Community 26 - "Main window shell"
Cohesion: 0.12
Nodes (3): IconoBandeja, MainWindow, TaskbarIcon

### Community 27 - "Attachment viewer window"
Cohesion: 0.17
Nodes (4): AnteriorBoton, SiguienteBoton, VisorAdjuntoWindow, Button

### Community 28 - "Legacy attachment migration"
Cohesion: 0.19
Nodes (4): MigradorAdjuntos, FilaPendiente, LecturaAdjunto, Resuelto

### Community 31 - "Application statistics"
Cohesion: 0.13
Nodes (3): EstadisticasSolicitud, ResumenEstadisticas, EstadisticasSolicitudTests

### Community 32 - "SonarCloud CI workflow"
Cohesion: 0.10
Nodes (20): runs-on, steps, SONAR_TOKEN, jobs, build, name, on, pull_request (+12 more)

### Community 33 - "Failing DbContext test doubles"
Cohesion: 0.25
Nodes (5): AppDbContextConFallo, FalloSaveChangesUnaVez, Fallar, ContextoQueFallaUnaVez, Fallar

### Community 34 - "Pagination helper"
Cohesion: 0.17
Nodes (7): PaginadorTests, Paginador, ResultadoPagina, PuedeAvanzar, PuedeIrAInicio, PuedeIrAlFinal, PuedeRetroceder

### Community 35 - "Offer editor fields"
Cohesion: 0.12
Nodes (16): AbrirEnlaceCommand, Editor.EnlaceOferta, Editor.Interes, Editor.Modalidad, Editor.Origen, Editor.Portal, Editor.PretensionSalarial, Editor.Requisitos (+8 more)

### Community 37 - "Single instance guard"
Cohesion: 0.26
Nodes (3): InstanciaUnicaTests, InstanciaUnica, EsPrimera

### Community 38 - "CSV preference tests"
Cohesion: 0.24
Nodes (3): PreferenciaCsvTests, PreferenciaCsv, Ruta

### Community 39 - "Event editor commands"
Cohesion: 0.12
Nodes (15): AbrirAdjuntoCommand, AdjuntarCartaCommand, AdjuntarCvCommand, AnadirEventoCommand, DataContext.EliminarEventoCommand, DataContext.TiposEvento, Descripcion, Editor.AdjuntoCarta (+7 more)

### Community 40 - "Status colour palette"
Cohesion: 0.16
Nodes (3): ColoresEstado, Traducciones, IdiomaActual

### Community 41 - "Test and ViewModel namespaces"
Cohesion: 0.21
Nodes (3): GestorSolicitudes.ViewModels, GestorSolicitudes.Tests, ViewModelTestExtensions

### Community 42 - "Event editor view model"
Cohesion: 0.17
Nodes (3): EventoViewModel, Original, EventoViewModelTests

### Community 43 - "Desktop build workflow"
Cohesion: 0.12
Nodes (15): runs-on, steps, jobs, build, name, on, pull_request, push (+7 more)

### Community 44 - "Dates and interest fields"
Cohesion: 0.16
Nodes (15): FechaCorta, InteresATextoEstrellas, FechaEntrevista, FechaPrimeraRespuesta, FechaSolicitud, Interes, Portal, ProximoSeguimiento (+7 more)

### Community 47 - "Ponytail audit findings"
Cohesion: 0.25
Nodes (14): Auditoría Ponytail, Hallazgo 10: la guarda de fórmulas del CSV solo mira el primer carácter, Hallazgo 1: guardar Oferta aceptada desde una fila cerrada mata la app, Hallazgo 2: un guardado fallido se escribe después desde el ChangeTracker, Hallazgo 3: Ctrl+S y Ctrl+N ignoran el control con el foco, Hallazgo 4: una segunda copia sobrescribe la primera, Hallazgo 5: fechas contradictorias y métricas negativas, Hallazgo 6: dos pipelines de CI para el mismo PR (+6 more)

### Community 49 - "CSV separator dialog"
Cohesion: 0.21
Nodes (7): Coma, PuntoYComa, Tabulador, Window, VentanaSeparadorCsv, SeparadorElegido, RadioButton

### Community 51 - "Attachment viewer layout"
Cohesion: 0.17
Nodes (12): BarraPaginas, CajaTexto, ImagenPagina, ScrollContenido, TextoAviso, TextoPagina, Window, StackPanel (+4 more)

### Community 53 - "Attachment row control"
Cohesion: 0.17
Nodes (6): FilaAdjunto, Adjunto, ComandoAbrir, ComandoAdjuntar, ComandoQuitar, Etiqueta

### Community 54 - "Rejection reason enum"
Cohesion: 0.17
Nodes (11): MotivoRechazo, AceptoOtraOferta, CulturalFit, ExperienciaInsuficiente, ModalidadOUbicacion, NoIndicado, Otro, PuestoCanceladoOPausado (+3 more)

### Community 55 - "Solution and dependencies"
Cohesion: 0.17
Nodes (10): GestorSolicitudes.Tests, net8.0-windows10.0.19041, StyleCop.Analyzers (1.2.0-beta.556), Microsoft.NET.Sdk, coverlet.msbuild (10.0.1), Microsoft.NET.Test.Sdk (17.11.1), TngTech.ArchUnitNET (0.13.4), xunit (2.9.2) (+2 more)

### Community 56 - "Language preference"
Cohesion: 0.27
Nodes (3): PreferenciaIdiomaTests, PreferenciaIdioma, Ruta

### Community 57 - "Security refactor decisions"
Cohesion: 0.22
Nodes (11): Decisión: documentos en la base de datos, Tabla DocumentosSolicitud (BLOB sin navegación), Límite de 2 MB como invariante del almacén, Migración idempotente de adjuntos antiguos a la base, Temporales para abrir documentos, MigradorAdjuntos, TemporalesAdjuntos, Plan de refactorización de seguridad (+3 more)

### Community 58 - "State and reopening decisions"
Cohesion: 0.20
Nodes (10): Decisión: estados, transiciones y reapertura, EsRespuestaDeLaEmpresa, EstadoSolicitud y grupos abiertos/cerrados, FechaReapertura y FueReabierta, MotivoRechazo ampliado y motivos por estado, Solicitud.Reabrir (nota obligatoria, fecha de reapertura), Tabla de transiciones de estado, InstanciaUnica (mutex y aviso entre copias) (+2 more)

### Community 60 - "Event type enum"
Cohesion: 0.18
Nodes (11): TipoEvento, Entrevista, Llamada, Nota, Oferta, PruebaTecnica, Reapertura, Rechazo (+3 more)

### Community 61 - "Attachment row XAML"
Cohesion: 0.20
Nodes (9): Adjunto.Nombre, Adjunto.Tipo, ComandoAbrir, ComandoAdjuntar, ComandoQuitar, raiz, UserControl, Etiqueta (+1 more)

### Community 62 - "Filter panel toggles"
Cohesion: 0.20
Nodes (10): PanelDesdeAbierto, PanelEstadosAbierto, PanelHastaAbierto, PanelDesdeAbierto, PanelEstadosAbierto, PanelHastaAbierto, BotonDesde, BotonEstados (+2 more)

### Community 64 - "Enum description helper"
Cohesion: 0.24
Nodes (4): EnumItem, EnumHelperTests, SinDescripcion, Uno

### Community 67 - "Editing refactor plan"
Cohesion: 0.22
Nodes (9): Plan de refactor: edición con búfer y modelo encapsulado, Etapa 5: correcciones tras la revisión final, Etapa R: estados y reapertura, EventoViewModel y emparejamiento por Original, Modelo encapsulado con métodos de negocio, Modelos sin setters públicos, CONTEXT.md: estado del proyecto, Decisiones vigentes del proyecto (+1 more)

### Community 68 - "Clean code review notes"
Cohesion: 0.25
Nodes (9): Revisión de clean code (segunda pasada), Localizacion monolítica, MainViewModel como clase Dios, Test de paridad de idiomas que no comprueba paridad, Revisión de clean code (tercera pasada), Directory.Build.props y puertas de calidad, GestorAdjuntos (copias y sustituciones), Reloj inyectable (TimeProvider/IClock) (+1 more)

### Community 73 - "Requirements window"
Cohesion: 0.28
Nodes (4): TextoRequisitos, Window, VentanaRequisitos, TextBox

### Community 74 - "Helper refactor notes"
Cohesion: 0.25
Nodes (7): TextoHelper.Escapar y neutralización de fórmulas, Revisión de clean code (primera pasada), CsvExportador, IDialogoService y DialogoService, EmbudoHelper, EstadisticasSolicitud, Gráfica de embudo

### Community 75 - "Metric header cards"
Cohesion: 0.25
Nodes (7): EnEntrevista, MediaDiasRespuesta, OfertasRecibidas, ProcesosAbiertos, TasaRespuesta, TotalSolicitudes, UserControl

### Community 76 - "Work modality enum"
Cohesion: 0.25
Nodes (5): Modalidad, Hibrido, Presencial, Remoto, SinIndicar

### Community 79 - "Save changes prompt"
Cohesion: 0.32
Nodes (4): ResultadoGuardar, Cancelar, Descartar, Guardar

### Community 80 - "Application source enum"
Cohesion: 0.25
Nodes (7): Origen, AplicacionDirecta, FeriaDeEmpleo, Networking, Otro, RecruiterMeContacto, Referido

### Community 81 - "Table UX decisions"
Cohesion: 0.29
Nodes (7): Decisión: paginación de la tabla, Paginar en el ViewModel y propiedad Pagina, Decisión: selector de columnas, ColumnasHelper y ColumnasViewModel, Paginador y ResultadoPagina, README: Gestor de candidaturas, Stack .NET 8 WPF MVVM + EF Core SQLite

### Community 82 - "Funnel chart command"
Cohesion: 0.29
Nodes (7): ConfigurarGraficaCommand, VerGraficaATexto, VerGrafica, ConfigurarGraficaCommand, VerGrafica, BotonGrafica, Button

### Community 85 - "Project file packages"
Cohesion: 0.29
Nodes (7): net8.0-windows10.0.19041, GestorSolicitudes, StyleCop.Analyzers (1.2.0-beta.556), Microsoft.NET.Sdk, CommunityToolkit.Mvvm (8.3.2), Hardcodet.NotifyIcon.Wpf (2.0.1), Microsoft.EntityFrameworkCore.Sqlite (8.0.31)

### Community 87 - "Attachment state enum"
Cohesion: 0.29
Nodes (7): EstadoAdjunto, Ausente, DemasiadoGrande, Fuera, Ilegible, Leido, SinRuta

### Community 90 - "OpenCode configuration"
Cohesion: 0.33
Nodes (5): instructions, plugin, $schema, skills, paths

### Community 91 - "Application resources"
Cohesion: 0.50
Nodes (4): Application, borde, marco, Border

### Community 92 - "StyleCop configuration"
Cohesion: 0.40
Nodes (4): companyName, $schema, settings, documentationRules

### Community 93 - "Language flag icons"
Cohesion: 0.40
Nodes (5): BanderaAlemania, BanderaEspana, BanderaInglaterra, Grid_Cabecera, Grid

### Community 94 - "Buffer editing decision"
Cohesion: 0.50
Nodes (3): Decisión: edición con búfer y modelo encapsulado, HayCambios y ResolverCambiosPendientes, SolicitudEdicionViewModel (búfer de edición)

### Community 95 - "Search text box"
Cohesion: 0.50
Nodes (4): TextoBusqueda, TextoBusqueda, BuscarEmpresas, TextBox

### Community 96 - "Attachment type enum"
Cohesion: 0.50
Nodes (3): TipoAdjunto, Carta, Cv

### Community 99 - "Column header styling"
Cohesion: 0.67
Nodes (3): Decisión: fondo de las cabeceras de columnas, Estilo CabeceraColumna (DataGridColumnHeader), Test AppXaml_LosEstilosConPlantillaSeComponenAlPintar

### Community 102 - "Filter selectors panel"
Cohesion: 0.67
Nodes (3): StackPanel_Selectores, StackPanelOpciones, StackPanel

## Knowledge Gaps
- **414 isolated node(s):** `Carpeta`, `RutaBaseDatos`, `Columna`, `Clave`, `Visible` (+409 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 538 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **41 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MainViewModel` connect `Dialog and save tests` to `Main table and filters`, `EF Core database context`, `xUnit test infrastructure`, `Editor buffer tests`, `Fake dialog recorder`, `Application status enum`, `Solicitud entity model`, `Document text extraction`, `Event history model`, `Attachment opening tests`, `Column configuration`, `Funnel aggregation helper`, `Localisation tests`, `Dialog service abstraction`, `Editor validation tests`, `WPF control tests`, `Main window shell`, `Main view model commands`, `Failing DbContext test doubles`, `Pagination helper`, `ViewModels layer overview`, `Single instance guard`, `CSV preference tests`, `Status colour palette`, `Test and ViewModel namespaces`, `Event editor view model`, `CSV escaping tests`, `Date handling in editor`, `Filter panel toggles`, `Enum description helper`, `Pagination view model tests`, `Application lifecycle`, `Link opening helper`, `Work modality enum`, `Funnel chart command`, `CSV view model tests`, `Solicitud construction tests`, `Search text box`, `Selection and paging`, `Overdue follow-up`?**
  _High betweenness centrality (0.323) - this node is a cross-community bridge._
- **Are the 221 inferred relationships involving `MainViewModel` (e.g. with `App.OnStartup()` and `ColumnasTests.Columnas_CadaClaveTieneSuTextoEnElDiccionario()`) actually correct?**
  _`MainViewModel` has 221 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Carpeta`, `RutaBaseDatos`, `Columna` to the rest of the system?**
  _414 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Main table and filters` be split into smaller, more focused modules?**
  _Cohesion score 0.017391304347826087 - nodes in this community are weakly interconnected._
- **Why does `Window` connect `Main table and filters` to `Dialog and save tests`, `Filter selectors panel`, `Days column`, `Dates and interest fields`, `Funnel chart command`, `Main window shell`, `Attachment viewer window`, `Language flag icons`, `Filter panel toggles`, `Search text box`?**
  _High betweenness centrality (0.100) - this node is a cross-community bridge._
- **Are the 268 inferred relationships involving `SolicitudBuilder` (e.g. with `AppDbContextTests.ActualizarEvento_SeGuardaYRecuperaCompleto()` and `AppDbContextTests.BorrarSolicitud_EliminaSusEventosEnCascada()`) actually correct?**
  _`SolicitudBuilder` has 268 INFERRED edges - model-reasoned connections that need verification._
- **Should `EF Core database context` be split into smaller, more focused modules?**
  _Cohesion score 0.07027168234064786 - nodes in this community are weakly interconnected._