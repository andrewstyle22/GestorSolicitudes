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

    [Fact]
    public void CargarDesde_CopiaElSeguimiento()
    {
        DateTime fecha = new(2026, 1, 10);
        Solicitud solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConMotivoRechazo(MotivoRechazo.Salario)
            .ConFechaSolicitud(fecha)
            .ConFechaPrimeraRespuesta(fecha.AddDays(1))
            .ConFechaEntrevista(fecha.AddDays(2))
            .ConProximoSeguimiento(fecha.AddDays(3))
            .ConFechaCierre(fecha.AddDays(4))
            .ConRespuestaEmpresa("Buscamos otro perfil")
            .Construir();
        var editor = new SolicitudEdicionViewModel();

        editor.CargarDesde(solicitud);

        Assert.Equal(EstadoSolicitud.Rechazada, editor.Estado);
        Assert.Equal(MotivoRechazo.Salario, editor.MotivoRechazo);
        Assert.Equal(fecha, editor.FechaSolicitud);
        Assert.Equal(fecha.AddDays(1), editor.FechaPrimeraRespuesta);
        Assert.Equal(fecha.AddDays(2), editor.FechaEntrevista);
        Assert.Equal(fecha.AddDays(3), editor.ProximoSeguimiento);
        Assert.Equal(fecha.AddDays(4), editor.FechaCierre);
        Assert.Equal("Buscamos otro perfil", editor.RespuestaEmpresa);
    }

    [Fact]
    public void AplicarEstado_VuelcaEstadoYMotivo()
    {
        Solicitud solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConMotivoRechazo(MotivoRechazo.Salario)
            .Construir();
        var editor = new SolicitudEdicionViewModel
        {
            Estado = EstadoSolicitud.OfertaRecibida,
            MotivoRechazo = null,
        };

        editor.AplicarEstado(solicitud);

        Assert.Equal(EstadoSolicitud.OfertaRecibida, solicitud.Estado);
        Assert.Null(solicitud.MotivoRechazo);
    }

    [Fact]
    public void AplicarFechas_VuelcaLasCincoFechas()
    {
        DateTime fecha = new(2026, 2, 1);
        Solicitud solicitud = new SolicitudBuilder().Construir();
        var editor = new SolicitudEdicionViewModel
        {
            FechaSolicitud = fecha,
            FechaPrimeraRespuesta = fecha.AddDays(1),
            FechaEntrevista = fecha.AddDays(2),
            ProximoSeguimiento = fecha.AddDays(3),
            FechaCierre = fecha.AddDays(4),
        };

        editor.AplicarFechas(solicitud);

        Assert.Equal(fecha, solicitud.FechaSolicitud);
        Assert.Equal(fecha.AddDays(1), solicitud.FechaPrimeraRespuesta);
        Assert.Equal(fecha.AddDays(2), solicitud.FechaEntrevista);
        Assert.Equal(fecha.AddDays(3), solicitud.ProximoSeguimiento);
        Assert.Equal(fecha.AddDays(4), solicitud.FechaCierre);
    }

    [Fact]
    public void AplicarRespuesta_VuelcaLaRespuestaDeLaEmpresa()
    {
        Solicitud solicitud = new SolicitudBuilder().ConRespuestaEmpresa("Antigua").Construir();
        var editor = new SolicitudEdicionViewModel { RespuestaEmpresa = "Oferta enviada" };

        editor.AplicarRespuesta(solicitud);

        Assert.Equal("Oferta enviada", solicitud.RespuestaEmpresa);
    }

    [Fact]
    public void CargarDesde_CopiaContactoYNotas()
    {
        Solicitud solicitud = new SolicitudBuilder()
            .ConContactoNombre("Ana")
            .ConContactoEmail("ana@ejemplo.test")
            .ConContactoTelefono("600123456")
            .ConNotas("Hablar el lunes")
            .Construir();
        var editor = new SolicitudEdicionViewModel();

        editor.CargarDesde(solicitud);

        Assert.Equal("Ana", editor.ContactoNombre);
        Assert.Equal("ana@ejemplo.test", editor.ContactoEmail);
        Assert.Equal("600123456", editor.ContactoTelefono);
        Assert.Equal("Hablar el lunes", editor.Notas);
    }

    [Fact]
    public void AplicarContacto_VuelcaContactoYNotas()
    {
        Solicitud solicitud = new SolicitudBuilder().Construir();
        var editor = new SolicitudEdicionViewModel
        {
            ContactoNombre = "Ana",
            ContactoEmail = "ana@ejemplo.test",
            ContactoTelefono = "600123456",
            Notas = "Hablar el lunes",
        };

        editor.AplicarContacto(solicitud);

        Assert.Equal("Ana", solicitud.ContactoNombre);
        Assert.Equal("ana@ejemplo.test", solicitud.ContactoEmail);
        Assert.Equal("600123456", solicitud.ContactoTelefono);
        Assert.Equal("Hablar el lunes", solicitud.Notas);
    }

    [Fact]
    public void AplicarA_VuelcaTodosLosGruposDelEditor()
    {
        DateTime fecha = new(2026, 2, 1);
        Solicitud solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir();
        var editor = new SolicitudEdicionViewModel
        {
            Empresa = "Otra",
            Puesto = "Otro",
            Portal = "LinkedIn",
            Estado = EstadoSolicitud.OfertaRecibida,
            FechaSolicitud = fecha,
            RespuestaEmpresa = "Oferta enviada",
            ContactoNombre = "Ana",
        };

        editor.AplicarA(solicitud);

        Assert.Equal("Otra", solicitud.Empresa);
        Assert.Equal("Otro", solicitud.Puesto);
        Assert.Equal("LinkedIn", solicitud.Portal);
        Assert.Equal(EstadoSolicitud.OfertaRecibida, solicitud.Estado);
        Assert.Equal(fecha, solicitud.FechaSolicitud);
        Assert.Equal("Oferta enviada", solicitud.RespuestaEmpresa);
        Assert.Equal("Ana", solicitud.ContactoNombre);
    }
}
