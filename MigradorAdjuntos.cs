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
    private const string SqlCv =
        "UPDATE Solicitudes SET RutaCv = NULL, NombreOriginalCv = $nombre WHERE Id = $id;";

    private const string SqlCarta =
        "UPDATE Solicitudes SET RutaCarta = NULL, NombreOriginalCarta = $nombre WHERE Id = $id;";

    /// <summary>
    /// Migra las filas con ruta pendiente dentro de una transacción. Un fichero que ya no existe
    /// deja su documento vacío y se registra, sin impedir que se migre el resto. Un fichero que
    /// existe pero no se puede leer conserva su ruta y se reintenta en el siguiente arranque. Es
    /// idempotente: una fila migrada queda sin ruta y no se vuelve a tocar. Tras confirmar la
    /// transacción, borra los ficheros migrados de la carpeta de adjuntos y la carpeta si queda vacía.
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
            MigrarFila(conexion, transaccion, fila, rutaRegistro, migradas);
        }

        transaccion.Commit();

        BorrarMigrados(migradas, carpeta ?? AdjuntosHelper.Carpeta, rutaRegistro);
    }

    private static void MigrarFila(
        SqliteConnection conexion,
        SqliteTransaction transaccion,
        FilaPendiente fila,
        string rutaRegistro,
        List<string> migradas)
    {
        LecturaAdjunto cv = LeerAdjunto(
            fila.RutaCv, fila.NombreCv, $"el CV de la candidatura {fila.Id}", rutaRegistro, migradas);
        LecturaAdjunto carta = LeerAdjunto(
            fila.RutaCarta, fila.NombreCarta, $"la carta de la candidatura {fila.Id}", rutaRegistro, migradas);

        ActualizarSolicitud(conexion, transaccion, fila.Id, cv, carta);

        if (cv.Contenido is not null || carta.Contenido is not null)
        {
            GuardarDocumentos(conexion, transaccion, fila.Id, cv.Contenido, carta.Contenido);
        }
    }

    /// <summary>
    /// Lee un adjunto de la fila. Sin ruta no hay nada que hacer; un fichero ausente deja el
    /// documento vacío; uno ilegible no se toca para reintentarlo en el siguiente arranque.
    /// </summary>
    private static LecturaAdjunto LeerAdjunto(
        string? ruta,
        string? nombreActual,
        string contexto,
        string rutaRegistro,
        List<string> migradas)
    {
        if (ruta is null)
        {
            return new LecturaAdjunto(EstadoAdjunto.SinRuta, null, null);
        }

        (EstadoAdjunto estado, byte[]? contenido) = LeerFichero(ruta, contexto, rutaRegistro);
        if (estado != EstadoAdjunto.Leido)
        {
            return new LecturaAdjunto(estado, null, null);
        }

        migradas.Add(ruta);
        return new LecturaAdjunto(estado, contenido, Nombre(ruta, nombreActual));
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
    /// Devuelve el contenido del fichero y si se pudo leer. Distingue el fichero que ya no existe
    /// (se puede dar por vacío) del que existe pero no se puede leer (se reintentará). Comprueba
    /// que el tamaño leído sigue siendo el que tenía el fichero antes de abrirlo.
    /// </summary>
    private static (EstadoAdjunto Estado, byte[]? Contenido) LeerFichero(
        string ruta,
        string contexto,
        string rutaRegistro)
    {
        try
        {
            long longitud = new FileInfo(ruta).Length;
            byte[] contenido = File.ReadAllBytes(ruta);
            if (contenido.LongLength != longitud)
            {
                throw new IOException($"El fichero '{ruta}' cambió de tamaño durante la migración.");
            }

            return (EstadoAdjunto.Leido, contenido);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            // El fichero ya no está: se da por vacío, como siempre.
            RegistroErrores.RegistrarEn(rutaRegistro, ex, $"no se pudo migrar {contexto} desde '{ruta}'");
            return (EstadoAdjunto.Ausente, null);
        }
        catch (Exception ex)
        {
            // Un fichero ilegible no puede dejar la aplicación sin arrancar ni perder la ruta:
            // la fila sigue pendiente y se reintenta en el siguiente arranque.
            RegistroErrores.RegistrarEn(rutaRegistro, ex, $"no se pudo migrar {contexto} desde '{ruta}'");
            return (EstadoAdjunto.Ilegible, null);
        }
    }

    private static string Nombre(string ruta, string? nombreActual) =>
        string.IsNullOrWhiteSpace(nombreActual) ? Path.GetFileName(ruta) : nombreActual;

    /// <summary>
    /// Suelta la ruta y guarda el nombre de los documentos resueltos. El documento ilegible no se
    /// toca, para no perder su ruta mientras se reintenta.
    /// </summary>
    private static void ActualizarSolicitud(
        SqliteConnection conexion,
        SqliteTransaction transaccion,
        int id,
        LecturaAdjunto cv,
        LecturaAdjunto carta)
    {
        if (cv.Resuelto)
        {
            EjecutarActualizacion(conexion, transaccion, SqlCv, id, cv.NombreFichero);
        }

        if (carta.Resuelto)
        {
            EjecutarActualizacion(conexion, transaccion, SqlCarta, id, carta.NombreFichero);
        }
    }

    private static void EjecutarActualizacion(
        SqliteConnection conexion,
        SqliteTransaction transaccion,
        string sql,
        int id,
        string? nombre)
    {
        using var comando = conexion.CreateCommand();
        comando.Transaction = transaccion;
        comando.CommandText = sql;
        comando.Parameters.AddWithValue("$nombre", (object?)nombre ?? DBNull.Value);
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

    private enum EstadoAdjunto
    {
        SinRuta,
        Leido,
        Ausente,
        Ilegible,
    }

    private sealed record LecturaAdjunto(EstadoAdjunto Estado, byte[]? Contenido, string? NombreFichero)
    {
        /// <summary>La fila deja de estar pendiente para este documento.</summary>
        public bool Resuelto => this.Estado is EstadoAdjunto.Leido or EstadoAdjunto.Ausente;
    }

    private sealed record FilaPendiente(
        int Id,
        string? RutaCv,
        string? RutaCarta,
        string? NombreCv,
        string? NombreCarta);
}
