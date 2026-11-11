namespace GestorSolicitudes.Tests;

using System.ComponentModel.DataAnnotations;
using GestorSolicitudes.Models;
using GestorSolicitudes.ViewModels;

public class SolicitudEdicionTests
{
    [Fact]
    public void Validacion_SinEmpresaOPuesto_MarcaLosDosCamposConElMensajeDeFaltanDatos()
    {
        var editor = new SolicitudEdicionViewModel();

        Assert.True(editor.HasErrors);
        Assert.Equal(
            Localizacion.Texto("Mensaje.FaltanDatos"),
            Assert.Single(editor.GetErrors(nameof(editor.Empresa)).Cast<ValidationResult>()).ErrorMessage);
        Assert.Equal(
            Localizacion.Texto("Mensaje.FaltanDatos"),
            Assert.Single(editor.GetErrors(nameof(editor.Puesto)).Cast<ValidationResult>()).ErrorMessage);
    }

    [Fact]
    public void Validacion_AlRellenarEmpresaYPuesto_DejaDeHaberErrores()
    {
        var editor = new SolicitudEdicionViewModel { Empresa = "ACME", Puesto = "Dev" };

        Assert.False(editor.HasErrors);
        Assert.Empty(editor.GetErrors(nameof(editor.Empresa)));
        Assert.Empty(editor.GetErrors(nameof(editor.Puesto)));
    }

    [Fact]
    public void Validacion_TextoQueSuperaElMaximo_MarcaElCampo()
    {
        var editor = new SolicitudEdicionViewModel
        {
            Empresa = "ACME",
            Puesto = "Dev",
            Portal = new string('x', Solicitud.LongitudMaximaPortal + 1),
        };

        editor.Validar();

        Assert.True(editor.HasErrors);
        Assert.NotEmpty(editor.GetErrors(nameof(editor.Portal)));
    }

    [Fact]
    public void Validacion_NotaReaperturaQueSuperaElMaximo_MarcaElCampo()
    {
        var editor = new SolicitudEdicionViewModel
        {
            NotaReapertura = new string('x', Solicitud.LongitudMaximaNotaReapertura + 1),
        };

        editor.Validar();

        Assert.NotEmpty(editor.GetErrors(nameof(editor.NotaReapertura)));
    }

    [Fact]
    public void Validacion_SalarioMinimoMayorQueElMaximo_MarcaElCampo()
    {
        var editor = new SolicitudEdicionViewModel
        {
            Empresa = "ACME",
            Puesto = "Dev",
            SalarioMin = 45000,
            SalarioMax = 30000,
        };

        editor.Validar();

        Assert.True(editor.HasErrors);
        Assert.Equal(
            Localizacion.Texto("Mensaje.SalarioMinimoMayor"),
            Assert.Single(editor.GetErrors(nameof(editor.SalarioMax)).Cast<ValidationResult>()).ErrorMessage);
    }

    [Fact]
    public void HayCambios_RecienAbierta_EsFalse()
    {
        var editor = new SolicitudEdicionViewModel(new SolicitudBuilder().ConEmpresa("ACME").Construir());

        Assert.False(editor.HayCambios);
    }

    [Fact]
    public void HayCambios_AlTocarUnCampo_EsTrue()
    {
        var editor = new SolicitudEdicionViewModel(new SolicitudBuilder().Construir());

        editor.Notas = "Hablar el lunes";

        Assert.True(editor.HayCambios);
    }

    [Fact]
    public void HayCambios_AsignarElMismoValor_NoCuenta()
    {
        var editor = new SolicitudEdicionViewModel(new SolicitudBuilder().ConEmpresa("ACME").Construir());

        editor.Empresa = "ACME";

        Assert.False(editor.HayCambios);
    }

    [Fact]
    public void HayCambios_AlEditarUnHito_EsTrue()
    {
        var solicitud = new SolicitudBuilder().Construir();
        solicitud.AgregarEvento(new Evento(new DateTime(2026, 3, 5), TipoEvento.Nota, "Primera nota"));
        var editor = new SolicitudEdicionViewModel(solicitud);

        editor.EventosEdicion[0].Descripcion = "Otra nota";

        Assert.True(editor.HayCambios);
    }

    [Fact]
    public void HayCambios_AlAnadirUnHito_EsTrue()
    {
        var editor = new SolicitudEdicionViewModel(new SolicitudBuilder().Construir());

        editor.EventosEdicion.Add(new EventoViewModel());

        Assert.True(editor.HayCambios);
    }

