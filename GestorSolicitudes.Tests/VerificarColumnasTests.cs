namespace GestorSolicitudes.Tests;

using System.IO;
using GestorSolicitudes.Data;
using GestorSolicitudes.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

public class VerificarColumnasTests
{
    private const string ColumnaFechaReapertura = "FechaReapertura";
    private const string ColumnaSolicitudAnteriorId = "SolicitudAnteriorId";
    private const string TablaSolicitudes = "Solicitudes";
    private const string TablaDocumentos = "DocumentosSolicitud";

    [Fact]
    public void VerificarColumnas_AñadeLasColumnasNuevasAUnaBaseVieja()
    {
        string ruta = CrearTablaSolicitudesVieja();

        EsquemaDb.VerificarColumnasFaltantes(ruta);

        using (var conexion = new SqliteConnection($"Data Source={ruta}"))
        {
            conexion.Open();
            using var comando = conexion.CreateCommand();
            comando.CommandText = "PRAGMA table_info(Solicitudes);";
            using var lector = comando.ExecuteReader();
            var columnas = new List<string>();
            while (lector.Read())
            {
                columnas.Add(Convert.ToString(lector["name"])!);
            }

            foreach (string esperada in new[] { "NombreOriginalCv", "NombreOriginalCarta", "Requisitos", "MotivoRechazo", "Origen", "ContactoTelefono", ColumnaFechaReapertura, ColumnaSolicitudAnteriorId })
            {
                Assert.Contains(esperada, columnas);
            }
        }
    }

    [Theory]
    [InlineData("Requisitos")]
    [InlineData("ContactoTelefono")]
    public void VerificarColumnas_EsIdempotente(string columnaYaPresente)
    {
        // Si ya existe, llamar dos veces no debe romper nada.
        string ruta = CrearTablaSolicitudesVieja(columnaYaPresente);

        EsquemaDb.VerificarColumnasFaltantes(ruta);
        EsquemaDb.VerificarColumnasFaltantes(ruta);

        Assert.True(File.Exists(ruta));
    }

    [Fact]
    public void VerificarColumnas_OrigenSeCreaEnteroNoNuloConDefaultACero()
    {
        string ruta = CrearTablaSolicitudesVieja();

        EsquemaDb.VerificarColumnasFaltantes(ruta);

        using (var conexion = new SqliteConnection($"Data Source={ruta}"))
        {
            conexion.Open();
            using var comando = conexion.CreateCommand();
            comando.CommandText = "PRAGMA table_info(Solicitudes);";
            using var lector = comando.ExecuteReader();
            var columnas = new Dictionary<string, (string Tipo, bool NoNulo, string? Default)>();
            while (lector.Read())
            {
                string nombre = Convert.ToString(lector["name"])!;
                string tipo = Convert.ToString(lector["type"])!;
                bool noNulo = Convert.ToInt64(lector["notnull"]) == 1;
                string? valorDefault = lector["dflt_value"] is DBNull
                    ? null
                    : Convert.ToString(lector["dflt_value"]);
                columnas[nombre] = (tipo, noNulo, valorDefault);
            }

            // Origen es entero NOT NULL con DEFAULT 0 para que las filas ya
            // existentes queden con AplicacionDirecta en vez de nulo.
            Assert.True(columnas.ContainsKey("Origen"));
            Assert.True(columnas["Origen"].NoNulo);
            Assert.Equal("0", columnas["Origen"].Default);

            // MotivoRechazo, ContactoTelefono y FechaReapertura admiten nulo.
            Assert.True(columnas.ContainsKey("MotivoRechazo"));
            Assert.False(columnas["MotivoRechazo"].NoNulo);
            Assert.True(columnas.ContainsKey("ContactoTelefono"));
            Assert.False(columnas["ContactoTelefono"].NoNulo);
            Assert.Equal("TEXT", columnas[ColumnaFechaReapertura].Tipo);
            Assert.False(columnas[ColumnaFechaReapertura].NoNulo);

            // El enlace con la candidatura anterior es opcional.
            Assert.Equal("INTEGER", columnas[ColumnaSolicitudAnteriorId].Tipo);
            Assert.False(columnas[ColumnaSolicitudAnteriorId].NoNulo);
        }
    }

