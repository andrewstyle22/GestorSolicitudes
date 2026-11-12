namespace GestorSolicitudes.Helpers;

using System.Diagnostics;
using System.IO;

/// <summary>
/// Separador del CSV recordado en %APPDATA%\GestorSolicitudes\csv.txt. Mismo patrón que
/// <see cref="PreferenciaIdioma"/>: un fichero de texto que se lee al exportar y se ignora en
/// silencio si no se puede leer o escribir. Cualquier valor que no sea un separador admitido
/// se descarta y se usa <see cref="PorDefecto"/>.
/// </summary>
public static class PreferenciaCsv
{
    /// <summary>Separador que se usa cuando no hay preferencia válida.</summary>
    public const char PorDefecto = ';';

    /// <summary>Ruta del fichero de preferencia; los tests usan las sobrecargas con ruta.</summary>
    public static string Ruta { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GestorSolicitudes",
        "csv.txt");

    /// <summary>Separador guardado, o null si no hay fichero, no se puede leer o no es válido.</summary>
    internal static char? LeerDe(string ruta)
    {
        try
        {
            if (!File.Exists(ruta))
            {
                return null;
            }

            // Sin Trim(): el tabulador es un separador válido y se guarda tal cual.
            string contenido = File.ReadAllText(ruta).TrimEnd('\r', '\n');
            return contenido.Length == 1 && EsValido(contenido[0]) ? contenido[0] : null;
        }
        catch (Exception ex)
        {
            // Si no se puede leer la preferencia, se cae al separador por defecto.
            Debug.WriteLine($"No se pudo leer '{ruta}': {ex}");
            return null;
        }
    }

    /// <summary>Guarda el separador elegido. Los errores se ignoran.</summary>
    internal static void GuardarEn(string ruta, char separador)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
            File.WriteAllText(ruta, separador.ToString());
        }
        catch (Exception ex)
        {
            // Un fallo al guardar la preferencia no debe impedir la exportación.
            Debug.WriteLine($"No se pudo guardar '{ruta}': {ex}");
        }
    }

    /// <summary>Indica si el carácter es uno de los separadores admitidos.</summary>
    public static bool EsValido(char separador) => separador is ';' or ',' or '\t';
}
