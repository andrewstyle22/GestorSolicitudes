namespace GestorSolicitudes.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// Una columna de la tabla que el usuario puede ocultar. <see cref="Columna"/> es el nombre
/// del DataGridColumn en el XAML y la clave con la que se guarda la preferencia;
/// <see cref="Clave"/> es la clave de texto que se muestra, explícita para no construirla
/// concatenando cadenas.
/// </summary>
public partial class ColumnaItem : ObservableObject
{
    private readonly Action alCambiar;
    private bool visible;

    public ColumnaItem(string columna, string clave, bool visible, Action alCambiar)
    {
        this.Columna = columna;
        this.Clave = clave;
        this.visible = visible;
        this.alCambiar = alCambiar;
    }

    /// <summary>Nombre del DataGridColumn en el XAML, que es también la clave guardada.</summary>
    public string Columna { get; }

    /// <summary>Clave de traducción del nombre visible.</summary>
    public string Clave { get; }

    public string Nombre => Localizacion.Texto(this.Clave);

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
