namespace GestorSolicitudes.Tests;

using GestorSolicitudes.ViewModels;

/// <summary>
/// IDialogoService de mentira: registra lo que el ViewModel pide y devuelve lo que se le
/// configure, para probar los comandos sin abrir ninguna ventana.
/// </summary>
internal sealed class DialogoFalso : IDialogoService
{
    public List<string> Avisos { get; } = [];

    public List<string> Informes { get; } = [];

    public List<string> Errores { get; } = [];

    public List<string> Confirmaciones { get; } = [];

    public List<string> EnlacesAbiertos { get; } = [];

    public List<string> AdjuntosAbiertos { get; } = [];

    public bool RespuestaConfirmar { get; set; }

    public string? FicheroElegido { get; set; }

    public string? DestinoElegido { get; set; }

    public bool AbrirEnlaceLanza { get; set; }

    public bool VerAdjuntoLanza { get; set; }

    public bool Confirmar(string mensaje, string titulo)
    {
        this.Confirmaciones.Add(mensaje);
        return this.RespuestaConfirmar;
    }

    public void Avisar(string mensaje, string titulo) => this.Avisos.Add(mensaje);

    public void Informar(string mensaje, string titulo) => this.Informes.Add(mensaje);

    public void Error(string mensaje, string titulo) => this.Errores.Add(mensaje);

    public string? ElegirFichero(string titulo, string filtro) => this.FicheroElegido;

    public string? ElegirDestino(string titulo, string filtro, string nombrePropuesto) => this.DestinoElegido;

    public void AbrirEnlace(string url)
    {
        this.EnlacesAbiertos.Add(url);
        if (this.AbrirEnlaceLanza)
        {
            throw new InvalidOperationException("no se pudo abrir");
        }
    }

    public void VerAdjunto(string ruta)
    {
        this.AdjuntosAbiertos.Add(ruta);
        if (this.VerAdjuntoLanza)
        {
            throw new InvalidOperationException("no se pudo abrir");
        }
    }
}
