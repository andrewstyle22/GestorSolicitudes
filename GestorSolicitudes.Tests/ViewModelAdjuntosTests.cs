namespace GestorSolicitudes.Tests;

using System.IO;
using GestorSolicitudes.Models;
using GestorSolicitudes.ViewModels;
using Microsoft.EntityFrameworkCore;

public class ViewModelAdjuntosTests
{
    // ---------------- Comandos del panel de detalle ----------------
    [Fact]
    public void AnadirEvento_AnadeUnaNotaAlEditorSinTocarLaEntidad()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;

        vm.AnadirEventoCommand.Execute(null);

        EventoViewModel nota = Assert.Single(vm.Editor!.EventosEdicion);
        Assert.Equal(TipoEvento.Nota, nota.Tipo);
        Assert.Equal(DateTime.Today, nota.Fecha);
        Assert.Empty(solicitud.Eventos);
        Assert.Equal(EntityState.Unchanged, db.Entry(solicitud).State);
    }

    [Fact]
    public void Cancelar_DescartaElHistorialDeLaVista()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir();
        solicitud.Eventos.Add(new Evento { Fecha = DateTime.Today, Tipo = TipoEvento.Nota });
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;
        Assert.Single(vm.Editor!.EventosEdicion);

        vm.CancelarCommand.Execute(null);

        Assert.Null(vm.Editor);
    }

    [Fact]
    public void EliminarEvento_SinEdicionNoHaceNada()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);

        vm.EliminarEventoCommand.Execute(new EventoViewModel());

        Assert.Empty(vm.Solicitudes);
    }

    [Fact]
    public void EliminarEvento_QuitaDeLaListaYMarcaElPersistidoComoBorrado()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir();
        solicitud.Eventos.Add(new Evento { Fecha = DateTime.Today.AddDays(-1), Tipo = TipoEvento.SolicitudEnviada });
        solicitud.Eventos.Add(new Evento { Fecha = DateTime.Today, Tipo = TipoEvento.Nota });
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;

        // La vista recibe una copia editable de cada evento, no la entidad del POCO.
        Evento persistido = solicitud.Eventos.First(e => e.Id != 0);
        EventoViewModel copia = vm.Editor!.EventosEdicion.Single(e => e.Tipo == persistido.Tipo);
        Assert.Equal(persistido.Fecha, copia.Fecha);

        vm.EliminarEventoCommand.Execute(copia);

        Assert.DoesNotContain(persistido, solicitud.Eventos);
        Assert.DoesNotContain(copia, vm.Editor.EventosEdicion);
        Assert.Equal(EntityState.Deleted, db.Entry(persistido).State);
    }

    [Fact]
    public void QuitarCv_NoBorraElFicheroHastaGuardar()
    {
        using var db = TestDb.NuevoContexto();
        string carpeta = TestDb.NuevaCarpeta();
        using var vm = new MainViewModel(db, carpetaAdjuntos: carpeta);
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").ConRutaCv(TestDb.CrearAdjuntoEn(carpeta, "cv.pdf")).Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;
        string rutaCv = solicitud.RutaCv!;

        vm.QuitarCvCommand.Execute(null);

        Assert.Null(vm.Editor!.RutaCv);
        // El fichero sigue en disco: la base todavía lo apunta hasta que se guarde.
        Assert.True(File.Exists(rutaCv));

        vm.GuardarCommand.Execute(null);

        Assert.False(File.Exists(rutaCv));
    }

    [Fact]
    public void QuitarCv_YCancelar_ConservaElFicheroYLaRutaGuardada()
    {
        using var db = TestDb.NuevoContexto();
        string carpeta = TestDb.NuevaCarpeta();
        using var vm = new MainViewModel(db, carpetaAdjuntos: carpeta);
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").ConRutaCv(TestDb.CrearAdjuntoEn(carpeta, "cv.pdf")).Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;
        string rutaCv = solicitud.RutaCv!;

        vm.QuitarCvCommand.Execute(null);
        vm.CancelarCommand.Execute(null);

        Assert.True(File.Exists(rutaCv));
        Assert.Equal(rutaCv, db.Solicitudes.Single().RutaCv);
    }

    [Fact]
    public void AdjuntarCv_YGuardar_PersisteLaRutaEnLaEntidad()
    {
        using var db = TestDb.NuevoContexto();
        string carpeta = TestDb.NuevaCarpeta();
        string origen = Path.Combine(TestDb.NuevaCarpeta(), "mi-cv.pdf");
        File.WriteAllText(origen, "cv");
        var dialogo = new DialogoFalso { FicheroElegido = origen };
        using var vm = new MainViewModel(db, carpetaAdjuntos: carpeta, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);
        vm.RellenarObligatorios("ACME", "Dev");

        vm.AdjuntarCvCommand.Execute(null);
        string rutaCv = vm.Editor!.RutaCv!;
        vm.GuardarCommand.Execute(null);

        Assert.Equal(rutaCv, db.Solicitudes.Single().RutaCv);
        Assert.Equal("mi-cv.pdf", db.Solicitudes.Single().NombreOriginalCv);
        Assert.True(File.Exists(rutaCv));
    }

    [Fact]
    public void QuitarCarta_NoBorraElFicheroHastaGuardar()
    {
        using var db = TestDb.NuevoContexto();
        string carpeta = TestDb.NuevaCarpeta();
        using var vm = new MainViewModel(db, carpetaAdjuntos: carpeta);
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").ConRutaCarta(TestDb.CrearAdjuntoEn(carpeta, "carta.pdf")).Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;
        string rutaCarta = solicitud.RutaCarta!;

        vm.QuitarCartaCommand.Execute(null);

        Assert.Null(vm.Editor!.RutaCarta);
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
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").ConRutaCv(TestDb.CrearAdjuntoEn(carpeta, "cv.pdf")).Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;
        string viejo = solicitud.RutaCv!;
        string nuevo = TestDb.CrearAdjuntoEn(carpeta, "cv-nuevo.pdf");
        vm.Editor!.RutaCv = nuevo;

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
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").ConRutaCv(TestDb.CrearAdjuntoEn(carpeta, "cv.pdf")).Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;
        string viejo = solicitud.RutaCv!;
        string nuevo = TestDb.CrearAdjuntoEn(carpeta, "cv-nuevo.pdf");
        vm.Editor!.RutaCv = nuevo;

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
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").ConRutaCv(TestDb.CrearAdjuntoEn(carpeta, "cv.pdf")).Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;
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
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").ConRutaCv(rutaAnterior).Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;

        string origenNuevo = Path.Combine(TestDb.NuevaCarpeta(), "cv-nuevo.pdf");
        File.WriteAllText(origenNuevo, "nuevo");
        dialogo.FicheroElegido = origenNuevo;
        vm.AdjuntarCvCommand.Execute(null);
        string rutaNueva = vm.Editor!.RutaCv!;

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
        vm.RellenarObligatorios("ACME", "Dev");

        vm.AdjuntarCvCommand.Execute(null);
        string intermedio = vm.Editor!.RutaCv!;
        dialogo.FicheroElegido = segundo;
        vm.AdjuntarCvCommand.Execute(null);
        string final = vm.Editor!.RutaCv!;

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
        vm.RellenarObligatorios("ACME", "Dev");

        vm.AdjuntarCvCommand.Execute(null);
        string intermedio = vm.Editor!.RutaCv!;
        dialogo.FicheroElegido = segundo;
        vm.AdjuntarCvCommand.Execute(null);
        string final = vm.Editor!.RutaCv!;

        vm.CancelarCommand.Execute(null);

        Assert.False(File.Exists(intermedio));
        Assert.False(File.Exists(final));
        Assert.Null(vm.Editor);
    }

    [Fact]
    public void QuitarCv_SinEdicionNoHaceNada()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);

        vm.QuitarCvCommand.Execute(null);

        Assert.Null(vm.Editor);
    }

    [Fact]
    public void AdjuntoCv_SinFicheroMuestraElTextoDeSinAdjuntar()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
        vm.NuevaCommand.Execute(null);

        Adjunto cv = vm.Editor!.AdjuntoCv;

        Assert.Null(cv.Ruta);
        Assert.Equal(Localizacion.Texto("Adjuntos.CurriculumSinAdjuntar"), cv.Nombre);
    }

    [Fact]
    public void AdjuntoCarta_UsaElNombreOriginalYLaRuta()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db);
        vm.NuevaCommand.Execute(null);
        vm.Editor!.RutaCarta = @"C:\adjuntos\a1b2.pdf";
        vm.Editor.NombreOriginalCarta = "carta.pdf";

        Adjunto carta = vm.Editor.AdjuntoCarta;

        Assert.Equal(@"C:\adjuntos\a1b2.pdf", carta.Ruta);
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
