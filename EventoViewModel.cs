namespace GestorSolicitudes.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using GestorSolicitudes.Models;

/// <summary>
/// Búfer de edición de un evento del historial: la UI escribe aquí y el modelo solo se toca al guardar.
/// </summary>
public sealed partial class EventoViewModel : ObservableObject
{
    [ObservableProperty]
    private DateTime fecha = DateTime.Today;

    [ObservableProperty]
    private TipoEvento tipo = TipoEvento.Nota;

    [ObservableProperty]
    private string descripcion = string.Empty;

    /// <summary>Entidad que hay detrás del búfer; el guardado escribe en ella.</summary>
    internal Evento? Original { get; private set; }

    public void CargarDesde(Evento evento)
    {
        this.Original = evento;
        this.Fecha = evento.Fecha;
        this.Tipo = evento.Tipo;
        this.Descripcion = evento.Descripcion;
    }

    public void AplicarA(Evento evento)
    {
        evento.Fecha = this.Fecha;
        evento.Tipo = this.Tipo;
        evento.Descripcion = this.Descripcion;
    }
}
