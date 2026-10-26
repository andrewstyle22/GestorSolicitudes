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

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    // La cultura de los números y fechas se toma del idioma activo; los tests que no pasan
    // por el arranque de la app se apoyan en que por defecto el idioma es el castellano.
    private const string ClaveError = "Error";

    private readonly AppDbContext db;
    private readonly string rutaColumnas;
    private readonly string carpetaAdjuntos;
    private readonly IDialogoService dialogo;
    private readonly List<string> adjuntosCopiados = new();
    private bool actualizandoIdioma;

    // Constructor con base de datos, preferencias y servicio de diálogos propios: los tests usan
    // una base temporal, no tocan la preferencia real de %APPDATA% y observan los avisos con un
    // falso. Sin servicio, los diálogos se ignoran (DialogosNulos).
    internal MainViewModel(
        AppDbContext db,
        string? rutaColumnas = null,
        string? carpetaAdjuntos = null,
        IDialogoService? dialogo = null)
    {
        this.db = db;
        this.rutaColumnas = rutaColumnas ?? ColumnasHelper.Ruta;
        this.carpetaAdjuntos = carpetaAdjuntos ?? AdjuntosHelper.Carpeta;
        this.dialogo = dialogo ?? new DialogosNulos();

        this.CargarTextosLocalizados();
        this.CargarColumnas();

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

    partial void OnIdiomaActualChanged(Idioma value) => Localizacion.Cambiar(value);

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
        foreach (ColumnaItem columna in this.Columnas)
        {
            columna.RefrescarNombre();
        }
    }

    // ---------------------------------------------------------------- Columnas

    /// <summary>
    /// Columnas de la tabla que el usuario puede ocultar. Empresa, Puesto, Estado y
    /// Enviada no entran aquí: se ven siempre.
    /// </summary>
    internal static readonly string[] ColumnasOcultables =
        ["Dias", "PrimeraRespuesta", "Entrevista", "Seguimiento", "Interes", "Portal"];

    public ObservableCollection<ColumnaItem> Columnas { get; } = new();

    /// <summary>Abre y cierra la lista de checkboxes del botón "Columnas".</summary>
    [ObservableProperty]
    private bool panelColumnasAbierto;

    /// <summary>
    /// La vista se suscribe para ocultar las columnas del DataGrid: un DataGridColumn no
    /// hereda el DataContext y no se puede enlazar, así que el aviso llega por aquí.
    /// </summary>
    public event Action? ColumnaVisibilidadCambiada;

    /// <summary>Las columnas ocultables con el estado guardado, o todas si no hay preferencia.</summary>
    private void CargarColumnas()
    {
        HashSet<string>? guardadas = ColumnasHelper.CargarDe(this.rutaColumnas);

        foreach (string columna in ColumnasOcultables)
        {
            this.Columnas.Add(new ColumnaItem(
                columna,
                guardadas is null || guardadas.Contains(columna),
                this.GuardarColumnas));
        }
    }

    /// <summary>Guarda la preferencia y avisa a la vista para que aplique el nuevo estado.</summary>
    private void GuardarColumnas()
    {
        ColumnasHelper.GuardarEn(
            this.rutaColumnas,
            this.Columnas.Where(c => c.Visible).Select(c => c.Columna));

        this.ColumnaVisibilidadCambiada?.Invoke();
    }

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

    /// <summary>Página actual, contando desde 1.</summary>
    [ObservableProperty]
    private int paginaActual = 1;

    [ObservableProperty]
    private int totalPaginas;

    /// <summary>Texto "1-10 de 47" que se ve junto a las flechas.</summary>
    [ObservableProperty]
    private string rangoPagina = string.Empty;

    [ObservableProperty]
    private bool puedeIrAInicio;

    [ObservableProperty]
    private bool puedeRetroceder;

    [ObservableProperty]
    private bool puedeAvanzar;

    [ObservableProperty]
    private bool puedeIrAlFinal;

    [RelayCommand]
    private void IrAInicio() => this.IrAPagina(1);

    [RelayCommand]
    private void IrAtras() => this.IrAPagina(this.PaginaActual - 1);

    [RelayCommand]
    private void IrAdelante() => this.IrAPagina(this.PaginaActual + 1);

    [RelayCommand]
    private void IrAlFinal() => this.IrAPagina(this.TotalPaginas);

    private void IrAPagina(int pagina)
    {
        int destino = Math.Clamp(pagina, 1, Math.Max(this.TotalPaginas, 1));

        if (destino == this.PaginaActual)
        {
            return;
        }

        this.PaginaActual = destino;

        // Recargar vuelve a filtrar la base y vuelve a recortar: un solo camino para los filtros
        // y para las flechas, sin listas intermedias que se queden desfasadas.
        this.Recargar();
    }

    /// <summary>
    /// Vuelca en <see cref="Solicitudes"/> la página actual de <paramref name="filtradas"/> y
    /// recalcula el rango y los botones. El recorte lo hace <see cref="Paginador"/>; aquí solo
    /// se formatea el rango con el idioma activo. WPF no pagina las vistas de colección, así que
    /// la exportación a CSV no pasa por esta lista y sigue saliendo entera.
    /// </summary>
    private void Paginar(List<Solicitud> filtradas)
    {
        ResultadoPagina<Solicitud> pagina = Paginador.Paginar(filtradas, this.TamanoPagina, this.PaginaActual);

        this.TotalPaginas = pagina.TotalPaginas;
        this.PaginaActual = pagina.PaginaActual;
        this.Solicitudes = new ObservableCollection<Solicitud>(pagina.Elementos);

        this.PuedeIrAInicio = pagina.PuedeIrAInicio;
        this.PuedeRetroceder = pagina.PuedeRetroceder;
        this.PuedeAvanzar = pagina.PuedeAvanzar;
        this.PuedeIrAlFinal = pagina.PuedeIrAlFinal;

        string de = Localizacion.Texto("Paginacion.De");
        this.RangoPagina = pagina.Total == 0
            ? $"0 {de} 0"
            : $"{pagina.Desde + 1}-{pagina.Hasta} {de} {pagina.Total}";
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

    partial void OnSolicitudSeleccionadaChanged(Solicitud? value)
    {
        // Ignoramos el null que manda el DataGrid al recargar la lista o al pulsar "Nueva".
        if (value is not null)
            Edicion = value;
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
        this.PuedeRetrocederPeriodo = this.PuedeRetrocederUnPeriodoMas;

        // El desplazamiento se conserva: si estabas tres meses atrás, al pasar a semanas
        // sigues tres semanas atrás, que es el mismo instante de la historia.
        this.RecargarEmbudoSiVisible();
    }

    partial void OnDesplazamientoPeriodoChanged(int value)
    {
        this.PuedeAvanzarPeriodo = value > 0;
        this.PuedeRetrocederPeriodo = this.PuedeRetrocederUnPeriodoMas;
        this.RecargarEmbudoSiVisible();
    }

    /// <summary>
    /// El rango de la gráfica va de la primera solicitud a hoy. Con la base vacía no hay
    /// suelo conocido, así que se deja ir hacia atrás (solo se ve ceros).
    /// </summary>
    private bool PuedeRetrocederUnPeriodoMas =>
        this.db.Solicitudes.Min(s => (DateTime?)s.FechaSolicitud) is not DateTime primera
        || EmbudoHelper.CalcularPeriodos(this.GranularidadGrafica, this.DesplazamientoPeriodo + 1)[^1]
            >= EmbudoHelper.InicioDePeriodo(primera, this.GranularidadGrafica);

    /// <summary>Título de la gráfica, que nombra la granularidad activa (se repinta al cambiar de idioma).</summary>
    public string TituloGrafica => string.Format(
        Localizacion.CulturaActual,
        Localizacion.Texto("Grafica.Titulo"),
        Localizacion.Texto($"Grafica.Titulo{(int)this.GranularidadGrafica}"));

    [RelayCommand]
    private void PeriodoAnterior()
    {
        if (this.PuedeRetrocederUnPeriodoMas)
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
    /// </summary>
    public void Recargar()
    {
        IQueryable<Solicitud> consulta = this.db.Solicitudes.Include(s => s.Eventos);

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
            lista = lista.Where(s =>
                CsvHelper.NormalizarBusqueda(s.Empresa).Contains(texto) ||
                CsvHelper.NormalizarBusqueda(s.Puesto).Contains(texto) ||
                (s.Tecnologias != null && CsvHelper.NormalizarBusqueda(s.Tecnologias).Contains(texto)) ||
                (s.Ubicacion != null && CsvHelper.NormalizarBusqueda(s.Ubicacion).Contains(texto)) ||
                (s.Portal != null && CsvHelper.NormalizarBusqueda(s.Portal).Contains(texto))).ToList();
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

        this.Paginar(lista);
        this.ActualizarEstadisticas();
    }

    private void ActualizarEstadisticas()
    {
        ResumenEstadisticas resumen = EstadisticasSolicitud.Calcular(this.db.Solicitudes.ToList());

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

    /// <summary>Solicitudes abiertas cuyo siguiente seguimiento ya venció. Lo usa el icono de la bandeja.</summary>
    public List<Solicitud> SeguimientosVencidosAhora()
    {
        DateTime hoy = DateTime.Today;
        return this.db.Solicitudes
            .Where(s => s.ProximoSeguimiento != null && s.ProximoSeguimiento.Value.Date <= hoy)
            .ToList()
            .Where(s => s.EstaAbierta)
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
            string nuevaRuta = AdjuntosHelper.Copiar(origen, adjunto.Etiqueta, this.carpetaAdjuntos);
            this.adjuntosCopiados.Add(nuevaRuta);
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

    /// <summary>Las rutas que apunta hoy la base de datos, no las que el usuario acaba de elegir.</summary>
    private (string? Cv, string? Carta) RutasGuardadas(Solicitud solicitud)
    {
        EntityEntry<Solicitud> entrada = this.db.Entry(solicitud);
        return (entrada.Property(s => s.RutaCv).OriginalValue,
                entrada.Property(s => s.RutaCarta).OriginalValue);
    }

    private void EliminarAdjunto(string? ruta) => AdjuntosHelper.Eliminar(ruta, this.carpetaAdjuntos);

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

        this.EliminarAdjuntosSustituidos(solicitud, (cvAnterior, cartaAnterior));
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

    /// <summary>
    /// Borra los adjuntos que la base ya no apunta tras guardar. Se hace aquí y no antes:
    /// si el guardado falla, la base sigue apuntando a un fichero que sigue existiendo.
    /// </summary>
    private void EliminarAdjuntosSustituidos(Solicitud solicitud, (string? Cv, string? Carta) anteriores)
    {
        if (anteriores.Cv != solicitud.RutaCv)
        {
            this.EliminarAdjunto(anteriores.Cv);
        }

        if (anteriores.Carta != solicitud.RutaCarta)
        {
            this.EliminarAdjunto(anteriores.Carta);
        }

        this.EliminarCopiasDeLaEdicion(solicitud.RutaCv, solicitud.RutaCarta);
    }

    /// <summary>
    /// Borra las copias hechas durante la edición salvo las que se quieren conservar.
    /// Sin argumentos las borra todas: es lo que toca al cancelar o al eliminar la fila.
    /// </summary>
    private void EliminarCopiasDeLaEdicion(string? conservarCv = null, string? conservarCarta = null)
    {
        foreach (string ruta in this.adjuntosCopiados)
        {
            if (ruta != conservarCv && ruta != conservarCarta)
            {
                this.EliminarAdjunto(ruta);
            }
        }

        this.adjuntosCopiados.Clear();
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

        this.EliminarAdjuntosNoGuardados(solicitud);

        // Desenganchamos lo que EF tenía en seguimiento: los cambios pendientes
        // se pierden y la siguiente consulta vuelve a traer los datos de disco.
        this.db.ChangeTracker.Clear();

        this.Edicion = null;
        this.SolicitudSeleccionada = null;
        this.Recargar();
    }

    /// <summary>
    /// Borra las copias hechas durante la edición que se descarta. Los adjuntos que ya
    /// estaban guardados se quedan, porque la base sigue apuntando a ellos.
    /// </summary>
    private void EliminarAdjuntosNoGuardados(Solicitud solicitud)
    {
        (string? cvGuardado, string? cartaGuardada) = solicitud.Id == 0
            ? (null, null)
            : this.RutasGuardadas(solicitud);

        if (solicitud.RutaCv != cvGuardado)
        {
            this.EliminarAdjunto(solicitud.RutaCv);
        }

        if (solicitud.RutaCarta != cartaGuardada)
        {
            this.EliminarAdjunto(solicitud.RutaCarta);
        }

        this.EliminarCopiasDeLaEdicion();
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

        string? cv = this.Edicion.RutaCv;
        string? carta = this.Edicion.RutaCarta;

        if (this.EliminarSolicitud(this.Edicion) is Exception error)
        {
            this.dialogo.Error(
                string.Format(Localizacion.Texto("Mensaje.NoSePudoEliminar"), error.Message),
                Localizacion.Texto(ClaveError));
            return;
        }

        this.EliminarAdjunto(cv);
        this.EliminarAdjunto(carta);
        this.EliminarCopiasDeLaEdicion();

        this.Edicion = null;
        this.SolicitudSeleccionada = null;
        this.Recargar();
        this.RecargarEmbudoSiVisible();
    }

    /// <summary>Borra la candidatura de la base. Devuelve el fallo en vez de lanzarlo para poder avisar.</summary>
    internal Exception? EliminarSolicitud(Solicitud solicitud)
    {
        try
        {
            this.db.Solicitudes.Remove(solicitud);
            this.db.SaveChanges();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
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

        if (this.EscribirCsv(destino) is Exception error)
        {
            this.dialogo.Error(
                string.Format(Localizacion.Texto("Mensaje.NoSePudoExportar"), error.Message),
                Localizacion.Texto(ClaveError));
            return;
        }

        this.dialogo.Informar(
            string.Format(Localizacion.Texto("Csv.ExportadasN"), this.db.Solicitudes.Count()),
            Localizacion.Texto("Titulo.ExportacionCompletada"));
    }

    /// <summary>Escribe el CSV. Devuelve el fallo en vez de lanzarlo para poder avisar.</summary>
    internal Exception? EscribirCsv(string ruta)
    {
        try
        {
            File.WriteAllText(ruta, this.ConstruirCsv(), new UTF8Encoding(true));
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    internal string ConstruirCsv() =>
        CsvExportador.Construir(this.db.Solicitudes
            .OrderByDescending(x => x.FechaSolicitud)
            .ThenByDescending(x => x.Id)
            .ToList());
}
