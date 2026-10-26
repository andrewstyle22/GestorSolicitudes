namespace GestorSolicitudes.Helpers;

using GestorSolicitudes.Models;

/// <summary>Los números de la cabecera, ya calculados (no formateados).</summary>
public sealed record ResumenEstadisticas(
    int Total,
    int Abiertas,
    int EnEntrevista,
    int Ofertas,
    int SeguimientosPendientes,
    double? TasaRespuesta,
    double? MediaDiasRespuesta);

/// <summary>Cálculo puro de las estadísticas de la cabecera.</summary>
public static class EstadisticasSolicitud
{
    public static ResumenEstadisticas Calcular(IReadOnlyList<Solicitud> solicitudes)
    {
        int total = solicitudes.Count;
        int conRespuesta = solicitudes.Count(s => s.HuboRespuesta);

        List<int> dias = solicitudes
            .Where(s => s.DiasHastaRespuesta.HasValue)
            .Select(s => s.DiasHastaRespuesta!.Value)
            .ToList();

        double? tasa = total == 0 ? null : (double)conRespuesta / total;
        double? media = dias.Count == 0 ? null : dias.Average();

        return new ResumenEstadisticas(total, solicitudes.Count(s => s.EstaAbierta),
            solicitudes.Count(s => s.Estado is EstadoSolicitud.EntrevistaRrhh or EstadoSolicitud.EntrevistaTecnica or EstadoSolicitud.EntrevistaFinal or EstadoSolicitud.PruebaTecnica),
            solicitudes.Count(s => s.Estado is EstadoSolicitud.OfertaRecibida or EstadoSolicitud.OfertaAceptada or EstadoSolicitud.OfertaRechazada),
            solicitudes.Count(s => s.SeguimientoPendiente),
            tasa,
            media);
    }
}