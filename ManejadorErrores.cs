namespace GestorSolicitudes;

using System.Globalization;
using GestorSolicitudes.Helpers;
using GestorSolicitudes.ViewModels;

/// <summary>
/// Respuesta a una excepción no controlada: deja el detalle en el registro y avisa al usuario
/// con la ruta del fichero. Vive en el Núcleo porque combina el registro (Helpers) con el
/// diálogo de error (IDialogoService), igual que <see cref="App"/>.
/// </summary>
internal static class ManejadorErrores
{
    /// <summary>Registra la excepción y muestra el aviso; no cierra la aplicación.</summary>
    public static void Avisar(Exception excepcion, string contexto, string ruta, IDialogoService dialogo)
    {
        RegistroErrores.RegistrarEn(ruta, excepcion, contexto);
        dialogo.Error(
            string.Format(
                CultureInfo.CurrentCulture,
                Localizacion.Texto("Mensaje.ErrorInesperado"),
                excepcion.Message,
                ruta),
            Localizacion.Texto("Titulo.ErrorInesperado"));
    }
}
