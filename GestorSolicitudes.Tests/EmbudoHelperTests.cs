using System.Globalization;
using GestorSolicitudes.Helpers;
using GestorSolicitudes.Models;

namespace GestorSolicitudes.Tests;

public class EmbudoHelperTests
{
    [Fact]
    public void EtiquetaDePeriodo_UsaLaCulturaQueRecibeYNoLaDelHilo()
    {
        var inicio = new DateTime(2024, 5, 1);
        CultureInfo ingles = CultureInfo.GetCultureInfo("en-US");

        Assert.Equal(
            inicio.ToString("MMM yyyy", ingles),
            EmbudoHelper.EtiquetaDePeriodo(inicio, GranularidadEmbudo.Mes, ingles));
    }

    [Fact]
    public void EnviadaEnRango_ReconoceElHitoDeEnvioDentroDelMes()
    {
        var conHito = new Solicitud
        {
            Eventos = { new Evento { Fecha = new DateTime(2026, 2, 10), Tipo = TipoEvento.SolicitudEnviada } },
        };

        Assert.True(EmbudoHelper.EnviadaEnRango(conHito, new DateTime(2026, 2, 1), new DateTime(2026, 3, 1)));
    }

    [Fact]
    public void EnviadaEnRango_ReconoceLaFechaLibroCuandoNoHayHito()
    {
        var sinHito = new Solicitud { FechaSolicitud = new DateTime(2026, 2, 15) };

        Assert.True(EmbudoHelper.EnviadaEnRango(sinHito, new DateTime(2026, 2, 1), new DateTime(2026, 3, 1)));
    }

    [Fact]
    public void EnviadaEnRango_DescartaFueraDelMesYConHitoFueraDelMes()
    {
        var fueraDeRango = new Solicitud { FechaSolicitud = new DateTime(2026, 3, 1) };

        Assert.False(EmbudoHelper.EnviadaEnRango(fueraDeRango, new DateTime(2026, 2, 1), new DateTime(2026, 3, 1)));

        var hitoFuera = new Solicitud
        {
            FechaSolicitud = new DateTime(2026, 1, 15),
            Eventos = { new Evento { Fecha = new DateTime(2026, 1, 20), Tipo = TipoEvento.SolicitudEnviada } },
        };

        Assert.False(EmbudoHelper.EnviadaEnRango(hitoFuera, new DateTime(2026, 2, 1), new DateTime(2026, 3, 1)));
    }

    [Fact]
    public void EmbudoPeriodo_AlturasSonPorcentajeDelMaximo()
    {
        var mes = new EmbudoPeriodo("Enero", 2, 4, 6, 8);
        Assert.Equal(25.0, mes.AlturaEnviadas);
        Assert.Equal(50.0, mes.AlturaRespondidas);
        Assert.Equal(75.0, mes.AlturaEntrevistas);
        Assert.Equal(100.0, mes.AlturaOfertas);
    }

    [Fact]
    public void EmbudoPeriodo_ConTodoACeroLasAlturasSonCero()
    {
        var mes = new EmbudoPeriodo("Enero", 0, 0, 0, 0);
        Assert.Equal(0.0, mes.AlturaEnviadas);
        Assert.Equal(0.0, mes.AlturaRespondidas);
        Assert.Equal(0.0, mes.AlturaEntrevistas);
        Assert.Equal(0.0, mes.AlturaOfertas);
    }
}
