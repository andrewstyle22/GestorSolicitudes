namespace GestorSolicitudes.Views;

using System.Diagnostics;
using System.Windows;
using GestorSolicitudes.ViewModels;
using Microsoft.Win32;

/// <summary>
/// Implementación WPF de <see cref="IDialogoService"/>: el único sitio del programa que muestra
/// un MessageBox o abre un diálogo de fichero. Aísla a los ViewModels de WPF y hace testables
/// sus comandos (los tests inyectan un falso en vez de esta clase).
/// </summary>
internal sealed class DialogoService : IDialogoService
{
    public bool Confirmar(string mensaje, string titulo) =>
        MessageBox.Show(mensaje, titulo, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public void Avisar(string mensaje, string titulo) =>
        MessageBox.Show(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Warning);

    public void Informar(string mensaje, string titulo) =>
        MessageBox.Show(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Information);

    public void Error(string mensaje, string titulo) =>
        MessageBox.Show(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Error);

    public string? ElegirFichero(string titulo, string filtro)
    {
        var dialogo = new OpenFileDialog
        {
            Title = titulo,
            Filter = filtro,
        };

        return dialogo.ShowDialog() == true ? dialogo.FileName : null;
    }

    public string? ElegirDestino(string titulo, string filtro, string nombrePropuesto)
    {
        var dialogo = new SaveFileDialog
        {
            Title = titulo,
            Filter = filtro,
            FileName = nombrePropuesto,
        };

        return dialogo.ShowDialog() == true ? dialogo.FileName : null;
    }

    public void AbrirEnlace(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}
