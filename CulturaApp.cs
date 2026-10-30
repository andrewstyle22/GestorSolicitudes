namespace GestorSolicitudes;

using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using GestorSolicitudes.Helpers;

/// <summary>
/// Aplica un idioma a la aplicación: cultura del hilo (fechas y números), lenguaje de las
/// ventanas WPF y diccionario de textos de Application.Resources. Solo lo usa
/// <see cref="Localizacion"/>; vive aparte para que la fachada no mezcle esto con la
/// preferencia guardada ni con los diccionarios.
/// </summary>
internal static class CulturaApp
{
    private static ResourceDictionary? diccionarioDeTextos;

    /// <summary>Cultura (fechas, números) que corresponde a cada idioma.</summary>
    public static CultureInfo Cultura(Idioma idioma) => idioma switch
    {
        Idioma.Ingles => CultureInfo.GetCultureInfo("en-US"),
        Idioma.Aleman => CultureInfo.GetCultureInfo("de-DE"),
        _ => CultureInfo.GetCultureInfo("es-ES"),
    };

    /// <summary>Cultura del hilo, lenguaje de las ventanas abiertas y recursos de texto.</summary>
    public static void Aplicar(Idioma idioma)
    {
        CultureInfo cultura = Cultura(idioma);
        CultureInfo.DefaultThreadCurrentCulture = cultura;
        CultureInfo.DefaultThreadCurrentUICulture = cultura;
        Thread.CurrentThread.CurrentCulture = cultura;
        Thread.CurrentThread.CurrentUICulture = cultura;
        ActualizarLenguajeDeLasVentanas(cultura);
        AplicarRecursos(idioma);
    }

    /// <summary>Fija el lenguaje por defecto de los FrameworkElement. Solo puede llamarse una vez por tipo.</summary>
    public static void FijarLenguajeInicial(CultureInfo cultura) =>
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(cultura.IetfLanguageTag)));

    /// <summary>Propaga el nuevo lenguaje a las ventanas abiertas (afecta a DatePicker y formatos).</summary>
    private static void ActualizarLenguajeDeLasVentanas(CultureInfo cultura)
    {
        if (Application.Current is null)
        {
            return;
        }

        string etiqueta = cultura.IetfLanguageTag;
        foreach (Window ventana in Application.Current.Windows)
        {
            ventana.Language = XmlLanguage.GetLanguage(etiqueta);
        }
    }

    /// <summary>
    /// Reconstruye el diccionario de textos inyectado en Application.Resources a partir de los
    /// diccionarios C#. Los {DynamicResource} resuelven contra él y se actualizan al cambiarlo.
    /// </summary>
    private static void AplicarRecursos(Idioma idioma)
    {
        ResourceDictionary? recursos = Application.Current?.Resources;
        if (recursos is null)
        {
            return;
        }

        if (diccionarioDeTextos is not null)
        {
            recursos.MergedDictionaries.Remove(diccionarioDeTextos);
        }

        var diccionario = new ResourceDictionary();
        foreach (KeyValuePair<string, string> par in Traducciones.Tabla(Idioma.Castellano))
        {
            diccionario[par.Key] = par.Value;
        }

        if (idioma != Idioma.Castellano)
        {
            foreach (KeyValuePair<string, string> par in Traducciones.Tabla(idioma))
            {
                diccionario[par.Key] = par.Value;
            }
        }

        diccionarioDeTextos = diccionario;
        recursos.MergedDictionaries.Add(diccionario);
    }
}
