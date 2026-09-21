namespace GestorSolicitudes.Views;

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Markup;

// Code-behind de la ventana de requisitos: no hay UI automatizable de la app,
// así que se excluye de la cobertura.
[ExcludeFromCodeCoverage]
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