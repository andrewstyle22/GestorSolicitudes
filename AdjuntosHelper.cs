using System.IO;

namespace GestorSolicitudes.Helpers;

/// <summary>
/// Copia y borra los adjuntos (CV, carta) en %APPDATA%\GestorSolicitudes\adjuntos.
/// Se copian al adjuntar para que sobrevivan aunque se mueva o borre el original,
/// y solo se borran ficheros que están dentro de esta carpeta: jamás uno arbitrario.
/// </summary>
public static class AdjuntosHelper
{
    public static string Carpeta { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GestorSolicitudes",
        "adjuntos");

    /// <summary>Copia el fichero elegido a la carpeta de adjuntos y devuelve su nueva ruta.</summary>
    public static string Copiar(string origen, string etiqueta)
    {
        Directory.CreateDirectory(Carpeta);

        string extension = Path.GetExtension(origen);
        string nombre = $"{etiqueta}-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}{extension}";
        string destino = Path.Combine(Carpeta, nombre);

        File.Copy(origen, destino);
        return destino;
    }

    /// <summary>Borra un adjunto solo si es de nuestra carpeta. Los errores se ignoran.</summary>
    public static void Eliminar(string? ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta)) return;

        bool esNuestraCarpeta = ruta.StartsWith(
            Carpeta + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);

        if (!esNuestraCarpeta) return;

        try
        {
            if (File.Exists(ruta)) File.Delete(ruta);
        }
        catch
        {
            // Dejar un fichero huérfano es mejor que romper la aplicación.
        }
    }
}