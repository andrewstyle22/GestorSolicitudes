using System.Globalization;
using System.IO;
using GestorSolicitudes.Helpers;

namespace GestorSolicitudes.Tests;

/// <summary>idioma.txt: la preferencia se guarda en un fichero aparte (nunca en %APPDATA% desde los tests).</summary>
public class PreferenciaIdiomaTests
{
    private static string RutaTemporal() => Path.Combine(TestDb.NuevaCarpeta(), "idioma.txt");

    [Fact]
    public void GuardarEn_YLeerDe_DevuelvenElMismoIdioma()
    {
        string ruta = RutaTemporal();

        PreferenciaIdioma.GuardarEn(ruta, Idioma.Aleman);

        Assert.Equal(Idioma.Aleman, PreferenciaIdioma.LeerDe(ruta));
    }

    [Fact]
    public void LeerDe_SinFicheroDevuelveNullParaUsarElDelSistema()
    {
        Assert.Null(PreferenciaIdioma.LeerDe(RutaTemporal()));
    }

    [Theory]
    [InlineData("Italiano")]
    [InlineData("99")]
    [InlineData("")]
    public void LeerDe_ConContenidoQueNoEsUnIdiomaDevuelveNull(string contenido)
    {
        string ruta = RutaTemporal();
        File.WriteAllText(ruta, contenido);

        Assert.Null(PreferenciaIdioma.LeerDe(ruta));
    }

    [Fact]
    public void GuardarEn_CreaLaCarpetaSiNoExiste()
    {
        string ruta = Path.Combine(TestDb.NuevaCarpeta(), "anidada", "idioma.txt");

        PreferenciaIdioma.GuardarEn(ruta, Idioma.Ingles);

        Assert.True(File.Exists(ruta));
    }

    [Theory]
    [InlineData("en-US", Idioma.Ingles)]
    [InlineData("de-AT", Idioma.Aleman)]
    [InlineData("es-MX", Idioma.Castellano)]
    [InlineData("fr-FR", Idioma.Castellano)]
    public void DelSistema_MapeaLasCulturasConocidasYElRestoACastellano(string cultura, Idioma esperado) =>
        Assert.Equal(esperado, PreferenciaIdioma.DelSistema(CultureInfo.GetCultureInfo(cultura)));
}
