namespace GestorSolicitudes.Tests;

using GestorSolicitudes.Helpers;

public class TextoYCsvTests
{
    // ---------------- Normalización de búsqueda ----------------
    [Fact]
    public void NormalizarBusqueda_QuitaTildesYMayusculas()
    {
        Assert.Equal("metrica", TextoHelper.NormalizarBusqueda("Métrica"));
        Assert.Equal("aeiou", TextoHelper.NormalizarBusqueda("ÁÉÍÓÚ"));
        Assert.Equal("nino", TextoHelper.NormalizarBusqueda("Ñino"));
    }

    // ---------------- Escapado para el CSV de exportación ----------------
    [Fact]
    public void Escapar_EncierraEntreComillas()
    {
        Assert.Equal("\"ACME\"", TextoHelper.Escapar("ACME"));
    }

    [Fact]
    public void Escapar_ComillasInternasSeDoblan()
    {
        Assert.Equal("\"Dijo \"\"hola\"\"\"", TextoHelper.Escapar("Dijo \"hola\""));
    }

    [Fact]
    public void Escapar_SaltosDeLineaSeSustituyenPorEspacios()
    {
        Assert.Equal("\"dos líneas\"", TextoHelper.Escapar("dos\nlíneas"));
    }

    [Fact]
    public void Escapar_NuloOVacioDevuelveCadenaVacia()
    {
        Assert.Equal(string.Empty, TextoHelper.Escapar(null));
        Assert.Equal(string.Empty, TextoHelper.Escapar(string.Empty));
    }
}
