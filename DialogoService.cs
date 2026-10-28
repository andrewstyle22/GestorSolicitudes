namespace GestorSolicitudes.Views;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using GestorSolicitudes.Helpers;
using GestorSolicitudes.ViewModels;
using Microsoft.Win32;

/// <summary>
/// Implementación WPF de <see cref="IDialogoService"/>: concentra los MessageBox y los diálogos
/// de fichero de los ViewModels y de las ventanas de la app. El aviso de fallo de arranque de
/// <see cref="App"/> sigue mostrándose directamente porque todavía no hay servicio creado.
/// </summary>
[ExcludeFromCodeCoverage] // Diálogos WPF: no hay UI automatizable.
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
            DefaultExt = Path.GetExtension(nombrePropuesto),
        };

        return dialogo.ShowDialog() == true ? dialogo.FileName : null;
    }

    public void AbrirEnlace(string url)
    {
        if (!Enlaces.EsWeb(url))
        {
            return;
        }

        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
