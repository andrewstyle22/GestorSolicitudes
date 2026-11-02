namespace GestorSolicitudes.Tests;

using GestorSolicitudes.Models;
using GestorSolicitudes.ViewModels;

public class EventoViewModelTests
{
    [Fact]
    public void CargarDesde_CopiaFechaTipoYDescripcion()
    {
        DateTime fecha = new(2026, 3, 5);
        var evento = new Evento(fecha, TipoEvento.Entrevista, "Entrevista con RR. HH.");
        var editor = new EventoViewModel();

        editor.CargarDesde(evento);

        Assert.Equal(fecha, editor.Fecha);
        Assert.Equal(TipoEvento.Entrevista, editor.Tipo);
        Assert.Equal("Entrevista con RR. HH.", editor.Descripcion);
    }

    [Fact]
    public void AplicarA_VuelcaFechaTipoYDescripcion()
    {
        DateTime fecha = new(2026, 3, 5);
        var evento = new Evento(new DateTime(2026, 1, 1), TipoEvento.Nota, string.Empty);
        var editor = new EventoViewModel
        {
            Fecha = fecha,
            Tipo = TipoEvento.Oferta,
            Descripcion = "Oferta enviada",
        };

        editor.AplicarA(evento);

        Assert.Equal(fecha, evento.Fecha);
        Assert.Equal(TipoEvento.Oferta, evento.Tipo);
        Assert.Equal("Oferta enviada", evento.Descripcion);
    }

    [Fact]
    public void AlCambiarLasPropiedades_NotificaCadaUna()
    {
        var editor = new EventoViewModel();
        var notificados = new List<string?>();
        editor.PropertyChanged += (_, e) => notificados.Add(e.PropertyName);

        editor.Fecha = new DateTime(2026, 4, 1);
        editor.Tipo = TipoEvento.Llamada;
        editor.Descripcion = "Llamada";

        Assert.Equal(3, notificados.Count);
        Assert.Equal(nameof(editor.Fecha), notificados[0]);
        Assert.Equal(nameof(editor.Tipo), notificados[1]);
        Assert.Equal(nameof(editor.Descripcion), notificados[2]);
    }
}
