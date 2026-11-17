namespace GestorSolicitudes;

using System.IO;
using GestorSolicitudes.Data;
using GestorSolicitudes.Helpers;
using Microsoft.Data.Sqlite;

/// <summary>
/// Migra al arrancar los adjuntos que las versiones anteriores guardaban como ficheros en disco:
/// copia sus bytes a <c>DocumentosSolicitud</c>, rellena el nombre original y deja la ruta a
/// <c>NULL</c>. Lee las rutas con SQL porque el modelo deja de exponerlas (Etapa E). Vive en el
/// Núcleo porque combina la base de datos con el registro de errores, igual que <see cref="App"/>.
/// </summary>
internal static class MigradorAdjuntos
{
    /// <summary>
    /// Migra las filas con ruta pendiente dentro de una transacción. Un fichero que ya no existe
    /// deja su documento vacío y se registra, sin impedir que se migre el resto. Es idempotente:
    /// una fila migrada queda sin ruta y no se vuelve a tocar. Tras confirmar la transacción,
    /// borra los ficheros migrados de la carpeta de adjuntos y la carpeta si queda vacía.
    /// </summary>
    public static void Migrar(string ruta, string rutaRegistro, string? carpeta = null)
    {
        using var conexion = new SqliteConnection(AppDbContext.CadenaConexion(ruta));
        conexion.Open();

        // Una base creada por esta versión ya no tiene las columnas de ruta.
        if (!TieneColumnasDeRuta(conexion))
        {
            return;
        }

        List<FilaPendiente> pendientes = LeerPendientes(conexion);
        if (pendientes.Count == 0)
        {
            return;
        }

        var migradas = new List<string>();
        using var transaccion = conexion.BeginTransaction();
        foreach (FilaPendiente fila in pendientes)
        {
            byte[]? cv = null;
            byte[]? carta = null;
            string? nombreCv = fila.NombreCv;
            string? nombreCarta = fila.NombreCarta;

            if (fila.RutaCv is not null)
            {
                cv = LeerFichero(fila.RutaCv, $"el CV de la candidatura {fila.Id}", rutaRegistro);
                nombreCv = Nombre(cv, fila.RutaCv, fila.NombreCv);
                if (cv is not null)
                {
                    migradas.Add(fila.RutaCv);
                }
            }

            if (fila.RutaCarta is not null)
            {
                carta = LeerFichero(fila.RutaCarta, $"la carta de la candidatura {fila.Id}", rutaRegistro);
                nombreCarta = Nombre(carta, fila.RutaCarta, fila.NombreCarta);
                if (carta is not null)
                {
                    migradas.Add(fila.RutaCarta);
                }
            }

            ActualizarSolicitud(conexion, transaccion, fila.Id, nombreCv, nombreCarta);

            if (cv is not null || carta is not null)
            {
                GuardarDocumentos(conexion, transaccion, fila.Id, cv, carta);
            }
        }

        transaccion.Commit();

        BorrarMigrados(migradas, carpeta ?? AdjuntosHelper.Carpeta, rutaRegistro);
    }

    /// <summary>
    /// Borra los ficheros ya copiados. Solo toca los que están dentro de la carpeta de adjuntos,
    /// igual que la limpieza normal: nunca un fichero arbitrario de la base. Un fallo al borrar
    /// se registra y no interrumpe el resto.
    /// </summary>
    private static void BorrarMigrados(
        IEnumerable<string> rutas,
        string carpeta,
        string rutaRegistro)
    {
        foreach (string ruta in rutas)
        {
            try
            {
                if (EstaDentro(ruta, carpeta) && File.Exists(ruta))
                {
                    File.Delete(ruta);
                }
            }
            catch (Exception ex)
            {
                RegistroErrores.RegistrarEn(rutaRegistro, ex, $"no se pudo borrar el adjunto '{ruta}'");
            }
        }

        try
        {
            if (Directory.Exists(carpeta) && !Directory.EnumerateFileSystemEntries(carpeta).Any())
            {
                Directory.Delete(carpeta);
            }
        }
        catch (Exception ex)
        {
            RegistroErrores.RegistrarEn(rutaRegistro, ex, $"no se pudo borrar la carpeta '{carpeta}'");
        }
    }

