namespace GestorSolicitudes.Views;

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using GestorSolicitudes.Helpers;

// Ventana modal para elegir el separador del CSV: tres opciones y la última elección marcada.
[ExcludeFromCodeCoverage] // Code-behind de diálogo: la UI no es automatizable.
public partial class VentanaSeparadorCsv : Window
{
    public VentanaSeparadorCsv(char actual)
    {
        this.InitializeComponent();

        char inicial = PreferenciaCsv.EsValido(actual) ? actual : PreferenciaCsv.PorDefecto;
        this.PuntoYComa.IsChecked = inicial == ';';
        this.Coma.IsChecked = inicial == ',';
        this.Tabulador.IsChecked = inicial == '\t';
    }

    /// <summary>Separador marcado al aceptar; null mientras no se acepte.</summary>
    public char? SeparadorElegido { get; private set; }

    private void Aceptar_Click(object sender, RoutedEventArgs e)
    {
        if (this.Coma.IsChecked == true)
        {
            this.SeparadorElegido = ',';
        }
        else if (this.Tabulador.IsChecked == true)
        {
            this.SeparadorElegido = '\t';
        }
        else
        {
            this.SeparadorElegido = ';';
        }

        this.DialogResult = true;
    }
}
