namespace GestorSolicitudes.Tests;

using GestorSolicitudes.ViewModels;

/// <summary>
/// El servicio de diálogos por defecto: no muestra nada y devuelve la respuesta neutra.
/// Es el que usa el ViewModel cuando nadie le inyecta un falso.
/// </summary>
public class DialogosNulosTests
{
    [Fact]
    public void NoMuestraNadaYDevuelveLaRespuestaNeutra()
    {
        var dialogo = new DialogosNulos();

        Assert.False(dialogo.Confirmar("mensaje", "titulo"));
        dialogo.Avisar("mensaje", "titulo");
        dialogo.Informar("mensaje", "titulo");
        dialogo.Error("mensaje", "titulo");
        Assert.Null(dialogo.ElegirFichero("titulo", "filtro"));
        Assert.Null(dialogo.ElegirDestino("titulo", "filtro", "propuesto.csv"));
        dialogo.AbrirEnlace("https://example.com");
        dialogo.VerAdjunto(@"C:\adjuntos\cv.pdf");
    }
}
