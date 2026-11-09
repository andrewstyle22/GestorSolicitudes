namespace GestorSolicitudes.Models;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Reflection;

/// <summary>
/// Una candidatura enviada a una oferta concreta.
/// </summary>
public class Solicitud
{
    public const int LongitudMaximaIdentificacion = 150;
    public const int LongitudMaximaEnlace = 500;
    public const int LongitudMaximaPortal = 80;
    public const int LongitudMaximaUbicacion = 120;
    public const int LongitudMaximaTecnologias = 300;
    public const int LongitudMaximaRequisitos = 4000;
    public const int LongitudMaximaContactoNombre = 120;
    public const int LongitudMaximaContactoEmail = 150;
    public const int LongitudMaximaContactoTelefono = 40;
    public const int LongitudMaximaNotaReapertura = 300;

    private const int LongitudMaximaRutaAdjunto = 500;
    private const int LongitudMaximaNombreOriginalAdjunto = 260;

    private Solicitud()
    {
    }

    public Solicitud(string empresa, string puesto)
    {
        this.Empresa = empresa;
        this.Puesto = puesto;
    }

    /// <summary>
    /// Crea una candidatura nueva a partir de esta: copia lo que describe la oferta
    /// (portal, ubicación, modalidad, tecnologías, salario e interés) y deja en blanco
    /// el proceso, el enlace, los requisitos y los adjuntos. Si esta candidatura ya está
    /// guardada, conserva su Id como origen de la copia.
    /// </summary>
    public Solicitud CrearCopia()
    {
        var copia = new Solicitud(this.Empresa, this.Puesto);

        copia.ActualizarOferta(new DatosOferta(
            EnlaceOferta: null,
            Portal: this.Portal,
            Origen: Origen.AplicacionDirecta,
            Ubicacion: this.Ubicacion,
            Modalidad: this.Modalidad,
            Tecnologias: this.Tecnologias,
            Requisitos: null));
        copia.ActualizarCondiciones(this.SalarioMin, this.SalarioMax, this.PretensionSalarial, this.Interes);
        copia.SolicitudAnteriorId = this.Id == 0 ? null : this.Id;

        return copia;
    }

    // Sin setter: EF escribe el campo de respaldo al insertar (ver AppDbContext).
    public int Id { get; }

    /// <summary>Gets la candidatura de la que salió esta, cuando Duplicar abre un proceso nuevo.</summary>
    public int? SolicitudAnteriorId { get; private set; }

    // --- Identificación de la oferta ---
    [MaxLength(LongitudMaximaIdentificacion)]
    public string Empresa { get; private set; } = string.Empty;

    [MaxLength(LongitudMaximaIdentificacion)]
    public string Puesto { get; private set; } = string.Empty;

    /// <summary>Gets enlace directo a la oferta (LinkedIn, InfoJobs, web corporativa...).</summary>
    [MaxLength(LongitudMaximaEnlace)]
    public string? EnlaceOferta { get; private set; }

    /// <summary>Gets dónde se encontró la oferta: LinkedIn, InfoJobs, Tecnoempleo, referido, etc.</summary>
    [MaxLength(LongitudMaximaPortal)]
    public string? Portal { get; private set; }

    /// <summary>Gets cómo se originó el contacto (distinto de dónde estaba publicada la oferta).</summary>
    public Origen Origen { get; private set; } = Origen.AplicacionDirecta;

    [MaxLength(LongitudMaximaUbicacion)]
    public string? Ubicacion { get; private set; }

    public Modalidad Modalidad { get; private set; } = Modalidad.SinIndicar;

    /// <summary>Gets stack pedido en la oferta, separado por comas.</summary>
    [MaxLength(LongitudMaximaTecnologias)]
    public string? Tecnologias { get; private set; }

    /// <summary>Gets requisitos, obligaciones y responsabilidades del puesto, tal como los publicó la empresa.</summary>
    [MaxLength(LongitudMaximaRequisitos)]
    public string? Requisitos { get; private set; }