    [Fact]
    public void HayCambios_AlQuitarUnHito_EsTrue()
    {
        Solicitud solicitud = new SolicitudBuilder()
            .ConEventos(new Evento(new DateTime(2026, 3, 5), TipoEvento.Nota, "Primera nota"))
            .Construir();
        var editor = new SolicitudEdicionViewModel(solicitud);

        editor.EliminarEvento(editor.EventosEdicion[0]);

        Assert.True(editor.HayCambios);
    }

    [Fact]
    public void HayCambios_AlVolverACargar_SeApaga()
    {
        var editor = new SolicitudEdicionViewModel(new SolicitudBuilder().Construir());
        editor.Notas = "Hablar el lunes";

        editor.CargarDesde(new SolicitudBuilder().Construir());

        Assert.False(editor.HayCambios);
    }

    [Fact]
    public void HayCambios_SeNotificaAlPasarAVerdadero()
    {
        var editor = new SolicitudEdicionViewModel(new SolicitudBuilder().Construir());
        var notificados = new List<string?>();
        editor.PropertyChanged += (_, e) => notificados.Add(e.PropertyName);

        editor.Notas = "Hablar el lunes";

        Assert.Single(notificados, n => n == nameof(editor.HayCambios));
    }

    [Fact]
    public void CargarDesde_CopiaIdentificacion()
    {
        Solicitud solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir();
        var editor = new SolicitudEdicionViewModel();

        editor.CargarDesde(solicitud);

        Assert.Equal("ACME", editor.Empresa);
        Assert.Equal("Dev", editor.Puesto);
    }

    [Fact]
    public void CargarDesde_CopiaElHistorialComoEventosEditables()
    {
        var solicitud = new SolicitudBuilder().Construir();
        solicitud.AgregarEvento(new Evento(
            new DateTime(2026, 3, 5),
            TipoEvento.Entrevista,
            "Entrevista con RR. HH."));
        var editor = new SolicitudEdicionViewModel();

        editor.CargarDesde(solicitud);

        EventoViewModel copia = Assert.Single(editor.EventosEdicion);
        Assert.Equal(new DateTime(2026, 3, 5), copia.Fecha);
        Assert.Equal(TipoEvento.Entrevista, copia.Tipo);
        Assert.Equal("Entrevista con RR. HH.", copia.Descripcion);
    }

    [Fact]
    public void AplicarIdentificacion_VuelcaEmpresaYPuesto()
    {
        Solicitud solicitud = new SolicitudBuilder().Construir();
        var editor = new SolicitudEdicionViewModel { Empresa = "  ACME  ", Puesto = "  Dev  " };

        editor.AplicarIdentificacion(solicitud);

        Assert.Equal("ACME", solicitud.Empresa);
        Assert.Equal("Dev", solicitud.Puesto);
    }

    [Fact]
    public void CargarDesde_CopiaLaOferta()
    {
        Solicitud solicitud = new SolicitudBuilder()
            .ConEnlaceOferta("https://ejemplo.test/oferta")
            .ConPortal("LinkedIn")
            .ConOrigen(Origen.Referido)
            .ConUbicacion("Madrid")
            .ConModalidad(Modalidad.Remoto)
            .ConTecnologias("C#, .NET")
            .ConRequisitos("3 años de experiencia")
            .Construir();
        var editor = new SolicitudEdicionViewModel();

        editor.CargarDesde(solicitud);

        Assert.Equal("https://ejemplo.test/oferta", editor.EnlaceOferta);
        Assert.Equal("LinkedIn", editor.Portal);
        Assert.Equal(Origen.Referido, editor.Origen);
        Assert.Equal("Madrid", editor.Ubicacion);
        Assert.Equal(Modalidad.Remoto, editor.Modalidad);
        Assert.Equal("C#, .NET", editor.Tecnologias);
        Assert.Equal("3 años de experiencia", editor.Requisitos);
    }

    [Fact]
    public void AplicarOferta_VuelcaLaOferta()
    {
        Solicitud solicitud = new SolicitudBuilder().Construir();
        var editor = new SolicitudEdicionViewModel
        {
            EnlaceOferta = "  https://ejemplo.test/oferta  ",
            Portal = "  LinkedIn  ",
            Origen = Origen.Referido,
            Ubicacion = "  Madrid  ",
            Modalidad = Modalidad.Remoto,
            Tecnologias = "  C#, .NET  ",
            Requisitos = "  3 años de experiencia  ",
        };

        editor.AplicarOferta(solicitud);

        Assert.Equal("https://ejemplo.test/oferta", solicitud.EnlaceOferta);
        Assert.Equal("LinkedIn", solicitud.Portal);
        Assert.Equal(Origen.Referido, solicitud.Origen);
        Assert.Equal("Madrid", solicitud.Ubicacion);
        Assert.Equal(Modalidad.Remoto, solicitud.Modalidad);
        Assert.Equal("C#, .NET", solicitud.Tecnologias);
        Assert.Equal("3 años de experiencia", solicitud.Requisitos);
    }

