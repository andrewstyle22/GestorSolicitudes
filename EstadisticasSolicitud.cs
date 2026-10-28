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

        int abiertas = solicitudes.Count(s => s.EstaAbierta);
        int enEntrevista = solicitudes.Count(s => s.Estado.EsEntrevista());
        int ofertas = solicitudes.Count(s => s.Estado.EsOferta());
        int seguimientosPendientes = solicitudes.Count(s => s.SeguimientoPendiente);

        return new ResumenEstadisticas(
            total,
            abiertas,
            enEntrevista,
            ofertas,
            seguimientosPendientes,
            tasa,
            media);
    }
}