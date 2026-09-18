using System.Windows;
using System.Windows.Input;
using GestorSolicitudes.ViewModels;

namespace GestorSolicitudes.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();

        _vm = new MainViewModel();
        DataContext = _vm;

        // Ctrl+N nueva candidatura, Ctrl+S guardar.
        InputBindings.Add(new KeyBinding(_vm.NuevaCommand, Key.N, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(_vm.GuardarCommand, Key.S, ModifierKeys.Control));
    }
}