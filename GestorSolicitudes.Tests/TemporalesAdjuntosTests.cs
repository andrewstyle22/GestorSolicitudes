namespace GestorSolicitudes.Tests;

using System.IO;
using GestorSolicitudes.Helpers;

public class TemporalesAdjuntosTests
{
    private const string NombreCv = "cv.pdf";

    [Fact]
    public void Escribir_GuardaElContenidoConSuNombreEnUnaCarpetaPropia()
    {
        string carpeta = TestDb.NuevaCarpeta();

        string ruta = TemporalesAdjuntos.Escribir("cv-final.pdf", [1, 2, 3], carpeta);

        Assert.StartsWith(carpeta, ruta);
        Assert.Equal("cv-final.pdf", Path.GetFileName(ruta));
        Assert.Equal([1, 2, 3], File.ReadAllBytes(ruta));
    }

    [Fact]
    public void Escribir_DosVeces_NoComparteCarpeta()
    {
        string carpeta = TestDb.NuevaCarpeta();

        string primera = TemporalesAdjuntos.Escribir(NombreCv, [1], carpeta);
        string segunda = TemporalesAdjuntos.Escribir(NombreCv, [2], carpeta);

        Assert.NotEqual(Path.GetDirectoryName(primera), Path.GetDirectoryName(segunda));
        Assert.True(File.Exists(primera));
        Assert.True(File.Exists(segunda));
    }

    [Fact]
    public void Escribir_DescartaLaRutaDelNombre()
    {
        string carpeta = TestDb.NuevaCarpeta();

        string ruta = TemporalesAdjuntos.Escribir(@"..\..\fuera.pdf", [1], carpeta);

        Assert.Equal("fuera.pdf", Path.GetFileName(ruta));
        Assert.StartsWith(carpeta, ruta);
    }

    [Fact]
    public void Escribir_SustituyeCaracteresInvalidos()
    {
        string ruta = TemporalesAdjuntos.Escribir("cv:malo.pdf", [1], TestDb.NuevaCarpeta());

        Assert.Equal("cv_malo.pdf", Path.GetFileName(ruta));
    }

    [Theory]
    [InlineData("")]
    [InlineData("..")]
    public void Escribir_SinNombreUsable_UsaDocumento(string nombre)
    {
        string ruta = TemporalesAdjuntos.Escribir(nombre, [1], TestDb.NuevaCarpeta());

        Assert.Equal("documento", Path.GetFileName(ruta));
    }

    [Fact]
    public void Limpiar_BorraLaCarpetaConSuContenido()
    {
        string carpeta = TestDb.NuevaCarpeta();
        TemporalesAdjuntos.Escribir(NombreCv, [1], carpeta);

        TemporalesAdjuntos.Limpiar(carpeta);

        Assert.False(Directory.Exists(carpeta));
    }

    [Fact]
    public void Limpiar_SinCarpeta_NoLanza()
    {
        TemporalesAdjuntos.Limpiar(Path.Combine(TestDb.NuevaCarpeta(), "no-existe"));
    }
}
