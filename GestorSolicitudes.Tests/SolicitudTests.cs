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
