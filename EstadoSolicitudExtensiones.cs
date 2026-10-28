namespace GestorSolicitudes.Models;

/// <summary>
/// Clasificación de los estados de una candidatura. Vive en el modelo para que la misma
/// definición la usen la entidad, las estadísticas y las consultas de la bandeja.
/// </summary>
public static class EstadoSolicitudExtensiones
{
    /// <summary>Gets los estados que dan el proceso por cerrado.</summary>
    public static IReadOnlyList<EstadoSolicitud> Cerrados { get; } = new[]
    {
        EstadoSolicitud.OfertaAceptada,
        EstadoSolicitud.OfertaRechazada,
        EstadoSolicitud.Rechazada,
        EstadoSolicitud.Retirada,
    };

    /// <summary>Indica si el proceso ya no sigue abierto con este estado.</summary>
    public static bool EstaCerrada(this EstadoSolicitud estado) => Cerrados.Contains(estado);

    /// <summary>Indica si el estado pertenece a la fase de entrevistas o pruebas técnicas.</summary>
    public static bool EsEntrevista(this EstadoSolicitud estado) => estado is
        EstadoSolicitud.PruebaTecnica
        or EstadoSolicitud.EntrevistaRrhh
        or EstadoSolicitud.EntrevistaTecnica
        or EstadoSolicitud.EntrevistaFinal;

    /// <summary>Indica si el estado pertenece a la fase de ofertas.</summary>
    public static bool EsOferta(this EstadoSolicitud estado) => estado is
        EstadoSolicitud.OfertaRecibida
        or EstadoSolicitud.OfertaAceptada
        or EstadoSolicitud.OfertaRechazada;
}
