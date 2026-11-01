namespace GestorSolicitudes.Tests;

using GestorSolicitudes.ViewModels;

internal static class ViewModelTestExtensions
{
    /// <summary>Rellena empresa y puesto del panel abierto, el par que casi todo test necesita.</summary>
    internal static void RellenarObligatorios(this MainViewModel vm, string empresa, string puesto)
    {
        vm.Editor!.Empresa = empresa;
        vm.Editor.Puesto = puesto;
    }
}
