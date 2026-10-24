namespace GestorSolicitudes.Helpers;

using GestorSolicitudes.Models;

/// <summary>Los números de la cabecera, ya formateados para la UI.</summary>
public sealed record ResumenEstadisticas(int Total, int Abiertas, int EnEntrevista, int Ofertas, int SeguimientosPendientes, string TasaRespuesta, string MediaDiasRespuesta);

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

        return new ResumenEstadisticas(total, solicitudes.Count(s => s.EstaAbierta),
            solicitudes.Count(s => s.Estado is EstadoSolicitud.EntrevistaRrhh or EstadoSolicitud.EntrevistaTecnica or EstadoSolicitud.EntrevistaFinal or EstadoSolicitud.PruebaTecnica),
            solicitudes.Count(s => s.Estado is EstadoSolicitud.OfertaRecibida or EstadoSolicitud.OfertaAceptada or EstadoSolicitud.OfertaRechazada),
            solicitudes.Count(s => s.SeguimientoPendiente),
            total == 0 ? "—" : ((double)conRespuesta / total).ToString("P0", Localizacion.CulturaActual),
            dias.Count == 0 ? "—" : $"{dias.Average().ToString("0.#", Localizacion.CulturaActual)} {Localizacion.Texto("Metrica.Dias")}");
    }
}