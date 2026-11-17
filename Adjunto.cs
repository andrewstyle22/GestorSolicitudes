namespace GestorSolicitudes.ViewModels;

using GestorSolicitudes.Models;

/// <summary>
/// Un adjunto del panel de detalle: cuál de los dos documentos es y el nombre que se pinta
/// (el original, o el texto de "sin adjuntar"). Es lo que recibe la fila y el comando de abrir.
/// </summary>
public sealed record Adjunto(TipoAdjunto Tipo, string? Nombre);
