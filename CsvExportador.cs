namespace GestorSolicitudes.Helpers;

using System.Text;
using GestorSolicitudes.Models;

/// <summary>
/// El CSV de exportación: una cabecera traducida y una fila por candidatura. Vive fuera del
/// ViewModel porque es formateo puro, y aquí la cabecera y la fila se pueden mantener alineadas.
/// </summary>
public static class CsvExportador
{
    private const string FormatoFecha = "yyyy-MM-dd";

    /// <summary>
    /// Una columna del CSV: la clave que se traduce en la cabecera y cómo se escribe su valor.
    /// Al vivir juntas, cabecera y fila no se pueden desalinear.
    /// </summary>
    private static readonly (string Clave, Func<Solicitud, string> Valor)[] Columnas =
    [
        ("Csv.Empresa", s => CsvHelper.Escapar(s.Empresa)),
        ("Csv.Puesto", s => CsvHelper.Escapar(s.Puesto)),
        ("Csv.Estado", s => CsvHelper.Escapar(EnumHelper.Descripcion(s.Estado))),
        ("Csv.Portal", s => CsvHelper.Escapar(s.Portal)),
        ("Csv.Ubicacion", s => CsvHelper.Escapar(s.Ubicacion)),
        ("Csv.Modalidad", s => CsvHelper.Escapar(EnumHelper.Descripcion(s.Modalidad))),
        ("Csv.FechaSolicitud", s => s.FechaSolicitud.ToString(FormatoFecha)),
        ("Csv.PrimeraRespuesta", s => s.FechaPrimeraRespuesta?.ToString(FormatoFecha) ?? string.Empty),
        ("Csv.DiasHastaRespuesta", s => s.DiasHastaRespuesta?.ToString() ?? string.Empty),
        ("Csv.Entrevista", s => s.FechaEntrevista?.ToString(FormatoFecha) ?? string.Empty),
        ("Csv.Cierre", s => s.FechaCierre?.ToString(FormatoFecha) ?? string.Empty),
        ("Csv.ProximoSeguimiento", s => s.ProximoSeguimiento?.ToString(FormatoFecha) ?? string.Empty),
        ("Csv.SalarioMin", s => s.SalarioMin?.ToString() ?? string.Empty),
        ("Csv.SalarioMax", s => s.SalarioMax?.ToString() ?? string.Empty),
        ("Csv.Pretension", s => s.PretensionSalarial?.ToString() ?? string.Empty),
        ("Csv.Interes", s => s.Interes.ToString()),
        ("Csv.Tecnologias", s => CsvHelper.Escapar(s.Tecnologias)),
        ("Csv.Contacto", s => CsvHelper.Escapar(s.ContactoNombre)),
        ("Csv.EmailContacto", s => CsvHelper.Escapar(s.ContactoEmail)),
        ("Csv.RespuestaEmpresa", s => CsvHelper.Escapar(s.RespuestaEmpresa)),
        ("Csv.Notas", s => CsvHelper.Escapar(s.Notas)),
        ("Csv.Enlace", s => CsvHelper.Escapar(s.EnlaceOferta)),
    ];

    /// <summary>CSV completo (cabecera + filas), en el orden en que lleguen las solicitudes.</summary>
    public static string Construir(IEnumerable<Solicitud> solicitudes)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(';', Columnas.Select(c => Localizacion.Texto(c.Clave))));

        foreach (Solicitud s in solicitudes)
        {
            sb.AppendLine(string.Join(';', Columnas.Select(c => c.Valor(s))));
        }

        return sb.ToString();
    }
}
