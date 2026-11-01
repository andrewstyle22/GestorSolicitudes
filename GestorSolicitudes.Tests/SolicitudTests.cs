namespace GestorSolicitudes.Tests;

using GestorSolicitudes.Models;

public class SolicitudTests
{
    [Fact]
    public void DiasDesdeSolicitud_CalculaDiasBien()
    {
        var solicitud = new SolicitudBuilder().ConFechaSolicitud(DateTime.Today.AddDays(-5)).Construir();
        Assert.Equal(5, solicitud.DiasDesdeSolicitud);
    }

    [Fact]
    public void DiasProceso_AbiertaCoincideConDiasDesdeSolicitud()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Enviada)
            .ConFechaSolicitud(DateTime.Today.AddDays(-5))
            .Construir();
        Assert.Equal(solicitud.DiasDesdeSolicitud, solicitud.DiasProceso);
    }

    [Fact]
    public void DiasProceso_CerradaMuestraLaDuracionHastaElCierre()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConFechaSolicitud(new DateTime(2026, 1, 10))
            .ConFechaCierre(new DateTime(2026, 1, 21))
            .Construir();
        Assert.Equal(11, solicitud.DiasProceso);
    }

    [Fact]
    public void DiasProceso_CerradaSinFechaDeCierreCaeAHoy()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConFechaSolicitud(DateTime.Today.AddDays(-3))
            .ConFechaCierre(null)
            .Construir();
        Assert.Equal(3, solicitud.DiasProceso);
    }

    [Fact]
    public void MotivoRechazo_PorDefectoEsNulo()
    {
        Assert.Null(new SolicitudBuilder().Construir().MotivoRechazo);
    }

    [Fact]
    public void Origen_PorDefectoEsAplicacionDirecta()
    {
        Assert.Equal(Origen.AplicacionDirecta, new SolicitudBuilder().Construir().Origen);
    }

    [Fact]
    public void DiasHastaRespuesta_ConRespuesta_DevuelveDias()
    {
        var baseFecha = new DateTime(2026, 1, 10);
        var solicitud = new SolicitudBuilder()
            .ConFechaSolicitud(baseFecha)
            .ConFechaPrimeraRespuesta(baseFecha.AddDays(3))
            .Construir();
        Assert.Equal(3, solicitud.DiasHastaRespuesta);
    }

    [Fact]
    public void DiasHastaRespuesta_SinRespuesta_EsNull()
    {
        var solicitud = new SolicitudBuilder().ConFechaSolicitud(DateTime.Today).Construir();
        Assert.Null(solicitud.DiasHastaRespuesta);
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
        var solicitud = new SolicitudBuilder().ConEstado(estado).Construir();
        Assert.Equal(esperado, solicitud.EstaAbierta);
    }

    [Theory]
    [InlineData(EstadoSolicitud.PruebaTecnica, true)]
    [InlineData(EstadoSolicitud.EntrevistaRrhh, true)]
    [InlineData(EstadoSolicitud.EntrevistaTecnica, true)]
    [InlineData(EstadoSolicitud.EntrevistaFinal, true)]
    [InlineData(EstadoSolicitud.Enviada, false)]
    [InlineData(EstadoSolicitud.OfertaRecibida, false)]
    public void EsEntrevista_SoloIncluyeLosEstadosDeEntrevista(EstadoSolicitud estado, bool esperado)
    {
        Assert.Equal(esperado, estado.EsEntrevista());
    }

    [Theory]
    [InlineData(EstadoSolicitud.OfertaRecibida, true)]
    [InlineData(EstadoSolicitud.OfertaAceptada, true)]
    [InlineData(EstadoSolicitud.OfertaRechazada, true)]
    [InlineData(EstadoSolicitud.Enviada, false)]
    [InlineData(EstadoSolicitud.EntrevistaFinal, false)]
    public void EsOferta_SoloIncluyeLosEstadosDeOferta(EstadoSolicitud estado, bool esperado)
    {
        Assert.Equal(esperado, estado.EsOferta());
    }

    [Theory]
    [InlineData(EstadoSolicitud.OfertaAceptada, true)]
    [InlineData(EstadoSolicitud.OfertaRechazada, true)]
    [InlineData(EstadoSolicitud.Rechazada, true)]
    [InlineData(EstadoSolicitud.Retirada, true)]
    [InlineData(EstadoSolicitud.Enviada, false)]
    [InlineData(EstadoSolicitud.EntrevistaTecnica, false)]
    public void EstaCerrada_SoloIncluyeLosEstadosTerminales(EstadoSolicitud estado, bool esperado)
    {
        Assert.Equal(esperado, estado.EstaCerrada());
    }

    [Fact]
    public void HuboRespuesta_ConFechaEsTrue()
    {
        Assert.True(new SolicitudBuilder().ConFechaPrimeraRespuesta(DateTime.Today).Construir().HuboRespuesta);
    }

    [Fact]
    public void HuboRespuesta_SinFechaEsFalse()
    {
        Assert.False(new SolicitudBuilder().Construir().HuboRespuesta);
    }

    [Fact]
    public void SeguimientoPendiente_AbiertaConFechaVencidaEsTrue()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Enviada)
            .ConProximoSeguimiento(DateTime.Today.AddDays(-1))
            .Construir();
        Assert.True(solicitud.SeguimientoPendiente);
    }

    [Fact]
    public void SeguimientoPendiente_ConFechaFuturaEsFalse()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Enviada)
            .ConProximoSeguimiento(DateTime.Today.AddDays(2))
            .Construir();
        Assert.False(solicitud.SeguimientoPendiente);
    }

    [Fact]
    public void SeguimientoPendiente_CerradaEsFalse()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConProximoSeguimiento(DateTime.Today.AddDays(-1))
            .Construir();
        Assert.False(solicitud.SeguimientoPendiente);
    }

    [Fact]
    public void NombreCv_UsaNombreOriginalSiExiste()
    {
        var solicitud = new SolicitudBuilder()
            .ConNombreOriginalCv("mi-cv-final.pdf")
            .ConRutaCv(@"C:\cualquier\cosa\a1b2.pdf")
            .Construir();
        Assert.Equal("mi-cv-final.pdf", solicitud.NombreCv);
    }

    [Fact]
    public void NombreCv_SinNombreOriginalUsaNombreDelFichero()
    {
        var solicitud = new SolicitudBuilder().ConRutaCv(@"C:\cualquier\cosa\a1b2.pdf").Construir();
        Assert.Equal("a1b2.pdf", solicitud.NombreCv);
    }

    [Fact]
    public void NombreCv_SinRutaEsNull()
    {
        Assert.Null(new SolicitudBuilder().Construir().NombreCv);
    }

    [Fact]
    public void Evento_TrasGuardarExponeIdSolicitudYPropiedadDeNavegacion()
    {
        using var db = TestDb.NuevoContexto();

        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir();
        solicitud.Eventos.Add(new Evento { Fecha = DateTime.Today, Tipo = TipoEvento.Nota });
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();

        Evento evento = solicitud.Eventos.First();
        Assert.True(evento.Id > 0);
        Assert.Equal(solicitud.Id, evento.SolicitudId);
        Assert.Same(solicitud, evento.Solicitud);
    }
}
