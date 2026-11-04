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
        vm.Editor!.Empresa = "ACME"; // falta el puesto

        vm.GuardarCommand.Execute(null);

        Assert.Single(dialogo.Avisos);
        Assert.NotNull(vm.Editor);
        Assert.Empty(db.Solicitudes);
    }

    [Fact]
    public void Guardar_LaValidacionLeeElEditor_NoLaEntidad()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso();
        using var vm = new MainViewModel(db, dialogo: dialogo);
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;
        vm.Editor!.Puesto = string.Empty; // la entidad conserva el puesto guardado

        vm.GuardarCommand.Execute(null);

        Assert.Single(dialogo.Avisos);
        Assert.Equal("Dev", db.Solicitudes.Single().Puesto);
    }

    [Fact]
    public void Guardar_ConTextoDemasiadoLargo_AvisaYNoGuarda()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso();
        using var vm = new MainViewModel(db, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);
        vm.RellenarObligatorios("ACME", "Dev");
        vm.Editor!.Portal = new string('x', Solicitud.LongitudMaximaPortal + 1);

        vm.GuardarCommand.Execute(null);

        Assert.Single(dialogo.Avisos);
        Assert.NotNull(vm.Editor);
        Assert.Empty(db.Solicitudes);
    }

    [Fact]
    public void Guardar_ConSalarioMinimoMayorQueElMaximo_AvisaYNoGuarda()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso();
        using var vm = new MainViewModel(db, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);
        vm.RellenarObligatorios("ACME", "Dev");
        vm.Editor!.SalarioMin = 45000;
        vm.Editor.SalarioMax = 30000;

        vm.GuardarCommand.Execute(null);

        Assert.Single(dialogo.Avisos);
        Assert.NotNull(vm.Editor);
        Assert.Empty(db.Solicitudes);
    }

    [Fact]
    public void Guardar_CuandoFallaLaBase_AvisaDeErrorYNoCierraElPanel()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso();
        using var vm = new MainViewModel(db, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);
        vm.RellenarObligatorios("ACME", "Dev");
        db.Database.ExecuteSqlRaw("DROP TABLE Solicitudes"); // que SaveChanges falle dentro del try

        vm.GuardarCommand.Execute(null);

        Assert.Single(dialogo.Errores);
        Assert.NotNull(vm.Editor);
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

        Assert.NotNull(vm.Editor!.RutaCv);
        Assert.StartsWith(carpeta, vm.Editor.RutaCv!);
        Assert.True(File.Exists(vm.Editor.RutaCv));
        Assert.Equal("mi-cv.pdf", vm.Editor.NombreOriginalCv);
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

        Assert.NotNull(vm.Editor!.RutaCarta);
        Assert.Equal("carta.docx", vm.Editor.NombreOriginalCarta);
    }

    [Fact]
    public void AdjuntarCv_CuandoSeCancela_NoCambiaNada()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso { FicheroElegido = null };
        using var vm = new MainViewModel(db, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);

        vm.AdjuntarCvCommand.Execute(null);

        Assert.Null(vm.Editor!.RutaCv);
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
        Assert.Null(vm.Editor!.RutaCv);
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
        Assert.Null(vm.Editor!.RutaCarta);
    }

    [Fact]
    public void Eliminar_CuandoSeConfirma_BorraLaFilaYLosAdjuntos()
    {
        using var db = TestDb.NuevoContexto();
        string carpeta = TestDb.NuevaCarpeta();
        var dialogo = new DialogoFalso { RespuestaConfirmar = true };
        using var vm = new MainViewModel(db, carpetaAdjuntos: carpeta, dialogo: dialogo);
        var solicitud = new SolicitudBuilder()
            .ConEmpresa("ACME")
            .ConPuesto("Dev")
            .ConRutaCv(TestDb.CrearAdjuntoEn(carpeta, "cv.pdf"))
            .Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;
        string rutaCv = solicitud.RutaCv!;

        vm.EliminarCommand.Execute(null);

        Assert.Empty(db.Solicitudes);
        Assert.Null(vm.Editor);
        Assert.False(File.Exists(rutaCv));
    }

    [Fact]
    public void Eliminar_CuandoElUsuarioNoConfirma_NoBorraNada()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso { RespuestaConfirmar = false };
        using var vm = new MainViewModel(db, dialogo: dialogo);
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;

        vm.EliminarCommand.Execute(null);

        Assert.Single(dialogo.Confirmaciones);
        Assert.Equal(1, db.Solicitudes.Count());
        Assert.NotNull(vm.Editor);
    }

    [Fact]
    public void Eliminar_CuandoFallaLaBase_AvisaDeErrorYNoCierraElPanel()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso { RespuestaConfirmar = true };
        using var vm = new MainViewModel(db, dialogo: dialogo);
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;
        db.Database.ExecuteSqlRaw("DROP TABLE Solicitudes");

        vm.EliminarCommand.Execute(null);

        Assert.Single(dialogo.Errores);
        Assert.NotNull(vm.Editor);
    }

    [Fact]
    public void ExportarCsv_ConDestinoEscribeEInforma()
    {
        using var db = TestDb.NuevoContexto();
        db.Solicitudes.Add(new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir());
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
        vm.Editor!.EnlaceOferta = "  https://ejemplo.com/oferta  ";

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
        vm.Editor!.EnlaceOferta = "https://ejemplo.com";

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
        vm.Editor!.EnlaceOferta = enlace;

        vm.AbrirEnlaceCommand.Execute(null);

        Assert.Single(dialogo.Avisos);
        Assert.Empty(dialogo.EnlacesAbiertos);
    }
}
