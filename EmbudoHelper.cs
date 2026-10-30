namespace GestorSolicitudes.Helpers;

using System.Globalization;
using GestorSolicitudes.Models;

/// <summary>
/// El embudo enviadas → respondidas → entrevistas → ofertas. Vive fuera del ViewModel porque
/// es aritmética de fechas y conteos; el ViewModel solo aporta las solicitudes y pinta el resultado.
/// </summary>
public static class EmbudoHelper
{
    /// <summary>Número de barras del embudo, sea cual sea la granularidad.</summary>
    public const int PeriodosDelEmbudo = 12;

    /// <summary>
    /// Embudo de los <see cref="PeriodosDelEmbudo"/> periodos que terminan en el actual, de más
    /// antiguo a más reciente. Las solicitudes deben venir con sus eventos cargados.
    /// </summary>
    public static IReadOnlyList<EmbudoPeriodo> Calcular(
        IReadOnlyList<Solicitud> solicitudes,
        GranularidadEmbudo granularidad,
        int desplazamiento,
        CultureInfo cultura)
    {
        List<DateTime> periodos = CalcularPeriodos(granularidad, desplazamiento);

        double[] enviadas = new double[periodos.Count];
        double[] respondidas = new double[periodos.Count];
        double[] entrevistas = new double[periodos.Count];
        double[] ofertas = new double[periodos.Count];

        for (int m = 0; m < periodos.Count; m++)
        {
            DateTime inicio = periodos[m];
            DateTime fin = SiguientePeriodo(inicio, granularidad);

            enviadas[m] = solicitudes.Count(s => EnviadaEnRango(s, inicio, fin));
            respondidas[m] = solicitudes.Count(s => s.FechaPrimeraRespuesta >= inicio && s.FechaPrimeraRespuesta < fin);
            entrevistas[m] = solicitudes
                .SelectMany(s => s.Eventos)
                .Count(e => e.Tipo == TipoEvento.Entrevista && e.Fecha >= inicio && e.Fecha < fin);
            ofertas[m] = solicitudes
                .SelectMany(s => s.Eventos)
                .Count(e => e.Tipo == TipoEvento.Oferta && e.Fecha >= inicio && e.Fecha < fin);
        }

        return periodos
            .Select((inicio, i) => new EmbudoPeriodo(
                EtiquetaDePeriodo(inicio, granularidad, cultura),
                enviadas[i],
                respondidas[i],
                entrevistas[i],
                ofertas[i]))
            .ToList();
    }

    /// <summary>
    /// Los 12 periodos que terminan en el actual, de más antiguo a más reciente.
    /// <paramref name="desplazamiento"/> los retrasa ese número de periodos.
    /// </summary>
    public static List<DateTime> CalcularPeriodos(GranularidadEmbudo granularidad, int desplazamiento)
    {
        // Retroceder un periodo es meterse en el que contiene el día anterior: sirve igual
        // para el mes (el día anterior ya es del mes pasado), para la semana (domingo) y
        // para el día. Por eso no hace falta un "periodo anterior" aparte.
        DateTime fin = SiguientePeriodo(InicioDePeriodo(DateTime.Today, granularidad), granularidad);

        var periodos = new List<DateTime>(PeriodosDelEmbudo);
        for (int i = 0; i < desplazamiento + PeriodosDelEmbudo; i++)
        {
            fin = InicioDePeriodo(fin.AddDays(-1), granularidad);

            if (i >= desplazamiento)
            {
                periodos.Add(fin);
            }
        }

        periodos.Reverse();
        return periodos;
    }

    /// <summary>Primer día del periodo (mes, semana de lunes o día) al que pertenece una fecha.</summary>
    public static DateTime InicioDePeriodo(DateTime fecha, GranularidadEmbudo granularidad) => granularidad switch
    {
        GranularidadEmbudo.Semana => fecha.Date.AddDays(-((int)fecha.DayOfWeek + 6) % 7), // lunes
        GranularidadEmbudo.Dia => fecha.Date,
        _ => new DateTime(fecha.Year, fecha.Month, 1, 0, 0, 0, DateTimeKind.Unspecified),
    };

    /// <summary>Primer día del periodo siguiente al que empieza en <paramref name="inicio"/>.</summary>
    public static DateTime SiguientePeriodo(DateTime inicio, GranularidadEmbudo granularidad) => granularidad switch
    {
        GranularidadEmbudo.Semana => inicio.AddDays(7),
        GranularidadEmbudo.Dia => inicio.AddDays(1),
        _ => inicio.AddMonths(1),
    };

    /// <summary>Texto de la barra: mes y año, el lunes de la semana o el día de la semana, según la granularidad.</summary>
    public static string EtiquetaDePeriodo(DateTime inicio, GranularidadEmbudo granularidad, CultureInfo cultura) =>
        granularidad switch
        {
            GranularidadEmbudo.Semana => inicio.ToString("d MMM", cultura),
            GranularidadEmbudo.Dia => inicio.ToString("ddd d", cultura),
            _ => inicio.ToString("MMM yyyy", cultura),
        };

    /// <summary>Si la solicitud se envió (por historial o por fecha) dentro del rango [inicio, fin).</summary>
    public static bool EnviadaEnRango(Solicitud s, DateTime inicio, DateTime fin)
    {
        bool tieneHito = s.Eventos.Any(e =>
            e.Tipo == TipoEvento.SolicitudEnviada && e.Fecha >= inicio && e.Fecha < fin);

        bool sinHito = !s.Eventos.Any(e => e.Tipo == TipoEvento.SolicitudEnviada)
            && s.FechaSolicitud >= inicio && s.FechaSolicitud < fin;

        return tieneHito || sinHito;
    }
}
