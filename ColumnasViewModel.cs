namespace GestorSolicitudes.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GestorSolicitudes.Helpers;

/// <summary>
/// Preferencia de columnas visibles de la tabla: las seis ocultables, su estado y el
/// guardado en disco. El DataGridColumn no hereda el DataContext y no se puede enlazar,
/// así que la vista se suscribe al evento para aplicar el cambio.
/// </summary>
public sealed partial class ColumnasViewModel : ObservableObject
{
    /// <summary>
    /// Columnas de la tabla que el usuario puede ocultar. Empresa, Puesto, Estado y
    /// Enviada no entran aquí: se ven siempre.
    /// </summary>
    internal static readonly (string Columna, string Clave)[] ColumnasOcultables =
    [
        ("Dias", "Columna.Dias"),
        ("PrimeraRespuesta", "Columna.PrimeraRespuesta"),
        ("Entrevista", "Columna.Entrevista"),
        ("Seguimiento", "Columna.Seguimiento"),
        ("Interes", "Columna.Interes"),
        ("Portal", "Columna.Portal"),
    ];

    private readonly string ruta;

    /// <summary>Sin ruta, usa la preferencia real de %APPDATA%; los tests pasan una temporal.</summary>
    public ColumnasViewModel(string? ruta = null)
    {
        this.ruta = ruta ?? ColumnasHelper.Ruta;

        HashSet<string>? guardadas = ColumnasHelper.CargarDe(this.ruta);

        foreach ((string columna, string clave) in ColumnasOcultables)
        {
            this.Columnas.Add(new ColumnaItem(
                columna,
                clave,
                guardadas is null || guardadas.Contains(columna),
                this.Guardar));
        }
    }

    public ObservableCollection<ColumnaItem> Columnas { get; } = new();

    /// <summary>Abre y cierra la lista de checkboxes del botón "Columnas".</summary>
    [ObservableProperty]
    private bool panelColumnasAbierto;

    /// <summary>La vista se suscribe para ocultar las columnas del DataGrid.</summary>
    public event Action? ColumnaVisibilidadCambiada;

    /// <summary>Relee los nombres visibles tras un cambio de idioma.</summary>
    public void RefrescarNombres()
    {
        foreach (ColumnaItem columna in this.Columnas)
        {
            columna.RefrescarNombre();
        }
    }

    /// <summary>Guarda la preferencia y avisa a la vista para que aplique el nuevo estado.</summary>
    private void Guardar()
    {
        ColumnasHelper.GuardarEn(
            this.ruta,
            this.Columnas.Where(c => c.Visible).Select(c => c.Columna));

        this.ColumnaVisibilidadCambiada?.Invoke();
    }
}
