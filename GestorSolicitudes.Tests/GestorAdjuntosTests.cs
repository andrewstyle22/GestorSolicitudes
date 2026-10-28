using System.IO;
using GestorSolicitudes.Helpers;
using GestorSolicitudes.Models;

namespace GestorSolicitudes.Tests;

public class GestorAdjuntosTests
{
    [Fact]
    public void Copiar_CopiaElFicheroDentroDeLaCarpeta()
    {
        string carpeta = TestDb.NuevaCarpeta();
        var gestor = new GestorAdjuntos(carpeta);

        string copia = gestor.Copiar(Origen("cv.pdf"), "cv");

        Assert.StartsWith(carpeta, copia);
        Assert.True(File.Exists(copia));
    }

    [Fact]
    public void DescartarSustituidas_BorraLaAnteriorYConservaLaNueva()
    {
        string carpeta = TestDb.NuevaCarpeta();
        var gestor = new GestorAdjuntos(carpeta);
        string anterior = gestor.Copiar(Origen("cv.pdf"), "cv");
        string nueva = gestor.Copiar(Origen("cv-nuevo.pdf"), "cv");
        var solicitud = new Solicitud { RutaCv = nueva };

        gestor.DescartarSustituidas(solicitud, (anterior, null));

        Assert.False(File.Exists(anterior));
        Assert.True(File.Exists(nueva));
    }

    [Fact]
    public void DescartarSustituidas_BorraLasCopiasQueNoSeConservan()
    {
        string carpeta = TestDb.NuevaCarpeta();
        var gestor = new GestorAdjuntos(carpeta);
        string conservada = gestor.Copiar(Origen("cv.pdf"), "cv");
        string sobrante = gestor.Copiar(Origen("carta.pdf"), "carta");
        var solicitud = new Solicitud { RutaCv = conservada };

        gestor.DescartarSustituidas(solicitud, (null, null));

        Assert.True(File.Exists(conservada));
        Assert.False(File.Exists(sobrante));
    }

    [Fact]
    public void DescartarNoGuardadas_BorraLaElegidaYConservaLaGuardada()
    {
        string carpeta = TestDb.NuevaCarpeta();
        var gestor = new GestorAdjuntos(carpeta);
        // La guardada es de una sesión anterior: no la recuerda este gestor.
        string guardada = AdjuntosHelper.Copiar(Origen("cv.pdf"), "cv", carpeta);
        string elegida = gestor.Copiar(Origen("cv-nuevo.pdf"), "cv");
        var solicitud = new Solicitud { RutaCv = elegida };

        gestor.DescartarNoGuardadas(solicitud, (guardada, null));

        Assert.False(File.Exists(elegida));
        Assert.True(File.Exists(guardada));
    }

    [Fact]
    public void DescartarCopias_ConservaLaIndicadaYBorraElResto()
    {
        string carpeta = TestDb.NuevaCarpeta();
        var gestor = new GestorAdjuntos(carpeta);
        string conservada = gestor.Copiar(Origen("cv.pdf"), "cv");
        string sobrante = gestor.Copiar(Origen("carta.pdf"), "carta");

        gestor.DescartarCopias(conservarCv: conservada);

        Assert.True(File.Exists(conservada));
        Assert.False(File.Exists(sobrante));
    }

    [Fact]
    public void DescartarCopias_SinArgumentosLasBorraTodas()
    {
        string carpeta = TestDb.NuevaCarpeta();
        var gestor = new GestorAdjuntos(carpeta);
        string primera = gestor.Copiar(Origen("cv.pdf"), "cv");
        string segunda = gestor.Copiar(Origen("carta.pdf"), "carta");

        gestor.DescartarCopias();

        Assert.False(File.Exists(primera));
        Assert.False(File.Exists(segunda));
    }

    private static string Origen(string nombre)
    {
        string ruta = Path.Combine(TestDb.NuevaCarpeta(), nombre);
        File.WriteAllText(ruta, "contenido");
        return ruta;
    }
}