    /// <summary>
    /// Actualiza los datos de la oferta: recorta espacios y exige que los textos
    /// no superen la longitud que admite la base de datos.
    /// </summary>
    public void ActualizarOferta(DatosOferta datos)
    {
        this.EnlaceOferta = Recortar(datos.EnlaceOferta, LongitudMaximaEnlace, "El enlace de la oferta");
        this.Portal = Recortar(datos.Portal, LongitudMaximaPortal, "El portal");
        this.Origen = datos.Origen;
        this.Ubicacion = Recortar(datos.Ubicacion, LongitudMaximaUbicacion, "La ubicación");
        this.Modalidad = datos.Modalidad;
        this.Tecnologias = Recortar(datos.Tecnologias, LongitudMaximaTecnologias, "Las tecnologías");
        this.Requisitos = Recortar(datos.Requisitos, LongitudMaximaRequisitos, "Los requisitos");
    }

    private static string? Recortar(string? valor, int maximo, string campo)
    {
        string? recortado = valor?.Trim();

        if (recortado?.Length > maximo)
        {
            throw new ArgumentException($"{campo} no puede superar los {maximo} caracteres.");
        }

        return recortado;
    }

    /// <summary>
    /// Actualiza la empresa y el puesto: recorta espacios y exige que no queden vacíos
    /// ni superen la longitud que admite la base de datos.
    /// </summary>
    public void ActualizarIdentificacion(string? empresa, string? puesto)
    {
        string empresaRecortada = (empresa ?? string.Empty).Trim();
        string puestoRecortado = (puesto ?? string.Empty).Trim();

        if (empresaRecortada.Length == 0 || puestoRecortado.Length == 0)
        {
            throw new ArgumentException("La empresa y el puesto son obligatorios.");
        }

        if (empresaRecortada.Length > LongitudMaximaIdentificacion
            || puestoRecortado.Length > LongitudMaximaIdentificacion)
        {
            throw new ArgumentException(
                $"La empresa y el puesto no pueden superar los {LongitudMaximaIdentificacion} caracteres.");
        }

        this.Empresa = empresaRecortada;
        this.Puesto = puestoRecortado;
    }

    // --- Económico (euros brutos anuales) ---
    public int? SalarioMin { get; private set; }

    public int? SalarioMax { get; private set; }

    public int? PretensionSalarial { get; private set; }

    /// <summary>
    /// Actualiza la parte económica y el interés: el salario mínimo no puede superar
    /// al máximo y el interés debe estar entre 1 y 5.
    /// </summary>
    public void ActualizarCondiciones(int? salarioMin, int? salarioMax, int? pretensionSalarial, int interes)
    {
        if (salarioMin > salarioMax)
        {
            throw new ArgumentException("El salario mínimo no puede superar al máximo.");
        }

        if (interes is < 1 or > 5)
        {
            throw new ArgumentException("El interés debe estar entre 1 y 5.");
        }

        this.SalarioMin = salarioMin;
        this.SalarioMax = salarioMax;
        this.PretensionSalarial = pretensionSalarial;
        this.Interes = interes;
    }

    // --- Fechas clave ---
    private DateTime? fechaReapertura = null;

    public DateTime FechaSolicitud { get; private set; } = DateTime.Today;

    public DateTime? FechaPrimeraRespuesta { get; private set; }

    public DateTime? FechaEntrevista { get; private set; }

    public DateTime? FechaCierre { get; private set; }

    public DateTime? FechaReapertura => this.fechaReapertura;

    /// <summary>Gets or sets fecha en la que toca volver a escribir si no hay noticias.</summary>
    public DateTime? ProximoSeguimiento { get; private set; }

    /// <summary>
    /// Actualiza las fechas que se editan a mano: envío, entrevista y próximo seguimiento.
    /// </summary>
    public void ActualizarFechas(DateTime fechaSolicitud, DateTime? fechaEntrevista, DateTime? proximoSeguimiento)
    {
        this.FechaSolicitud = fechaSolicitud;
        this.FechaEntrevista = fechaEntrevista;
        this.ProximoSeguimiento = proximoSeguimiento;
    }

    /// <summary>
    /// Propone un seguimiento a siete días del envío, solo si el proceso sigue abierto
    /// y todavía no hay una fecha fijada.
    /// </summary>
    public void ProponerSeguimiento()
    {
        if (this.EstaAbierta && this.ProximoSeguimiento is null)
        {
            this.ProximoSeguimiento = (this.FechaReapertura ?? this.FechaSolicitud).AddDays(7);
        }
    }

