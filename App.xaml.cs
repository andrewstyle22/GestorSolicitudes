namespace GestorSolicitudes;

using System.Windows;
using GestorSolicitudes.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public partial class App : Application
{
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
            VerificarColumnasFaltantes();
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

    /// <summary>
    /// Añade las columnas nuevas del modelo a bases de datos creadas por versiones
    /// anteriores. Más adelante, si el esquema crece, lo natural es pasar a migraciones.
    /// </summary>
    private static void VerificarColumnasFaltantes() => VerificarColumnasFaltantes(AppDbContext.RutaBaseDatos);

    internal static void VerificarColumnasFaltantes(string ruta)
    {
        using var conexion = new SqliteConnection($"Data Source={ruta}");
        conexion.Open();

        var columnas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var command = conexion.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info(Solicitudes);";
            using var lector = command.ExecuteReader();
            while (lector.Read())
            {
                columnas.Add(Convert.ToString(lector["name"]) ?? string.Empty);
            }
        }

        // Los enums se guardan como entero; Origen es NOT NULL con valor por defecto
        // (0 = AplicacionDirecta) para que las filas ya existentes queden con un
        // valor válido en vez de nulo.
        const string TextoNulo = "TEXT NULL";

        (string Columna, string Definicion)[] nuevas =
        {
            ("RutaCv", TextoNulo),
            ("RutaCarta", TextoNulo),
            ("NombreOriginalCv", TextoNulo),
            ("NombreOriginalCarta", TextoNulo),
            ("Requisitos", TextoNulo),
            ("MotivoRechazo", "INTEGER NULL"),
            ("Origen", "INTEGER NOT NULL DEFAULT 0"),
            ("ContactoTelefono", "TEXT NULL"),
        };

        foreach ((string columna, string definicion) in nuevas)
        {
            if (columnas.Contains(columna))
            {
                continue;
            }

            using var command = conexion.CreateCommand();
            command.CommandText = $"ALTER TABLE Solicitudes ADD COLUMN \"{columna}\" {definicion};";
            command.ExecuteNonQuery();
        }
    }
}
