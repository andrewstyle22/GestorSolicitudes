namespace GestorSolicitudes.Tests;

using GestorSolicitudes.Helpers;
using GestorSolicitudes.Models;
using GestorSolicitudes.ViewModels;

public class ViewModelEmbudoTests
{
    private static readonly string[] GranularidadesEsperadas = { "Mes", "Semana", "Día" };

    [Fact]
    public void ConfigurarGrafica_ActivaElEmbudoConDoceMeses()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);

        db.Solicitudes.Add(new Solicitud { Empresa = "A", Puesto = "P" });
        db.SaveChanges();

        vm.ConfigurarGraficaCommand.Execute(null);

        Assert.True(vm.VerGrafica);
        Assert.Equal(12, vm.PeriodosEmbudo.Count);
        Assert.All(vm.PeriodosEmbudo, m =>
            Assert.InRange(m.AlturaEnviadas, 0, 100));
    }

    [Theory]
    [InlineData(GranularidadEmbudo.Mes, "Grafica.TituloMes")]
    [InlineData(GranularidadEmbudo.Semana, "Grafica.TituloSemana")]
    [InlineData(GranularidadEmbudo.Dia, "Grafica.TituloDia")]
    public void TituloGrafica_UsaLaTraduccionDeLaGranularidad(GranularidadEmbudo granularidad, string clave)
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);

        vm.GranularidadGrafica = granularidad;

        Assert.Contains(Localizacion.Texto(clave), vm.TituloGrafica);
        Assert.DoesNotContain("Grafica.Titulo", vm.TituloGrafica);
        Assert.DoesNotContain("por por", vm.TituloGrafica);
    }

    [Fact]
    public void Granularidad_SemanaMuestraLasDoceSemanasHastaLaDelHoy()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
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
        using var vm = new MainViewModel(db);
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
        using var vm = new MainViewModel(db);

        // El desplegable arranca en el primer item de la lista, que es el mes.
        Assert.Same(vm.GranularidadesEmbudo[0], vm.GranularidadEmbudoItem);

        vm.GranularidadEmbudoItem = vm.GranularidadesEmbudo.First(g => Equals(g.Valor, GranularidadEmbudo.Dia));

        Assert.Equal(GranularidadEmbudo.Dia, vm.GranularidadGrafica);
    }

    [Fact]
    public void GranularidadEmbudoItem_EnNullNoCambiaLaGranularidad()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
        vm.GranularidadEmbudoItem = vm.GranularidadesEmbudo.First(g => Equals(g.Valor, GranularidadEmbudo.Semana));

        vm.GranularidadEmbudoItem = null;

        Assert.Equal(GranularidadEmbudo.Semana, vm.GranularidadGrafica);
    }

    [Fact]
    public void GranularidadEmbudoItem_SobreviveAlCambioDeIdioma()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
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
        using var vm = new MainViewModel(db);
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

        Assert.Equal(Idioma.Castellano, vm.IdiomaActual);
        // La asignación del filtro no debe sumar una recarga extra a la explícita.
        Assert.Equal(1, recargas);
        // Los botones de la gráfica usan un convertidor y hay que reevaluarlos.
        Assert.Contains(nameof(MainViewModel.VerGrafica), notificadas);
        // El rango de paginación es texto traducido y también se repinta.
        Assert.Contains(nameof(MainViewModel.RangoPagina), notificadas);
    }

    [Fact]
    public void IdiomaActual_UsaElCambioInyectadoSinTocarElIdiomaGlobal()
    {
        using var db = TestDb.NuevoContexto();
        Idioma? aplicado = null;
        using var vm = new MainViewModel(db, cambiarIdioma: idioma => aplicado = idioma);
        Idioma global = Localizacion.IdiomaActual;

        vm.IdiomaActual = Idioma.Ingles;

        Assert.Equal(Idioma.Ingles, aplicado);
        Assert.Equal(global, Localizacion.IdiomaActual);
    }

    [Fact]
    public void PeriodoAnteriorY_Siguiente_MuevenLaVentanaYNoPasanDelHoy()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
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
        using var vm = new MainViewModel(db);
        vm.VerGrafica = true;
        vm.GranularidadGrafica = GranularidadEmbudo.Dia;

        vm.PeriodoAnteriorCommand.Execute(null);
        vm.PeriodoAnteriorCommand.Execute(null);
        Assert.Equal(2, vm.DesplazamientoPeriodo);

        vm.IrAHoyCommand.Execute(null);

        Assert.Equal(0, vm.DesplazamientoPeriodo);
        Assert.False(vm.PuedeAvanzarPeriodo);
        Assert.Equal(
            EmbudoHelper.EtiquetaDePeriodo(DateTime.Today, GranularidadEmbudo.Dia, Localizacion.CulturaActual),
            vm.PeriodosEmbudo[^1].Etiqueta);
    }

    [Fact]
    public void RecargarEmbudoSiVisible_ConLaGraficaVisibleRefrescaLosMeses()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
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
        using var vm = new MainViewModel(db);
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
}