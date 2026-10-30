namespace GestorSolicitudes.Tests;

using System.IO;
using GestorSolicitudes.Models;
using GestorSolicitudes.ViewModels;
using Microsoft.EntityFrameworkCore;

public class ViewModelDialogosTests
{
    // ---------------- Comandos con diálogos ----------------
    [Fact]
    public void Guardar_SinEmpresaOPuesto_AvisaYSigueEditando()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso();
        using var vm = new MainViewModel(db, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);
        vm.Edicion!.Empresa = "ACME"; // falta el puesto

        vm.GuardarCommand.Execute(null);

        Assert.Single(dialogo.Avisos);
        Assert.NotNull(vm.Edicion);
        Assert.Empty(db.Solicitudes);
    }

    [Fact]
    public void Guardar_CuandoFallaLaBase_AvisaDeErrorYNoCierraElPanel()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso();
        using var vm = new MainViewModel(db, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);
        vm.Edicion!.Empresa = "ACME";
        vm.Edicion.Puesto = "Dev";
        db.Database.ExecuteSqlRaw("DROP TABLE Solicitudes"); // que SaveChanges falle dentro del try

        vm.GuardarCommand.Execute(null);

        Assert.Single(dialogo.Errores);
        Assert.NotNull(vm.Edicion);
    }

    [Fact]
    public void AdjuntarCv_CopiaElFicheroYPoneElNombreOriginal()
    {
        using var db = TestDb.NuevoContexto();
        string carpeta = TestDb.NuevaCarpeta();
        string origen = Path.Combine(TestDb.NuevaCarpeta(), "mi-cv.pdf");
        File.WriteAllText(origen, "cv");
        var dialogo = new DialogoFalso { FicheroElegido = origen };
        using var vm = new MainViewModel(db, carpetaAdjuntos: carpeta, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);

        vm.AdjuntarCvCommand.Execute(null);

        Assert.NotNull(vm.Edicion!.RutaCv);
        Assert.StartsWith(carpeta, vm.Edicion.RutaCv!);
        Assert.True(File.Exists(vm.Edicion.RutaCv));
        Assert.Equal("mi-cv.pdf", vm.Edicion.NombreOriginalCv);
    }

    [Fact]
    public void AdjuntarCarta_CopiaElFicheroYPoneElNombreOriginal()
    {
        using var db = TestDb.NuevoContexto();
        string carpeta = TestDb.NuevaCarpeta();
        string origen = Path.Combine(TestDb.NuevaCarpeta(), "carta.docx");
        File.WriteAllText(origen, "carta");
        var dialogo = new DialogoFalso { FicheroElegido = origen };
        using var vm = new MainViewModel(db, carpetaAdjuntos: carpeta, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);

        vm.AdjuntarCartaCommand.Execute(null);

        Assert.NotNull(vm.Edicion!.RutaCarta);
        Assert.Equal("carta.docx", vm.Edicion.NombreOriginalCarta);
    }

    [Fact]
    public void AdjuntarCv_CuandoSeCancela_NoCambiaNada()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso { FicheroElegido = null };
        using var vm = new MainViewModel(db, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);

        vm.AdjuntarCvCommand.Execute(null);

        Assert.Null(vm.Edicion!.RutaCv);
    }

    [Fact]
    public void AdjuntarCv_CuandoNoSePuedeCopiar_AvisaDeError()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso
        {
            FicheroElegido = Path.Combine(TestDb.NuevaCarpeta(), "no-existe.pdf"),
        };
        using var vm = new MainViewModel(db, carpetaAdjuntos: TestDb.NuevaCarpeta(), dialogo: dialogo);
        vm.NuevaCommand.Execute(null);

        vm.AdjuntarCvCommand.Execute(null);

        Assert.Contains(
            Localizacion.Texto("Mensaje.NoSePudoAdjuntar").Replace("{0}", string.Empty),
            Assert.Single(dialogo.Errores));
        Assert.Null(vm.Edicion!.RutaCv);
    }

    [Fact]
    public void AdjuntarCarta_CuandoNoSePuedeCopiar_AvisaDeError()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso
        {
            FicheroElegido = Path.Combine(TestDb.NuevaCarpeta(), "no-existe.docx"),
        };
        using var vm = new MainViewModel(db, carpetaAdjuntos: TestDb.NuevaCarpeta(), dialogo: dialogo);
        vm.NuevaCommand.Execute(null);

        vm.AdjuntarCartaCommand.Execute(null);

        Assert.Contains(
            Localizacion.Texto("Mensaje.NoSePudoAdjuntar").Replace("{0}", string.Empty),
            Assert.Single(dialogo.Errores));
        Assert.Null(vm.Edicion!.RutaCarta);
    }

    [Fact]
    public void Eliminar_CuandoSeConfirma_BorraLaFilaYLosAdjuntos()
    {
        using var db = TestDb.NuevoContexto();
        string carpeta = TestDb.NuevaCarpeta();
        var dialogo = new DialogoFalso { RespuestaConfirmar = true };
        using var vm = new MainViewModel(db, carpetaAdjuntos: carpeta, dialogo: dialogo);
        var solicitud = new Solicitud { Empresa = "ACME", Puesto = "Dev", RutaCv = TestDb.CrearAdjuntoEn(carpeta, "cv.pdf") };
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.Edicion = solicitud;
        string rutaCv = solicitud.RutaCv!;

        vm.EliminarCommand.Execute(null);

        Assert.Empty(db.Solicitudes);
        Assert.Null(vm.Edicion);
        Assert.False(File.Exists(rutaCv));
    }

    [Fact]
    public void Eliminar_CuandoElUsuarioNoConfirma_NoBorraNada()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso { RespuestaConfirmar = false };
        using var vm = new MainViewModel(db, dialogo: dialogo);
        var solicitud = new Solicitud { Empresa = "ACME", Puesto = "Dev" };
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.Edicion = solicitud;

        vm.EliminarCommand.Execute(null);

        Assert.Single(dialogo.Confirmaciones);
        Assert.Equal(1, db.Solicitudes.Count());
        Assert.NotNull(vm.Edicion);
    }

    [Fact]
    public void Eliminar_CuandoFallaLaBase_AvisaDeErrorYNoCierraElPanel()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso { RespuestaConfirmar = true };
        using var vm = new MainViewModel(db, dialogo: dialogo);
        var solicitud = new Solicitud { Empresa = "ACME", Puesto = "Dev" };
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.Edicion = solicitud;
        db.Database.ExecuteSqlRaw("DROP TABLE Solicitudes");

        vm.EliminarCommand.Execute(null);

        Assert.Single(dialogo.Errores);
        Assert.NotNull(vm.Edicion);
    }

    [Fact]
    public void ExportarCsv_ConDestinoEscribeEInforma()
    {
        using var db = TestDb.NuevoContexto();
        db.Solicitudes.Add(new Solicitud { Empresa = "ACME", Puesto = "Dev" });
        db.SaveChanges();
        var dialogo = new DialogoFalso { DestinoElegido = Path.Combine(TestDb.NuevaCarpeta(), "salida.csv") };
        using var vm = new MainViewModel(db, dialogo: dialogo);

        vm.ExportarCsvCommand.Execute(null);

        Assert.True(File.Exists(dialogo.DestinoElegido));
        Assert.Single(dialogo.Informes);
        Assert.Empty(dialogo.Errores);
    }

    [Fact]
    public void ExportarCsv_CuandoSeCancela_NoEscribeNiInforma()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso { DestinoElegido = null };
        using var vm = new MainViewModel(db, dialogo: dialogo);

        vm.ExportarCsvCommand.Execute(null);

        Assert.Empty(dialogo.Informes);
        Assert.Empty(dialogo.Errores);
    }

    [Fact]
    public void ExportarCsv_CuandoNoSePuedeEscribir_AvisaDeError()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso
        {
            DestinoElegido = Path.Combine(TestDb.NuevaCarpeta(), "no", "existe", "salida.csv"),
        };
        using var vm = new MainViewModel(db, dialogo: dialogo);

        vm.ExportarCsvCommand.Execute(null);

        Assert.Single(dialogo.Errores);
        Assert.Empty(dialogo.Informes);
    }

    [Fact]
    public void AbrirEnlace_ConUrlLaAbreSinEspacios()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso();
        using var vm = new MainViewModel(db, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);
        vm.Edicion!.EnlaceOferta = "  https://ejemplo.com/oferta  ";

        vm.AbrirEnlaceCommand.Execute(null);

        Assert.Equal("https://ejemplo.com/oferta", Assert.Single(dialogo.EnlacesAbiertos));
    }

    [Fact]
    public void AbrirEnlace_SinUrlNoLlamaAlNavegador()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso();
        using var vm = new MainViewModel(db, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);

        vm.AbrirEnlaceCommand.Execute(null);

        Assert.Empty(dialogo.EnlacesAbiertos);
    }

    [Fact]
    public void AbrirEnlace_CuandoFalla_AvisaPeroNoRevienta()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso { AbrirEnlaceLanza = true };
        using var vm = new MainViewModel(db, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);
        vm.Edicion!.EnlaceOferta = "https://ejemplo.com";

        vm.AbrirEnlaceCommand.Execute(null);

        Assert.Single(dialogo.Avisos);
    }

    [Theory]
    [InlineData("mailto:alguien@example.com")]
    [InlineData("ejemplo.com")]
    public void AbrirEnlace_ConEnlaceQueNoEsWeb_AvisaYNoLoAbre(string enlace)
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso();
        using var vm = new MainViewModel(db, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);
        vm.Edicion!.EnlaceOferta = enlace;

        vm.AbrirEnlaceCommand.Execute(null);

        Assert.Single(dialogo.Avisos);
        Assert.Empty(dialogo.EnlacesAbiertos);
    }
}
