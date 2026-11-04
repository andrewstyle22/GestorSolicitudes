namespace GestorSolicitudes.Tests;

using GestorSolicitudes.Helpers;
using GestorSolicitudes.Models;

public class EstadisticasSolicitudTests
{
    [Fact]
    public void Calcular_SinSolicitudesDevuelveCerosYValoresNulos()
    {
        ResumenEstadisticas resumen = EstadisticasSolicitud.Calcular(Array.Empty<Solicitud>());

        Assert.Equal(0, resumen.Total);
        Assert.Equal(0, resumen.Abiertas);
        Assert.Equal(0, resumen.EnEntrevista);
        Assert.Equal(0, resumen.Ofertas);
        Assert.Equal(0, resumen.SeguimientosPendientes);
        Assert.Null(resumen.TasaRespuesta);
        Assert.Null(resumen.MediaDiasRespuesta);
    }

    [Fact]
    public void Calcular_CuentaCadaEstadoEnSuGrupo()
    {
        var baseFecha = new DateTime(2026, 1, 10);
        var solicitudes = new List<Solicitud>
        {
            new SolicitudBuilder().ConFechaSolicitud(baseFecha).ConEstado(EstadoSolicitud.Enviada).Construir(),
            new SolicitudBuilder().ConFechaSolicitud(baseFecha).ConEstado(EstadoSolicitud.EntrevistaTecnica).Construir(),
            new SolicitudBuilder().ConFechaSolicitud(baseFecha).ConEstado(EstadoSolicitud.PruebaTecnica).Construir(),
            new SolicitudBuilder().ConFechaSolicitud(baseFecha).ConEstado(EstadoSolicitud.OfertaRecibida).Construir(),
            new SolicitudBuilder().ConFechaSolicitud(baseFecha).ConEstado(EstadoSolicitud.OfertaAceptada).Construir(),
            new SolicitudBuilder().ConFechaSolicitud(baseFecha).ConEstado(EstadoSolicitud.Rechazada).Construir(),
        };

        ResumenEstadisticas resumen = EstadisticasSolicitud.Calcular(solicitudes);

        Assert.Equal(6, resumen.Total);
        Assert.Equal(4, resumen.Abiertas);
        Assert.Equal(2, resumen.EnEntrevista);
        Assert.Equal(2, resumen.Ofertas);
    }

    [Fact]
    public void Calcular_CuentaSeguimientosVencidos()
    {
        var solicitudes = new List<Solicitud>
        {
            new SolicitudBuilder().ConEstado(EstadoSolicitud.Enviada).ConProximoSeguimiento(DateTime.Today.AddDays(-1)).Construir(),
            new SolicitudBuilder().ConEstado(EstadoSolicitud.Enviada).ConProximoSeguimiento(DateTime.Today.AddDays(3)).Construir(),
            new SolicitudBuilder().ConEstado(EstadoSolicitud.Rechazada).ConProximoSeguimiento(DateTime.Today.AddDays(-1)).Construir(),
        };

        Assert.Equal(1, EstadisticasSolicitud.Calcular(solicitudes).SeguimientosPendientes);
    }

    [Fact]
    public void Calcular_CalculaTasaYMediaDeRespuesta()
    {
        var baseFecha = new DateTime(2026, 1, 10);
        var solicitudes = new List<Solicitud>
        {
            new SolicitudBuilder().ConFechaSolicitud(baseFecha).ConFechaPrimeraRespuesta(baseFecha.AddDays(2)).Construir(),
            new SolicitudBuilder().ConFechaSolicitud(baseFecha).ConFechaPrimeraRespuesta(baseFecha.AddDays(4)).Construir(),
            new SolicitudBuilder().ConFechaSolicitud(baseFecha).Construir(),
        };

        ResumenEstadisticas resumen = EstadisticasSolicitud.Calcular(solicitudes);

        // 2 de 3 tienen respuesta → 2/3, sin redondear a texto.
        Assert.Equal(2.0 / 3.0, resumen.TasaRespuesta!.Value, precision: 10);
        // Media de 2 y 4 días → 3 días.
        Assert.Equal(3.0, resumen.MediaDiasRespuesta!.Value, precision: 10);
    }

    [Fact]
    public void Calcular_MediaConDecimalesNoSeRedondea()
    {
        var baseFecha = new DateTime(2026, 1, 10);
        var solicitudes = new List<Solicitud>
        {
            new SolicitudBuilder().ConFechaSolicitud(baseFecha).ConFechaPrimeraRespuesta(baseFecha.AddDays(2)).Construir(),
            new SolicitudBuilder().ConFechaSolicitud(baseFecha).ConFechaPrimeraRespuesta(baseFecha.AddDays(5)).Construir(),
        };

        ResumenEstadisticas resumen = EstadisticasSolicitud.Calcular(solicitudes);

        Assert.Equal(1.0, resumen.TasaRespuesta!.Value, precision: 10);
        Assert.Equal(3.5, resumen.MediaDiasRespuesta!.Value, precision: 10);
    }
}
