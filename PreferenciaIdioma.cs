namespace GestorSolicitudes.Helpers;

using System.Diagnostics;
using System.Globalization;
using System.IO;

/// <summary>
/// Idioma recordado en %APPDATA%\GestorSolicitudes\idioma.txt, con el idioma del sistema como
/// respaldo. Mismo patrón que <see cref="ColumnasHelper"/>: un fichero de texto que se lee al
/// arrancar y se ignora en silencio si no se puede leer o escribir.
/// </summary>
public static class PreferenciaIdioma
{
    /// <summary>Ruta del fichero de preferencia; los tests usan las sobrecargas con ruta.</summary>
    public static string Ruta { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GestorSolicitudes",
        "idioma.txt");

    /// <summary>Idioma guardado, o null si no hay fichero, no se puede leer o no es un idioma conocido.</summary>
    internal static Idioma? LeerDe(string ruta)
    {
        try
        {
            if (!File.Exists(ruta))
            {
                return null;
            }

            string contenido = File.ReadAllText(ruta).Trim();
            return Enum.TryParse(contenido, out Idioma guardado) && Enum.IsDefined(guardado)
                ? guardado
                : null;
        }
        catch (Exception ex)
        {
            // Si no se puede leer la preferencia, se cae al idioma del sistema.
            Debug.WriteLine($"No se pudo leer '{ruta}': {ex}");
            return null;
        }
    }

    /// <summary>Guarda el idioma elegido. Los errores se ignoran.</summary>
    internal static void GuardarEn(string ruta, Idioma idioma)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
            File.WriteAllText(ruta, idioma.ToString());
        }
        catch (Exception ex)
        {
            // Un fallo al guardar la preferencia no debe impedir el cambio de idioma.
            Debug.WriteLine($"No se pudo guardar '{ruta}': {ex}");
        }
    }

    /// <summary>Idioma que corresponde a una cultura; el resto cae a castellano.</summary>
    internal static Idioma DelSistema(CultureInfo cultura) => cultura.TwoLetterISOLanguageName switch
    {
        "en" => Idioma.Ingles,
        "de" => Idioma.Aleman,
        _ => Idioma.Castellano,
    };
}
