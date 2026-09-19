using Xunit;
using System.IO;
using GestorSolicitudes.Helpers;

namespace GestorSolicitudes.Tests;

public class AdjuntosHelperTests
{
    [Fact]
    public void CopiarEn_CopiaElFicheroADentroDeLaCarpeta()
    {
        string carpeta = TestDb.NuevaCarpeta();
        string origen = Path.Combine(TestDb.NuevaCarpeta(), "cv-final.pdf");
        File.WriteAllText(origen, "contenido");

        string destino = AdjuntosHelper.CopiarEn(origen, "cv", carpeta);

        Assert.StartsWith(carpeta, destino);
        Assert.True(File.Exists(destino));
        Assert.Equal("contenido", File.ReadAllText(destino));
    }

    [Fact]
    public void EliminarDe_BorraFicheroDeNuestraCarpeta()
    {
        string carpeta = TestDb.NuevaCarpeta();
        string origen = Path.Combine(TestDb.NuevaCarpeta(), "carta.docx");
        File.WriteAllText(origen, "hola");
        string destino = AdjuntosHelper.CopiarEn(origen, "carta", carpeta);

        AdjuntosHelper.EliminarDe(destino, carpeta);

        Assert.False(File.Exists(destino));
    }

    [Fact]
    public void EliminarDe_IgnoraRutasFueraDeLaCarpeta()
    {
        string carpeta = TestDb.NuevaCarpeta();
        string fuera = Path.Combine(TestDb.NuevaCarpeta(), "otro.pdf");
        File.WriteAllText(fuera, "datos");

        AdjuntosHelper.EliminarDe(fuera, carpeta);

        Assert.True(File.Exists(fuera)); // jamás borra un fichero arbitrario
    }

    [Fact]
    public void EliminarDe_NuloOEnBlancoNoLanza()
    {
        string carpeta = TestDb.NuevaCarpeta();
        AdjuntosHelper.EliminarDe(null, carpeta);
        AdjuntosHelper.EliminarDe("   ", carpeta);
        AdjuntosHelper.EliminarDe("", carpeta);
    }
}