namespace GestorSolicitudes.Helpers;

/// <summary>
/// Validación de los enlaces que abre la app. Solo http y https, y sin credenciales embebidas:
/// un campo de texto no debe poder lanzar un protocolo registrado ni un fichero local.
/// </summary>
public static class Enlaces
{
    public static bool EsWeb(string? url) => Canonica(url) is not null;

    /// <summary>
    /// Forma canónica que se le pasa al shell, o null si el enlace no es web. Un único análisis
    /// evita que el shell interprete la cadena original de otra manera.
    /// </summary>
    public static string? Canonica(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && uri.UserInfo.Length == 0
            ? uri.AbsoluteUri
            : null;
}