    // --- Seguimiento ---
    public EstadoSolicitud Estado { get; private set; } = EstadoSolicitud.Enviada;

    /// <summary>Gets or sets por qué se cerró en falso, cuando <see cref="Estado"/> es un rechazo.</summary>
    public MotivoRechazo? MotivoRechazo { get; private set; }

    /// <summary>
    /// Cambia el estado de la solicitud junto con su motivo de rechazo y su fecha de cierre.
    /// Exige un motivo válido en los cierres negativos, lo limpia en el resto y fija hoy
    /// como fecha de cierre si no se indica otra. Una candidatura cerrada solo se reabre
    /// con <see cref="Reabrir"/>.
    /// </summary>
    public void CambiarEstado(EstadoSolicitud estado, MotivoRechazo? motivo, DateTime? fechaCierre)
    {
        bool esTransicion = estado != this.Estado;

        if (esTransicion && this.Estado.EstaCerrada() && !estado.EstaCerrada())
        {
            throw new InvalidOperationException(
                "Una candidatura cerrada solo se reabre con Reabrir.");
        }

        if (esTransicion && estado == EstadoSolicitud.OfertaAceptada && this.Estado.EstaCerrada())
        {
            throw new InvalidOperationException(
                "Una oferta cerrada no se puede aceptar sin reabrirla a \"Oferta recibida\".");
        }

        if (estado.AdmiteMotivo())
        {
            if (motivo is not MotivoRechazo motivoValido || !estado.MotivosPermitidos().Contains(motivoValido))
            {
                throw new ArgumentException(
                    "Los cierres negativos exigen un motivo válido para el estado.",
                    nameof(motivo));
            }
        }
        else
        {
            motivo = null;
        }

        DateTime hoy = DateTime.Today;
        if (esTransicion && estado.EstaCerrada())
        {
            fechaCierre ??= hoy;
        }

        if (esTransicion && this.Estado == EstadoSolicitud.SinRespuesta)
        {
            this.FechaPrimeraRespuesta ??= hoy;
            this.AgregarEvento(new Evento(
                hoy,
                TipoEvento.Respuesta,
                $"La empresa respondió. Nuevo estado: {Texto(estado)}"));
        }

        if (esTransicion && estado == EstadoSolicitud.Rechazada && motivo is MotivoRechazo motivoCierre)
        {
            this.AgregarEvento(new Evento(
                hoy,
                TipoEvento.Rechazo,
                $"{Texto(estado)}: {Texto(motivoCierre)}"));
        }

        this.Estado = estado;
        this.MotivoRechazo = motivo;
        this.FechaCierre = fechaCierre;
    }

    /// <summary>
    /// Reabre una candidatura cerrada en un estado abierto: limpia el cierre anterior,
    /// reinicia el contador de días, conserva el seguimiento indicado o propone uno nuevo,
    /// y guarda el cierre en el historial.
    /// </summary>
    public void Reabrir(EstadoSolicitud nuevoEstado, string? nota, DateTime? proximoSeguimiento = null)
    {
        if (!this.Estado.EstaCerrada())
        {
            throw new InvalidOperationException("Solo se puede reabrir una candidatura cerrada.");
        }

        if (nuevoEstado.EstaCerrada() || nuevoEstado == EstadoSolicitud.SinRespuesta)
        {
            throw new ArgumentException(
                "La candidatura debe reabrirse a un estado abierto distinto de \"Sin respuesta\".",
                nameof(nuevoEstado));
        }

        if (string.IsNullOrWhiteSpace(nota))
        {
            throw new ArgumentException("La reapertura exige una nota.", nameof(nota));
        }

        DateTime hoy = DateTime.Today;
        string cierreAnterior = this.DescribirCierre();
        string? notaRecortada = Recortar(nota, LongitudMaximaNotaReapertura, "La nota de la reapertura");

        this.Estado = nuevoEstado;
        this.FechaCierre = null;
        this.MotivoRechazo = null;
        this.fechaReapertura = hoy;
        this.FechaPrimeraRespuesta ??= hoy;
        this.ProximoSeguimiento = proximoSeguimiento;
        this.ProponerSeguimiento();

        string descripcion = $"Reabierta. Antes: {cierreAnterior}. {notaRecortada}";

        this.AgregarEvento(new Evento(hoy, TipoEvento.Reapertura, descripcion));
    }

