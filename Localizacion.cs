namespace GestorSolicitudes;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;

/// <summary>Idioma de la interfaz. El selector de la cabecera se apoya en este enum.</summary>
public enum Idioma
{
    Castellano,
    Ingles,
    Aleman,
}

/// <summary>Opción del selector de idioma: idioma + nombre tal como se ve en su propia lengua.</summary>
public sealed record IdiomaItem(Idioma Idioma, string Nombre);

/// <summary>
/// Traducciones de toda la interfaz y gestión del idioma activo. La fuente única de los
/// textos son los diccionarios de este archivo; el recurso XAML que se inyecta en la
/// aplicación se construye a partir de ellos para que los {DynamicResource} se actualicen
/// al cambiar de idioma sin reiniciar. La supresión de la regla de literales duplicados
/// (S1192) es a propósito: la misma palabra se repite como clave y como texto en los tres
/// idiomas, y definir constantes ensuciaría la tabla de traducciones.
/// </summary>
[SuppressMessage("SonarAnalyzer.CSharp", "S1192",
    Justification = "Las traducciones se repiten por naturaleza: la misma palabra es clave y valor en varios idiomas.")]
public static class Localizacion
{
    /// <summary>Se dispara al cambiar de idioma, para que la interfaz refresque lo que no usa DynamicResource.</summary>
    public static event EventHandler<Idioma>? IdiomaCambiado;

