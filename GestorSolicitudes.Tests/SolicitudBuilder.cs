namespace GestorSolicitudes.Tests;

using GestorSolicitudes.Models;

/// <summary>
/// Constructor de solicitudes para los tests: parte de los valores por defecto de la entidad
/// y solo cambia lo que pide cada caso.
/// </summary>
internal sealed class SolicitudBuilder
{
    private readonly Solicitud solicitud = new();

    public SolicitudBuilder ConEmpresa(string empresa) => this.Asignar(s => s.Empresa = empresa);

    public SolicitudBuilder ConPuesto(string puesto) => this.Asignar(s => s.Puesto = puesto);

    public SolicitudBuilder ConEstado(EstadoSolicitud estado) => this.Asignar(s => s.Estado = estado);

    public SolicitudBuilder ConFechaSolicitud(DateTime fecha) => this.Asignar(s => s.FechaSolicitud = fecha);

    public SolicitudBuilder ConPortal(string? portal) => this.Asignar(s => s.Portal = portal);

    public SolicitudBuilder ConEnlaceOferta(string? enlace) => this.Asignar(s => s.EnlaceOferta = enlace);

    public SolicitudBuilder ConNotas(string? notas) => this.Asignar(s => s.Notas = notas);

    public SolicitudBuilder ConRutaCv(string? ruta) => this.Asignar(s => s.RutaCv = ruta);

    public SolicitudBuilder ConRutaCarta(string? ruta) => this.Asignar(s => s.RutaCarta = ruta);

    public SolicitudBuilder ConNombreOriginalCv(string? nombre) => this.Asignar(s => s.NombreOriginalCv = nombre);

    public SolicitudBuilder ConFechaPrimeraRespuesta(DateTime? fecha) =>
        this.Asignar(s => s.FechaPrimeraRespuesta = fecha);

    public SolicitudBuilder ConFechaCierre(DateTime? fecha) => this.Asignar(s => s.FechaCierre = fecha);

    public SolicitudBuilder ConProximoSeguimiento(DateTime? fecha) => this.Asignar(s => s.ProximoSeguimiento = fecha);

    public SolicitudBuilder ConEventos(params Evento[] eventos) => this.Asignar(s => s.Eventos.AddRange(eventos));

    public Solicitud Construir() => this.solicitud;

    private SolicitudBuilder Asignar(Action<Solicitud> asignacion)
    {
        asignacion(this.solicitud);
        return this;
    }
}
