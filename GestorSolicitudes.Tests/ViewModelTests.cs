using System.Globalization;
using System.IO;
using Microsoft.EntityFrameworkCore;
using GestorSolicitudes.Data;
using GestorSolicitudes.Helpers;
using GestorSolicitudes.Models;
using GestorSolicitudes.ViewModels;

namespace GestorSolicitudes.Tests;

public class ViewModelTests
{
    private static readonly int[] TamanosPaginaEsperados = { 5, 10, 20, 30 };

    private static readonly string[] GranularidadesEsperadas = { "Mes", "Semana", "Día" };

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
    public void Guardar_NuevaCandidaturaValida_PersisteYCierraElPanel()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        vm.NuevaCommand.Execute(null);
        vm.Edicion!.Empresa = "ACME";
        vm.Edicion.Puesto = "Desarrollador";
        vm.Edicion.ContactoNombre = "Jefa";
        vm.GuardarCommand.Execute(null);

        Assert.Equal(1, vm.TotalSolicitudes);
        Solicitud guardada = Assert.Single(vm.Solicitudes);
        Assert.Equal(1, guardada.Id);
        Assert.Equal("ACME", guardada.Empresa);
        Assert.Equal("Jefa", guardada.ContactoNombre);
        // Si el proceso sigue abierto, se propone el seguimiento a +7 días.
        Assert.Equal(DateTime.Today.AddDays(7), guardada.ProximoSeguimiento);
        Assert.Null(vm.Edicion);
        Assert.Null(vm.SolicitudSeleccionada);
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

        Solicitud guardada = Assert.Single(vm.Solicitudes);
        Assert.NotEmpty(guardada.Eventos);
        Assert.Contains(guardada.Eventos, e => e.Tipo == TipoEvento.SolicitudEnviada);
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
        var vm = new MainViewModel(db);

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
        var vm = new MainViewModel(db);
        db.Solicitudes.Add(new Solicitud { Empresa = "ACME", Puesto = "Dev" });
        db.SaveChanges();
        vm.Recargar();

        vm.SolicitudSeleccionada = Assert.Single(vm.Solicitudes);

