namespace GestorSolicitudes.Data;

using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;

/// <summary>
/// Operaciones de esquema de SQLite que no encajan en <see cref="AppDbContext"/>: hoy, añadir
/// las columnas nuevas a bases creadas por versiones anteriores de la app.
/// </summary>
internal static class EsquemaDb
{
    /// <summary>
    /// Añade las columnas nuevas del modelo a bases de datos creadas por versiones anteriores.
    /// Más adelante, si el esquema crece, lo natural es pasar a migraciones.
    /// </summary>
    [SuppressMessage("SonarAnalyzer.CSharp", "S2077",
        Justification = "ALTER TABLE de SQLite no admite parámetros y tanto el nombre como la definición de la columna salen de un array fijo del código, nunca de entrada del usuario.")]
    internal static void VerificarColumnasFaltantes(string ruta)
    {
        using var conexion = new SqliteConnection($"Data Source={ruta}");
        conexion.Open();

        var columnas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var command = conexion.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info(Solicitudes);";
            using var lector = command.ExecuteReader();
            while (lector.Read())
            {
                columnas.Add(Convert.ToString(lector["name"]) ?? string.Empty);
            }
        }

        // Los enums se guardan como entero; Origen es NOT NULL con valor por defecto
        // (0 = AplicacionDirecta) para que las filas ya existentes queden con un
        // valor válido en vez de nulo.
        const string TextoNulo = "TEXT NULL";

        (string Columna, string Definicion)[] nuevas =
        {
            ("RutaCv", TextoNulo),
            ("RutaCarta", TextoNulo),
            ("NombreOriginalCv", TextoNulo),
            ("NombreOriginalCarta", TextoNulo),
            ("Requisitos", TextoNulo),
            ("MotivoRechazo", "INTEGER NULL"),
            ("Origen", "INTEGER NOT NULL DEFAULT 0"),
            ("ContactoTelefono", TextoNulo),
        };

        IEnumerable<(string Columna, string Definicion)> faltantes =
            nuevas.Where(nueva => !columnas.Contains(nueva.Columna));

        foreach ((string columna, string definicion) in faltantes)
        {
            using var command = conexion.CreateCommand();
            command.CommandText = $"ALTER TABLE Solicitudes ADD COLUMN \"{columna}\" {definicion};";
            command.ExecuteNonQuery();
        }
    }
}
