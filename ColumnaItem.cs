namespace GestorSolicitudes.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// Una columna de la tabla que el usuario puede ocultar. <see cref="Columna"/> es a la vez
/// el nombre del DataGridColumn en el XAML y la clave con la que se guarda la preferencia,
/// así que el nombre visible sale del diccionario de textos (Columna.Dias, Columna.Portal...).
/// </summary>
public partial class ColumnaItem : ObservableObject
{
    private readonly Action alCambiar;
    private bool visible;

    public ColumnaItem(string columna, bool visible, Action alCambiar)
    {
        this.Columna = columna;
        this.visible = visible;
        this.alCambiar = alCambiar;
    }

    /// <summary>Nombre del DataGridColumn en el XAML, que es también la clave guardada.</summary>
    public string Columna { get; }

    public string Nombre => Localizacion.Texto("Columna." + this.Columna);

    public bool Visible
    {
        get => this.visible;
        set
        {
            if (this.SetProperty(ref this.visible, value))
            {
                this.alCambiar();
            }
        }
    }

    /// <summary>Reevalúa el nombre visible tras un cambio de idioma.</summary>
    public void RefrescarNombre() => this.OnPropertyChanged(nameof(this.Nombre));
}
