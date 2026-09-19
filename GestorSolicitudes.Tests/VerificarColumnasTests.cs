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

            foreach (string esperada in new[] { "RutaCv", "RutaCarta", "NombreOriginalCv", "NombreOriginalCarta", "Requisitos" })
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
}