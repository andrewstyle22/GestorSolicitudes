namespace GestorSolicitudes.ViewModels;

/// <summary>
/// Un adjunto del panel de detalle: la ruta del fichero y el nombre que se pinta (el original,
/// o el texto de "sin adjuntar"). Es lo que recibe la fila y el comando de abrir.
/// </summary>
public sealed record Adjunto(string? Ruta, string? Nombre);
