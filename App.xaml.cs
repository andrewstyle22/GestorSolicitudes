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

        // Un cierre abrupto deja los temporales de adjuntos en disco: los borra la copia
        // principal antes de abrir ningún documento; la segunda no toca los de la primera.
        TemporalesAdjuntos.LimpiarSiEsPrimera(this.instancia.EsPrimera);

        if (!this.instancia.EsPrimera)
        {
            this.instancia.AvisarALaPrimera();
            this.instancia.Dispose();
            this.instancia = null;
            this.Shutdown();
            return;
        }

        AppDbContext contexto = new();
        IReadOnlyList<string> pendientesPorTamano = [];
        try
        {
            contexto.Database.EnsureCreated();

            // EnsureCreated no versiona el esquema: si la base ya existía sin las
            // columnas nuevas, las añadimos aquí con un ALTER TABLE barato.
            EsquemaDb.VerificarColumnasFaltantes(AppDbContext.RutaBaseDatos);

            // El motivo de cierre pasa a ser obligatorio en los cierres negativos:
            // las filas antiguas se normalizan antes de que el editor lo exija.
            EsquemaDb.NormalizarMotivosDeCierre(AppDbContext.RutaBaseDatos);

            // Los adjuntos de versiones anteriores viven en disco: se copian a la base
            // antes de crear el ViewModel, que ya no los busca en ficheros.
            try
            {
                pendientesPorTamano = MigradorAdjuntos.Migrar(AppDbContext.RutaBaseDatos, RegistroErrores.Ruta);
            }
            catch (Exception ex)
            {
                // La aplicación sigue arrancando: las rutas quedan intactas y la migración
                // se reintenta en el siguiente inicio.
                RegistroErrores.RegistrarEn(RegistroErrores.Ruta, ex, "no se pudieron migrar los adjuntos");
            }
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

        // Un solo aviso por arranque: los documentos que no caben en la base se resuelven
        // reduciendo el fichero o moviéndolo, y el siguiente arranque los reintenta.
        if (pendientesPorTamano.Count > 0)
        {
            this.dialogo.Avisar(
                string.Format(
                    Localizacion.Texto("Mensaje.AdjuntosAntiguosDemasiadoGrandes"),
                    pendientesPorTamano.Count,
                    AdjuntosHelper.Carpeta),
                Localizacion.Texto("Titulo.AdjuntosAntiguos"));
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

        this.EscucharAvisosDeOtrasCopias(ventana);
    }

    // Otra copia lanzada avisa aquí: la ventana se muestra desde el hilo de la interfaz,
    // tanto si estaba minimizada como si solo había quedado el icono de la bandeja.
    [ExcludeFromCodeCoverage]
    private void EscucharAvisosDeOtrasCopias(MainWindow ventana)
    {
        _ = Task.Run(() => this.instancia?.EsperarAviso(() =>
        {
            if (this.Dispatcher.HasShutdownStarted)
            {
                return;
            }

            try
            {
                this.Dispatcher.Invoke(() => ventana.MostrarVentana());
            }
            catch (InvalidOperationException)
            {
                // La aplicación se está cerrando y el despachador ya no acepta trabajo.
            }
            catch (TaskCanceledException)
            {
                // El despachador se cerró entre la comprobación y el Invoke.
            }
        }));
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
            RegistroErrores.RegistrarEn(
                RegistroErrores.Ruta,
                (Exception)e.ExceptionObject,
                "excepción no controlada en segundo plano");

        TaskScheduler.UnobservedTaskException += (_, e) =>
            RegistroErrores.RegistrarEn(
                RegistroErrores.Ruta,
                e.Exception,
                "excepción no observada en una tarea");
    }

    // El ViewModel se suelta antes que la base: Dispose se desengancha del evento
    // estático de idioma antes de que el contexto deje de ser válido.
    [ExcludeFromCodeCoverage]
    protected override void OnExit(ExitEventArgs e)
    {
        // Solo la copia principal limpia los temporales: la segunda se cierra aquí
        // mientras la primera puede tener documentos abiertos en el visor.
        bool eraPrimera = this.instancia?.EsPrimera == true;

        this.viewModel?.Dispose();
        this.db?.Dispose();
        this.instancia?.Dispose();

        // Los documentos abiertos se copiaron a %TEMP% para el visor: no deben
        // sobrevivir a la sesión.
        TemporalesAdjuntos.LimpiarSiEsPrimera(eraPrimera);

        base.OnExit(e);
    }
}
