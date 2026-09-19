using Xunit;
using GestorSolicitudes.Helpers;
using GestorSolicitudes.Models;

namespace GestorSolicitudes.Tests;

public class EnumHelperTests
{
    [Fact]
    public void Descripcion_DevuelveDescripcionDelAtributo()
    {
        Assert.Equal("Enviada", EnumHelper.Descripcion(EstadoSolicitud.Enviada));
        Assert.Equal("Entrevista RR. HH.", EnumHelper.Descripcion(EstadoSolicitud.EntrevistaRrhh));
        Assert.Equal("100 % remoto", EnumHelper.Descripcion(Modalidad.Remoto));
        Assert.Equal("Solicitud enviada", EnumHelper.Descripcion(TipoEvento.SolicitudEnviada));
    }

    [Fact]
    public void Descripcion_SinAtributoUsaElNombre()
    {
        Assert.Equal("Uno", EnumHelper.Descripcion(SinDescripcion.Uno));
    }

    [Fact]
    public void Valores_DevuelveTodosLosValoresTraducidos()
    {
        IReadOnlyList<EnumItem> valores = EnumHelper.Valores<EstadoSolicitud>();

        Assert.Equal(Enum.GetValues<EstadoSolicitud>().Length, valores.Count);
        Assert.Equal("Enviada", valores[0].Descripcion);
        Assert.Equal(EstadoSolicitud.Enviada, valores[0].Valor);
        Assert.All(valores, v => Assert.False(string.IsNullOrWhiteSpace(v.Descripcion)));
    }

    [Fact]
    public void Valores_DeModalidadEmpiezaPorSinIndicar()
    {
        IReadOnlyList<EnumItem> valores = EnumHelper.Valores<Modalidad>();
        Assert.Equal(Modalidad.SinIndicar, valores[0].Valor);
    }

    private enum SinDescripcion
    {
        Uno
    }
}