    [Fact]
    public void VerificarColumnas_CreaLaTablaDeDocumentosEnUnaBaseVieja()
    {
        string ruta = CrearTablaSolicitudesVieja();

        EsquemaDb.VerificarColumnasFaltantes(ruta);

        using var conexion = new SqliteConnection($"Data Source={ruta}");
        conexion.Open();

        using (var crear = conexion.CreateCommand())
        {
            crear.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'DocumentosSolicitud';";
            Assert.Equal("DocumentosSolicitud", crear.ExecuteScalar());
        }

        using var claves = conexion.CreateCommand();
        claves.CommandText = "PRAGMA foreign_key_list(DocumentosSolicitud);";
        using var lector = claves.ExecuteReader();
        Assert.True(lector.Read());
        Assert.Equal("Solicitudes", lector["table"]);
        Assert.Equal("CASCADE", lector["on_delete"]);
    }

    [Fact]
    public void VerificarColumnas_CubreTodasLasColumnasDelModelo()
    {
        string ruta = CrearTablaSolicitudesVieja();

        EsquemaDb.VerificarColumnasFaltantes(ruta);

        var columnas = LeerColumnas(ruta, TablaSolicitudes);
        foreach (string esperada in ColumnasDelModelo<Solicitud>(TablaSolicitudes))
        {
            Assert.Contains(esperada, columnas);
        }

        var columnasDocumentos = LeerColumnas(ruta, TablaDocumentos);
        foreach (string esperada in ColumnasDelModelo<DocumentosSolicitud>(TablaDocumentos))
        {
            Assert.Contains(esperada, columnasDocumentos);
        }
    }

    private static HashSet<string> LeerColumnas(string ruta, string tabla)
    {
        using var conexion = new SqliteConnection($"Data Source={ruta}");
        conexion.Open();
        using var comando = conexion.CreateCommand();
        comando.CommandText = "SELECT name FROM pragma_table_info($tabla);";
        comando.Parameters.AddWithValue("$tabla", tabla);
        using var lector = comando.ExecuteReader();
        var columnas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (lector.Read())
        {
            columnas.Add(Convert.ToString(lector["name"])!);
        }

        return columnas;
    }

    private static List<string> ColumnasDelModelo<T>(string tabla)
    {
        using var db = TestDb.NuevoContexto();
        var entidad = db.Model.FindEntityType(typeof(T))!;
        var objetivo = StoreObjectIdentifier.Table(tabla);
        return entidad.GetProperties()
            .Select(propiedad => propiedad.GetColumnName(objetivo))
            .OfType<string>()
            .ToList();
    }

    /// <summary>
    /// Base de la primera versión (sin las columnas que añade <see cref="EsquemaDb"/>) con
    /// las columnas extra que pida el test, para simular una base que ya las tenía.
    /// </summary>
    private static string CrearTablaSolicitudesVieja(params string[] columnasExtra)
    {
        string ruta = TestDb.NuevaRuta();
        string extra = string.Concat(columnasExtra.Select(c => $",\n    {c} TEXT NULL"));

        using var conexion = new SqliteConnection($"Data Source={ruta}");
        conexion.Open();
        using var crear = conexion.CreateCommand();
        crear.CommandText = $"""
            CREATE TABLE Solicitudes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Empresa TEXT NOT NULL,
                Puesto TEXT NOT NULL,
                EnlaceOferta TEXT NULL,
                Portal TEXT NULL,
                Ubicacion TEXT NULL,
                Modalidad INTEGER NOT NULL,
                Tecnologias TEXT NULL,
                SalarioMin INTEGER NULL,
                SalarioMax INTEGER NULL,
                PretensionSalarial INTEGER NULL,
                FechaSolicitud TEXT NOT NULL,
                FechaPrimeraRespuesta TEXT NULL,
                FechaEntrevista TEXT NULL,
                FechaCierre TEXT NULL,
                ProximoSeguimiento TEXT NULL,
                Estado INTEGER NOT NULL,
                RespuestaEmpresa TEXT NULL,
                ContactoNombre TEXT NULL,
                ContactoEmail TEXT NULL,
                Interes INTEGER NOT NULL DEFAULT 3,
                Notas TEXT NULL,
                RutaCv TEXT NULL,
                RutaCarta TEXT NULL{extra}
            );
            """;
        crear.ExecuteNonQuery();
        return ruta;
    }
}
