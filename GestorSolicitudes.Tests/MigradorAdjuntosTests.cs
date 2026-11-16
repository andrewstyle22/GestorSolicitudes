namespace GestorSolicitudes.Tests;

using System.IO;
using GestorSolicitudes.Data;
using GestorSolicitudes.Models;

public class MigradorAdjuntosTests
{
    private const string NombreCv = "cv.pdf";
    private const string NombreRegistro = "errores.log";

    [Fact]
    public void Migrar_CopiaLosDocumentosYDejaLasRutasAnuladas()
    {
        string carpeta = TestDb.NuevaCarpeta();
        string ficheroCv = CrearFichero(carpeta, NombreCv, [1, 2, 3]);
        string ficheroCarta = CrearFichero(carpeta, "carta.pdf", [4, 5]);
        string registro = Path.Combine(carpeta, NombreRegistro);
        int id;
        string ruta;

        using (var db = TestDb.NuevoContexto(out ruta))
        {
            var solicitud = new SolicitudBuilder()
                .ConRutaCv(ficheroCv)
                .ConRutaCarta(ficheroCarta)
                .Construir();
            db.Solicitudes.Add(solicitud);
            db.SaveChanges();
            id = solicitud.Id;
        }

        MigradorAdjuntos.Migrar(ruta, registro);

        using var db2 = new AppDbContext(ruta);
        Solicitud migrada = db2.Solicitudes.Single(s => s.Id == id);
        Assert.Null(migrada.RutaCv);
        Assert.Null(migrada.RutaCarta);
        Assert.Equal(NombreCv, migrada.NombreOriginalCv);
        Assert.Equal("carta.pdf", migrada.NombreOriginalCarta);

        DocumentosSolicitud documentos = db2.Set<DocumentosSolicitud>().Single(d => d.SolicitudId == id);
        Assert.Equal([1, 2, 3], documentos.Cv);
        Assert.Equal([4, 5], documentos.Carta);
        Assert.False(File.Exists(registro));
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
                .ConRutaCv(ficheroCv)
                .ConNombreOriginalCv("mi curriculum.pdf")
                .Construir();
            db.Solicitudes.Add(solicitud);
            db.SaveChanges();
        }

        MigradorAdjuntos.Migrar(ruta, Path.Combine(carpeta, NombreRegistro));

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
                .ConRutaCv(rutaCv)
                .ConNombreOriginalCv(NombreCv)
                .Construir();
            db.Solicitudes.Add(solicitud);
            db.SaveChanges();
        }

        MigradorAdjuntos.Migrar(ruta, registro);

        using var db2 = new AppDbContext(ruta);
        Solicitud migrada = db2.Solicitudes.Single();
        Assert.Null(migrada.RutaCv);
        Assert.Null(migrada.NombreOriginalCv);
        Assert.Empty(db2.Set<DocumentosSolicitud>());
        Assert.Contains("no se pudo migrar el CV", File.ReadAllText(registro));
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
            var solicitud = new SolicitudBuilder().ConRutaCv(ficheroCv).Construir();
            db.Solicitudes.Add(solicitud);
            db.SaveChanges();
        }

        MigradorAdjuntos.Migrar(ruta, registro);
        MigradorAdjuntos.Migrar(ruta, registro);

        using var db2 = new AppDbContext(ruta);
        DocumentosSolicitud documentos = db2.Set<DocumentosSolicitud>().Single();
        Assert.Equal([7, 8], documentos.Cv);
        Assert.Equal(NombreCv, db2.Solicitudes.Single().NombreOriginalCv);
        Assert.False(File.Exists(registro));
    }

    private static string CrearFichero(string carpeta, string nombre, byte[] contenido)
    {
        string ruta = Path.Combine(carpeta, nombre);
        File.WriteAllBytes(ruta, contenido);
        return ruta;
    }
}