    [Fact]
    public void CargarDesde_CopiaElEconomicoYElInteres()
    {
        Solicitud solicitud = new SolicitudBuilder()
            .ConSalarioMin(30000)
            .ConSalarioMax(45000)
            .ConPretensionSalarial(40000)
            .ConInteres(5)
            .Construir();
        var editor = new SolicitudEdicionViewModel();

        editor.CargarDesde(solicitud);

        Assert.Equal(30000, editor.SalarioMin);
        Assert.Equal(45000, editor.SalarioMax);
        Assert.Equal(40000, editor.PretensionSalarial);
        Assert.Equal(5, editor.Interes);
    }

    [Fact]
    public void AplicarEconomico_VuelcaNulos()
    {
        Solicitud solicitud = new SolicitudBuilder()
            .ConSalarioMin(30000)
            .ConSalarioMax(45000)
            .ConPretensionSalarial(40000)
            .Construir();
        var editor = new SolicitudEdicionViewModel { SalarioMin = null, SalarioMax = null, PretensionSalarial = null };

        editor.AplicarEconomico(solicitud);

        Assert.Null(solicitud.SalarioMin);
        Assert.Null(solicitud.SalarioMax);
        Assert.Null(solicitud.PretensionSalarial);
        Assert.Equal(3, solicitud.Interes);
    }

    [Fact]
    public void CargarDesde_CopiaElSeguimiento()
    {
        DateTime fecha = new(2026, 1, 10);
        Solicitud solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConMotivoRechazo(MotivoRechazo.Salario)
            .ConFechaSolicitud(fecha)
            .ConFechaPrimeraRespuesta(fecha.AddDays(1))
            .ConFechaEntrevista(fecha.AddDays(2))
            .ConProximoSeguimiento(fecha.AddDays(3))
            .ConFechaCierre(fecha.AddDays(4))
            .ConRespuestaEmpresa("Buscamos otro perfil")
            .Construir();
        var editor = new SolicitudEdicionViewModel();

        editor.CargarDesde(solicitud);

        Assert.Equal(EstadoSolicitud.Rechazada, editor.Estado);
        Assert.Equal(MotivoRechazo.Salario, editor.MotivoRechazo);
        Assert.Equal(fecha, editor.FechaSolicitud);
        Assert.Equal(fecha.AddDays(1), editor.FechaPrimeraRespuesta);
        Assert.Equal(fecha.AddDays(2), editor.FechaEntrevista);
        Assert.Equal(fecha.AddDays(3), editor.ProximoSeguimiento);
        Assert.Equal(fecha.AddDays(4), editor.FechaCierre);
        Assert.Equal("Buscamos otro perfil", editor.RespuestaEmpresa);
    }

    [Fact]
    public void AplicarEstado_VuelcaEstadoMotivoYFechaCierre()
    {
        DateTime cierre = new(2026, 2, 10);
        Solicitud solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Enviada)
            .Construir();
        var editor = new SolicitudEdicionViewModel
        {
            Estado = EstadoSolicitud.OfertaRecibida,
            MotivoRechazo = null,
            FechaCierre = cierre,
        };

        editor.AplicarEstado(solicitud);

