namespace GestorSolicitudes.ViewModels;

using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestorSolicitudes.Data;
using GestorSolicitudes.Helpers;
using GestorSolicitudes.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

/// <summary>Lo mínimo que necesita el aviso de la bandeja, sin arrastrar la entidad completa.</summary>
internal sealed record SeguimientoVencido(int Id, string Empresa, string Puesto, DateTime? ProximoSeguimiento);

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    // La cultura de los números y fechas se toma del idioma activo; los tests que no pasan
    // por el arranque de la app se apoyan en que por defecto el idioma es el castellano.
    private const string ClaveError = "Error";

    private readonly AppDbContext db;
    private readonly IDialogoService dialogo;
    private readonly Action<Idioma> cambiarIdioma;
    private readonly GestorAdjuntos adjuntos;
    private bool actualizandoIdioma;

    // Constructor con base de datos, preferencias y servicios propios: los tests usan una base
    // temporal, no tocan la preferencia real de %APPDATA% y observan los avisos con un falso.
    // Sin servicio, los diálogos se ignoran (DialogosNulos). Sin cambiarIdioma, cambiar la
    // propiedad no toca la cultura global: en producción se pasa Localizacion.Cambiar.
    internal MainViewModel(
        AppDbContext db,
        string? rutaColumnas = null,
        string? carpetaAdjuntos = null,
        IDialogoService? dialogo = null,
        Action<Idioma>? cambiarIdioma = null)
    {
        this.db = db;
        this.ColumnasVm = new ColumnasViewModel(rutaColumnas);
        this.adjuntos = new GestorAdjuntos(carpetaAdjuntos ?? AdjuntosHelper.Carpeta);
        this.dialogo = dialogo ?? new DialogosNulos();
        this.cambiarIdioma = cambiarIdioma ?? (_ => { });

        this.CargarTextosLocalizados();

        // Asignación directa al campo para no disparar la recarga dos veces.
        this.estadoFiltroItem = this.EstadosFiltro[0];
        this.granularidadEmbudoItem = this.GranularidadesEmbudo[0];

        Localizacion.IdiomaCambiado += this.CuandoCambiaIdioma;

        this.Recargar();
    }

    /// <summary>
    /// Se desengancha del evento estático de idioma: si no, cada ViewModel construido
    /// (los tests crean decenas) queda retenido para siempre.
    /// </summary>
    public void Dispose()
    {
        Localizacion.IdiomaCambiado -= this.CuandoCambiaIdioma;
        GC.SuppressFinalize(this);
    }

    // ---------------------------------------------------------------- Listas
    [ObservableProperty]
    private IReadOnlyList<EnumItem> estados = Array.Empty<EnumItem>();

    [ObservableProperty]
    private IReadOnlyList<EnumItem> estadosFiltro = Array.Empty<EnumItem>();

    [ObservableProperty]
    private IReadOnlyList<EnumItem> modalidades = Array.Empty<EnumItem>();

    [ObservableProperty]
    private IReadOnlyList<EnumItem> tiposEvento = Array.Empty<EnumItem>();

    /// <summary>Gets mes / semana / día para el desplegable de la gráfica.</summary>
    [ObservableProperty]
    private IReadOnlyList<EnumItem> granularidadesEmbudo = Array.Empty<EnumItem>();

    /// <summary>Gets vías de contacto (aplicación directa, recruiter, referido...), para el desplegable.</summary>
    [ObservableProperty]
    private IReadOnlyList<EnumItem> origenes = Array.Empty<EnumItem>();

    /// <summary>Gets motivos de rechazo, con un valor "sin especificar" al principio para poder dejarlo en blanco.</summary>
    [ObservableProperty]
    private IReadOnlyList<EnumItem> motivosRechazo = Array.Empty<EnumItem>();

    /// <summary>Gets valores posibles del interés (1 a 5), para el desplegable del panel de detalle.</summary>
    public IReadOnlyList<int> NivelesInteres { get; } = new[] { 1, 2, 3, 4, 5 };

    // ---------------------------------------------------------------- Idioma
    [ObservableProperty]
    private Idioma idiomaActual = Localizacion.IdiomaActual;

    [ObservableProperty]
    private IReadOnlyList<IdiomaItem> idiomasDisponibles = Localizacion.IdiomasDisponibles();

    partial void OnIdiomaActualChanged(Idioma value) => this.cambiarIdioma(value);

    /// <summary>
    /// Reconstruye los desplegables y nombres de idioma con los textos del idioma activo.
    /// Los enums se guardan por valor numérico, así que solo cambia el texto mostrado.
    /// </summary>
    private void CargarTextosLocalizados()
    {
        this.Estados = EnumHelper.Valores<EstadoSolicitud>();
        this.Modalidades = EnumHelper.Valores<Modalidad>();
        this.TiposEvento = EnumHelper.Valores<TipoEvento>();
        this.Origenes = EnumHelper.Valores<Origen>();
        this.GranularidadesEmbudo = EnumHelper.Valores<GranularidadEmbudo>();

        this.MotivosRechazo = new List<EnumItem> { new(null, Localizacion.Texto("Filtro.SinEspecificar")) }
            .Concat(EnumHelper.Valores<MotivoRechazo>())
            .ToList();

        this.EstadosFiltro = new List<EnumItem> { new(null, Localizacion.Texto("Filtro.TodosLosEstados")) }
            .Concat(this.Estados)
            .ToList();

        // IdiomasDisponibles no se toca aquí a propósito: los tres nombres son fijos
        // (cada uno en su propia lengua), y reconstruir la lista forzaría al ComboBox
        // del selector a re-sincronizar su selección.

        // Los nombres de las columnas ocultables salen del diccionario, así que también
        // se releen al cambiar de idioma.
        this.ColumnasVm.RefrescarNombres();
    }

    // ---------------------------------------------------------------- Columnas

    /// <summary>Preferencia de columnas visibles y estado del panel del botón "Columnas".</summary>
    public ColumnasViewModel ColumnasVm { get; }

    /// <summary>
    /// Al cambiar de idioma: se refrescan los desplegables, la lista, las estadísticas y la
    /// gráfica para que lo que no se repinta solo (combos, celdas del DataGrid) lo haga.
    /// </summary>
    /// <remarks>
    /// Se puede invocar directamente con el idioma activo para probar el refresco sin mutar
    /// el idioma global ni escribir la preferencia en %APPDATA%.
    /// </remarks>
    internal void CuandoCambiaIdioma(object? sender, Idioma idioma)
    {
        // El ComboBox se sincroniza aunque el cambio venga de fuera del ViewModel.
        this.IdiomaActual = idioma;

        EstadoSolicitud? filtro = this.EstadoFiltroItem?.Valor as EstadoSolicitud?;

        // Mientras se reasignan los desplegables, el filtro no debe recargar por su cuenta:
        // la recarga se hace una sola vez al final.
        this.actualizandoIdioma = true;
        try
        {
            this.CargarTextosLocalizados();

            // El item seleccionado del filtro pertenecía a la lista anterior: se reencuentra
            // por valor en la nueva (los objetos son distintos aunque representen lo mismo).
            this.EstadoFiltroItem = this.EstadosFiltro.FirstOrDefault(i => Equals(i.Valor, filtro))
                ?? this.EstadosFiltro[0];

            this.RefrescarItemGranularidad();

            // Los botones de la gráfica usan un convertidor: se notifica para que se re-evalúen.
            this.OnPropertyChanged(nameof(this.VerGrafica));

            // El título de la gráfica se arma en el ViewModel con el texto de la granularidad.
            this.OnPropertyChanged(nameof(this.TituloGrafica));

            if (this.VerGrafica)
            {
                this.ActualizarEmbudo();
            }
        }
        finally
        {
            this.actualizandoIdioma = false;
        }

        this.Recargar();
        this.RefrescarPanelDetalle();
    }

    [ObservableProperty]
    private ObservableCollection<Solicitud> solicitudes = new();

    // ---------------------------------------------------------------- Paginación

    /// <summary>Filas por página que ofrece el desplegable.</summary>
    public IReadOnlyList<int> TamanosPagina { get; } = new[] { 5, 10, 20, 30 };

    [ObservableProperty]
    private int tamanoPagina = 10;

    // Al cambiar el tamaño se repagina la lista, quedándose en la página actual si sigue existiendo.
    partial void OnTamanoPaginaChanged(int value) => this.Recargar();

    /// <summary>Página actual, el rango y si se puede mover cada flecha: un solo estado.</summary>
    [ObservableProperty]
    private ResultadoPagina<Solicitud> pagina =
        new(Array.Empty<Solicitud>(), 1, 0, 0, 0, 0);

    /// <summary>Texto "1-10 de 47" que se ve junto a las flechas.</summary>
    public string RangoPagina => this.Pagina.Total == 0
        ? $"0 {Localizacion.Texto("Paginacion.De")} 0"
        : $"{this.Pagina.Desde + 1}-{this.Pagina.Hasta} {Localizacion.Texto("Paginacion.De")} {this.Pagina.Total}";

    [RelayCommand]
    private void IrAInicio() => this.IrAPagina(1);

    [RelayCommand]
    private void IrAtras() => this.IrAPagina(this.Pagina.PaginaActual - 1);

    [RelayCommand]
    private void IrAdelante() => this.IrAPagina(this.Pagina.PaginaActual + 1);

    [RelayCommand]
    private void IrAlFinal() => this.IrAPagina(this.Pagina.TotalPaginas);

    private void IrAPagina(int pagina)
    {
        int destino = Math.Clamp(pagina, 1, Math.Max(this.Pagina.TotalPaginas, 1));

        if (destino == this.Pagina.PaginaActual)
        {
            return;
        }

        // Recargar vuelve a filtrar la base y vuelve a recortar: un solo camino para los filtros
        // y para las flechas, sin listas intermedias que se queden desfasadas.
        this.Recargar(destino);
    }

    /// <summary>
    /// Vuelca en <see cref="Solicitudes"/> la página actual de <paramref name="filtradas"/> y
    /// recalcula el rango y los botones. El recorte lo hace <see cref="Paginador"/>; aquí solo
    /// se formatea el rango con el idioma activo. WPF no pagina las vistas de colección, así que
    /// la exportación a CSV no pasa por esta lista y sigue saliendo entera.
    /// </summary>
    private void Paginar(List<Solicitud> filtradas, int? pagina = null)
    {
        this.Pagina = Paginador.Paginar(
            filtradas,
            this.TamanoPagina,
            pagina ?? this.Pagina.PaginaActual);

        this.Solicitudes = new ObservableCollection<Solicitud>(this.Pagina.Elementos);

        // El rango es texto traducido: se recalcula aunque la página sea la misma (cambio de idioma).
        this.OnPropertyChanged(nameof(this.RangoPagina));
    }

    // ---------------------------------------------------------------- Filtros
    [ObservableProperty]
    private string textoBusqueda = string.Empty;

    partial void OnTextoBusquedaChanged(string value) => Recargar();

    [ObservableProperty]
    private EnumItem? estadoFiltroItem;

    partial void OnEstadoFiltroItemChanged(EnumItem? value)
    {
        if (!this.actualizandoIdioma)
        {
            this.Recargar();
        }
    }

    [ObservableProperty]
    private bool soloAbiertas;

    partial void OnSoloAbiertasChanged(bool value) => Recargar();

    [ObservableProperty]
    private bool soloConSeguimientoPendiente;

    partial void OnSoloConSeguimientoPendienteChanged(bool value) => Recargar();

    // ---------------------------------------------------------------- Selección
    [ObservableProperty]
    private Solicitud? solicitudSeleccionada;

    partial void OnSolicitudSeleccionadaChanged(Solicitud? value) => this.Seleccionar(value);

    /// <summary>
    /// Abre una fila en el panel de detalle. Hace E/S a propósito (carga el historial, que
    /// solo usa el detalle) y por eso es un método explícito: el setter existe solo porque
    /// el DataGrid enlaza <c>SelectedItem</c> en dos direcciones.
    /// </summary>
    private void Seleccionar(Solicitud? value)
    {
        // Ignoramos el null que manda el DataGrid al recargar la lista o al pulsar "Nueva".
        if (value is not null)
        {
            this.db.Entry(value).Collection(s => s.Eventos).Load();
            this.Edicion = value;
        }
    }

    /// <summary>Entidad mostrada en el panel de detalle.</summary>
    [ObservableProperty]
    private Solicitud? edicion;

    // ---------------------------------------------------------------- Estadísticas
    [ObservableProperty]
    private int totalSolicitudes;
    [ObservableProperty]
    private int procesosAbiertos;
    [ObservableProperty]
    private int enEntrevista;
    [ObservableProperty]
    private int ofertasRecibidas;
    [ObservableProperty]
    private int seguimientosPendientes;
    [ObservableProperty]
    private string tasaRespuesta = "—";
    [ObservableProperty]
    private string mediaDiasRespuesta = "—";

    // ---------------------------------------------------------------- Gráfica de embudo
    [ObservableProperty]
    private bool verGrafica;

    /// <summary>Filas del embudo (una por mes, semana o día) con las cuatro series ya escaladas a porcentaje.</summary>
    [ObservableProperty]
    private IReadOnlyList<EmbudoPeriodo> periodosEmbudo = new List<EmbudoPeriodo>();

    /// <summary>Cada cuánto agrupa las barras: mes, semana o día.</summary>
    [ObservableProperty]
    private GranularidadEmbudo granularidadGrafica = GranularidadEmbudo.Mes;

    private EnumItem? granularidadEmbudoItem;

    /// <summary>
    /// Item del desplegable de granularidad. El ComboBox va por SelectedItem y no por
    /// SelectedValue porque, al cambiar de idioma, la lista se reconstruye con objetos nuevos
    /// y SelectedValue se quedaría sin nada seleccionado. Por eso la propiedad está escrita a
    /// mano y no con [ObservableProperty]: EnumItem es un record, así que el item nuevo es igual
    /// al viejo y el setter generado se saltaría la asignación.
    /// </summary>
    public EnumItem? GranularidadEmbudoItem
    {
        get => this.granularidadEmbudoItem;
        set
        {
            this.granularidadEmbudoItem = value;
            this.OnPropertyChanged();

            // El null que el ComboBox puede mandar a mitad de la actualización no cambia nada.
            if (value?.Valor is GranularidadEmbudo granularidad)
            {
                this.GranularidadGrafica = granularidad;
            }
        }
    }

    /// <summary>
    /// Reencuentra en la lista actual el item de la granularidad elegida. Sin esto, tras
    /// cambiar de idioma el desplegable se queda vacío, como se quedaba el filtro de estados.
    /// </summary>
    internal void RefrescarItemGranularidad()
    {
        GranularidadEmbudo granularidad = this.GranularidadGrafica;
        this.GranularidadEmbudoItem = this.GranularidadesEmbudo.FirstOrDefault(i => Equals(i.Valor, granularidad))
            ?? this.GranularidadesEmbudo[0];
    }

    /// <summary>
    /// Cuántos periodos se retrocede desde el actual (0 = el que contiene hoy). Hacia atrás
    /// el tope es la primera solicitud; hacia delante no hay ninguno, porque el futuro no
    /// tiene datos.
    /// </summary>
    [ObservableProperty]
    private int desplazamientoPeriodo;

    /// <summary>Deshabilita la flecha de avanzar cuando ya se está en el periodo actual.</summary>
    [ObservableProperty]
    private bool puedeAvanzarPeriodo;

    /// <summary>Deshabilita la flecha de retroceder cuando la ventana ya llega a la primera solicitud.</summary>
    [ObservableProperty]
    private bool puedeRetrocederPeriodo = true;

    partial void OnGranularidadGraficaChanged(GranularidadEmbudo value)
    {
        this.OnPropertyChanged(nameof(this.TituloGrafica));

        // El suelo se mide en periodos, así que también cambia al cambiar de granularidad.
        this.ActualizarLimitesEmbudo();

        // El desplazamiento se conserva: si estabas tres meses atrás, al pasar a semanas
        // sigues tres semanas atrás, que es el mismo instante de la historia.
        this.RecargarEmbudoSiVisible();
    }

    partial void OnDesplazamientoPeriodoChanged(int value)
    {
        this.PuedeAvanzarPeriodo = value > 0;
        this.ActualizarLimitesEmbudo();
        this.RecargarEmbudoSiVisible();
    }

    /// <summary>
    /// Recalcula si la ventana puede retroceder otro periodo. El suelo es la primera
    /// solicitud; con la base vacía no hay suelo conocido y se deja ir hacia atrás.
    /// </summary>
    private void ActualizarLimitesEmbudo()
    {
        DateTime? primera = this.db.Solicitudes.Min(s => (DateTime?)s.FechaSolicitud);

        this.PuedeRetrocederPeriodo = primera is not DateTime suelo
            || EmbudoHelper.CalcularPeriodos(this.GranularidadGrafica, this.DesplazamientoPeriodo + 1)[^1]
                >= EmbudoHelper.InicioDePeriodo(suelo, this.GranularidadGrafica);
    }

    /// <summary>Título de la gráfica, que nombra la granularidad activa (se repinta al cambiar de idioma).</summary>
    public string TituloGrafica => string.Format(
        Localizacion.CulturaActual,
        Localizacion.Texto("Grafica.Titulo"),
        TextoTitulo(this.GranularidadGrafica));

    /// <summary>
    /// Texto de la granularidad que completa el título. Se elige por valor del enum, no por
    /// su número: las claves son TituloMes, TituloSemana y TituloDia.
    /// </summary>
    private static string TextoTitulo(GranularidadEmbudo granularidad) => granularidad switch
    {
        GranularidadEmbudo.Semana => Localizacion.Texto("Grafica.TituloSemana"),
        GranularidadEmbudo.Dia => Localizacion.Texto("Grafica.TituloDia"),
        _ => Localizacion.Texto("Grafica.TituloMes"),
    };

    [RelayCommand]
    private void PeriodoAnterior()
    {
        if (this.PuedeRetrocederPeriodo)
        {
            this.DesplazamientoPeriodo++;
        }
    }

    [RelayCommand]
    private void PeriodoSiguiente() => this.DesplazamientoPeriodo = Math.Max(0, this.DesplazamientoPeriodo - 1);

    /// <summary>Vuelve al periodo actual (el único que se puede alcanzar hacia delante).</summary>
    [RelayCommand]
    private void IrAHoy() => this.DesplazamientoPeriodo = 0;

    // ---------------------------------------------------------------- Carga

    /// <summary>
    /// Recarga la lista, los filtros y las estadísticas. Público: la ventana lo
    /// usa para refrescar los contadores tras avisar de seguimientos vencidos.
    /// Con <paramref name="pagina"/>, repagina sobre esa página en vez de la actual.
    /// </summary>
    public void Recargar(int? pagina = null)
    {
        IQueryable<Solicitud> consulta = this.db.Solicitudes;

        if (this.EstadoFiltroItem?.Valor is EstadoSolicitud estado)
        {
            consulta = consulta.Where(s => s.Estado == estado);
        }

        List<Solicitud> lista = consulta
            .OrderByDescending(s => s.FechaSolicitud)
            .ThenByDescending(s => s.Id)
            .ToList();

        // La búsqueda de texto se hace en memoria porque SQLite no sabe ignorar
        // acentos: normalizamos (minúsculas y sin tildes) el texto y los campos.
        if (!string.IsNullOrWhiteSpace(this.TextoBusqueda))
        {
            string texto = CsvHelper.NormalizarBusqueda(this.TextoBusqueda);
            lista = lista.Where(s => CamposBuscables(s).Contains(texto)).ToList();
        }

        // EstaAbierta y SeguimientoPendiente son [NotMapped]: se filtran en memoria.
        if (this.SoloAbiertas)
        {
            lista = lista.Where(s => s.EstaAbierta).ToList();
        }

        if (this.SoloConSeguimientoPendiente)
        {
            lista = lista.Where(s => s.SeguimientoPendiente).ToList();
        }

        this.Paginar(lista, pagina);
        this.ActualizarEstadisticas();
        this.ActualizarLimitesEmbudo();
    }

    /// <summary>
    /// Los campos del buscador en una sola cadena ya normalizada. Se separan con salto de
    /// línea para que una búsqueda no case a caballo entre dos campos.
    /// </summary>
    private static string CamposBuscables(Solicitud solicitud) =>
        CsvHelper.NormalizarBusqueda(string.Join(
            '\n',
            solicitud.Empresa,
            solicitud.Puesto,
            solicitud.Tecnologias,
            solicitud.Ubicacion,
            solicitud.Portal));

    /// <summary>
    /// La cabecera mide la base entera, no la lista filtrada: los filtros solo recortan la tabla.
    /// Así los totales no bailan al buscar o al marcar «Solo abiertas».
    /// </summary>
    private void ActualizarEstadisticas()
    {
        // Se leen las entidades completas: las estadísticas usan propiedades [NotMapped] y
        // proyectar a un DTO obligaría a duplicar sus cálculos. La consulta de solo lectura
        // queda fuera del change tracking.
        List<Solicitud> paraCalculo = this.db.Solicitudes
            .AsNoTracking()
            .ToList();

        ResumenEstadisticas resumen = EstadisticasSolicitud.Calcular(paraCalculo);

        this.TotalSolicitudes = resumen.Total;
        this.ProcesosAbiertos = resumen.Abiertas;
        this.OfertasRecibidas = resumen.Ofertas;
        this.EnEntrevista = resumen.EnEntrevista;
        this.SeguimientosPendientes = resumen.SeguimientosPendientes;
        this.TasaRespuesta = FormatearTasaRespuesta(resumen.TasaRespuesta);
        this.MediaDiasRespuesta = FormatearMediaDiasRespuesta(resumen.MediaDiasRespuesta);
    }

    private static string FormatearTasaRespuesta(double? tasa) =>
        tasa is double valor
            ? valor.ToString("P0", Localizacion.CulturaActual)
            : "—";

    private static string FormatearMediaDiasRespuesta(double? media) =>
        media is double valor
            ? $"{valor.ToString("0.#", Localizacion.CulturaActual)} {Localizacion.Texto("Metrica.Dias")}"
            : "—";

    // ---------------------------------------------------------------- Comandos
    [RelayCommand]
    private void ConfigurarGrafica()
    {
        this.VerGrafica = !this.VerGrafica;
        if (this.VerGrafica)
        {
            this.ActualizarEmbudo();
        }
    }

    /// <summary>Si el panel de la gráfica está abierto, la refresca con los datos actuales.</summary>
    private void RecargarEmbudoSiVisible()
    {
        if (this.VerGrafica)
        {
            this.ActualizarEmbudo();
        }
    }

    /// <summary>
    /// Embudo enviadas → respondidas → entrevistas → ofertas de los últimos 12 periodos
    /// (meses, semanas o días según elijamos). Se apoya en el historial (Eventos) para
    /// fechar cada hito con precisión.
    /// </summary>
    private void ActualizarEmbudo()
    {
        List<Solicitud> todas = this.db.Solicitudes.Include(s => s.Eventos).ToList();

        this.PeriodosEmbudo = EmbudoHelper.Calcular(todas, this.GranularidadGrafica, this.DesplazamientoPeriodo);
    }

    /// <summary>Seguimientos vencidos de candidaturas abiertas. Lo usa el icono de la bandeja.</summary>
    internal List<SeguimientoVencido> SeguimientosVencidosAhora()
    {
        DateTime hoy = DateTime.Today;
        return this.db.Solicitudes
            .AsNoTracking()
            .Where(s => s.ProximoSeguimiento != null && s.ProximoSeguimiento.Value.Date <= hoy)
            .Where(s => !EstadoSolicitudExtensiones.Cerrados.Contains(s.Estado))
            .Select(s => new SeguimientoVencido(s.Id, s.Empresa, s.Puesto, s.ProximoSeguimiento))
            .ToList();
    }

    // ---------------------------------------------------------------- Adjuntos

    /// <summary>CV y carta se adjuntan y se quitan igual: sólo cambian la etiqueta del fichero y las propiedades.</summary>
    private sealed record DescriptorAdjunto(string Etiqueta, Action<Solicitud, string?, string?> Asignar);

    private static readonly DescriptorAdjunto AdjuntoCv = new(
        "cv",
        (solicitud, ruta, nombre) =>
        {
            solicitud.RutaCv = ruta;
            solicitud.NombreOriginalCv = nombre;
        });

    private static readonly DescriptorAdjunto AdjuntoCarta = new(
        "carta",
        (solicitud, ruta, nombre) =>
        {
            solicitud.RutaCarta = ruta;
            solicitud.NombreOriginalCarta = nombre;
        });

    [RelayCommand]
    private void AdjuntarCv() => this.AdjuntarAdjunto(AdjuntoCv, Localizacion.Texto("Dialogo.SeleccionaCv"));

    [RelayCommand]
    private void AdjuntarCarta() => this.AdjuntarAdjunto(AdjuntoCarta, Localizacion.Texto("Dialogo.SeleccionaCarta"));

    private void AdjuntarAdjunto(DescriptorAdjunto adjunto, string titulo)
    {
        Solicitud? editar = this.Edicion;
        if (editar is null)
        {
            return;
        }

        string? origen = this.dialogo.ElegirFichero(titulo, Localizacion.Texto("Dialogo.FiltroAdjuntos"));
        if (origen is null)
        {
            return;
        }

        try
        {
            string nuevaRuta = this.adjuntos.Copiar(origen, adjunto.Etiqueta);
            adjunto.Asignar(editar, nuevaRuta, Path.GetFileName(origen));
        }
        catch (Exception ex)
        {
            this.dialogo.Error(
                string.Format(Localizacion.Texto("Mensaje.NoSePudoAdjuntar"), ex.Message),
                Localizacion.Texto(ClaveError));
            return;
        }

        // Las entidades son POCOs sin INotifyPropertyChanged: se reasigna Edicion
        // al mismo objeto para que el panel de detalle repinte el nombre del adjunto.
        this.RefrescarPanelDetalle();
    }

    [RelayCommand]
    private void QuitarCv() => this.QuitarAdjunto(AdjuntoCv);

    [RelayCommand]
    private void QuitarCarta() => this.QuitarAdjunto(AdjuntoCarta);

    /// <summary>
    /// Quita la ruta del adjunto, pero no el fichero: si el usuario cancela, el que
    /// había en disco sigue siendo el que apunta la base de datos.
    /// </summary>
    private void QuitarAdjunto(DescriptorAdjunto adjunto)
    {
        if (this.Edicion is null)
        {
            return;
        }

        adjunto.Asignar(this.Edicion, null, null);
        this.RefrescarPanelDetalle();
    }

    private void RefrescarPanelDetalle()
    {
        Solicitud? actual = this.Edicion;
        this.Edicion = null;
        this.Edicion = actual;
    }

    /// <summary>
    /// Las rutas que apunta hoy la base de datos, no las que el usuario acaba de elegir.
    /// Exige que <paramref name="solicitud"/> esté en seguimiento por el contexto: si estuviera
    /// separada, EF no conserva el valor original y devolvería el actual.
    /// </summary>
    private (string? Cv, string? Carta) RutasGuardadas(Solicitud solicitud)
    {
        EntityEntry<Solicitud> entrada = this.db.Entry(solicitud);
        return (entrada.Property(s => s.RutaCv).OriginalValue,
                entrada.Property(s => s.RutaCarta).OriginalValue);
    }

    [RelayCommand]
    private void Nueva()
    {
        this.SolicitudSeleccionada = null;
        this.Edicion = new Solicitud
        {
            FechaSolicitud = DateTime.Today,
            Estado = EstadoSolicitud.Enviada,
        };
    }

    /// <summary>
    /// Crea una candidatura nueva a partir de la seleccionada: util para volver a
    /// aplicar a la misma empresa (otro puesto) o a una oferta muy parecida.
    /// No copia fechas de proceso, contacto, adjuntos ni el enlace, porque casi
    /// seguro cambian; si copia lo que describe la oferta en si.
    /// </summary>
    [RelayCommand]
    private void Duplicar()
    {
        if (this.Edicion is null || this.Edicion.Id == 0)
        {
            return;
        }

        Solicitud origen = this.Edicion;
        var copia = new Solicitud
        {
            Empresa = origen.Empresa,
            Puesto = origen.Puesto,
            Portal = origen.Portal,
            Ubicacion = origen.Ubicacion,
            Modalidad = origen.Modalidad,
            Tecnologias = origen.Tecnologias,
            SalarioMin = origen.SalarioMin,
            SalarioMax = origen.SalarioMax,
            PretensionSalarial = origen.PretensionSalarial,
            Interes = origen.Interes,
            FechaSolicitud = DateTime.Today,
            Estado = EstadoSolicitud.Enviada,
            Notas = string.Format(
                Localizacion.Texto("Duplicar.Nota"),
                origen.Id, origen.Empresa, origen.FechaSolicitud.ToString("d", Localizacion.CulturaActual)),
        };

        this.SolicitudSeleccionada = null;
        this.Edicion = copia;
    }

    /// <summary>
    /// Filtra la lista a todas las candidaturas de la misma empresa (usando la
    /// búsqueda de texto ya existente): la forma más barata de "agrupar por
    /// empresa" sin construir una vista nueva.
    /// </summary>
    [RelayCommand]
    private void VerCandidaturasDeEstaEmpresa()
    {
        if (this.Edicion is null || string.IsNullOrWhiteSpace(this.Edicion.Empresa))
        {
            return;
        }

        this.TextoBusqueda = this.Edicion.Empresa;
    }

    /// <summary>Borra el texto de búsqueda para volver a mostrar la lista completa.</summary>
    [RelayCommand]
    private void LimpiarBusqueda() => this.TextoBusqueda = string.Empty;

    [RelayCommand]
    private void Guardar()
    {
        if (this.Edicion is not Solicitud solicitud || !this.Validar(solicitud))
        {
            return;
        }

        bool esNueva = this.PrepararNueva(solicitud);

        // Lo que apunta hoy la base, para borrar el adjunto viejo solo si el guardado funciona.
        (string? cvAnterior, string? cartaAnterior) = esNueva
            ? (null, null)
            : this.RutasGuardadas(solicitud);

        if (!this.Persistir())
        {
            return;
        }

        this.adjuntos.DescartarSustituidas(solicitud, (cvAnterior, cartaAnterior));
        this.CerrarEdicion();
    }

    /// <summary>Comprueba lo mínimo para guardar; si falta algo, avisa y deja el panel abierto.</summary>
    private bool Validar(Solicitud solicitud)
    {
        if (ReglasSolicitud.TieneDatosObligatorios(solicitud))
        {
            return true;
        }

        this.dialogo.Avisar(
            Localizacion.Texto("Mensaje.FaltanDatos"),
            Localizacion.Texto("Titulo.FaltanDatos"));
        return false;
    }

    /// <summary>
    /// Propone el seguimiento y, si la candidatura es nueva, la añade con su primer hito.
    /// Devuelve si era nueva.
    /// </summary>
    private bool PrepararNueva(Solicitud solicitud)
    {
        // Si el proceso sigue abierto y no se ha fijado cuando tocaria volver a
        // escribir, proponemos +7 dias desde el envio.
        if (ReglasSolicitud.ProponerSeguimiento(solicitud) is DateTime propuesta)
        {
            solicitud.ProximoSeguimiento = propuesta;
        }

        if (solicitud.Id != 0)
        {
            return false;
        }

        this.db.Solicitudes.Add(solicitud);

        // Primer hito del historial, para que la línea temporal no empiece vacía.
        ReglasSolicitud.AnadirHitoInicial(solicitud);
        return true;
    }

    /// <summary>Escribe los cambios; si la base falla, avisa y deja el panel abierto.</summary>
    private bool Persistir()
    {
        try
        {
            this.db.SaveChanges();
            return true;
        }
        catch (Exception ex)
        {
            this.dialogo.Error(
                string.Format(Localizacion.Texto("Mensaje.NoSePudoGuardar"), ex.Message),
                Localizacion.Texto(ClaveError));
            return false;
        }
    }

    /// <summary>Cierra el panel y refresca lista, cabecera y embudo.</summary>
    private void CerrarEdicion()
    {
        this.Recargar();
        this.RecargarEmbudoSiVisible();
        this.SolicitudSeleccionada = null;
        this.Edicion = null;
    }

    [RelayCommand]
    private void Cancelar()
    {
        if (this.Edicion is not Solicitud solicitud)
        {
            return;
        }

        // Lo que apunta la base, no lo que el usuario acaba de cambiar sin guardar.
        (string? cvGuardado, string? cartaGuardada) = solicitud.Id == 0
            ? (null, null)
            : this.RutasGuardadas(solicitud);

        this.adjuntos.DescartarNoGuardadas(solicitud, (cvGuardado, cartaGuardada));

        // Desenganchamos lo que EF tenía en seguimiento: los cambios pendientes
        // se pierden y la siguiente consulta vuelve a traer los datos de disco.
        this.db.ChangeTracker.Clear();

        this.Edicion = null;
        this.SolicitudSeleccionada = null;
        this.Recargar();
    }

    [RelayCommand]
    private void Eliminar()
    {
        if (this.Edicion is null || this.Edicion.Id == 0)
        {
            return;
        }

        bool confirmado = this.dialogo.Confirmar(
            string.Format(Localizacion.Texto("Mensaje.ConfirmarEliminar"), this.Edicion.Empresa, this.Edicion.Puesto),
            Localizacion.Texto("Titulo.Confirmar"));

        if (!confirmado)
        {
            return;
        }

        // Lo que apunta la base, no lo que el usuario acaba de cambiar sin guardar.
        (string? cv, string? carta) = this.RutasGuardadas(this.Edicion);

        if (!this.TryEliminarSolicitud(this.Edicion, out Exception? error))
        {
            this.dialogo.Error(
                string.Format(Localizacion.Texto("Mensaje.NoSePudoEliminar"), error!.Message),
                Localizacion.Texto(ClaveError));
            return;
        }

        this.adjuntos.Eliminar(cv);
        this.adjuntos.Eliminar(carta);
        this.adjuntos.DescartarCopias();

        this.Edicion = null;
        this.SolicitudSeleccionada = null;
        this.Recargar();
        this.RecargarEmbudoSiVisible();
    }

    /// <summary>
    /// Borra la candidatura de la base. Devuelve <c>false</c> y el fallo en <paramref name="error"/>
    /// si no se pudo borrar, para que el comando avise sin lanzar.
    /// </summary>
    internal bool TryEliminarSolicitud(Solicitud solicitud, out Exception? error)
    {
        try
        {
            this.db.Solicitudes.Remove(solicitud);
            this.db.SaveChanges();
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex;
            return false;
        }
    }

    [RelayCommand]
    private void AbrirEnlace()
    {
        string? url = this.Edicion?.EnlaceOferta;
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            this.dialogo.AbrirEnlace(url.Trim());
        }
        catch (Exception ex)
        {
            this.dialogo.Avisar(
                string.Format(Localizacion.Texto("Mensaje.NoSePudoAbrirEnlace"), ex.Message),
                Localizacion.Texto(ClaveError));
        }
    }

    [RelayCommand]
    private void AnadirEvento()
    {
        this.Edicion?.Eventos.Add(new Evento
        {
            Fecha = DateTime.Today,
            Tipo = TipoEvento.Nota,
            Descripcion = string.Empty,
        });
    }

    [RelayCommand]
    private void EliminarEvento(Evento? evento)
    {
        if (this.Edicion is null || evento is null)
        {
            return;
        }

        this.Edicion.Eventos.Remove(evento);
        if (evento.Id != 0)
        {
            this.db.Eventos.Remove(evento);
        }
    }

    [RelayCommand]
    private void ExportarCsv()
    {
        string? destino = this.dialogo.ElegirDestino(
            Localizacion.Texto("Dialogo.ExportarCandidaturas"),
            "CSV (*.csv)|*.csv",
            $"solicitudes-{DateTime.Today:yyyy-MM-dd}.csv");

        if (destino is null)
        {
            return;
        }

        if (!this.TryEscribirCsv(destino, out Exception? error))
        {
            this.dialogo.Error(
                string.Format(Localizacion.Texto("Mensaje.NoSePudoExportar"), error!.Message),
                Localizacion.Texto(ClaveError));
            return;
        }

        this.dialogo.Informar(
            string.Format(Localizacion.Texto("Csv.ExportadasN"), this.db.Solicitudes.Count()),
            Localizacion.Texto("Titulo.ExportacionCompletada"));
    }

    /// <summary>
    /// Escribe el CSV. Devuelve <c>false</c> y el fallo en <paramref name="error"/> si no se pudo
    /// escribir, para que el comando avise sin lanzar.
    /// </summary>
    internal bool TryEscribirCsv(string ruta, out Exception? error)
    {
        try
        {
            File.WriteAllText(ruta, this.ConstruirCsv(), new UTF8Encoding(true));
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex;
            return false;
        }
    }

    internal string ConstruirCsv() =>
        CsvExportador.Construir(this.db.Solicitudes
            .OrderByDescending(x => x.FechaSolicitud)
            .ThenByDescending(x => x.Id)
            .ToList());
}
