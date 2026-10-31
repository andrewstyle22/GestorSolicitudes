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

    public void CargarDesde(Solicitud solicitud)
    {
        this.Empresa = solicitud.Empresa;
        this.Puesto = solicitud.Puesto;
    }

    public void AplicarIdentificacion(Solicitud solicitud)
    {
        solicitud.Empresa = this.Empresa;
        solicitud.Puesto = this.Puesto;
    }
}
