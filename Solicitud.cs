namespace GestorSolicitudes.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

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
    /// el proceso, el enlace, los requisitos y los adjuntos.
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

        return copia;
    }

    public int Id { get; set; }

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
    public int? SalarioMin { get; set; }

    public int? SalarioMax { get; set; }

    public int? PretensionSalarial { get; set; }

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
    public DateTime FechaSolicitud { get; set; } = DateTime.Today;

    public DateTime? FechaPrimeraRespuesta { get; set; }

    public DateTime? FechaEntrevista { get; set; }

    public DateTime? FechaCierre { get; set; }

    /// <summary>Gets or sets fecha en la que toca volver a escribir si no hay noticias.</summary>
    public DateTime? ProximoSeguimiento { get; set; }

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
            this.ProximoSeguimiento = this.FechaSolicitud.AddDays(7);
        }
    }

    // --- Seguimiento ---
    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Enviada;

    /// <summary>Gets or sets por qué se cerró en falso, cuando <see cref="Estado"/> es un rechazo.</summary>
    public MotivoRechazo? MotivoRechazo { get; set; }

    /// <summary>
    /// Cambia el estado de la solicitud junto con su motivo de rechazo y su fecha de cierre.
    /// Todavía no valida transiciones: eso llega con las reglas de estado.
    /// </summary>
    public void CambiarEstado(EstadoSolicitud estado, MotivoRechazo? motivo, DateTime? fechaCierre)
    {
        this.Estado = estado;
        this.MotivoRechazo = motivo;
        this.FechaCierre = fechaCierre;
    }

    /// <summary>Gets or sets texto literal o resumen de lo que contestó la empresa.</summary>
    public string? RespuestaEmpresa { get; set; }

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
    public string? ContactoNombre { get; set; }

    [MaxLength(LongitudMaximaContactoEmail)]
    public string? ContactoEmail { get; set; }

    /// <summary>Gets or sets teléfono de la persona de contacto (recruiter, hiring manager...).</summary>
    [MaxLength(LongitudMaximaContactoTelefono)]
    public string? ContactoTelefono { get; set; }

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
    public int Interes { get; set; } = 3;

    public string? Notas { get; set; }

    /// <summary>Actualiza las notas: recorta espacios y no impone límite de longitud.</summary>
    public void ActualizarNotas(string? notas) => this.Notas = notas?.Trim();

    // --- Adjuntos (copias en %APPDATA%\GestorSolicitudes\adjuntos) ---
    [MaxLength(LongitudMaximaRutaAdjunto)]
    public string? RutaCv { get; set; }

    [MaxLength(LongitudMaximaRutaAdjunto)]
    public string? RutaCarta { get; set; }

    /// <summary>Gets or sets nombre "bonito" del CV tal como lo eligió el usuario (el fichero en disco es un GUID).</summary>
    [MaxLength(LongitudMaximaNombreOriginalAdjunto)]
    public string? NombreOriginalCv { get; set; }

    /// <summary>Gets or sets nombre "bonito" de la carta tal como la eligió el usuario.</summary>
    [MaxLength(LongitudMaximaNombreOriginalAdjunto)]
    public string? NombreOriginalCarta { get; set; }

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
    /// Gets días desde el envío mientras el proceso sigue abierto; una vez cerrado,
    /// en vez de seguir contando hasta hoy (lo que ya no dice nada), muestra cuánto
    /// duró en total hasta <see cref="FechaCierre"/>.
    /// </summary>
    [NotMapped]
    public int DiasProceso => this.EstaAbierta
        ? this.DiasDesdeSolicitud
        : (int)((this.FechaCierre ?? DateTime.Today).Date - this.FechaSolicitud.Date).TotalDays;

    [NotMapped]
    public int? DiasHastaRespuesta => this.FechaPrimeraRespuesta.HasValue
        ? (int)(this.FechaPrimeraRespuesta.Value.Date - this.FechaSolicitud.Date).TotalDays
        : null;

    [NotMapped]
    public bool EstaAbierta => !this.Estado.EstaCerrada();

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
