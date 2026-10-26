using System.IO;
using System.IO.Compression;
using GestorSolicitudes.Helpers;

namespace GestorSolicitudes.Tests;

public class DocumentoHelperTests
{
    [Fact]
    public void ExtraerTextoDocx_DevuelveElTextoDeLosParrafos()
    {
        string ruta = EscribirZip(
            "documento.docx",
            "word/document.xml",
            "<w:document><w:body><w:p><w:r><w:t>Hola mundo</w:t></w:r></w:p>"
            + "<w:p><w:r><w:t>Segunda linea &amp; Cia</w:t></w:r></w:p></w:body></w:document>");

        string texto = DocumentoHelper.ExtraerTextoDocx(ruta);

        Assert.Contains("Hola mundo", texto);
        Assert.Contains("Segunda linea & Cia", texto);
        Assert.DoesNotContain("<", texto);
    }

    [Fact]
    public void ExtraerTextoOdt_DevuelveElTextoDeLosParrafos()
    {
        string ruta = EscribirZip(
            "documento.odt",
            "content.xml",
            "<office:document-content><office:body><office:text>"
            + "<text:p>Hola desde LibreOffice</text:p><text:p>Segundo parrafo</text:p>"
            + "</office:text></office:body></office:document-content>");

        string texto = DocumentoHelper.ExtraerTextoOdt(ruta);

        Assert.Contains("Hola desde LibreOffice", texto);
        Assert.Contains("Segundo parrafo", texto);
        Assert.DoesNotContain("<", texto);
    }

    [Fact]
    public void ExtraerTextoDocx_SinElXmlEsperadoDevuelveVacio()
    {
        string ruta = EscribirZip("raro.docx", "otra.xml", "<a/>");

        Assert.Equal(string.Empty, DocumentoHelper.ExtraerTextoDocx(ruta));
    }

    private static string EscribirZip(string nombre, string entrada, string contenido)
    {
        string ruta = Path.Combine(TestDb.NuevaCarpeta(), nombre);
        using FileStream archivo = File.Create(ruta);
        using var zip = new ZipArchive(archivo, ZipArchiveMode.Create);
        using StreamWriter escritor = new(zip.CreateEntry(entrada).Open());
        escritor.Write(contenido);
        return ruta;
    }
}
