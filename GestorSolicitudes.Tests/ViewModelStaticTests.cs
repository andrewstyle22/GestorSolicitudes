using Xunit;
using System.IO;
using GestorSolicitudes;
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
        Assert.Equal(esperado, CsvHelper.MapearEstadoLinkedIn(texto));
    }

    // ---------------- Fechas ----------------

    [Theory]
    [InlineData("2024-05-01", 2024, 5, 1)]
    [InlineData("01/05/2024", 2024, 5, 1)]
    [InlineData("5/1/2024", 2024, 5, 1)]
    [InlineData("2024-05-01T10:30:00", 2024, 5, 1)]
    public void ParsearFecha_ReconoceFormatosComunes(string texto, int anio, int mes, int dia)
    {
        DateTime fecha = CsvHelper.ParsearFecha(texto);
        Assert.Equal(new DateTime(anio, mes, dia), fecha.Date);
    }

    [Fact]
    public void ParsearFecha_InvalidaCaeAlDiaActual()
    {
        Assert.Equal(DateTime.Today, CsvHelper.ParsearFecha("no-es-una-fecha"));
        Assert.Equal(DateTime.Today, CsvHelper.ParsearFecha(""));
    }

    // ---------------- CSV ----------------

    [Fact]
    public void DividirLinea_RespetaComillasYEscapes()
    {
        List<string> campos = CsvHelper.DividirLinea("\"Hola, mundo\";\"Dijo \"\"hola\"\"\"", ';');
        Assert.Equal(new[] { "Hola, mundo", "Dijo \"hola\"" }, campos);
    }

    [Fact]
    public void DelimitadorDe_EligePuntoYComaCuandoAbruma()
    {
        string linea = "a;b;c;d,e";
        Assert.Equal(';', CsvHelper.DelimitadorDe(linea));
    }

    [Fact]
    public void DelimitadorDe_EligeComaCuandoAbruma()
    {
        string linea = "a,b,c;d";
        Assert.Equal(',', CsvHelper.DelimitadorDe(linea));
    }

    [Fact]
    public void LeerCsv_DetectaDelimitadorYDevuelveFilas()
    {
        string ruta = Path.Combine(TestDb.NuevaCarpeta(), "datos.csv");
        File.WriteAllText(ruta, "Empresa;Puesto\nACME;Desarrollador\n");

        List<List<string>> lineas = CsvHelper.LeerCsv(ruta);

        Assert.Equal(2, lineas.Count);
        Assert.Equal(new[] { "Empresa", "Puesto" }, lineas[0]);
        Assert.Equal(new[] { "ACME", "Desarrollador" }, lineas[1]);
    }

    // ---------------- Columnas del CSV de LinkedIn ----------------

    [Fact]
    public void IdentificarColumnas_ReconoceCabeceraEnInglesYEspanol()
    {
        Dictionary<string, int> columnas = CsvHelper.IdentificarColumnas(
            new List<string> { "Company", "Job Title", "Application Date", "Status" });

        Assert.True(columnas.ContainsKey("empresa"));
        Assert.True(columnas.ContainsKey("puesto"));
        Assert.True(columnas.ContainsKey("fecha"));
        Assert.True(columnas.ContainsKey("estado"));
    }

    [Fact]
    public void IdentificarColumnas_NormalizaTildesDeLaCabecera()
    {
        Dictionary<string, int> columnas = CsvHelper.IdentificarColumnas(
            new List<string> { "Empresa", "Puesto", "Ubicación" });

        Assert.True(columnas.ContainsKey("ubicacion"));
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

    [Fact]
    public void ObtenerCampo_DevuelveVacioSiIndiceFueraDeRango()
    {
        var columnas = new Dictionary<string, int> { { "empresa", 0 } };
        Assert.Equal(string.Empty, CsvHelper.ObtenerCampo(new List<string> { "a" }, columnas, "puesto"));
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

    // ---------------- Resumen de importación ----------------

    [Fact]
    public void ResumenImportacion_CombinaLosContadoresDelResumen()
    {
        string soloImportadas = MainViewModel.ResumenImportacion(3, 0, 0);
        Assert.Equal(string.Format(Localizacion.Texto("Importar.ResumenImportadas"), 3), soloImportadas);
        Assert.DoesNotContain("\n", soloImportadas);

        string completo = MainViewModel.ResumenImportacion(3, 2, 1);
        Assert.Contains(string.Format(Localizacion.Texto("Importar.ResumenDuplicadas"), 2), completo);
        Assert.Contains(string.Format(Localizacion.Texto("Importar.ResumenOmitidas"), 1), completo);
    }
}