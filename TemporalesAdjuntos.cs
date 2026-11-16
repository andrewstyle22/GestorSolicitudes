namespace GestorSolicitudes.Helpers;

using System.Diagnostics;
using System.IO;

/// <summary>
/// Escribe un documento en una subcarpeta temporal única para abrirlo en su visor sin tocar
/// los ficheros del usuario. La carpeta se borra al cerrar y al arrancar la aplicación.
/// </summary>
public static class TemporalesAdjuntos
{
    /// <summary>Carpeta de temporales; los tests pasan una propia.</summary>
    public static string Carpeta { get; } = Path.Combine(
        Path.GetTempPath(),
        "GestorSolicitudes",
        "adjuntos");

    /// <summary>
    /// Guarda el contenido con el nombre saneado dentro de una subcarpeta nueva y devuelve su
    /// ruta. Así dos documentos con el mismo nombre no se pisan.
    /// </summary>
    public static string Escribir(string nombre, byte[] contenido, string? carpeta = null)
    {
        string destino = Path.Combine(carpeta ?? Carpeta, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(destino);
        string ruta = Path.Combine(destino, NombreSeguro(nombre));
        File.WriteAllBytes(ruta, contenido);
        return ruta;
    }

    /// <summary>Borra la carpeta de temporales con su contenido. Un fallo no rompe el cierre.</summary>
    public static void Limpiar(string? carpeta = null)
    {
        string destino = carpeta ?? Carpeta;
        try
        {
            if (Directory.Exists(destino))
            {
                Directory.Delete(destino, recursive: true);
            }
        }
        catch (Exception ex)
        {
            // Dejar un temporal huérfano es mejor que tumbar la aplicación al cerrarse.
            Debug.WriteLine($"No se pudo limpiar '{destino}': {ex}");
        }
    }

    private static string NombreSeguro(string nombre)
    {
        // El nombre viene del fichero que eligió el usuario: se descarta cualquier ruta y se
        // sustituyen los caracteres inválidos para no salir de la subcarpeta temporal.
        char[] invalidos = Path.GetInvalidFileNameChars();
        string limpio = new(
            Path.GetFileName(nombre).Select(c => invalidos.Contains(c) ? '_' : c).ToArray());
        limpio = limpio.Trim();
        return limpio is "" or "." or ".." ? "documento" : limpio;
    }
}
