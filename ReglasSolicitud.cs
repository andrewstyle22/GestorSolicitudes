namespace GestorSolicitudes.Helpers;

using GestorSolicitudes.Models;

/// <summary>
/// Reglas de negocio puras para la operación de Guardar una solicitud.
/// Viven fuera del ViewModel para facilitar su prueba y mantener el ViewModel delgado.
/// </summary>
public static class ReglasSolicitud
{
    /// <summary>
    /// Verifica si los datos obligatorios (empresa y puesto) están rellenos.
    /// </summary>
    public static bool TieneDatosObligatorios(Solicitud solicitud) =>
        !string.IsNullOrWhiteSpace(solicitud.Empresa)
        && !string.IsNullOrWhiteSpace(solicitud.Puesto);

    /// <summary>
    /// Propone una fecha de seguimiento por defecto: 7 días después de la solicitud,
    /// solo si la solicitud está abierta y no tiene fecha de seguimiento.
    /// </summary>
    public static DateTime? ProponerSeguimiento(Solicitud solicitud) =>
        solicitud.EstaAbierta && solicitud.ProximoSeguimiento is null
            ? solicitud.FechaSolicitud.AddDays(7)
            : null;

    /// <summary>
    /// Añade el hito inicial (SolicitudEnviada) al historial de eventos si la solicitud
    /// todavía no tiene eventos cargados. Se llama al crear una nueva solicitud.
    /// </summary>
    public static void AnadirHitoInicial(Solicitud solicitud)
    {
        if (solicitud.Eventos.Count == 0)
        {
            solicitud.Eventos.Add(new Evento
            {
                Fecha = solicitud.FechaSolicitud,
                Tipo = TipoEvento.SolicitudEnviada,
                Descripcion = Traducciones.Texto("Evento.CandidaturaEnviada"),
            });
        }
    }
}
