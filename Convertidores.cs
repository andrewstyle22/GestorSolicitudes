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

/// <summary>Oculta un elemento cuando el origen es nulo. Con parámetro "invertir" hace lo contrario.</summary>
public class NuloAVisibilidadConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool visible = value is not null;
        if (string.Equals(parameter as string, "invertir", StringComparison.OrdinalIgnoreCase))
        {
            visible = !visible;
        }

        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Color de la etiqueta de estado, para localizar de un vistazo dónde está cada proceso.</summary>
public class EstadoAColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var color = value switch
        {
            EstadoSolicitud.Enviada => "#64748B",
            EstadoSolicitud.EnRevision => "#0284C7",
            EstadoSolicitud.PruebaTecnica => "#7C3AED",
            EstadoSolicitud.EntrevistaRrhh => "#7C3AED",
            EstadoSolicitud.EntrevistaTecnica => "#7C3AED",
            EstadoSolicitud.EntrevistaFinal => "#7C3AED",
            EstadoSolicitud.OfertaRecibida => "#059669",
            EstadoSolicitud.OfertaAceptada => "#047857",
            EstadoSolicitud.OfertaRechazada => "#B45309",
            EstadoSolicitud.Rechazada => "#DC2626",
            EstadoSolicitud.Retirada => "#B45309",
            EstadoSolicitud.SinRespuesta => "#94A3B8",
            _ => "#64748B",
        };

        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
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
