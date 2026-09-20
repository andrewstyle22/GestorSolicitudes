namespace GestorSolicitudes.Views;

using System.Windows;
using System.Windows.Markup;

public partial class VentanaRequisitos : Window
{
    public VentanaRequisitos(string texto)
    {
        this.InitializeComponent();
        this.Language = XmlLanguage.GetLanguage(Localizacion.CulturaActual.IetfLanguageTag);
        this.TextoRequisitos.Text = texto;
        this.Owner = Application.Current.MainWindow;
    }

    private void Cerrar_Click(object? sender, RoutedEventArgs e) => this.Close();
}