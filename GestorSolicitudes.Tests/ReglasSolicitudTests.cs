using GestorSolicitudes.Helpers;
using GestorSolicitudes.Models;

namespace GestorSolicitudes.Tests;

public class ReglasSolicitudTests
{
    [Fact]
    public void TieneDatosObligatorios_VerdeCuandoEmpresaYPuestoLlenos()
    {
        var solicitud = new Solicitud { Empresa = "ACME", Puesto = "Dev" };

        Assert.True(ReglasSolicitud.TieneDatosObligatorios(solicitud));
    }

    [Fact]
    public void TieneDatosObligatorios_RojoCuandoEmpresaVacia()
    {
        var solicitud = new Solicitud { Empresa = "", Puesto = "Dev" };

        Assert.False(ReglasSolicitud.TieneDatosObligatorios(solicitud));
    }

    [Fact]
    public void TieneDatosObligatorios_RojoCuandoPuestoVacio()
    {
        var solicitud = new Solicitud { Empresa = "ACME", Puesto = "" };

        Assert.False(ReglasSolicitud.TieneDatosObligatorios(solicitud));
    }

    [Fact]
    public void TieneDatosObligatorios_RojoCuandoAmbosVacios()
    {
        var solicitud = new Solicitud { Empresa = "", Puesto = "" };

        Assert.False(ReglasSolicitud.TieneDatosObligatorios(solicitud));
    }

    [Fact]
    public void ProponerSeguimiento_DaFechaCuandoEstaAbiertaYSinFecha()
    {
        var solicitud = new Solicitud
        {
            Estado = EstadoSolicitud.Enviada,
            FechaSolicitud = new DateTime(2026, 1, 10),
        };

        var resultado = ReglasSolicitud.ProponerSeguimiento(solicitud);

        Assert.Equal(new DateTime(2026, 1, 17), resultado);
    }

    [Fact]
    public void ProponerSeguimiento_NuloCuandoNoEstaAbierta()
    {
        var solicitud = new Solicitud
        {
            Estado = EstadoSolicitud.OfertaAceptada,  // hace que EstaAbierta sea false
            FechaSolicitud = new DateTime(2026, 1, 10),
        };

        Assert.Null(ReglasSolicitud.ProponerSeguimiento(solicitud));
    }

    [Fact]
    public void ProponerSeguimiento_NuloCuandoYaTieneFecha()
    {
        var solicitud = new Solicitud
        {
            Estado = EstadoSolicitud.Enviada,
            FechaSolicitud = new DateTime(2026, 1, 10),
            ProximoSeguimiento = new DateTime(2026, 1, 20),
        };

        Assert.Null(ReglasSolicitud.ProponerSeguimiento(solicitud));
    }

    [Fact]
    public void AnadirHitoInicial_AñadeEventoCuandoVacio()
    {
        var solicitud = new Solicitud { FechaSolicitud = new DateTime(2026, 3, 1) };

        ReglasSolicitud.AnadirHitoInicial(solicitud);

        Assert.Single(solicitud.Eventos);
        Assert.Equal(TipoEvento.SolicitudEnviada, solicitud.Eventos[0].Tipo);
        Assert.Equal(new DateTime(2026, 3, 1), solicitud.Eventos[0].Fecha);
    }

    [Fact]
    public void AnadirHitoInicial_NoDuplicaCuandoYaTieneEventos()
    {
        var solicitud = new Solicitud
        {
            FechaSolicitud = new DateTime(2026, 3, 1),
            Eventos = { new Evento { Fecha = new DateTime(2026, 3, 1), Tipo = TipoEvento.SolicitudEnviada } },
        };

        ReglasSolicitud.AnadirHitoInicial(solicitud);

        Assert.Single(solicitud.Eventos);
    }
}