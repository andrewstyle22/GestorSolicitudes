namespace GestorSolicitudes.Tests;

using System.IO;
using GestorSolicitudes.Data;
using GestorSolicitudes.Helpers;
using GestorSolicitudes.Models;
using Microsoft.Data.Sqlite;

public class MigradorAdjuntosTests
{
    private const string NombreCv = "cv.pdf";
    private const string NombreRegistro = "errores.log";

    [Fact]
    public void Migrar_CopiaLosDocumentosYDejaLosNombres()
    {
        string carpeta = TestDb.NuevaCarpeta();
        string ficheroCv = CrearFichero(carpeta, NombreCv, [1, 2, 3]);
        string ficheroCarta = CrearFichero(carpeta, "carta.pdf", [4, 5]);
        string registro = Path.Combine(carpeta, NombreRegistro);
        int id;
        string ruta;

        using (var db = TestDb.NuevoContexto(out ruta))
        {
            var solicitud = new SolicitudBuilder().Construir();
            db.Solicitudes.Add(solicitud);
            db.SaveChanges();
            id = solicitud.Id;
            ApuntarRutas(ruta, id, ficheroCv, ficheroCarta);
        }

        IReadOnlyList<string> pendientes = MigradorAdjuntos.Migrar(ruta, registro, carpeta);

        using var db2 = new AppDbContext(ruta);
        Solicitud migrada = db2.Solicitudes.Single(s => s.Id == id);
        Assert.Equal(NombreCv, migrada.NombreOriginalCv);
        Assert.Equal("carta.pdf", migrada.NombreOriginalCarta);
        Assert.Empty(pendientes);

        DocumentosSolicitud documentos = db2.Set<DocumentosSolicitud>().Single(d => d.SolicitudId == id);
        Assert.Equal([1, 2, 3], documentos.Cv);
        Assert.Equal([4, 5], documentos.Carta);
        Assert.False(File.Exists(registro));
        Assert.False(File.Exists(ficheroCv));
        Assert.False(File.Exists(ficheroCarta));
        Assert.False(Directory.Exists(carpeta));
    }

    [Fact]
    public void Migrar_NoLeeNiBorraLosFicherosFueraDeLaCarpetaDeAdjuntos()
    {
        string origen = TestDb.NuevaCarpeta();
        string adjuntos = TestDb.NuevaCarpeta();
        string ficheroCv = CrearFichero(origen, NombreCv, [9]);
        string registro = Path.Combine(origen, NombreRegistro);
        int id;
        string ruta;

        using (var db = TestDb.NuevoContexto(out ruta))
        {
            var solicitud = new SolicitudBuilder().Construir();
            db.Solicitudes.Add(solicitud);
            db.SaveChanges();
            id = solicitud.Id;
            ApuntarRutas(ruta, id, ficheroCv, null);
        }

        MigradorAdjuntos.Migrar(ruta, registro, adjuntos);

        Assert.True(File.Exists(ficheroCv));
        Assert.Equal(ficheroCv, LeerRutas(ruta, id).Cv);
        Assert.Contains(ficheroCv, File.ReadAllText(registro));

        using var db2 = new AppDbContext(ruta);
        Assert.Empty(db2.Set<DocumentosSolicitud>());
    }

    [Fact]
    public void Migrar_UnaCarpetaHermanaConPrefijoComun_NoSeLee()
    {
        string padre = TestDb.NuevaCarpeta();
        string adjuntos = Path.Combine(padre, "adjuntos");
        string hermana = Path.Combine(padre, "adjuntos-otro");
        Directory.CreateDirectory(adjuntos);
        Directory.CreateDirectory(hermana);
        string ficheroCv = CrearFichero(hermana, NombreCv, [7]);
        string registro = Path.Combine(padre, NombreRegistro);
        int id;
        string ruta;

        using (var db = TestDb.NuevoContexto(out ruta))
        {
            var solicitud = new SolicitudBuilder().Construir();
            db.Solicitudes.Add(solicitud);
            db.SaveChanges();
            id = solicitud.Id;
            ApuntarRutas(ruta, id, ficheroCv, null);
        }

        MigradorAdjuntos.Migrar(ruta, registro, adjuntos);

        Assert.True(File.Exists(ficheroCv));
        Assert.Equal(ficheroCv, LeerRutas(ruta, id).Cv);
    }

