namespace GestorSolicitudes.Tests;

using System.IO;
using GestorSolicitudes.Models;
using GestorSolicitudes.ViewModels;

public class ViewModelCsvTests
{
    // ---------------- Exportación CSV ----------------
    [Fact]
    public void EscribirCsv_EnRutaValidaGuardaElContenido()
    {
        using var db = TestDb.NuevoContexto();
        db.Solicitudes.Add(new Solicitud { Empresa = "ACME", Puesto = "Dev" });
        db.SaveChanges();
        using var vm = new MainViewModel(db);
        string ruta = Path.Combine(TestDb.NuevaCarpeta(), "salida.csv");

        bool escrito = vm.TryEscribirCsv(ruta, out Exception? error);

        Assert.True(escrito);
        Assert.Null(error);
        string csv = File.ReadAllText(ruta);
        Assert.Contains(Localizacion.Texto("Csv.Empresa"), csv);
        Assert.Contains("ACME", csv);
    }

    [Fact]
    public void EscribirCsv_EnRutaInvalidaDevuelveElFalloEnVezDeLanzarlo()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
        string ruta = Path.Combine(TestDb.NuevaCarpeta(), "no", "existe", "salida.csv");

        Assert.False(vm.TryEscribirCsv(ruta, out Exception? error));
        Assert.NotNull(error);
    }

    [Fact]
    public void ConstruirCsv_ConLaMismaFechaDesempataPorIdDescendente()
    {
        using var db = TestDb.NuevoContexto();
        var fecha = new DateTime(2024, 5, 1);
        db.Solicitudes.Add(new Solicitud { Empresa = "Antigua", Puesto = "Dev", FechaSolicitud = fecha });
        db.SaveChanges();
        db.Solicitudes.Add(new Solicitud { Empresa = "Nueva", Puesto = "Dev", FechaSolicitud = fecha });
        db.SaveChanges();
        using var vm = new MainViewModel(db);

        string[] filas = vm.ConstruirCsv().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.True(
            Array.FindIndex(filas, f => f.Contains("Nueva")) < Array.FindIndex(filas, f => f.Contains("Antigua")),
            "La solicitud con el Id mayor debe ir primero cuando comparten fecha.");
    }

    [Fact]
    public void ExportarCsv_InformaLasFilasEscritasAunqueLaCabeceraEsteDesfasada()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso { DestinoElegido = Path.Combine(TestDb.NuevaCarpeta(), "salida.csv") };
        using var vm = new MainViewModel(db, dialogo: dialogo);
        db.Solicitudes.Add(new Solicitud { Empresa = "ACME", Puesto = "Dev" });
        db.Solicitudes.Add(new Solicitud { Empresa = "ACME", Puesto = "Dev" });
        db.SaveChanges();

        vm.ExportarCsvCommand.Execute(null);

        // TotalSolicitudes sigue en 0: el aviso cuenta lo que se escribe, no la cabecera.
        Assert.Equal(string.Format(Localizacion.Texto("Csv.ExportadasN"), 2), Assert.Single(dialogo.Informes));
    }
}