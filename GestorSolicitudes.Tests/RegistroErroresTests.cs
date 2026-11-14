namespace GestorSolicitudes.Tests;

using System.IO;
using GestorSolicitudes.Helpers;

/// <summary>errores.log: se escribe en una ruta temporal, nunca en %APPDATA% desde los tests.</summary>
public class RegistroErroresTests
{
    private static string RutaTemporal() => Path.Combine(TestDb.NuevaCarpeta(), "errores.log");

    [Fact]
    public void RegistrarEn_EscribeLaFechaElContextoYLaExcepcion()
    {
        string ruta = RutaTemporal();

        RegistroErrores.RegistrarEn(ruta, new InvalidOperationException("algo falló"), "al guardar");

        string contenido = File.ReadAllText(ruta);
        Assert.Contains("al guardar", contenido);
        Assert.Contains(nameof(InvalidOperationException), contenido);
        Assert.Contains("algo falló", contenido);
    }

    [Fact]
    public void RegistrarEn_AñadeAlFinalSinBorrarLoAnterior()
    {
        string ruta = RutaTemporal();

        RegistroErrores.RegistrarEn(ruta, new InvalidOperationException("primera"), "uno");
        RegistroErrores.RegistrarEn(ruta, new InvalidOperationException("segunda"), "dos");

        string contenido = File.ReadAllText(ruta);
        Assert.Contains("primera", contenido);
        Assert.Contains("segunda", contenido);
    }

    [Fact]
    public void RegistrarEn_CreaLaCarpetaSiNoExiste()
    {
        string ruta = Path.Combine(TestDb.NuevaCarpeta(), "anidada", "errores.log");

        RegistroErrores.RegistrarEn(ruta, new InvalidOperationException(), "contexto");

        Assert.True(File.Exists(ruta));
    }

    [Fact]
    public void RegistrarEn_PasadoElLimiteConservaLoSoloReciente()
    {
        string ruta = RutaTemporal();
        RegistroErrores.RegistrarEn(ruta, new InvalidOperationException("vieja"), "ANTIGUA" + new string('a', 150_000));
        for (int i = 0; i < 8; i++)
        {
            RegistroErrores.RegistrarEn(ruta, new InvalidOperationException("relleno"), new string('b', 150_000));
        }

        RegistroErrores.RegistrarEn(ruta, new InvalidOperationException("nueva"), "RECIENTE");

        string contenido = File.ReadAllText(ruta);
        Assert.True(new FileInfo(ruta).Length <= RegistroErrores.LimiteBytes);
        Assert.DoesNotContain("ANTIGUA", contenido);
        Assert.Contains("RECIENTE", contenido);
    }

    [Fact]
    public void RegistrarEn_RutaQueNoSePuedeEscribirNoLanza()
    {
        string carpeta = TestDb.NuevaCarpeta();

        Exception? error = Record.Exception(() =>
            RegistroErrores.RegistrarEn(carpeta, new InvalidOperationException(), "contexto"));

        Assert.Null(error);
    }
}
