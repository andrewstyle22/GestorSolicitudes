using Xunit;
using GestorSolicitudes.Models;

namespace GestorSolicitudes.Tests;

public class SolicitudTests
{
    [Fact]
    public void DiasDesdeSolicitud_CalculaDiasBien()
    {
        var s = new Solicitud { FechaSolicitud = DateTime.Today.AddDays(-5) };
        Assert.Equal(5, s.DiasDesdeSolicitud);
    }

    [Fact]
    public void DiasProceso_AbiertaCoincideConDiasDesdeSolicitud()
    {
        var s = new Solicitud
        {
            Estado = EstadoSolicitud.Enviada,
            FechaSolicitud = DateTime.Today.AddDays(-5)
        };
        Assert.Equal(s.DiasDesdeSolicitud, s.DiasProceso);
    }

    [Fact]
    public void DiasProceso_CerradaMuestraLaDuracionHastaElCierre()
    {
        var s = new Solicitud
        {
            Estado = EstadoSolicitud.Rechazada,
            FechaSolicitud = new DateTime(2026, 1, 10),
            FechaCierre = new DateTime(2026, 1, 21)
        };
        Assert.Equal(11, s.DiasProceso);
    }

    [Fact]
    public void DiasProceso_CerradaSinFechaDeCierreCaeAHoy()
    {
        var s = new Solicitud
        {
            Estado = EstadoSolicitud.Rechazada,
            FechaSolicitud = DateTime.Today.AddDays(-3),
            FechaCierre = null
        };
        Assert.Equal(3, s.DiasProceso);
    }

    [Fact]
    public void MotivoRechazo_PorDefectoEsNulo()
    {
        Assert.Null(new Solicitud().MotivoRechazo);
    }

    [Fact]
    public void Origen_PorDefectoEsAplicacionDirecta()
    {
        Assert.Equal(Origen.AplicacionDirecta, new Solicitud().Origen);
    }

    [Fact]
    public void DiasHastaRespuesta_ConRespuesta_DevuelveDias()
    {
        var baseFecha = new DateTime(2026, 1, 10);
        var s = new Solicitud
        {
            FechaSolicitud = baseFecha,
            FechaPrimeraRespuesta = baseFecha.AddDays(3)
        };
        Assert.Equal(3, s.DiasHastaRespuesta);
    }

    [Fact]
    public void DiasHastaRespuesta_SinRespuesta_EsNull()
    {
        var s = new Solicitud { FechaSolicitud = DateTime.Today };
        Assert.Null(s.DiasHastaRespuesta);
    }

    [Theory]
    [InlineData(EstadoSolicitud.Enviada, true)]
    [InlineData(EstadoSolicitud.EnRevision, true)]
    [InlineData(EstadoSolicitud.SinRespuesta, true)]
    [InlineData(EstadoSolicitud.EntrevistaRrhh, true)]
    [InlineData(EstadoSolicitud.OfertaAceptada, false)]
    [InlineData(EstadoSolicitud.OfertaRechazada, false)]
    [InlineData(EstadoSolicitud.Rechazada, false)]
    [InlineData(EstadoSolicitud.Retirada, false)]
    public void EstaAbierta_SegunEstado(EstadoSolicitud estado, bool esperado)
    {
        var s = new Solicitud { Estado = estado };
        Assert.Equal(esperado, s.EstaAbierta);
    }

    [Fact]
    public void HuboRespuesta_ConFechaEsTrue()
    {
        Assert.True(new Solicitud { FechaPrimeraRespuesta = DateTime.Today }.HuboRespuesta);
    }

    [Fact]
    public void HuboRespuesta_SinFechaEsFalse()
    {
        Assert.False(new Solicitud().HuboRespuesta);
    }

    [Fact]
    public void SeguimientoPendiente_AbiertaConFechaVencidaEsTrue()
    {
        var s = new Solicitud
        {
            Estado = EstadoSolicitud.Enviada,
            ProximoSeguimiento = DateTime.Today.AddDays(-1)
        };
        Assert.True(s.SeguimientoPendiente);
        Assert.True(s.SeguimientoPendiente);
    }

    [Fact]
    public void SeguimientoPendiente_ConFechaFuturaEsFalse()
    {
        var s = new Solicitud
        {
            Estado = EstadoSolicitud.Enviada,
            ProximoSeguimiento = DateTime.Today.AddDays(2)
        };
        Assert.False(s.SeguimientoPendiente);
    }

    [Fact]
    public void SeguimientoPendiente_CerradaEsFalse()
    {
        var s = new Solicitud
        {
            Estado = EstadoSolicitud.Rechazada,
            ProximoSeguimiento = DateTime.Today.AddDays(-1)
        };
        Assert.False(s.SeguimientoPendiente);
    }

    [Fact]
    public void NombreCv_UsaNombreOriginalSiExiste()
    {
        var s = new Solicitud { NombreOriginalCv = "mi-cv-final.pdf", RutaCv = @"C:\cualquier\cosa\a1b2.pdf" };
        Assert.Equal("mi-cv-final.pdf", s.NombreCv);
    }

    [Fact]
    public void NombreCv_SinNombreOriginalUsaNombreDelFichero()
    {
        var s = new Solicitud { RutaCv = @"C:\cualquier\cosa\a1b2.pdf" };
        Assert.Equal("a1b2.pdf", s.NombreCv);
    }

    [Fact]
    public void NombreCv_SinRutaEsNull()
    {
        Assert.Null(new Solicitud().NombreCv);
    }
}