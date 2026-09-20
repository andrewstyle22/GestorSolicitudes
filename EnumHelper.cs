namespace GestorSolicitudes.Helpers;

using System.ComponentModel;
using System.Reflection;

/// <summary>Par valor/texto para alimentar los ComboBox de la interfaz.</summary>
public record EnumItem(object? Valor, string Descripcion);

public static class EnumHelper
{
    /// <summary>
    /// Devuelve la descripción del valor en el idioma activo. Como los enums se guardan por su
    /// valor numérico, el texto mostrado puede cambiar de idioma sin tocar los datos.
    /// </summary>
    public static string Descripcion(Enum valor)
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
    public static string Clave(Enum valor) => $"{valor.GetType().Name}.{valor}";

    /// <summary>Todos los valores de un enum ya traducidos, listos para enlazar.</summary>
    public static IReadOnlyList<EnumItem> Valores<T>()
        where T : struct, Enum
    {
        return Enum.GetValues<T>()
            .Select(v => new EnumItem(v, Descripcion(v)))
            .ToList();
    }
}
