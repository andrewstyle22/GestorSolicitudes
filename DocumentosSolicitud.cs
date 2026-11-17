namespace GestorSolicitudes.Models;

/// <summary>
/// CV y carta de presentación de una candidatura, guardados en la base de datos.
/// La fila existe como mucho una vez por candidatura y se lee solo al abrir un documento.
/// </summary>
public class DocumentosSolicitud
{
    private DocumentosSolicitud()
    {
    }

    public DocumentosSolicitud(int solicitudId)
    {
        this.SolicitudId = solicitudId;
    }

    public int SolicitudId { get; private set; }

    public byte[]? Cv { get; private set; }

    public byte[]? Carta { get; private set; }

    public void AsignarCv(byte[] contenido) => this.Cv = contenido;

    public void QuitarCv() => this.Cv = null;

    public void AsignarCarta(byte[] contenido) => this.Carta = contenido;

    public void QuitarCarta() => this.Carta = null;
}