    private static readonly IReadOnlyDictionary<string, string> TextoCastellano = Listar(
        // ----- Ventana y cabecera -----
        ("TituloVentana", "Gestor de candidaturas"),
        ("SubtituloVentana", "Seguimiento de ofertas a las que me he inscrito"),
        ("Error", "Error"),
        ("Confirmar", "Confirmar"),

        // ----- Métricas -----
        ("Metrica.Enviadas", "Enviadas"),
        ("Metrica.Abiertas", "Abiertas"),
        ("Metrica.EnProceso", "En proceso"),
        ("Metrica.Ofertas", "Ofertas"),
        ("Metrica.TasaRespuesta", "Tasa de respuesta"),
        ("Metrica.MediaRespuesta", "Media de respuesta"),
        ("Metrica.Dias", "días"),

        // ----- Botones de la cabecera -----
        ("Boton.ImportarLinkedIn", "Importar LinkedIn"),
        ("Boton.ExportarCsv", "Exportar CSV"),
        ("Boton.NuevaCandidatura", "+  Nueva candidatura"),

        // ----- Gráfica -----
        ("Grafica.Titulo", "Embudo del último año, por mes (enviadas → respondidas → entrevistas → ofertas)"),
        ("Grafica.Enviadas", "Enviadas"),
        ("Grafica.Respondidas", "Respondidas"),
        ("Grafica.Entrevistas", "Entrevistas"),
        ("Grafica.Ofertas", "Ofertas"),
("Grafica.Ver", "Gráfico"),
        ("Grafica.Ocultar", "Ocultar gráfica"),

        // ----- Filtros -----
        ("Filtro.BuscarToolTip", "Busca por empresa, puesto, tecnología, ubicación o portal"),
        ("Filtro.BorrarBusqueda", "Borrar la búsqueda"),
        ("Filtro.SoloAbiertas", "Solo abiertas"),
        ("Filtro.SeguimientoPendiente", "Seguimiento pendiente"),
        ("Filtro.SinEspecificar", "Sin especificar"),
        ("Filtro.TodosLosEstados", "Todos los estados"),

        // ----- Columnas de la tabla -----
        ("Columna.Empresa", "Empresa"),
        ("Columna.Puesto", "Puesto"),
        ("Columna.Estado", "Estado"),
        ("Columna.Enviada", "Enviada"),
        ("Columna.Dias", "Días"),
        ("Columna.DiasToolTip", "Días desde el envío; si el proceso ya se cerró, cuánto duró hasta el cierre"),
        ("Columna.PrimeraRespuesta", "1ª respuesta"),
        ("Columna.Entrevista", "Entrevista"),
        ("Columna.Seguimiento", "Seguimiento"),
        ("Columna.Interes", "Interés"),
        ("Columna.Portal", "Portal"),

        // ----- Panel de detalle -----
        ("Detalle.Vacio", "Selecciona una candidatura de la lista\n o crea una nueva."),
        ("Detalle.Empresa", "Empresa *"),
        ("Detalle.Puesto", "Puesto *"),
        ("Detalle.VerTodas", "Ver todas"),
        ("Detalle.VerTodasToolTip", "Filtra la lista a todas las candidaturas de esta empresa"),
        ("Pestana.Oferta", "Oferta"),
        ("Pestana.Seguimiento", "Seguimiento"),
        ("Pestana.AdjuntosHistorial", "Adjuntos e historial"),

        // ----- Pestaña Oferta -----
        ("Oferta.EnlaceOferta", "Enlace de la oferta"),
        ("Oferta.PortalOrigen", "Portal de origen"),
        ("Oferta.PortalToolTip", "LinkedIn, InfoJobs, Tecnoempleo, web de la empresa, referido..."),
        ("Oferta.Ubicacion", "Ubicación"),
        ("Oferta.Modalidad", "Modalidad"),
        ("Oferta.ViaContacto", "Vía de contacto"),
        ("Oferta.ViaContactoToolTip", "Cómo llegó el contacto: distinto de dónde estaba publicada la oferta"),
        ("Oferta.Interes", "Interés (1-5)"),
        ("Oferta.Tecnologias", "Tecnologías de la oferta"),
        ("Oferta.TecnologiasToolTip", "Separadas por comas: C#, .NET 8, Vue, SQL Server..."),
        ("Oferta.SalarioMin", "Salario mín."),
        ("Oferta.SalarioMax", "Salario máx."),
        ("Oferta.Pretension", "Mi pretensión"),
        ("Oferta.Requisitos", "Requisitos del cargo"),
        ("Oferta.RequisitosToolTip", "Requisitos, responsabilidades y funciones del puesto"),

        // ----- Pestaña Seguimiento -----
        ("Seguimiento.EstadoActual", "Estado actual"),
        ("Seguimiento.MotivoRechazo", "Motivo del rechazo"),
        ("Seguimiento.FechaSolicitud", "Fecha de solicitud"),
        ("Seguimiento.PrimeraContestacion", "1ª contestación"),
        ("Seguimiento.DiaEntrevista", "Día de la entrevista"),
        ("Seguimiento.ProximoSeguimiento", "Próximo seguimiento"),
        ("Seguimiento.ProximoSeguimientoToolTip", "Cuándo toca volver a escribir si no hay noticias"),
        ("Seguimiento.FechaCierre", "Fecha de cierre"),
        ("Seguimiento.RespuestaEmpresa", "Respuesta de la empresa"),
        ("Seguimiento.Contacto", "CONTACTO"),

        // ----- Pestaña Adjuntos e historial -----
        ("Adjuntos.Titulo", "ADJUNTOS"),
        ("Adjuntos.Texto", "El CV y la carta concretos que enviaste a esta oferta, guardados en copia local."),
        ("Adjuntos.Curriculum", "Curriculum"),
        ("Adjuntos.Carta", "Carta de presentación"),
        ("Adjuntos.SinAdjuntar", "Sin adjuntar"),
        ("Historial.Titulo", "HISTORIAL"),
        ("Historial.Anadir", "+ Añadir"),

        // ----- Camios comunes -----
        ("Campo.Nombre", "Nombre"),
        ("Campo.Email", "Email"),
        ("Campo.Telefono", "Teléfono"),
        ("Campo.Notas", "Notas"),
        ("Boton.Abrir", "Abrir"),
        ("Boton.Quitar", "Quitar"),
        ("Boton.Adjuntar", "Adjuntar…"),
        ("Boton.Leer", "Leer"),
        ("Boton.Eliminar", "Eliminar"),
        ("Boton.Duplicar", "Duplicar"),
        ("Boton.Cancelar", "Cancelar"),
        ("Boton.Guardar", "Guardar"),
        ("Boton.Cerrar", "Cerrar"),

        // ----- Bandeja -----
        ("Bandeja.Abrir", "Abrir Gestor de candidaturas"),
        ("Bandeja.Comprobar", "Comprobar seguimientos ahora"),
        ("Bandeja.Salir", "Salir"),
        ("Bandeja.SeguimientoVencido", "Seguimiento vencido"),
        ("Bandeja.VencidoTexto", "{0} — {1}. Pendiente desde el {2}."),
        ("Bandeja.VencidosMultiples", "{0} seguimientos vencidos"),
        ("Bandeja.RecordatorioLinea", "• {0} — {1}"),

        // ----- Diálogos de fichero -----
        ("Dialogo.SeleccionaCv", "Selecciona el CV que enviaste"),
        ("Dialogo.SeleccionaCarta", "Selecciona la carta de presentación"),
        ("Dialogo.FiltroAdjuntos", "Documentos (*.pdf;*.docx;*.doc)|*.pdf;*.docx;*.doc|Currículos (*.pdf;*.docx;*.doc)|*.pdf;*.docx;*.doc|Todos los archivos (*.*)|*.*"),
        ("Dialogo.ImportarCsvLinkedIn", "Importar el CSV de 'Mis candidaturas' de LinkedIn"),
        ("Dialogo.ExportarCandidaturas", "Exportar candidaturas"),

        // ----- Mensajes -----
        ("Mensaje.NoSePudoAdjuntar", "No se pudo adjuntar el fichero:\n\n{0}"),
        ("Mensaje.SinAdjuntoOYaNoExiste", "No hay un fichero adjunto, o ya no existe en disco."),
        ("Mensaje.NoSePudoAbrirFichero", "No se pudo abrir el fichero: {0}"),
        ("Mensaje.NoSePudoAbrirEnlace", "No se pudo abrir el enlace: {0}"),
        ("Mensaje.NoSePudoLeerFichero", "No se pudo leer el fichero:\n\n{0}"),
        ("Mensaje.CsvSinFilas", "El fichero parece no tener filas de datos."),
        ("Mensaje.CsvColumnas", "No se reconocen las columnas de empresa o puesto en la cabecera.\n\nSe espera el CSV que exporta LinkedIn en Ajustes → Privacidad de datos → 'Obtener una copia de tus datos' (fichero Jobs)."),
        ("Mensaje.FaltanDatos", "La empresa y el puesto son obligatorios."),
        ("Mensaje.NoSePudoGuardar", "No se pudo guardar: {0}"),
        ("Mensaje.ConfirmarEliminar", "¿Eliminar la candidatura de {0} ({1})?"),
        ("Mensaje.ErrorBaseDatos", "No se pudo preparar la base de datos:\n\n{0}\n\nRuta: {1}"),

        // ----- Títulos de avisos -----
        ("Titulo.Importar", "Importar"),
        ("Titulo.Adjunto", "Adjunto"),
        ("Titulo.FaltanDatos", "Faltan datos"),
        ("Titulo.ImportacionCompletada", "Importación completada"),
        ("Titulo.ExportacionCompletada", "Exportación completada"),
        ("Titulo.ErrorAlIniciar", "Error al iniciar"),

        // ----- Hitos del historial -----
        ("Evento.CandidaturaEnviada", "Candidatura enviada"),
        ("Evento.ImportadaLinkedIn", "Importada desde LinkedIn"),
        ("Importar.EventoLinkedIn", "Evento LinkedIn: {0}"),

        // ----- Importación / duplicar / exportar -----
        ("Importar.ResumenImportadas", "Se importaron {0} candidaturas desde LinkedIn."),
        ("Importar.ResumenDuplicadas", "Se omitieron {0} ya existentes."),
        ("Importar.ResumenOmitidas", "Se saltaron {0} filas sin empresa o puesto."),
        ("Duplicar.Nota", "Duplicada de la candidatura #{0} ({1}, {2})."),

        // ----- Ventana de requisitos -----
        ("VentanaRequisitos.Titulo", "Requisitos del cargo"),

        // ----- Selector de idioma -----
        ("Selector.ToolTip", "Idioma de la interfaz"),
        ("Idioma.Espanol", "Español"),
        ("Idioma.Ingles", "Inglés"),
        ("Idioma.Aleman", "Alemán"),

        // ----- Cabeceras del CSV de exportación -----
        ("Csv.Empresa", "Empresa"),
        ("Csv.Puesto", "Puesto"),
        ("Csv.Estado", "Estado"),
        ("Csv.Portal", "Portal"),
        ("Csv.Ubicacion", "Ubicacion"),
        ("Csv.Modalidad", "Modalidad"),
        ("Csv.FechaSolicitud", "Fecha solicitud"),
        ("Csv.PrimeraRespuesta", "Primera respuesta"),
        ("Csv.DiasHastaRespuesta", "Dias hasta respuesta"),
        ("Csv.Entrevista", "Entrevista"),
        ("Csv.Cierre", "Cierre"),
        ("Csv.ProximoSeguimiento", "Proximo seguimiento"),
        ("Csv.SalarioMin", "Salario min"),
        ("Csv.SalarioMax", "Salario max"),
        ("Csv.Pretension", "Pretension"),
        ("Csv.Interes", "Interes"),
        ("Csv.Tecnologias", "Tecnologias"),
        ("Csv.Contacto", "Contacto"),
        ("Csv.EmailContacto", "Email contacto"),
        ("Csv.RespuestaEmpresa", "Respuesta empresa"),
        ("Csv.Notas", "Notas"),
        ("Csv.Enlace", "Enlace"),
        ("Csv.ExportadasN", "Exportadas {0} candidaturas."),

        // ----- Enums (EstadoSolicitud, Modalidad, TipoEvento, MotivoRechazo, Origen) -----
        ("EstadoSolicitud.Enviada", "Enviada"),
        ("EstadoSolicitud.EnRevision", "En revisión"),
        ("EstadoSolicitud.PruebaTecnica", "Prueba técnica"),
        ("EstadoSolicitud.EntrevistaRrhh", "Entrevista RR. HH."),
        ("EstadoSolicitud.EntrevistaTecnica", "Entrevista técnica"),
        ("EstadoSolicitud.EntrevistaFinal", "Entrevista final"),
        ("EstadoSolicitud.OfertaRecibida", "Oferta recibida"),
        ("EstadoSolicitud.OfertaAceptada", "Oferta aceptada"),
        ("EstadoSolicitud.OfertaRechazada", "Oferta rechazada por mí"),
        ("EstadoSolicitud.Rechazada", "Descartado por la empresa"),
        ("EstadoSolicitud.Retirada", "Retirada por mí"),
        ("EstadoSolicitud.SinRespuesta", "Sin respuesta"),
        ("Modalidad.SinIndicar", "Sin indicar"),
        ("Modalidad.Presencial", "Presencial"),
        ("Modalidad.Hibrido", "Híbrido"),
        ("Modalidad.Remoto", "100 % remoto"),
        ("TipoEvento.Nota", "Nota"),
        ("TipoEvento.SolicitudEnviada", "Solicitud enviada"),
        ("TipoEvento.Respuesta", "Respuesta de la empresa"),
        ("TipoEvento.Llamada", "Llamada / screening"),
        ("TipoEvento.PruebaTecnica", "Prueba técnica"),
        ("TipoEvento.Entrevista", "Entrevista"),
        ("TipoEvento.Seguimiento", "Seguimiento enviado"),
        ("TipoEvento.Oferta", "Oferta"),
        ("TipoEvento.Rechazo", "Rechazo"),
        ("MotivoRechazo.Salario", "Salario / pretensión"),
        ("MotivoRechazo.ExperienciaInsuficiente", "Experiencia insuficiente"),
        ("MotivoRechazo.SeleccionaronOtroCandidato", "Seleccionaron a otro candidato"),
        ("MotivoRechazo.CulturalFit", "No encajaba con el equipo/cultura"),
        ("MotivoRechazo.PuestoCanceladoOPausado", "Puesto cancelado o pausado"),
        ("MotivoRechazo.Otro", "Otro motivo"),
        ("Origen.AplicacionDirecta", "Aplicación directa"),
        ("Origen.RecruiterMeContacto", "Recruiter me contactó"),
        ("Origen.Referido", "Referido por alguien"),
        ("Origen.Networking", "Networking / evento"),
        ("Origen.FeriaDeEmpleo", "Feria de empleo"),
        ("Origen.Otro", "Otro"));

