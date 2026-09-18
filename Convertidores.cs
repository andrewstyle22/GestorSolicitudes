using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using GestorSolicitudes.Helpers;
using GestorSolicitudes.Models;

namespace GestorSolicitudes.Converters;

/// <summary>Muestra el [Description] de un enum en lugar de su nombre en código.</summary>
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
            visible = !visible;
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
            _ => "#64748B"
        };

        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
