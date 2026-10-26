namespace GestorSolicitudes.Helpers;

/// <summary>
/// Validación de los enlaces que abre la app. Solo http y https: un campo de texto no debe
/// poder lanzar un protocolo registrado ni un fichero local.
/// </summary>
public static class Enlaces
{
    public static bool EsWeb(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
