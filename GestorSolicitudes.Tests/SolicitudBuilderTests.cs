namespace GestorSolicitudes.Tests;

using GestorSolicitudes.Models;

public class SolicitudBuilderTests
{
    [Fact]
    public void Construir_SinDatos_UsaLosValoresPorDefectoDeLaEntidad()
    {
        Solicitud solicitud = new SolicitudBuilder().Construir();

        Assert.Equal(string.Empty, solicitud.Empresa);
        Assert.Equal(string.Empty, solicitud.Puesto);
        Assert.Equal(EstadoSolicitud.Enviada, solicitud.Estado);
        Assert.Equal(Origen.AplicacionDirecta, solicitud.Origen);
        Assert.Equal(3, solicitud.Interes);
        Assert.Equal(DateTime.Today, solicitud.FechaSolicitud);
        Assert.Empty(solicitud.Eventos);
    }

    [Fact]
    public void Con_AsignaCadaDatoYAcumulaEventos()
    {
        DateTime fecha = new(2026, 1, 10);
        var evento = new Evento { Fecha = fecha, Tipo = TipoEvento.Nota };

        Solicitud solicitud = new SolicitudBuilder()
            .ConEmpresa("ACME")
            .ConPuesto("Dev")
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConFechaSolicitud(fecha)
            .ConPortal("LinkedIn")
            .ConTecnologias("C#, .NET")
            .ConEnlaceOferta("https://ejemplo.test/oferta")
            .ConNotas("nota")
            .ConRutaCv(@"C:\cv.pdf")
            .ConRutaCarta(@"C:\carta.pdf")
            .ConNombreOriginalCv("mi-cv.pdf")
            .ConFechaPrimeraRespuesta(fecha.AddDays(1))
            .ConFechaCierre(fecha.AddDays(2))
            .ConProximoSeguimiento(fecha.AddDays(3))
            .ConEventos(evento)
            .Construir();

        Assert.Equal("ACME", solicitud.Empresa);
        Assert.Equal("Dev", solicitud.Puesto);
        Assert.Equal(EstadoSolicitud.Rechazada, solicitud.Estado);
        Assert.Equal(fecha, solicitud.FechaSolicitud);
        Assert.Equal("LinkedIn", solicitud.Portal);
        Assert.Equal("C#, .NET", solicitud.Tecnologias);
        Assert.Equal("https://ejemplo.test/oferta", solicitud.EnlaceOferta);
        Assert.Equal("nota", solicitud.Notas);
        Assert.Equal(@"C:\cv.pdf", solicitud.RutaCv);
        Assert.Equal(@"C:\carta.pdf", solicitud.RutaCarta);
        Assert.Equal("mi-cv.pdf", solicitud.NombreOriginalCv);
        Assert.Equal(fecha.AddDays(1), solicitud.FechaPrimeraRespuesta);
        Assert.Equal(fecha.AddDays(2), solicitud.FechaCierre);
        Assert.Equal(fecha.AddDays(3), solicitud.ProximoSeguimiento);
        Assert.Same(evento, Assert.Single(solicitud.Eventos));
    }
}
