using GestorSolicitudes.Data;
using GestorSolicitudes.Models;
using GestorSolicitudes.ViewModels;

namespace GestorSolicitudes.Tests;

public class ViewModelPaginacionTests
{
    private static readonly int[] TamanosPaginaEsperados = { 5, 10, 20, 30 };

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
        using (vm)
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
        using var vm = new MainViewModel(db);

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
        using (vm)
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
        using (vm)
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
        using (vm)
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
        using (vm)
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
        using (vm)
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
