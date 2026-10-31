namespace GestorSolicitudes.Tests;

using GestorSolicitudes.Models;
using GestorSolicitudes.ViewModels;

public class SolicitudEdicionTests
{
    [Fact]
    public void CargarDesde_CopiaIdentificacion()
    {
        Solicitud solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir();
        var editor = new SolicitudEdicionViewModel();

        editor.CargarDesde(solicitud);

        Assert.Equal("ACME", editor.Empresa);
        Assert.Equal("Dev", editor.Puesto);
    }

    [Fact]
    public void AplicarIdentificacion_VuelcaEmpresaYPuesto()
    {
        Solicitud solicitud = new SolicitudBuilder().Construir();
        var editor = new SolicitudEdicionViewModel { Empresa = "ACME", Puesto = "Dev" };

        editor.AplicarIdentificacion(solicitud);

        Assert.Equal("ACME", solicitud.Empresa);
        Assert.Equal("Dev", solicitud.Puesto);
    }
}
