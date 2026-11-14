namespace GestorSolicitudes.Tests;

using System.IO;

/// <summary>Respuesta a una excepción no controlada: registro en disco y aviso con la ruta.</summary>
public class ManejadorErroresTests
{
    [Fact]
    public void Avisar_RegistraElErrorYMuestraLaRutaDelFichero()
    {
        string ruta = Path.Combine(TestDb.NuevaCarpeta(), "errores.log");
        var dialogo = new DialogoFalso();

        ManejadorErrores.Avisar(new InvalidOperationException("algo falló"), "al guardar", ruta, dialogo);

        Assert.Contains("al guardar", File.ReadAllText(ruta));
        string mensaje = Assert.Single(dialogo.Errores);
        Assert.Contains("algo falló", mensaje);
        Assert.Contains(ruta, mensaje);
    }

    [Fact]
    public void Avisar_ConRutaNoEscribibleSigueMostrandoElAviso()
    {
        string ruta = TestDb.NuevaCarpeta();
        var dialogo = new DialogoFalso();

        Exception? error = Record.Exception(() =>
            ManejadorErrores.Avisar(new InvalidOperationException("algo falló"), "al guardar", ruta, dialogo));

        Assert.Null(error);
        Assert.Single(dialogo.Errores);
    }
}