        Assert.NotNull(vm.Edicion);
        Assert.Equal("ACME", vm.Edicion!.Empresa);
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
            FechaPrimeraRespuesta = baseFecha.AddDays(2)
        });
        db.Solicitudes.Add(new Solicitud
        {
            Empresa = "B",
            Puesto = "P",
            FechaSolicitud = baseFecha,
            FechaPrimeraRespuesta = baseFecha.AddDays(5)
        });
        db.Solicitudes.Add(new Solicitud { Empresa = "C", Puesto = "P", FechaSolicitud = baseFecha });
        db.SaveChanges();

        // En GitHub Actions el hilo corre con en-US (o invariable); si el ViewModel
        // formateara con la cultura del hilo daría "67%" y "3.5 días". Debe dar siempre es-ES.
        CultureInfo actual = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        try
        {
            vm.Recargar();

            Assert.Equal("67 %", vm.TasaRespuesta);
            Assert.Equal("3,5 días", vm.MediaDiasRespuesta);
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
        Assert.Equal(12, vm.PeriodosEmbudo.Count);
        Assert.All(vm.PeriodosEmbudo, m =>
            Assert.InRange(m.AlturaEnviadas, 0, 100));
    }

    [Fact]
    public void Granularidad_SemanaMuestraLasDoceSemanasHastaLaDelHoy()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);
        vm.VerGrafica = true;

        vm.GranularidadGrafica = GranularidadEmbudo.Semana;

        Assert.Equal(12, vm.PeriodosEmbudo.Count);
        Assert.Equal(GranularidadesEsperadas, vm.GranularidadesEmbudo.Select(g => g.Descripcion));

        List<DateTime> semanas = EmbudoHelper.CalcularPeriodos(GranularidadEmbudo.Semana, 0);
        Assert.Equal(EmbudoHelper.InicioDePeriodo(DateTime.Today, GranularidadEmbudo.Semana), semanas[^1]);
        Assert.All(semanas, s => Assert.Equal(DayOfWeek.Monday, s.DayOfWeek));
        Assert.Equal(TimeSpan.FromDays(7), semanas[^1] - semanas[^2]);
    }

    [Fact]
    public void Granularidad_DiaMuestraLosUltimosDoceDias()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);
        vm.VerGrafica = true;

        vm.GranularidadGrafica = GranularidadEmbudo.Dia;

        Assert.Equal(12, vm.PeriodosEmbudo.Count);
        List<DateTime> dias = EmbudoHelper.CalcularPeriodos(GranularidadEmbudo.Dia, 0);
        Assert.Equal(12, dias.Count);
        Assert.Equal(DateTime.Today, dias[^1]);
        Assert.Equal(DateTime.Today.AddDays(-11), dias[0]);
    }

    [Fact]
    public void GranularidadEmbudoItem_AplicaLaGranularidadDelItemElegido()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        // El desplegable arranca en el primer item de la lista, que es el mes.
        Assert.Same(vm.GranularidadesEmbudo[0], vm.GranularidadEmbudoItem);

        vm.GranularidadEmbudoItem = vm.GranularidadesEmbudo.First(g => Equals(g.Valor, GranularidadEmbudo.Dia));

        Assert.Equal(GranularidadEmbudo.Dia, vm.GranularidadGrafica);
    }

    [Fact]
    public void GranularidadEmbudoItem_EnNullNoCambiaLaGranularidad()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);
        vm.GranularidadEmbudoItem = vm.GranularidadesEmbudo.First(g => Equals(g.Valor, GranularidadEmbudo.Semana));

        vm.GranularidadEmbudoItem = null;

        Assert.Equal(GranularidadEmbudo.Semana, vm.GranularidadGrafica);
    }

    [Fact]
    public void GranularidadEmbudoItem_SobreviveAlCambioDeIdioma()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);
        vm.GranularidadEmbudoItem = vm.GranularidadesEmbudo.First(g => Equals(g.Valor, GranularidadEmbudo.Semana));
        EnumItem itemAnterior = vm.GranularidadEmbudoItem!;

        // Lo que hace el cambio de idioma: la lista se reconstruye con objetos nuevos aunque
        // representen la misma granularidad. El desplegable debe seguir con la semana puesta.
        vm.GranularidadesEmbudo = EnumHelper.Valores<GranularidadEmbudo>();
        Assert.NotSame(itemAnterior, vm.GranularidadesEmbudo[1]);

        vm.RefrescarItemGranularidad();

        Assert.Same(vm.GranularidadesEmbudo[1], vm.GranularidadEmbudoItem);
        Assert.Equal(GranularidadEmbudo.Semana, vm.GranularidadGrafica);
    }

    [Fact]
    public void CuandoCambiaIdioma_RecargaLaListaUnaSolaVez()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);
        int recargas = 0;
        var notificadas = new List<string?>();
        vm.PropertyChanged += (_, e) =>
        {
            notificadas.Add(e.PropertyName);
            if (e.PropertyName == nameof(MainViewModel.Solicitudes))
            {
                recargas++;
            }
        };

        vm.CuandoCambiaIdioma(null, Idioma.Castellano);

        // La asignación del filtro no debe sumar una recarga extra a la explícita.
        Assert.Equal(1, recargas);
        // Los botones de la gráfica usan un convertidor y hay que reevaluarlos.
        Assert.Contains(nameof(MainViewModel.VerGrafica), notificadas);
    }

    [Fact]
    public void PeriodoAnteriorY_Siguiente_MuevenLaVentanaYNoPasanDelHoy()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);
        vm.VerGrafica = true;
        vm.GranularidadGrafica = GranularidadEmbudo.Semana;

        vm.PeriodoAnteriorCommand.Execute(null);

        Assert.Equal(1, vm.DesplazamientoPeriodo);
        Assert.True(vm.PuedeAvanzarPeriodo);

        DateTime estaSemana = EmbudoHelper.InicioDePeriodo(DateTime.Today, GranularidadEmbudo.Semana);
        Assert.Equal(estaSemana.AddDays(-7), EmbudoHelper.CalcularPeriodos(GranularidadEmbudo.Semana, 1)[^1]);

        vm.PeriodoSiguienteCommand.Execute(null);

        Assert.Equal(0, vm.DesplazamientoPeriodo);
        Assert.False(vm.PuedeAvanzarPeriodo);

        // Sin datos futuros, avanzar desde el periodo actual no hace nada.
        vm.PeriodoSiguienteCommand.Execute(null);
        Assert.Equal(0, vm.DesplazamientoPeriodo);
    }

    [Fact]
    public void IrAHoy_DevuelveLaVentanaAlPeriodoActual()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);
        vm.VerGrafica = true;
        vm.GranularidadGrafica = GranularidadEmbudo.Dia;

        vm.PeriodoAnteriorCommand.Execute(null);
        vm.PeriodoAnteriorCommand.Execute(null);
        Assert.Equal(2, vm.DesplazamientoPeriodo);

        vm.IrAHoyCommand.Execute(null);

        Assert.Equal(0, vm.DesplazamientoPeriodo);
        Assert.False(vm.PuedeAvanzarPeriodo);
        Assert.Equal(
            EmbudoHelper.EtiquetaDePeriodo(DateTime.Today, GranularidadEmbudo.Dia),
            vm.PeriodosEmbudo[^1].Etiqueta);
    }

    // ---------------- Exportación CSV ----------------

    [Fact]
    public void EscribirCsv_EnRutaValidaGuardaElContenido()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);
        string ruta = Path.Combine(TestDb.NuevaCarpeta(), "salida.csv");

        Exception? error = vm.EscribirCsv(ruta);

        Assert.Null(error);
        Assert.Contains(Localizacion.Texto("Csv.Empresa"), File.ReadAllText(ruta));
    }

    [Fact]
    public void EscribirCsv_EnRutaInvalidaDevuelveElFalloEnVezDeLanzarlo()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);
        string ruta = Path.Combine(TestDb.NuevaCarpeta(), "no", "existe", "salida.csv");

        Assert.NotNull(vm.EscribirCsv(ruta));
    }

    // ---------------- Comandos del panel de detalle ----------------

    [Fact]
    public void AnadirEvento_AnadeUnaNotaConLaFechaDeHoy()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);
        vm.NuevaCommand.Execute(null);
        vm.Edicion!.Empresa = "ACME";
        vm.Edicion.Puesto = "Dev";

        vm.AnadirEventoCommand.Execute(null);

        Evento nota = Assert.Single(vm.Edicion.Eventos);
        Assert.Equal(TipoEvento.Nota, nota.Tipo);
        Assert.Equal(DateTime.Today, nota.Fecha);
    }

    [Fact]
    public void EliminarEvento_SinEdicionNoHaceNada()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        vm.EliminarEventoCommand.Execute(new Evento());

        Assert.Empty(vm.Solicitudes);
    }

    [Fact]
    public void EliminarEvento_QuitaDeLaListaYMarcaElPersistidoComoBorrado()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);
        var solicitud = new Solicitud { Empresa = "ACME", Puesto = "Dev" };
        solicitud.Eventos.Add(new Evento { Fecha = DateTime.Today.AddDays(-1), Tipo = TipoEvento.SolicitudEnviada });
        solicitud.Eventos.Add(new Evento { Fecha = DateTime.Today, Tipo = TipoEvento.Nota });
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.Edicion = solicitud;

        Evento persistido = solicitud.Eventos.First(e => e.Id != 0);
        vm.EliminarEventoCommand.Execute(persistido);

        Assert.DoesNotContain(persistido, solicitud.Eventos);
        Assert.Equal(EntityState.Deleted, db.Entry(persistido).State);
    }

    [Fact]
    public void QuitarCv_NoBorraElFicheroHastaGuardar()
    {
        using var db = TestDb.NuevoContexto();
        string carpeta = TestDb.NuevaCarpeta();
        var vm = new MainViewModel(db, carpetaAdjuntos: carpeta);
        var solicitud = new Solicitud { Empresa = "ACME", Puesto = "Dev", RutaCv = CopiarAdjunto(carpeta, "cv.pdf") };
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.Edicion = solicitud;
        string rutaCv = solicitud.RutaCv!;

        vm.QuitarCvCommand.Execute(null);

        Assert.Null(solicitud.RutaCv);
        // El fichero sigue en disco: la base todavía lo apunta hasta que se guarde.
        Assert.True(File.Exists(rutaCv));

        vm.GuardarCommand.Execute(null);

        Assert.False(File.Exists(rutaCv));
    }

    [Fact]
    public void QuitarCarta_NoBorraElFicheroHastaGuardar()
    {
        using var db = TestDb.NuevoContexto();
        string carpeta = TestDb.NuevaCarpeta();
        var vm = new MainViewModel(db, carpetaAdjuntos: carpeta);
        var solicitud = new Solicitud { Empresa = "ACME", Puesto = "Dev", RutaCarta = CopiarAdjunto(carpeta, "carta.pdf") };
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.Edicion = solicitud;
        string rutaCarta = solicitud.RutaCarta!;

        vm.QuitarCartaCommand.Execute(null);

        Assert.Null(solicitud.RutaCarta);
        Assert.True(File.Exists(rutaCarta));

        vm.GuardarCommand.Execute(null);

        Assert.False(File.Exists(rutaCarta));
    }

    [Fact]
    public void Guardar_ConElCvSustituido_BorraElAnteriorYGuardaElNuevo()
    {
        using var db = TestDb.NuevoContexto();
        string carpeta = TestDb.NuevaCarpeta();
        var vm = new MainViewModel(db, carpetaAdjuntos: carpeta);
        var solicitud = new Solicitud { Empresa = "ACME", Puesto = "Dev", RutaCv = CopiarAdjunto(carpeta, "cv.pdf") };
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.Edicion = solicitud;
        string viejo = solicitud.RutaCv!;
        string nuevo = CopiarAdjunto(carpeta, "cv-nuevo.pdf");
        solicitud.RutaCv = nuevo;

        vm.GuardarCommand.Execute(null);

        Assert.False(File.Exists(viejo));
        Assert.True(File.Exists(nuevo));
        Assert.Equal(nuevo, db.Solicitudes.Single().RutaCv);
    }

    [Fact]
    public void Cancelar_ConElCvSustituido_ConservaElAnteriorYBorraElNuevo()
    {
        using var db = TestDb.NuevoContexto();
        string carpeta = TestDb.NuevaCarpeta();
        var vm = new MainViewModel(db, carpetaAdjuntos: carpeta);
        var solicitud = new Solicitud { Empresa = "ACME", Puesto = "Dev", RutaCv = CopiarAdjunto(carpeta, "cv.pdf") };
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.Edicion = solicitud;
        string viejo = solicitud.RutaCv!;
        string nuevo = CopiarAdjunto(carpeta, "cv-nuevo.pdf");
        solicitud.RutaCv = nuevo;

        vm.CancelarCommand.Execute(null);

        Assert.True(File.Exists(viejo));
        Assert.False(File.Exists(nuevo));
        Assert.Equal(viejo, db.Solicitudes.Single().RutaCv);
    }

    [Fact]
    public void QuitarCv_SinEdicionNoHaceNada()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        vm.QuitarCvCommand.Execute(null);

        Assert.Null(vm.Edicion);
    }

    /// <summary>Crea un fichero dentro de la carpeta de adjuntos, como haría AdjuntarCv.</summary>
    private static string CopiarAdjunto(string carpeta, string nombre)
    {
        string origen = Path.Combine(TestDb.NuevaCarpeta(), nombre);
        File.WriteAllText(origen, "contenido");
        return AdjuntosHelper.Copiar(origen, "adjunto", carpeta);
    }

    [Fact]
    public void VerCandidaturasDeEstaEmpresa_RellenaLaBusquedaConLaEmpresa()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);
        vm.NuevaCommand.Execute(null);
        vm.Edicion!.Empresa = "ACME";

        vm.VerCandidaturasDeEstaEmpresaCommand.Execute(null);

        Assert.Equal("ACME", vm.TextoBusqueda);
    }

    [Fact]
    public void VerCandidaturasDeEstaEmpresa_SinEdicionOSinEmpresaNoTocaElFiltro()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

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
        var vm = new MainViewModel(db);

        vm.GuardarCommand.Execute(null);

        Assert.Empty(vm.Solicitudes);
    }

    [Fact]
    public void Duplicar_SinEdicionNoHaceNada()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        vm.DuplicarCommand.Execute(null);

        Assert.Null(vm.Edicion);
    }

    [Fact]
    public void Cancelar_SinEdicionNoHaceNada()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        vm.CancelarCommand.Execute(null);

        Assert.Null(vm.Edicion);
    }

    [Fact]
    public void Cancelar_ConCandidaturaGuardada_DesenganchaYRecargaDesdeDisco()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);
        var solicitud = new Solicitud { Empresa = "ACME", Puesto = "Dev" };
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.Edicion = solicitud;
        vm.Edicion.Empresa = "SinGuardar";

        vm.CancelarCommand.Execute(null);

        Assert.Equal(EntityState.Detached, db.Entry(solicitud).State);
        Assert.Null(vm.Edicion);
        Assert.Null(vm.SolicitudSeleccionada);
        Assert.Equal("ACME", Assert.Single(vm.Solicitudes).Empresa);
    }

    [Fact]
    public void EliminarSolicitud_BorraLaFilaYDevuelveNull()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);
        var solicitud = new Solicitud { Empresa = "ACME", Puesto = "Dev" };
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();

        Exception? error = vm.EliminarSolicitud(solicitud);

        Assert.Null(error);
        Assert.Empty(db.Solicitudes);
    }

    [Fact]
    public void Dispose_NoLanzaAunqueSeLlameDosVeces()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

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
        var vm = new MainViewModel(db);
        db.Solicitudes.Add(new Solicitud { Empresa = "Enviada", Puesto = "A", Estado = EstadoSolicitud.Enviada });
        db.Solicitudes.Add(new Solicitud { Empresa = "Rechazada", Puesto = "B", Estado = EstadoSolicitud.Rechazada });
        db.SaveChanges();
        vm.Recargar();

        EnumItem enviada = vm.EstadosFiltro.First(i => Equals(i.Valor, EstadoSolicitud.Enviada));
        vm.EstadoFiltroItem = enviada;

        Solicitud sola = Assert.Single(vm.Solicitudes);
        Assert.Equal("Enviada", sola.Empresa);
    }

    [Fact]
    public void RecargarEmbudoSiVisible_ConLaGraficaVisibleRefrescaLosMeses()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);
        vm.VerGrafica = true;

        vm.NuevaCommand.Execute(null);
        vm.Edicion!.Empresa = "ACME";
        vm.Edicion.Puesto = "Dev";
        vm.GuardarCommand.Execute(null);

        Assert.Equal(12, vm.PeriodosEmbudo.Count);
    }

    [Fact]
    public void PeriodoAnterior_NoPasaDeLaSolicitudMasAntigua()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);
        vm.VerGrafica = true;
        vm.GranularidadGrafica = GranularidadEmbudo.Semana;

        db.Solicitudes.Add(new Solicitud
        {
            Empresa = "A",
            Puesto = "P",
            FechaSolicitud = DateTime.Today.AddDays(-10),
        });
        db.SaveChanges();

        for (int i = 0; i < 20; i++)
        {
            vm.PeriodoAnteriorCommand.Execute(null);
        }

        // Se para en la semana de la primera solicitud: más atrás el embudo iría a cero.
        DateTime suelo = EmbudoHelper.InicioDePeriodo(DateTime.Today.AddDays(-10), GranularidadEmbudo.Semana);
        Assert.Equal(suelo, EmbudoHelper.CalcularPeriodos(GranularidadEmbudo.Semana, vm.DesplazamientoPeriodo)[^1]);
        Assert.False(vm.PuedeRetrocederPeriodo);
    }

    // ---------------- Comandos con diálogos ----------------

    [Fact]
    public void Guardar_SinEmpresaOPuesto_AvisaYSigueEditando()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso();
        var vm = new MainViewModel(db, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);
        vm.Edicion!.Empresa = "ACME"; // falta el puesto

        vm.GuardarCommand.Execute(null);

        Assert.Single(dialogo.Avisos);
        Assert.NotNull(vm.Edicion);
        Assert.Empty(db.Solicitudes);
    }

    [Fact]
    public void Guardar_CuandoFallaLaBase_AvisaDeErrorYNoCierraElPanel()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso();
        var vm = new MainViewModel(db, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);
        vm.Edicion!.Empresa = "ACME";
        vm.Edicion.Puesto = "Dev";
        db.Database.ExecuteSqlRaw("DROP TABLE Solicitudes"); // que SaveChanges falle dentro del try

        vm.GuardarCommand.Execute(null);

        Assert.Single(dialogo.Errores);
        Assert.NotNull(vm.Edicion);
    }

    [Fact]
    public void AdjuntarCv_CopiaElFicheroYPoneElNombreOriginal()
    {
        using var db = TestDb.NuevoContexto();
        string carpeta = TestDb.NuevaCarpeta();
        string origen = Path.Combine(TestDb.NuevaCarpeta(), "mi-cv.pdf");
        File.WriteAllText(origen, "cv");
        var dialogo = new DialogoFalso { FicheroElegido = origen };
        var vm = new MainViewModel(db, carpetaAdjuntos: carpeta, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);

        vm.AdjuntarCvCommand.Execute(null);

        Assert.NotNull(vm.Edicion!.RutaCv);
        Assert.StartsWith(carpeta, vm.Edicion.RutaCv!);
        Assert.True(File.Exists(vm.Edicion.RutaCv));
        Assert.Equal("mi-cv.pdf", vm.Edicion.NombreOriginalCv);
    }

    [Fact]
    public void AdjuntarCarta_CopiaElFicheroYPoneElNombreOriginal()
    {
        using var db = TestDb.NuevoContexto();
        string carpeta = TestDb.NuevaCarpeta();
        string origen = Path.Combine(TestDb.NuevaCarpeta(), "carta.docx");
        File.WriteAllText(origen, "carta");
        var dialogo = new DialogoFalso { FicheroElegido = origen };
        var vm = new MainViewModel(db, carpetaAdjuntos: carpeta, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);

        vm.AdjuntarCartaCommand.Execute(null);

        Assert.NotNull(vm.Edicion!.RutaCarta);
        Assert.Equal("carta.docx", vm.Edicion.NombreOriginalCarta);
    }

    [Fact]
    public void AdjuntarCv_CuandoSeCancela_NoCambiaNada()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso { FicheroElegido = null };
        var vm = new MainViewModel(db, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);

        vm.AdjuntarCvCommand.Execute(null);

        Assert.Null(vm.Edicion!.RutaCv);
    }

    [Fact]
    public void AdjuntarCv_CuandoNoSePuedeCopiar_AvisaDeError()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso
        {
            FicheroElegido = Path.Combine(TestDb.NuevaCarpeta(), "no-existe.pdf"),
        };
        var vm = new MainViewModel(db, carpetaAdjuntos: TestDb.NuevaCarpeta(), dialogo: dialogo);
        vm.NuevaCommand.Execute(null);

        vm.AdjuntarCvCommand.Execute(null);

        Assert.Contains(
            Localizacion.Texto("Mensaje.NoSePudoAdjuntar").Replace("{0}", string.Empty),
            Assert.Single(dialogo.Errores));
        Assert.Null(vm.Edicion!.RutaCv);
    }

    [Fact]
    public void AdjuntarCarta_CuandoNoSePuedeCopiar_AvisaDeError()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso
        {
            FicheroElegido = Path.Combine(TestDb.NuevaCarpeta(), "no-existe.docx"),
        };
        var vm = new MainViewModel(db, carpetaAdjuntos: TestDb.NuevaCarpeta(), dialogo: dialogo);
        vm.NuevaCommand.Execute(null);

        vm.AdjuntarCartaCommand.Execute(null);

        Assert.Contains(
            Localizacion.Texto("Mensaje.NoSePudoAdjuntar").Replace("{0}", string.Empty),
            Assert.Single(dialogo.Errores));
        Assert.Null(vm.Edicion!.RutaCarta);
    }

    [Fact]
    public void Eliminar_CuandoSeConfirma_BorraLaFilaYLosAdjuntos()
    {
        using var db = TestDb.NuevoContexto();
        string carpeta = TestDb.NuevaCarpeta();
        var dialogo = new DialogoFalso { RespuestaConfirmar = true };
        var vm = new MainViewModel(db, carpetaAdjuntos: carpeta, dialogo: dialogo);
        var solicitud = new Solicitud { Empresa = "ACME", Puesto = "Dev", RutaCv = CopiarAdjunto(carpeta, "cv.pdf") };
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.Edicion = solicitud;
        string rutaCv = solicitud.RutaCv!;

        vm.EliminarCommand.Execute(null);

        Assert.Empty(db.Solicitudes);
        Assert.Null(vm.Edicion);
        Assert.False(File.Exists(rutaCv));
    }

    [Fact]
    public void Eliminar_CuandoElUsuarioNoConfirma_NoBorraNada()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso { RespuestaConfirmar = false };
        var vm = new MainViewModel(db, dialogo: dialogo);
        var solicitud = new Solicitud { Empresa = "ACME", Puesto = "Dev" };
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.Edicion = solicitud;

        vm.EliminarCommand.Execute(null);

        Assert.Single(dialogo.Confirmaciones);
        Assert.Equal(1, db.Solicitudes.Count());
        Assert.NotNull(vm.Edicion);
    }

    [Fact]
    public void Eliminar_CuandoFallaLaBase_AvisaDeErrorYNoCierraElPanel()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso { RespuestaConfirmar = true };
        var vm = new MainViewModel(db, dialogo: dialogo);
        var solicitud = new Solicitud { Empresa = "ACME", Puesto = "Dev" };
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.Edicion = solicitud;
        db.Database.ExecuteSqlRaw("DROP TABLE Solicitudes");

        vm.EliminarCommand.Execute(null);

        Assert.Single(dialogo.Errores);
        Assert.NotNull(vm.Edicion);
    }

    [Fact]
    public void ExportarCsv_ConDestinoEscribeEInforma()
    {
        using var db = TestDb.NuevoContexto();
        db.Solicitudes.Add(new Solicitud { Empresa = "ACME", Puesto = "Dev" });
        db.SaveChanges();
        var dialogo = new DialogoFalso { DestinoElegido = Path.Combine(TestDb.NuevaCarpeta(), "salida.csv") };
        var vm = new MainViewModel(db, dialogo: dialogo);

        vm.ExportarCsvCommand.Execute(null);

        Assert.True(File.Exists(dialogo.DestinoElegido));
        Assert.Single(dialogo.Informes);
        Assert.Empty(dialogo.Errores);
    }

    [Fact]
    public void ExportarCsv_CuandoSeCancela_NoEscribeNiInforma()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso { DestinoElegido = null };
        var vm = new MainViewModel(db, dialogo: dialogo);

        vm.ExportarCsvCommand.Execute(null);

        Assert.Empty(dialogo.Informes);
        Assert.Empty(dialogo.Errores);
    }

    [Fact]
    public void ExportarCsv_CuandoNoSePuedeEscribir_AvisaDeError()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso
        {
            DestinoElegido = Path.Combine(TestDb.NuevaCarpeta(), "no", "existe", "salida.csv"),
        };
        var vm = new MainViewModel(db, dialogo: dialogo);

        vm.ExportarCsvCommand.Execute(null);

        Assert.Single(dialogo.Errores);
        Assert.Empty(dialogo.Informes);
    }

    [Fact]
    public void AbrirEnlace_ConUrlLaAbreSinEspacios()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso();
        var vm = new MainViewModel(db, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);
        vm.Edicion!.EnlaceOferta = "  https://ejemplo.com/oferta  ";

        vm.AbrirEnlaceCommand.Execute(null);

        Assert.Equal("https://ejemplo.com/oferta", Assert.Single(dialogo.EnlacesAbiertos));
    }

    [Fact]
    public void AbrirEnlace_SinUrlNoLlamaAlNavegador()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso();
        var vm = new MainViewModel(db, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);

        vm.AbrirEnlaceCommand.Execute(null);

        Assert.Empty(dialogo.EnlacesAbiertos);
    }

    [Fact]
    public void AbrirEnlace_CuandoFalla_AvisaPeroNoRevienta()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso { AbrirEnlaceLanza = true };
        var vm = new MainViewModel(db, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);
        vm.Edicion!.EnlaceOferta = "https://ejemplo.com";

        vm.AbrirEnlaceCommand.Execute(null);

        Assert.Single(dialogo.Avisos);
    }

    // ---------------------------------------------------------------- Paginación

    /// <summary>25 candidaturas con empresa "E00".."E24" y un día distinto cada una, para que el
    /// orden por fecha (descendente, el del ViewModel) sea previsible.</summary>
    private static (AppDbContext Db, MainViewModel Vm) Con25()
    {
        var db = TestDb.NuevoContexto();
        for (int i = 1; i <= 25; i++)
        {
            db.Solicitudes.Add(new Solicitud
            {
                Empresa = $"E{i:00}",
                Puesto = "P",
                FechaSolicitud = DateTime.Today.AddDays(-i),
            });
        }

        db.SaveChanges();
        return (db, new MainViewModel(db));
    }

    [Fact]
    public void Paginacion_PorDefectoMuestraLasDiezPrimerasYUnaSolaPagina()
    {
        var (db, vm) = Con25();
        using (db)
        {
            Assert.Equal(10, vm.TamanoPagina);
            Assert.Equal(TamanosPaginaEsperados, vm.TamanosPagina);
            Assert.Equal(3, vm.TotalPaginas);
            Assert.Equal(1, vm.PaginaActual);
            Assert.Equal(10, vm.Solicitudes.Count);
            Assert.Equal("E01", vm.Solicitudes[0].Empresa);
            Assert.Equal("E10", vm.Solicitudes[9].Empresa);
            Assert.Equal("1-10 de 25", vm.RangoPagina);
        }
    }

    [Fact]
    public void Paginacion_SinResultsMuestraCeroDeCeroYNoDejaSaltarDePagina()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        Assert.Empty(vm.Solicitudes);
        Assert.Equal(0, vm.TotalPaginas);
        Assert.Equal(1, vm.PaginaActual);
        Assert.Equal("0 de 0", vm.RangoPagina);
        Assert.False(vm.PuedeAvanzar);
        Assert.False(vm.PuedeIrAlFinal);
    }

    [Fact]
    public void Paginacion_IrAdelanteMuestraLasSiguientesYRecortaLaUltima()
    {
        var (db, vm) = Con25();
        using (db)
        {
            vm.IrAdelanteCommand.Execute(null);
            Assert.Equal(2, vm.PaginaActual);
            Assert.Equal("E11", vm.Solicitudes[0].Empresa);
            Assert.Equal("11-20 de 25", vm.RangoPagina);
            Assert.True(vm.PuedeRetroceder);
            Assert.True(vm.PuedeIrAInicio);

            vm.IrAdelanteCommand.Execute(null);
            Assert.Equal(3, vm.PaginaActual);
            Assert.Equal("21-25 de 25", vm.RangoPagina);
            Assert.Equal(5, vm.Solicitudes.Count);

            // Ya no hay más: la última flecha no mueve nada.
            vm.IrAdelanteCommand.Execute(null);
            Assert.Equal(3, vm.PaginaActual);
            Assert.False(vm.PuedeAvanzar);
            Assert.False(vm.PuedeIrAlFinal);
        }
    }

    [Fact]
    public void Paginacion_IrAlFinalVaALaUltimaPaginaEIrAInicioVuelveALaPrimera()
    {
        var (db, vm) = Con25();
        using (db)
        {
            vm.IrAlFinalCommand.Execute(null);

            Assert.Equal(3, vm.PaginaActual);
            Assert.Equal("E21", vm.Solicitudes[0].Empresa);
            Assert.Equal("21-25 de 25", vm.RangoPagina);

            // En la última página solo se puede ir hacia atrás.
            Assert.True(vm.PuedeRetroceder);
            Assert.False(vm.PuedeAvanzar);
            Assert.False(vm.PuedeIrAlFinal);

            vm.IrAInicioCommand.Execute(null);

            Assert.Equal(1, vm.PaginaActual);
            Assert.Equal("E01", vm.Solicitudes[0].Empresa);
            Assert.Equal("1-10 de 25", vm.RangoPagina);
        }
    }

    [Fact]
    public void Paginacion_IrAtrasEnLaPrimeraPaginaNoHaceNada()
    {
        var (db, vm) = Con25();
        using (db)
        {
            vm.IrAtrasCommand.Execute(null);

            Assert.Equal(1, vm.PaginaActual);
            Assert.False(vm.PuedeRetroceder);
            Assert.Equal("1-10 de 25", vm.RangoPagina);
        }
    }

    [Fact]
    public void Paginacion_CambiarElTamanoRecortaLaPaginaActual()
    {
        var (db, vm) = Con25();
        using (db)
        {
            vm.TamanoPagina = 5;

            Assert.Equal(5, vm.TotalPaginas);
            Assert.Equal(1, vm.PaginaActual);
            Assert.Equal(5, vm.Solicitudes.Count);
            Assert.Equal("1-5 de 25", vm.RangoPagina);

            // 25 filas de 5 en 5: la última página es la quinta y va entera.
            vm.IrAlFinalCommand.Execute(null);
            Assert.Equal(5, vm.PaginaActual);
            Assert.Equal("21-25 de 25", vm.RangoPagina);

            vm.TamanoPagina = 20;

            Assert.Equal(2, vm.TotalPaginas);

            // La página 5 ya no existe con 20 por página, así que se recorta a la última.
            Assert.Equal(2, vm.PaginaActual);
            Assert.Equal("21-25 de 25", vm.RangoPagina);
        }
    }

    [Fact]
    public void Paginacion_FiltrarReduceElTotalYRecortaLaPagina()
    {
        var (db, vm) = Con25();
        using (db)
        {
            vm.IrAlFinalCommand.Execute(null);
            Assert.Equal(3, vm.PaginaActual);

            vm.TextoBusqueda = "E1";

            // Solo E10..E19 (10 filas, "e01" no contiene "e1"): cabe en una página, así que la
            // tercera página en la que estábamos se recorta a la primera.
            Assert.Equal(1, vm.TotalPaginas);
            Assert.Equal(1, vm.PaginaActual);
            Assert.Equal(10, vm.Solicitudes.Count);
            Assert.Equal("E10", vm.Solicitudes[0].Empresa);
            Assert.Equal("1-10 de 10", vm.RangoPagina);
        }
    }
}