    private static readonly IReadOnlyDictionary<string, string> TextoIngles = Listar(
        // ----- Ventana y cabecera -----
        ("TituloVentana", "Application Tracker"),
        ("SubtituloVentana", "Track the job offers you have applied to"),
        ("Error", "Error"),
        ("Confirmar", "Confirm"),

        // ----- Métricas -----
        ("Metrica.Enviadas", "Sent"),
        ("Metrica.Abiertas", "Open"),
        ("Metrica.EnProceso", "In process"),
        ("Metrica.Ofertas", "Offers"),
        ("Metrica.TasaRespuesta", "Response rate"),
        ("Metrica.MediaRespuesta", "Avg. response time"),
        ("Metrica.Dias", "days"),

        // ----- Botones de la cabecera -----
        ("Boton.ImportarLinkedIn", "Import LinkedIn"),
        ("Boton.ExportarCsv", "Export CSV"),
        ("Boton.NuevaCandidatura", "+  New application"),

        // ----- Gráfica -----
        ("Grafica.Titulo", "Last-year funnel by month (sent → answered → interviews → offers)"),
        ("Grafica.Enviadas", "Sent"),
        ("Grafica.Respondidas", "Answered"),
        ("Grafica.Entrevistas", "Interviews"),
        ("Grafica.Ofertas", "Offers"),
("Grafica.Ver", "Chart"),
        ("Grafica.Ocultar", "Hide chart"),

        // ----- Filtros -----
        ("Filtro.BuscarToolTip", "Search by company, position, technology, location or portal"),
        ("Filtro.BorrarBusqueda", "Clear search"),
        ("Filtro.SoloAbiertas", "Open only"),
        ("Filtro.SeguimientoPendiente", "Follow-up due"),
        ("Filtro.SinEspecificar", "Not specified"),
        ("Filtro.TodosLosEstados", "All statuses"),

        // ----- Columnas de la tabla -----
        ("Columna.Empresa", "Company"),
        ("Columna.Puesto", "Position"),
        ("Columna.Estado", "Status"),
        ("Columna.Enviada", "Sent"),
        ("Columna.Dias", "Days"),
        ("Columna.DiasToolTip", "Days since it was sent; once closed, how long the process lasted"),
        ("Columna.PrimeraRespuesta", "1st response"),
        ("Columna.Entrevista", "Interview"),
        ("Columna.Seguimiento", "Follow-up"),
        ("Columna.Interes", "Interest"),
        ("Columna.Portal", "Portal"),

        // ----- Panel de detalle -----
        ("Detalle.Vacio", "Select an application from the list\nor create a new one."),
        ("Detalle.Empresa", "Company *"),
        ("Detalle.Puesto", "Position *"),
        ("Detalle.VerTodas", "View all"),
        ("Detalle.VerTodasToolTip", "Filters the list to all applications from this company"),
        ("Pestana.Oferta", "Offer"),
        ("Pestana.Seguimiento", "Follow-up"),
        ("Pestana.AdjuntosHistorial", "Attachments & history"),

        // ----- Pestaña Oferta -----
        ("Oferta.EnlaceOferta", "Offer link"),
        ("Oferta.PortalOrigen", "Source portal"),
        ("Oferta.PortalToolTip", "LinkedIn, InfoJobs, Tecnoempleo, company website, referral..."),
        ("Oferta.Ubicacion", "Location"),
        ("Oferta.Modalidad", "Mode"),
        ("Oferta.ViaContacto", "Contact method"),
        ("Oferta.ViaContactoToolTip", "How the contact came about, as opposed to where the offer was posted"),
        ("Oferta.Interes", "Interest (1-5)"),
        ("Oferta.Tecnologias", "Job technologies"),
        ("Oferta.TecnologiasToolTip", "Comma separated: C#, .NET 8, Vue, SQL Server..."),
        ("Oferta.SalarioMin", "Min. salary"),
        ("Oferta.SalarioMax", "Max. salary"),
        ("Oferta.Pretension", "My expected salary"),
        ("Oferta.Requisitos", "Job requirements"),
        ("Oferta.RequisitosToolTip", "Requirements, responsibilities and duties of the position"),

        // ----- Pestaña Seguimiento -----
        ("Seguimiento.EstadoActual", "Current status"),
        ("Seguimiento.MotivoRechazo", "Reason for rejection"),
        ("Seguimiento.FechaSolicitud", "Application date"),
        ("Seguimiento.PrimeraContestacion", "First response"),
        ("Seguimiento.DiaEntrevista", "Interview day"),
        ("Seguimiento.ProximoSeguimiento", "Next follow-up"),
        ("Seguimiento.ProximoSeguimientoToolTip", "When you should write again if there is no news"),
        ("Seguimiento.FechaCierre", "Closing date"),
        ("Seguimiento.RespuestaEmpresa", "Company response"),
        ("Seguimiento.Contacto", "CONTACT"),

        // ----- Pestaña Adjuntos e historial -----
        ("Adjuntos.Titulo", "ATTACHMENTS"),
        ("Adjuntos.Texto", "The exact CV and cover letter you sent for this offer, stored locally."),
        ("Adjuntos.Curriculum", "CV"),
        ("Adjuntos.Carta", "Cover letter"),
        ("Adjuntos.SinAdjuntar", "Not attached"),
        ("Historial.Titulo", "HISTORY"),
        ("Historial.Anadir", "+ Add"),

        // ----- Camios comunes -----
        ("Campo.Nombre", "Name"),
        ("Campo.Email", "Email"),
        ("Campo.Telefono", "Phone"),
        ("Campo.Notas", "Notes"),
        ("Boton.Abrir", "Open"),
        ("Boton.Quitar", "Remove"),
        ("Boton.Adjuntar", "Attach…"),
        ("Boton.Leer", "Read"),
        ("Boton.Eliminar", "Delete"),
        ("Boton.Duplicar", "Duplicate"),
        ("Boton.Cancelar", "Cancel"),
        ("Boton.Guardar", "Save"),
        ("Boton.Cerrar", "Close"),

        // ----- Bandeja -----
        ("Bandeja.Abrir", "Open Application Tracker"),
        ("Bandeja.Comprobar", "Check follow-ups now"),
        ("Bandeja.Salir", "Exit"),
        ("Bandeja.SeguimientoVencido", "Follow-up overdue"),
        ("Bandeja.VencidoTexto", "{0} — {1}. Due since {2}."),
        ("Bandeja.VencidosMultiples", "{0} follow-ups overdue"),
        ("Bandeja.RecordatorioLinea", "• {0} — {1}"),

        // ----- Diálogos de fichero -----
        ("Dialogo.SeleccionaCv", "Select the CV you sent"),
        ("Dialogo.SeleccionaCarta", "Select the cover letter"),
        ("Dialogo.FiltroAdjuntos", "Documents (*.pdf;*.docx;*.doc)|*.pdf;*.docx;*.doc|CVs (*.pdf;*.docx;*.doc)|*.pdf;*.docx;*.doc|All files (*.*)|*.*"),
        ("Dialogo.ImportarCsvLinkedIn", "Import the 'My Jobs' CSV from LinkedIn"),
        ("Dialogo.ExportarCandidaturas", "Export applications"),

        // ----- Mensajes -----
        ("Mensaje.NoSePudoAdjuntar", "Could not attach the file:\n\n{0}"),
        ("Mensaje.SinAdjuntoOYaNoExiste", "There is no attached file, or it no longer exists on disk."),
        ("Mensaje.NoSePudoAbrirFichero", "Could not open the file: {0}"),
        ("Mensaje.NoSePudoAbrirEnlace", "Could not open the link: {0}"),
        ("Mensaje.NoSePudoLeerFichero", "Could not read the file:\n\n{0}"),
        ("Mensaje.CsvSinFilas", "The file does not seem to have any data rows."),
        ("Mensaje.CsvColumnas", "The company or position columns were not recognized in the header.\n\nExpected CSV exported by LinkedIn under Settings → Data privacy → 'Get a copy of your data' (Jobs file)."),
        ("Mensaje.FaltanDatos", "Company and position are required."),
        ("Mensaje.NoSePudoGuardar", "Could not save: {0}"),
        ("Mensaje.ConfirmarEliminar", "Delete the application for {0} ({1})?"),
        ("Mensaje.ErrorBaseDatos", "Could not prepare the database:\n\n{0}\n\nPath: {1}"),

        // ----- Títulos de avisos -----
        ("Titulo.Importar", "Import"),
        ("Titulo.Adjunto", "Attachment"),
        ("Titulo.FaltanDatos", "Missing data"),
        ("Titulo.ImportacionCompletada", "Import completed"),
        ("Titulo.ExportacionCompletada", "Export completed"),
        ("Titulo.ErrorAlIniciar", "Startup error"),

        // ----- Hitos del historial -----
        ("Evento.CandidaturaEnviada", "Application sent"),
        ("Evento.ImportadaLinkedIn", "Imported from LinkedIn"),
        ("Importar.EventoLinkedIn", "LinkedIn event: {0}"),

        // ----- Importación / duplicar / exportar -----
        ("Importar.ResumenImportadas", "{0} applications imported from LinkedIn."),
        ("Importar.ResumenDuplicadas", "{0} existing ones were skipped."),
        ("Importar.ResumenOmitidas", "{0} rows without company or position were skipped."),
        ("Duplicar.Nota", "Duplicated from application #{0} ({1}, {2})."),

        // ----- Ventana de requisitos -----
        ("VentanaRequisitos.Titulo", "Job requirements"),

        // ----- Selector de idioma -----
        ("Selector.ToolTip", "Interface language"),
        ("Idioma.Espanol", "Spanish"),
        ("Idioma.Ingles", "English"),
        ("Idioma.Aleman", "German"),

        // ----- Cabeceras del CSV de exportación -----
        ("Csv.Empresa", "Company"),
        ("Csv.Puesto", "Position"),
        ("Csv.Estado", "Status"),
        ("Csv.Portal", "Portal"),
        ("Csv.Ubicacion", "Location"),
        ("Csv.Modalidad", "Work mode"),
        ("Csv.FechaSolicitud", "Application date"),
        ("Csv.PrimeraRespuesta", "First response"),
        ("Csv.DiasHastaRespuesta", "Days to response"),
        ("Csv.Entrevista", "Interview"),
        ("Csv.Cierre", "Closing"),
        ("Csv.ProximoSeguimiento", "Next follow-up"),
        ("Csv.SalarioMin", "Min salary"),
        ("Csv.SalarioMax", "Max salary"),
        ("Csv.Pretension", "Expected salary"),
        ("Csv.Interes", "Interest"),
        ("Csv.Tecnologias", "Technologies"),
        ("Csv.Contacto", "Contact"),
        ("Csv.EmailContacto", "Contact email"),
        ("Csv.RespuestaEmpresa", "Company response"),
        ("Csv.Notas", "Notes"),
        ("Csv.Enlace", "Link"),
        ("Csv.ExportadasN", "Exported {0} applications."),

        // ----- Enums (EstadoSolicitud, Modalidad, TipoEvento, MotivoRechazo, Origen) -----
        ("EstadoSolicitud.Enviada", "Sent"),
        ("EstadoSolicitud.EnRevision", "Under review"),
        ("EstadoSolicitud.PruebaTecnica", "Technical test"),
        ("EstadoSolicitud.EntrevistaRrhh", "HR interview"),
        ("EstadoSolicitud.EntrevistaTecnica", "Technical interview"),
        ("EstadoSolicitud.EntrevistaFinal", "Final interview"),
        ("EstadoSolicitud.OfertaRecibida", "Offer received"),
        ("EstadoSolicitud.OfertaAceptada", "Offer accepted"),
        ("EstadoSolicitud.OfertaRechazada", "Offer declined by me"),
        ("EstadoSolicitud.Rechazada", "Rejected by the company"),
        ("EstadoSolicitud.Retirada", "Withdrawn by me"),
        ("EstadoSolicitud.SinRespuesta", "No response"),
        ("Modalidad.SinIndicar", "Not specified"),
        ("Modalidad.Presencial", "On-site"),
        ("Modalidad.Hibrido", "Hybrid"),
        ("Modalidad.Remoto", "100% remote"),
        ("TipoEvento.Nota", "Note"),
        ("TipoEvento.SolicitudEnviada", "Application sent"),
        ("TipoEvento.Respuesta", "Company response"),
        ("TipoEvento.Llamada", "Call / screening"),
        ("TipoEvento.PruebaTecnica", "Technical test"),
        ("TipoEvento.Entrevista", "Interview"),
        ("TipoEvento.Seguimiento", "Follow-up sent"),
        ("TipoEvento.Oferta", "Offer"),
        ("TipoEvento.Rechazo", "Rejection"),
        ("MotivoRechazo.Salario", "Salary / expectation"),
        ("MotivoRechazo.ExperienciaInsuficiente", "Insufficient experience"),
        ("MotivoRechazo.SeleccionaronOtroCandidato", "Another candidate was chosen"),
        ("MotivoRechazo.CulturalFit", "Did not fit the team/culture"),
        ("MotivoRechazo.PuestoCanceladoOPausado", "Position cancelled or on hold"),
        ("MotivoRechazo.Otro", "Other reason"),
        ("Origen.AplicacionDirecta", "Direct application"),
        ("Origen.RecruiterMeContacto", "Recruiter contacted me"),
        ("Origen.Referido", "Referral"),
        ("Origen.Networking", "Networking / event"),
        ("Origen.FeriaDeEmpleo", "Job fair"),
        ("Origen.Otro", "Other"));

