namespace GestorSolicitudes.Models;

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

/// <summary>
/// Una candidatura enviada a una oferta concreta.
/// </summary>
public class Solicitud
{
    public int Id { get; set; }

    // --- Identificación de la oferta ---
    [MaxLength(150)]
    public string Empresa { get; set; } = string.Empty;

    [MaxLength(150)]
    public string Puesto { get; set; } = string.Empty;

    /// <summary>Gets or sets enlace directo a la oferta (LinkedIn, InfoJobs, web corporativa...).</summary>
    [MaxLength(500)]
    public string? EnlaceOferta { get; set; }

    /// <summary>Gets or sets dónde se encontró la oferta: LinkedIn, InfoJobs, Tecnoempleo, referido, etc.</summary>
    [MaxLength(80)]
    public string? Portal { get; set; }

    /// <summary>Gets or sets cómo se originó el contacto (distinto de dónde estaba publicada la oferta).</summary>
    public Origen Origen { get; set; } = Origen.AplicacionDirecta;

    [MaxLength(120)]
    public string? Ubicacion { get; set; }

    public Modalidad Modalidad { get; set; } = Modalidad.SinIndicar;

    /// <summary>Gets or sets stack pedido en la oferta, separado por comas.</summary>
    [MaxLength(300)]
    public string? Tecnologias { get; set; }

    /// <summary>Gets or sets requisitos, obligaciones y responsabilidades del puesto, tal como los publicó la empresa.</summary>
    [MaxLength(4000)]
    public string? Requisitos { get; set; }

    // --- Económico (euros brutos anuales) ---
    public int? SalarioMin { get; set; }

    public int? SalarioMax { get; set; }

    public int? PretensionSalarial { get; set; }

    // --- Fechas clave ---
    public DateTime FechaSolicitud { get; set; } = DateTime.Today;

    public DateTime? FechaPrimeraRespuesta { get; set; }

    public DateTime? FechaEntrevista { get; set; }

    public DateTime? FechaCierre { get; set; }

    /// <summary>Gets or sets fecha en la que toca volver a escribir si no hay noticias.</summary>
    public DateTime? ProximoSeguimiento { get; set; }

    // --- Seguimiento ---
    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Enviada;

    /// <summary>Gets or sets por qué se cerró en falso, cuando <see cref="Estado"/> es un rechazo.</summary>
    public MotivoRechazo? MotivoRechazo { get; set; }

    /// <summary>Gets or sets texto literal o resumen de lo que contestó la empresa.</summary>
    public string? RespuestaEmpresa { get; set; }

    [MaxLength(120)]
    public string? ContactoNombre { get; set; }

    [MaxLength(150)]
    public string? ContactoEmail { get; set; }

    /// <summary>Gets or sets teléfono de la persona de contacto (recruiter, hiring manager...).</summary>
    [MaxLength(40)]
    public string? ContactoTelefono { get; set; }

    /// <summary>Gets or sets interés propio en la oferta, de 1 a 5.</summary>
    public int Interes { get; set; } = 3;

    public string? Notas { get; set; }

    // --- Adjuntos (copias en %APPDATA%\GestorSolicitudes\adjuntos) ---
    [MaxLength(500)]
    public string? RutaCv { get; set; }

    [MaxLength(500)]
    public string? RutaCarta { get; set; }

    /// <summary>Gets or sets nombre "bonito" del CV tal como lo eligió el usuario (el fichero en disco es un GUID).</summary>
    [MaxLength(260)]
    public string? NombreOriginalCv { get; set; }

    /// <summary>Gets or sets nombre "bonito" de la carta tal como la eligió el usuario.</summary>
    [MaxLength(260)]
    public string? NombreOriginalCarta { get; set; }

    /// <summary>Gets or sets historial cronológico: cada llamada, prueba o entrevista.</summary>
    public ObservableCollection<Evento> Eventos { get; set; } = new();

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
    public bool EstaAbierta => this.Estado is not (EstadoSolicitud.OfertaAceptada
        or EstadoSolicitud.OfertaRechazada
        or EstadoSolicitud.Rechazada
        or EstadoSolicitud.Retirada);

    [NotMapped]
    public bool HuboRespuesta => this.FechaPrimeraRespuesta.HasValue;

    /// <summary>Gets a value indicating whether ya tiene fila en la base de datos (frente a un borrador recién creado con "Nueva").</summary>
    [NotMapped]
    public bool EstaGuardada => this.Id != 0;

    [NotMapped]
    public bool SeguimientoPendiente => this.EstaAbierta
        && this.ProximoSeguimiento.HasValue
        && this.ProximoSeguimiento.Value.Date <= DateTime.Today;

    [NotMapped]
    public string RangoSalarial => (this.SalarioMin, this.SalarioMax) switch
    {
        (null, null) => "—",
        (int min, null) => $"desde {min:N0} €",
        (null, int max) => $"hasta {max:N0} €",
        (int min, int max) => $"{min:N0} – {max:N0} €",
    };

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
