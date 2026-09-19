using System.IO;

namespace GestorSolicitudes.Helpers;

/// <summary>
/// Copia y borra los adjuntos (CV, carta) en %APPDATA%\GestorSolicitudes\adjuntos.
/// Se copian al adjuntar para que sobrevivan aunque se mueva o borre el original,
/// y solo se borran ficheros que están dentro de esta carpeta: jamás uno arbitrario.
/// El nombre en disco es solo un GUID (evita colisiones y problemas con caracteres raros);
/// el nombre "bonito" que ve el usuario se guarda aparte, en Solicitud.NombreOriginalCv/Carta.
/// </summary>
public static class AdjuntosHelper
{
    public static string Carpeta { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GestorSolicitudes",
        "adjuntos");

    /// <summary>Copia el fichero elegido a la carpeta de adjuntos y devuelve su nueva ruta.</summary>
    public static string Copiar(string origen, string etiqueta) => CopiarEn(origen, etiqueta, Carpeta);

    /// <summary>Copia a una carpeta concreta (sobrecarga interna usada por los tests).</summary>
    internal static string CopiarEn(string origen, string etiqueta, string carpeta)
    {
        Directory.CreateDirectory(carpeta);

        string extension = Path.GetExtension(origen);
        string nombre = $"{etiqueta}-{Guid.NewGuid():N}{extension}";
        string destino = Path.Combine(carpeta, nombre);

        File.Copy(origen, destino);
        return destino;
    }

    /// <summary>Borra un adjunto solo si es de nuestra carpeta. Los errores se ignoran.</summary>
    public static void Eliminar(string? ruta) => EliminarDe(ruta, Carpeta);

    /// <summary>Borra de una carpeta concreta (sobrecarga interna usada por los tests).</summary>
    internal static void EliminarDe(string? ruta, string carpeta)
    {
        if (string.IsNullOrWhiteSpace(ruta)) return;

        bool esNuestraCarpeta = ruta.StartsWith(
            carpeta + Path.DirectorySeparatorChar,
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