    private string DescribirCierre()
    {
        string estado = Texto(this.Estado);
        if (this.FechaCierre is null)
        {
            return estado;
        }

        string fecha = this.FechaCierre.Value.ToString("dd/MM", System.Globalization.CultureInfo.InvariantCulture);
        string? motivo = this.MotivoRechazo is null ? null : Texto(this.MotivoRechazo.Value);
        return motivo is null ? $"{estado} ({fecha})" : $"{estado} ({fecha}, {motivo})";
    }

    // El modelo no puede depender de Helpers: el hito guarda el cierre como texto histórico
    // con el [Description] del enum, en castellano.
    private static string Texto(Enum valor) =>
        valor.GetType().GetField(valor.ToString())?.GetCustomAttribute<DescriptionAttribute>()?.Description
        ?? valor.ToString();

    /// <summary>Gets or sets texto literal o resumen de lo que contestó la empresa.</summary>
    public string? RespuestaEmpresa { get; private set; }

    /// <summary>
    /// Registra la primera respuesta de la empresa. Fecha y texto son opcionales
    /// e independientes: puede haber respuesta escrita sin fecha y fecha sin texto.
    /// </summary>
    public void RegistrarRespuesta(DateTime? fechaPrimeraRespuesta, string? respuestaEmpresa)
    {
        this.FechaPrimeraRespuesta = fechaPrimeraRespuesta;
        this.RespuestaEmpresa = respuestaEmpresa;
    }

    [MaxLength(LongitudMaximaContactoNombre)]
    public string? ContactoNombre { get; private set; }

    [MaxLength(LongitudMaximaContactoEmail)]
    public string? ContactoEmail { get; private set; }

    /// <summary>Gets or sets teléfono de la persona de contacto (recruiter, hiring manager...).</summary>
    [MaxLength(LongitudMaximaContactoTelefono)]
    public string? ContactoTelefono { get; private set; }

    /// <summary>
    /// Actualiza la persona de contacto: recorta espacios y exige que los textos
    /// no superen la longitud que admite la base de datos.
    /// </summary>
    public void ActualizarContacto(string? nombre, string? email, string? telefono)
    {
        this.ContactoNombre = Recortar(nombre, LongitudMaximaContactoNombre, "El nombre del contacto");
        this.ContactoEmail = Recortar(email, LongitudMaximaContactoEmail, "El correo del contacto");
        this.ContactoTelefono = Recortar(telefono, LongitudMaximaContactoTelefono, "El teléfono del contacto");
    }

    /// <summary>Gets or sets interés propio en la oferta, de 1 a 5.</summary>
    public int Interes { get; private set; } = 3;

    public string? Notas { get; private set; }

    /// <summary>Actualiza las notas: recorta espacios y no impone límite de longitud.</summary>
    public void ActualizarNotas(string? notas) => this.Notas = notas?.Trim();

    // --- Adjuntos (copias en %APPDATA%\GestorSolicitudes\adjuntos) ---
    [MaxLength(LongitudMaximaRutaAdjunto)]
    public string? RutaCv { get; private set; }

    [MaxLength(LongitudMaximaRutaAdjunto)]
    public string? RutaCarta { get; private set; }

    /// <summary>Gets or sets nombre "bonito" del CV tal como lo eligió el usuario (el fichero en disco es un GUID).</summary>
    [MaxLength(LongitudMaximaNombreOriginalAdjunto)]
    public string? NombreOriginalCv { get; private set; }

    /// <summary>Gets or sets nombre "bonito" de la carta tal como la eligió el usuario.</summary>
    [MaxLength(LongitudMaximaNombreOriginalAdjunto)]
    public string? NombreOriginalCarta { get; private set; }

    /// <summary>
    /// Asigna el CV: la ruta y el nombre original que vio el usuario van juntos,
    /// y ninguno puede superar la longitud que admite la base de datos.
    /// </summary>
    public void AsignarCv(string? ruta, string? nombreOriginal)
    {
        this.RutaCv = Recortar(ruta, LongitudMaximaRutaAdjunto, "La ruta del CV");
        this.NombreOriginalCv = Recortar(nombreOriginal, LongitudMaximaNombreOriginalAdjunto, "El nombre original del CV");
    }

