using Xunit;
using System.IO;
using GestorSolicitudes.Models;
using GestorSolicitudes.ViewModels;

namespace GestorSolicitudes.Tests;

public class ViewModelStaticTests
{
    // ---------------- Normalización de búsqueda ----------------

    [Fact]
    public void NormalizarBusqueda_QuitaTildesYMayusculas()
    {
        Assert.Equal("metrica", MainViewModel.NormalizarBusqueda("Métrica"));
        Assert.Equal("aeiou", MainViewModel.NormalizarBusqueda("ÁÉÍÓÚ"));
        Assert.Equal("nino", MainViewModel.NormalizarBusqueda("Ñino"));
    }

    // ---------------- Mapeo de estado de LinkedIn ----------------

    [Theory]
    [InlineData("Applied", EstadoSolicitud.Enviada)]
    [InlineData("Sent", EstadoSolicitud.Enviada)]
    [InlineData("Don't know", EstadoSolicitud.Enviada)]
    [InlineData("In progress", EstadoSolicitud.EnRevision)]
    [InlineData("Interview", EstadoSolicitud.EntrevistaRrhh)]
    [InlineData("Offer", EstadoSolicitud.OfertaRecibida)]
    [InlineData("Hired", EstadoSolicitud.OfertaAceptada)]
    [InlineData("Rejected", EstadoSolicitud.Rechazada)]
    [InlineData("Not selected", EstadoSolicitud.Rechazada)]
    [InlineData("Withdrawn", EstadoSolicitud.Retirada)]
    [InlineData("Algo raro", EstadoSolicitud.Enviada)]
    public void MapearEstadoLinkedIn_ReconoceLosEstados(string texto, EstadoSolicitud esperado)
    {
        Assert.Equal(esperado, MainViewModel.MapearEstadoLinkedIn(texto));
    }

    // ---------------- Fechas ----------------

    [Theory]
    [InlineData("2024-05-01", 2024, 5, 1)]
    [InlineData("01/05/2024", 2024, 5, 1)]
    [InlineData("5/1/2024", 2024, 5, 1)]
    [InlineData("2024-05-01T10:30:00", 2024, 5, 1)]
    public void ParsearFecha_ReconoceFormatosComunes(string texto, int anio, int mes, int dia)
    {
        DateTime fecha = MainViewModel.ParsearFecha(texto);
        Assert.Equal(new DateTime(anio, mes, dia), fecha.Date);
    }

    [Fact]
    public void ParsearFecha_InvalidaCaeAlDiaActual()
    {
        Assert.Equal(DateTime.Today, MainViewModel.ParsearFecha("no-es-una-fecha"));
        Assert.Equal(DateTime.Today, MainViewModel.ParsearFecha(""));
    }

    // ---------------- CSV ----------------

    [Fact]
    public void DividirLinea_RespetaComillasYEscapes()
    {
        List<string> campos = MainViewModel.DividirLinea("\"Hola, mundo\";\"Dijo \"\"hola\"\"\"", ';');
        Assert.Equal(new[] { "Hola, mundo", "Dijo \"hola\"" }, campos);
    }

    [Fact]
    public void DelimitadorDe_EligePuntoYComaCuandoAbruma()
    {
        string linea = "a;b;c;d,e";
        Assert.Equal(';', MainViewModel.DelimitadorDe(linea));
    }

    [Fact]
    public void DelimitadorDe_EligeComaCuandoAbruma()
    {
        string linea = "a,b,c;d";
        Assert.Equal(',', MainViewModel.DelimitadorDe(linea));
    }

    [Fact]
    public void LeerCsv_DetectaDelimitadorYDevuelveFilas()
    {
        string ruta = Path.Combine(TestDb.NuevaCarpeta(), "datos.csv");
        File.WriteAllText(ruta, "Empresa;Puesto\nACME;Desarrollador\n");

        List<List<string>> lineas = MainViewModel.LeerCsv(ruta);

        Assert.Equal(2, lineas.Count);
        Assert.Equal(new[] { "Empresa", "Puesto" }, lineas[0]);
        Assert.Equal(new[] { "ACME", "Desarrollador" }, lineas[1]);
    }

    // ---------------- Columnas del CSV de LinkedIn ----------------

    [Fact]
    public void IdentificarColumnas_ReconoceCabeceraEnInglesYEspanol()
    {
        Dictionary<string, int> columnas = MainViewModel.IdentificarColumnas(
            new List<string> { "Company", "Job Title", "Application Date", "Status" });

        Assert.True(columnas.ContainsKey("empresa"));
        Assert.True(columnas.ContainsKey("puesto"));
        Assert.True(columnas.ContainsKey("fecha"));
        Assert.True(columnas.ContainsKey("estado"));
    }

    [Fact]
    public void IdentificarColumnas_NormalizaTildesDeLaCabecera()
    {
        Dictionary<string, int> columnas = MainViewModel.IdentificarColumnas(
            new List<string> { "Empresa", "Puesto", "Ubicación" });

        Assert.True(columnas.ContainsKey("ubicacion"));
    }

    // ---------------- Escapado para el CSV de exportación ----------------

    [Fact]
    public void Escapar_EncierraEntreComillas()
    {
        Assert.Equal("\"ACME\"", MainViewModel.Escapar("ACME"));
    }

    [Fact]
    public void Escapar_ComillasInternasSeDoblan()
    {
        Assert.Equal("\"Dijo \"\"hola\"\"\"", MainViewModel.Escapar("Dijo \"hola\""));
    }

    [Fact]
    public void Escapar_SaltosDeLineaSeSustituyenPorEspacios()
    {
        Assert.Equal("\"dos líneas\"", MainViewModel.Escapar("dos\nlíneas"));
    }

    [Fact]
    public void Escapar_NuloOVacioDevuelveCadenaVacia()
    {
        Assert.Equal(string.Empty, MainViewModel.Escapar(null));
        Assert.Equal(string.Empty, MainViewModel.Escapar(""));
    }

    [Fact]
    public void Nulo_VacioEsNull()
    {
        Assert.Null(MainViewModel.Nulo(""));
        Assert.Equal("texto", MainViewModel.Nulo("texto"));
    }

    [Fact]
    public void ObtenerCampo_DevuelveVacioSiIndiceFueraDeRango()
    {
        var columnas = new Dictionary<string, int> { { "empresa", 0 } };
        Assert.Equal(string.Empty, MainViewModel.ObtenerCampo(new List<string> { "a" }, columnas, "puesto"));
    }
}