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

    [Fact]
    public void CargarDesde_CopiaLaOferta()
    {
        Solicitud solicitud = new SolicitudBuilder()
            .ConEnlaceOferta("https://ejemplo.test/oferta")
            .ConPortal("LinkedIn")
            .ConOrigen(Origen.Referido)
            .ConUbicacion("Madrid")
            .ConModalidad(Modalidad.Remoto)
            .ConTecnologias("C#, .NET")
            .ConRequisitos("3 años de experiencia")
            .Construir();
        var editor = new SolicitudEdicionViewModel();

        editor.CargarDesde(solicitud);

        Assert.Equal("https://ejemplo.test/oferta", editor.EnlaceOferta);
        Assert.Equal("LinkedIn", editor.Portal);
        Assert.Equal(Origen.Referido, editor.Origen);
        Assert.Equal("Madrid", editor.Ubicacion);
        Assert.Equal(Modalidad.Remoto, editor.Modalidad);
        Assert.Equal("C#, .NET", editor.Tecnologias);
        Assert.Equal("3 años de experiencia", editor.Requisitos);
    }

    [Fact]
    public void AplicarOferta_VuelcaLaOferta()
    {
        Solicitud solicitud = new SolicitudBuilder().Construir();
        var editor = new SolicitudEdicionViewModel
        {
            EnlaceOferta = "https://ejemplo.test/oferta",
            Portal = "LinkedIn",
            Origen = Origen.Referido,
            Ubicacion = "Madrid",
            Modalidad = Modalidad.Remoto,
            Tecnologias = "C#, .NET",
            Requisitos = "3 años de experiencia",
        };

        editor.AplicarOferta(solicitud);

        Assert.Equal("https://ejemplo.test/oferta", solicitud.EnlaceOferta);
        Assert.Equal("LinkedIn", solicitud.Portal);
        Assert.Equal(Origen.Referido, solicitud.Origen);
        Assert.Equal("Madrid", solicitud.Ubicacion);
        Assert.Equal(Modalidad.Remoto, solicitud.Modalidad);
        Assert.Equal("C#, .NET", solicitud.Tecnologias);
        Assert.Equal("3 años de experiencia", solicitud.Requisitos);
    }

    [Fact]
    public void CargarDesde_CopiaElEconomicoYElInteres()
    {
        Solicitud solicitud = new SolicitudBuilder()
            .ConSalarioMin(30000)
            .ConSalarioMax(45000)
            .ConPretensionSalarial(40000)
            .ConInteres(5)
            .Construir();
        var editor = new SolicitudEdicionViewModel();

        editor.CargarDesde(solicitud);

        Assert.Equal(30000, editor.SalarioMin);
        Assert.Equal(45000, editor.SalarioMax);
        Assert.Equal(40000, editor.PretensionSalarial);
        Assert.Equal(5, editor.Interes);
    }

    [Fact]
    public void AplicarEconomico_VuelcaNulos()
    {
        Solicitud solicitud = new SolicitudBuilder()
            .ConSalarioMin(30000)
            .ConSalarioMax(45000)
            .ConPretensionSalarial(40000)
            .Construir();
        var editor = new SolicitudEdicionViewModel { SalarioMin = null, SalarioMax = null, PretensionSalarial = null };

        editor.AplicarEconomico(solicitud);

        Assert.Null(solicitud.SalarioMin);
        Assert.Null(solicitud.SalarioMax);
        Assert.Null(solicitud.PretensionSalarial);
        Assert.Equal(3, solicitud.Interes);
    }
}
