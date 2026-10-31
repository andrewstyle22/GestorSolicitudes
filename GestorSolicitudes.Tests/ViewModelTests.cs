namespace GestorSolicitudes.Tests;

using System.Globalization;
using GestorSolicitudes.Helpers;
using GestorSolicitudes.Models;
using GestorSolicitudes.ViewModels;
using Microsoft.EntityFrameworkCore;

public class ViewModelTests
{
    [Fact]
    public void Constructor_CargaListaVacia()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);

        Assert.Empty(vm.Solicitudes);
        Assert.Equal(0, vm.TotalSolicitudes);
        Assert.Null(vm.SolicitudSeleccionada);
        Assert.Equal("—", vm.TasaRespuesta);
        // El filtro por defecto es "Todos los estados"
        Assert.Null(vm.EstadoFiltroItem!.Valor);
        Assert.Equal(Localizacion.Texto("Filtro.TodosLosEstados"), vm.EstadoFiltroItem.Descripcion);
    }

    [Fact]
    public void Nueva_CreaBorradorParaHoyConEstadoEnviada()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);

        vm.NuevaCommand.Execute(null);

        Assert.NotNull(vm.Edicion);
        Assert.Equal(0, vm.Edicion!.Id);
        Assert.Equal(DateTime.Today, vm.Edicion.FechaSolicitud);
        Assert.Equal(EstadoSolicitud.Enviada, vm.Edicion.Estado);
    }

    [Fact]
    public void RellenarObligatorios_AsignaEmpresaYPuesto()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
        vm.NuevaCommand.Execute(null);

        vm.RellenarObligatorios("ACME", "Dev");

        Assert.Equal("ACME", vm.Edicion!.Empresa);
        Assert.Equal("Dev", vm.Edicion.Puesto);
    }

    [Fact]
    public void Guardar_NuevaCandidaturaValida_PersisteYCierraElPanel()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);

        vm.NuevaCommand.Execute(null);
        vm.RellenarObligatorios("ACME", "Desarrollador");
        vm.Edicion!.ContactoNombre = "Jefa";
        vm.GuardarCommand.Execute(null);

        Assert.Equal(1, vm.TotalSolicitudes);
        Solicitud guardada = Assert.Single(vm.Solicitudes);
        Assert.Equal(1, guardada.Id);
        Assert.Equal("ACME", guardada.Empresa);
        Assert.Equal("Jefa", guardada.ContactoNombre);
        // Si el proceso sigue abierto, se propone el seguimiento a +7 días.
        Assert.Equal(DateTime.Today.AddDays(7), guardada.ProximoSeguimiento);
        Assert.Null(vm.Edicion);
        Assert.Null(vm.Editor);
        Assert.Null(vm.SolicitudSeleccionada);
    }

    [Fact]
    public void Guardar_FilaExistenteSinSeguimiento_ProponeFecha()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.SolicitudSeleccionada = solicitud;

        vm.GuardarCommand.Execute(null);

        Assert.Equal(DateTime.Today.AddDays(7), db.Solicitudes.Single().ProximoSeguimiento);
    }

    [Fact]
    public void Guardar_FilaExistenteConSeguimiento_ConservaLaFecha()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
        var seguimiento = DateTime.Today.AddDays(2);
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").ConProximoSeguimiento(seguimiento).Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.SolicitudSeleccionada = solicitud;

        vm.GuardarCommand.Execute(null);

        Assert.Equal(seguimiento, db.Solicitudes.Single().ProximoSeguimiento);
    }

    [Fact]
    public void Guardar_AñadeElHitoEnviadaAlHistorial()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);

        vm.NuevaCommand.Execute(null);
        vm.RellenarObligatorios("ACME", "Desarrollador");
        vm.GuardarCommand.Execute(null);

        Solicitud guardada = Assert.Single(vm.Solicitudes);
        // El historial no viene en la recarga: lo carga el panel al abrir la fila.
        db.Entry(guardada).Collection(s => s.Eventos).Load();
        Assert.NotEmpty(guardada.Eventos);
        Assert.Contains(guardada.Eventos, e => e.Tipo == TipoEvento.SolicitudEnviada);
    }

    [Fact]
    public void Duplicar_CopiaLoQueDescribeLaOfertaPeroNoElProceso()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);

        vm.NuevaCommand.Execute(null);
        vm.RellenarObligatorios("ACME", "Desarrollador");
        vm.Edicion!.ContactoNombre = "Jefa";
        vm.Editor!.FechaSolicitud = DateTime.Today.AddDays(-20);
        vm.Editor.ProximoSeguimiento = DateTime.Today.AddDays(-5);
        vm.GuardarCommand.Execute(null);

        vm.SolicitudSeleccionada = Assert.Single(vm.Solicitudes);
        vm.DuplicarCommand.Execute(null);

        Assert.NotNull(vm.Edicion);
        Assert.Equal(0, vm.Edicion!.Id); // es un borrador nuevo
        Assert.Equal("ACME", vm.Edicion.Empresa);
        Assert.Equal("Desarrollador", vm.Edicion.Puesto);
        Assert.Equal(EstadoSolicitud.Enviada, vm.Edicion.Estado);
        Assert.Equal(DateTime.Today, vm.Edicion.FechaSolicitud);
        Assert.Null(vm.Edicion.ContactoNombre); // el contacto no se copia
        Assert.Contains("Duplicada", vm.Edicion.Notas!);
        // La fecha va en el formato corto del idioma activo, no fija en dd/MM/yyyy.
        Assert.Contains(
            DateTime.Today.AddDays(-20).ToString("d", Localizacion.CulturaActual),
            vm.Edicion.Notas!);
    }

    [Fact]
    public void Cancelar_BorradorSinGuardar_DescartaEdicion()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);

        vm.NuevaCommand.Execute(null);
        Assert.NotNull(vm.Edicion);
        vm.CancelarCommand.Execute(null);

        Assert.Null(vm.Edicion);
        Assert.Empty(vm.Solicitudes);
    }

    [Fact]
    public void SeleccionarUnaFila_AbreElPanelDeDetalle()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
        db.Solicitudes.Add(new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir());
        db.SaveChanges();
        vm.Recargar();

        vm.SolicitudSeleccionada = Assert.Single(vm.Solicitudes);

        Assert.NotNull(vm.Edicion);
        Assert.Equal("ACME", vm.Edicion!.Empresa);
        Assert.NotNull(vm.Editor);
        Assert.Equal("ACME", vm.Editor.Empresa);
    }

    [Fact]
    public void FiltroSoloAbiertas_ExcluyeLasCerradas()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);

        db.Solicitudes.Add(new SolicitudBuilder().ConEmpresa("Abierta").ConPuesto("A").Construir());
        db.Solicitudes.Add(new SolicitudBuilder().ConEmpresa("Cerrada").ConPuesto("B").ConEstado(EstadoSolicitud.Rechazada).Construir());
        db.SaveChanges();
        vm.Recargar();

        vm.SoloAbiertas = true;

        Assert.Single(vm.Solicitudes);
        Assert.Equal("Abierta", vm.Solicitudes[0].Empresa);
    }

    [Fact]
    public void Estadisticas_MidenLaBaseEnteraAunqueLaTablaEsteFiltrada()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
        db.Solicitudes.Add(new SolicitudBuilder().ConEmpresa("Abierta").ConPuesto("A").Construir());
        db.Solicitudes.Add(new SolicitudBuilder().ConEmpresa("Cerrada").ConPuesto("B").ConEstado(EstadoSolicitud.Rechazada).Construir());
        db.SaveChanges();
        vm.Recargar();

        vm.SoloAbiertas = true;

        // El filtro recorta la tabla, no la cabecera.
        Assert.Single(vm.Solicitudes);
        Assert.Equal(2, vm.TotalSolicitudes);
        Assert.Equal(1, vm.ProcesosAbiertos);
    }

    [Fact]
    public void FiltroSeguimientoPendiente_MuestraSoloLosVencidosAbiertos()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);

        db.Solicitudes.Add(new SolicitudBuilder()
            .ConEmpresa("Vencida")
            .ConPuesto("A")
            .ConProximoSeguimiento(DateTime.Today.AddDays(-1))
            .Construir());
        db.Solicitudes.Add(new SolicitudBuilder()
            .ConEmpresa("Futura")
            .ConPuesto("B")
            .ConProximoSeguimiento(DateTime.Today.AddDays(3))
            .Construir());
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
        using var vm = new MainViewModel(db);

        db.Solicitudes.Add(new SolicitudBuilder().ConEmpresa("Grupo Métricas").ConPuesto("Data").Construir());
        db.Solicitudes.Add(new SolicitudBuilder().ConEmpresa("Otra empresa").ConPuesto("Dev").Construir());
        db.SaveChanges();
        vm.Recargar();

        vm.TextoBusqueda = "metricas";

        Assert.Single(vm.Solicitudes);
        Assert.Equal("Grupo Métricas", vm.Solicitudes[0].Empresa);
    }

    [Fact]
    public void TextoBusqueda_EncuentraTecnologiaYPortalEnLaMismaPasada()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
        db.Solicitudes.Add(new SolicitudBuilder()
            .ConEmpresa("ACME")
            .ConPuesto("Dev")
            .ConTecnologias("Kubernetes, C#")
            .ConPortal("InfoJobs")
            .Construir());
        db.Solicitudes.Add(new SolicitudBuilder().ConEmpresa("Otra").ConPuesto("Dev").Construir());
        db.SaveChanges();
        vm.Recargar();

        vm.TextoBusqueda = "kubernetes";
        Assert.Single(vm.Solicitudes);

        vm.TextoBusqueda = "infojobs";
        Assert.Single(vm.Solicitudes);
        Assert.Equal("ACME", vm.Solicitudes[0].Empresa);
    }

    [Fact]
    public void Recargar_NoTraeElHistorialYSeleccionarLaFilaSiLoCarga()
    {
        using var db = TestDb.NuevoContexto();
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir();
        solicitud.Eventos.Add(new Evento { Fecha = DateTime.Today, Tipo = TipoEvento.Nota });
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        using var vm = new MainViewModel(db);

        // Se olvida lo que había en memoria para que la consulta sea la única fuente.
        db.ChangeTracker.Clear();
        vm.Recargar();
        Assert.Empty(vm.Solicitudes[0].Eventos);

        vm.SolicitudSeleccionada = vm.Solicitudes[0];

        Assert.Single(vm.SolicitudSeleccionada!.Eventos);
    }

    [Fact]
    public void Recargar_RecalculaElSueloDelEmbudo()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);

        // Con la base vacía no hay suelo: se puede retroceder.
        Assert.True(vm.PuedeRetrocederPeriodo);

        db.Solicitudes.Add(new SolicitudBuilder().ConEmpresa("A").ConPuesto("P").ConFechaSolicitud(DateTime.Today).Construir());
        db.SaveChanges();
        vm.Recargar();

        // La única solicitud está en el periodo actual: no queda nada más atrás.
        Assert.False(vm.PuedeRetrocederPeriodo);
    }

    [Fact]
    public void LimpiarBusqueda_VaciaElTextoYRestauraLaListaCompleta()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);

        db.Solicitudes.Add(new SolicitudBuilder().ConEmpresa("Grupo Métricas").ConPuesto("Data").Construir());
        db.Solicitudes.Add(new SolicitudBuilder().ConEmpresa("Otra empresa").ConPuesto("Dev").Construir());
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
        using var vm = new MainViewModel(db);
        var baseFecha = new DateTime(2026, 1, 10);

        db.Solicitudes.Add(new SolicitudBuilder()
            .ConEmpresa("A")
            .ConPuesto("P")
            .ConFechaSolicitud(baseFecha)
            .ConFechaPrimeraRespuesta(baseFecha.AddDays(3))
            .Construir());
        db.Solicitudes.Add(new SolicitudBuilder().ConEmpresa("B").ConPuesto("P").ConFechaSolicitud(baseFecha).Construir());
        db.SaveChanges();
        vm.Recargar();

        Assert.Equal(2, vm.TotalSolicitudes);
        Assert.Equal(2, vm.ProcesosAbiertos);
        Assert.Equal(0, vm.OfertasRecibidas);
        Assert.Equal(Porcentaje(0.5), vm.TasaRespuesta);
        Assert.Equal(DiasDeMedia(3), vm.MediaDiasRespuesta);
    }

    [Fact]
    public void Estadisticas_NoDependenDeLaCulturaDelHilo()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
        var baseFecha = new DateTime(2026, 1, 10);

        db.Solicitudes.Add(new SolicitudBuilder()
            .ConEmpresa("A")
            .ConPuesto("P")
            .ConFechaSolicitud(baseFecha)
            .ConFechaPrimeraRespuesta(baseFecha.AddDays(2))
            .Construir());
        db.Solicitudes.Add(new SolicitudBuilder()
            .ConEmpresa("B")
            .ConPuesto("P")
            .ConFechaSolicitud(baseFecha)
            .ConFechaPrimeraRespuesta(baseFecha.AddDays(5))
            .Construir());
        db.Solicitudes.Add(new SolicitudBuilder().ConEmpresa("C").ConPuesto("P").ConFechaSolicitud(baseFecha).Construir());
        db.SaveChanges();

        // En GitHub Actions el hilo corre con en-US (o invariable); si el ViewModel
        // formateara con la cultura del hilo daría "67%" y "3.5 días". Debe dar siempre es-ES.
        CultureInfo actual = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        try
        {
            vm.Recargar();

            Assert.Equal(Porcentaje(2.0 / 3.0), vm.TasaRespuesta);
            Assert.Equal(DiasDeMedia(3.5), vm.MediaDiasRespuesta);
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
        using var vm = new MainViewModel(db);

        db.Solicitudes.Add(new SolicitudBuilder()
            .ConEmpresa("Vencida abierta")
            .ConPuesto("A")
            .ConProximoSeguimiento(DateTime.Today.AddDays(-1))
            .Construir());
        db.Solicitudes.Add(new SolicitudBuilder()
            .ConEmpresa("Cerrada vencida")
            .ConPuesto("B")
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConProximoSeguimiento(DateTime.Today.AddDays(-2))
            .Construir());
        db.Solicitudes.Add(new SolicitudBuilder()
            .ConEmpresa("Abierta al día")
            .ConPuesto("C")
            .ConProximoSeguimiento(DateTime.Today.AddDays(5))
            .Construir());
        db.SaveChanges();
        vm.Recargar();

        List<SeguimientoVencido> vencidos = vm.SeguimientosVencidosAhora();

        Assert.Single(vencidos);
        Assert.Equal("Vencida abierta", vencidos[0].Empresa);
    }

    [Fact]
    public void VerCandidaturasDeEstaEmpresa_RellenaLaBusquedaConLaEmpresa()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
        vm.NuevaCommand.Execute(null);
        vm.Edicion!.Empresa = "ACME";

        vm.VerCandidaturasDeEstaEmpresaCommand.Execute(null);

        Assert.Equal("ACME", vm.TextoBusqueda);
    }

    [Fact]
    public void VerCandidaturasDeEstaEmpresa_SinEdicionOSinEmpresaNoTocaElFiltro()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);

        vm.VerCandidaturasDeEstaEmpresaCommand.Execute(null);
        Assert.Equal(string.Empty, vm.TextoBusqueda);

        vm.NuevaCommand.Execute(null);
        vm.VerCandidaturasDeEstaEmpresaCommand.Execute(null);
        Assert.Equal(string.Empty, vm.TextoBusqueda);
    }

    // ---------------- Salidas tempranas y recarga ----------------
    [Fact]
    public void Guardar_SinEdicionNoHaceNada()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);

        vm.GuardarCommand.Execute(null);

        Assert.Empty(vm.Solicitudes);
    }

    [Fact]
    public void Duplicar_SinEdicionNoHaceNada()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);

        vm.DuplicarCommand.Execute(null);

        Assert.Null(vm.Edicion);
    }

    [Fact]
    public void Cancelar_SinEdicionNoHaceNada()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);

        vm.CancelarCommand.Execute(null);

        Assert.Null(vm.Edicion);
    }

    [Fact]
    public void Cancelar_ConCandidaturaGuardada_DesenganchaYRecargaDesdeDisco()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.Edicion = solicitud;
        vm.Edicion.Empresa = "SinGuardar";

        vm.CancelarCommand.Execute(null);

        Assert.Equal(EntityState.Detached, db.Entry(solicitud).State);
        Assert.Null(vm.Edicion);
        Assert.Null(vm.Editor);
        Assert.Null(vm.SolicitudSeleccionada);
        Assert.Equal("ACME", Assert.Single(vm.Solicitudes).Empresa);
    }

    [Fact]
    public void TryEliminarSolicitud_BorraLaFilaYDevuelveTrue()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();

        bool borrada = vm.TryEliminarSolicitud(solicitud, out Exception? error);

        Assert.True(borrada);
        Assert.Null(error);
        Assert.Empty(db.Solicitudes);
    }

    [Fact]
    public void TryEliminarSolicitud_CuandoFallaLaBase_DevuelveFalseYElError()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        db.Database.ExecuteSqlRaw("DROP TABLE Solicitudes");

        bool borrada = vm.TryEliminarSolicitud(solicitud, out Exception? error);

        Assert.False(borrada);
        Assert.NotNull(error);
    }

    [Fact]
    public void Dispose_NoLanzaAunqueSeLlameDosVeces()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);

        Exception? excepcion = Record.Exception(() =>
        {
            vm.Dispose();
            vm.Dispose();
        });

        Assert.Null(excepcion);
    }

    [Fact]
    public void EstadoFiltro_FiltraPorUnEstadoConcreto()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
        db.Solicitudes.Add(new SolicitudBuilder().ConEmpresa("Enviada").ConPuesto("A").ConEstado(EstadoSolicitud.Enviada).Construir());
        db.Solicitudes.Add(new SolicitudBuilder().ConEmpresa("Rechazada").ConPuesto("B").ConEstado(EstadoSolicitud.Rechazada).Construir());
        db.SaveChanges();
        vm.Recargar();

        EnumItem enviada = vm.EstadosFiltro.First(i => Equals(i.Valor, EstadoSolicitud.Enviada));
        vm.EstadoFiltroItem = enviada;

        Solicitud sola = Assert.Single(vm.Solicitudes);
        Assert.Equal("Enviada", sola.Empresa);
    }

    // Mismo formato que la cabecera, con la cultura del idioma activo: los tests comparan el
    // número formateado, no el texto traducido congelado.
    private static string Porcentaje(double valor) => valor.ToString("P0", Localizacion.CulturaActual);

    private static string DiasDeMedia(double dias) =>
        $"{dias.ToString("0.#", Localizacion.CulturaActual)} {Localizacion.Texto("Metrica.Dias")}";
}
