namespace GestorSolicitudes.Views;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

/// <summary>
/// Fila de un adjunto (CV o carta): etiqueta, nombre del fichero y los botones de
/// adjuntar, quitar y abrir. Los dos bloques del panel de detalle eran la misma fila.
/// </summary>
public partial class FilaAdjunto : UserControl
{
    public static readonly DependencyProperty EtiquetaProperty = DependencyProperty.Register(
        nameof(Etiqueta), typeof(string), typeof(FilaAdjunto), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty TextoProperty = DependencyProperty.Register(
        nameof(Texto), typeof(string), typeof(FilaAdjunto), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty RutaProperty = DependencyProperty.Register(
        nameof(Ruta), typeof(string), typeof(FilaAdjunto), new PropertyMetadata(null));

    public static readonly DependencyProperty ComandoAdjuntarProperty = DependencyProperty.Register(
        nameof(ComandoAdjuntar), typeof(ICommand), typeof(FilaAdjunto), new PropertyMetadata(null));

    public static readonly DependencyProperty ComandoQuitarProperty = DependencyProperty.Register(
        nameof(ComandoQuitar), typeof(ICommand), typeof(FilaAdjunto), new PropertyMetadata(null));

    public FilaAdjunto() => this.InitializeComponent();

    /// <summary>El usuario quiere ver el fichero; la ventana decide con qué visor.</summary>
    public event EventHandler? Abrir;

    public string Etiqueta
    {
        get => (string)this.GetValue(EtiquetaProperty);
        set => this.SetValue(EtiquetaProperty, value);
    }

    public string Texto
    {
        get => (string)this.GetValue(TextoProperty);
        set => this.SetValue(TextoProperty, value);
    }

    public string? Ruta
    {
        get => (string?)this.GetValue(RutaProperty);
        set => this.SetValue(RutaProperty, value);
    }

    public ICommand? ComandoAdjuntar
    {
        get => (ICommand?)this.GetValue(ComandoAdjuntarProperty);
        set => this.SetValue(ComandoAdjuntarProperty, value);
    }

    public ICommand? ComandoQuitar
    {
        get => (ICommand?)this.GetValue(ComandoQuitarProperty);
        set => this.SetValue(ComandoQuitarProperty, value);
    }

    private void Abrir_Click(object sender, RoutedEventArgs e) => this.Abrir?.Invoke(this, EventArgs.Empty);
}
