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

/// <summary>
/// Por qué se cerró en falso una candidatura. Solo tiene sentido cuando
/// <see cref="EstadoSolicitud.Rechazada"/>; con suficientes candidaturas es el dato
/// que más dice sobre qué ajustar (CV, pretensión salarial, a qué perfiles aplicar).
/// </summary>
public enum MotivoRechazo
{
    [Description("Salario / pretensión")]
    Salario = 0,
    [Description("Experiencia insuficiente")]
    ExperienciaInsuficiente = 1,
    [Description("Seleccionaron a otro candidato")]
    SeleccionaronOtroCandidato = 2,
    [Description("No encajaba con el equipo/cultura")]
    CulturalFit = 3,
    [Description("Puesto cancelado o pausado")]
    PuestoCanceladoOPausado = 4,
    [Description("Otro motivo")]
    Otro = 5,
}

/// <summary>
/// Cómo llegó la candidatura a existir: distinto de <see cref="Solicitud.Portal"/>,
/// que dice dónde estaba publicada la oferta, no cómo se inició el contacto.
/// </summary>
public enum Origen
{
    [Description("Aplicación directa")]
    AplicacionDirecta = 0,
    [Description("Recruiter me contactó")]
    RecruiterMeContacto = 1,
    [Description("Referido por alguien")]
    Referido = 2,
    [Description("Networking / evento")]
    Networking = 3,
    [Description("Feria de empleo")]
    FeriaDeEmpleo = 4,
    [Description("Otro")]
    Otro = 5,
}
