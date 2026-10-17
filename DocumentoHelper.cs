namespace GestorSolicitudes.Helpers;

using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;

/// <summary>
/// Extrae el texto de documentos ofimáticos basados en ZIP (DOCX de Word, ODT de
/// LibreOffice Writer) para previsualizarlos: WPF no trae visor nativo de estos formatos
/// y así se muestran en la ventana de solo lectura sin abrirlos en su programa.
/// </summary>
public static class DocumentoHelper
{
    /// <summary>Extrae el texto plano de un DOCX (Word).</summary>
    public static string ExtraerTextoDocx(string ruta) =>
        ExtraerTextoZip(ruta, "word/document.xml", "</w:p>");

    /// <summary>Extrae el texto plano de un ODT (LibreOffice Writer).</summary>
    public static string ExtraerTextoOdt(string ruta) =>
        ExtraerTextoZip(ruta, "content.xml", "</text:p>");

    private static string ExtraerTextoZip(string ruta, string entrada, string finParrafo)
    {
        using ZipArchive zip = ZipFile.OpenRead(ruta);
        ZipArchiveEntry? xml = zip.GetEntry(entrada);
        if (xml is null)
        {
            return string.Empty;
        }

        using var lector = new StreamReader(xml.Open());
        string contenido = lector.ReadToEnd();
        contenido = Regex.Replace(contenido, finParrafo, "\n");
        contenido = Regex.Replace(contenido, "<[^>]+>", string.Empty);
        return System.Net.WebUtility.HtmlDecode(contenido);
    }
}
