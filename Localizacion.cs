namespace GestorSolicitudes;

using System.Globalization;
using GestorSolicitudes.Helpers;

/// <summary>Opción del selector de idioma: idioma + nombre tal como se ve en su propia lengua.</summary>
public sealed record IdiomaItem(Idioma Idioma, string Nombre);

/// <summary>
/// Fachada del idioma: idioma activo, textos y cambio de idioma. Los diccionarios viven en
/// <see cref="Traducciones"/> (Helpers), la cultura del hilo y los recursos WPF en
/// <see cref="CulturaApp"/>, y la preferencia guardada en <see cref="PreferenciaIdioma"/>.
/// </summary>
public static class Localizacion
{
    /// <summary>Se dispara al cambiar de idioma, para que la interfaz refresque lo que no usa DynamicResource.</summary>
    public static event EventHandler<Idioma>? IdiomaCambiado;

    /// <summary>Gets idioma activo en este momento. Se decide al arrancar y cambia con el selector.</summary>
    public static Idioma IdiomaActual => Traducciones.IdiomaActual;

    /// <summary>Gets cultura correspondiente al idioma activo.</summary>
    public static CultureInfo CulturaActual => Cultura(IdiomaActual);

    /// <summary>Cultura (fechas, números) que corresponde a cada idioma.</summary>
    public static CultureInfo Cultura(Idioma idioma) => CulturaApp.Cultura(idioma);

    /// <summary>Traduce una clave con el idioma activo. Si falta, cae al castellano y, en último caso, a la propia clave.</summary>
    public static string Texto(string clave) => Traducciones.Texto(clave);

    /// <summary>Traduce una clave a un idioma concreto, sin tocar el idioma activo.</summary>
    public static string Texto(Idioma idioma, string clave) => Traducciones.Texto(idioma, clave);

    /// <summary>Indica si una clave existe en las traducciones.</summary>
    public static bool Contiene(string clave) => Traducciones.Contiene(clave);

    /// <summary>Las tres opciones del selector de idioma, con su nombre en cada lengua.</summary>
    public static IReadOnlyList<IdiomaItem> IdiomasDisponibles() =>
        new[] { Idioma.Castellano, Idioma.Ingles, Idioma.Aleman }
            .Select(i => new IdiomaItem(i, Traducciones.NombrePropio(i)))
            .ToList();

    /// <summary>Las claves de un idioma, para comprobar que los tres diccionarios tienen las mismas.</summary>
    public static IReadOnlyCollection<string> Claves(Idioma idioma) => Traducciones.Claves(idioma);

    /// <summary>
    /// Decide el idioma inicial (preferencia guardada o, si no hay, el del sistema) y lo aplica
    /// a la cultura del hilo y a los recursos de aplicación. Solo una vez, en el arranque,
    /// antes de crear la ventana.
    /// </summary>
    public static void Inicializar()
    {
        Traducciones.IdiomaActual =
            PreferenciaIdioma.LeerDe(PreferenciaIdioma.Ruta)
            ?? PreferenciaIdioma.DelSistema(CultureInfo.CurrentCulture);
        CulturaApp.FijarLenguajeInicial(CulturaActual);
        CulturaApp.Aplicar(Traducciones.IdiomaActual);
    }

    /// <summary>Cambia el idioma al instante: guarda la preferencia, lo aplica y avisa a los suscriptores.</summary>
    public static void Cambiar(Idioma idioma)
    {
        if (idioma == Traducciones.IdiomaActual)
        {
            return;
        }

        Traducciones.IdiomaActual = idioma;
        PreferenciaIdioma.GuardarEn(PreferenciaIdioma.Ruta, idioma);
        CulturaApp.Aplicar(idioma);
        IdiomaCambiado?.Invoke(null, idioma);
    }
}