    /// <summary>Quita el CV: borra a la vez su ruta y su nombre original.</summary>
    public void QuitarCv()
    {
        this.RutaCv = null;
        this.NombreOriginalCv = null;
    }

    /// <summary>
    /// Asigna la carta: la ruta y el nombre original que vio el usuario van juntos,
    /// y ninguno puede superar la longitud que admite la base de datos.
    /// </summary>
    public void AsignarCarta(string? ruta, string? nombreOriginal)
    {
        this.RutaCarta = Recortar(ruta, LongitudMaximaRutaAdjunto, "La ruta de la carta");
        this.NombreOriginalCarta = Recortar(nombreOriginal, LongitudMaximaNombreOriginalAdjunto, "El nombre original de la carta");
    }

    /// <summary>Quita la carta: borra a la vez su ruta y su nombre original.</summary>
    public void QuitarCarta()
    {
        this.RutaCarta = null;
        this.NombreOriginalCarta = null;
    }

    private readonly List<Evento> eventos = [];

    /// <summary>Gets historial cronológico: cada llamada, prueba o entrevista.</summary>
    public IReadOnlyList<Evento> Eventos => this.eventos;

    /// <summary>Añade un hito al historial.</summary>
    public void AgregarEvento(Evento evento) => this.eventos.Add(evento);

    /// <summary>Quita un hito del historial.</summary>
    public void QuitarEvento(Evento evento) => this.eventos.Remove(evento);

    /// <summary>
    /// Registra el hito de envío de la solicitud, solo si el historial está vacío,
    /// para que la línea temporal no empiece en blanco.
    /// </summary>
    public void RegistrarEnvio(string descripcion)
    {
        if (this.eventos.Count == 0)
        {
            this.AgregarEvento(new Evento(this.FechaSolicitud, TipoEvento.SolicitudEnviada, descripcion));
        }
    }

    // --- Calculadas (no se guardan en base de datos) ---
    [NotMapped]
    public int DiasDesdeSolicitud => (int)(DateTime.Today - this.FechaSolicitud.Date).TotalDays;

    /// <summary>
    /// Gets días desde el envío, o desde la última reapertura, mientras el proceso sigue
    /// abierto; una vez cerrado, en vez de seguir contando hasta hoy (lo que ya no dice
    /// nada), muestra cuánto duró en total hasta <see cref="FechaCierre"/>.
    /// </summary>
    [NotMapped]
    public int DiasProceso
    {
        get
        {
            DateTime referencia = this.FechaReapertura ?? this.FechaSolicitud;
            DateTime fin = this.EstaAbierta ? DateTime.Today : this.FechaCierre ?? DateTime.Today;
            return (int)(fin.Date - referencia.Date).TotalDays;
        }
    }

    [NotMapped]
    public int? DiasHastaRespuesta => this.FechaPrimeraRespuesta.HasValue
        ? (int)(this.FechaPrimeraRespuesta.Value.Date - this.FechaSolicitud.Date).TotalDays
        : null;

    [NotMapped]
    public bool EstaAbierta => !this.Estado.EstaCerrada();

    [NotMapped]
    public bool FueReabierta => this.FechaReapertura.HasValue;

    [NotMapped]
    public bool HuboRespuesta => this.FechaPrimeraRespuesta.HasValue;

    [NotMapped]
    public bool SeguimientoPendiente => this.EstaAbierta
        && this.ProximoSeguimiento.HasValue
        && this.ProximoSeguimiento.Value.Date <= DateTime.Today;

    /// <summary>Gets nombre a mostrar del CV: el original si se conserva, o el del disco si no.</summary>
    [NotMapped]
    public string? NombreCv => this.NombreOriginalCv ?? Archivo(this.RutaCv);

    /// <summary>Gets nombre a mostrar de la carta: el original si se conserva, o el del disco si no.</summary>
    [NotMapped]
    public string? NombreCarta => this.NombreOriginalCarta ?? Archivo(this.RutaCarta);

    private static string? Archivo(string? ruta) =>
        string.IsNullOrWhiteSpace(ruta) ? null : Path.GetFileName(ruta);

    public override string ToString() => $"{this.Empresa} — {this.Puesto}";
}