        Assert.Equal(EstadoSolicitud.OfertaRecibida, solicitud.Estado);
        Assert.Null(solicitud.MotivoRechazo);
        Assert.Equal(cierre, solicitud.FechaCierre);
    }

    [Fact]
    public void AplicarFechas_VuelcaEnvioEntrevistaYSeguimiento()
    {
        DateTime fecha = new(2026, 2, 1);
        Solicitud solicitud = new SolicitudBuilder()
            .ConFechaPrimeraRespuesta(fecha.AddDays(-1))
            .ConFechaCierre(fecha.AddDays(-2))
            .Construir();
        var editor = new SolicitudEdicionViewModel
        {
            FechaSolicitud = fecha,
            FechaPrimeraRespuesta = fecha.AddDays(1),
            FechaEntrevista = fecha.AddDays(2),
            ProximoSeguimiento = fecha.AddDays(3),
            FechaCierre = fecha.AddDays(4),
        };

        editor.AplicarFechas(solicitud);

        Assert.Equal(fecha, solicitud.FechaSolicitud);
        Assert.Equal(fecha.AddDays(2), solicitud.FechaEntrevista);
        Assert.Equal(fecha.AddDays(3), solicitud.ProximoSeguimiento);
        Assert.Equal(fecha.AddDays(-1), solicitud.FechaPrimeraRespuesta);
        Assert.Equal(fecha.AddDays(-2), solicitud.FechaCierre);
    }

    [Fact]
    public void AplicarRespuesta_VuelcaFechaYRespuestaDeLaEmpresa()
    {
        DateTime fecha = new(2026, 3, 1);
        Solicitud solicitud = new SolicitudBuilder()
            .ConFechaPrimeraRespuesta(fecha.AddDays(-1))
            .ConRespuestaEmpresa("Antigua")
            .Construir();
        var editor = new SolicitudEdicionViewModel
        {
            FechaPrimeraRespuesta = fecha,
            RespuestaEmpresa = "Oferta enviada",
        };

        editor.AplicarRespuesta(solicitud);

        Assert.Equal(fecha, solicitud.FechaPrimeraRespuesta);
        Assert.Equal("Oferta enviada", solicitud.RespuestaEmpresa);
    }

    [Fact]
    public void AplicarA_SalirDeSinRespuesta_ConservaLaFechaQueRellenaElEstado()
    {
        Solicitud solicitud = new SolicitudBuilder()
            .ConEmpresa("ACME")
            .ConPuesto("Dev")
            .ConEstado(EstadoSolicitud.SinRespuesta)
            .Construir();
        var editor = new SolicitudEdicionViewModel();
        editor.CargarDesde(solicitud);
        editor.Estado = EstadoSolicitud.EntrevistaTecnica;

        editor.AplicarA(solicitud);

        Assert.Equal(EstadoSolicitud.EntrevistaTecnica, solicitud.Estado);
        Assert.Equal(DateTime.Today, solicitud.FechaPrimeraRespuesta);
        Assert.Contains(solicitud.Eventos, e => e.Tipo == TipoEvento.Respuesta);
    }

    [Fact]
    public void CargarDesde_CopiaContactoYNotas()
    {
        Solicitud solicitud = new SolicitudBuilder()
            .ConContactoNombre("Ana")
            .ConContactoEmail("ana@ejemplo.test")
            .ConContactoTelefono("600123456")
            .ConNotas("Hablar el lunes")
            .Construir();
        var editor = new SolicitudEdicionViewModel();

        editor.CargarDesde(solicitud);

        Assert.Equal("Ana", editor.ContactoNombre);
        Assert.Equal("ana@ejemplo.test", editor.ContactoEmail);
        Assert.Equal("600123456", editor.ContactoTelefono);
        Assert.Equal("Hablar el lunes", editor.Notas);
    }

    [Fact]
    public void CargarDesde_VaciaLaNotaReapertura()
    {
        var editor = new SolicitudEdicionViewModel { NotaReapertura = "Retomamos el proceso" };

        editor.CargarDesde(new SolicitudBuilder().Construir());

        Assert.Null(editor.NotaReapertura);
    }

    [Fact]
    public void ReaperturaResumen_SinHitoDeReapertura_EsNulo()
    {
        var editor = new SolicitudEdicionViewModel(new SolicitudBuilder().Construir());

        Assert.Null(editor.ReaperturaResumen);
    }

    [Fact]
    public void ReaperturaResumen_LeeElUltimoHitoSinRepetirElPrefijo()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConFechaCierre(new DateTime(2026, 10, 10))
            .Construir();
        solicitud.Reabrir(EstadoSolicitud.Enviada, "Retomamos el proceso");

        var editor = new SolicitudEdicionViewModel(solicitud);

        Assert.Equal(
            string.Format(
                Localizacion.Texto("Seguimiento.ReaperturaResumen"),
                DateTime.Today.ToString("d", Localizacion.CulturaActual),
                "Antes: Descartado por la empresa (10/10, No indicado). Retomamos el proceso"),
            editor.ReaperturaResumen);
    }

    [Fact]
    public void ReaperturaResumen_SinPrefijo_UsaLaDescripcionEntera()
    {
        var solicitud = new SolicitudBuilder().Construir();
        solicitud.AgregarEvento(new Evento(new DateTime(2026, 10, 10), TipoEvento.Reapertura, "Retomamos el proceso"));

        var editor = new SolicitudEdicionViewModel(solicitud);

        Assert.Equal(
            string.Format(
                Localizacion.Texto("Seguimiento.ReaperturaResumen"),
                new DateTime(2026, 10, 10).ToString("d", Localizacion.CulturaActual),
                "Retomamos el proceso"),
            editor.ReaperturaResumen);
    }

    [Fact]
    public void EliminarEvento_Reapertura_DejaElResumenVacio()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Rechazada)
            .Construir();
        solicitud.Reabrir(EstadoSolicitud.Enviada, "Retomamos el proceso");
        var editor = new SolicitudEdicionViewModel(solicitud);

        editor.EliminarEvento(editor.EventosEdicion.Single(e => e.Tipo == TipoEvento.Reapertura));

        Assert.Null(editor.ReaperturaResumen);
    }

    [Fact]
    public void CargarDesde_NotificaElResumenDeReapertura()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Rechazada)
            .Construir();
        solicitud.Reabrir(EstadoSolicitud.Enviada, "Retomamos el proceso");
        var editor = new SolicitudEdicionViewModel();
        var notificados = new List<string?>();
        editor.PropertyChanged += (_, e) => notificados.Add(e.PropertyName);

        editor.CargarDesde(solicitud);

        Assert.Contains(nameof(editor.ReaperturaResumen), notificados);
    }

    [Fact]
    public void AplicarContacto_VuelcaContactoYNotas()
    {
        Solicitud solicitud = new SolicitudBuilder().Construir();
        var editor = new SolicitudEdicionViewModel
        {
            ContactoNombre = "Ana",
            ContactoEmail = "ana@ejemplo.test",
            ContactoTelefono = "600123456",
            Notas = "Hablar el lunes",
        };

        editor.AplicarContacto(solicitud);

        Assert.Equal("Ana", solicitud.ContactoNombre);
        Assert.Equal("ana@ejemplo.test", solicitud.ContactoEmail);
        Assert.Equal("600123456", solicitud.ContactoTelefono);
        Assert.Equal("Hablar el lunes", solicitud.Notas);
    }

    [Fact]
    public void AplicarContacto_RecortaEspacios()
    {
        Solicitud solicitud = new SolicitudBuilder().Construir();
        var editor = new SolicitudEdicionViewModel
        {
            ContactoNombre = "  Ana  ",
            ContactoEmail = "  ana@ejemplo.test  ",
            ContactoTelefono = "  600123456  ",
            Notas = "  Hablar el lunes  ",
        };

        editor.AplicarContacto(solicitud);

        Assert.Equal("Ana", solicitud.ContactoNombre);
        Assert.Equal("ana@ejemplo.test", solicitud.ContactoEmail);
        Assert.Equal("600123456", solicitud.ContactoTelefono);
        Assert.Equal("Hablar el lunes", solicitud.Notas);
    }

    [Fact]
    public void CargarDesde_CopiaLosAdjuntos()
    {
        Solicitud solicitud = new SolicitudBuilder()
            .ConRutaCv(@"C:\adjuntos\abc.pdf")
            .ConNombreOriginalCv("mi-cv.pdf")
            .ConRutaCarta(@"C:\adjuntos\def.pdf")
            .Construir();
        var editor = new SolicitudEdicionViewModel();

        editor.CargarDesde(solicitud);

        Assert.Equal(@"C:\adjuntos\abc.pdf", editor.RutaCv);
        Assert.Equal("mi-cv.pdf", editor.NombreOriginalCv);
        Assert.Equal(@"C:\adjuntos\def.pdf", editor.RutaCarta);
        Assert.Null(editor.NombreOriginalCarta);
    }

    [Fact]
    public void AdjuntoCv_SinRuta_MuestraElTextoDeSinAdjuntar()
    {
        var editor = new SolicitudEdicionViewModel();

        Assert.Null(editor.AdjuntoCv.Ruta);
        Assert.Equal(Localizacion.Texto("Adjuntos.CurriculumSinAdjuntar"), editor.AdjuntoCv.Nombre);
    }

    [Fact]
    public void AdjuntoCv_UsaElNombreOriginalYSinNoHayElDelFichero()
    {
        var editor = new SolicitudEdicionViewModel { RutaCv = @"C:\adjuntos\abc.pdf", NombreOriginalCv = "mi-cv.pdf" };
        var sinNombre = new SolicitudEdicionViewModel { RutaCv = @"C:\adjuntos\abc.pdf" };

        Assert.Equal("mi-cv.pdf", editor.AdjuntoCv!.Nombre);
        Assert.Equal("abc.pdf", sinNombre.AdjuntoCv!.Nombre);
    }

    [Fact]
    public void AdjuntoCv_NotificaAlCambiarLaRutaONombre()
    {
        var editor = new SolicitudEdicionViewModel();
        var notificados = new List<string?>();
        editor.PropertyChanged += (_, e) => notificados.Add(e.PropertyName);

        editor.RutaCv = @"C:\adjuntos\abc.pdf";
        editor.NombreOriginalCv = "mi-cv.pdf";

        Assert.Equal(2, notificados.Count(nombre => nombre == nameof(editor.AdjuntoCv)));
    }

    [Fact]
    public void AplicarAdjuntos_VuelcaRutasYNombres()
    {
        Solicitud solicitud = new SolicitudBuilder().Construir();
        var editor = new SolicitudEdicionViewModel
        {
            RutaCv = @"C:\adjuntos\abc.pdf",
            NombreOriginalCv = "mi-cv.pdf",
            RutaCarta = @"C:\adjuntos\def.pdf",
            NombreOriginalCarta = "mi-carta.pdf",
        };

        editor.AplicarAdjuntos(solicitud);

        Assert.Equal(@"C:\adjuntos\abc.pdf", solicitud.RutaCv);
        Assert.Equal("mi-cv.pdf", solicitud.NombreOriginalCv);
        Assert.Equal(@"C:\adjuntos\def.pdf", solicitud.RutaCarta);
        Assert.Equal("mi-carta.pdf", solicitud.NombreOriginalCarta);
    }

    [Fact]
    public void AplicarAdjuntos_RecortaEspacios()
    {
        Solicitud solicitud = new SolicitudBuilder().Construir();
        var editor = new SolicitudEdicionViewModel
        {
            RutaCv = @"  C:\adjuntos\abc.pdf  ",
            NombreOriginalCv = "  mi-cv.pdf  ",
            RutaCarta = @"  C:\adjuntos\def.pdf  ",
            NombreOriginalCarta = "  mi-carta.pdf  ",
        };

        editor.AplicarAdjuntos(solicitud);

        Assert.Equal(@"C:\adjuntos\abc.pdf", solicitud.RutaCv);
        Assert.Equal("mi-cv.pdf", solicitud.NombreOriginalCv);
        Assert.Equal(@"C:\adjuntos\def.pdf", solicitud.RutaCarta);
        Assert.Equal("mi-carta.pdf", solicitud.NombreOriginalCarta);
    }

    [Fact]
    public void AplicarA_VuelcaTodosLosGruposDelEditor()
    {
        DateTime fecha = new(2026, 2, 1);
        Solicitud solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir();
        var editor = new SolicitudEdicionViewModel
        {
            Empresa = "Otra",
            Puesto = "Otro",
            Portal = "LinkedIn",
            Estado = EstadoSolicitud.OfertaRecibida,
            FechaSolicitud = fecha,
            RespuestaEmpresa = "Oferta enviada",
            ContactoNombre = "Ana",
            RutaCv = @"C:\adjuntos\abc.pdf",
        };

        editor.AplicarA(solicitud);

        Assert.Equal("Otra", solicitud.Empresa);
        Assert.Equal("Otro", solicitud.Puesto);
        Assert.Equal("LinkedIn", solicitud.Portal);
        Assert.Equal(EstadoSolicitud.OfertaRecibida, solicitud.Estado);
        Assert.Equal(fecha, solicitud.FechaSolicitud);
        Assert.Equal("Oferta enviada", solicitud.RespuestaEmpresa);
        Assert.Equal("Ana", solicitud.ContactoNombre);
        Assert.Equal(@"C:\adjuntos\abc.pdf", solicitud.RutaCv);
    }

    [Theory]
    [InlineData(EstadoSolicitud.Rechazada, true)]
    [InlineData(EstadoSolicitud.Retirada, true)]
    [InlineData(EstadoSolicitud.OfertaRechazada, true)]
    [InlineData(EstadoSolicitud.Enviada, false)]
    [InlineData(EstadoSolicitud.OfertaAceptada, false)]
    public void AdmiteMotivo_DependeDelEstado(EstadoSolicitud estado, bool esperado)
    {
        var editor = new SolicitudEdicionViewModel { Estado = estado };

        Assert.Equal(esperado, editor.AdmiteMotivo);
    }

    [Theory]
    [InlineData(EstadoSolicitud.Rechazada)]
    [InlineData(EstadoSolicitud.Retirada)]
    [InlineData(EstadoSolicitud.OfertaRechazada)]
    public void MotivosDelEstado_SonLosPermitidosSinLaEntradaVacia(EstadoSolicitud estado)
    {
        var editor = new SolicitudEdicionViewModel { Estado = estado };

        Assert.Equal(estado.MotivosPermitidos(), editor.MotivosDelEstado.Select(i => (MotivoRechazo)i.Valor!));
        Assert.DoesNotContain(editor.MotivosDelEstado, i => i.Valor is null);
    }

    [Fact]
    public void CambiarEstado_NotificaLosMotivosYLaVisibilidad()
    {
        var editor = new SolicitudEdicionViewModel();
        var notificados = new List<string?>();
        editor.PropertyChanged += (_, e) => notificados.Add(e.PropertyName);

        editor.Estado = EstadoSolicitud.Rechazada;

        Assert.Contains(nameof(editor.AdmiteMotivo), notificados);
        Assert.Contains(nameof(editor.MotivosDelEstado), notificados);
    }

    [Fact]
    public void RefrescarMotivosDelEstado_NotificaLaLista()
    {
        var editor = new SolicitudEdicionViewModel { Estado = EstadoSolicitud.Rechazada };
        var notificados = new List<string?>();
        editor.PropertyChanged += (_, e) => notificados.Add(e.PropertyName);

        editor.RefrescarMotivosDelEstado();

        Assert.Contains(nameof(editor.MotivosDelEstado), notificados);
    }

    [Theory]
    [InlineData(EstadoSolicitud.Rechazada, null, MotivoRechazo.NoIndicado)]
    [InlineData(EstadoSolicitud.Retirada, MotivoRechazo.ExperienciaInsuficiente, MotivoRechazo.NoIndicado)]
    [InlineData(EstadoSolicitud.OfertaRechazada, MotivoRechazo.AceptoOtraOferta, MotivoRechazo.AceptoOtraOferta)]
    public void CambiarEstado_PreseleccionaNoIndicadoSiElMotivoNoVale(
        EstadoSolicitud estado,
        MotivoRechazo? motivo,
        MotivoRechazo esperado)
    {
        var editor = new SolicitudEdicionViewModel { Estado = EstadoSolicitud.Enviada, MotivoRechazo = motivo };

        editor.Estado = estado;

        Assert.Equal(esperado, editor.MotivoRechazo);
    }

    [Fact]
    public void CambiarEstado_FueraDeLosCierres_LimpiaElMotivo()
    {
        var editor = new SolicitudEdicionViewModel
        {
            Estado = EstadoSolicitud.Rechazada,
            MotivoRechazo = MotivoRechazo.Salario,
        };

        editor.Estado = EstadoSolicitud.OfertaAceptada;

        Assert.Null(editor.MotivoRechazo);
    }

    [Theory]
    [InlineData(EstadoSolicitud.Rechazada)]
    [InlineData(EstadoSolicitud.Retirada)]
    [InlineData(EstadoSolicitud.OfertaRechazada)]
    public void Validar_SinMotivoDeCierre_EsError(EstadoSolicitud estado)
    {
        var editor = new SolicitudEdicionViewModel { Estado = estado, MotivoRechazo = null };

        Assert.Equal(
            Localizacion.Texto("Mensaje.MotivoCierreNoValido"),
            Assert.Single(editor.GetErrors(nameof(editor.MotivoRechazo)).Cast<ValidationResult>()).ErrorMessage);
    }

    [Fact]
    public void Validar_MotivoNoPermitidoParaElEstado_EsError()
    {
        var editor = new SolicitudEdicionViewModel
        {
            Estado = EstadoSolicitud.Rechazada,
            MotivoRechazo = MotivoRechazo.AceptoOtraOferta,
        };

        editor.Validar();

        Assert.Single(editor.GetErrors(nameof(editor.MotivoRechazo)));
    }

    [Theory]
    [InlineData(EstadoSolicitud.Rechazada, MotivoRechazo.Salario)]
    [InlineData(EstadoSolicitud.Retirada, MotivoRechazo.YaNoMeInteresa)]
    [InlineData(EstadoSolicitud.OfertaRechazada, MotivoRechazo.AceptoOtraOferta)]
    public void Validar_MotivoPermitido_NoEsError(EstadoSolicitud estado, MotivoRechazo motivo)
    {
        var editor = new SolicitudEdicionViewModel
        {
            Empresa = "ACME",
            Puesto = "Dev",
            Estado = estado,
            MotivoRechazo = motivo,
        };

        editor.Validar();

        Assert.False(editor.HasErrors);
    }

    [Fact]
    public void CambiarEstado_RevalidaElMotivo()
    {
        var editor = new SolicitudEdicionViewModel
        {
            Empresa = "ACME",
            Puesto = "Dev",
            Estado = EstadoSolicitud.Rechazada,
            MotivoRechazo = null,
        };
        Assert.True(editor.HasErrors);

        editor.Estado = EstadoSolicitud.Enviada;

        Assert.False(editor.HasErrors);
    }

    [Fact]
    public void CargarDesde_ConEstadoQueAdmiteMotivoYVacio_PreseleccionaNoIndicado()
    {
        Solicitud solicitud = new SolicitudBuilder().ConEstado(EstadoSolicitud.Rechazada).Construir();
        var editor = new SolicitudEdicionViewModel();

        editor.CargarDesde(solicitud);

        Assert.Equal(MotivoRechazo.NoIndicado, editor.MotivoRechazo);
    }

    [Theory]
    [InlineData(EstadoSolicitud.OfertaAceptada, EstadoSolicitud.EntrevistaTecnica)]
    [InlineData(EstadoSolicitud.Rechazada, EstadoSolicitud.Enviada)]
    [InlineData(EstadoSolicitud.Retirada, EstadoSolicitud.OfertaRecibida)]
    [InlineData(EstadoSolicitud.OfertaRechazada, EstadoSolicitud.PruebaTecnica)]
    public void ReaperturaPendiente_CerradoAAbierto_EsTrue(
        EstadoSolicitud estadoCerrado,
        EstadoSolicitud estadoAbierto)
    {
        Solicitud solicitud = new SolicitudBuilder().ConEstado(estadoCerrado).Construir();
        var editor = new SolicitudEdicionViewModel(solicitud);

        Assert.False(editor.ReaperturaPendiente);

        editor.Estado = estadoAbierto;

        Assert.True(editor.ReaperturaPendiente);
    }

    [Fact]
    public void ReaperturaPendiente_AbiertoAAbierto_EsFalse()
    {
        Solicitud solicitud = new SolicitudBuilder().ConEstado(EstadoSolicitud.Enviada).Construir();
        var editor = new SolicitudEdicionViewModel(solicitud);

        editor.Estado = EstadoSolicitud.EntrevistaRrhh;

        Assert.False(editor.ReaperturaPendiente);
    }

    [Fact]
    public void ReaperturaPendiente_CerradoACerrado_EsFalse()
    {
        Solicitud solicitud = new SolicitudBuilder().ConEstado(EstadoSolicitud.Rechazada).Construir();
        var editor = new SolicitudEdicionViewModel(solicitud);

        editor.Estado = EstadoSolicitud.Retirada;

        Assert.False(editor.ReaperturaPendiente);
    }

    [Fact]
    public void ReaperturaPendiente_AlVolverAlEstadoOriginal_EsFalse()
    {
        Solicitud solicitud = new SolicitudBuilder().ConEstado(EstadoSolicitud.Rechazada).Construir();
        var editor = new SolicitudEdicionViewModel(solicitud);
        editor.Estado = EstadoSolicitud.EntrevistaTecnica;

        editor.Estado = EstadoSolicitud.Rechazada;

        Assert.False(editor.ReaperturaPendiente);
    }

    [Fact]
    public void ReaperturaPendiente_SeNotificaAlCambiarEstado()
    {
        Solicitud solicitud = new SolicitudBuilder().ConEstado(EstadoSolicitud.Rechazada).Construir();
        var editor = new SolicitudEdicionViewModel(solicitud);
        var notificados = new List<string?>();
        editor.PropertyChanged += (_, e) => notificados.Add(e.PropertyName);

        editor.Estado = EstadoSolicitud.EntrevistaTecnica;

        Assert.Contains(nameof(editor.ReaperturaPendiente), notificados);
    }

    [Fact]
    public void Validar_ReaperturaPendienteSinNota_EsError()
    {
        Solicitud solicitud = new SolicitudBuilder().ConEstado(EstadoSolicitud.Rechazada).Construir();
        var editor = new SolicitudEdicionViewModel(solicitud);
        editor.Estado = EstadoSolicitud.EntrevistaTecnica;

        editor.Validar();

        Assert.Equal(
            Localizacion.Texto("Mensaje.NotaReaperturaObligatoria"),
            Assert.Single(editor.GetErrors(nameof(editor.NotaReapertura)).Cast<ValidationResult>()).ErrorMessage);
    }

    [Fact]
    public void Validar_ReaperturaPendienteConNota_NoEsError()
    {
        Solicitud solicitud = new SolicitudBuilder().ConEstado(EstadoSolicitud.Rechazada).Construir();
        var editor = new SolicitudEdicionViewModel(solicitud)
        {
            Empresa = "ACME",
            Puesto = "Dev",
        };
        editor.Estado = EstadoSolicitud.EntrevistaTecnica;
        editor.NotaReapertura = "Retomamos el proceso";

        editor.Validar();

        Assert.False(editor.HasErrors);
    }

    [Fact]
    public void CambiarEstado_RevalidaLaNotaDeLaReapertura()
    {
        Solicitud solicitud = new SolicitudBuilder().ConEstado(EstadoSolicitud.Rechazada).Construir();
        var editor = new SolicitudEdicionViewModel(solicitud);

        editor.Estado = EstadoSolicitud.EntrevistaTecnica;

        Assert.NotEmpty(editor.GetErrors(nameof(editor.NotaReapertura)));

        editor.Estado = EstadoSolicitud.Rechazada;

        Assert.Empty(editor.GetErrors(nameof(editor.NotaReapertura)));
    }
}
