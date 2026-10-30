namespace GestorSolicitudes.Tests;

using System.Globalization;
using GestorSolicitudes.Helpers;
using GestorSolicitudes.Models;

/// <summary>
/// Los tests de localización son puros: usan la sobrecarga Texto(Idioma, clave)
/// y no tocan el idioma activo para no interferir con el resto de suites que
/// esperan el castellano por defecto.
/// </summary>
public class LocalizacionTests
{
    private static readonly Idioma[] IdiomasEsperados = [Idioma.Castellano, Idioma.Ingles, Idioma.Aleman];

    [Fact]
    public void Texto_TraduceConElIdiomaPedidoSinTocarElActivo()
    {
        Assert.Equal("Gestor de candidaturas", Localizacion.Texto(Idioma.Castellano, "TituloVentana"));
        Assert.Equal("Application Tracker", Localizacion.Texto(Idioma.Ingles, "TituloVentana"));
        Assert.Equal("Bewerbungstracker", Localizacion.Texto(Idioma.Aleman, "TituloVentana"));
    }

    [Fact]
    public void Texto_TraduceLosEnumsQueUsanLosDesplegables()
    {
        Assert.Equal("Enviada", Localizacion.Texto(Idioma.Castellano, "EstadoSolicitud.Enviada"));
        Assert.Equal("Sent", Localizacion.Texto(Idioma.Ingles, "EstadoSolicitud.Enviada"));
        Assert.Equal("Gesendet", Localizacion.Texto(Idioma.Aleman, "EstadoSolicitud.Enviada"));

        Assert.Equal("100 % remoto", Localizacion.Texto(Idioma.Castellano, "Modalidad.Remoto"));
        Assert.Equal("100% remote", Localizacion.Texto(Idioma.Ingles, "Modalidad.Remoto"));
        Assert.Equal("100 % remote", Localizacion.Texto(Idioma.Aleman, "Modalidad.Remoto"));
    }

    [Fact]
    public void Texto_ClaveDesconocidaDevuelveLaPropiaClave()
    {
        // Sin clave en ningún diccionario, se devuelve la propia clave como último
        // recurso, sea cual sea el idioma pedido.
        Assert.Equal("Clave.Inventada", Localizacion.Texto(Idioma.Castellano, "Clave.Inventada"));
        Assert.Equal("Clave.Inventada", Localizacion.Texto(Idioma.Ingles, "Clave.Inventada"));
        Assert.Equal("Clave.Inventada", Localizacion.Texto(Idioma.Aleman, "Clave.Inventada"));
        Assert.Equal("Clave.Inventada", Localizacion.Texto("Clave.Inventada"));
    }

    [Fact]
    public void Contiene_ReconoceLasClavesExistentes()
    {
        Assert.True(Localizacion.Contiene("TituloVentana"));
        Assert.True(Localizacion.Contiene("EstadoSolicitud.Enviada"));
        Assert.False(Localizacion.Contiene("No.Existe.Esta.Clave"));
    }

    [Fact]
    public void TodosLosIdiomasCubrenLasMismasClaves()
    {
        IReadOnlyCollection<string> claves = Localizacion.Claves(Idioma.Castellano);
        Assert.NotEmpty(claves);

        foreach (Idioma idioma in Enum.GetValues<Idioma>())
        {
            // Mismos conjuntos: comparar los textos no sirve porque Texto cae al castellano.
            Assert.Equal(claves.Order(), Localizacion.Claves(idioma).Order());

            foreach (string clave in claves)
            {
                // Ni una clave vacía en ningún idioma.
                Assert.False(
                    string.IsNullOrWhiteSpace(Localizacion.Texto(idioma, clave)),
                    $"La clave '{clave}' no tiene texto en {idioma}.");
            }
        }
    }

    [Fact]
    public void IdiomasDisponibles_SonLosTresConSuNombreNativo()
    {
        IReadOnlyList<IdiomaItem> idiomas = Localizacion.IdiomasDisponibles();

        Assert.Equal(3, idiomas.Count);
        Assert.Equal("Español", idiomas[0].Nombre);
        Assert.Equal("English", idiomas[1].Nombre);
        Assert.Equal("Deutsch", idiomas[2].Nombre);
        Assert.Equal(IdiomasEsperados, idiomas.Select(i => i.Idioma));
    }

    [Fact]
    public void Cultura_DevuelveLaCulturaDeCadaIdioma()
    {
        Assert.Equal(new CultureInfo("es-ES"), Localizacion.Cultura(Idioma.Castellano));
        Assert.Equal(new CultureInfo("en-US"), Localizacion.Cultura(Idioma.Ingles));
        Assert.Equal(new CultureInfo("de-DE"), Localizacion.Cultura(Idioma.Aleman));
    }

    [Fact]
    public void MetricaDias_EstaEnLosTresIdiomas()
    {
        Assert.Equal("días", Localizacion.Texto(Idioma.Castellano, "Metrica.Dias"));
        Assert.Equal("days", Localizacion.Texto(Idioma.Ingles, "Metrica.Dias"));
        Assert.Equal("Tage", Localizacion.Texto(Idioma.Aleman, "Metrica.Dias"));
    }

    [Theory]
    [InlineData(Idioma.Castellano, "por por")]
    [InlineData(Idioma.Ingles, "by by")]
    [InlineData(Idioma.Aleman, "nach nach")]
    public void TituloDeLaGrafica_NoDuplicaLaPreposicion(Idioma idioma, string duplicada)
    {
        string titulo = string.Format(
            Localizacion.Cultura(idioma),
            Localizacion.Texto(idioma, "Grafica.Titulo"),
            Localizacion.Texto(idioma, "Grafica.TituloMes"));

        Assert.DoesNotContain(duplicada, titulo);
    }

    [Fact]
    public void EnumHelper_UsaLasTraduccionesDeLocalizacion()
    {
        // En el idioma activo (castellano por defecto) los desplegables recogen las
        // traducciones de los diccionarios, no los [Description].
        Assert.Equal(EnumHelper.Descripcion(EstadoSolicitud.Enviada), Localizacion.Texto("EstadoSolicitud.Enviada"));
        Assert.Equal(EnumHelper.Descripcion(TipoEvento.SolicitudEnviada), Localizacion.Texto("TipoEvento.SolicitudEnviada"));
    }
}