    private static readonly IReadOnlyDictionary<string, string> TextoAleman = Listar(
        // ----- Ventana y cabecera -----
        ("TituloVentana", "Bewerbungstracker"),
        ("SubtituloVentana", "Verfolge die Stellenangebote, bei denen du dich beworben hast"),
        ("Error", "Fehler"),
        ("Confirmar", "Bestätigen"),

        // ----- Métricas -----
        ("Metrica.Enviadas", "Gesendet"),
        ("Metrica.Abiertas", "Offen"),
        ("Metrica.EnProceso", "In Bearbeitung"),
        ("Metrica.Ofertas", "Angebote"),
        ("Metrica.TasaRespuesta", "Antwortquote"),
        ("Metrica.MediaRespuesta", "Ø Antwortzeit"),
        ("Metrica.Dias", "Tage"),

        // ----- Botones de la cabecera -----
        ("Boton.ImportarLinkedIn", "LinkedIn importieren"),
        ("Boton.ExportarCsv", "CSV exportieren"),
        ("Boton.NuevaCandidatura", "+  Neue Bewerbung"),

        // ----- Gráfica -----
        ("Grafica.Titulo", "Trichter des letzten Jahres nach Monat (gesendet → Antworten → Interviews → Angebote)"),
        ("Grafica.Enviadas", "Gesendet"),
        ("Grafica.Respondidas", "Beantwortet"),
        ("Grafica.Entrevistas", "Interviews"),
        ("Grafica.Ofertas", "Angebote"),
("Grafica.Ver", "Diagramm"),
        ("Grafica.Ocultar", "Diagramm ausblenden"),

        // ----- Filtros -----
        ("Filtro.BuscarToolTip", "Suche nach Unternehmen, Position, Technologie, Standort oder Portal"),
        ("Filtro.BorrarBusqueda", "Suche löschen"),
        ("Filtro.SoloAbiertas", "Nur offene"),
        ("Filtro.SeguimientoPendiente", "Follow-up fällig"),
        ("Filtro.SinEspecificar", "Nicht angegeben"),
        ("Filtro.TodosLosEstados", "Alle Status"),

        // ----- Columnas de la tabla -----
        ("Columna.Empresa", "Unternehmen"),
        ("Columna.Puesto", "Position"),
        ("Columna.Estado", "Status"),
        ("Columna.Enviada", "Gesendet"),
        ("Columna.Dias", "Tage"),
        ("Columna.DiasToolTip", "Tage seit dem Versand; nach Abschluss, wie lange der Prozess gedauert hat"),
        ("Columna.PrimeraRespuesta", "1. Antwort"),
        ("Columna.Entrevista", "Interview"),
        ("Columna.Seguimiento", "Follow-up"),
        ("Columna.Interes", "Interesse"),
        ("Columna.Portal", "Portal"),

        // ----- Panel de detalle -----
        ("Detalle.Vacio", "Wähle eine Bewerbung aus der Liste\noder erstelle eine neue."),
        ("Detalle.Empresa", "Unternehmen *"),
        ("Detalle.Puesto", "Position *"),
        ("Detalle.VerTodas", "Alle anzeigen"),
        ("Detalle.VerTodasToolTip", "Filtert die Liste auf alle Bewerbungen dieses Unternehmens"),
        ("Pestana.Oferta", "Angebot"),
        ("Pestana.Seguimiento", "Follow-up"),
        ("Pestana.AdjuntosHistorial", "Anhänge & Verlauf"),

        // ----- Pestaña Oferta -----
        ("Oferta.EnlaceOferta", "Link zum Angebot"),
        ("Oferta.PortalOrigen", "Ursprungsportal"),
        ("Oferta.PortalToolTip", "LinkedIn, InfoJobs, Tecnoempleo, Unternehmenswebsite, Empfehlung..."),
        ("Oferta.Ubicacion", "Standort"),
        ("Oferta.Modalidad", "Arbeitsmodell"),
        ("Oferta.ViaContacto", "Kontaktweg"),
        ("Oferta.ViaContactoToolTip", "Wie der Kontakt entstanden ist, statt wo das Angebot veröffentlicht wurde"),
        ("Oferta.Interes", "Interesse (1-5)"),
        ("Oferta.Tecnologias", "Benötigte Technologien"),
        ("Oferta.TecnologiasToolTip", "Kommagetrennt: C#, .NET 8, Vue, SQL Server..."),
        ("Oferta.SalarioMin", "Mindestgehalt"),
        ("Oferta.SalarioMax", "Höchstgehalt"),
        ("Oferta.Pretension", "Meine Gehaltsvorstellung"),
        ("Oferta.Requisitos", "Anforderungen an die Stelle"),
        ("Oferta.RequisitosToolTip", "Anforderungen, Verantwortlichkeiten und Aufgaben der Position"),

        // ----- Pestaña Seguimiento -----
        ("Seguimiento.EstadoActual", "Aktueller Status"),
        ("Seguimiento.MotivoRechazo", "Grund der Ablehnung"),
        ("Seguimiento.FechaSolicitud", "Bewerbungsdatum"),
        ("Seguimiento.PrimeraContestacion", "Erste Antwort"),
        ("Seguimiento.DiaEntrevista", "Tag des Interviews"),
        ("Seguimiento.ProximoSeguimiento", "Nächstes Follow-up"),
        ("Seguimiento.ProximoSeguimientoToolTip", "Wann du wieder schreiben solltest, falls keine Nachricht kommt"),
        ("Seguimiento.FechaCierre", "Abschlussdatum"),
        ("Seguimiento.RespuestaEmpresa", "Antwort des Unternehmens"),
        ("Seguimiento.Contacto", "KONTAKT"),

        // ----- Pestaña Adjuntos e historial -----
        ("Adjuntos.Titulo", "ANHÄNGE"),
        ("Adjuntos.Texto", "Der konkrete Lebenslauf und das Anschreiben, die du für dieses Angebot eingereicht hast, lokal gespeichert."),
        ("Adjuntos.Curriculum", "Lebenslauf"),
        ("Adjuntos.Carta", "Anschreiben"),
        ("Adjuntos.SinAdjuntar", "Nicht angehängt"),
        ("Historial.Titulo", "VERLAUF"),
        ("Historial.Anadir", "+ Hinzufügen"),

        // ----- Camios comunes -----
        ("Campo.Nombre", "Name"),
        ("Campo.Email", "E-Mail"),
        ("Campo.Telefono", "Telefon"),
        ("Campo.Notas", "Notizen"),
        ("Boton.Abrir", "Öffnen"),
        ("Boton.Quitar", "Entfernen"),
        ("Boton.Adjuntar", "Anhängen…"),
        ("Boton.Leer", "Lesen"),
        ("Boton.Eliminar", "Löschen"),
        ("Boton.Duplicar", "Duplizieren"),
        ("Boton.Cancelar", "Abbrechen"),
        ("Boton.Guardar", "Speichern"),
        ("Boton.Cerrar", "Schließen"),

        // ----- Bandeja -----
        ("Bandeja.Abrir", "Bewerbungstracker öffnen"),
        ("Bandeja.Comprobar", "Follow-ups jetzt prüfen"),
        ("Bandeja.Salir", "Beenden"),
        ("Bandeja.SeguimientoVencido", "Follow-up überfällig"),
        ("Bandeja.VencidoTexto", "{0} — {1}. Fällig seit {2}."),
        ("Bandeja.VencidosMultiples", "{0} überfällige Follow-ups"),
        ("Bandeja.RecordatorioLinea", "• {0} — {1}"),

        // ----- Diálogos de fichero -----
        ("Dialogo.SeleccionaCv", "Wähle den Lebenslauf, den du eingereicht hast"),
        ("Dialogo.SeleccionaCarta", "Wähle das Anschreiben"),
        ("Dialogo.FiltroAdjuntos", "Dokumente (*.pdf;*.docx;*.doc)|*.pdf;*.docx;*.doc|Lebensläufe (*.pdf;*.docx;*.doc)|*.pdf;*.docx;*.doc|Alle Dateien (*.*)|*.*"),
        ("Dialogo.ImportarCsvLinkedIn", "LinkedIn-CSV »Meine Jobs« importieren"),
        ("Dialogo.ExportarCandidaturas", "Bewerbungen exportieren"),

        // ----- Mensajes -----
        ("Mensaje.NoSePudoAdjuntar", "Anhängen nicht möglich:\n\n{0}"),
        ("Mensaje.SinAdjuntoOYaNoExiste", "Keine Anlage vorhanden oder die Datei ist nicht mehr verfügbar."),
        ("Mensaje.NoSePudoAbrirFichero", "Die Datei konnte nicht geöffnet werden: {0}"),
        ("Mensaje.NoSePudoAbrirEnlace", "Der Link konnte nicht geöffnet werden: {0}"),
        ("Mensaje.NoSePudoLeerFichero", "Die Datei konnte nicht gelesen werden:\n\n{0}"),
        ("Mensaje.CsvSinFilas", "Die Datei scheint keine Datenzeilen zu enthalten."),
        ("Mensaje.CsvColumnas", "Die Spalten für Unternehmen oder Position wurden in der Kopfzeile nicht erkannt.\n\nErwartet wird die CSV-Datei, die LinkedIn unter Einstellungen → Datenschutz → „Kopie deiner Daten“ exportiert (Datei „Jobs“)."),
        ("Mensaje.FaltanDatos", "Unternehmen und Position sind Pflichtfelder."),
        ("Mensaje.NoSePudoGuardar", "Speichern fehlgeschlagen: {0}"),
        ("Mensaje.ConfirmarEliminar", "Bewerbung von {0} ({1}) löschen?"),
        ("Mensaje.ErrorBaseDatos", "Die Datenbank konnte nicht vorbereitet werden:\n\n{0}\n\nPfad: {1}"),

        // ----- Títulos de avisos -----
        ("Titulo.Importar", "Importieren"),
        ("Titulo.Adjunto", "Anlage"),
        ("Titulo.FaltanDatos", "Fehlende Angaben"),
        ("Titulo.ImportacionCompletada", "Import abgeschlossen"),
        ("Titulo.ExportacionCompletada", "Export abgeschlossen"),
        ("Titulo.ErrorAlIniciar", "Startfehler"),

        // ----- Hitos del historial -----
        ("Evento.CandidaturaEnviada", "Bewerbung gesendet"),
        ("Evento.ImportadaLinkedIn", "Von LinkedIn importiert"),
        ("Importar.EventoLinkedIn", "LinkedIn-Event: {0}"),

        // ----- Importación / duplicar / exportar -----
        ("Importar.ResumenImportadas", "{0} Bewerbungen von LinkedIn importiert."),
        ("Importar.ResumenDuplicadas", "{0} bereits vorhandene übersprungen."),
        ("Importar.ResumenOmitidas", "{0} Zeilen ohne Unternehmen oder Position übersprungen."),
        ("Duplicar.Nota", "Dupliziert aus Bewerbung Nr. {0} ({1}, {2})."),

        // ----- Ventana de requisitos -----
        ("VentanaRequisitos.Titulo", "Anforderungen an die Stelle"),

        // ----- Selector de idioma -----
        ("Selector.ToolTip", "Sprache der Oberfläche"),
        ("Idioma.Espanol", "Spanisch"),
        ("Idioma.Ingles", "Englisch"),
        ("Idioma.Aleman", "Deutsch"),

        // ----- Cabeceras del CSV de exportación -----
        ("Csv.Empresa", "Unternehmen"),
        ("Csv.Puesto", "Position"),
        ("Csv.Estado", "Status"),
        ("Csv.Portal", "Portal"),
        ("Csv.Ubicacion", "Standort"),
        ("Csv.Modalidad", "Arbeitsmodell"),
        ("Csv.FechaSolicitud", "Bewerbungsdatum"),
        ("Csv.PrimeraRespuesta", "Erste Antwort"),
        ("Csv.DiasHastaRespuesta", "Tage bis Antwort"),
        ("Csv.Entrevista", "Interview"),
        ("Csv.Cierre", "Abschluss"),
        ("Csv.ProximoSeguimiento", "Nächstes Follow-up"),
        ("Csv.SalarioMin", "Mindestgehalt"),
        ("Csv.SalarioMax", "Höchstgehalt"),
        ("Csv.Pretension", "Gehaltsvorstellung"),
        ("Csv.Interes", "Interesse"),
        ("Csv.Tecnologias", "Technologien"),
        ("Csv.Contacto", "Kontakt"),
        ("Csv.EmailContacto", "Kontakt-E-Mail"),
        ("Csv.RespuestaEmpresa", "Antwort des Unternehmens"),
        ("Csv.Notas", "Notizen"),
        ("Csv.Enlace", "Link"),
        ("Csv.ExportadasN", "{0} Bewerbungen exportiert."),

        // ----- Enums (EstadoSolicitud, Modalidad, TipoEvento, MotivoRechazo, Origen) -----
        ("EstadoSolicitud.Enviada", "Gesendet"),
        ("EstadoSolicitud.EnRevision", "In Prüfung"),
        ("EstadoSolicitud.PruebaTecnica", "Technischer Test"),
        ("EstadoSolicitud.EntrevistaRrhh", "HR-Interview"),
        ("EstadoSolicitud.EntrevistaTecnica", "Technisches Interview"),
        ("EstadoSolicitud.EntrevistaFinal", "Endgespräch"),
        ("EstadoSolicitud.OfertaRecibida", "Angebot erhalten"),
        ("EstadoSolicitud.OfertaAceptada", "Angebot angenommen"),
        ("EstadoSolicitud.OfertaRechazada", "Angebot von mir abgelehnt"),
        ("EstadoSolicitud.Rechazada", "Vom Unternehmen abgelehnt"),
        ("EstadoSolicitud.Retirada", "Von mir zurückgezogen"),
        ("EstadoSolicitud.SinRespuesta", "Keine Antwort"),
        ("Modalidad.SinIndicar", "Nicht angegeben"),
        ("Modalidad.Presencial", "Vor Ort"),
        ("Modalidad.Hibrido", "Hybrid"),
        ("Modalidad.Remoto", "100 % remote"),
        ("TipoEvento.Nota", "Notiz"),
        ("TipoEvento.SolicitudEnviada", "Bewerbung gesendet"),
        ("TipoEvento.Respuesta", "Antwort des Unternehmens"),
        ("TipoEvento.Llamada", "Anruf / Screening"),
        ("TipoEvento.PruebaTecnica", "Technischer Test"),
        ("TipoEvento.Entrevista", "Interview"),
        ("TipoEvento.Seguimiento", "Follow-up gesendet"),
        ("TipoEvento.Oferta", "Angebot"),
        ("TipoEvento.Rechazo", "Absage"),
        ("MotivoRechazo.Salario", "Gehalt / Vorstellung"),
        ("MotivoRechazo.ExperienciaInsuficiente", "Unzureichende Erfahrung"),
        ("MotivoRechazo.SeleccionaronOtroCandidato", "Anderer Kandidat gewählt"),
        ("MotivoRechazo.CulturalFit", "Passte nicht zu Team/Kultur"),
        ("MotivoRechazo.PuestoCanceladoOPausado", "Position abgesagt oder pausiert"),
        ("MotivoRechazo.Otro", "Anderer Grund"),
        ("Origen.AplicacionDirecta", "Direktbewerbung"),
        ("Origen.RecruiterMeContacto", "Recruiter kontaktierte mich"),
        ("Origen.Referido", "Empfehlung"),
        ("Origen.Networking", "Networking / Veranstaltung"),
        ("Origen.FeriaDeEmpleo", "Jobmesse"),
        ("Origen.Otro", "Sonstiges"));

