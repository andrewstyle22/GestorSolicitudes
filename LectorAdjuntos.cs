namespace GestorSolicitudes.Helpers;

using System.IO;

/// <summary>Lee un adjunto a memoria respetando el límite de tamaño de <see cref="AdjuntosHelper"/>.</summary>
public static class LectorAdjuntos
{
    /// <summary>
    /// Devuelve el contenido del fichero. Lanza <see cref="DocumentoDemasiadoGrandeException"/>
    /// si supera el límite; así no se llega a reservar memoria para un fichero enorme.
    /// </summary>
    public static byte[] Leer(string ruta)
    {
        using var fichero = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (fichero.Length > AdjuntosHelper.TamanoMaximoBytes)
        {
            throw new DocumentoDemasiadoGrandeException();
        }

        byte[] contenido = new byte[fichero.Length];
        fichero.ReadExactly(contenido);
        return contenido;
    }
}
