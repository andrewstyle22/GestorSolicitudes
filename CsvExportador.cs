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

    /// <summary>CSV completo (cabecera + filas), en el orden en que lleguen las solicitudes.</summary>
    public static string Construir(IEnumerable<Solicitud> solicitudes)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(
            ';',
            Localizacion.Texto("Csv.Empresa"),
            Localizacion.Texto("Csv.Puesto"),
            Localizacion.Texto("Csv.Estado"),
            Localizacion.Texto("Csv.Portal"),
            Localizacion.Texto("Csv.Ubicacion"),
            Localizacion.Texto("Csv.Modalidad"),
            Localizacion.Texto("Csv.FechaSolicitud"),
            Localizacion.Texto("Csv.PrimeraRespuesta"),
            Localizacion.Texto("Csv.DiasHastaRespuesta"),
            Localizacion.Texto("Csv.Entrevista"),
            Localizacion.Texto("Csv.Cierre"),
            Localizacion.Texto("Csv.ProximoSeguimiento"),
            Localizacion.Texto("Csv.SalarioMin"),
            Localizacion.Texto("Csv.SalarioMax"),
            Localizacion.Texto("Csv.Pretension"),
            Localizacion.Texto("Csv.Interes"),
            Localizacion.Texto("Csv.Tecnologias"),
            Localizacion.Texto("Csv.Contacto"),
            Localizacion.Texto("Csv.EmailContacto"),
            Localizacion.Texto("Csv.RespuestaEmpresa"),
            Localizacion.Texto("Csv.Notas"),
            Localizacion.Texto("Csv.Enlace")));

        foreach (Solicitud s in solicitudes)
        {
            sb.AppendLine(string.Join(
                ';',
                CsvHelper.Escapar(s.Empresa),
                CsvHelper.Escapar(s.Puesto),
                CsvHelper.Escapar(EnumHelper.Descripcion(s.Estado)),
                CsvHelper.Escapar(s.Portal),
                CsvHelper.Escapar(s.Ubicacion),
                CsvHelper.Escapar(EnumHelper.Descripcion(s.Modalidad)),
                s.FechaSolicitud.ToString(FormatoFecha),
                s.FechaPrimeraRespuesta?.ToString(FormatoFecha) ?? string.Empty,
                s.DiasHastaRespuesta?.ToString() ?? string.Empty,
                s.FechaEntrevista?.ToString(FormatoFecha) ?? string.Empty,
                s.FechaCierre?.ToString(FormatoFecha) ?? string.Empty,
                s.ProximoSeguimiento?.ToString(FormatoFecha) ?? string.Empty,
                s.SalarioMin?.ToString() ?? string.Empty,
                s.SalarioMax?.ToString() ?? string.Empty,
                s.PretensionSalarial?.ToString() ?? string.Empty,
                s.Interes.ToString(),
                CsvHelper.Escapar(s.Tecnologias),
                CsvHelper.Escapar(s.ContactoNombre),
                CsvHelper.Escapar(s.ContactoEmail),
                CsvHelper.Escapar(s.RespuestaEmpresa),
                CsvHelper.Escapar(s.Notas),
                CsvHelper.Escapar(s.EnlaceOferta)));
        }

        return sb.ToString();
    }
}
