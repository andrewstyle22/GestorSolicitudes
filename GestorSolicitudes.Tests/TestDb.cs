using System.IO;
using GestorSolicitudes.Data;

namespace GestorSolicitudes.Tests;

/// <summary>
/// Utilidades comunes: bases SQLite temporales y carpetas aisladas para que los
/// tests nunca toquen los datos reales de %APPDATA%.
/// </summary>
internal static class TestDb
{
    public static string NuevaRuta()
    {
        string carpeta = Path.Combine(Path.GetTempPath(), "gs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(carpeta);
        return Path.Combine(carpeta, "test.db");
    }

    public static string NuevaCarpeta()
    {
        string carpeta = Path.Combine(Path.GetTempPath(), "gs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(carpeta);
        return carpeta;
    }

    public static AppDbContext NuevoContexto()
    {
        var db = new AppDbContext(NuevaRuta());
        db.Database.EnsureCreated();
        return db;
    }
}