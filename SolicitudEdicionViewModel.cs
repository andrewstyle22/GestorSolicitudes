namespace GestorSolicitudes.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using GestorSolicitudes.Models;

/// <summary>
/// Búfer de edición de una solicitud: la interfaz escribe aquí y el modelo solo se toca al guardar.
/// </summary>
public sealed partial class SolicitudEdicionViewModel : ObservableObject
{
    [ObservableProperty]
    private string empresa = string.Empty;

    [ObservableProperty]
    private string puesto = string.Empty;

    [ObservableProperty]
    private string? enlaceOferta;

    [ObservableProperty]
    private string? portal;

    [ObservableProperty]
    private Origen origen = Origen.AplicacionDirecta;

    [ObservableProperty]
    private string? ubicacion;

    [ObservableProperty]
    private Modalidad modalidad = Modalidad.SinIndicar;

    [ObservableProperty]
    private string? tecnologias;

    [ObservableProperty]
    private string? requisitos;

    [ObservableProperty]
    private int? salarioMin;

    [ObservableProperty]
    private int? salarioMax;

    [ObservableProperty]
    private int? pretensionSalarial;

    [ObservableProperty]
    private int interes = 3;

    [ObservableProperty]
    private EstadoSolicitud estado = EstadoSolicitud.Enviada;

    [ObservableProperty]
    private MotivoRechazo? motivoRechazo;

    [ObservableProperty]
    private DateTime fechaSolicitud = DateTime.Today;

    [ObservableProperty]
    private DateTime? fechaPrimeraRespuesta;

    [ObservableProperty]
    private DateTime? fechaEntrevista;

    [ObservableProperty]
    private DateTime? proximoSeguimiento;

    [ObservableProperty]
    private DateTime? fechaCierre;

    [ObservableProperty]
    private string? respuestaEmpresa;

    [ObservableProperty]
    private string? contactoNombre;

    [ObservableProperty]
    private string? contactoEmail;

    [ObservableProperty]
    private string? contactoTelefono;

    [ObservableProperty]
    private string? notas;

    public SolicitudEdicionViewModel() => this.CargarDesde(new Solicitud());

    public SolicitudEdicionViewModel(Solicitud solicitud) => this.CargarDesde(solicitud);

    public void CargarDesde(Solicitud solicitud)
    {
        this.Empresa = solicitud.Empresa;
        this.Puesto = solicitud.Puesto;
        this.EnlaceOferta = solicitud.EnlaceOferta;
        this.Portal = solicitud.Portal;
        this.Origen = solicitud.Origen;
        this.Ubicacion = solicitud.Ubicacion;
        this.Modalidad = solicitud.Modalidad;
        this.Tecnologias = solicitud.Tecnologias;
        this.Requisitos = solicitud.Requisitos;
        this.SalarioMin = solicitud.SalarioMin;
        this.SalarioMax = solicitud.SalarioMax;
        this.PretensionSalarial = solicitud.PretensionSalarial;
        this.Interes = solicitud.Interes;
        this.Estado = solicitud.Estado;
        this.MotivoRechazo = solicitud.MotivoRechazo;
        this.FechaSolicitud = solicitud.FechaSolicitud;
        this.FechaPrimeraRespuesta = solicitud.FechaPrimeraRespuesta;
        this.FechaEntrevista = solicitud.FechaEntrevista;
        this.ProximoSeguimiento = solicitud.ProximoSeguimiento;
        this.FechaCierre = solicitud.FechaCierre;
        this.RespuestaEmpresa = solicitud.RespuestaEmpresa;
        this.ContactoNombre = solicitud.ContactoNombre;
        this.ContactoEmail = solicitud.ContactoEmail;
        this.ContactoTelefono = solicitud.ContactoTelefono;
        this.Notas = solicitud.Notas;
    }

    public void AplicarIdentificacion(Solicitud solicitud)
    {
        solicitud.Empresa = this.Empresa;
        solicitud.Puesto = this.Puesto;
    }

    public void AplicarOferta(Solicitud solicitud)
    {
        solicitud.EnlaceOferta = this.EnlaceOferta;
        solicitud.Portal = this.Portal;
        solicitud.Origen = this.Origen;
        solicitud.Ubicacion = this.Ubicacion;
        solicitud.Modalidad = this.Modalidad;
        solicitud.Tecnologias = this.Tecnologias;
        solicitud.Requisitos = this.Requisitos;
    }

    public void AplicarEconomico(Solicitud solicitud)
    {
        solicitud.SalarioMin = this.SalarioMin;
        solicitud.SalarioMax = this.SalarioMax;
        solicitud.PretensionSalarial = this.PretensionSalarial;
        solicitud.Interes = this.Interes;
    }

    public void AplicarSeguimiento(Solicitud solicitud)
    {
        solicitud.Estado = this.Estado;
        solicitud.MotivoRechazo = this.MotivoRechazo;
        solicitud.FechaSolicitud = this.FechaSolicitud;
        solicitud.FechaPrimeraRespuesta = this.FechaPrimeraRespuesta;
        solicitud.FechaEntrevista = this.FechaEntrevista;
        solicitud.ProximoSeguimiento = this.ProximoSeguimiento;
        solicitud.FechaCierre = this.FechaCierre;
        solicitud.RespuestaEmpresa = this.RespuestaEmpresa;
    }

    public void AplicarContacto(Solicitud solicitud)
    {
        solicitud.ContactoNombre = this.ContactoNombre;
        solicitud.ContactoEmail = this.ContactoEmail;
        solicitud.ContactoTelefono = this.ContactoTelefono;
        solicitud.Notas = this.Notas;
    }

    /// <summary>
    /// Vuelca al modelo solo los grupos cuya pestaña ya escribe en el editor. Mientras la UI
    /// siga bindeada a la entidad, ese grupo no se toca para no pisar lo editado.
    /// </summary>
    public void AplicarA(Solicitud solicitud)
    {
        this.AplicarOferta(solicitud);
        this.AplicarEconomico(solicitud);
    }
}