    private static readonly Dictionary<Idioma, IReadOnlyDictionary<string, string>> Tablas = new()
    {
        [Idioma.Castellano] = TextoCastellano,
        [Idioma.Ingles] = TextoIngles,
        [Idioma.Aleman] = TextoAleman,
    };

    private static ResourceDictionary? diccionarioDeTextos;

    /// <summary>Gets idioma activo en este momento. Se decide al arrancar y cambia con el selector.</summary>
    public static Idioma IdiomaActual { get; private set; } = Idioma.Castellano;

    /// <summary>Gets cultura correspondiente al idioma activo.</summary>
    public static CultureInfo CulturaActual => Cultura(IdiomaActual);

    /// <summary>Cultura (fechas, números) que corresponde a cada idioma.</summary>
    public static CultureInfo Cultura(Idioma idioma) => idioma switch
    {
        Idioma.Ingles => CultureInfo.GetCultureInfo("en-US"),
        Idioma.Aleman => CultureInfo.GetCultureInfo("de-DE"),
        _ => CultureInfo.GetCultureInfo("es-ES"),
    };

    /// <summary>Traduce una clave con el idioma activo. Si falta, cae al castellano y, en último caso, a la propia clave.</summary>
    public static string Texto(string clave) => Texto(IdiomaActual, clave);

