using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using GestorSolicitudes.Converters;
using GestorSolicitudes.Models;

namespace GestorSolicitudes.Tests;

public class ConvertidorTests
{
    [Fact]
    public void EnumDescripcion_MuestraLaDescripcion()
    {
        var conversor = new EnumDescripcionConverter();

        object resultado = conversor.Convert(EstadoSolicitud.EntrevistaRrhh, typeof(string), null, null!);

        Assert.Equal("Entrevista RR. HH.", resultado);
    }

    [Fact]
    public void EnumDescripcion_ConNuloDevuelveVacio()
    {
        var conversor = new EnumDescripcionConverter();
        Assert.Equal(string.Empty, conversor.Convert(null, typeof(string), null, null!));
    }

    [Fact]
    public void NuloAVisibilidad_ConDatosVisibleSinDatosColapsado()
    {
        var conversor = new NuloAVisibilidadConverter();

        Assert.Equal(Visibility.Collapsed, conversor.Convert(null, typeof(Visibility), null, null!));
        Assert.Equal(Visibility.Visible, conversor.Convert("algo", typeof(Visibility), null, null!));
    }

    [Fact]
    public void FechaCorta_UsaLaCulturaDelIdiomaActivoEIgnoraLaDelBinding()
    {
        var conversor = new FechaCortaConverter();
        var fecha = new DateTime(2024, 5, 1);

        object resultado = conversor.Convert(fecha, typeof(string), null, new CultureInfo("en-US"));

        Assert.Equal(fecha.ToString("d", Localizacion.CulturaActual), resultado);
    }

    [Fact]
    public void FechaCorta_ConNuloDevuelveVacio()
    {
        var conversor = new FechaCortaConverter();
        Assert.Equal(string.Empty, conversor.Convert(null, typeof(string), null, null!));
    }

    [Theory]
    [InlineData(true, "Ocultar gráfica")]
    [InlineData(false, "Gráfico")]
    public void VerGraficaATexto_DevuelveElTextoDelBoton(bool visible, string esperado)
    {
        var conversor = new VerGraficaATextoConverter();
        Assert.Equal(esperado, conversor.Convert(visible, typeof(string), null, null!));
    }

    [StaFact]
    public void EstadoAColor_ConValorDesconocidoUsaElGris()
    {
        var conversor = new EstadoAColorConverter();
        var pincel = (SolidColorBrush)conversor.Convert(null, typeof(Brush), null, null!);
        Assert.Equal((byte)0x64, pincel.Color.R);
    }

    [StaFact]
    public void EstadoAColor_CadaEstadoTieneSuColor()
    {
        var conversor = new EstadoAColorConverter();
        var esperados = new Dictionary<EstadoSolicitud, string>
        {
            { EstadoSolicitud.Enviada, "#64748B" },
            { EstadoSolicitud.EnRevision, "#0284C7" },
            { EstadoSolicitud.PruebaTecnica, "#7C3AED" },
            { EstadoSolicitud.EntrevistaRrhh, "#7C3AED" },
            { EstadoSolicitud.EntrevistaTecnica, "#7C3AED" },
            { EstadoSolicitud.EntrevistaFinal, "#7C3AED" },
            { EstadoSolicitud.OfertaRecibida, "#059669" },
            { EstadoSolicitud.OfertaAceptada, "#047857" },
            { EstadoSolicitud.OfertaRechazada, "#B45309" },
            { EstadoSolicitud.Rechazada, "#DC2626" },
            { EstadoSolicitud.Retirada, "#B45309" },
            { EstadoSolicitud.SinRespuesta, "#94A3B8" },
        };

        foreach ((EstadoSolicitud estado, string hex) in esperados)
        {
            var color = ((SolidColorBrush)conversor.Convert(estado, typeof(Brush), null, null!)).Color;
            Assert.Equal((Color)ColorConverter.ConvertFromString(hex), color);
        }
    }

    [StaFact]
    public void EstadoAColor_NingunEstadoSeQuedaSinColorPropio()
    {
        var conversor = new EstadoAColorConverter();

        // Enviada es el gris por defecto a propósito; el resto no debe caer en él.
        foreach (EstadoSolicitud estado in Enum.GetValues<EstadoSolicitud>().Except([EstadoSolicitud.Enviada]))
        {
            Assert.NotSame(ColoresEstado.PorDefecto, conversor.Convert(estado, typeof(Brush), null, null!));
        }
    }

    [Theory]
    [InlineData(EstadoSolicitud.Rechazada, Visibility.Visible)]
    [InlineData(EstadoSolicitud.Enviada, Visibility.Collapsed)]
    [InlineData(EstadoSolicitud.EnRevision, Visibility.Collapsed)]
    [InlineData(EstadoSolicitud.OfertaAceptada, Visibility.Collapsed)]
    [InlineData(EstadoSolicitud.Retirada, Visibility.Collapsed)]
    public void EstadoEsRechazadaAVisibilidad_SoloRechazadaEsVisible(EstadoSolicitud estado, Visibility esperado)
    {
        var conversor = new EstadoEsRechazadaAVisibilidadConverter();
        Assert.Equal(esperado, conversor.Convert(estado, typeof(Visibility), null, null!));
    }

    [Fact]
    public void EstadoEsRechazadaAVisibilidad_ConValorNuloColapsado()
    {
        var conversor = new EstadoEsRechazadaAVisibilidadConverter();
        Assert.Equal(Visibility.Collapsed, conversor.Convert(null, typeof(Visibility), null, null!));
    }

    [Theory]
    [InlineData(1, "★☆☆☆☆")]
    [InlineData(2, "★★☆☆☆")]
    [InlineData(3, "★★★☆☆")]
    [InlineData(4, "★★★★☆")]
    [InlineData(5, "★★★★★")]
    public void InteresATextoEstrellas_UnaEstrellaPorPunto(int interes, string esperado)
    {
        var conversor = new InteresATextoEstrellasConverter();
        Assert.Equal(esperado, conversor.Convert(interes, typeof(string), null, null!));
    }

    [Fact]
    public void InteresATextoEstrellas_AcotaFueraDeRangoYNulos()
    {
        var conversor = new InteresATextoEstrellasConverter();

        Assert.Equal("☆☆☆☆☆", conversor.Convert(0, typeof(string), null, null!));
        Assert.Equal("★★★★★", conversor.Convert(9, typeof(string), null, null!));
        Assert.Equal("☆☆☆☆☆", conversor.Convert(null, typeof(string), null, null!));
    }

    [Fact]
    public void ConvertBack_LanzaNotSupportedEnTodosLosConvertidores()
    {
        var invertir = new Action<IValueConverter>(c =>
            Assert.Throws<NotSupportedException>(() => c.ConvertBack(null, typeof(object), null, CultureInfo.InvariantCulture)));

        invertir(new EnumDescripcionConverter());
        invertir(new NuloAVisibilidadConverter());
        invertir(new EstadoAColorConverter());
        invertir(new FechaCortaConverter());
        invertir(new VerGraficaATextoConverter());
        invertir(new EstadoEsRechazadaAVisibilidadConverter());
        invertir(new InteresATextoEstrellasConverter());
    }
}