namespace GestorSolicitudes.Tests;

using System.IO;
using GestorSolicitudes.Helpers;

public class LectorAdjuntosTests
{
    [Fact]
    public void Leer_DevuelveElContenidoDelFichero()
    {
        string ruta = Path.Combine(TestDb.NuevaCarpeta(), "cv.pdf");
        File.WriteAllBytes(ruta, [1, 2, 3, 4]);

        byte[] contenido = LectorAdjuntos.Leer(ruta);

        Assert.Equal([1, 2, 3, 4], contenido);
    }

    [Fact]
    public void Leer_ConElTamanoMaximo_LeeTodo()
    {
        string ruta = CrearFicheroDeBytes(AdjuntosHelper.TamanoMaximoBytes);

        byte[] contenido = LectorAdjuntos.Leer(ruta);

        Assert.Equal(AdjuntosHelper.TamanoMaximoBytes, contenido.Length);
    }

    [Fact]
    public void Leer_ConUnByteDeMas_Lanza()
    {
        string ruta = CrearFicheroDeBytes(AdjuntosHelper.TamanoMaximoBytes + 1);

        Assert.Throws<DocumentoDemasiadoGrandeException>(() => LectorAdjuntos.Leer(ruta));
    }

    private static string CrearFicheroDeBytes(int bytes)
    {
        string ruta = Path.Combine(TestDb.NuevaCarpeta(), "documento.pdf");
        using FileStream fichero = File.Create(ruta);
        fichero.SetLength(bytes);
        return ruta;
    }
}
