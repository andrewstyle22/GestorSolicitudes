namespace GestorSolicitudes.Helpers;

using System.Globalization;
using System.Text;

/// <summary>
/// Utilidades puras de texto: normalización para la búsqueda y escapado de campos al
/// exportar a CSV. Sin estado ni dependencias: las usan el ViewModel y la exportación.
/// </summary>
public static class TextoHelper
{
    /// <summary>
    /// Minúsculas y sin tildes, para que la búsqueda ignore mayúsculas y acentos:
    /// teclear "metrica" encuentra "Métrica". Se aplica igual al texto buscado y a
    /// los campos, así la comparación es estable.
    /// </summary>
    internal static string NormalizarBusqueda(string valor)
    {
        string descompuesto = valor.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);

        foreach (char c in descompuesto.Where(
            caracter => CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark))
        {
            sb.Append(c);
        }

        return sb.ToString();
    }

    internal static string Escapar(string? valor)
    {
        if (string.IsNullOrEmpty(valor))
        {
            return string.Empty;
        }

        string limpio = valor.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ");
        return $"\"{limpio}\"";
    }
}
