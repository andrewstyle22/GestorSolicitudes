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

    [Theory]
    [InlineData("=1+1", "\"'=1+1\"")]
    [InlineData("+34 600", "\"'+34 600\"")]
    [InlineData("-100", "\"'-100\"")]
    [InlineData("@usuario", "\"'@usuario\"")]
    [InlineData("\ttexto", "\"'\ttexto\"")]
    [InlineData("\rtexto", "\"' texto\"")]
    public void Escapar_NeutralizaLasCeldasQueEmpiezanPorFormula(string valor, string esperado)
    {
        Assert.Equal(esperado, TextoHelper.Escapar(valor));
    }

    [Fact]
    public void Escapar_NeutralizaLasComillasQueEmpiezanPorFormula()
    {
        Assert.Equal("\"'=\"\"hola\"\"\"", TextoHelper.Escapar("=\"hola\""));
    }

    [Fact]
    public void Escapar_NuloOVacioDevuelveCadenaVacia()
    {
        Assert.Equal(string.Empty, TextoHelper.Escapar(null));
        Assert.Equal(string.Empty, TextoHelper.Escapar(string.Empty));
    }
}
