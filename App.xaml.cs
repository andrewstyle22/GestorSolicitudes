namespace GestorSolicitudes;

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using GestorSolicitudes.Data;
using Microsoft.EntityFrameworkCore;

public partial class App : Application
{
    // Arranque de la app: ventanas, bandeja y MessageBox del fallo de base de datos.
    // Requiere el ciclo de vida completo de WPF y no se puede probar con los tests.
    [ExcludeFromCodeCoverage]
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Idioma de la interfaz: preferencia guardada (o el idioma del sistema la primera vez),
        // cultura de fechas/números e inyección de los textos como recursos de aplicación.
        Localizacion.Inicializar();

        try
        {
            using var db = new AppDbContext();
            db.Database.EnsureCreated();

            // EnsureCreated no versiona el esquema: si la base ya existía sin las
            // columnas nuevas, las añadimos aquí con un ALTER TABLE barato.
            EsquemaDb.VerificarColumnasFaltantes(AppDbContext.RutaBaseDatos);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                string.Format(Localizacion.Texto("Mensaje.ErrorBaseDatos"), ex.Message, AppDbContext.RutaBaseDatos),
                Localizacion.Texto("Titulo.ErrorAlIniciar"), MessageBoxButton.OK, MessageBoxImage.Error);
            this.Shutdown();
            return;
        }

        // Creamos la ventana aquí en lugar de con StartupUri: así el fallo de arriba
        // aborta de verdad el arranque y no dependemos de resolver una URI de recurso.
        // ShutdownMode="OnMainWindowClose" en App.xaml hace que cerrar la ventana
        // (con la X o con el menú "Salir" de la bandeja) termine la aplicación.
        var ventana = new Views.MainWindow();
        this.MainWindow = ventana;
        ventana.Show();
    }
}
