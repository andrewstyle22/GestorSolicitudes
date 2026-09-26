using Xunit;
using GestorSolicitudes.Helpers;
using GestorSolicitudes.Models;
using GestorSolicitudes.ViewModels;

namespace GestorSolicitudes.Tests;

public class ViewModelStaticTests
{
    // ---------------- Normalización de búsqueda ----------------

    [Fact]
    public void NormalizarBusqueda_QuitaTildesYMayusculas()
    {
        Assert.Equal("metrica", CsvHelper.NormalizarBusqueda("Métrica"));
        Assert.Equal("aeiou", CsvHelper.NormalizarBusqueda("ÁÉÍÓÚ"));
        Assert.Equal("nino", CsvHelper.NormalizarBusqueda("Ñino"));
    }

    // ---------------- Escapado para el CSV de exportación ----------------

    [Fact]
    public void Escapar_EncierraEntreComillas()
    {
        Assert.Equal("\"ACME\"", CsvHelper.Escapar("ACME"));
    }

    [Fact]
    public void Escapar_ComillasInternasSeDoblan()
    {
        Assert.Equal("\"Dijo \"\"hola\"\"\"", CsvHelper.Escapar("Dijo \"hola\""));
    }

    [Fact]
    public void Escapar_SaltosDeLineaSeSustituyenPorEspacios()
    {
        Assert.Equal("\"dos líneas\"", CsvHelper.Escapar("dos\nlíneas"));
    }

    [Fact]
    public void Escapar_NuloOVacioDevuelveCadenaVacia()
    {
        Assert.Equal(string.Empty, CsvHelper.Escapar(null));
        Assert.Equal(string.Empty, CsvHelper.Escapar(""));
    }

    // ---------------- Embudo ----------------

    [Fact]
    public void EnviadaEnMes_ReconoceElHitoDeEnvioDentroDelMes()
    {
        var conHito = new Solicitud
        {
            Eventos = { new Evento { Fecha = new DateTime(2026, 2, 10), Tipo = TipoEvento.SolicitudEnviada } },
        };

        Assert.True(MainViewModel.EnviadaEnMes(conHito, new DateTime(2026, 2, 1), new DateTime(2026, 3, 1)));
    }

    [Fact]
    public void EnviadaEnMes_ReconoceLaFechaLibroCuandoNoHayHito()
    {
        var sinHito = new Solicitud { FechaSolicitud = new DateTime(2026, 2, 15) };

        Assert.True(MainViewModel.EnviadaEnMes(sinHito, new DateTime(2026, 2, 1), new DateTime(2026, 3, 1)));
    }

    [Fact]
    public void EnviadaEnMes_DescartaFueraDelMesYConHitoFueraDelMes()
    {
        var fueraDeRango = new Solicitud { FechaSolicitud = new DateTime(2026, 3, 1) };
        Assert.False(MainViewModel.EnviadaEnMes(fueraDeRango, new DateTime(2026, 2, 1), new DateTime(2026, 3, 1)));

        var hitoFuera = new Solicitud
        {
            FechaSolicitud = new DateTime(2026, 1, 15),
            Eventos = { new Evento { Fecha = new DateTime(2026, 1, 20), Tipo = TipoEvento.SolicitudEnviada } },
        };
        Assert.False(MainViewModel.EnviadaEnMes(hitoFuera, new DateTime(2026, 2, 1), new DateTime(2026, 3, 1)));
    }

    [Fact]
    public void EmbudoMes_AlturasSonPorcentajeDelMaximo()
    {
        var mes = new EmbudoMes("Enero", 2, 4, 6, 8);
        Assert.Equal(25.0, mes.AlturaEnviadas);
        Assert.Equal(50.0, mes.AlturaRespondidas);
        Assert.Equal(75.0, mes.AlturaEntrevistas);
        Assert.Equal(100.0, mes.AlturaOfertas);
    }

    [Fact]
    public void EmbudoMes_ConTodoACeroLasAlturasSonCero()
    {
        var mes = new EmbudoMes("Enero", 0, 0, 0, 0);
        Assert.Equal(0.0, mes.AlturaEnviadas);
        Assert.Equal(0.0, mes.AlturaRespondidas);
        Assert.Equal(0.0, mes.AlturaEntrevistas);
        Assert.Equal(0.0, mes.AlturaOfertas);
    }
}
