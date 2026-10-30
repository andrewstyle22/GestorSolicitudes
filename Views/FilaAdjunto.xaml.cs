namespace GestorSolicitudes.Views;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GestorSolicitudes.ViewModels;

/// <summary>
/// Fila de un adjunto (CV o carta): etiqueta, nombre del fichero y los botones de
/// adjuntar, quitar y abrir. Los dos bloques del panel de detalle eran la misma fila.
/// </summary>
public partial class FilaAdjunto : UserControl
{
    public static readonly DependencyProperty EtiquetaProperty = DependencyProperty.Register(
        nameof(Etiqueta), typeof(string), typeof(FilaAdjunto), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty AdjuntoProperty = DependencyProperty.Register(
        nameof(Adjunto), typeof(Adjunto), typeof(FilaAdjunto), new PropertyMetadata(null));

    public static readonly DependencyProperty ComandoAdjuntarProperty = DependencyProperty.Register(
        nameof(ComandoAdjuntar), typeof(ICommand), typeof(FilaAdjunto), new PropertyMetadata(null));

    public static readonly DependencyProperty ComandoQuitarProperty = DependencyProperty.Register(
        nameof(ComandoQuitar), typeof(ICommand), typeof(FilaAdjunto), new PropertyMetadata(null));

    public static readonly DependencyProperty ComandoAbrirProperty = DependencyProperty.Register(
        nameof(ComandoAbrir), typeof(ICommand), typeof(FilaAdjunto), new PropertyMetadata(null));

    public FilaAdjunto() => this.InitializeComponent();

    public string Etiqueta
    {
        get => (string)this.GetValue(EtiquetaProperty);
        set => this.SetValue(EtiquetaProperty, value);
    }

    /// <summary>Ruta + nombre del fichero; con null la fila enseña el texto de "sin adjuntar".</summary>
    public Adjunto? Adjunto
    {
        get => (Adjunto?)this.GetValue(AdjuntoProperty);
        set => this.SetValue(AdjuntoProperty, value);
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

    /// <summary>Recibe el <see cref="Adjunto"/> de la fila como parámetro.</summary>
    public ICommand? ComandoAbrir
    {
        get => (ICommand?)this.GetValue(ComandoAbrirProperty);
        set => this.SetValue(ComandoAbrirProperty, value);
    }
}
