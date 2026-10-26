namespace GestorSolicitudes.Helpers;

/// <summary>
/// Recorta una colección a la página pedida. Es puro y genérico: el ViewModel solo expone
/// el <see cref="ResultadoPagina{T}"/> y formatea el rango con la cultura activa.
/// </summary>
public static class Paginador
{
    /// <summary>
    /// Devuelve la página <paramref name="paginaActual"/> de <paramref name="elementos"/>.
    /// La página se recorta al rango válido; un tamaño menor que uno se trata como uno.
    /// </summary>
    public static ResultadoPagina<T> Paginar<T>(
        IReadOnlyList<T> elementos,
        int tamanoPagina,
        int paginaActual)
    {
        ArgumentNullException.ThrowIfNull(elementos);

        int tamano = Math.Max(1, tamanoPagina);
        int totalPaginas = elementos.Count == 0 ? 0 : ((elementos.Count - 1) / tamano) + 1;
        int pagina = Math.Clamp(paginaActual, 1, Math.Max(totalPaginas, 1));
        int desde = (pagina - 1) * tamano;
        int hasta = Math.Min(desde + tamano, elementos.Count);

        IReadOnlyList<T> paginaElementos = elementos
            .Skip(desde)
            .Take(tamano)
            .ToList();

        return new ResultadoPagina<T>(
            paginaElementos,
            pagina,
            totalPaginas,
            desde,
            hasta,
            elementos.Count);
    }
}

/// <summary>
/// Resultado de paginar una colección: la página recortada y los datos que necesita la UI
/// para el rango y las flechas. Es inmutable y no conoce WPF ni el ViewModel.
/// </summary>
public sealed record ResultadoPagina<T>(
    IReadOnlyList<T> Elementos,
    int PaginaActual,
    int TotalPaginas,
    int Desde,
    int Hasta,
    int Total)
{
    /// <summary>Gets si se puede ir a la primera página.</summary>
    public bool PuedeIrAInicio => this.PaginaActual > 1;

    /// <summary>Gets si se puede retroceder una página.</summary>
    public bool PuedeRetroceder => this.PaginaActual > 1;

    /// <summary>Gets si se puede avanzar una página.</summary>
    public bool PuedeAvanzar => this.PaginaActual < this.TotalPaginas;

    /// <summary>Gets si se puede ir a la última página.</summary>
    public bool PuedeIrAlFinal => this.PaginaActual < this.TotalPaginas;
}
