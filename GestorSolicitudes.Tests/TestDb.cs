using System.IO;
using GestorSolicitudes.Data;
using GestorSolicitudes.Helpers;

namespace GestorSolicitudes.Tests;

/// <summary>
/// Utilidades comunes: bases SQLite temporales y carpetas aisladas para que los
/// tests nunca toquen los datos reales de %APPDATA%.
/// </summary>
internal static class TestDb
{
    private static readonly string Raiz = Path.Combine(Path.GetTempPath(), "gs-tests");

    /// <summary>
    /// Borra las carpetas de ejecuciones anteriores. No se puede limpiar al terminar sin un
    /// fixture global, así que se limpia al empezar: la basura no se acumula entre ejecuciones.
    /// </summary>
    static TestDb()
    {
        try
        {
            if (Directory.Exists(Raiz))
            {
                Directory.Delete(Raiz, recursive: true);
            }
        }
        catch (Exception)
        {
            // Si algo quedó bloqueado por otra ejecución, se limpia la próxima vez.
        }
    }

    internal static string NuevaCarpeta()
    {
        string carpeta = Path.Combine(Raiz, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(carpeta);
        return carpeta;
    }

    public static string NuevaRuta() => Path.Combine(NuevaCarpeta(), "test.db");

    public static AppDbContext NuevoContexto() => NuevoContexto(out _);

    /// <summary>Contexto sobre una base temporal nueva; devuelve la ruta para poder reabrirla.</summary>
    public static AppDbContext NuevoContexto(out string ruta)
    {
        ruta = NuevaRuta();
        var db = new AppDbContext(ruta);
        db.Database.EnsureCreated();
        return db;
    }

    /// <summary>Crea un adjunto de prueba dentro de la carpeta indicada y devuelve su ruta.</summary>
    internal static string CrearAdjuntoEn(string carpeta, string nombre)
    {
        string origen = Path.Combine(NuevaCarpeta(), nombre);
        File.WriteAllText(origen, "contenido");
        return AdjuntosHelper.Copiar(origen, "adjunto", carpeta);
    }
}