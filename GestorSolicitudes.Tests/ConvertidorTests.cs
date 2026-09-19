using Xunit;
using System.Windows;
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
    public void NuloAVisibilidad_ConParametroInvertirHaceLoContrario()
    {
        var conversor = new NuloAVisibilidadConverter();

        Assert.Equal(Visibility.Visible, conversor.Convert(null, typeof(Visibility), "invertir", null!));
        Assert.Equal(Visibility.Collapsed, conversor.Convert("algo", typeof(Visibility), "invertir", null!));
    }

    [Theory]
    [InlineData(true, "Ocultar gráfica")]
    [InlineData(false, "Ver gráfica")]
    public void VerGraficaATexto_DevuelveElTextoDelBoton(bool visible, string esperado)
    {
        var conversor = new VerGraficaATextoConverter();
        Assert.Equal(esperado, conversor.Convert(visible, typeof(string), null, null!));
    }

    [StaFact]
    public void EstadoAColor_PintaCadaEstadoConSuColor()
    {
        var conversor = new EstadoAColorConverter();

        var gris = (SolidColorBrush)conversor.Convert(EstadoSolicitud.Enviada, typeof(Brush), null, null!);
        Assert.Equal((byte)0x64, gris.Color.R);
        Assert.Equal((byte)0x74, gris.Color.G);
        Assert.Equal((byte)0x8B, gris.Color.B);

        var azul = (SolidColorBrush)conversor.Convert(EstadoSolicitud.EnRevision, typeof(Brush), null, null!);
        Assert.Equal((byte)0x02, azul.Color.R);

        var rojo = (SolidColorBrush)conversor.Convert(EstadoSolicitud.Rechazada, typeof(Brush), null, null!);
        Assert.Equal((byte)0xDC, rojo.Color.R);
        Assert.Equal((byte)0x26, rojo.Color.G);
        Assert.Equal((byte)0x26, rojo.Color.B);
    }

    [StaFact]
    public void EstadoAColor_ConValorDesconocidoUsaElGris()
    {
        var conversor = new EstadoAColorConverter();
        var pincel = (SolidColorBrush)conversor.Convert(null, typeof(Brush), null, null!);
        Assert.Equal((byte)0x64, pincel.Color.R);
    }

    [StaFact]
    public void InteresDeEstrella_RellenaSoloLasPosicionesDentroDelValor()
    {
        var conversor = new InteresAColorEstrellaConverter();

        var dentro = (SolidColorBrush)conversor.Convert(3, typeof(Brush), "2", null!);
        Assert.Equal((byte)0xF5, dentro.Color.R); // ámbar #F59E0B

        var fuera = (SolidColorBrush)conversor.Convert(3, typeof(Brush), "4", null!);
        Assert.Equal((byte)0xCB, fuera.Color.R); // gris #CBD5E1
    }
}