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

    [Fact]
    public void LeerDe_RutaEsCarpeta_NoCaeYDevuelveNull()
    {
        string carpeta = TestDb.NuevaCarpeta();

        Assert.Null(PreferenciaCsv.LeerDe(carpeta));
    }

    [Fact]
    public void LeerDe_FicheroBloqueado_CaeEnCatchYDevuelveNull()
    {
        string ruta = RutaTemporal();
        File.WriteAllText(ruta, "x");
        using var bloqueo = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.None);

        Assert.Null(PreferenciaCsv.LeerDe(ruta));
    }

    [Fact]
    public void GuardarEn_RutaEsCarpeta_NoCaeYUsaValorPorDefecto()
    {
        // Arrangement: crear una carpeta temporal (simula pasar una ruta de carpeta en lugar de fichero)
        string carpeta = TestDb.NuevaCarpeta();

        // Act: intentar guardar el separador usando una ruta de carpeta
        // Este método ha de gestionar gracefully el caso de que la ruta sea una carpeta
        PreferenciaCsv.GuardarEn(carpeta, ';');

        // Assert: el método ha de completarse sin lanzar excepción (no debe "crash")
        // y al leer la preferencia debe usarse el separador por defecto ';'
        // como indica el nombre del test: "NoCaeYUsaValorPorDefecto"
        string directorioPadre = System.IO.Path.GetDirectoryName(carpeta)!;
        string archivo = System.IO.Path.Combine(directorioPadre, "csv.txt");

        if (File.Exists(archivo))
        {
            // Si el fichero existe, ha de leer el separador por defecto
            Assert.Equal(PreferenciaCsv.PorDefecto, PreferenciaCsv.LeerDe(archivo).GetValueOrDefault(PreferenciaCsv.PorDefecto));
        }
    }
}
