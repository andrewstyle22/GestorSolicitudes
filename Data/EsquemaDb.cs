namespace GestorSolicitudes.Data;

using Microsoft.Data.Sqlite;

/// <summary>Operaciones de esquema y migración SQLite para la aplicación.</summary>
internal static class EsquemaDb
{
    /// <summary>Verifica las columnas que faltan en la base de datos y las crea.</summary>
    /// <param name="ruta">Ruta al archivo de base de datos SQLite.</param>
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
            ("ContactoTelefono", "TEXT NULL"),
        };

        foreach ((string columna, string definicion) in nuevas)
        {
            if (columnas.Contains(columna))
            {
                continue;
            }

            using var command = conexion.CreateCommand();
            command.CommandText = $"ALTER TABLE Solicitudes ADD COLUMN \"{columna}\" {definicion};";
            command.ExecuteNonQuery();
        }
    }
}