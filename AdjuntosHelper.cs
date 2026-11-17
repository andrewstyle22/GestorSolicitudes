namespace GestorSolicitudes.Helpers;

using System.IO;

/// <summary>
/// Constante de tamaño de los documentos y carpeta donde vivían los adjuntos copiados en disco.
/// La carpeta solo la usa la migración a la base de datos: ya no se copian ficheros ahí.
/// </summary>
public static class AdjuntosHelper
{
    /// <summary>Peso máximo de un adjunto (2 MB). El editor lo comprueba antes de leer.</summary>
    public const int TamanoMaximoBytes = 2 * 1024 * 1024;

    public static string Carpeta { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GestorSolicitudes",
        "adjuntos");
}
