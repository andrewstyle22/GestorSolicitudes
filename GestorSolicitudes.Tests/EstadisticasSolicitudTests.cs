using GestorSolicitudes.Helpers;
using GestorSolicitudes.Models;

namespace GestorSolicitudes.Tests;

public class EstadisticasSolicitudTests
{
    [Fact]
    public void Calcular_SinSolicitudesDevuelveCerosYGuiones()
    {
        ResumenEstadisticas resumen = EstadisticasSolicitud.Calcular(Array.Empty<Solicitud>());

        Assert.Equal(0, resumen.Total);
        Assert.Equal(0, resumen.Abiertas);
        Assert.Equal(0, resumen.EnEntrevista);
        Assert.Equal(0, resumen.Ofertas);
        Assert.Equal(0, resumen.SeguimientosPendientes);
        Assert.Equal("—", resumen.TasaRespuesta);
        Assert.Equal("—", resumen.MediaDiasRespuesta);
    }

    [Fact]
    public void Calcular_CuentaCadaEstadoEnSuGrupo()
    {
        var baseFecha = new DateTime(2026, 1, 10);
        var solicitudes = new List<Solicitud>
        {
            new() { Estado = EstadoSolicitud.Enviada, FechaSolicitud = baseFecha },
            new() { Estado = EstadoSolicitud.EntrevistaTecnica, FechaSolicitud = baseFecha },
            new() { Estado = EstadoSolicitud.PruebaTecnica, FechaSolicitud = baseFecha },
            new() { Estado = EstadoSolicitud.OfertaRecibida, FechaSolicitud = baseFecha },
            new() { Estado = EstadoSolicitud.OfertaAceptada, FechaSolicitud = baseFecha },
            new() { Estado = EstadoSolicitud.Rechazada, FechaSolicitud = baseFecha },
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
            new() { Estado = EstadoSolicitud.Enviada, ProximoSeguimiento = DateTime.Today.AddDays(-1) },
            new() { Estado = EstadoSolicitud.Enviada, ProximoSeguimiento = DateTime.Today.AddDays(3) },
            new() { Estado = EstadoSolicitud.Rechazada, ProximoSeguimiento = DateTime.Today.AddDays(-1) },
        };

        Assert.Equal(1, EstadisticasSolicitud.Calcular(solicitudes).SeguimientosPendientes);
    }

    [Fact]
    public void Calcular_CalculaTasaYMediaDeRespuesta()
    {
        var baseFecha = new DateTime(2026, 1, 10);
        var solicitudes = new List<Solicitud>
        {
            new() { FechaSolicitud = baseFecha, FechaPrimeraRespuesta = baseFecha.AddDays(2) },
            new() { FechaSolicitud = baseFecha, FechaPrimeraRespuesta = baseFecha.AddDays(4) },
            new() { FechaSolicitud = baseFecha },
        };

        ResumenEstadisticas resumen = EstadisticasSolicitud.Calcular(solicitudes);

        Assert.Equal("67 %", resumen.TasaRespuesta);
        Assert.Equal("3 días", resumen.MediaDiasRespuesta);
    }
}
