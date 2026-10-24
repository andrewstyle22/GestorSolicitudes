using GestorSolicitudes.Helpers;

namespace GestorSolicitudes.Tests;

public class PaginadorTests
{
    private static readonly IReadOnlyList<int> Veinticinco = Enumerable.Range(1, 25).ToList();

    [Fact]
    public void Paginar_PrimeraPaginaRecortaYHabilitaSoloAvanzar()
    {
        ResultadoPagina<int> pagina = Paginador.Paginar(Veinticinco, 10, 1);

        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, pagina.Elementos.ToArray());
        Assert.Equal(1, pagina.PaginaActual);
        Assert.Equal(3, pagina.TotalPaginas);
        Assert.Equal(0, pagina.Desde);
        Assert.Equal(10, pagina.Hasta);
        Assert.Equal(25, pagina.Total);
        Assert.False(pagina.PuedeIrAInicio);
        Assert.False(pagina.PuedeRetroceder);
        Assert.True(pagina.PuedeAvanzar);
        Assert.True(pagina.PuedeIrAlFinal);
    }

    [Fact]
    public void Paginar_UltimaPaginaParcialRecortaYHabilitaSoloRetroceder()
    {
        ResultadoPagina<int> pagina = Paginador.Paginar(Veinticinco, 10, 3);

        Assert.Equal(new[] { 21, 22, 23, 24, 25 }, pagina.Elementos.ToArray());
        Assert.Equal(3, pagina.PaginaActual);
        Assert.Equal(3, pagina.TotalPaginas);
        Assert.Equal(20, pagina.Desde);
        Assert.Equal(25, pagina.Hasta);
        Assert.True(pagina.PuedeIrAInicio);
        Assert.True(pagina.PuedeRetroceder);
        Assert.False(pagina.PuedeAvanzar);
        Assert.False(pagina.PuedeIrAlFinal);
    }

    [Fact]
    public void Paginar_SinElementosDevuelveCeroDeCeroYSinFlechas()
    {
        ResultadoPagina<int> pagina = Paginador.Paginar(Array.Empty<int>(), 10, 7);

        Assert.Empty(pagina.Elementos);
        Assert.Equal(1, pagina.PaginaActual);
        Assert.Equal(0, pagina.TotalPaginas);
        Assert.Equal(0, pagina.Desde);
        Assert.Equal(0, pagina.Hasta);
        Assert.Equal(0, pagina.Total);
        Assert.False(pagina.PuedeIrAInicio);
        Assert.False(pagina.PuedeRetroceder);
        Assert.False(pagina.PuedeAvanzar);
        Assert.False(pagina.PuedeIrAlFinal);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-3, 1)]
    [InlineData(99, 3)]
    public void Paginar_RecortaLaPaginaAlRangoValido(int pedida, int esperada)
    {
        ResultadoPagina<int> pagina = Paginador.Paginar(Veinticinco, 10, pedida);

        Assert.Equal(esperada, pagina.PaginaActual);
    }

    [Fact]
    public void Paginar_TamanoCeroSeTrataComoUno()
    {
        ResultadoPagina<int> pagina = Paginador.Paginar(Veinticinco, 0, 1);

        Assert.Single(pagina.Elementos);
        Assert.Equal(25, pagina.TotalPaginas);
        Assert.Equal(1, pagina.Hasta);
    }

    [Fact]
    public void Paginar_ListaNulaLanza()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            Paginador.Paginar<int>(null!, 10, 1);
        });
    }
}
