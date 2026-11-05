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
            var solicitud = new SolicitudBuilder()
                .ConEmpresa("ACME")
                .ConPuesto("Dev")
                .ConEstado(EstadoSolicitud.OfertaAceptada)
                .Construir();
            solicitud.AgregarEvento(new Evento(DateTime.Today, TipoEvento.Oferta, "¡Sí!"));
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
        Assert.Equal(DateTime.Today, evento.Fecha);
        Assert.Equal("¡Sí!", evento.Descripcion);
    }

    [Fact]
    public void ActualizarEvento_SeGuardaYRecuperaCompleto()
    {
        // El acceso por campo debe servir también para leer, mutar y volver a guardar.
        int id;
        string ruta;
        using (var db = TestDb.NuevoContexto(out ruta))
        {
            var solicitud = new SolicitudBuilder()
                .ConEmpresa("ACME")
                .ConPuesto("Dev")
                .ConEnlaceOferta("https://ejemplo.test/oferta")
                .ConPortal("LinkedIn")
                .ConOrigen(Origen.Referido)
                .ConUbicacion("Madrid")
                .ConModalidad(Modalidad.Remoto)
                .ConTecnologias("C#, .NET")
                .ConRequisitos("Experiencia con EF Core")
                .ConSalarioMin(30000)
                .ConSalarioMax(45000)
                .ConPretensionSalarial(40000)
                .ConInteres(5)
                .ConFechaSolicitud(new DateTime(2026, 4, 10))
                .ConFechaPrimeraRespuesta(new DateTime(2026, 4, 12))
                .ConFechaEntrevista(new DateTime(2026, 4, 16))
                .ConEstado(EstadoSolicitud.Rechazada)
                .ConMotivoRechazo(MotivoRechazo.SeleccionaronOtroCandidato)
                .ConFechaCierre(new DateTime(2026, 5, 1))
                .ConProximoSeguimiento(new DateTime(2026, 4, 20))
                .ConRespuestaEmpresa("Seguimos en contacto")
                .ConContactoNombre("Ana")
                .ConContactoEmail("ana@ejemplo.test")
                .ConContactoTelefono("600123456")
                .ConNotas("Hablar el lunes")
                .ConEventos(new Evento(DateTime.Today, TipoEvento.Nota, "Antes"))
                .Construir();
            db.Solicitudes.Add(solicitud);
            db.SaveChanges();
            id = solicitud.Id;
        }

        using (var db = new AppDbContext(ruta))
        {
            Evento evento = db.Solicitudes.Include(s => s.Eventos).Single(s => s.Id == id).Eventos[0];
            evento.Actualizar(new DateTime(2026, 5, 1), TipoEvento.Oferta, "Después");
            db.SaveChanges();
        }

        using var db2 = new AppDbContext(ruta);
        Solicitud solicitudRecuperada = db2.Solicitudes.Include(s => s.Eventos).Single(s => s.Id == id);
        Assert.Equal("ACME", solicitudRecuperada.Empresa);
        Assert.Equal("Dev", solicitudRecuperada.Puesto);
        Assert.Equal("https://ejemplo.test/oferta", solicitudRecuperada.EnlaceOferta);
        Assert.Equal("LinkedIn", solicitudRecuperada.Portal);
        Assert.Equal(Origen.Referido, solicitudRecuperada.Origen);
        Assert.Equal("Madrid", solicitudRecuperada.Ubicacion);
        Assert.Equal(Modalidad.Remoto, solicitudRecuperada.Modalidad);
        Assert.Equal("C#, .NET", solicitudRecuperada.Tecnologias);
        Assert.Equal("Experiencia con EF Core", solicitudRecuperada.Requisitos);
        Assert.Equal(30000, solicitudRecuperada.SalarioMin);
        Assert.Equal(45000, solicitudRecuperada.SalarioMax);
        Assert.Equal(40000, solicitudRecuperada.PretensionSalarial);
        Assert.Equal(5, solicitudRecuperada.Interes);
        Assert.Equal(new DateTime(2026, 4, 10), solicitudRecuperada.FechaSolicitud);
        Assert.Equal(new DateTime(2026, 4, 12), solicitudRecuperada.FechaPrimeraRespuesta);
        Assert.Equal(new DateTime(2026, 4, 16), solicitudRecuperada.FechaEntrevista);
        Assert.Equal(new DateTime(2026, 5, 1), solicitudRecuperada.FechaCierre);
        Assert.Equal(new DateTime(2026, 4, 20), solicitudRecuperada.ProximoSeguimiento);
        Assert.Equal(EstadoSolicitud.Rechazada, solicitudRecuperada.Estado);
        Assert.Equal(MotivoRechazo.SeleccionaronOtroCandidato, solicitudRecuperada.MotivoRechazo);
        Assert.Equal("Seguimos en contacto", solicitudRecuperada.RespuestaEmpresa);
        Assert.Equal("Ana", solicitudRecuperada.ContactoNombre);
        Assert.Equal("ana@ejemplo.test", solicitudRecuperada.ContactoEmail);
        Assert.Equal("600123456", solicitudRecuperada.ContactoTelefono);
        Assert.Equal("Hablar el lunes", solicitudRecuperada.Notas);

        Evento recuperado = solicitudRecuperada.Eventos[0];
        Assert.Equal(new DateTime(2026, 5, 1), recuperado.Fecha);
        Assert.Equal(TipoEvento.Oferta, recuperado.Tipo);
        Assert.Equal("Después", recuperado.Descripcion);
    }

    [Fact]
    public void BorrarSolicitud_EliminaSusEventosEnCascada()
    {
        int id;
        string ruta;
        using (var db = TestDb.NuevoContexto(out ruta))
        {
            var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir();
            solicitud.AgregarEvento(new Evento(DateTime.Today, TipoEvento.Nota, string.Empty));
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
