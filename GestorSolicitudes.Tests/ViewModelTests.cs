using Xunit;
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
    public void QuitarCv_LimpiaLaRutaSinBorrarUnFicheroAjero()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);
        string rutaCv = Path.Combine(TestDb.NuevaCarpeta(), "cv.pdf");
        File.WriteAllText(rutaCv, "contenido");
        vm.NuevaCommand.Execute(null);
        vm.Edicion!.RutaCv = rutaCv;

        vm.QuitarCvCommand.Execute(null);

        Assert.Null(vm.Edicion.RutaCv);
        // Solo se borran ficheros de la carpeta de adjuntos: uno ajeno se deja quieto.
        Assert.True(File.Exists(rutaCv));
    }

    [Fact]
    public void QuitarCarta_LimpiaLaRutaSinBorrarUnFicheroAjero()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);
        string rutaCarta = Path.Combine(TestDb.NuevaCarpeta(), "carta.pdf");
        File.WriteAllText(rutaCarta, "contenido");
        vm.NuevaCommand.Execute(null);
        vm.Edicion!.RutaCarta = rutaCarta;

        vm.QuitarCartaCommand.Execute(null);

        Assert.Null(vm.Edicion.RutaCarta);
        Assert.True(File.Exists(rutaCarta));
    }

    [Fact]
    public void QuitarCv_SinEdicionNoHaceNada()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db);

        vm.QuitarCvCommand.Execute(null);

        Assert.Null(vm.Edicion);
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

        Assert.Equal(12, vm.MesesEmbudo.Count);
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
            Assert.Equal(new[] { 5, 10, 20, 30 }, vm.TamanosPagina);
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