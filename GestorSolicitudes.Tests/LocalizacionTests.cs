using Xunit;
using System.Globalization;
using GestorSolicitudes.Helpers;
using GestorSolicitudes.Models;

namespace GestorSolicitudes.Tests;

/// <summary>
/// Los tests de localización son puros: usan la sobrecarga Texto(Idioma, clave)
/// y no tocan el idioma activo para no interferir con el resto de suites que
/// esperan el castellano por defecto.
/// </summary>
public class LocalizacionTests
{
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
        IReadOnlyCollection<string> claves = Localizacion.Claves();
        Assert.NotEmpty(claves);

        foreach (Idioma idioma in Enum.GetValues<Idioma>())
        {
            foreach (string clave in claves)
            {
                // Ni una clave sin traducir ni vacía en ningún idioma.
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
        Assert.Equal(new[] { Idioma.Castellano, Idioma.Ingles, Idioma.Aleman }, idiomas.Select(i => i.Idioma));
    }

    [Fact]
    public void Cultura_DevuelveLaCulturaDeCadaIdioma()
    {
        Assert.Equal(new CultureInfo("es-ES"), Localizacion.Cultura(Idioma.Castellano));
        Assert.Equal(new CultureInfo("en-US"), Localizacion.Cultura(Idioma.Ingles));
        Assert.Equal(new CultureInfo("de-DE"), Localizacion.Cultura(Idioma.Aleman));
    }

    [Fact]
    public void Codigo_Y_TextoDeLasEstadisticasDependenDelIdioma()
    {
        Assert.Equal("es", Localizacion.Codigo(Idioma.Castellano));
        Assert.Equal("en", Localizacion.Codigo(Idioma.Ingles));
        Assert.Equal("de", Localizacion.Codigo(Idioma.Aleman));

        Assert.Equal("3 días", $"{3} {Localizacion.Texto(Idioma.Castellano, "Metrica.Dias")}");
        Assert.Equal("3 days", $"{3} {Localizacion.Texto(Idioma.Ingles, "Metrica.Dias")}");
        Assert.Equal("3 Tage", $"{3} {Localizacion.Texto(Idioma.Aleman, "Metrica.Dias")}");
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