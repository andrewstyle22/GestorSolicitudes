using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace GestorSolicitudes.Models;

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

    /// <summary>Enlace directo a la oferta (LinkedIn, InfoJobs, web corporativa...).</summary>
    [MaxLength(500)]
    public string? EnlaceOferta { get; set; }

    /// <summary>Dónde se encontró la oferta: LinkedIn, InfoJobs, Tecnoempleo, referido, etc.</summary>
    [MaxLength(80)]
    public string? Portal { get; set; }

    [MaxLength(120)]
    public string? Ubicacion { get; set; }

    public Modalidad Modalidad { get; set; } = Modalidad.SinIndicar;

    /// <summary>Stack pedido en la oferta, separado por comas.</summary>
    [MaxLength(300)]
    public string? Tecnologias { get; set; }

    /// <summary>Requisitos, obligaciones y responsabilidades del puesto, tal como los publicó la empresa.</summary>
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

    /// <summary>Fecha en la que toca volver a escribir si no hay noticias.</summary>
    public DateTime? ProximoSeguimiento { get; set; }

    // --- Seguimiento ---
    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Enviada;

    /// <summary>Texto literal o resumen de lo que contestó la empresa.</summary>
    public string? RespuestaEmpresa { get; set; }

    [MaxLength(120)]
    public string? ContactoNombre { get; set; }

    [MaxLength(150)]
    public string? ContactoEmail { get; set; }

    /// <summary>Interés propio en la oferta, de 1 a 5.</summary>
    public int Interes { get; set; } = 3;

    public string? Notas { get; set; }

    // --- Adjuntos (copias en %APPDATA%\GestorSolicitudes\adjuntos) ---
    [MaxLength(500)]
    public string? RutaCv { get; set; }

    [MaxLength(500)]
    public string? RutaCarta { get; set; }

    /// <summary>Nombre "bonito" del CV tal como lo eligió el usuario (el fichero en disco es un GUID).</summary>
    [MaxLength(260)]
    public string? NombreOriginalCv { get; set; }

    /// <summary>Nombre "bonito" de la carta tal como la eligió el usuario.</summary>
    [MaxLength(260)]
    public string? NombreOriginalCarta { get; set; }

    /// <summary>Historial cronológico: cada llamada, prueba o entrevista.</summary>
    public ObservableCollection<Evento> Eventos { get; set; } = new();

    // --- Calculadas (no se guardan en base de datos) ---

    [NotMapped]
    public int DiasDesdeSolicitud => (int)(DateTime.Today - FechaSolicitud.Date).TotalDays;

    [NotMapped]
    public int? DiasHastaRespuesta => FechaPrimeraRespuesta.HasValue
        ? (int)(FechaPrimeraRespuesta.Value.Date - FechaSolicitud.Date).TotalDays
        : null;

    [NotMapped]
    public bool EstaAbierta => Estado is not (EstadoSolicitud.OfertaAceptada
        or EstadoSolicitud.OfertaRechazada
        or EstadoSolicitud.Rechazada
        or EstadoSolicitud.Retirada);

    [NotMapped]
    public bool HuboRespuesta => FechaPrimeraRespuesta.HasValue;

    /// <summary>Ya tiene fila en la base de datos (frente a un borrador recién creado con "Nueva").</summary>
    [NotMapped]
    public bool EstaGuardada => Id != 0;

    [NotMapped]
    public bool SeguimientoPendiente => EstaAbierta
        && ProximoSeguimiento.HasValue
        && ProximoSeguimiento.Value.Date <= DateTime.Today;

    [NotMapped]
    public string RangoSalarial => (SalarioMin, SalarioMax) switch
    {
        (null, null) => "—",
        (int min, null) => $"desde {min:N0} €",
        (null, int max) => $"hasta {max:N0} €",
        (int min, int max) => $"{min:N0} – {max:N0} €"
    };

    /// <summary>Nombre a mostrar del CV: el original si se conserva, o el del disco si no.</summary>
    [NotMapped]
    public string? NombreCv => NombreOriginalCv ?? Archivo(RutaCv);

    /// <summary>Nombre a mostrar de la carta: el original si se conserva, o el del disco si no.</summary>
    [NotMapped]
    public string? NombreCarta => NombreOriginalCarta ?? Archivo(RutaCarta);

    private static string? Archivo(string? ruta) =>
        string.IsNullOrWhiteSpace(ruta) ? null : Path.GetFileName(ruta);

    public override string ToString() => $"{Empresa} — {Puesto}";
}
