namespace GestorSolicitudes.Helpers;

using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Extrae el texto de documentos ofimáticos basados en ZIP (DOCX de Word, ODT de
/// LibreOffice Writer) para previsualizarlos: WPF no trae visor nativo de estos formatos
/// y así se muestran en la ventana de solo lectura sin abrirlos en su programa.
/// </summary>
public static class DocumentoHelper
{
    // Límite de tiempo para las expresiones regulares: evita un cuelgue si un documento
    // malicioso o corrupto dispara un backtracking desbocado.
    private static readonly TimeSpan TiempoLimiteRegex = TimeSpan.FromSeconds(1);

    // Tope del XML descomprimido que se lee de un DOCX u ODT: un ZIP pequeño puede expandirse
    // a gigabytes y agotar la memoria antes de que el usuario vea nada.
    internal const int LimiteTexto = 10 * 1024 * 1024;

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
        var acumulado = new StringBuilder();
        char[] bloque = new char[8192];
        int leidos;
        while ((leidos = lector.Read(bloque, 0, bloque.Length)) > 0)
        {
            acumulado.Append(bloque, 0, leidos);
            if (acumulado.Length > LimiteTexto)
            {
                throw new DocumentoDemasiadoGrandeException();
            }
        }

        string contenido = acumulado.ToString();
        contenido = Regex.Replace(contenido, finParrafo, "\n", RegexOptions.None, TiempoLimiteRegex);
        contenido = Regex.Replace(contenido, "<[^>]+>", string.Empty, RegexOptions.None, TiempoLimiteRegex);
        return System.Net.WebUtility.HtmlDecode(contenido);
    }
}