    private static bool EstaDentro(string ruta, string carpeta)
    {
        string carpetaCompleta = Path.GetFullPath(carpeta) + Path.DirectorySeparatorChar;
        string rutaCompleta = Path.GetFullPath(ruta);
        return rutaCompleta.StartsWith(carpetaCompleta, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TieneColumnasDeRuta(SqliteConnection conexion)
    {
        using var comando = conexion.CreateCommand();
        comando.CommandText = "PRAGMA table_info(Solicitudes);";
        using var lector = comando.ExecuteReader();

        bool cv = false;
        bool carta = false;
        while (lector.Read())
        {
            string nombre = Convert.ToString(lector["name"]) ?? string.Empty;
            cv |= string.Equals(nombre, "RutaCv", StringComparison.OrdinalIgnoreCase);
            carta |= string.Equals(nombre, "RutaCarta", StringComparison.OrdinalIgnoreCase);
        }

        return cv && carta;
    }

    private static List<FilaPendiente> LeerPendientes(SqliteConnection conexion)
    {
        using var comando = conexion.CreateCommand();
        comando.CommandText = """
            SELECT Id, RutaCv, RutaCarta, NombreOriginalCv, NombreOriginalCarta
            FROM Solicitudes
            WHERE RutaCv IS NOT NULL OR RutaCarta IS NOT NULL;
            """;

        using var lector = comando.ExecuteReader();
        var filas = new List<FilaPendiente>();
        while (lector.Read())
        {
            filas.Add(new FilaPendiente(
                lector.GetInt32(0),
                lector.IsDBNull(1) ? null : lector.GetString(1),
                lector.IsDBNull(2) ? null : lector.GetString(2),
                lector.IsDBNull(3) ? null : lector.GetString(3),
                lector.IsDBNull(4) ? null : lector.GetString(4)));
        }

        return filas;
    }

    /// <summary>
    /// Devuelve el contenido del fichero o <c>null</c> si falta o no se puede leer. Comprueba que
    /// el tamaño leído sigue siendo el que tenía el fichero antes de abrirlo.
    /// </summary>
    private static byte[]? LeerFichero(string ruta, string contexto, string rutaRegistro)
    {
        try
        {
            long longitud = new FileInfo(ruta).Length;
            byte[] contenido = File.ReadAllBytes(ruta);
            if (contenido.LongLength != longitud)
            {
                throw new IOException($"El fichero '{ruta}' cambió de tamaño durante la migración.");
            }

            return contenido;
        }
        catch (Exception ex)
        {
            // Un fichero ausente o ilegible no puede dejar la aplicación sin arrancar.
            RegistroErrores.RegistrarEn(rutaRegistro, ex, $"no se pudo migrar {contexto}");
            return null;
        }
    }

    private static string? Nombre(byte[]? contenido, string ruta, string? nombreActual)
    {
        if (contenido is null)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(nombreActual) ? Path.GetFileName(ruta) : nombreActual;
    }

    private static void ActualizarSolicitud(
        SqliteConnection conexion,
        SqliteTransaction transaccion,
        int id,
        string? nombreCv,
        string? nombreCarta)
    {
        using var comando = conexion.CreateCommand();
        comando.Transaction = transaccion;
        comando.CommandText = """
            UPDATE Solicitudes
            SET RutaCv = NULL, RutaCarta = NULL, NombreOriginalCv = $cv, NombreOriginalCarta = $carta
            WHERE Id = $id;
            """;
        comando.Parameters.AddWithValue("$cv", (object?)nombreCv ?? DBNull.Value);
        comando.Parameters.AddWithValue("$carta", (object?)nombreCarta ?? DBNull.Value);
        comando.Parameters.AddWithValue("$id", id);
        comando.ExecuteNonQuery();
    }

    private static void GuardarDocumentos(
        SqliteConnection conexion,
        SqliteTransaction transaccion,
        int id,
        byte[]? cv,
        byte[]? carta)
    {
        using var comando = conexion.CreateCommand();
        comando.Transaction = transaccion;
        comando.CommandText = """
            INSERT INTO DocumentosSolicitud (SolicitudId, Cv, Carta)
            VALUES ($id, $cv, $carta)
            ON CONFLICT(SolicitudId) DO UPDATE SET
                Cv = COALESCE(excluded.Cv, Cv),
                Carta = COALESCE(excluded.Carta, Carta);
            """;
        comando.Parameters.AddWithValue("$id", id);
        comando.Parameters.AddWithValue("$cv", (object?)cv ?? DBNull.Value);
        comando.Parameters.AddWithValue("$carta", (object?)carta ?? DBNull.Value);
        comando.ExecuteNonQuery();
    }

    private sealed record FilaPendiente(
        int Id,
        string? RutaCv,
        string? RutaCarta,
        string? NombreCv,
        string? NombreCarta);
}
