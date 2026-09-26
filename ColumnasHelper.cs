namespace GestorSolicitudes.Helpers;

using System.IO;

/// <summary>
/// Recuerda qué columnas de la tabla están visibles en %APPDATA%\GestorSolicitudes\columnas.txt,
/// una línea por clave visible. Mismo patrón que la preferencia de idioma: un fichero de
/// texto que se lee al arrancar y se ignora en silencio si no se puede leer o escribir.
/// Solo se guardan las visibles, así que un fichero con líneas desconocidas no rompe nada.
/// </summary>
public static class ColumnasHelper
{
    /// <summary>Ruta del fichero de preferencia; los tests usan las sobrecargas con ruta.</summary>
    public static string Ruta { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GestorSolicitudes",
        "columnas.txt");

    /// <summary>Claves guardadas, o null si no hay fichero (todas visibles por defecto).</summary>
    public static HashSet<string>? Cargar() => CargarDe(Ruta);

    /// <summary>Igual que <see cref="Cargar"/> pero sobre una ruta concreta (tests).</summary>
    internal static HashSet<string>? CargarDe(string ruta)
    {
        try
        {
            if (!File.Exists(ruta))
            {
                return null;
            }

            return File.ReadAllLines(ruta)
                .Select(linea => linea.Trim())
                .Where(linea => linea.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            // Si no se puede leer la preferencia, se cae al comportamiento por defecto.
            return null;
        }
    }

    /// <summary>Guarda las claves visibles. Los errores se ignoran.</summary>
    public static void Guardar(IEnumerable<string> claves) => GuardarEn(Ruta, claves);

    /// <summary>Igual que <see cref="Guardar"/> pero sobre una ruta concreta (tests).</summary>
    internal static void GuardarEn(string ruta, IEnumerable<string> claves)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
            File.WriteAllLines(ruta, claves);
        }
        catch
        {
            // Un fallo al guardar la preferencia no debe impedir cambiar columnas.
        }
    }
}
