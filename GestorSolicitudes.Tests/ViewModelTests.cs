using Xunit;
using System.Globalization;
using GestorSolicitudes.Helpers;
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
    public void LimpiarBusqueda_VaciaElTextoYRestauraLaListaCompleta()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        db.Solicitudes.Add(new Solicitud { Empresa = "Grupo Métricas", Puesto = "Data" });
        db.Solicitudes.Add(new Solicitud { Empresa = "Otra empresa", Puesto = "Dev" });
        db.SaveChanges();
        vm.Recargar();

        vm.TextoBusqueda = "metricas";
        Assert.Single(vm.Solicitudes);

        vm.LimpiarBusquedaCommand.Execute(null);

        Assert.Equal(string.Empty, vm.TextoBusqueda);
        Assert.Equal(2, vm.Solicitudes.Count);
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
    public void Estadisticas_NoDependenDeLaCulturaDelHilo()
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

        // En GitHub Actions el hilo corre con en-US (o invariable); si el ViewModel
        // formateara con la cultura del hilo daría "50%". Debe dar siempre es-ES.
        CultureInfo actual = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        try
        {
            Assert.Equal("50 %", vm.TasaRespuesta);
            Assert.Equal("3 días", vm.MediaDiasRespuesta);
        }
        finally
        {
            CultureInfo.CurrentCulture = actual;
        }
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
        Assert.Equal(12, vm.MesesEmbudo.Count);
        Assert.All(vm.MesesEmbudo, m =>
            Assert.InRange(m.AlturaEnviadas, 0, 100));
    }

    // ---------------- Importación LinkedIn ----------------

    private static Dictionary<string, int> ColumnasLinkedIn() =>
        CsvHelper.IdentificarColumnas(new List<string> { "Company", "Title", "Application date", "Status", "Location", "event", "UUID" });

    [Fact]
    public void ImportarFila_FilaCortaNoCuentaNada()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        var resultado = vm.ImportarFila(new List<string> { "Empresa" }, ColumnasLinkedIn(), 6, new HashSet<string>());

        Assert.Equal((0, 0, 0), resultado);
        Assert.Equal(0, vm.TotalSolicitudes);
    }

    [Fact]
    public void ImportarFila_SinEmpresaCuentaComoOmitida()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        var resultado = vm.ImportarFila(
            new List<string> { "", "Dev", "2024-05-01", "Applied", "", "", "" },
            ColumnasLinkedIn(), 6, new HashSet<string>());

        Assert.Equal((0, 0, 1), resultado);
        Assert.Equal(0, vm.TotalSolicitudes);
    }

    [Fact]
    public void ImportarFila_DuplicadaEnBaseDeDatosNoSeInserta()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);
        db.Solicitudes.Add(new Solicitud { Empresa = "ACME", Puesto = "Dev", FechaSolicitud = new DateTime(2024, 5, 1) });
        db.SaveChanges();

        var resultado = vm.ImportarFila(
            new List<string> { "ACME", "Dev", "2024-05-01", "Applied", "", "", "" },
            ColumnasLinkedIn(), 6, new HashSet<string>());

        Assert.Equal((0, 1, 0), resultado);
        vm.Recargar();
        Assert.Equal(1, vm.TotalSolicitudes);
    }

    [Fact]
    public void ImportarFila_NuevaInsertaLaCandidaturaConSuHito()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        var resultado = vm.ImportarFila(
            new List<string> { "ACME", "Dev", "2024-05-01", "Applied", "Madrid", "Entró por un referido", "abc123" },
            ColumnasLinkedIn(), 6, new HashSet<string>());

        Assert.Equal((1, 0, 0), resultado);
        db.SaveChanges();
        vm.Recargar();
        Solicitud guardada = Assert.Single(vm.Solicitudes);
        Assert.Equal("ACME", guardada.Empresa);
        Assert.Equal("LinkedIn", guardada.Portal);
        Assert.Equal("https://www.linkedin.com/jobs/view/abc123", guardada.EnlaceOferta);
        Assert.Equal(new DateTime(2024, 5, 1), guardada.FechaSolicitud);
        Assert.Contains(guardada.Eventos, e => e.Tipo == TipoEvento.SolicitudEnviada);
    }

    [Fact]
    public void ImportarLineas_ImportaValidasYCuentaDuplicadasYOmitidas()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        var lineas = new List<List<string>>
        {
            new() { "Company", "Title", "Application date", "Status", "Location", "event", "UUID" },
            new() { "ACME", "Dev", "2024-05-01", "Applied", "Madrid", "", "abc" },
            new() { "ACME" },
            new() { "", "Dev2", "2024-05-02", "Applied", "", "", "" },
            new() { "ACME", "Dev", "2024-05-01", "Applied", "Madrid", "", "abc" },
        };

        (int Importadas, int Duplicadas, int Omitidas) resultado =
            vm.ImportarLineas(lineas, CsvHelper.IdentificarColumnas(lineas[0]));

        Assert.Equal(1, resultado.Importadas);
        Assert.Equal(1, resultado.Duplicadas);
        Assert.Equal(1, resultado.Omitidas);
        Assert.Equal(1, vm.TotalSolicitudes);
        Assert.Equal("https://www.linkedin.com/jobs/view/abc", Assert.Single(vm.Solicitudes).EnlaceOferta);
    }

    [Fact]
    public void ImportarLineas_TodoDuplicadoNoGuardaNada()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);
        db.Solicitudes.Add(new Solicitud { Empresa = "ACME", Puesto = "Dev", FechaSolicitud = new DateTime(2024, 5, 1) });
        db.SaveChanges();

        var lineas = new List<List<string>>
        {
            new() { "Company", "Title", "Application date", "Status", "Location", "event", "UUID" },
            new() { "ACME", "Dev", "2024-05-01", "Applied", "Madrid", "", "abc" },
            new() { "ACME", "Dev", "2024-05-01", "Applied", "Madrid", "", "abc" },
        };

        (int Importadas, int Duplicadas, int Omitidas) resultado =
            vm.ImportarLineas(lineas, CsvHelper.IdentificarColumnas(lineas[0]));

        Assert.Equal((0, 2, 0), resultado);
        Assert.Equal(1, vm.TotalSolicitudes);
    }

    [Fact]
    public void ValidarImportacion_ConMenosDeDosLineasDevuelveAviso()
    {
        string? aviso = MainViewModel.ValidarImportacion(new List<List<string>> { new() { "Company" } });

        Assert.Equal(Localizacion.Texto("Mensaje.CsvSinFilas"), aviso);
    }

    [Fact]
    public void ValidarImportacion_SinColumnasDeEmpresaOPuestoTambienAvisa()
    {
        string? aviso = MainViewModel.ValidarImportacion(new List<List<string>>
        {
            new() { "FechaSolicitud", "Notas" },
            new() { "2024-05-01", "sin empresa ni puesto" },
        });

        Assert.Equal(Localizacion.Texto("Mensaje.CsvColumnas"), aviso);
    }

    [Fact]
    public void ValidarImportacion_ConLineasValidasDevuelveNulo()
    {
        var lineas = new List<List<string>>
        {
            new() { "Company", "Title", "Application date", "Status", "Location", "event", "UUID" },
            new() { "ACME", "Dev", "2024-05-01", "Applied", "Madrid", "", "abc" },
        };

        string? aviso = MainViewModel.ValidarImportacion(lineas);

        Assert.Null(aviso);
    }

    // ---------------- Exportación CSV ----------------

    [Fact]
    public void ConstruirCsv_ConCandidaturasIncluyeCabeceraYFilas()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);
        db.Solicitudes.Add(new Solicitud
        {
            Empresa = "ACME",
            Puesto = "Dev",
            FechaSolicitud = new DateTime(2024, 5, 1),
            Portal = "LinkedIn",
            Estado = EstadoSolicitud.Enviada,
            EnlaceOferta = "https://www.linkedin.com/jobs/view/abc",
        });
        db.SaveChanges();

        string csv = vm.ConstruirCsv();

        Assert.Contains(Localizacion.Texto("Csv.Empresa"), csv);
        Assert.Contains("ACME", csv);
        Assert.Contains("2024-05-01", csv);
        Assert.Contains("https://www.linkedin.com/jobs/view/abc", csv);
    }
}