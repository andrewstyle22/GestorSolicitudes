namespace GestorSolicitudes.Views;

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using GestorSolicitudes.ViewModels;
using Hardcodet.Wpf.TaskbarNotification;

// Code-behind de la ventana: cableado de eventos, bandeja y temporizadores WPF.
// No hay UI automatizable de la app, así que se excluye de la cobertura.
[ExcludeFromCodeCoverage]
public partial class MainWindow : Window
{
    private const int SegundosAntesDelPrimerAviso = 20;
    private const int MinutosEntreAvisos = 5;

    private readonly MainViewModel vm;
    private readonly HashSet<int> idsVencidosAvisados = [];
    private readonly Stream? flujoIcono;
    private DispatcherTimer? temporizadorRecordatorios;

    // Ruta absoluta al recurso empaquetado. BitmapFrame.Create no resuelve la forma relativa
    // ("/Ensamblado;component/..."): la interpreta como ruta de fichero. El nombre del
    // ensamblado se toma del propio tipo, así que renombrar el proyecto no rompe la ruta.
    internal static readonly Uri RutaIcono = new(
        $"pack://application:,,,/{typeof(MainWindow).Assembly.GetName().Name};component/Resources/appicon.ico",
        UriKind.Absolute);

    public MainWindow(MainViewModel vm)
    {
        this.InitializeComponent();

        this.vm = vm;
        this.DataContext = this.vm;

        // El icono de la ventana sale de la misma ruta que el de la bandeja: una sola fuente.
        this.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(RutaIcono);

        // Ctrl+N nueva candidatura, Ctrl+S guardar.
        this.InputBindings.Add(new KeyBinding(this.vm.NuevaCommand, Key.N, ModifierKeys.Control));
        this.InputBindings.Add(new KeyBinding(this.vm.GuardarCommand, Key.S, ModifierKeys.Control));

        this.flujoIcono = Application.GetResourceStream(RutaIcono)?.Stream;

        // Icon(Stream) no copia los bytes: el flujo tiene que vivir con el icono, de ahí
        // el campo. Si el recurso faltara, al icono genérico de Windows.
        this.IconoBandeja.Icon = this.flujoIcono is null
            ? System.Drawing.SystemIcons.Application
            : new System.Drawing.Icon(this.flujoIcono);

        this.ArrancarRecordatorios();

        // Un DataGridColumn no hereda el DataContext, así que su visibilidad no se puede
        // enlazar: la fija la vista al construirse y cada vez que cambia una casilla.
        this.vm.ColumnasVm.ColumnaVisibilidadCambiada += this.AplicarVisibilidadColumnas;
        this.AplicarVisibilidadColumnas();
    }

    /// <summary>Muestra u oculta las columnas que el usuario puede decidir.</summary>
    private void AplicarVisibilidadColumnas()
    {
        foreach (ColumnaItem columna in this.vm.ColumnasVm.Columnas)
        {
            // El x:Name de cada columna genera el campo con el que se llega desde aquí.
            DataGridColumn columnaVista = columna.Columna switch
            {
                "Dias" => this.Dias,
                "PrimeraRespuesta" => this.PrimeraRespuesta,
                "Entrevista" => this.Entrevista,
                "Seguimiento" => this.Seguimiento,
                "Interes" => this.Interes,
                "Portal" => this.Portal,
                _ => throw new InvalidOperationException($"Columna desconocida en la visibilidad: {columna.Columna}"),
            };

            columnaVista.Visibility = columna.Visible ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    // ------------------------------------------------------------ Recordatorios

    /// <summary>
    /// Mientras la app está abierta, revisa cada 5 minutos si hay seguimientos
    /// vencidos y avisa con un globo del icono de la bandeja del sistema.
    /// </summary>
    private void ArrancarRecordatorios()
    {
        this.temporizadorRecordatorios = new DispatcherTimer { Interval = TimeSpan.FromSeconds(SegundosAntesDelPrimerAviso) };
        this.temporizadorRecordatorios.Tick += (_, _) =>
        {
            this.temporizadorRecordatorios.Interval = TimeSpan.FromMinutes(MinutosEntreAvisos);
            this.ComprobarSeguimientosVencidos();
        };
        this.temporizadorRecordatorios.Start();
    }

    private void ComprobarSeguimientosVencidos()
    {
        List<SeguimientoVencido> vencidos = this.vm.SeguimientosVencidosAhora();

        // El aviso es una vez por sesión, pero solo para los que siguen vencidos: así el
        // HashSet no crece con filas que ya se resolvieron o se borraron.
        this.idsVencidosAvisados.IntersectWith(vencidos.Select(s => s.Id));

        // Add devuelve false si ya estaba avisado: filtra y marca en la misma pasada.
        var nuevos = vencidos.Where(vencido => this.idsVencidosAvisados.Add(vencido.Id)).ToList();

        if (nuevos.Count == 0)
        {
            return;
        }

        if (nuevos.Count == 1)
        {
            SeguimientoVencido s = nuevos[0];
            this.IconoBandeja.ShowBalloonTip(
                Localizacion.Texto("Bandeja.SeguimientoVencido"),
                string.Format(
                    Localizacion.Texto("Bandeja.VencidoTexto"),
                    s.Empresa,
                    s.Puesto,
                    s.ProximoSeguimiento?.ToString("d", Localizacion.CulturaActual)),
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

    protected override void OnClosed(System.EventArgs e)
    {
        this.IconoBandeja.Visibility = Visibility.Hidden;
        this.temporizadorRecordatorios?.Stop();

        // La base y el ViewModel los libera App; aquí solo los recursos propios de la ventana.
        this.IconoBandeja.Dispose();
        this.flujoIcono?.Dispose();

        base.OnClosed(e);
    }
}
