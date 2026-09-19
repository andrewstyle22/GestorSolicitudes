using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using GestorSolicitudes.Data;
using Microsoft.Data.Sqlite;
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

            // EnsureCreated no versiona el esquema: si la base ya existía sin las
            // columnas nuevas, las añadimos aquí con un ALTER TABLE barato.
            VerificarColumnasFaltantes();
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
        // ShutdownMode="OnMainWindowClose" en App.xaml hace que cerrar la ventana
        // (con la X o con el menú "Salir" de la bandeja) termine la aplicación.
        var ventana = new Views.MainWindow();
        MainWindow = ventana;
        ventana.Show();
    }

    /// <summary>
    /// Añade las columnas nuevas del modelo a bases de datos creadas por versiones
    /// anteriores. Más adelante, si el esquema crece, lo natural es pasar a migraciones.
    /// </summary>
    private static void VerificarColumnasFaltantes()
    {
        using var conexion = new SqliteConnection($"Data Source={AppDbContext.RutaBaseDatos}");
        conexion.Open();

        var columnas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var command = conexion.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info(Solicitudes);";
            using var lector = command.ExecuteReader();
            while (lector.Read())
                columnas.Add(Convert.ToString(lector["name"]) ?? string.Empty);
        }

        foreach (string columna in new[] { "RutaCv", "RutaCarta", "NombreOriginalCv", "NombreOriginalCarta" })
        {
            if (columnas.Contains(columna)) continue;

            using var command = conexion.CreateCommand();
            command.CommandText = $"ALTER TABLE Solicitudes ADD COLUMN \"{columna}\" TEXT NULL;";
            command.ExecuteNonQuery();
        }
    }
}
