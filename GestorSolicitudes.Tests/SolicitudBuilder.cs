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

    public SolicitudBuilder ConEstado(EstadoSolicitud estado) => this.Asignar(s => s.Estado = estado);

    public SolicitudBuilder ConFechaSolicitud(DateTime fecha) => this.Asignar(s => s.FechaSolicitud = fecha);

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

    public SolicitudBuilder ConSalarioMin(int? salario) => this.Asignar(s => s.SalarioMin = salario);

    public SolicitudBuilder ConSalarioMax(int? salario) => this.Asignar(s => s.SalarioMax = salario);

    public SolicitudBuilder ConPretensionSalarial(int? salario) => this.Asignar(s => s.PretensionSalarial = salario);

    public SolicitudBuilder ConInteres(int interes) => this.Asignar(s => s.Interes = interes);

    public SolicitudBuilder ConMotivoRechazo(MotivoRechazo? motivo) => this.Asignar(s => s.MotivoRechazo = motivo);

    public SolicitudBuilder ConFechaEntrevista(DateTime? fecha) => this.Asignar(s => s.FechaEntrevista = fecha);

    public SolicitudBuilder ConRespuestaEmpresa(string? respuesta) => this.Asignar(s => s.RespuestaEmpresa = respuesta);

    public SolicitudBuilder ConContactoNombre(string? nombre) => this.Asignar(s => s.ContactoNombre = nombre);

    public SolicitudBuilder ConContactoEmail(string? email) => this.Asignar(s => s.ContactoEmail = email);

    public SolicitudBuilder ConContactoTelefono(string? telefono) => this.Asignar(s => s.ContactoTelefono = telefono);

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

    public SolicitudBuilder ConNotas(string? notas) => this.Asignar(s => s.Notas = notas);

    public SolicitudBuilder ConRutaCv(string? ruta) => this.Asignar(s => s.RutaCv = ruta);

    public SolicitudBuilder ConRutaCarta(string? ruta) => this.Asignar(s => s.RutaCarta = ruta);

    public SolicitudBuilder ConNombreOriginalCv(string? nombre) => this.Asignar(s => s.NombreOriginalCv = nombre);

    public SolicitudBuilder ConNombreOriginalCarta(string? nombre) =>
        this.Asignar(s => s.NombreOriginalCarta = nombre);

    public SolicitudBuilder ConFechaPrimeraRespuesta(DateTime? fecha) =>
        this.Asignar(s => s.FechaPrimeraRespuesta = fecha);

    public SolicitudBuilder ConFechaCierre(DateTime? fecha) => this.Asignar(s => s.FechaCierre = fecha);

    public SolicitudBuilder ConProximoSeguimiento(DateTime? fecha) => this.Asignar(s => s.ProximoSeguimiento = fecha);

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
        resultado.SalarioMin = this.solicitud.SalarioMin;
        resultado.SalarioMax = this.solicitud.SalarioMax;
        resultado.PretensionSalarial = this.solicitud.PretensionSalarial;
        resultado.Interes = this.solicitud.Interes;
        resultado.FechaSolicitud = this.solicitud.FechaSolicitud;
        resultado.FechaPrimeraRespuesta = this.solicitud.FechaPrimeraRespuesta;
        resultado.FechaEntrevista = this.solicitud.FechaEntrevista;
        resultado.FechaCierre = this.solicitud.FechaCierre;
        resultado.ProximoSeguimiento = this.solicitud.ProximoSeguimiento;
        resultado.Estado = this.solicitud.Estado;
        resultado.MotivoRechazo = this.solicitud.MotivoRechazo;
        resultado.RespuestaEmpresa = this.solicitud.RespuestaEmpresa;
        resultado.ContactoNombre = this.solicitud.ContactoNombre;
        resultado.ContactoEmail = this.solicitud.ContactoEmail;
        resultado.ContactoTelefono = this.solicitud.ContactoTelefono;
        resultado.Notas = this.solicitud.Notas;
        resultado.RutaCv = this.solicitud.RutaCv;
        resultado.RutaCarta = this.solicitud.RutaCarta;
        resultado.NombreOriginalCv = this.solicitud.NombreOriginalCv;
        resultado.NombreOriginalCarta = this.solicitud.NombreOriginalCarta;

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
