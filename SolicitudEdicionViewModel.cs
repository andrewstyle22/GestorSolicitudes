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
}
