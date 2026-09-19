namespace GestorSolicitudes.Models;

using System.ComponentModel;

/// <summary>
/// Punto del embudo en el que se encuentra la candidatura.
/// </summary>
public enum EstadoSolicitud
{
    [Description("Enviada")]
    Enviada = 0,
    [Description("En revisión")]
    EnRevision = 1,
    [Description("Prueba técnica")]
    PruebaTecnica = 2,
    [Description("Entrevista RR. HH.")]
    EntrevistaRrhh = 3,
    [Description("Entrevista técnica")]
    EntrevistaTecnica = 4,
    [Description("Entrevista final")]
    EntrevistaFinal = 5,
    [Description("Oferta recibida")]
    OfertaRecibida = 6,
    [Description("Oferta aceptada")]
    OfertaAceptada = 7,
    [Description("Oferta rechazada por mí")]
    OfertaRechazada = 8,
    [Description("Descartado por la empresa")]
    Rechazada = 9,
    [Description("Retirada por mí")]
    Retirada = 10,
    [Description("Sin respuesta")]
    SinRespuesta = 11,
}

public enum Modalidad
{
    [Description("Sin indicar")]
    SinIndicar = 0,
    [Description("Presencial")]
    Presencial = 1,
    [Description("Híbrido")]
    Hibrido = 2,
    [Description("100 % remoto")]
    Remoto = 3,
}

public enum TipoEvento
{
    [Description("Nota")]
    Nota = 0,
    [Description("Solicitud enviada")]
    SolicitudEnviada = 1,
    [Description("Respuesta de la empresa")]
    Respuesta = 2,
    [Description("Llamada / screening")]
    Llamada = 3,
    [Description("Prueba técnica")]
    PruebaTecnica = 4,
    [Description("Entrevista")]
    Entrevista = 5,
    [Description("Seguimiento enviado")]
    Seguimiento = 6,
    [Description("Oferta")]
    Oferta = 7,
    [Description("Rechazo")]
    Rechazo = 8,
}
