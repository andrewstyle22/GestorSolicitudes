using System.IO;
using Microsoft.EntityFrameworkCore;
using GestorSolicitudes.Models;
using GestorSolicitudes.ViewModels;

namespace GestorSolicitudes.Tests;

public class ViewModelAdjuntosTests
{
    // ---------------- Comandos del panel de detalle ----------------

    [Fact]
    public void AnadirEvento_AnadeUnaNotaConLaFechaDeHoy()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
        vm.NuevaCommand.Execute(null);
        vm.Edicion!.Empresa = "ACME";
        vm.Edicion.Puesto = "Dev";

        vm.AnadirEventoCommand.Execute(null);

        Evento nota = Assert.Single(vm.Edicion.Eventos);
        Assert.Equal(TipoEvento.Nota, nota.Tipo);
        Assert.Equal(DateTime.Today, nota.Fecha);
    }

    [Fact]
    public void EliminarEvento_SinEdicionNoHaceNada()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);

        vm.EliminarEventoCommand.Execute(new Evento());

        Assert.Empty(vm.Solicitudes);
    }

    [Fact]
    public void EliminarEvento_QuitaDeLaListaYMarcaElPersistidoComoBorrado()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
        var solicitud = new Solicitud { Empresa = "ACME", Puesto = "Dev" };
        solicitud.Eventos.Add(new Evento { Fecha = DateTime.Today.AddDays(-1), Tipo = TipoEvento.SolicitudEnviada });
        solicitud.Eventos.Add(new Evento { Fecha = DateTime.Today, Tipo = TipoEvento.Nota });
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.Edicion = solicitud;

        Evento persistido = solicitud.Eventos.First(e => e.Id != 0);
        vm.EliminarEventoCommand.Execute(persistido);

        Assert.DoesNotContain(persistido, solicitud.Eventos);
        Assert.Equal(EntityState.Deleted, db.Entry(persistido).State);
    }

    [Fact]
    public void QuitarCv_NoBorraElFicheroHastaGuardar()
    {
        using var db = TestDb.NuevoContexto();
        string carpeta = TestDb.NuevaCarpeta();
        using var vm = new MainViewModel(db, carpetaAdjuntos: carpeta);
        var solicitud = new Solicitud { Empresa = "ACME", Puesto = "Dev", RutaCv = TestDb.CrearAdjuntoEn(carpeta, "cv.pdf") };
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.Edicion = solicitud;
        string rutaCv = solicitud.RutaCv!;

        vm.QuitarCvCommand.Execute(null);

        Assert.Null(solicitud.RutaCv);
        // El fichero sigue en disco: la base todavía lo apunta hasta que se guarde.
        Assert.True(File.Exists(rutaCv));

        vm.GuardarCommand.Execute(null);

        Assert.False(File.Exists(rutaCv));
    }

    [Fact]
    public void QuitarCarta_NoBorraElFicheroHastaGuardar()
    {
        using var db = TestDb.NuevoContexto();
        string carpeta = TestDb.NuevaCarpeta();
        using var vm = new MainViewModel(db, carpetaAdjuntos: carpeta);
        var solicitud = new Solicitud { Empresa = "ACME", Puesto = "Dev", RutaCarta = TestDb.CrearAdjuntoEn(carpeta, "carta.pdf") };
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.Edicion = solicitud;
        string rutaCarta = solicitud.RutaCarta!;

        vm.QuitarCartaCommand.Execute(null);

        Assert.Null(solicitud.RutaCarta);
        Assert.True(File.Exists(rutaCarta));

        vm.GuardarCommand.Execute(null);

        Assert.False(File.Exists(rutaCarta));
    }

    [Fact]
    public void Guardar_ConElCvSustituido_BorraElAnteriorYGuardaElNuevo()
    {
        using var db = TestDb.NuevoContexto();
        string carpeta = TestDb.NuevaCarpeta();
        using var vm = new MainViewModel(db, carpetaAdjuntos: carpeta);
        var solicitud = new Solicitud { Empresa = "ACME", Puesto = "Dev", RutaCv = TestDb.CrearAdjuntoEn(carpeta, "cv.pdf") };
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.Edicion = solicitud;
        string viejo = solicitud.RutaCv!;
        string nuevo = TestDb.CrearAdjuntoEn(carpeta, "cv-nuevo.pdf");
        solicitud.RutaCv = nuevo;

        vm.GuardarCommand.Execute(null);

        Assert.False(File.Exists(viejo));
        Assert.True(File.Exists(nuevo));
        Assert.Equal(nuevo, db.Solicitudes.Single().RutaCv);
    }

    [Fact]
    public void Cancelar_ConElCvSustituido_ConservaElAnteriorYBorraElNuevo()
    {
        using var db = TestDb.NuevoContexto();
        string carpeta = TestDb.NuevaCarpeta();
        using var vm = new MainViewModel(db, carpetaAdjuntos: carpeta);
        var solicitud = new Solicitud { Empresa = "ACME", Puesto = "Dev", RutaCv = TestDb.CrearAdjuntoEn(carpeta, "cv.pdf") };
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.Edicion = solicitud;
        string viejo = solicitud.RutaCv!;
        string nuevo = TestDb.CrearAdjuntoEn(carpeta, "cv-nuevo.pdf");
        solicitud.RutaCv = nuevo;

        vm.CancelarCommand.Execute(null);

        Assert.True(File.Exists(viejo));
        Assert.False(File.Exists(nuevo));
        Assert.Equal(viejo, db.Solicitudes.Single().RutaCv);
    }

    [Fact]
    public void Eliminar_ConElCvQuitadoSinGuardar_BorraElFicheroGuardado()
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

        vm.QuitarCvCommand.Execute(null);
        vm.EliminarCommand.Execute(null);

        Assert.Empty(db.Solicitudes);
        Assert.False(File.Exists(rutaCv));
    }

    [Fact]
    public void Eliminar_ConElCvSustituidoSinGuardar_BorraElAnteriorYLaCopia()
    {
        using var db = TestDb.NuevoContexto();
        string carpeta = TestDb.NuevaCarpeta();
        string rutaAnterior = TestDb.CrearAdjuntoEn(carpeta, "cv.pdf");
        var dialogo = new DialogoFalso { RespuestaConfirmar = true };
        using var vm = new MainViewModel(db, carpetaAdjuntos: carpeta, dialogo: dialogo);
        var solicitud = new Solicitud { Empresa = "ACME", Puesto = "Dev", RutaCv = rutaAnterior };
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.Edicion = solicitud;

        string origenNuevo = Path.Combine(TestDb.NuevaCarpeta(), "cv-nuevo.pdf");
        File.WriteAllText(origenNuevo, "nuevo");
        dialogo.FicheroElegido = origenNuevo;
        vm.AdjuntarCvCommand.Execute(null);
        string rutaNueva = solicitud.RutaCv!;

        vm.EliminarCommand.Execute(null);

        Assert.Empty(db.Solicitudes);
        Assert.False(File.Exists(rutaAnterior));
        Assert.False(File.Exists(rutaNueva));
    }

    [Fact]
    public void AdjuntarCv_DosVecesAntesDeGuardar_BorraLaCopiaIntermedia()
    {
        using var db = TestDb.NuevoContexto();
        string carpeta = TestDb.NuevaCarpeta();
        string primero = Path.Combine(TestDb.NuevaCarpeta(), "cv-1.pdf");
        string segundo = Path.Combine(TestDb.NuevaCarpeta(), "cv-2.pdf");
        File.WriteAllText(primero, "uno");
        File.WriteAllText(segundo, "dos");
        var dialogo = new DialogoFalso { FicheroElegido = primero };
        using var vm = new MainViewModel(db, carpetaAdjuntos: carpeta, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);
        vm.Edicion!.Empresa = "ACME";
        vm.Edicion.Puesto = "Dev";

        vm.AdjuntarCvCommand.Execute(null);
        string intermedio = vm.Edicion.RutaCv!;
        dialogo.FicheroElegido = segundo;
        vm.AdjuntarCvCommand.Execute(null);
        string final = vm.Edicion.RutaCv!;

        vm.GuardarCommand.Execute(null);

        Assert.False(File.Exists(intermedio));
        Assert.True(File.Exists(final));
    }

    [Fact]
    public void Cancelar_ConDosCopiasDelCv_BorraLasDos()
    {
        using var db = TestDb.NuevoContexto();
        string carpeta = TestDb.NuevaCarpeta();
        string primero = Path.Combine(TestDb.NuevaCarpeta(), "cv-1.pdf");
        string segundo = Path.Combine(TestDb.NuevaCarpeta(), "cv-2.pdf");
        File.WriteAllText(primero, "uno");
        File.WriteAllText(segundo, "dos");
        var dialogo = new DialogoFalso { FicheroElegido = primero };
        using var vm = new MainViewModel(db, carpetaAdjuntos: carpeta, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);
        vm.Edicion!.Empresa = "ACME";
        vm.Edicion.Puesto = "Dev";

        vm.AdjuntarCvCommand.Execute(null);
        string intermedio = vm.Edicion.RutaCv!;
        dialogo.FicheroElegido = segundo;
        vm.AdjuntarCvCommand.Execute(null);
        string final = vm.Edicion.RutaCv!;

        vm.CancelarCommand.Execute(null);

        Assert.False(File.Exists(intermedio));
        Assert.False(File.Exists(final));
        Assert.Null(vm.Edicion);
    }

    [Fact]
    public void QuitarCv_SinEdicionNoHaceNada()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);

        vm.QuitarCvCommand.Execute(null);

        Assert.Null(vm.Edicion);
    }

    [Fact]
    public void AdjuntoCv_SinFicheroMuestraElTextoDeSinAdjuntar()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
        vm.NuevaCommand.Execute(null);

        Adjunto? cv = vm.AdjuntoCv;

        Assert.NotNull(cv);
        Assert.Null(cv.Ruta);
        Assert.Equal(Localizacion.Texto("Adjuntos.CurriculumSinAdjuntar"), cv.Nombre);
    }

    [Fact]
    public void AdjuntoCarta_UsaElNombreOriginalYLaRuta()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
        vm.NuevaCommand.Execute(null);
        vm.Edicion!.RutaCarta = @"C:\adjuntos\a1b2.pdf";
        vm.Edicion.NombreOriginalCarta = "carta.pdf";

        Adjunto? carta = vm.AdjuntoCarta;

        Assert.Equal(@"C:\adjuntos\a1b2.pdf", carta!.Ruta);
        Assert.Equal("carta.pdf", carta.Nombre);
    }

    [Fact]
    public void AbrirAdjunto_ConFicheroExistente_LoPasaAlVisor()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso();
        using var vm = new MainViewModel(db, dialogo: dialogo);
        string ruta = TestDb.CrearAdjuntoEn(TestDb.NuevaCarpeta(), "cv.pdf");

        vm.AbrirAdjuntoCommand.Execute(new Adjunto(ruta, "cv.pdf"));

        Assert.Equal(ruta, Assert.Single(dialogo.AdjuntosAbiertos));
    }

    [Fact]
    public void AbrirAdjunto_SinFicheroInformaYNoLlamaAlVisor()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso();
        using var vm = new MainViewModel(db, dialogo: dialogo);

        vm.AbrirAdjuntoCommand.Execute(new Adjunto(null, "Currículum sin adjuntar"));

        Assert.Single(dialogo.Informes);
        Assert.Empty(dialogo.AdjuntosAbiertos);
    }

    [Fact]
    public void AbrirAdjunto_CuandoElVisorFalla_Avisa()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso { VerAdjuntoLanza = true };
        using var vm = new MainViewModel(db, dialogo: dialogo);
        string ruta = TestDb.CrearAdjuntoEn(TestDb.NuevaCarpeta(), "cv.pdf");

        vm.AbrirAdjuntoCommand.Execute(new Adjunto(ruta, "cv.pdf"));

        Assert.Single(dialogo.Avisos);
    }
}