    /// <summary>Traduce una clave a un idioma concreto, sin tocar el idioma activo.</summary>
    public static string Texto(Idioma idioma, string clave)
    {
        if (Tablas[idioma].TryGetValue(clave, out string? traducido))
        {
            return traducido;
        }

        return TextoCastellano.TryGetValue(clave, out string? espanol) ? espanol : clave;
    }

    /// <summary>Indica si una clave existe en las traducciones.</summary>
    public static bool Contiene(string clave) =>
        Tablas[IdiomaActual].ContainsKey(clave) || TextoCastellano.ContainsKey(clave);

    /// <summary>Las tres opciones del selector de idioma, con su nombre en cada lengua.</summary>
    public static IReadOnlyList<IdiomaItem> IdiomasDisponibles() =>
        new[] { Idioma.Castellano, Idioma.Ingles, Idioma.Aleman }
            .Select(i => new IdiomaItem(i, NombrePropio(i)))
            .ToList();

    /// <summary>Todas las claves conocidas, para comprobar que ningún idioma se queda atrás.</summary>
    public static IReadOnlyCollection<string> Claves() => TextoCastellano.Keys.ToArray();

    /// <summary>
    /// Decide el idioma inicial (preferencia guardada o, si no hay, el del sistema), fija la
    /// cultura de la aplicación e inyecta los textos como recursos de aplicación. Solo una vez,
    /// en el arranque, antes de crear la ventana.
    /// </summary>
    public static void Inicializar()
    {
        IdiomaActual = CargarPreferenciaODetectar();
        AplicarCulturaDelHilo();
        ConfigurarLenguajeInicial();
        AplicarRecursosDeApplication();
    }

