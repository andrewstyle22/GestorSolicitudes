namespace GestorSolicitudes.Converters;

using System.Windows.Media;
using GestorSolicitudes.Models;

/// <summary>
/// Tabla estado → color de la etiqueta, para localizar de un vistazo dónde está cada proceso.
/// Pinceles congelados y compartidos: no se crea uno por celda ni por repintado. Son estáticos
/// de C# y no recursos de App.xaml porque los tests de los convertidores no son STA.
/// </summary>
internal static class ColoresEstado
{
    /// <summary>Gris de los estados sin color propio.</summary>
    public static readonly Brush PorDefecto = Crear("#64748B");

    private static readonly Brush Entrevista = Crear("#7C3AED");

    // Mismo pincel para los dos cierres sin éxito: oferta rechazada y retirada.
    private static readonly Brush CierreSinExito = Crear("#B45309");

    private static readonly Dictionary<EstadoSolicitud, Brush> PorEstado = new()
    {
        [EstadoSolicitud.Enviada] = PorDefecto,
        [EstadoSolicitud.EnRevision] = Crear("#0284C7"),
        [EstadoSolicitud.PruebaTecnica] = Entrevista,
        [EstadoSolicitud.EntrevistaRrhh] = Entrevista,
        [EstadoSolicitud.EntrevistaTecnica] = Entrevista,
        [EstadoSolicitud.EntrevistaFinal] = Entrevista,
        [EstadoSolicitud.OfertaRecibida] = Crear("#059669"),
        [EstadoSolicitud.OfertaAceptada] = Crear("#047857"),
        [EstadoSolicitud.OfertaRechazada] = CierreSinExito,
        [EstadoSolicitud.Rechazada] = Crear("#DC2626"),
        [EstadoSolicitud.Retirada] = CierreSinExito,
        [EstadoSolicitud.SinRespuesta] = Crear("#94A3B8"),
    };

    public static Brush De(EstadoSolicitud estado) =>
        PorEstado.TryGetValue(estado, out Brush? pincel) ? pincel : PorDefecto;

    private static SolidColorBrush Crear(string hex)
    {
        var pincel = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        pincel.Freeze();
        return pincel;
    }
}