    [Fact]
    public void Migrar_UnaRutaConPuntosSuspensivosQueSaleDeLaCarpeta_NoSeLee()
    {
        string padre = TestDb.NuevaCarpeta();
        string adjuntos = Path.Combine(padre, "adjuntos");
        Directory.CreateDirectory(adjuntos);
        string ficheroFuera = CrearFichero(padre, NombreCv, [5]);
        string registro = Path.Combine(padre, NombreRegistro);
        string rutaCv = Path.Combine(adjuntos, "..", NombreCv);
        int id;
        string ruta;

        using (var db = TestDb.NuevoContexto(out ruta))
        {
            var solicitud = new SolicitudBuilder().Construir();
            db.Solicitudes.Add(solicitud);
            db.SaveChanges();
            id = solicitud.Id;
            ApuntarRutas(ruta, id, rutaCv, null);
        }

        MigradorAdjuntos.Migrar(ruta, registro, adjuntos);

        Assert.True(File.Exists(ficheroFuera));
        Assert.Equal(rutaCv, LeerRutas(ruta, id).Cv);
        Assert.Contains(rutaCv, File.ReadAllText(registro));
    }

    [Fact]
    public void Migrar_SiNoPuedeBorrarUnFicheroLoRegistraYNoInterrumpe()
    {
        string carpeta = TestDb.NuevaCarpeta();
        string ficheroCv = CrearFichero(carpeta, NombreCv, [1]);
        string ficheroCarta = CrearFichero(carpeta, "carta.pdf", [2]);
        string registro = Path.Combine(TestDb.NuevaCarpeta(), NombreRegistro);
        string ruta;

        using (var db = TestDb.NuevoContexto(out ruta))
        {
            var solicitud = new SolicitudBuilder().Construir();
            db.Solicitudes.Add(solicitud);
            db.SaveChanges();
            ApuntarRutas(ruta, solicitud.Id, ficheroCv, ficheroCarta);
        }

        // Sin FileShare.Delete, Windows no deja borrar el fichero mientras está abierto.
        using (new FileStream(ficheroCv, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            MigradorAdjuntos.Migrar(ruta, registro, carpeta);
        }

        Assert.True(File.Exists(ficheroCv));
        Assert.False(File.Exists(ficheroCarta));
        Assert.True(Directory.Exists(carpeta));
        Assert.Contains("no se pudo borrar", File.ReadAllText(registro));

        using var db2 = new AppDbContext(ruta);
        Assert.Equal([1], db2.Set<DocumentosSolicitud>().Single().Cv);
    }

    [Fact]
    public void Migrar_ConservaElNombreOriginalQueYaHabia()
    {
        string carpeta = TestDb.NuevaCarpeta();
        string ficheroCv = CrearFichero(carpeta, NombreCv, [1]);
        string ruta;

        using (var db = TestDb.NuevoContexto(out ruta))
        {
            var solicitud = new SolicitudBuilder()
                .ConNombreOriginalCv("mi curriculum.pdf")
                .Construir();
            db.Solicitudes.Add(solicitud);
            db.SaveChanges();
            ApuntarRutas(ruta, solicitud.Id, ficheroCv, null);
        }

        MigradorAdjuntos.Migrar(ruta, Path.Combine(carpeta, NombreRegistro), carpeta);

        using var db2 = new AppDbContext(ruta);
        Assert.Equal("mi curriculum.pdf", db2.Solicitudes.Single().NombreOriginalCv);
    }

    [Fact]
    public void Migrar_FicheroQueYaNoExisteDejaElDocumentoVacioYLoRegistra()
    {
        string carpeta = TestDb.NuevaCarpeta();
        string registro = Path.Combine(TestDb.NuevaCarpeta(), NombreRegistro);
        string rutaCv = Path.Combine(carpeta, "ya-no-esta.pdf");
        string ruta;

        using (var db = TestDb.NuevoContexto(out ruta))
        {
            var solicitud = new SolicitudBuilder()
                .ConNombreOriginalCv(NombreCv)
                .Construir();
            db.Solicitudes.Add(solicitud);
            db.SaveChanges();
            ApuntarRutas(ruta, solicitud.Id, rutaCv, null);
        }

        MigradorAdjuntos.Migrar(ruta, registro, carpeta);

        using var db2 = new AppDbContext(ruta);
        Solicitud migrada = db2.Solicitudes.Single();
        Assert.Null(migrada.NombreOriginalCv);
        Assert.Empty(db2.Set<DocumentosSolicitud>());
        Assert.Contains("no se pudo migrar el CV", File.ReadAllText(registro));
    }

    [Fact]
    public void Migrar_FicheroQueExistePeroNoSePuedeLeer_ConservaLaRutaYLoReintentaEnElSiguienteArranque()
    {
        string carpeta = TestDb.NuevaCarpeta();
        string ficheroCv = CrearFichero(carpeta, NombreCv, [1, 2, 3]);
        string registro = Path.Combine(TestDb.NuevaCarpeta(), NombreRegistro);
        int id;
        string ruta;

        using (var db = TestDb.NuevoContexto(out ruta))
        {
            var solicitud = new SolicitudBuilder()
                .ConNombreOriginalCv(NombreCv)
                .Construir();
            db.Solicitudes.Add(solicitud);
            db.SaveChanges();
            id = solicitud.Id;
            ApuntarRutas(ruta, id, ficheroCv, null);
        }

        using (new FileStream(ficheroCv, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            MigradorAdjuntos.Migrar(ruta, registro, carpeta);
        }

        using (var db2 = new AppDbContext(ruta))
        {
            Assert.Equal(NombreCv, db2.Solicitudes.Single().NombreOriginalCv);
            Assert.Empty(db2.Set<DocumentosSolicitud>());
        }

        Assert.Equal(ficheroCv, LeerRutas(ruta, id).Cv);
        Assert.True(File.Exists(ficheroCv));
        Assert.Contains(ficheroCv, File.ReadAllText(registro));

        // Sin el bloqueo, el siguiente arranque la reintenta.
        MigradorAdjuntos.Migrar(ruta, registro, carpeta);

        using var db3 = new AppDbContext(ruta);
        Assert.Equal([1, 2, 3], db3.Set<DocumentosSolicitud>().Single().Cv);
        Assert.Null(LeerRutas(ruta, id).Cv);
        Assert.False(File.Exists(ficheroCv));
    }

    [Fact]
    public void Migrar_UnDocumentoIlegible_NoImpideMigrarElOtro()
    {
        string carpeta = TestDb.NuevaCarpeta();
        string ficheroCv = CrearFichero(carpeta, NombreCv, [1, 2]);
        string ficheroCarta = CrearFichero(carpeta, "carta.pdf", [3, 4]);
        string registro = Path.Combine(TestDb.NuevaCarpeta(), NombreRegistro);
        int id;
        string ruta;

        using (var db = TestDb.NuevoContexto(out ruta))
        {
            var solicitud = new SolicitudBuilder().Construir();
            db.Solicitudes.Add(solicitud);
            db.SaveChanges();
            id = solicitud.Id;
            ApuntarRutas(ruta, id, ficheroCv, ficheroCarta);
        }

        using (new FileStream(ficheroCarta, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            MigradorAdjuntos.Migrar(ruta, registro, carpeta);
        }

        using (var db2 = new AppDbContext(ruta))
        {
            DocumentosSolicitud documentos = db2.Set<DocumentosSolicitud>().Single(d => d.SolicitudId == id);
            Assert.Equal([1, 2], documentos.Cv);
            Assert.Null(documentos.Carta);

            Solicitud migrada = db2.Solicitudes.Single();
            Assert.Equal(NombreCv, migrada.NombreOriginalCv);
            Assert.Null(migrada.NombreOriginalCarta);
        }

        Assert.Null(LeerRutas(ruta, id).Cv);
        Assert.Equal(ficheroCarta, LeerRutas(ruta, id).Carta);
        Assert.False(File.Exists(ficheroCv));
        Assert.True(File.Exists(ficheroCarta));
        Assert.Contains(ficheroCarta, File.ReadAllText(registro));
    }

    [Fact]
    public void Migrar_FicheroDeExactamenteElLimite_SeMigra()
    {
        string carpeta = TestDb.NuevaCarpeta();
        string ficheroCv = CrearFichero(carpeta, NombreCv, new byte[AdjuntosHelper.TamanoMaximoBytes]);
        string registro = Path.Combine(carpeta, NombreRegistro);
        int id;
        string ruta;

        using (var db = TestDb.NuevoContexto(out ruta))
        {
            var solicitud = new SolicitudBuilder().Construir();
            db.Solicitudes.Add(solicitud);
            db.SaveChanges();
            id = solicitud.Id;
            ApuntarRutas(ruta, id, ficheroCv, null);
        }

        MigradorAdjuntos.Migrar(ruta, registro, carpeta);

        using var db2 = new AppDbContext(ruta);
        DocumentosSolicitud documentos = db2.Set<DocumentosSolicitud>().Single(d => d.SolicitudId == id);
        Assert.Equal(AdjuntosHelper.TamanoMaximoBytes, documentos.Cv!.Length);
        Assert.Null(LeerRutas(ruta, id).Cv);
        Assert.False(File.Exists(ficheroCv));
    }

    [Fact]
    public void Migrar_FicheroQueSuperaElLimite_QuedaPendienteSinBlobYConservaElFichero()
    {
        string carpeta = TestDb.NuevaCarpeta();
        string ficheroCv = CrearFichero(carpeta, NombreCv, new byte[AdjuntosHelper.TamanoMaximoBytes + 1]);
        string registro = Path.Combine(TestDb.NuevaCarpeta(), NombreRegistro);
        int id;
        string ruta;

        using (var db = TestDb.NuevoContexto(out ruta))
        {
            var solicitud = new SolicitudBuilder().Construir();
            db.Solicitudes.Add(solicitud);
            db.SaveChanges();
            id = solicitud.Id;
            ApuntarRutas(ruta, id, ficheroCv, null);
        }

        IReadOnlyList<string> pendientes = MigradorAdjuntos.Migrar(ruta, registro, carpeta);

        using (var db2 = new AppDbContext(ruta))
        {
            Assert.Empty(db2.Set<DocumentosSolicitud>());
        }

        Assert.Equal([ficheroCv], pendientes);
        Assert.Equal(ficheroCv, LeerRutas(ruta, id).Cv);
        Assert.True(File.Exists(ficheroCv));
        Assert.Contains(ficheroCv, File.ReadAllText(registro));
    }

    [Fact]
    public void Migrar_UnDocumentoDemasiadoGrande_NoImpideMigrarElOtro()
    {
        string carpeta = TestDb.NuevaCarpeta();
        string ficheroCv = CrearFichero(carpeta, NombreCv, new byte[AdjuntosHelper.TamanoMaximoBytes + 1]);
        string ficheroCarta = CrearFichero(carpeta, "carta.pdf", [3, 4]);
        string registro = Path.Combine(TestDb.NuevaCarpeta(), NombreRegistro);
        int id;
        string ruta;

        using (var db = TestDb.NuevoContexto(out ruta))
        {
            var solicitud = new SolicitudBuilder().Construir();
            db.Solicitudes.Add(solicitud);
            db.SaveChanges();
            id = solicitud.Id;
            ApuntarRutas(ruta, id, ficheroCv, ficheroCarta);
        }

        IReadOnlyList<string> pendientes = MigradorAdjuntos.Migrar(ruta, registro, carpeta);

        using (var db2 = new AppDbContext(ruta))
        {
            DocumentosSolicitud documentos = db2.Set<DocumentosSolicitud>().Single(d => d.SolicitudId == id);
            Assert.Null(documentos.Cv);
            Assert.Equal([3, 4], documentos.Carta);
        }

        Assert.Equal([ficheroCv], pendientes);
        Assert.Equal(ficheroCv, LeerRutas(ruta, id).Cv);
        Assert.Null(LeerRutas(ruta, id).Carta);
        Assert.True(File.Exists(ficheroCv));
        Assert.False(File.Exists(ficheroCarta));
    }

    [Fact]
    public void Migrar_DosVeces_NoCambiaNadaLaSegundaVez()
    {
        string carpeta = TestDb.NuevaCarpeta();
        string ficheroCv = CrearFichero(carpeta, NombreCv, [7, 8]);
        string registro = Path.Combine(carpeta, NombreRegistro);
        string ruta;

        using (var db = TestDb.NuevoContexto(out ruta))
        {
            var solicitud = new SolicitudBuilder().Construir();
            db.Solicitudes.Add(solicitud);
            db.SaveChanges();
            ApuntarRutas(ruta, solicitud.Id, ficheroCv, null);
        }

        MigradorAdjuntos.Migrar(ruta, registro, carpeta);
        MigradorAdjuntos.Migrar(ruta, registro, carpeta);

        using var db2 = new AppDbContext(ruta);
        DocumentosSolicitud documentos = db2.Set<DocumentosSolicitud>().Single();
        Assert.Equal([7, 8], documentos.Cv);
        Assert.Equal(NombreCv, db2.Solicitudes.Single().NombreOriginalCv);
        Assert.False(File.Exists(registro));
        Assert.False(File.Exists(ficheroCv));
    }

    [Fact]
    public void Migrar_BaseSinColumnasDeRuta_NoHaceNada()
    {
        string registro = Path.Combine(TestDb.NuevaCarpeta(), NombreRegistro);
        string ruta;

        using (var db = TestDb.NuevoContexto(out ruta))
        {
            db.Solicitudes.Add(new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir());
            db.SaveChanges();
        }

        MigradorAdjuntos.Migrar(ruta, registro);

        using var db2 = new AppDbContext(ruta);
        Assert.Empty(db2.Set<DocumentosSolicitud>());
        Assert.False(File.Exists(registro));
    }

    /// <summary>
    /// Simula una base de la versión anterior: añade las columnas de ruta y las rellena por SQL,
    /// fuera del modelo actual.
    /// </summary>
    private static void ApuntarRutas(string rutaBd, int id, string? cv, string? carta)
    {
        using var conexion = new SqliteConnection(AppDbContext.CadenaConexion(rutaBd));
        conexion.Open();

        using (var columnas = conexion.CreateCommand())
        {
            columnas.CommandText = """
                ALTER TABLE Solicitudes ADD COLUMN RutaCv TEXT NULL;
                ALTER TABLE Solicitudes ADD COLUMN RutaCarta TEXT NULL;
                """;
            columnas.ExecuteNonQuery();
        }

        using var comando = conexion.CreateCommand();
        comando.CommandText = "UPDATE Solicitudes SET RutaCv = $cv, RutaCarta = $carta WHERE Id = $id;";
        comando.Parameters.AddWithValue("$cv", (object?)cv ?? DBNull.Value);
        comando.Parameters.AddWithValue("$carta", (object?)carta ?? DBNull.Value);
        comando.Parameters.AddWithValue("$id", id);
        comando.ExecuteNonQuery();
    }

    private static string CrearFichero(string carpeta, string nombre, byte[] contenido)
    {
        string ruta = Path.Combine(carpeta, nombre);
        File.WriteAllBytes(ruta, contenido);
        return ruta;
    }

    private static (string? Cv, string? Carta) LeerRutas(string rutaBd, int id)
    {
        using var conexion = new SqliteConnection(AppDbContext.CadenaConexion(rutaBd));
        conexion.Open();
        using var comando = conexion.CreateCommand();
        comando.CommandText = "SELECT RutaCv, RutaCarta FROM Solicitudes WHERE Id = $id;";
        comando.Parameters.AddWithValue("$id", id);
        using var lector = comando.ExecuteReader();
        lector.Read();
        return (
            lector.IsDBNull(0) ? null : lector.GetString(0),
            lector.IsDBNull(1) ? null : lector.GetString(1));
    }
}
