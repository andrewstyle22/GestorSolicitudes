using Xunit;
using GestorSolicitudes.Models;
using GestorSolicitudes.ViewModels;

namespace GestorSolicitudes.Tests;

public class ViewModelTests
{
    [Fact]
    public void Constructor_CargaListaVacia()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        Assert.Empty(vm.Solicitudes);
        Assert.Equal(0, vm.TotalSolicitudes);
        Assert.Null(vm.SolicitudSeleccionada);
        Assert.Equal("—", vm.TasaRespuesta);
        // El filtro por defecto es "Todos los estados"
        Assert.Equal("Todos los estados", vm.EstadoFiltroItem!.Descripcion);
    }

    [Fact]
    public void Nueva_CreaBorradorParaHoyConEstadoEnviada()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        vm.NuevaCommand.Execute(null);

        Assert.NotNull(vm.Edicion);
        Assert.Equal(0, vm.Edicion!.Id);
        Assert.Equal(DateTime.Today, vm.Edicion.FechaSolicitud);
        Assert.Equal(EstadoSolicitud.Enviada, vm.Edicion.Estado);
    }

    [Fact]
    public void Guardar_NuevaCandidaturaValida_PersisteYSelecciona()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        vm.NuevaCommand.Execute(null);
        vm.Edicion!.Empresa = "ACME";
        vm.Edicion.Puesto = "Desarrollador";
        vm.Edicion.ContactoNombre = "Jefa";
        vm.GuardarCommand.Execute(null);

        Assert.Equal(1, vm.TotalSolicitudes);
        Assert.NotNull(vm.SolicitudSeleccionada);
        Assert.Equal(1, vm.SolicitudSeleccionada!.Id);
        Assert.Equal("ACME", vm.SolicitudSeleccionada.Empresa);
        Assert.Equal("Jefa", vm.SolicitudSeleccionada.ContactoNombre);
        // Si el proceso sigue abierto, se propone el seguimiento a +7 días.
        Assert.Equal(DateTime.Today.AddDays(7), vm.SolicitudSeleccionada.ProximoSeguimiento);
    }

    [Fact]
    public void Guardar_AñadeElHitoEnviadaAlHistorial()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        vm.NuevaCommand.Execute(null);
        vm.Edicion!.Empresa = "ACME";
        vm.Edicion.Puesto = "Desarrollador";
        vm.GuardarCommand.Execute(null);

        Assert.NotEmpty(vm.SolicitudSeleccionada!.Eventos);
        Assert.Contains(vm.SolicitudSeleccionada.Eventos,
            e => e.Tipo == TipoEvento.SolicitudEnviada);
    }

    [Fact]
    public void Duplicar_CopiaLoQueDescribeLaOfertaPeroNoElProceso()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        vm.NuevaCommand.Execute(null);
        vm.Edicion!.Empresa = "ACME";
        vm.Edicion.Puesto = "Desarrollador";
        vm.Edicion.ContactoNombre = "Jefa";
        vm.Edicion.FechaSolicitud = DateTime.Today.AddDays(-20);
        vm.Edicion.ProximoSeguimiento = DateTime.Today.AddDays(-5);
        vm.GuardarCommand.Execute(null);

        vm.DuplicarCommand.Execute(null);

        Assert.NotNull(vm.Edicion);
        Assert.Equal(0, vm.Edicion!.Id); // es un borrador nuevo
        Assert.Equal("ACME", vm.Edicion.Empresa);
        Assert.Equal("Desarrollador", vm.Edicion.Puesto);
        Assert.Equal(EstadoSolicitud.Enviada, vm.Edicion.Estado);
        Assert.Equal(DateTime.Today, vm.Edicion.FechaSolicitud);
        Assert.Null(vm.Edicion.ContactoNombre); // el contacto no se copia
        Assert.Contains("Duplicada", vm.Edicion.Notas!);
    }

    [Fact]
    public void Cancelar_BorradorSinGuardar_DescartaEdicion()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        vm.NuevaCommand.Execute(null);
        Assert.NotNull(vm.Edicion);
        vm.CancelarCommand.Execute(null);

        Assert.Null(vm.Edicion);
        Assert.Empty(vm.Solicitudes);
    }

    [Fact]
    public void EstablecerInteres_FijaElValorDelPanel()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        vm.NuevaCommand.Execute(null);
        Assert.NotNull(vm.Edicion);

        vm.EstablecerInteresCommand.Execute("5");
        Assert.Equal(5, vm.Edicion!.Interes);

        vm.EstablecerInteresCommand.Execute("1");
        Assert.Equal(1, vm.Edicion.Interes);
    }

    [Fact]
    public void FiltroSoloAbiertas_ExcluyeLasCerradas()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        db.Solicitudes.Add(new Solicitud { Empresa = "Abierta", Puesto = "A" });
        db.Solicitudes.Add(new Solicitud { Empresa = "Cerrada", Puesto = "B", Estado = EstadoSolicitud.Rechazada });
        db.SaveChanges();
        vm.Recargar();

        vm.SoloAbiertas = true;

        Assert.Single(vm.Solicitudes);
        Assert.Equal("Abierta", vm.Solicitudes[0].Empresa);
    }

    [Fact]
    public void FiltroSeguimientoPendiente_MuestraSoloLosVencidosAbiertos()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        db.Solicitudes.Add(new Solicitud
        {
            Empresa = "Vencida",
            Puesto = "A",
            ProximoSeguimiento = DateTime.Today.AddDays(-1)
        });
        db.Solicitudes.Add(new Solicitud
        {
            Empresa = "Futura",
            Puesto = "B",
            ProximoSeguimiento = DateTime.Today.AddDays(3)
        });
        db.SaveChanges();
        vm.Recargar();

        vm.SoloConSeguimientoPendiente = true;

        Assert.Single(vm.Solicitudes);
        Assert.Equal("Vencida", vm.Solicitudes[0].Empresa);
    }

    [Fact]
    public void TextoBusqueda_IgnoraTildesYMayusculas()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        db.Solicitudes.Add(new Solicitud { Empresa = "Grupo Métricas", Puesto = "Data" });
        db.Solicitudes.Add(new Solicitud { Empresa = "Otra empresa", Puesto = "Dev" });
        db.SaveChanges();
        vm.Recargar();

        vm.TextoBusqueda = "metricas";

        Assert.Single(vm.Solicitudes);
        Assert.Equal("Grupo Métricas", vm.Solicitudes[0].Empresa);
    }

    [Fact]
    public void Estadisticas_CalculanTasaYMediaDeRespuesta()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);
        var baseFecha = new DateTime(2026, 1, 10);

        db.Solicitudes.Add(new Solicitud
        {
            Empresa = "A",
            Puesto = "P",
            FechaSolicitud = baseFecha,
            FechaPrimeraRespuesta = baseFecha.AddDays(3)
        });
        db.Solicitudes.Add(new Solicitud { Empresa = "B", Puesto = "P", FechaSolicitud = baseFecha });
        db.SaveChanges();
        vm.Recargar();

        Assert.Equal(2, vm.TotalSolicitudes);
        Assert.Equal(2, vm.ProcesosAbiertos);
        Assert.Equal(0, vm.OfertasRecibidas);
        Assert.Equal("50 %", vm.TasaRespuesta);
        Assert.Equal("3 días", vm.MediaDiasRespuesta);
    }

    [Fact]
    public void SeguimientosVencidosAhora_DevuelveSoloLasAbiertasVencidas()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        db.Solicitudes.Add(new Solicitud
        {
            Empresa = "Vencida abierta",
            Puesto = "A",
            ProximoSeguimiento = DateTime.Today.AddDays(-1)
        });
        db.Solicitudes.Add(new Solicitud
        {
            Empresa = "Cerrada vencida",
            Puesto = "B",
            Estado = EstadoSolicitud.Rechazada,
            ProximoSeguimiento = DateTime.Today.AddDays(-2)
        });
        db.Solicitudes.Add(new Solicitud
        {
            Empresa = "Abierta al día",
            Puesto = "C",
            ProximoSeguimiento = DateTime.Today.AddDays(5)
        });
        db.SaveChanges();
        vm.Recargar();

        List<Solicitud> vencidos = vm.SeguimientosVencidosAhora();

        Assert.Single(vencidos);
        Assert.Equal("Vencida abierta", vencidos[0].Empresa);
    }

    [Fact]
    public void ConfigurarGrafica_ActivaElEmbudoConDoceMeses()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        db.Solicitudes.Add(new Solicitud { Empresa = "A", Puesto = "P" });
        db.SaveChanges();

        vm.ConfigurarGraficaCommand.Execute(null);

        Assert.True(vm.VerGrafica);
        Assert.Equal(4, vm.SerieEmbudo.Length);
        Assert.Equal(12, vm.EjesXEmbudo[0].Labels!.Count);
    }
}