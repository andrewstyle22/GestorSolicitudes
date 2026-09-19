using Xunit;
using System.IO;
using Microsoft.Data.Sqlite;

namespace GestorSolicitudes.Tests;

public class VerificarColumnasTests
{
    [Fact]
    public void VerificarColumnas_AñadeLasColumnasNuevasAUnaBaseVieja()
    {
        string ruta = TestDb.NuevaRuta();

        // Simulamos una base de la primera versión: Solicitudes sin las columnas nuevas.
        using (var conexion = new SqliteConnection($"Data Source={ruta}"))
        {
            conexion.Open();
            using var crear = conexion.CreateCommand();
            crear.CommandText = """
                CREATE TABLE Solicitudes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Empresa TEXT NOT NULL,
                    Puesto TEXT NOT NULL,
                    FechaSolicitud TEXT NOT NULL,
                    Estado INTEGER NOT NULL,
                    Interes INTEGER NOT NULL DEFAULT 3
                );
                """;
            crear.ExecuteNonQuery();
        }

        App.VerificarColumnasFaltantes(ruta);

        using (var conexion = new SqliteConnection($"Data Source={ruta}"))
        {
            conexion.Open();
            using var comando = conexion.CreateCommand();
            comando.CommandText = "PRAGMA table_info(Solicitudes);";
            using var lector = comando.ExecuteReader();
            var columnas = new List<string>();
            while (lector.Read())
                columnas.Add(Convert.ToString(lector["name"])!);

            foreach (string esperada in new[] { "RutaCv", "RutaCarta", "NombreOriginalCv", "NombreOriginalCarta", "Requisitos", "MotivoRechazo", "Origen", "ContactoTelefono" })
                Assert.Contains(esperada, columnas);
        }
    }

    [Theory]
    [InlineData("Requisitos")]
    [InlineData("RutaCv")]
    public void VerificarColumnas_EsIdempotente(string columnaYaPresente)
    {
        // Si ya existe, llamar dos veces no debe romper nada.
        string ruta = TestDb.NuevaRuta();
        using (var conexion = new SqliteConnection($"Data Source={ruta}"))
        {
            conexion.Open();
            using var crear = conexion.CreateCommand();
            crear.CommandText = $$"""
                CREATE TABLE Solicitudes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Empresa TEXT NOT NULL,
                    Puesto TEXT NOT NULL,
                    {{columnaYaPresente}} TEXT NULL
                );
                """;
            crear.ExecuteNonQuery();
        }

        App.VerificarColumnasFaltantes(ruta);
        App.VerificarColumnasFaltantes(ruta);

        Assert.True(File.Exists(ruta));
    }

    [Fact]
    public void VerificarColumnas_OrigenSeCreaEnteroNoNuloConDefaultACero()
    {
        string ruta = TestDb.NuevaRuta();
        using (var conexion = new SqliteConnection($"Data Source={ruta}"))
        {
            conexion.Open();
            using var crear = conexion.CreateCommand();
            crear.CommandText = """
                CREATE TABLE Solicitudes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Empresa TEXT NOT NULL,
                    Puesto TEXT NOT NULL,
                    FechaSolicitud TEXT NOT NULL,
                    Estado INTEGER NOT NULL,
                    Interes INTEGER NOT NULL DEFAULT 3
                );
                """;
            crear.ExecuteNonQuery();
        }

        App.VerificarColumnasFaltantes(ruta);

        using (var conexion = new SqliteConnection($"Data Source={ruta}"))
        {
            conexion.Open();
            using var comando = conexion.CreateCommand();
            comando.CommandText = "PRAGMA table_info(Solicitudes);";
            using var lector = comando.ExecuteReader();
            var columnas = new Dictionary<string, (bool NoNulo, string? Default)>();
            while (lector.Read())
            {
                string nombre = Convert.ToString(lector["name"])!;
                bool noNulo = Convert.ToInt64(lector["notnull"]) == 1;
                string? valorDefault = lector["dflt_value"] is DBNull
                    ? null
                    : Convert.ToString(lector["dflt_value"]);
                columnas[nombre] = (noNulo, valorDefault);
            }

            // Origen es entero NOT NULL con DEFAULT 0 para que las filas ya
            // existentes queden con AplicacionDirecta en vez de nulo.
            Assert.True(columnas.ContainsKey("Origen"));
            Assert.True(columnas["Origen"].NoNulo);
            Assert.Equal("0", columnas["Origen"].Default);

            // MotivoRechazo y ContactoTelefono admiten nulo.
            Assert.True(columnas.ContainsKey("MotivoRechazo"));
            Assert.False(columnas["MotivoRechazo"].NoNulo);
            Assert.True(columnas.ContainsKey("ContactoTelefono"));
            Assert.False(columnas["ContactoTelefono"].NoNulo);
        }
    }
}