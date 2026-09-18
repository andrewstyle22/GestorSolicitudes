using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GestorSolicitudes.Models;
using GestorSolicitudes.ViewModels;
using Hardcodet.Wpf.TaskbarNotification;

namespace GestorSolicitudes.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly HashSet<int> _idsVencidosAvisados = new();
    private DispatcherTimer? _temporizadorRecordatorios;

    public MainWindow()
    {
        InitializeComponent();

        _vm = new MainViewModel();
        DataContext = _vm;

        // Ctrl+N nueva candidatura, Ctrl+S guardar.
        InputBindings.Add(new KeyBinding(_vm.NuevaCommand, Key.N, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(_vm.GuardarCommand, Key.S, ModifierKeys.Control));

        IconoBandeja.Icon = System.Drawing.SystemIcons.Application;
        ArrancarRecordatorios();
    }

    // ------------------------------------------------------------ Recordatorios

    /// <summary>
    /// Mientras la app está abierta, revisa cada 5 minutos si hay seguimientos
    /// vencidos y avisa con un globo del icono de la bandeja del sistema.
    /// </summary>
    private void ArrancarRecordatorios()
    {
        var primera = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
        primera.Tick += (_, _) =>
        {
            primera.Stop();
            ComprobarSeguimientosVencidos();
        };
        primera.Start();

        _temporizadorRecordatorios = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
        _temporizadorRecordatorios.Tick += (_, _) => ComprobarSeguimientosVencidos();
        _temporizadorRecordatorios.Start();
    }

    private void ComprobarSeguimientosVencidos()
    {
        List<Solicitud> vencidos = _vm.SeguimientosVencidosAhora();
        List<Solicitud> nuevos = vencidos
            .Where(s => _idsVencidosAvisados.Add(s.Id))
            .ToList();

        if (nuevos.Count == 0) return;

        if (nuevos.Count == 1)
        {
            Solicitud s = nuevos[0];
            IconoBandeja.ShowBalloonTip(
                "Seguimiento vencido",
                $"{s.Empresa} — {s.Puesto}. Pendiente desde el {s.ProximoSeguimiento:dd/MM/yyyy}.",
                BalloonIcon.Info);
        }
        else
        {
            IconoBandeja.ShowBalloonTip(
                $"{nuevos.Count} seguimientos vencidos",
                string.Join("\n", nuevos.Take(4).Select(s => $"• {s.Empresa} — {s.Puesto}")),
                BalloonIcon.Warning);
        }

        // Refresca también el contador y el filtro de la cabecera.
        _vm.Recargar();
    }

    // ------------------------------------------------------------ Bandeja

    private void IconoBandeja_DobleClic(object? sender, RoutedEventArgs e) => MostrarVentana();

    private void MenuAbrir_Click(object? sender, RoutedEventArgs e) => MostrarVentana();

    private void MenuComprobar_Click(object? sender, RoutedEventArgs e) => ComprobarSeguimientosVencidos();

    private void MenuSalir_Click(object? sender, RoutedEventArgs e)
    {
        IconoBandeja.Visibility = Visibility.Hidden;
        Close();
    }

    private void MostrarVentana()
    {
        Show();
        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;
        Activate();
    }

    protected override void OnClosed(System.EventArgs e)
    {
        IconoBandeja.Visibility = Visibility.Hidden;
        base.OnClosed(e);
    }
}