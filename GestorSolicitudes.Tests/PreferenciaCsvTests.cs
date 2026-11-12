namespace GestorSolicitudes.Tests;

using System.IO;
using GestorSolicitudes.Helpers;

/// <summary>csv.txt: la preferencia se guarda en un fichero aparte (nunca en %APPDATA% desde los tests).</summary>
public class PreferenciaCsvTests
{
    private static string RutaTemporal() => Path.Combine(TestDb.NuevaCarpeta(), "csv.txt");

    [Theory]
    [InlineData(';')]
    [InlineData(',')]
    [InlineData('\t')]
    public void GuardarEn_YLeerDe_DevuelvenElMismoSeparador(char separador)
    {
        string ruta = RutaTemporal();

        PreferenciaCsv.GuardarEn(ruta, separador);

        Assert.Equal(separador, PreferenciaCsv.LeerDe(ruta));
    }

    [Fact]
    public void LeerDe_SinFicheroDevuelveNullParaUsarElPorDefecto()
    {
        Assert.Null(PreferenciaCsv.LeerDe(RutaTemporal()));
    }

    [Theory]
    [InlineData(';')]
    [InlineData(',')]
    [InlineData('\t')]
    public void EsValido_AceptaLosTresSeparadores(char separador) =>
        Assert.True(PreferenciaCsv.EsValido(separador));

    [Theory]
    [InlineData('|')]
    [InlineData(' ')]
    [InlineData('a')]
    public void EsValido_RechazaCualquierOtroCaracter(char separador) =>
        Assert.False(PreferenciaCsv.EsValido(separador));

    [Theory]
    [InlineData("|")]
    [InlineData(";;")]
    [InlineData("")]
    public void LeerDe_ConContenidoQueNoEsUnSeparadorDevuelveNull(string contenido)
    {
        string ruta = RutaTemporal();
        File.WriteAllText(ruta, contenido);

        Assert.Null(PreferenciaCsv.LeerDe(ruta));
    }

    [Fact]
    public void GuardarEn_CreaLaCarpetaSiNoExiste()
    {
        string ruta = Path.Combine(TestDb.NuevaCarpeta(), "anidada", "csv.txt");

        PreferenciaCsv.GuardarEn(ruta, ',');

        Assert.True(File.Exists(ruta));
    }
}
