using System.Windows;

namespace GestorSolicitudes.Views;

public partial class VentanaRequisitos : Window
{
    public VentanaRequisitos(string texto)
    {
        InitializeComponent();
        TextoRequisitos.Text = texto;
        Owner = Application.Current.MainWindow;
    }

    private void Cerrar_Click(object? sender, RoutedEventArgs e) => Close();
}