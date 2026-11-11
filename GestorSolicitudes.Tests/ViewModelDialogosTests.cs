namespace GestorSolicitudes.Tests;

using System.IO;
using GestorSolicitudes.Data;
using GestorSolicitudes.Models;
using GestorSolicitudes.ViewModels;
using Microsoft.EntityFrameworkCore;

public class ViewModelDialogosTests
{
    // ---------------- Comandos con diálogos ----------------
    [Fact]
    public void TryGuardar_GuardadoCorrecto_DevuelveTrueYCierraElPanel()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db, dialogo: new DialogoFalso());
        vm.NuevaCommand.Execute(null);
        vm.RellenarObligatorios("Globex", "Analista");

        Assert.True(vm.TryGuardar());
        Assert.Null(vm.Editor);
        Assert.Single(db.Solicitudes);
    }

    [Fact]
    public void TryGuardar_SinDatosObligatorios_DevuelveFalseYNoGuarda()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db, dialogo: new DialogoFalso());
        vm.NuevaCommand.Execute(null);
        vm.Editor!.Empresa = "Initech"; // falta el puesto

        Assert.False(vm.TryGuardar());
        Assert.NotNull(vm.Editor);
        Assert.Empty(db.Solicitudes);
    }

    [Fact]
    public void TryGuardar_CuandoFallaLaBase_DevuelveFalseYNoCierraElPanel()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db, dialogo: new DialogoFalso());
        vm.NuevaCommand.Execute(null);
        vm.RellenarObligatorios("Umbrella", "Soporte");
        db.Database.ExecuteSqlRaw("DROP TABLE Solicitudes");

        Assert.False(vm.TryGuardar());
        Assert.NotNull(vm.Editor);
    }

    [Fact]
    public void TryGuardar_SinPanelAbierto_DevuelveFalse()
    {
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(db, dialogo: new DialogoFalso());

        Assert.False(vm.TryGuardar());
    }

    [Fact]
    public void ResolverCambiosPendientes_SinEditor_DevuelveTrueYNoPregunta()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso();
        using var vm = new MainViewModel(db, dialogo: dialogo);

        Assert.True(vm.ResolverCambiosPendientes());
        Assert.Empty(dialogo.PreguntasGuardar);
    }

    [Fact]
    public void ResolverCambiosPendientes_SinCambios_DevuelveTrueYNoPregunta()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso();
        using var vm = new MainViewModel(db, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);

        Assert.True(vm.ResolverCambiosPendientes());
        Assert.Empty(dialogo.PreguntasGuardar);
    }

    [Fact]
    public void ResolverCambiosPendientes_Guardar_PersisteYCierraElPanel()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso { RespuestaPreguntarGuardar = ResultadoGuardar.Guardar };
        using var vm = new MainViewModel(db, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);
        vm.RellenarObligatorios("Globex", "Analista");

        Assert.True(vm.ResolverCambiosPendientes());
        Assert.Single(dialogo.PreguntasGuardar);
        Assert.Null(vm.Editor);
        Assert.Equal("Globex", Assert.Single(db.Solicitudes).Empresa);
    }

    [Fact]
    public void ResolverCambiosPendientes_GuardarConFallo_DevuelveFalseYNoCierraElPanel()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso { RespuestaPreguntarGuardar = ResultadoGuardar.Guardar };
        using var vm = new MainViewModel(db, dialogo: dialogo);
        vm.NuevaCommand.Execute(null);
        vm.RellenarObligatorios("Umbrella", "Soporte");
        db.Database.ExecuteSqlRaw("DROP TABLE Solicitudes");

        Assert.False(vm.ResolverCambiosPendientes());
        Assert.NotNull(vm.Editor);
    }

    [Fact]
    public void ResolverCambiosPendientes_Descartar_LimpiaAdjuntosYDesenganchaLaEntidad()
    {
        using var db = TestDb.NuevoContexto();
        string carpeta = TestDb.NuevaCarpeta();
        var dialogo = new DialogoFalso { RespuestaPreguntarGuardar = ResultadoGuardar.Descartar };
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
        string viejo = solicitud.RutaCv!;
        string nuevo = TestDb.CrearAdjuntoEn(carpeta, "cv-nuevo.pdf");
        vm.Editor!.RutaCv = nuevo;

        Assert.True(vm.ResolverCambiosPendientes());
        Assert.Equal(EntityState.Detached, db.Entry(solicitud).State);
        Assert.True(File.Exists(viejo));
        Assert.False(File.Exists(nuevo));
        Assert.Equal(viejo, db.Solicitudes.Single().RutaCv);
    }

    [Fact]
    public void ResolverCambiosPendientes_Cancelar_DevuelveFalseYNoTocaNada()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso { RespuestaPreguntarGuardar = ResultadoGuardar.Cancelar };
        using var vm = new MainViewModel(db, dialogo: dialogo);
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;
        vm.Editor!.Empresa = "SinGuardar";

        Assert.False(vm.ResolverCambiosPendientes());
        Assert.NotNull(vm.Editor);
        Assert.Equal("ACME", db.Solicitudes.Single().Empresa);
    }

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
    public void Guardar_ReaperturaConfirmada_MuestraElCierreYRegistraElHito()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso { RespuestaConfirmar = true };
        using var vm = new MainViewModel(db, dialogo: dialogo);
        DateTime fechaCierre = new(2026, 10, 10);
        var solicitud = new SolicitudBuilder()
            .ConEmpresa("ACME")
            .ConPuesto("Dev")
            .ConEstado(EstadoSolicitud.OfertaRechazada)
            .ConMotivoRechazo(MotivoRechazo.Salario)
            .ConFechaCierre(fechaCierre)
            .Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;
        vm.Editor!.Estado = EstadoSolicitud.EntrevistaTecnica;
        vm.Editor.NotaReapertura = "Retomamos el proceso";

        vm.GuardarCommand.Execute(null);

        string confirmacion = Assert.Single(dialogo.Confirmaciones);
        Assert.Contains("Estado anterior: Oferta rechazada por mí", confirmacion);
        Assert.Contains($"Fecha de cierre: {fechaCierre.ToString("d", Localizacion.CulturaActual)}", confirmacion);
        Assert.Contains("Motivo de cierre: Salario / pretensión", confirmacion);
        Assert.Contains("Nuevo estado: Entrevista técnica", confirmacion);
        Assert.Contains("Nota: Retomamos el proceso", confirmacion);
        Assert.Equal(EstadoSolicitud.EntrevistaTecnica, solicitud.Estado);
        Assert.Null(solicitud.FechaCierre);
        Assert.Null(solicitud.MotivoRechazo);
        Assert.Equal(DateTime.Today, solicitud.FechaReapertura);
        Evento hito = Assert.Single(solicitud.Eventos);
        Assert.Equal(TipoEvento.Reapertura, hito.Tipo);
        Assert.Contains("Retomamos el proceso", hito.Descripcion);
        Assert.Null(vm.Editor);
    }

    [Fact]
    public void Guardar_ReaperturaCancelada_ConservaElCierreYLaEdicion()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso();
        using var vm = new MainViewModel(db, dialogo: dialogo);
        DateTime fechaCierre = new(2026, 10, 10);
        var solicitud = new SolicitudBuilder()
            .ConEmpresa("ACME")
            .ConPuesto("Dev")
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConMotivoRechazo(MotivoRechazo.Salario)
            .ConFechaCierre(fechaCierre)
            .Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;
        vm.Editor!.Estado = EstadoSolicitud.Enviada;
        vm.Editor.NotaReapertura = "Retomamos el proceso";

        vm.GuardarCommand.Execute(null);

        Assert.Single(dialogo.Confirmaciones);
        Assert.Equal(EstadoSolicitud.Rechazada, solicitud.Estado);
        Assert.Equal(fechaCierre, solicitud.FechaCierre);
        Assert.Equal(MotivoRechazo.Salario, solicitud.MotivoRechazo);
        Assert.False(solicitud.FueReabierta);
        Assert.NotNull(vm.Editor);
    }

    [Fact]
    public void Guardar_ReaperturaDesdeOfertaAceptada_UsaConfirmacionReforzada()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso { RespuestaConfirmar = true };
        using var vm = new MainViewModel(db, dialogo: dialogo);
        var solicitud = new SolicitudBuilder()
            .ConEmpresa("ACME")
            .ConPuesto("Dev")
            .ConEstado(EstadoSolicitud.OfertaAceptada)
            .ConFechaCierre(null)
            .Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;
        vm.Editor!.Estado = EstadoSolicitud.Enviada;
        vm.Editor.NotaReapertura = "Retomamos el proceso";

        vm.GuardarCommand.Execute(null);

        string confirmacion = Assert.Single(dialogo.Confirmaciones);
        Assert.Contains("Habías aceptado esta oferta. ¿Seguro", confirmacion);
        Assert.Contains("Fecha de cierre: Sin fecha", confirmacion);
        Assert.Contains("Motivo de cierre: No aplica", confirmacion);
        Assert.Contains("Nota: Retomamos el proceso", confirmacion);
        Assert.Equal(EstadoSolicitud.Enviada, solicitud.Estado);
        Assert.Equal(TipoEvento.Reapertura, Assert.Single(solicitud.Eventos).Tipo);
    }

    [Fact]
    public void Guardar_ReaperturaASinRespuesta_AvisaYNoReabre()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso { RespuestaConfirmar = true };
        using var vm = new MainViewModel(db, dialogo: dialogo);
        var solicitud = new SolicitudBuilder()
            .ConEmpresa("ACME")
            .ConPuesto("Dev")
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConMotivoRechazo(MotivoRechazo.Salario)
            .Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;
        vm.Editor!.Estado = EstadoSolicitud.SinRespuesta;
        vm.Editor.NotaReapertura = "Retomamos el proceso";

        vm.GuardarCommand.Execute(null);

        Assert.Single(dialogo.Avisos);
        Assert.Empty(dialogo.Confirmaciones);
        Assert.Equal(EstadoSolicitud.Rechazada, solicitud.Estado);
        Assert.False(solicitud.FueReabierta);
        Assert.Contains(
            vm.Editor.GetErrors(nameof(vm.Editor.Estado)),
            error => error.ErrorMessage == Localizacion.Texto("Mensaje.ReaperturaSinRespuesta"));
    }

    [Fact]
    public void Guardar_ReaperturaSinNota_AvisaYNoReabre()
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso { RespuestaConfirmar = true };
        using var vm = new MainViewModel(db, dialogo: dialogo);
        var solicitud = new SolicitudBuilder()
            .ConEmpresa("ACME")
            .ConPuesto("Dev")
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConMotivoRechazo(MotivoRechazo.Salario)
            .Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;
        vm.Editor!.Estado = EstadoSolicitud.EntrevistaTecnica;

        vm.GuardarCommand.Execute(null);

        Assert.Single(dialogo.Avisos);
        Assert.Empty(dialogo.Confirmaciones);
        Assert.Equal(EstadoSolicitud.Rechazada, solicitud.Estado);
        Assert.False(solicitud.FueReabierta);
        Assert.Contains(
            vm.Editor.GetErrors(nameof(vm.Editor.NotaReapertura)),
            error => error.ErrorMessage == Localizacion.Texto("Mensaje.NotaReaperturaObligatoria"));
    }

    [Fact]
    public void Guardar_ReaperturaQueFallaAlGuardar_ElReintentoNoLaRepite()
    {
        string ruta = TestDb.NuevaRuta();
        using var db = new ContextoQueFallaUnaVez(ruta);
        db.Database.EnsureCreated();
        var dialogo = new DialogoFalso { RespuestaConfirmar = true };
        using var vm = new MainViewModel(db, dialogo: dialogo);
        DateTime fechaCierre = new(2026, 10, 10);
        var solicitud = new SolicitudBuilder()
            .ConEmpresa("ACME")
            .ConPuesto("Dev")
            .ConFechaSolicitud(new DateTime(2026, 10, 1))
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConMotivoRechazo(MotivoRechazo.Salario)
            .ConFechaCierre(fechaCierre)
            .Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;
        vm.Editor!.Estado = EstadoSolicitud.EntrevistaTecnica;
        vm.Editor.NotaReapertura = "Retomamos el proceso";
        db.Fallar = true;

        vm.GuardarCommand.Execute(null);

        Assert.Single(dialogo.Errores);
        Assert.NotNull(vm.Editor);
        Assert.Equal(EstadoSolicitud.EntrevistaTecnica, solicitud.Estado);
        Assert.Null(solicitud.FechaCierre);

        vm.GuardarCommand.Execute(null);

        Assert.Single(dialogo.Errores);
        Assert.Single(dialogo.Confirmaciones);
        Assert.Null(vm.Editor);
        Assert.Equal(EstadoSolicitud.EntrevistaTecnica, solicitud.Estado);
        Assert.Null(solicitud.FechaCierre);
        Assert.Equal(DateTime.Today, solicitud.FechaPrimeraRespuesta);
        Assert.Equal(DateTime.Today.AddDays(7), solicitud.ProximoSeguimiento);
        Assert.Equal(TipoEvento.Reapertura, solicitud.Eventos[^1].Tipo);
    }

    [Fact]
    public void Guardar_CierreQueFallaAlGuardar_ElReintentoConservaLaFechaDeCierre()
    {
        string ruta = TestDb.NuevaRuta();
        using var db = new ContextoQueFallaUnaVez(ruta);
        db.Database.EnsureCreated();
        var dialogo = new DialogoFalso();
        using var vm = new MainViewModel(db, dialogo: dialogo);
        var solicitud = new SolicitudBuilder()
            .ConEmpresa("ACME")
            .ConPuesto("Dev")
            .ConEstado(EstadoSolicitud.Enviada)
            .Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;
        vm.Editor!.Estado = EstadoSolicitud.Rechazada;
        vm.Editor.MotivoRechazo = MotivoRechazo.Salario;
        db.Fallar = true;

        vm.GuardarCommand.Execute(null);

        Assert.Single(dialogo.Errores);
        Assert.NotNull(vm.Editor);

        vm.GuardarCommand.Execute(null);

        Assert.Null(vm.Editor);
        Assert.Equal(DateTime.Today, solicitud.FechaCierre);
        Assert.Equal(1, solicitud.Eventos.Count(evento => evento.Tipo == TipoEvento.Rechazo));
    }

    [Fact]
    public void Guardar_SalidaDeSinRespuestaQueFallaAlGuardar_ElReintentoConservaLaPrimeraRespuesta()
    {
        string ruta = TestDb.NuevaRuta();
        using var db = new ContextoQueFallaUnaVez(ruta);
        db.Database.EnsureCreated();
        var dialogo = new DialogoFalso();
        using var vm = new MainViewModel(db, dialogo: dialogo);
        var solicitud = new SolicitudBuilder()
            .ConEmpresa("ACME")
            .ConPuesto("Dev")
            .ConEstado(EstadoSolicitud.SinRespuesta)
            .Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;
        vm.Editor!.Estado = EstadoSolicitud.EnRevision;
        db.Fallar = true;

        vm.GuardarCommand.Execute(null);

        Assert.Single(dialogo.Errores);
        Assert.NotNull(vm.Editor);

        vm.GuardarCommand.Execute(null);

        Assert.Null(vm.Editor);
        Assert.Equal(DateTime.Today, solicitud.FechaPrimeraRespuesta);
        Assert.Equal(1, solicitud.Eventos.Count(evento => evento.Tipo == TipoEvento.Respuesta));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Guardar_ReaperturaRespetaElSeguimientoEditado(int caso)
    {
        using var db = TestDb.NuevoContexto();
        var dialogo = new DialogoFalso { RespuestaConfirmar = true };
        using var vm = new MainViewModel(db, dialogo: dialogo);
        DateTime seguimientoOriginal = new(2026, 10, 5);
        DateTime seguimientoEditado = new(2026, 10, 1);
        var solicitud = new SolicitudBuilder()
            .ConEmpresa("Empresa R.12g")
            .ConPuesto("Puesto R.12g")
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConMotivoRechazo(MotivoRechazo.Salario)
            .ConProximoSeguimiento(seguimientoOriginal)
            .Construir();
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        vm.Recargar();
        vm.SolicitudSeleccionada = solicitud;
        vm.Editor!.Estado = EstadoSolicitud.EntrevistaTecnica;
        vm.Editor.NotaReapertura = "Nota de reapertura R.12g";

        if (caso == 1)
        {
            vm.Editor.ProximoSeguimiento = seguimientoEditado;
        }
        else if (caso == 2)
        {
            vm.Editor.ProximoSeguimiento = null;
        }

        vm.GuardarCommand.Execute(null);

        DateTime esperado = caso == 1 ? seguimientoEditado : DateTime.Today.AddDays(7);
        Assert.Equal(esperado, solicitud.ProximoSeguimiento);
        Assert.Null(vm.Editor);
    }

    /// <summary>Contexto que falla la primera vez que se guarda, para probar el reintento.</summary>
    private sealed class ContextoQueFallaUnaVez(string ruta) : AppDbContext(ruta)
    {
        public bool Fallar { get; set; }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            if (this.Fallar)
            {
                this.Fallar = false;
                throw new InvalidOperationException("fallo simulado");
            }

            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
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
