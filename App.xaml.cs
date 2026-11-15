namespace GestorSolicitudes;

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using GestorSolicitudes.Data;
using GestorSolicitudes.Helpers;
using GestorSolicitudes.ViewModels;
using GestorSolicitudes.Views;

public partial class App : Application
{
    // La sesión completa vive aquí: la ventana solo recibe lo que necesita y no decide
    // qué base se abre ni cuándo se libera.
    private readonly DialogoService dialogo = new();
    private AppDbContext? db;
    private MainViewModel? viewModel;
    private InstanciaUnica? instancia;

    // Arranque de la app: ventanas, bandeja y MessageBox del fallo de base de datos.
    // Requiere el ciclo de vida completo de WPF y no se puede probar con los tests.
    [ExcludeFromCodeCoverage]
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Idioma de la interfaz: preferencia guardada (o el idioma del sistema la primera vez),
        // cultura de fechas/números e inyección de los textos como recursos de aplicación.
        Localizacion.Inicializar();

        // Los manejadores se instalan antes de tocar nada más para que ningún fallo
        // del arranque se quede sin registro ni aviso.
        this.RegistrarManejadoresDeErrores();

        // Una sola copia abierta: dos contextos sobre la misma base se pisan sin avisar.
        // La segunda avisa a la primera (que mostrará su ventana) y se cierra sin abrir nada.
        this.instancia = new InstanciaUnica();
        if (!this.instancia.EsPrimera)
        {
            this.instancia.AvisarALaPrimera();
            this.instancia.Dispose();
            this.instancia = null;
            this.Shutdown();
            return;
        }

        AppDbContext contexto = new();
        try
        {
            contexto.Database.EnsureCreated();

            // EnsureCreated no versiona el esquema: si la base ya existía sin las
            // columnas nuevas, las añadimos aquí con un ALTER TABLE barato.
            EsquemaDb.VerificarColumnasFaltantes(AppDbContext.RutaBaseDatos);

            // El motivo de cierre pasa a ser obligatorio en los cierres negativos:
            // las filas antiguas se normalizan antes de que el editor lo exija.
            EsquemaDb.NormalizarMotivosDeCierre(AppDbContext.RutaBaseDatos);
        }
        catch (Exception ex)
        {
            contexto.Dispose();
            MessageBox.Show(
                string.Format(Localizacion.Texto("Mensaje.ErrorBaseDatos"), ex.Message, AppDbContext.RutaBaseDatos),
                Localizacion.Texto("Titulo.ErrorAlIniciar"), MessageBoxButton.OK, MessageBoxImage.Error);
            this.Shutdown();
            return;
        }

        this.db = contexto;
        this.viewModel = new MainViewModel(contexto, dialogo: this.dialogo, cambiarIdioma: Localizacion.Cambiar);

        // Creamos la ventana aquí en lugar de con StartupUri: así el fallo de arriba
        // aborta de verdad el arranque y no dependemos de resolver una URI de recurso.
        // ShutdownMode="OnMainWindowClose" en App.xaml hace que cerrar la ventana
        // (con la X o con el menú "Salir" de la bandeja) termine la aplicación.
        var ventana = new MainWindow(this.viewModel);
        this.MainWindow = ventana;
        ventana.Show();
    }

    // Red de seguridad: un fallo en la interfaz se registra, se avisa con la ruta del
    // fichero y se marca como controlado para que la aplicación siga abierta.
    [ExcludeFromCodeCoverage]
    private void RegistrarManejadoresDeErrores()
    {
        this.DispatcherUnhandledException += (_, e) =>
        {
            ManejadorErrores.Avisar(e.Exception, "excepción no controlada en la interfaz", RegistroErrores.Ruta, this.dialogo);
            e.Handled = true;
        };

        // Sin interfaz que avisar: solo queda dejar rastro antes de que el proceso muera.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            RegistroErrores.Registrar((Exception)e.ExceptionObject, "excepción no controlada en segundo plano");

        TaskScheduler.UnobservedTaskException += (_, e) =>
            RegistroErrores.Registrar(e.Exception, "excepción no observada en una tarea");
    }

    // El ViewModel se suelta antes que la base: Dispose se desengancha del evento
    // estático de idioma antes de que el contexto deje de ser válido.
    [ExcludeFromCodeCoverage]
    protected override void OnExit(ExitEventArgs e)
    {
        this.viewModel?.Dispose();
        this.db?.Dispose();
        this.instancia?.Dispose();
        base.OnExit(e);
    }
}
