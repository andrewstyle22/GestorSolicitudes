namespace GestorSolicitudes.Data;

using System.Diagnostics.CodeAnalysis;
using GestorSolicitudes.Models;
using Microsoft.Data.Sqlite;

/// <summary>
/// Operaciones de esquema de SQLite que no encajan en <see cref="AppDbContext"/>: hoy, añadir
/// las columnas nuevas a bases creadas por versiones anteriores de la app.
/// </summary>
internal static class EsquemaDb
{
    /// <summary>
    /// Añade a bases de datos creadas por versiones anteriores las columnas nuevas del
    /// modelo y la tabla de documentos. Más adelante, si el esquema crece, lo natural es
    /// pasar a migraciones.
    /// </summary>
    [SuppressMessage("SonarAnalyzer.CSharp", "S2077",
        Justification = "ALTER TABLE de SQLite no admite parámetros y tanto el nombre como la definición de la columna salen de un array fijo del código, nunca de entrada del usuario.")]
    internal static void VerificarColumnasFaltantes(string ruta)
    {
        using var conexion = new SqliteConnection(AppDbContext.CadenaConexion(ruta));
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
            ("FechaReapertura", TextoNulo),
            ("SolicitudAnteriorId", "INTEGER NULL"),
        };

        IEnumerable<(string Columna, string Definicion)> faltantes =
            nuevas.Where(nueva => !columnas.Contains(nueva.Columna));

        foreach ((string columna, string definicion) in faltantes)
        {
            using var command = conexion.CreateCommand();
            command.CommandText = $"ALTER TABLE Solicitudes ADD COLUMN \"{columna}\" {definicion};";
            command.ExecuteNonQuery();
        }

        CrearTablaDocumentos(conexion);
    }

    /// <summary>
    /// Crea la tabla de documentos si no existe: EnsureCreated no versiona el esquema y
    /// las bases anteriores a la Etapa E no la tienen. El borrado en cascada depende de
    /// que la conexión tenga activadas las claves foráneas.
    /// </summary>
    private static void CrearTablaDocumentos(SqliteConnection conexion)
    {
        using var command = conexion.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS "DocumentosSolicitud" (
                "SolicitudId" INTEGER NOT NULL CONSTRAINT "PK_DocumentosSolicitud" PRIMARY KEY,
                "Cv" BLOB NULL,
                "Carta" BLOB NULL,
                CONSTRAINT "FK_DocumentosSolicitud_Solicitudes_SolicitudId"
                    FOREIGN KEY ("SolicitudId") REFERENCES "Solicitudes" ("Id") ON DELETE CASCADE
            );
            """;
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Rellena el motivo de cierre de las filas antiguas: en los tres cierres negativos
    /// el motivo pasa a ser obligatorio, y un motivo de empresa no vale para un cierre
    /// propio. Es idempotente: después solo se escriben motivos válidos.
    /// </summary>
    internal static void NormalizarMotivosDeCierre(string ruta)
    {
        using var conexion = new SqliteConnection(AppDbContext.CadenaConexion(ruta));
        conexion.Open();

        using var command = conexion.CreateCommand();
        command.CommandText = """
            UPDATE Solicitudes SET MotivoRechazo = $noIndicado
            WHERE Estado IN ($ofertaRechazada, $descartado, $retirada) AND MotivoRechazo IS NULL;
            UPDATE Solicitudes SET MotivoRechazo = $noIndicado
            WHERE Estado IN ($ofertaRechazada, $retirada)
              AND MotivoRechazo IN ($experienciaInsuficiente, $seleccionaronOtro, $culturalFit, $puestoCancelado);
            """;

        foreach ((string nombre, int valor) in new[]
        {
            ("$noIndicado", (int)MotivoRechazo.NoIndicado),
            ("$ofertaRechazada", (int)EstadoSolicitud.OfertaRechazada),
            ("$descartado", (int)EstadoSolicitud.Rechazada),
            ("$retirada", (int)EstadoSolicitud.Retirada),
            ("$experienciaInsuficiente", (int)MotivoRechazo.ExperienciaInsuficiente),
            ("$seleccionaronOtro", (int)MotivoRechazo.SeleccionaronOtroCandidato),
            ("$culturalFit", (int)MotivoRechazo.CulturalFit),
            ("$puestoCancelado", (int)MotivoRechazo.PuestoCanceladoOPausado),
        })
        {
            command.Parameters.AddWithValue(nombre, valor);
        }

        command.ExecuteNonQuery();
    }
}
