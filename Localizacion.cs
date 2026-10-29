namespace GestorSolicitudes;

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using GestorSolicitudes.Helpers;

/// <summary>Opción del selector de idioma: idioma + nombre tal como se ve en su propia lengua.</summary>
public sealed record IdiomaItem(Idioma Idioma, string Nombre);

/// <summary>
/// Fachada del idioma: decide el idioma activo, fija la cultura del hilo y mantiene los textos
/// inyectados como recursos de Application. Los textos en sí y su consulta viven en
/// <see cref="Traducciones"/> (Helpers), para que cualquier helper pueda traducir sin depender
/// del Núcleo.
/// </summary>
public static class Localizacion
{
    /// <summary>Se dispara al cambiar de idioma, para que la interfaz refresque lo que no usa DynamicResource.</summary>
    public static event EventHandler<Idioma>? IdiomaCambiado;

    private static ResourceDictionary? diccionarioDeTextos;

    /// <summary>Gets idioma activo en este momento. Se decide al arrancar y cambia con el selector.</summary>
    public static Idioma IdiomaActual => Traducciones.IdiomaActual;

    /// <summary>Gets cultura correspondiente al idioma activo.</summary>
    public static CultureInfo CulturaActual => Cultura(IdiomaActual);

    /// <summary>Cultura (fechas, números) que corresponde a cada idioma.</summary>
    public static CultureInfo Cultura(Idioma idioma) => idioma switch
    {
        Idioma.Ingles => CultureInfo.GetCultureInfo("en-US"),
        Idioma.Aleman => CultureInfo.GetCultureInfo("de-DE"),
        _ => CultureInfo.GetCultureInfo("es-ES"),
    };

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
    /// Decide el idioma inicial (preferencia guardada o, si no hay, el del sistema), fija la
    /// cultura de la aplicación e inyecta los textos como recursos de aplicación. Solo una vez,
    /// en el arranque, antes de crear la ventana.
    /// </summary>
    public static void Inicializar()
    {
        Traducciones.IdiomaActual = CargarPreferenciaODetectar();
        AplicarCulturaDelHilo();
        ConfigurarLenguajeInicial();
        AplicarRecursosDeApplication();
    }

    /// <summary>Cambia el idioma al instante: cultura, textos de la interfaz y aviso a los suscriptores.</summary>
    public static void Cambiar(Idioma idioma)
    {
        if (idioma == Traducciones.IdiomaActual)
        {
            return;
        }

        Traducciones.IdiomaActual = idioma;
        GuardarPreferencia(idioma);
        AplicarCulturaDelHilo();
        ActualizarLenguajeDeLasVentanas();
        AplicarRecursosDeApplication();
        IdiomaCambiado?.Invoke(null, idioma);
    }

    /// <summary>
    /// Reconstruye el diccionario de textos inyectado en Application.Resources a partir de los
    /// diccionarios C#. Los {DynamicResource} resuelven contra él y se actualizan al cambiarlo.
    /// </summary>
    private static void AplicarRecursosDeApplication()
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

        if (IdiomaActual != Idioma.Castellano)
        {
            foreach (KeyValuePair<string, string> par in Traducciones.Tabla(IdiomaActual))
            {
                diccionario[par.Key] = par.Value;
            }
        }

        diccionarioDeTextos = diccionario;
        recursos.MergedDictionaries.Add(diccionario);
    }

    private static void AplicarCulturaDelHilo()
    {
        CultureInfo cultura = CulturaActual;
        CultureInfo.DefaultThreadCurrentCulture = cultura;
        CultureInfo.DefaultThreadCurrentUICulture = cultura;
        Thread.CurrentThread.CurrentCulture = cultura;
        Thread.CurrentThread.CurrentUICulture = cultura;
    }

    /// <summary>Fija el lenguaje por defecto de los FrameworkElement. Solo puede llamarse una vez por tipo.</summary>
    private static void ConfigurarLenguajeInicial()
    {
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CulturaActual.IetfLanguageTag)));
    }

    /// <summary>Propaga el nuevo lenguaje a las ventanas abiertas (afecta a DatePicker y formatos).</summary>
    private static void ActualizarLenguajeDeLasVentanas()
    {
        if (Application.Current is null)
        {
            return;
        }

        string etiqueta = CulturaActual.IetfLanguageTag;
        foreach (Window ventana in Application.Current.Windows)
        {
            ventana.Language = XmlLanguage.GetLanguage(etiqueta);
        }
    }

    /// <summary>Preferencia guardada en %APPDATA%\GestorSolicitudes\idioma.txt, o el idioma del sistema.</summary>
    private static Idioma CargarPreferenciaODetectar()
    {
        try
        {
            if (File.Exists(RutaPreferencia))
            {
                string contenido = File.ReadAllText(RutaPreferencia).Trim();
                if (Enum.TryParse(contenido, out Idioma guardado))
                {
                    return guardado;
                }
            }
        }
        catch (Exception ex)
        {
            // Si no se puede leer la preferencia, se cae al idioma del sistema.
            Debug.WriteLine($"No se pudo leer la preferencia de idioma: {ex}");
        }

        return CultureInfo.CurrentCulture.TwoLetterISOLanguageName switch
        {
            "en" => Idioma.Ingles,
            "de" => Idioma.Aleman,
            _ => Idioma.Castellano,
        };
    }

    private static void GuardarPreferencia(Idioma idioma)
    {
        try
        {
            string carpeta = Path.GetDirectoryName(RutaPreferencia)!;
            Directory.CreateDirectory(carpeta);
            File.WriteAllText(RutaPreferencia, idioma.ToString());
        }
        catch (Exception ex)
        {
            // Un fallo al guardar la preferencia no debe impedir el cambio de idioma.
            Debug.WriteLine($"No se pudo guardar la preferencia de idioma: {ex}");
        }
    }

    private static string RutaPreferencia => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GestorSolicitudes",
        "idioma.txt");
}
