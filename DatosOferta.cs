namespace GestorSolicitudes.Models;

/// <summary>
/// Datos de la oferta que se editan juntos: enlace, portal, origen, ubicación,
/// modalidad, tecnologías y requisitos.
/// </summary>
public sealed record DatosOferta(
    string? EnlaceOferta,
    string? Portal,
    Origen Origen,
    string? Ubicacion,
    Modalidad Modalidad,
    string? Tecnologias,
    string? Requisitos);
