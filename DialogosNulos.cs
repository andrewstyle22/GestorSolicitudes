namespace GestorSolicitudes.ViewModels;

/// <summary>
/// Implementación que no muestra nada. Es el valor por defecto del constructor de MainViewModel:
/// los tests que no comprueban diálogos no tienen que montar un falso, y si algún día se
/// construyera sin servicio, la app calla en vez de reventar.
/// </summary>
internal sealed class DialogosNulos : IDialogoService
{
    public DialogosNulos() =>
        System.Diagnostics.Debug.WriteLine("MainViewModel sin IDialogoService: los avisos no se mostrarán.");

    public bool Confirmar(string mensaje, string titulo) => false;

    public void Avisar(string mensaje, string titulo)
    {
    }

    public void Informar(string mensaje, string titulo)
    {
    }

    public void Error(string mensaje, string titulo)
    {
    }

    public string? ElegirFichero(string titulo, string filtro) => null;

    public string? ElegirDestino(string titulo, string filtro, string nombrePropuesto) => null;

    public void AbrirEnlace(string url)
    {
    }
}
