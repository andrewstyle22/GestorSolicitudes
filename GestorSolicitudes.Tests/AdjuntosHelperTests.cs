using System.IO;
using GestorSolicitudes.Helpers;

namespace GestorSolicitudes.Tests;

public class AdjuntosHelperTests
{
    [Fact]
    public void Copiar_CopiaElFicheroADentroDeLaCarpeta()
    {
        string carpeta = TestDb.NuevaCarpeta();
        string origen = Path.Combine(TestDb.NuevaCarpeta(), "cv-final.pdf");
        File.WriteAllText(origen, "contenido");

        string destino = AdjuntosHelper.Copiar(origen, "cv", carpeta);

        Assert.StartsWith(carpeta, destino);
        Assert.True(File.Exists(destino));
        Assert.Equal("contenido", File.ReadAllText(destino));
    }

    [Fact]
    public void Eliminar_BorraFicheroDeNuestraCarpeta()
    {
        string carpeta = TestDb.NuevaCarpeta();
        string origen = Path.Combine(TestDb.NuevaCarpeta(), "carta.docx");
        File.WriteAllText(origen, "hola");
        string destino = AdjuntosHelper.Copiar(origen, "carta", carpeta);

        AdjuntosHelper.Eliminar(destino, carpeta);

        Assert.False(File.Exists(destino));
    }

    [Fact]
    public void Eliminar_IgnoraRutasFueraDeLaCarpeta()
    {
        string carpeta = TestDb.NuevaCarpeta();
        string fuera = Path.Combine(TestDb.NuevaCarpeta(), "otro.pdf");
        File.WriteAllText(fuera, "datos");

        AdjuntosHelper.Eliminar(fuera, carpeta);

        Assert.True(File.Exists(fuera)); // jamás borra un fichero arbitrario
    }

    [Fact]
    public void Eliminar_NoBorraUnFicheroQueEscapaDeLaCarpetaConPuntosSuspensivos()
    {
        string carpeta = Path.Combine(TestDb.NuevaCarpeta(), "adjuntos");
        Directory.CreateDirectory(carpeta);
        string fuera = Path.Combine(Path.GetDirectoryName(carpeta)!, "otro.pdf");
        File.WriteAllText(fuera, "datos");

        // La ruta empieza por la carpeta, pero al canonicalizar sale de ella.
        string trampa = Path.Combine(carpeta, "..", "otro.pdf");

        AdjuntosHelper.Eliminar(trampa, carpeta);

        Assert.True(File.Exists(fuera));
    }

    [Fact]
    public void Eliminar_NuloOEnBlancoNoLanza()
    {
        string carpeta = TestDb.NuevaCarpeta();

        Exception? excepcion = Record.Exception(() =>
        {
            AdjuntosHelper.Eliminar(null, carpeta);
            AdjuntosHelper.Eliminar("   ", carpeta);
            AdjuntosHelper.Eliminar("", carpeta);
        });

        Assert.Null(excepcion);
    }
}