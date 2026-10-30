namespace GestorSolicitudes.Tests;

using System.IO;
using GestorSolicitudes.Data;
using GestorSolicitudes.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public class AppDbContextTests
{
    [Fact]
    public void Guardar_SolicitudConEventos_SeRecuperaCompleta()
    {
        int id;
        string ruta;
        using (var db = TestDb.NuevoContexto(out ruta))
        {
            var solicitud = new Solicitud
            {
                Empresa = "ACME",
                Puesto = "Dev",
                Estado = EstadoSolicitud.OfertaAceptada,
            };
            solicitud.Eventos.Add(new Evento { Fecha = DateTime.Today, Tipo = TipoEvento.Oferta, Descripcion = "¡Sí!" });
            db.Solicitudes.Add(solicitud);
            db.SaveChanges();
            id = solicitud.Id;
        }

        using var db2 = new AppDbContext(ruta);
        Solicitud? recuperada = db2.Solicitudes.Include(s => s.Eventos).FirstOrDefault(s => s.Id == id);
        Assert.NotNull(recuperada);
        Assert.Equal("ACME", recuperada!.Empresa);
        Assert.Equal(EstadoSolicitud.OfertaAceptada, recuperada.Estado);
        Evento evento = Assert.Single(recuperada.Eventos);
        Assert.Equal(TipoEvento.Oferta, evento.Tipo);
    }

    [Fact]
    public void BorrarSolicitud_EliminaSusEventosEnCascada()
    {
        int id;
        string ruta;
        using (var db = TestDb.NuevoContexto(out ruta))
        {
            var solicitud = new Solicitud { Empresa = "ACME", Puesto = "Dev" };
            solicitud.Eventos.Add(new Evento { Fecha = DateTime.Today, Tipo = TipoEvento.Nota });
            db.Solicitudes.Add(solicitud);
            db.SaveChanges();
            id = solicitud.Id;
        }

        using (var db = new AppDbContext(ruta))
        {
            db.Solicitudes.Remove(db.Solicitudes.Include(s => s.Eventos).Single(s => s.Id == id));
            db.SaveChanges();
        }

        using var db2 = new AppDbContext(ruta);
        Assert.Equal(0, db2.Eventos.Count(e => e.SolicitudId == id));
    }

    [Fact]
    public void RutaBaseDatos_ApuntaALaBaseRealDeLaApp()
    {
        // Leer la propiedad estática cubre el inicializador (cctor); no abre la base.
        string ruta = AppDbContext.RutaBaseDatos;
        Assert.Contains("GestorSolicitudes", ruta);
        Assert.EndsWith("solicitudes.db", ruta);
    }

    [Fact]
    public void OnConfiguring_RutaSinCarpeta_OmitelaCreacionDelDirectorio()
    {
        // Una ruta sin carpeta (solo nombre) entra por la rama false del if de OnConfiguring.
        string ruta = "base-solo-nombre.db";
        try
        {
            using var db = new AppDbContext(ruta);
            db.Database.EnsureCreated();
            Assert.True(File.Exists(ruta));
        }
        finally
        {
            // El pool de conexiones de SQLite mantiene el fichero abierto tras el
            // Dispose del contexto: se vacía antes de poder borrar el archivo.
            SqliteConnection.ClearAllPools();
            File.Delete(ruta);
        }
    }
}
