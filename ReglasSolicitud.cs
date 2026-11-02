namespace GestorSolicitudes.Helpers;

using GestorSolicitudes.Models;

/// <summary>
/// Reglas de negocio puras para la operación de Guardar una solicitud.
/// Viven fuera del ViewModel para facilitar su prueba y mantener el ViewModel delgado.
/// </summary>
public static class ReglasSolicitud
{
    /// <summary>
    /// Propone una fecha de seguimiento por defecto: 7 días después de la solicitud,
    /// solo si la solicitud está abierta y no tiene fecha de seguimiento.
    /// </summary>
    public static DateTime? ProponerSeguimiento(Solicitud solicitud) =>
        solicitud.EstaAbierta && solicitud.ProximoSeguimiento is null
            ? solicitud.FechaSolicitud.AddDays(7)
            : null;
}
