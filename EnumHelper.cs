namespace GestorSolicitudes.Helpers;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

/// <summary>Par valor/texto para alimentar los ComboBox de la interfaz.</summary>
public record EnumItem(object? Valor, string Descripcion);

public static class EnumHelper
{
    // La clave incluye el idioma: al cambiar de idioma se pide otra entrada y no hay que invalidar.
    private static readonly ConcurrentDictionary<(Idioma Idioma, Type Tipo, string Valor), string> Cache = new();

    /// <summary>
    /// Devuelve la descripción del valor en el idioma activo. Como los enums se guardan por su
    /// valor numérico, el texto mostrado puede cambiar de idioma sin tocar los datos.
    /// </summary>
    public static string Descripcion(Enum valor)
    {
        var claveCache = (Localizacion.IdiomaActual, valor.GetType(), valor.ToString());
        if (Cache.TryGetValue(claveCache, out string? cacheada))
        {
            return cacheada;
        }

        string texto = DescripcionSinCache(valor);
        Cache[claveCache] = texto;
        return texto;
    }

    private static string DescripcionSinCache(Enum valor)
    {
        string clave = Clave(valor);
        if (Localizacion.Contiene(clave))
        {
            return Localizacion.Texto(clave);
        }

        // Fallback: el [Description] (castellano) o, si no, el nombre del valor en código.
        FieldInfo? campo = valor.GetType().GetField(valor.ToString());
        var atributo = campo?.GetCustomAttribute<DescriptionAttribute>();
        return atributo?.Description ?? valor.ToString();
    }

    /// <summary>Clave de traducción de un valor de enum, del tipo "EstadoSolicitud.Enviada".</summary>
    internal static string Clave(Enum valor) => $"{valor.GetType().Name}.{valor}";

    /// <summary>Todos los valores de un enum ya traducidos, listos para enlazar.</summary>
    public static IReadOnlyList<EnumItem> Valores<T>()
        where T : struct, Enum
    {
        return Enum.GetValues<T>()
            .Select(v => new EnumItem(v, Descripcion(v)))
            .ToList();
    }
}
