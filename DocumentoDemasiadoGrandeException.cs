namespace GestorSolicitudes.Helpers;

/// <summary>El fichero elegido como adjunto supera el límite de tamaño permitido.</summary>
public sealed class DocumentoDemasiadoGrandeException : Exception
{
    public DocumentoDemasiadoGrandeException()
        : base("El documento supera el tamaño máximo permitido.")
    {
    }
}
