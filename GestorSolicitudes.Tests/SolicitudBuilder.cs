namespace GestorSolicitudes.Tests;

using GestorSolicitudes.Models;

/// <summary>
/// Constructor de solicitudes para los tests: parte de los valores por defecto de la entidad
/// y solo cambia lo que pide cada caso.
/// </summary>
internal sealed class SolicitudBuilder
{
    private readonly Solicitud solicitud = new(string.Empty, string.Empty);
    private string empresa = string.Empty;
    private string puesto = string.Empty;
    private DatosOferta oferta = new(null, null, Origen.AplicacionDirecta, null, Modalidad.SinIndicar, null, null);

    public SolicitudBuilder ConEmpresa(string empresa)
    {
        this.empresa = empresa;
        return this;
    }

    public SolicitudBuilder ConPuesto(string puesto)
    {
        this.puesto = puesto;
        return this;
    }

    public SolicitudBuilder ConEstado(EstadoSolicitud estado) => this.Asignar(s =>
    {
        MotivoRechazo? motivo = s.MotivoRechazo;
        if (estado.AdmiteMotivo()
            && (motivo is not MotivoRechazo actual || !estado.MotivosPermitidos().Contains(actual)))
        {
            motivo = MotivoRechazo.NoIndicado;
        }

        s.CambiarEstado(estado, motivo, s.FechaCierre);
    });

    public SolicitudBuilder ConFechaSolicitud(DateTime fecha) => this.Asignar(s =>
        s.ActualizarFechas(fecha, s.FechaEntrevista, s.ProximoSeguimiento));

    public SolicitudBuilder ConPortal(string? portal)
    {
        this.oferta = this.oferta with { Portal = portal };
        return this;
    }

    public SolicitudBuilder ConOrigen(Origen origen)
    {
        this.oferta = this.oferta with { Origen = origen };
        return this;
    }

    public SolicitudBuilder ConUbicacion(string? ubicacion)
    {
        this.oferta = this.oferta with { Ubicacion = ubicacion };
        return this;
    }

    public SolicitudBuilder ConModalidad(Modalidad modalidad)
    {
        this.oferta = this.oferta with { Modalidad = modalidad };
        return this;
    }

    public SolicitudBuilder ConRequisitos(string? requisitos)
    {
        this.oferta = this.oferta with { Requisitos = requisitos };
        return this;
    }

    public SolicitudBuilder ConSalarioMin(int? salario) => this.Asignar(s =>
        s.ActualizarCondiciones(salario, s.SalarioMax, s.PretensionSalarial, s.Interes));

    public SolicitudBuilder ConSalarioMax(int? salario) => this.Asignar(s =>
        s.ActualizarCondiciones(s.SalarioMin, salario, s.PretensionSalarial, s.Interes));

    public SolicitudBuilder ConPretensionSalarial(int? salario) => this.Asignar(s =>
        s.ActualizarCondiciones(s.SalarioMin, s.SalarioMax, salario, s.Interes));

    public SolicitudBuilder ConInteres(int interes) => this.Asignar(s =>
        s.ActualizarCondiciones(s.SalarioMin, s.SalarioMax, s.PretensionSalarial, interes));

    public SolicitudBuilder ConMotivoRechazo(MotivoRechazo? motivo) => this.Asignar(s =>
        s.CambiarEstado(s.Estado, motivo, s.FechaCierre));

    public SolicitudBuilder ConFechaEntrevista(DateTime? fecha) => this.Asignar(s =>
        s.ActualizarFechas(s.FechaSolicitud, fecha, s.ProximoSeguimiento));

    public SolicitudBuilder ConRespuestaEmpresa(string? respuesta) =>
        this.Asignar(s => s.RegistrarRespuesta(s.FechaPrimeraRespuesta, respuesta));

    public SolicitudBuilder ConContactoNombre(string? nombre) =>
        this.Asignar(s => s.ActualizarContacto(nombre, s.ContactoEmail, s.ContactoTelefono));

