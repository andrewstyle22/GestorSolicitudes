namespace GestorSolicitudes.Tests;

using GestorSolicitudes.Data;
using GestorSolicitudes.Models;
using Microsoft.EntityFrameworkCore;

public class DocumentosSolicitudTests
{
    [Fact]
    public void Documentos_SeGuardanYRecuperan()
    {
        int id;
        string ruta;
        byte[] cv = [1, 2, 3];
        byte[] carta = [4, 5, 6];

        using (var db = TestDb.NuevoContexto(out ruta))
        {
            var solicitud = new SolicitudBuilder().Construir();
            db.Solicitudes.Add(solicitud);
            db.SaveChanges();

            var documentos = new DocumentosSolicitud(solicitud.Id);
            documentos.AsignarCv(cv);
            documentos.AsignarCarta(carta);
            db.Set<DocumentosSolicitud>().Add(documentos);
            db.SaveChanges();
            id = solicitud.Id;
        }

        using var db2 = new AppDbContext(ruta);
        DocumentosSolicitud recuperados = db2.Set<DocumentosSolicitud>().Single();
        Assert.Equal(id, recuperados.SolicitudId);
        Assert.Equal(cv, recuperados.Cv);
        Assert.Equal(carta, recuperados.Carta);
    }

    [Fact]
    public void Quitar_LosDocumentosQuedanNulosYSeGuardaAsi()
    {
        int id;
        string ruta;

        using (var db = TestDb.NuevoContexto(out ruta))
        {
            var solicitud = new SolicitudBuilder().Construir();
            db.Solicitudes.Add(solicitud);
            db.SaveChanges();

            var documentos = new DocumentosSolicitud(solicitud.Id);
            documentos.AsignarCv([1]);
            documentos.AsignarCarta([2]);
            db.Set<DocumentosSolicitud>().Add(documentos);
            db.SaveChanges();

            documentos.QuitarCv();
            documentos.QuitarCarta();
            db.SaveChanges();
            id = solicitud.Id;
        }

        using var db2 = new AppDbContext(ruta);
        DocumentosSolicitud recuperados = db2.Set<DocumentosSolicitud>().Single(d => d.SolicitudId == id);
        Assert.Null(recuperados.Cv);
        Assert.Null(recuperados.Carta);
    }

    [Fact]
    public void BorrarSolicitud_EliminaSusDocumentosEnCascada()
    {
        int id;
        string ruta;

        using (var db = TestDb.NuevoContexto(out ruta))
        {
            var solicitud = new SolicitudBuilder().Construir();
            db.Solicitudes.Add(solicitud);
            db.SaveChanges();

            var documentos = new DocumentosSolicitud(solicitud.Id);
            documentos.AsignarCv([1]);
            db.Set<DocumentosSolicitud>().Add(documentos);
            db.SaveChanges();
            id = solicitud.Id;
        }

        using (var db = new AppDbContext(ruta))
        {
            db.Solicitudes.Remove(db.Solicitudes.Single(s => s.Id == id));
            db.SaveChanges();
        }

        using var db2 = new AppDbContext(ruta);
        Assert.Empty(db2.Set<DocumentosSolicitud>());
    }

    [Fact]
    public void LaConexionActivaLasClavesForaneas()
    {
        using var db = TestDb.NuevoContexto();
        db.Database.OpenConnection();

        using var comando = db.Database.GetDbConnection().CreateCommand();
        comando.CommandText = "PRAGMA foreign_keys;";

        Assert.Equal(1L, Convert.ToInt64(comando.ExecuteScalar()));
    }

    [Fact]
    public void ConsultarSolicitudes_NoLeeLaTablaDeDocumentos()
    {
        using var db = TestDb.NuevoContexto();

        string sql = db.Solicitudes.ToQueryString();

        Assert.DoesNotContain("DocumentosSolicitud", sql, StringComparison.OrdinalIgnoreCase);
    }
}
