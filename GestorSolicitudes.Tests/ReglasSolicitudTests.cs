namespace GestorSolicitudes.Tests;

using GestorSolicitudes.Helpers;
using GestorSolicitudes.Models;

public class ReglasSolicitudTests
{
    [Fact]
    public void TieneDatosObligatorios_VerdeCuandoEmpresaYPuestoLlenos()
    {
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir();

        Assert.True(ReglasSolicitud.TieneDatosObligatorios(solicitud));
    }

    [Fact]
    public void TieneDatosObligatorios_RojoCuandoEmpresaVacia()
    {
        var solicitud = new SolicitudBuilder().ConEmpresa(string.Empty).ConPuesto("Dev").Construir();

        Assert.False(ReglasSolicitud.TieneDatosObligatorios(solicitud));
    }

    [Fact]
    public void TieneDatosObligatorios_RojoCuandoPuestoVacio()
    {
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto(string.Empty).Construir();

        Assert.False(ReglasSolicitud.TieneDatosObligatorios(solicitud));
    }

    [Fact]
    public void TieneDatosObligatorios_RojoCuandoAmbosVacios()
    {
        var solicitud = new SolicitudBuilder().ConEmpresa(string.Empty).ConPuesto(string.Empty).Construir();

        Assert.False(ReglasSolicitud.TieneDatosObligatorios(solicitud));
    }

    [Fact]
    public void ProponerSeguimiento_DaFechaCuandoEstaAbiertaYSinFecha()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Enviada)
            .ConFechaSolicitud(new DateTime(2026, 1, 10))
            .Construir();

        var resultado = ReglasSolicitud.ProponerSeguimiento(solicitud);

        Assert.Equal(new DateTime(2026, 1, 17), resultado);
    }

    [Fact]
    public void ProponerSeguimiento_NuloCuandoNoEstaAbierta()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.OfertaAceptada) // hace que EstaAbierta sea false
            .ConFechaSolicitud(new DateTime(2026, 1, 10))
            .Construir();

        Assert.Null(ReglasSolicitud.ProponerSeguimiento(solicitud));
    }

    [Fact]
    public void ProponerSeguimiento_NuloCuandoYaTieneFecha()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Enviada)
            .ConFechaSolicitud(new DateTime(2026, 1, 10))
            .ConProximoSeguimiento(new DateTime(2026, 1, 20))
            .Construir();

        Assert.Null(ReglasSolicitud.ProponerSeguimiento(solicitud));
    }

    [Fact]
    public void AnadirHitoInicial_AñadeEventoCuandoVacio()
    {
        var solicitud = new SolicitudBuilder().ConFechaSolicitud(new DateTime(2026, 3, 1)).Construir();

        ReglasSolicitud.AnadirHitoInicial(solicitud);

        Assert.Single(solicitud.Eventos);
        Assert.Equal(TipoEvento.SolicitudEnviada, solicitud.Eventos[0].Tipo);
        Assert.Equal(new DateTime(2026, 3, 1), solicitud.Eventos[0].Fecha);
    }

    [Fact]
    public void AnadirHitoInicial_NoDuplicaCuandoYaTieneEventos()
    {
        var solicitud = new SolicitudBuilder()
            .ConFechaSolicitud(new DateTime(2026, 3, 1))
            .ConEventos(new Evento { Fecha = new DateTime(2026, 3, 1), Tipo = TipoEvento.SolicitudEnviada })
            .Construir();

        ReglasSolicitud.AnadirHitoInicial(solicitud);

        Assert.Single(solicitud.Eventos);
    }
}
