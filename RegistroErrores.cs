namespace GestorSolicitudes.Helpers;

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

/// <summary>
/// Registro de errores sin controlar en %APPDATA%\GestorSolicitudes\errores.log. Añade al final
/// y, cuando el fichero pasa de <see cref="LimiteBytes"/>, se queda solo con la parte reciente.
/// Escribir el registro nunca lanza: es lo último que puede fallar cuando la aplicación ya falla.
/// </summary>
public static class RegistroErrores
{
    /// <summary>Tamaño a partir del cual se recorta el fichero (1 MB).</summary>
    public const long LimiteBytes = 1024 * 1024;

    /// <summary>Ruta del fichero de registro; los tests usan la sobrecarga con ruta.</summary>
    public static string Ruta { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GestorSolicitudes",
        "errores.log");

    /// <summary>Anota una excepción junto al contexto en el que ocurrió. Nunca lanza.</summary>
    public static void Registrar(Exception excepcion, string contexto) => RegistrarEn(Ruta, excepcion, contexto);

    /// <summary>Igual que <see cref="Registrar"/>, sobre una ruta concreta (tests).</summary>
    internal static void RegistrarEn(string ruta, Exception excepcion, string contexto)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
            File.AppendAllText(ruta, Entrada(excepcion, contexto), Encoding.UTF8);
            Recortar(ruta);
        }
        catch (Exception ex)
        {
            // El registro nunca puede tumbar a la aplicación que intenta salvar.
            Debug.WriteLine($"No se pudo escribir '{ruta}': {ex}");
        }
    }

    private static string Entrada(Exception excepcion, string contexto) =>
        string.Join(
            Environment.NewLine,
            $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}] {contexto}",
            excepcion.ToString(),
            "---",
            string.Empty);

    private static void Recortar(string ruta)
    {
        if (new FileInfo(ruta).Length <= LimiteBytes)
        {
            return;
        }

        // Se conserva la mitad reciente y se empieza en la línea siguiente para no dejar
        // una entrada partida por la mitad.
        byte[] contenido = File.ReadAllBytes(ruta);
        int corte = contenido.Length - (int)(LimiteBytes / 2);
        int salto = Array.IndexOf(contenido, (byte)'\n', corte);
        File.WriteAllBytes(ruta, salto >= 0 ? contenido[(salto + 1)..] : contenido[corte..]);
    }
}
