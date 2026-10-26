namespace GestorSolicitudes.Tests;

using GestorSolicitudes.ViewModels;

/// <summary>
/// IDialogoService de mentira: registra lo que el ViewModel pide y devuelve lo que se le
/// configure, para probar los comandos sin abrir ninguna ventana.
/// </summary>
internal sealed class DialogoFalso : IDialogoService
{
    public List<string> Avisos { get; } = new();

    public List<string> Informes { get; } = new();

    public List<string> Errores { get; } = new();

    public List<string> Confirmaciones { get; } = new();

    public List<string> EnlacesAbiertos { get; } = new();

    public bool RespuestaConfirmar { get; set; }

    public string? FicheroElegido { get; set; }

    public string? DestinoElegido { get; set; }

    public bool AbrirEnlaceLanza { get; set; }

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
}
