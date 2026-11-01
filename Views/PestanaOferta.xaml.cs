namespace GestorSolicitudes.Views;

using System.Windows;
using System.Windows.Controls;
using GestorSolicitudes.ViewModels;

/// <summary>Pestaña "Oferta" del panel de detalle.</summary>
public partial class PestanaOferta : UserControl
{
    public PestanaOferta() => this.InitializeComponent();

    /// <summary>Muestra los requisitos del cargo en una ventana grande si hay texto.</summary>
    private void VerRequisitos_Click(object sender, RoutedEventArgs e)
    {
        if (this.DataContext is MainViewModel { Editor: SolicitudEdicionViewModel editor }
            && !string.IsNullOrWhiteSpace(editor.Requisitos))
        {
            var ventana = new VentanaRequisitos(editor.Requisitos) { Owner = Window.GetWindow(this) };
            ventana.ShowDialog();
        }
    }
}
