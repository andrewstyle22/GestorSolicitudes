// <copyright file="EnumHelper.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace GestorSolicitudes.Helpers;

using System.ComponentModel;
using System.Reflection;

/// <summary>Par valor/texto para alimentar los ComboBox de la interfaz.</summary>
public record EnumItem(object? Valor, string Descripcion);

public static class EnumHelper
{
    /// <summary>Devuelve el [Description] del valor, o su nombre si no lo tiene.</summary>
    /// <returns></returns>
    public static string Descripcion(Enum valor)
    {
        FieldInfo? campo = valor.GetType().GetField(valor.ToString());
        var atributo = campo?.GetCustomAttribute<DescriptionAttribute>();
        return atributo?.Description ?? valor.ToString();
    }

    /// <summary>Todos los valores de un enum ya traducidos, listos para enlazar.</summary>
    /// <returns></returns>
    public static IReadOnlyList<EnumItem> Valores<T>()
        where T : struct, Enum
        =>
        Enum.GetValues<T>()
            .Select(v => new EnumItem(v, Descripcion(v)))
            .ToList();
}