    public SolicitudBuilder ConContactoEmail(string? email) =>
        this.Asignar(s => s.ActualizarContacto(s.ContactoNombre, email, s.ContactoTelefono));

    public SolicitudBuilder ConContactoTelefono(string? telefono) =>
        this.Asignar(s => s.ActualizarContacto(s.ContactoNombre, s.ContactoEmail, telefono));

    public SolicitudBuilder ConTecnologias(string? tecnologias)
    {
        this.oferta = this.oferta with { Tecnologias = tecnologias };
        return this;
    }

    public SolicitudBuilder ConEnlaceOferta(string? enlace)
    {
        this.oferta = this.oferta with { EnlaceOferta = enlace };
        return this;
    }

    public SolicitudBuilder ConNotas(string? notas) => this.Asignar(s => s.ActualizarNotas(notas));

    public SolicitudBuilder ConRutaCv(string? ruta) => this.Asignar(s => s.AsignarCv(ruta, s.NombreOriginalCv));

    public SolicitudBuilder ConRutaCarta(string? ruta) => this.Asignar(s => s.AsignarCarta(ruta, s.NombreOriginalCarta));

    public SolicitudBuilder ConNombreOriginalCv(string? nombre) =>
        this.Asignar(s => s.AsignarCv(s.RutaCv, nombre));

    public SolicitudBuilder ConNombreOriginalCarta(string? nombre) =>
        this.Asignar(s => s.AsignarCarta(s.RutaCarta, nombre));

    public SolicitudBuilder ConFechaPrimeraRespuesta(DateTime? fecha) =>
        this.Asignar(s => s.RegistrarRespuesta(fecha, s.RespuestaEmpresa));

    public SolicitudBuilder ConFechaCierre(DateTime? fecha) => this.Asignar(s =>
        s.CambiarEstado(s.Estado, s.MotivoRechazo, fecha));

    public SolicitudBuilder ConProximoSeguimiento(DateTime? fecha) => this.Asignar(s =>
        s.ActualizarFechas(s.FechaSolicitud, s.FechaEntrevista, fecha));

    public SolicitudBuilder ConEventos(params Evento[] eventos) => this.Asignar(s =>
    {
        foreach (Evento evento in eventos)
        {
            s.AgregarEvento(evento);
        }
    });

    public Solicitud Construir()
    {
        var resultado = new Solicitud(this.empresa, this.puesto);
        resultado.ActualizarOferta(this.oferta);
        resultado.ActualizarCondiciones(
            this.solicitud.SalarioMin,
            this.solicitud.SalarioMax,
            this.solicitud.PretensionSalarial,
            this.solicitud.Interes);
        resultado.ActualizarFechas(
            this.solicitud.FechaSolicitud,
            this.solicitud.FechaEntrevista,
            this.solicitud.ProximoSeguimiento);
        resultado.RegistrarRespuesta(this.solicitud.FechaPrimeraRespuesta, this.solicitud.RespuestaEmpresa);
        resultado.CambiarEstado(this.solicitud.Estado, this.solicitud.MotivoRechazo, this.solicitud.FechaCierre);
        resultado.ActualizarContacto(
            this.solicitud.ContactoNombre,
            this.solicitud.ContactoEmail,
            this.solicitud.ContactoTelefono);
        resultado.ActualizarNotas(this.solicitud.Notas);
        resultado.AsignarCv(this.solicitud.RutaCv, this.solicitud.NombreOriginalCv);
        resultado.AsignarCarta(this.solicitud.RutaCarta, this.solicitud.NombreOriginalCarta);

        foreach (Evento evento in this.solicitud.Eventos)
        {
            resultado.AgregarEvento(evento);
        }

        return resultado;
    }

    private SolicitudBuilder Asignar(Action<Solicitud> asignacion)
    {
        asignacion(this.solicitud);
        return this;
    }
}
