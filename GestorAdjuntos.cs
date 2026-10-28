namespace GestorSolicitudes.Helpers;

using GestorSolicitudes.Models;

/// <summary>
/// Ciclo de vida de los adjuntos de una edición: copia los ficheros elegidos, recuerda esas
/// copias y borra las que la base ya no apunta. No conoce EF ni la UI; el ViewModel le pasa
/// las rutas guardadas y él decide qué ficheros sobran.
/// </summary>
public sealed class GestorAdjuntos
{
    private readonly string carpeta;
    private readonly List<string> copias = new();

    public GestorAdjuntos(string carpeta) => this.carpeta = carpeta;

    /// <summary>Copia el fichero elegido a la carpeta de adjuntos y recuerda la copia.</summary>
    public string Copiar(string origen, string etiqueta)
    {
        string ruta = AdjuntosHelper.Copiar(origen, etiqueta, this.carpeta);
        this.copias.Add(ruta);
        return ruta;
    }

    /// <summary>Borra un adjunto si está dentro de la carpeta de la aplicación.</summary>
    public void Eliminar(string? ruta) => AdjuntosHelper.Eliminar(ruta, this.carpeta);

    /// <summary>
    /// Tras guardar: borra los adjuntos que la base ya no apunta y las copias intermedias
    /// que no se han conservado.
    /// </summary>
    public void DescartarSustituidas(Solicitud solicitud, (string? Cv, string? Carta) anteriores)
    {
        if (anteriores.Cv != solicitud.RutaCv)
        {
            this.Eliminar(anteriores.Cv);
        }

        if (anteriores.Carta != solicitud.RutaCarta)
        {
            this.Eliminar(anteriores.Carta);
        }

        this.DescartarCopias(solicitud.RutaCv, solicitud.RutaCarta);
    }

    /// <summary>
    /// Al descartar la edición: borra los adjuntos elegidos sin guardar y las copias
    /// intermedias; los que ya estaban guardados se quedan.
    /// </summary>
    public void DescartarNoGuardadas(Solicitud solicitud, (string? Cv, string? Carta) guardadas)
    {
        if (solicitud.RutaCv != guardadas.Cv)
        {
            this.Eliminar(solicitud.RutaCv);
        }

        if (solicitud.RutaCarta != guardadas.Carta)
        {
            this.Eliminar(solicitud.RutaCarta);
        }

        this.DescartarCopias();
    }

    /// <summary>
    /// Borra las copias hechas durante la edición salvo las que se quieren conservar.
    /// Sin argumentos las borra todas: es lo que toca al cancelar o al eliminar la fila.
    /// </summary>
    public void DescartarCopias(string? conservarCv = null, string? conservarCarta = null)
    {
        foreach (string ruta in this.copias.Where(copia => copia != conservarCv && copia != conservarCarta))
        {
            this.Eliminar(ruta);
        }

        this.copias.Clear();
    }
}
