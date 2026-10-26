namespace GestorSolicitudes.Converters;

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using GestorSolicitudes.Helpers;
using GestorSolicitudes.Models;

/// <summary>Muestra la descripción traducida de un enum en lugar de su nombre en código.</summary>
public class EnumDescripcionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Enum e ? EnumHelper.Descripcion(e) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Oculta un elemento cuando el origen es nulo.</summary>
public class NuloAVisibilidadConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Fecha corta en la cultura del idioma activo. Se ignora la cultura que llega por parámetro
/// (WPF pasa la del xml:lang, que por defecto es en-US) y se usa la de la app.
/// </summary>
public class FechaCortaConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DateTime fecha ? fecha.ToString("d", Localizacion.CulturaActual) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Color de la etiqueta de estado, para localizar de un vistazo dónde está cada proceso.</summary>
public class EstadoAColorConverter : IValueConverter
{
    private const string ColorEntrevista = "#7C3AED";

    // Pinceles congelados y compartidos: no se crea uno por celda ni por repintado.
    private static readonly Brush Enviada = Crear("#64748B");
    private static readonly Brush EnRevision = Crear("#0284C7");
    private static readonly Brush Entrevista = Crear(ColorEntrevista);
    private static readonly Brush OfertaRecibida = Crear("#059669");
    private static readonly Brush OfertaAceptada = Crear("#047857");
    private static readonly Brush OfertaRechazada = Crear("#B45309");
    private static readonly Brush Rechazada = Crear("#DC2626");
    private static readonly Brush SinRespuesta = Crear("#94A3B8");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            EstadoSolicitud.Enviada => Enviada,
            EstadoSolicitud.EnRevision => EnRevision,
            EstadoSolicitud.PruebaTecnica => Entrevista,
            EstadoSolicitud.EntrevistaRrhh => Entrevista,
            EstadoSolicitud.EntrevistaTecnica => Entrevista,
            EstadoSolicitud.EntrevistaFinal => Entrevista,
            EstadoSolicitud.OfertaRecibida => OfertaRecibida,
            EstadoSolicitud.OfertaAceptada => OfertaAceptada,
            EstadoSolicitud.OfertaRechazada => OfertaRechazada,
            EstadoSolicitud.Rechazada => Rechazada,
            EstadoSolicitud.Retirada => OfertaRechazada,
            EstadoSolicitud.SinRespuesta => SinRespuesta,
            _ => Enviada,
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static Brush Crear(string hex)
    {
        var pincel = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        pincel.Freeze();
        return pincel;
    }
}

/// <summary>Texto del botón que muestra/oculta el panel de la gráfica de embudo.</summary>
public class VerGraficaATextoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Localizacion.Texto("Grafica.Ocultar") : Localizacion.Texto("Grafica.Ver");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Si el valor es una cadena no vacía la devuelve; si no, el texto de la clave localizada que llega por parámetro.</summary>
public class NuloACadenaConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string s && !string.IsNullOrEmpty(s)
            ? s
            : Localizacion.Texto(parameter as string ?? string.Empty);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Visible solo cuando el estado es "Descartado por la empresa": es el único caso
/// en el que tiene sentido preguntar el motivo del rechazo.
/// </summary>
public class EstadoEsRechazadaAVisibilidadConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is EstadoSolicitud.Rechazada ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Convierte un nivel de interés (1 a 5) en su representación compacta con estrellas Unicode.</summary>
public class InteresATextoEstrellasConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        int interes = value is int i ? Math.Clamp(i, 0, 5) : 0;
        return new string('★', interes) + new string('☆', 5 - interes);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
