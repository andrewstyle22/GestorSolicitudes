namespace GestorSolicitudes.Views;

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using GestorSolicitudes.Models;
using GestorSolicitudes.ViewModels;
using Hardcodet.Wpf.TaskbarNotification;

// Code-behind de la ventana: cableado de eventos, bandeja y temporizadores WPF.
// No hay UI automatizable de la app, así que se excluye de la cobertura.
[ExcludeFromCodeCoverage]
public partial class MainWindow : Window
{
    private readonly MainViewModel vm;
    private readonly HashSet<int> idsVencidosAvisados = new();
    private DispatcherTimer? temporizadorRecordatorios;

    public MainWindow()
    {
        this.InitializeComponent();

        this.vm = new MainViewModel();
        this.DataContext = this.vm;

        // Ctrl+N nueva candidatura, Ctrl+S guardar.
        this.InputBindings.Add(new KeyBinding(this.vm.NuevaCommand, Key.N, ModifierKeys.Control));
        this.InputBindings.Add(new KeyBinding(this.vm.GuardarCommand, Key.S, ModifierKeys.Control));

        this.IconoBandeja.Icon = System.Drawing.SystemIcons.Application;
        this.ArrancarRecordatorios();

        // Un DataGridColumn no hereda el DataContext, así que su visibilidad no se puede
        // enlazar: la fija la vista al construirse y cada vez que cambia una casilla.
        this.vm.ColumnaVisibilidadCambiada += this.AplicarVisibilidadColumnas;
        this.AplicarVisibilidadColumnas();
    }

    /// <summary>Muestra u oculta las columnas que el usuario puede decidir.</summary>
    private void AplicarVisibilidadColumnas()
    {
        foreach (ColumnaItem columna in this.vm.Columnas)
        {
            // El x:Name de cada columna genera el campo con el que se llega desde aquí.
            DataGridColumn? cd = columna.Columna switch
            {
                "Dias" => this.Dias,
                "PrimeraRespuesta" => this.PrimeraRespuesta,
                "Entrevista" => this.Entrevista,
                "Seguimiento" => this.Seguimiento,
                "Interes" => this.Interes,
                "Portal" => this.Portal,
                _ => null,
            };

            if (cd is not null)
            {
                cd.Visibility = columna.Visible ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    // ------------------------------------------------------------ Recordatorios

    /// <summary>
    /// Mientras la app está abierta, revisa cada 5 minutos si hay seguimientos
    /// vencidos y avisa con un globo del icono de la bandeja del sistema.
    /// </summary>
    private void ArrancarRecordatorios()
    {
        this.temporizadorRecordatorios = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
        this.temporizadorRecordatorios.Tick += (_, _) =>
        {
            this.temporizadorRecordatorios.Interval = TimeSpan.FromMinutes(5);
            this.ComprobarSeguimientosVencidos();
        };
        this.temporizadorRecordatorios.Start();
    }

    private void ComprobarSeguimientosVencidos()
    {
        List<Solicitud> vencidos = this.vm.SeguimientosVencidosAhora();
        List<Solicitud> nuevos = vencidos
            .Where(s => this.idsVencidosAvisados.Add(s.Id))
            .ToList();

        if (nuevos.Count == 0)
        {
            return;
        }

        if (nuevos.Count == 1)
        {
            Solicitud s = nuevos[0];
            this.IconoBandeja.ShowBalloonTip(
                Localizacion.Texto("Bandeja.SeguimientoVencido"),
                string.Format(
                    Localizacion.Texto("Bandeja.VencidoTexto"),
                    s.Empresa,
                    s.Puesto,
                    s.ProximoSeguimiento?.ToString("dd/MM/yyyy")),
                BalloonIcon.Info);
        }
        else
        {
            this.IconoBandeja.ShowBalloonTip(
                string.Format(Localizacion.Texto("Bandeja.VencidosMultiples"), nuevos.Count),
                string.Join("\n", nuevos.Take(4).Select(s => string.Format(
                    Localizacion.Texto("Bandeja.RecordatorioLinea"), s.Empresa, s.Puesto))),
                BalloonIcon.Warning);
        }

        // Refresca también el contador y el filtro de la cabecera.
        this.vm.Recargar();
    }

    // ------------------------------------------------------------ Bandeja
    private void IconoBandeja_DobleClic(object? sender, RoutedEventArgs e) => this.MostrarVentana();

    private void MenuAbrir_Click(object? sender, RoutedEventArgs e) => this.MostrarVentana();

    private void MenuComprobar_Click(object? sender, RoutedEventArgs e) => this.ComprobarSeguimientosVencidos();

    private void MenuSalir_Click(object? sender, RoutedEventArgs e)
    {
        this.IconoBandeja.Visibility = Visibility.Hidden;
        this.Close();
    }

    private void MostrarVentana()
    {
        this.Show();
        if (this.WindowState == WindowState.Minimized)
        {
            this.WindowState = WindowState.Normal;
        }

        this.Activate();
    }

    // ------------------------------------------------------------ Detalle

    /// <summary>Muestra los requisitos del cargo en una ventana grande si hay texto.</summary>
    private void VerRequisitos_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: Solicitud solicitud }
            && !string.IsNullOrWhiteSpace(solicitud.Requisitos))
        {
            var ventana = new VentanaRequisitos(solicitud.Requisitos) { Owner = this };
            ventana.ShowDialog();
        }
    }

    protected override void OnClosed(System.EventArgs e)
    {
        this.IconoBandeja.Visibility = Visibility.Hidden;
        base.OnClosed(e);
    }
}