    /// <summary>Cambia el idioma al instante: cultura, textos de la interfaz y aviso a los suscriptores.</summary>
    public static void Cambiar(Idioma idioma)
    {
        if (idioma == IdiomaActual)
        {
            return;
        }

        IdiomaActual = idioma;
        GuardarPreferencia(idioma);
        AplicarCulturaDelHilo();
        ActualizarLenguajeDeLasVentanas();
        AplicarRecursosDeApplication();
        IdiomaCambiado?.Invoke(null, idioma);
    }

    /// <summary>
    /// Reconstruye el diccionario de textos inyectado en Application.Resources a partir de los
    /// diccionarios C#. Los {DynamicResource} resuelven contra él y se actualizan al cambiarlo.
    /// </summary>
    public static void AplicarRecursosDeApplication()
    {
        ResourceDictionary? recursos = Application.Current?.Resources;
        if (recursos is null)
        {
            return;
        }

        if (diccionarioDeTextos is not null)
        {
            recursos.MergedDictionaries.Remove(diccionarioDeTextos);
        }

        var diccionario = new ResourceDictionary();
        foreach (KeyValuePair<string, string> par in TextoCastellano)
        {
            diccionario[par.Key] = par.Value;
        }

        if (IdiomaActual != Idioma.Castellano)
        {
            foreach (KeyValuePair<string, string> par in Tablas[IdiomaActual])
            {
                diccionario[par.Key] = par.Value;
            }
        }

        diccionarioDeTextos = diccionario;
        recursos.MergedDictionaries.Add(diccionario);
    }

