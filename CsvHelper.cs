namespace GestorSolicitudes.Helpers;

using System.Globalization;
using System.IO;
using System.Text;
using GestorSolicitudes.Models;

/// <summary>
/// Utilidades puras de CSV: lectura/escritura de campos, detección de delimitador,
/// normalización de cabeceras y mapeo de los estados del CSV de LinkedIn. Sin estado
/// ni dependencias: lo usan la importación y la exportación del MainViewModel.
/// </summary>
public static class CsvHelper
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

        foreach (char c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    internal static EstadoSolicitud MapearEstadoLinkedIn(string estado)
    {
        string s = estado.Trim().ToLowerInvariant();

        if (s.Contains("applied") || s.Contains("sent") || s.Contains("don't know"))
        {
            return EstadoSolicitud.Enviada;
        }

        if (s.Contains("progress"))
        {
            return EstadoSolicitud.EnRevision;
        }

        if (s.Contains("interview"))
        {
            return EstadoSolicitud.EntrevistaRrhh;
        }

        if (s.Contains("offer"))
        {
            return EstadoSolicitud.OfertaRecibida;
        }

        if (s.Contains("hired") || s.Contains("accepted"))
        {
            return EstadoSolicitud.OfertaAceptada;
        }

        if (s.Contains("reject") || s.Contains("not selected") || s.Contains("not moving"))
        {
            return EstadoSolicitud.Rechazada;
        }

        if (s.Contains("withdrawn") || s.Contains("withdrew") || s.Contains("archived"))
        {
            return EstadoSolicitud.Retirada;
        }

        return EstadoSolicitud.Enviada;
    }

    internal static DateTime ParsearFecha(string valor)
    {
        var formatos = new[]
        {
            "yyyy-MM-dd", "dd/MM/yyyy", "M/d/yyyy", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss",
        };

        if (DateTime.TryParseExact(valor, formatos, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateTime exacta))
        {
            return exacta;
        }

        if (DateTime.TryParse(valor, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime local))
        {
            return local;
        }

        if (DateTime.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime invariante))
        {
            return invariante;
        }

        return DateTime.Today;
    }

    /// <summary>Localiza cada columna útil del CSV de LinkedIn por el nombre de la cabecera.</summary>
    internal static Dictionary<string, int> IdentificarColumnas(List<string> cabecera)
    {
        var resultado = new Dictionary<string, int>();

        for (int i = 0; i < cabecera.Count; i++)
        {
            string col = NormalizarCabecera(cabecera[i]);
            if (col.Length == 0)
            {
                continue;
            }

            if (!resultado.ContainsKey("empresa") && col.Contains("company"))
            {
                resultado["empresa"] = i;
            }

            if (!resultado.ContainsKey("puesto") && (col.Contains("title") || col == "puesto"))
            {
                resultado["puesto"] = i;
            }

            if (!resultado.ContainsKey("fecha") && ((col.Contains("application") && col.Contains("date")) || col == "fecha"))
            {
                resultado["fecha"] = i;
            }

            if (!resultado.ContainsKey("estado") && (col.Contains("status") || col.Contains("estado")))
            {
                resultado["estado"] = i;
            }

            if (!resultado.ContainsKey("ubicacion") && (col.Contains("location") || col.Contains("ubicacion")))
            {
                resultado["ubicacion"] = i;
            }

            if (!resultado.ContainsKey("evento") && (col == "event" || col.Contains("evento")))
            {
                resultado["evento"] = i;
            }

            if (!resultado.ContainsKey("uuid") && col.Contains("uuid"))
            {
                resultado["uuid"] = i;
            }
        }

        return resultado;
    }

    /// <summary>
    /// Deja la cabecera en minúsculas y solo con letras/dígitos ASCII, quitando también
    /// las tildes: así "Ubicación" y "Ubicacion" identifican la misma columna.
    /// </summary>
    internal static string NormalizarCabecera(string valor)
    {
        string descompuesto = NormalizarBusqueda(valor);
        var sb = new StringBuilder(descompuesto.Length);

        foreach (char c in descompuesto)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    internal static string ObtenerCampo(List<string> campos, Dictionary<string, int> columnas, string nombre) =>
        columnas.TryGetValue(nombre, out int indice) && indice >= 0 && indice < campos.Count
            ? campos[indice]
            : string.Empty;

    /// <summary>Lee un CSV respetando comillas y detecta si el separador es ';' o ','.</summary>
    internal static List<List<string>> LeerCsv(string ruta)
    {
        var lineas = new List<List<string>>();
        char delimitador = ',';

        using var lector = new StreamReader(ruta, Encoding.UTF8, true);

        string? linea;
        bool primera = true;
        while ((linea = lector.ReadLine()) is not null)
        {
            if (primera)
            {
                delimitador = DelimitadorDe(linea);
                primera = false;
            }

            lineas.Add(DividirLinea(linea, delimitador));
        }

        return lineas;
    }

    internal static char DelimitadorDe(string linea)
    {
        int puntoYComa = 0, coma = 0;
        bool dentroDeComillas = false;

        foreach (char c in linea)
        {
            if (c == '"')
            {
                dentroDeComillas = !dentroDeComillas;
            }
            else if (!dentroDeComillas && c == ';')
            {
                puntoYComa++;
            }
            else if (!dentroDeComillas && c == ',')
            {
                coma++;
            }
        }

        return puntoYComa > coma ? ';' : ',';
    }

    internal static List<string> DividirLinea(string linea, char delimitador)
    {
        var campos = new List<string>();
        var actual = new StringBuilder();
        bool dentroDeComillas = false;

        for (int i = 0; i < linea.Length; i++)
        {
            char c = linea[i];

            if (c == '"')
            {
                if (dentroDeComillas && i + 1 < linea.Length && linea[i + 1] == '"')
                {
                    // Comillas dobles escapadas dentro de un campo ("" -> ")
                    actual.Append('"');
                    i++;
                }
                else
                {
                    dentroDeComillas = !dentroDeComillas;
                }
            }
            else if (c == delimitador && !dentroDeComillas)
            {
                campos.Add(actual.ToString().Trim());
                actual.Clear();
            }
            else
            {
                actual.Append(c);
            }
        }

        campos.Add(actual.ToString().Trim());
        return campos;
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