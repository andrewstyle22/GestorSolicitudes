namespace GestorSolicitudes.Tests;

using GestorSolicitudes.Models;

public class SolicitudTests
{
    [Fact]
    public void DiasDesdeSolicitud_CalculaDiasBien()
    {
        var solicitud = new SolicitudBuilder().ConFechaSolicitud(DateTime.Today.AddDays(-5)).Construir();
        Assert.Equal(5, solicitud.DiasDesdeSolicitud);
    }

    [Fact]
    public void DiasProceso_AbiertaCoincideConDiasDesdeSolicitud()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Enviada)
            .ConFechaSolicitud(DateTime.Today.AddDays(-5))
            .Construir();
        Assert.Equal(solicitud.DiasDesdeSolicitud, solicitud.DiasProceso);
    }

    [Fact]
    public void DiasProceso_CerradaMuestraLaDuracionHastaElCierre()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConFechaSolicitud(new DateTime(2026, 1, 10))
            .ConFechaCierre(new DateTime(2026, 1, 21))
            .Construir();
        Assert.Equal(11, solicitud.DiasProceso);
    }

    [Fact]
    public void DiasProceso_CerradaSinFechaDeCierreCaeAHoy()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConFechaSolicitud(DateTime.Today.AddDays(-3))
            .ConFechaCierre(null)
            .Construir();
        Assert.Equal(3, solicitud.DiasProceso);
    }

    [Fact]
    public void DiasProceso_ReabiertaCuentaDesdeLaUltimaReapertura()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Enviada)
            .ConFechaSolicitud(DateTime.Today.AddDays(-10))
            .Construir();
        MarcarReabierta(solicitud, DateTime.Today.AddDays(-3));

        Assert.Equal(3, solicitud.DiasProceso);
    }

    [Fact]
    public void DiasProceso_ReabiertaYCerradaCuentaDesdeLaUltimaReapertura()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConFechaSolicitud(new DateTime(2026, 1, 1))
            .ConFechaCierre(new DateTime(2026, 1, 20))
            .Construir();
        MarcarReabierta(solicitud, new DateTime(2026, 1, 10));

        Assert.Equal(10, solicitud.DiasProceso);
    }

    private static void MarcarReabierta(Solicitud solicitud, DateTime fecha)
    {
        // FechaReapertura aún no tiene setter público: se escribe como lo hará EF (por campo).
        using var db = TestDb.NuevoContexto();
        db.Solicitudes.Add(solicitud);
        db.Entry(solicitud).Property(s => s.FechaReapertura).CurrentValue = fecha;
    }

    [Fact]
    public void Solicitud_ConstructorAsignaEmpresaYPuesto()
    {
        var solicitud = new Solicitud("ACME", "Dev");

        Assert.Equal("ACME", solicitud.Empresa);
        Assert.Equal("Dev", solicitud.Puesto);
    }

    [Fact]
    public void Solicitud_ConstructorAsignaValoresPorDefecto()
    {
        var solicitud = new Solicitud(string.Empty, string.Empty);

        Assert.Equal(string.Empty, solicitud.Empresa);
        Assert.Equal(string.Empty, solicitud.Puesto);
        Assert.Equal(EstadoSolicitud.Enviada, solicitud.Estado);
    }

    [Fact]
    public void CrearCopia_CopiaLaOfertaYDejaElProcesoEnBlanco()
    {
        var origen = new SolicitudBuilder()
            .ConEmpresa("ACME")
            .ConPuesto("Dev")
            .ConPortal("LinkedIn")
            .ConUbicacion("Madrid")
            .ConModalidad(Modalidad.Hibrido)
            .ConTecnologias("C#")
            .ConSalarioMin(30000)
            .ConSalarioMax(45000)
            .ConPretensionSalarial(40000)
            .ConInteres(5)
            .ConEnlaceOferta("https://ejemplo.test")
            .ConRequisitos("Requisitos")
            .ConContactoNombre("Jefa")
            .ConNotas("Notas")
            .ConFechaSolicitud(DateTime.Today.AddDays(-20))
            .ConEstado(EstadoSolicitud.Rechazada)
            .Construir();

        Solicitud copia = origen.CrearCopia();

        Assert.Equal("ACME", copia.Empresa);
        Assert.Equal("Dev", copia.Puesto);
        Assert.Equal("LinkedIn", copia.Portal);
        Assert.Equal("Madrid", copia.Ubicacion);
        Assert.Equal(Modalidad.Hibrido, copia.Modalidad);
        Assert.Equal("C#", copia.Tecnologias);
        Assert.Equal(30000, copia.SalarioMin);
        Assert.Equal(45000, copia.SalarioMax);
        Assert.Equal(40000, copia.PretensionSalarial);
        Assert.Equal(5, copia.Interes);
        Assert.Equal(DateTime.Today, copia.FechaSolicitud);
        Assert.Equal(EstadoSolicitud.Enviada, copia.Estado);
        Assert.Null(copia.SolicitudAnteriorId);
        Assert.Null(copia.EnlaceOferta);
        Assert.Null(copia.Requisitos);
        Assert.Null(copia.ContactoNombre);
        Assert.Null(copia.Notas);
        Assert.Null(copia.RutaCv);
        Assert.Empty(copia.Eventos);
        Assert.NotSame(origen, copia);
    }

    [Fact]
    public void ActualizarIdentificacion_RecortaEspacios()
    {
        var solicitud = new SolicitudBuilder().Construir();

        solicitud.ActualizarIdentificacion("  ACME  ", "  Dev  ");

        Assert.Equal("ACME", solicitud.Empresa);
        Assert.Equal("Dev", solicitud.Puesto);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "Dev")]
    [InlineData("ACME", "")]
    [InlineData("   ", "   ")]
    public void ActualizarIdentificacion_SinEmpresaOPuesto_Lanza(string? empresa, string? puesto)
    {
        var solicitud = new SolicitudBuilder().Construir();

        Assert.Throws<ArgumentException>(() => solicitud.ActualizarIdentificacion(empresa, puesto));
    }

    [Theory]
    [InlineData(151, 1)]
    [InlineData(1, 151)]
    public void ActualizarIdentificacion_DemasiadoLargo_Lanza(int longitudEmpresa, int longitudPuesto)
    {
        var solicitud = new SolicitudBuilder().Construir();

        Assert.Throws<ArgumentException>(() => solicitud.ActualizarIdentificacion(
            new string('a', longitudEmpresa), new string('b', longitudPuesto)));
    }

    [Fact]
    public void ActualizarOferta_AsignaTodosLosCampos()
    {
        var solicitud = new SolicitudBuilder().Construir();
        var datos = new DatosOferta(
            "https://ejemplo.test/oferta",
            "LinkedIn",
            Origen.Referido,
            "Madrid",
            Modalidad.Remoto,
            "C#, WPF",
            "Experiencia con EF Core");

        solicitud.ActualizarOferta(datos);

        Assert.Equal(datos.EnlaceOferta, solicitud.EnlaceOferta);
        Assert.Equal(datos.Portal, solicitud.Portal);
        Assert.Equal(datos.Origen, solicitud.Origen);
        Assert.Equal(datos.Ubicacion, solicitud.Ubicacion);
        Assert.Equal(datos.Modalidad, solicitud.Modalidad);
        Assert.Equal(datos.Tecnologias, solicitud.Tecnologias);
        Assert.Equal(datos.Requisitos, solicitud.Requisitos);
    }

    [Fact]
    public void ActualizarOferta_RecortaEspacios()
    {
        var solicitud = new SolicitudBuilder().Construir();
        var datos = new DatosOferta(
            "  https://ejemplo.test/oferta  ",
            "  LinkedIn  ",
            Origen.AplicacionDirecta,
            "  Madrid  ",
            Modalidad.SinIndicar,
            "  C#  ",
            "  Requisitos  ");

        solicitud.ActualizarOferta(datos);

        Assert.Equal("https://ejemplo.test/oferta", solicitud.EnlaceOferta);
        Assert.Equal("LinkedIn", solicitud.Portal);
        Assert.Equal("Madrid", solicitud.Ubicacion);
        Assert.Equal("C#", solicitud.Tecnologias);
        Assert.Equal("Requisitos", solicitud.Requisitos);
    }

    [Fact]
    public void ActualizarOferta_EnlaceDemasiadoLargo_Lanza() => AssertRechazaOferta(
        new DatosOferta(new string('a', 501), null, Origen.AplicacionDirecta, null, Modalidad.SinIndicar, null, null));

    [Fact]
    public void ActualizarOferta_PortalDemasiadoLargo_Lanza() => AssertRechazaOferta(
        new DatosOferta(null, new string('b', 81), Origen.AplicacionDirecta, null, Modalidad.SinIndicar, null, null));

    [Fact]
    public void ActualizarOferta_UbicacionDemasiadoLarga_Lanza() => AssertRechazaOferta(
        new DatosOferta(null, null, Origen.AplicacionDirecta, new string('c', 121), Modalidad.SinIndicar, null, null));

    [Fact]
    public void ActualizarOferta_TecnologiasDemasiadoLargas_Lanza() => AssertRechazaOferta(
        new DatosOferta(null, null, Origen.AplicacionDirecta, null, Modalidad.SinIndicar, new string('d', 301), null));

    [Fact]
    public void ActualizarOferta_RequisitosDemasiadoLargos_Lanza() => AssertRechazaOferta(
        new DatosOferta(null, null, Origen.AplicacionDirecta, null, Modalidad.SinIndicar, null, new string('e', 4001)));

    private static void AssertRechazaOferta(DatosOferta datos) =>
        Assert.Throws<ArgumentException>(() => new SolicitudBuilder().Construir().ActualizarOferta(datos));

    [Fact]
    public void ActualizarCondiciones_AsignaTodosLosCampos()
    {
        var solicitud = new SolicitudBuilder().Construir();

        solicitud.ActualizarCondiciones(30000, 45000, 40000, 4);

        Assert.Equal(30000, solicitud.SalarioMin);
        Assert.Equal(45000, solicitud.SalarioMax);
        Assert.Equal(40000, solicitud.PretensionSalarial);
        Assert.Equal(4, solicitud.Interes);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(30000, null)]
    [InlineData(null, 45000)]
    [InlineData(30000, 30000)]
    public void ActualizarCondiciones_RangoValido_Asigna(int? salarioMin, int? salarioMax)
    {
        var solicitud = new SolicitudBuilder().Construir();

        solicitud.ActualizarCondiciones(salarioMin, salarioMax, null, 3);

        Assert.Equal(salarioMin, solicitud.SalarioMin);
        Assert.Equal(salarioMax, solicitud.SalarioMax);
    }

    [Fact]
    public void ActualizarCondiciones_SalarioMinMayorQueMax_Lanza()
    {
        var solicitud = new SolicitudBuilder().Construir();

        Assert.Throws<ArgumentException>(() => solicitud.ActualizarCondiciones(45000, 30000, null, 3));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void ActualizarCondiciones_InteresFueraDeRango_Lanza(int interes)
    {
        var solicitud = new SolicitudBuilder().Construir();

        Assert.Throws<ArgumentException>(() => solicitud.ActualizarCondiciones(null, null, null, interes));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void ActualizarCondiciones_InteresEnLosLimites_Asigna(int interes)
    {
        var solicitud = new SolicitudBuilder().Construir();

        solicitud.ActualizarCondiciones(null, null, null, interes);

        Assert.Equal(interes, solicitud.Interes);
    }

    [Fact]
    public void CambiarEstado_AsignaEstadoMotivoYFechaCierre()
    {
        var solicitud = new SolicitudBuilder().Construir();
        var fecha = new DateTime(2026, 2, 3);

        solicitud.CambiarEstado(EstadoSolicitud.Rechazada, MotivoRechazo.Salario, fecha);

        Assert.Equal(EstadoSolicitud.Rechazada, solicitud.Estado);
        Assert.Equal(MotivoRechazo.Salario, solicitud.MotivoRechazo);
        Assert.Equal(fecha, solicitud.FechaCierre);
    }

    [Theory]
    [InlineData(EstadoSolicitud.Rechazada)]
    [InlineData(EstadoSolicitud.Retirada)]
    [InlineData(EstadoSolicitud.OfertaRechazada)]
    public void CambiarEstado_SinMotivoEnCierreNegativo_Lanza(EstadoSolicitud estado)
    {
        var solicitud = new SolicitudBuilder().Construir();

        ArgumentException excepcion = Assert.Throws<ArgumentException>(
            () => solicitud.CambiarEstado(estado, null, null));

        Assert.Equal("motivo", excepcion.ParamName);
    }

    [Theory]
    [InlineData(EstadoSolicitud.Rechazada, MotivoRechazo.AceptoOtraOferta)]
    [InlineData(EstadoSolicitud.Retirada, MotivoRechazo.ExperienciaInsuficiente)]
    [InlineData(EstadoSolicitud.OfertaRechazada, MotivoRechazo.CulturalFit)]
    public void CambiarEstado_MotivoNoPermitido_Lanza(EstadoSolicitud estado, MotivoRechazo motivo)
    {
        var solicitud = new SolicitudBuilder().Construir();

        Assert.Throws<ArgumentException>(() => solicitud.CambiarEstado(estado, motivo, null));
    }

    [Fact]
    public void CambiarEstado_FueraDeLosCierres_LimpiaElMotivo()
    {
        var solicitud = new SolicitudBuilder().Construir();

        solicitud.CambiarEstado(EstadoSolicitud.Enviada, MotivoRechazo.Salario, null);

        Assert.Null(solicitud.MotivoRechazo);
    }

    [Fact]
    public void CambiarEstado_SinMotivoNiFecha_LosDejaNulos()
    {
        var solicitud = new SolicitudBuilder().Construir();

        solicitud.CambiarEstado(EstadoSolicitud.EntrevistaRrhh, null, null);

        Assert.Equal(EstadoSolicitud.EntrevistaRrhh, solicitud.Estado);
        Assert.Null(solicitud.MotivoRechazo);
        Assert.Null(solicitud.FechaCierre);
    }

    [Theory]
    [InlineData(EstadoSolicitud.OfertaAceptada)]
    [InlineData(EstadoSolicitud.OfertaRechazada)]
    [InlineData(EstadoSolicitud.Rechazada)]
    [InlineData(EstadoSolicitud.Retirada)]
    public void CambiarEstado_CerradaAAbierta_Lanza(EstadoSolicitud estadoCerrado)
    {
        var solicitud = new SolicitudBuilder().ConEstado(estadoCerrado).Construir();

        Assert.Throws<InvalidOperationException>(
            () => solicitud.CambiarEstado(EstadoSolicitud.Enviada, null, null));
    }

    [Theory]
    [InlineData(EstadoSolicitud.OfertaRechazada)]
    [InlineData(EstadoSolicitud.Rechazada)]
    [InlineData(EstadoSolicitud.Retirada)]
    public void CambiarEstado_CerradaAOfertaAceptada_Lanza(EstadoSolicitud estadoCerrado)
    {
        var solicitud = new SolicitudBuilder().ConEstado(estadoCerrado).Construir();

        Assert.Throws<InvalidOperationException>(
            () => solicitud.CambiarEstado(EstadoSolicitud.OfertaAceptada, null, null));
    }

    [Theory]
    [InlineData(EstadoSolicitud.Rechazada)]
    [InlineData(EstadoSolicitud.Retirada)]
    [InlineData(EstadoSolicitud.OfertaRechazada)]
    public void CambiarEstado_AlCerrarSinFecha_UsaHoy(EstadoSolicitud estado)
    {
        var solicitud = new SolicitudBuilder().Construir();

        solicitud.CambiarEstado(estado, MotivoRechazo.NoIndicado, null);

        Assert.Equal(DateTime.Today, solicitud.FechaCierre);
    }

    [Fact]
    public void CambiarEstado_CerradoACerrado_ActualizaElMotivo()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.OfertaRechazada)
            .Construir();

        solicitud.CambiarEstado(EstadoSolicitud.Retirada, MotivoRechazo.YaNoMeInteresa, null);

        Assert.Equal(EstadoSolicitud.Retirada, solicitud.Estado);
        Assert.Equal(MotivoRechazo.YaNoMeInteresa, solicitud.MotivoRechazo);
        Assert.Equal(DateTime.Today, solicitud.FechaCierre);
    }

    [Fact]
    public void CambiarEstado_Descartado_AnadeElHitoDeRechazo()
    {
        var solicitud = new SolicitudBuilder().Construir();

        solicitud.CambiarEstado(EstadoSolicitud.Rechazada, MotivoRechazo.Salario, null);

        Evento evento = Assert.Single(solicitud.Eventos);
        Assert.Equal(DateTime.Today, evento.Fecha);
        Assert.Equal(TipoEvento.Rechazo, evento.Tipo);
        Assert.Contains("Salario / pretensión", evento.Descripcion);
    }

    [Fact]
    public void CambiarEstado_RepiteElMismoCierre_NoDuplicaElHito()
    {
        var solicitud = new SolicitudBuilder().Construir();

        solicitud.CambiarEstado(EstadoSolicitud.Rechazada, MotivoRechazo.Salario, null);
        solicitud.CambiarEstado(EstadoSolicitud.Rechazada, MotivoRechazo.Salario, null);

        Assert.Single(solicitud.Eventos);
    }

    [Fact]
    public void CambiarEstado_SalirDeSinRespuesta_RellenaLaPrimeraRespuestaYAnadeElHito()
    {
        var solicitud = new SolicitudBuilder().ConEstado(EstadoSolicitud.SinRespuesta).Construir();

        solicitud.CambiarEstado(EstadoSolicitud.EntrevistaTecnica, null, null);

        Assert.Equal(DateTime.Today, solicitud.FechaPrimeraRespuesta);
        Evento evento = Assert.Single(solicitud.Eventos);
        Assert.Equal(TipoEvento.Respuesta, evento.Tipo);
    }

    [Fact]
    public void CambiarEstado_SalirDeSinRespuesta_ConservaLaPrimeraRespuesta()
    {
        var primeraRespuesta = new DateTime(2026, 3, 1);
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.SinRespuesta)
            .ConFechaPrimeraRespuesta(primeraRespuesta)
            .Construir();

        solicitud.CambiarEstado(EstadoSolicitud.EnRevision, null, null);

        Assert.Equal(primeraRespuesta, solicitud.FechaPrimeraRespuesta);
        Assert.Single(solicitud.Eventos);
    }

    [Fact]
    public void Reabrir_AsignaEstadoYLimpiaElCierre()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConMotivoRechazo(MotivoRechazo.Salario)
            .ConFechaCierre(new DateTime(2026, 10, 10))
            .ConFechaSolicitud(new DateTime(2026, 9, 1))
            .Construir();

        solicitud.Reabrir(EstadoSolicitud.EntrevistaTecnica, "Retomamos el proceso");

        Assert.Equal(EstadoSolicitud.EntrevistaTecnica, solicitud.Estado);
        Assert.Null(solicitud.FechaCierre);
        Assert.Null(solicitud.MotivoRechazo);
        Assert.Equal(new DateTime(2026, 9, 1), solicitud.FechaSolicitud);
        Assert.True(solicitud.FueReabierta);
    }

    [Fact]
    public void Reabrir_MarcaLaReaperturaDeHoy()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Retirada)
            .ConFechaCierre(new DateTime(2026, 10, 10))
            .Construir();
        MarcarReabierta(solicitud, new DateTime(2026, 1, 1));

        solicitud.Reabrir(EstadoSolicitud.Enviada, "Retomamos el proceso");

        Assert.Equal(DateTime.Today, solicitud.FechaReapertura);
    }

    [Fact]
    public void Reabrir_SinPrimeraRespuesta_LaRellenaConHoy()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConFechaCierre(new DateTime(2026, 10, 10))
            .Construir();

        solicitud.Reabrir(EstadoSolicitud.Enviada, "Retomamos el proceso");

        Assert.Equal(DateTime.Today, solicitud.FechaPrimeraRespuesta);
    }

    [Fact]
    public void Reabrir_ConservaLaPrimeraRespuestaQueYaExistia()
    {
        var primeraRespuesta = new DateTime(2026, 9, 5);
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConFechaCierre(new DateTime(2026, 10, 10))
            .ConFechaPrimeraRespuesta(primeraRespuesta)
            .Construir();

        solicitud.Reabrir(EstadoSolicitud.Enviada, "Retomamos el proceso");

        Assert.Equal(primeraRespuesta, solicitud.FechaPrimeraRespuesta);
    }

    [Fact]
    public void Reabrir_ProponeUnSeguimientoNuevoASieteDias()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConFechaCierre(new DateTime(2026, 10, 10))
            .ConFechaSolicitud(new DateTime(2026, 9, 1))
            .ConProximoSeguimiento(new DateTime(2026, 9, 8))
            .Construir();

        solicitud.Reabrir(EstadoSolicitud.Enviada, "Retomamos el proceso");

        Assert.Equal(DateTime.Today.AddDays(7), solicitud.ProximoSeguimiento);
    }

    [Fact]
    public void Reabrir_AnadeElHitoConElCierreAnteriorYLaNota()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConMotivoRechazo(MotivoRechazo.CulturalFit)
            .ConFechaCierre(new DateTime(2026, 10, 10))
            .Construir();

        solicitud.Reabrir(EstadoSolicitud.EntrevistaRrhh, "  perfil no adecuado  ");

        Evento evento = solicitud.Eventos[^1];
        Assert.Equal(DateTime.Today, evento.Fecha);
        Assert.Equal(TipoEvento.Reapertura, evento.Tipo);
        Assert.Equal(
            "Reabierta. Antes: Descartado por la empresa (10/10, No encajaba con el equipo/cultura). perfil no adecuado",
            evento.Descripcion);
    }

    [Fact]
    public void Reabrir_NotaDemasiadoLarga_Lanza()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Rechazada)
            .Construir();

        Assert.Throws<ArgumentException>(() =>
            solicitud.Reabrir(EstadoSolicitud.Enviada, new string('x', Solicitud.LongitudMaximaNotaReapertura + 1)));
    }

    [Fact]
    public void Reabrir_SinMotivo_DescribeElEstadoLaFechaYLaNota()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.OfertaAceptada)
            .ConFechaCierre(new DateTime(2026, 10, 10))
            .Construir();

        solicitud.Reabrir(EstadoSolicitud.Enviada, "Retomamos el proceso");

        Assert.Equal(
            "Reabierta. Antes: Oferta aceptada (10/10). Retomamos el proceso",
            solicitud.Eventos[^1].Descripcion);
    }

    [Fact]
    public void Reabrir_SinFechaDeCierre_DescribeElEstadoYLaNota()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConFechaCierre(null)
            .Construir();

        solicitud.Reabrir(EstadoSolicitud.Enviada, "Retomamos el proceso");

        Assert.Equal(
            "Reabierta. Antes: Descartado por la empresa. Retomamos el proceso",
            solicitud.Eventos[^1].Descripcion);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reabrir_SinNota_Lanza(string? nota)
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Rechazada)
            .Construir();

        Assert.Throws<ArgumentException>(
            () => solicitud.Reabrir(EstadoSolicitud.Enviada, nota));
    }

    [Fact]
    public void Reabrir_UnaCandidaturaAbierta_Lanza()
    {
        var solicitud = new SolicitudBuilder().ConEstado(EstadoSolicitud.Enviada).Construir();

        Assert.Throws<InvalidOperationException>(
            () => solicitud.Reabrir(EstadoSolicitud.EntrevistaRrhh, null));
    }

    [Theory]
    [InlineData(EstadoSolicitud.OfertaAceptada)]
    [InlineData(EstadoSolicitud.SinRespuesta)]
    public void Reabrir_AUnEstadoNoPermitido_Lanza(EstadoSolicitud nuevoEstado)
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConFechaCierre(new DateTime(2026, 10, 10))
            .Construir();

        Assert.Throws<ArgumentException>(() => solicitud.Reabrir(nuevoEstado, null));
    }

    [Fact]
    public void RegistrarRespuesta_AsignaFechaYTexto()
    {
        var solicitud = new SolicitudBuilder().Construir();
        var fecha = new DateTime(2026, 2, 3);

        solicitud.RegistrarRespuesta(fecha, "Buscamos otro perfil");

        Assert.Equal(fecha, solicitud.FechaPrimeraRespuesta);
        Assert.Equal("Buscamos otro perfil", solicitud.RespuestaEmpresa);
    }

    [Fact]
    public void RegistrarRespuesta_SoloTexto_DejaLaFechaNula()
    {
        var solicitud = new SolicitudBuilder().ConFechaPrimeraRespuesta(DateTime.Today).Construir();

        solicitud.RegistrarRespuesta(null, "Oferta enviada");

        Assert.Null(solicitud.FechaPrimeraRespuesta);
        Assert.Equal("Oferta enviada", solicitud.RespuestaEmpresa);
    }

    [Fact]
    public void RegistrarRespuesta_SoloFecha_DejaElTextoNulo()
    {
        var solicitud = new SolicitudBuilder().ConRespuestaEmpresa("Antigua").Construir();
        var fecha = new DateTime(2026, 2, 3);

        solicitud.RegistrarRespuesta(fecha, null);

        Assert.Equal(fecha, solicitud.FechaPrimeraRespuesta);
        Assert.Null(solicitud.RespuestaEmpresa);
    }

    [Fact]
    public void ActualizarContacto_AsignaTodosLosCampos()
    {
        var solicitud = new SolicitudBuilder().Construir();

        solicitud.ActualizarContacto("Ana", "ana@ejemplo.test", "600123456");

        Assert.Equal("Ana", solicitud.ContactoNombre);
        Assert.Equal("ana@ejemplo.test", solicitud.ContactoEmail);
        Assert.Equal("600123456", solicitud.ContactoTelefono);
    }

    [Fact]
    public void ActualizarContacto_RecortaEspacios()
    {
        var solicitud = new SolicitudBuilder().Construir();

        solicitud.ActualizarContacto("  Ana  ", "  ana@ejemplo.test  ", "  600123456  ");

        Assert.Equal("Ana", solicitud.ContactoNombre);
        Assert.Equal("ana@ejemplo.test", solicitud.ContactoEmail);
        Assert.Equal("600123456", solicitud.ContactoTelefono);
    }

    [Fact]
    public void ActualizarContacto_NombreDemasiadoLargo_Lanza() =>
        Assert.Throws<ArgumentException>(
            () => new SolicitudBuilder().Construir().ActualizarContacto(new string('a', 121), null, null));

    [Fact]
    public void ActualizarContacto_EmailDemasiadoLargo_Lanza() =>
        Assert.Throws<ArgumentException>(
            () => new SolicitudBuilder().Construir().ActualizarContacto(null, new string('b', 151), null));

    [Fact]
    public void ActualizarContacto_TelefonoDemasiadoLargo_Lanza() =>
        Assert.Throws<ArgumentException>(
            () => new SolicitudBuilder().Construir().ActualizarContacto(null, null, new string('c', 41)));

    [Fact]
    public void ActualizarNotas_AsignaSinLimiteDeLongitud()
    {
        var solicitud = new SolicitudBuilder().Construir();
        string notas = new('n', 5000);

        solicitud.ActualizarNotas($"  {notas}  ");

        Assert.Equal(notas, solicitud.Notas);
    }

    [Fact]
    public void ActualizarFechas_AsignaEnvioEntrevistaYSeguimiento()
    {
        var solicitud = new SolicitudBuilder().Construir();
        var envio = new DateTime(2026, 2, 1);

        solicitud.ActualizarFechas(envio, envio.AddDays(2), envio.AddDays(7));

        Assert.Equal(envio, solicitud.FechaSolicitud);
        Assert.Equal(envio.AddDays(2), solicitud.FechaEntrevista);
        Assert.Equal(envio.AddDays(7), solicitud.ProximoSeguimiento);
    }

    [Fact]
    public void ProponerSeguimiento_AbiertaSinFecha_ProponeSieteDiasDespues()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Enviada)
            .ConFechaSolicitud(new DateTime(2026, 1, 10))
            .Construir();

        solicitud.ProponerSeguimiento();

        Assert.Equal(new DateTime(2026, 1, 17), solicitud.ProximoSeguimiento);
    }

    [Fact]
    public void ProponerSeguimiento_NoPisaUnaFechaYaInformada()
    {
        var seguimiento = new DateTime(2026, 1, 20);
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Enviada)
            .ConFechaSolicitud(new DateTime(2026, 1, 10))
            .ConProximoSeguimiento(seguimiento)
            .Construir();

        solicitud.ProponerSeguimiento();

        Assert.Equal(seguimiento, solicitud.ProximoSeguimiento);
    }

    [Fact]
    public void ProponerSeguimiento_Cerrada_NoAsigna()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.OfertaAceptada)
            .ConFechaSolicitud(new DateTime(2026, 1, 10))
            .Construir();

        solicitud.ProponerSeguimiento();

        Assert.Null(solicitud.ProximoSeguimiento);
    }

    [Fact]
    public void MotivoRechazo_PorDefectoEsNulo()
    {
        Assert.Null(new SolicitudBuilder().Construir().MotivoRechazo);
    }

    [Fact]
    public void Origen_PorDefectoEsAplicacionDirecta()
    {
        Assert.Equal(Origen.AplicacionDirecta, new SolicitudBuilder().Construir().Origen);
    }

    [Fact]
    public void DiasHastaRespuesta_ConRespuesta_DevuelveDias()
    {
        var baseFecha = new DateTime(2026, 1, 10);
        var solicitud = new SolicitudBuilder()
            .ConFechaSolicitud(baseFecha)
            .ConFechaPrimeraRespuesta(baseFecha.AddDays(3))
            .Construir();
        Assert.Equal(3, solicitud.DiasHastaRespuesta);
    }

    [Fact]
    public void DiasHastaRespuesta_SinRespuesta_EsNull()
    {
        var solicitud = new SolicitudBuilder().ConFechaSolicitud(DateTime.Today).Construir();
        Assert.Null(solicitud.DiasHastaRespuesta);
    }

    [Theory]
    [InlineData(EstadoSolicitud.Enviada, true)]
    [InlineData(EstadoSolicitud.EnRevision, true)]
    [InlineData(EstadoSolicitud.SinRespuesta, true)]
    [InlineData(EstadoSolicitud.EntrevistaRrhh, true)]
    [InlineData(EstadoSolicitud.OfertaAceptada, false)]
    [InlineData(EstadoSolicitud.OfertaRechazada, false)]
    [InlineData(EstadoSolicitud.Rechazada, false)]
    [InlineData(EstadoSolicitud.Retirada, false)]
    public void EstaAbierta_SegunEstado(EstadoSolicitud estado, bool esperado)
    {
        var solicitud = new SolicitudBuilder().ConEstado(estado).Construir();
        Assert.Equal(esperado, solicitud.EstaAbierta);
    }

    [Theory]
    [InlineData(EstadoSolicitud.PruebaTecnica, true)]
    [InlineData(EstadoSolicitud.EntrevistaRrhh, true)]
    [InlineData(EstadoSolicitud.EntrevistaTecnica, true)]
    [InlineData(EstadoSolicitud.EntrevistaFinal, true)]
    [InlineData(EstadoSolicitud.Enviada, false)]
    [InlineData(EstadoSolicitud.OfertaRecibida, false)]
    public void EsEntrevista_SoloIncluyeLosEstadosDeEntrevista(EstadoSolicitud estado, bool esperado)
    {
        Assert.Equal(esperado, estado.EsEntrevista());
    }

    [Theory]
    [InlineData(EstadoSolicitud.OfertaRecibida, true)]
    [InlineData(EstadoSolicitud.OfertaAceptada, true)]
    [InlineData(EstadoSolicitud.OfertaRechazada, true)]
    [InlineData(EstadoSolicitud.Enviada, false)]
    [InlineData(EstadoSolicitud.EntrevistaFinal, false)]
    public void EsOferta_SoloIncluyeLosEstadosDeOferta(EstadoSolicitud estado, bool esperado)
    {
        Assert.Equal(esperado, estado.EsOferta());
    }

    [Theory]
    [InlineData(EstadoSolicitud.OfertaAceptada, true)]
    [InlineData(EstadoSolicitud.OfertaRechazada, true)]
    [InlineData(EstadoSolicitud.Rechazada, true)]
    [InlineData(EstadoSolicitud.Retirada, true)]
    [InlineData(EstadoSolicitud.Enviada, false)]
    [InlineData(EstadoSolicitud.EntrevistaTecnica, false)]
    public void EstaCerrada_SoloIncluyeLosEstadosTerminales(EstadoSolicitud estado, bool esperado)
    {
        Assert.Equal(esperado, estado.EstaCerrada());
    }

    [Theory]
    [InlineData(EstadoSolicitud.Rechazada, true)]
    [InlineData(EstadoSolicitud.Retirada, true)]
    [InlineData(EstadoSolicitud.OfertaRechazada, true)]
    [InlineData(EstadoSolicitud.Enviada, false)]
    [InlineData(EstadoSolicitud.EnRevision, false)]
    [InlineData(EstadoSolicitud.PruebaTecnica, false)]
    [InlineData(EstadoSolicitud.EntrevistaRrhh, false)]
    [InlineData(EstadoSolicitud.EntrevistaTecnica, false)]
    [InlineData(EstadoSolicitud.EntrevistaFinal, false)]
    [InlineData(EstadoSolicitud.OfertaRecibida, false)]
    [InlineData(EstadoSolicitud.OfertaAceptada, false)]
    [InlineData(EstadoSolicitud.SinRespuesta, false)]
    public void AdmiteMotivo_SoloEnLosTresCierresNegativos(EstadoSolicitud estado, bool esperado)
    {
        Assert.Equal(esperado, estado.AdmiteMotivo());
    }

    [Fact]
    public void MotivosPermitidos_DescartadoSonLosDeLaEmpresa()
    {
        Assert.Equal(
            new[]
            {
                MotivoRechazo.Salario,
                MotivoRechazo.ExperienciaInsuficiente,
                MotivoRechazo.SeleccionaronOtroCandidato,
                MotivoRechazo.CulturalFit,
                MotivoRechazo.PuestoCanceladoOPausado,
                MotivoRechazo.Otro,
                MotivoRechazo.NoIndicado,
            },
            EstadoSolicitud.Rechazada.MotivosPermitidos());
    }

    [Theory]
    [InlineData(EstadoSolicitud.Retirada)]
    [InlineData(EstadoSolicitud.OfertaRechazada)]
    public void MotivosPermitidos_RetiradaYOfertaRechazadaSonLosPropios(EstadoSolicitud estado)
    {
        Assert.Equal(
            new[]
            {
                MotivoRechazo.Salario,
                MotivoRechazo.AceptoOtraOferta,
                MotivoRechazo.ModalidadOUbicacion,
                MotivoRechazo.YaNoMeInteresa,
                MotivoRechazo.Otro,
                MotivoRechazo.NoIndicado,
            },
            estado.MotivosPermitidos());
    }

    [Theory]
    [InlineData(EstadoSolicitud.Enviada)]
    [InlineData(EstadoSolicitud.OfertaAceptada)]
    [InlineData(EstadoSolicitud.SinRespuesta)]
    public void MotivosPermitidos_SinMotivoDevuelveListaVacia(EstadoSolicitud estado)
    {
        Assert.Empty(estado.MotivosPermitidos());
    }

    [Fact]
    public void HuboRespuesta_ConFechaEsTrue()
    {
        Assert.True(new SolicitudBuilder().ConFechaPrimeraRespuesta(DateTime.Today).Construir().HuboRespuesta);
    }

    [Fact]
    public void HuboRespuesta_SinFechaEsFalse()
    {
        Assert.False(new SolicitudBuilder().Construir().HuboRespuesta);
    }

    [Fact]
    public void SeguimientoPendiente_AbiertaConFechaVencidaEsTrue()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Enviada)
            .ConProximoSeguimiento(DateTime.Today.AddDays(-1))
            .Construir();
        Assert.True(solicitud.SeguimientoPendiente);
    }

    [Fact]
    public void SeguimientoPendiente_ConFechaFuturaEsFalse()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Enviada)
            .ConProximoSeguimiento(DateTime.Today.AddDays(2))
            .Construir();
        Assert.False(solicitud.SeguimientoPendiente);
    }

    [Fact]
    public void SeguimientoPendiente_CerradaEsFalse()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConProximoSeguimiento(DateTime.Today.AddDays(-1))
            .Construir();
        Assert.False(solicitud.SeguimientoPendiente);
    }

    [Fact]
    public void AsignarCv_AsignaRutaYNombreOriginal()
    {
        var solicitud = new SolicitudBuilder().Construir();

        solicitud.AsignarCv(@"C:\adjuntos\a1b2.pdf", "mi-cv.pdf");

        Assert.Equal(@"C:\adjuntos\a1b2.pdf", solicitud.RutaCv);
        Assert.Equal("mi-cv.pdf", solicitud.NombreOriginalCv);
    }

    [Fact]
    public void AsignarCv_RutaDemasiadoLarga_Lanza() =>
        Assert.Throws<ArgumentException>(
            () => new SolicitudBuilder().Construir().AsignarCv(new string('a', 501), null));

    [Fact]
    public void AsignarCv_NombreDemasiadoLargo_Lanza() =>
        Assert.Throws<ArgumentException>(
            () => new SolicitudBuilder().Construir().AsignarCv(null, new string('b', 261)));

    [Fact]
    public void QuitarCv_LimpiaRutaYNombreOriginal()
    {
        var solicitud = new SolicitudBuilder()
            .ConRutaCv(@"C:\adjuntos\a1b2.pdf")
            .ConNombreOriginalCv("mi-cv.pdf")
            .Construir();

        solicitud.QuitarCv();

        Assert.Null(solicitud.RutaCv);
        Assert.Null(solicitud.NombreOriginalCv);
    }

    [Fact]
    public void AsignarCarta_AsignaRutaYNombreOriginal()
    {
        var solicitud = new SolicitudBuilder().Construir();

        solicitud.AsignarCarta(@"C:\adjuntos\a1b2.pdf", "mi-carta.pdf");

        Assert.Equal(@"C:\adjuntos\a1b2.pdf", solicitud.RutaCarta);
        Assert.Equal("mi-carta.pdf", solicitud.NombreOriginalCarta);
    }

    [Fact]
    public void AsignarCarta_RutaDemasiadoLarga_Lanza() =>
        Assert.Throws<ArgumentException>(
            () => new SolicitudBuilder().Construir().AsignarCarta(new string('a', 501), null));

    [Fact]
    public void AsignarCarta_NombreDemasiadoLargo_Lanza() =>
        Assert.Throws<ArgumentException>(
            () => new SolicitudBuilder().Construir().AsignarCarta(null, new string('b', 261)));

    [Fact]
    public void QuitarCarta_LimpiaRutaYNombreOriginal()
    {
        var solicitud = new SolicitudBuilder()
            .ConRutaCarta(@"C:\adjuntos\a1b2.pdf")
            .ConNombreOriginalCarta("mi-carta.pdf")
            .Construir();

        solicitud.QuitarCarta();

        Assert.Null(solicitud.RutaCarta);
        Assert.Null(solicitud.NombreOriginalCarta);
    }

    [Fact]
    public void NombreCv_UsaNombreOriginalSiExiste()
    {
        var solicitud = new SolicitudBuilder()
            .ConNombreOriginalCv("mi-cv-final.pdf")
            .ConRutaCv(@"C:\cualquier\cosa\a1b2.pdf")
            .Construir();
        Assert.Equal("mi-cv-final.pdf", solicitud.NombreCv);
    }

    [Fact]
    public void NombreCv_SinNombreOriginalUsaNombreDelFichero()
    {
        var solicitud = new SolicitudBuilder().ConRutaCv(@"C:\cualquier\cosa\a1b2.pdf").Construir();
        Assert.Equal("a1b2.pdf", solicitud.NombreCv);
    }

    [Fact]
    public void NombreCv_SinRutaEsNull()
    {
        Assert.Null(new SolicitudBuilder().Construir().NombreCv);
    }

    [Fact]
    public void NombreCarta_UsaNombreOriginalSiExiste()
    {
        var solicitud = new SolicitudBuilder()
            .ConNombreOriginalCarta("mi-carta-final.pdf")
            .ConRutaCarta(@"C:\cualquier\cosa\a1b2.pdf")
            .Construir();
        Assert.Equal("mi-carta-final.pdf", solicitud.NombreCarta);
    }

    [Fact]
    public void NombreCarta_SinNombreOriginalUsaNombreDelFichero()
    {
        var solicitud = new SolicitudBuilder().ConRutaCarta(@"C:\cualquier\cosa\a1b2.pdf").Construir();
        Assert.Equal("a1b2.pdf", solicitud.NombreCarta);
    }

    [Fact]
    public void NombreCarta_SinRutaEsNull()
    {
        Assert.Null(new SolicitudBuilder().Construir().NombreCarta);
    }

    [Fact]
    public void ToString_DevuelveEmpresaYPuesto()
    {
        var solicitud = new Solicitud("ACME", "Dev");

        Assert.Equal("ACME — Dev", solicitud.ToString());
    }

    [Fact]
    public void Evento_TrasGuardarExponeIdSolicitudYPropiedadDeNavegacion()
    {
        using var db = TestDb.NuevoContexto();

        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir();
        solicitud.AgregarEvento(new Evento(DateTime.Today, TipoEvento.Nota, string.Empty));
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();

        Evento evento = solicitud.Eventos[0];
        Assert.True(evento.Id > 0);
        Assert.Equal(solicitud.Id, evento.SolicitudId);
        Assert.Same(solicitud, evento.Solicitud);
    }

    [Fact]
    public void Evento_ConstructorAsignaFechaTipoYDescripcion()
    {
        const string descripcion = "Entrevista";
        var evento = new Evento(new DateTime(2026, 3, 5), TipoEvento.Entrevista, descripcion);

        Assert.Equal(new DateTime(2026, 3, 5), evento.Fecha);
        Assert.Equal(TipoEvento.Entrevista, evento.Tipo);
        Assert.Equal(descripcion, evento.Descripcion);
    }

    [Fact]
    public void Evento_ActualizarCambiaFechaTipoYDescripcion()
    {
        const string descripcion = "Oferta";
        var evento = new Evento(new DateTime(2026, 3, 5), TipoEvento.Entrevista, "Entrevista");

        evento.Actualizar(new DateTime(2026, 4, 1), TipoEvento.Oferta, descripcion);

        Assert.Equal(new DateTime(2026, 4, 1), evento.Fecha);
        Assert.Equal(TipoEvento.Oferta, evento.Tipo);
        Assert.Equal(descripcion, evento.Descripcion);
    }

    [Fact]
    public void AgregarEvento_LoAnadeAlHistorial()
    {
        var solicitud = new SolicitudBuilder().Construir();
        var evento = new Evento(new DateTime(2026, 3, 5), TipoEvento.Llamada, "Llamada de screening");

        solicitud.AgregarEvento(evento);

        Assert.Same(evento, Assert.Single(solicitud.Eventos));
    }

    [Fact]
    public void QuitarEvento_LoSacaDelHistorial()
    {
        var evento = new Evento(new DateTime(2026, 3, 6), TipoEvento.PruebaTecnica, "Prueba técnica");
        var solicitud = new SolicitudBuilder().ConEventos(evento).Construir();

        solicitud.QuitarEvento(evento);

        Assert.Empty(solicitud.Eventos);
    }

    [Fact]
    public void RegistrarEnvio_AnadeElHitoCuandoElHistorialEstaVacio()
    {
        var solicitud = new SolicitudBuilder().ConFechaSolicitud(new DateTime(2026, 3, 1)).Construir();

        solicitud.RegistrarEnvio("Candidatura enviada");

        Evento evento = Assert.Single(solicitud.Eventos);
        Assert.Equal(TipoEvento.SolicitudEnviada, evento.Tipo);
        Assert.Equal(new DateTime(2026, 3, 1), evento.Fecha);
        Assert.Equal("Candidatura enviada", evento.Descripcion);
    }

    [Fact]
    public void RegistrarEnvio_NoDuplicaCuandoYaTieneEventos()
    {
        var solicitud = new SolicitudBuilder()
            .ConEventos(new Evento(new DateTime(2026, 3, 1), TipoEvento.SolicitudEnviada, string.Empty))
            .Construir();

        solicitud.RegistrarEnvio("Candidatura enviada");

        Assert.Single(solicitud.Eventos);
    }
}
