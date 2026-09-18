using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using GestorSolicitudes.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorSolicitudes;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Fechas y números en formato español, también dentro de los bindings de WPF.
        var cultura = new CultureInfo("es-ES");
        CultureInfo.DefaultThreadCurrentCulture = cultura;
        CultureInfo.DefaultThreadCurrentUICulture = cultura;
        Thread.CurrentThread.CurrentCulture = cultura;
        Thread.CurrentThread.CurrentUICulture = cultura;

        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(cultura.IetfLanguageTag)));

        try
        {
            using var db = new AppDbContext();
            db.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo preparar la base de datos:\n\n{ex.Message}\n\nRuta: {AppDbContext.RutaBaseDatos}",
                "Error al iniciar", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        // Creamos la ventana aquí en lugar de con StartupUri: así el fallo de arriba
        // aborta de verdad el arranque y no dependemos de resolver una URI de recurso.
        var ventana = new Views.MainWindow();
        MainWindow = ventana;
        ventana.Show();
    }
}