    /// <summary>Nombre con el que cada idioma se nombra a sí mismo, fijo sea cual sea el activo.</summary>
    private static string NombrePropio(Idioma idioma) => idioma switch
    {
        Idioma.Ingles => "English",
        Idioma.Aleman => "Deutsch",
        _ => "Español",
    };

    private static void AplicarCulturaDelHilo()
    {
        CultureInfo cultura = CulturaActual;
        CultureInfo.DefaultThreadCurrentCulture = cultura;
        CultureInfo.DefaultThreadCurrentUICulture = cultura;
        Thread.CurrentThread.CurrentCulture = cultura;
        Thread.CurrentThread.CurrentUICulture = cultura;
    }

    /// <summary>Fija el lenguaje por defecto de los FrameworkElement. Solo puede llamarse una vez por tipo.</summary>
    private static void ConfigurarLenguajeInicial()
    {
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CulturaActual.IetfLanguageTag)));
    }

    /// <summary>Propaga el nuevo lenguaje a las ventanas abiertas (afecta a DatePicker y formatos).</summary>
    private static void ActualizarLenguajeDeLasVentanas()
    {
        if (Application.Current is null)
        {
            return;
        }

        string etiqueta = CulturaActual.IetfLanguageTag;
        foreach (Window ventana in Application.Current.Windows)
        {
            ventana.Language = XmlLanguage.GetLanguage(etiqueta);
        }
    }

    /// <summary>Preferencia guardada en %APPDATA%\GestorSolicitudes\idioma.txt, o el idioma del sistema.</summary>
    private static Idioma CargarPreferenciaODetectar()
    {
        try
        {
            if (File.Exists(RutaPreferencia))
            {
                string contenido = File.ReadAllText(RutaPreferencia).Trim();
                if (Enum.TryParse(contenido, out Idioma guardado))
                {
                    return guardado;
                }
            }
        }
        catch
        {
            // Si no se puede leer la preferencia, se cae al idioma del sistema.
        }

        return CultureInfo.CurrentCulture.TwoLetterISOLanguageName switch
        {
            "en" => Idioma.Ingles,
            "de" => Idioma.Aleman,
            _ => Idioma.Castellano,
        };
    }

    private static void GuardarPreferencia(Idioma idioma)
    {
        try
        {
            string carpeta = Path.GetDirectoryName(RutaPreferencia)!;
            Directory.CreateDirectory(carpeta);
            File.WriteAllText(RutaPreferencia, idioma.ToString());
        }
        catch
        {
            // Un fallo al guardar la preferencia no debe impedir el cambio de idioma.
        }
    }

    private static string RutaPreferencia => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GestorSolicitudes",
        "idioma.txt");

    private static IReadOnlyDictionary<string, string> Listar(params (string Clave, string Texto)[] pares) =>
        pares.ToDictionary(p => p.Clave, p => p.Texto, StringComparer.Ordinal);
}