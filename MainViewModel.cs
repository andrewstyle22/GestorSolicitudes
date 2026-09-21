namespace GestorSolicitudes.ViewModels;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestorSolicitudes.Data;
using GestorSolicitudes.Helpers;
using GestorSolicitudes.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Win32;

public partial class MainViewModel : ObservableObject
{
    // La cultura de los números y fechas se toma del idioma activo; los tests que no pasan
    // por el arranque de la app se apoyan en que por defecto el idioma es el castellano.
    private const string ClaveError = "Error";
    private const string FormatoFecha = "yyyy-MM-dd";

    private readonly AppDbContext db;

    public MainViewModel()
        : this(new AppDbContext())
    {
    }

    // Constructor con base de datos propia, para que los tests usen una base temporal.
    internal MainViewModel(AppDbContext db)
    {
        this.db = db;

        this.CargarTextosLocalizados();

        // Asignación directa al campo para no disparar la recarga dos veces.
        this.estadoFiltroItem = this.EstadosFiltro[0];

        Localizacion.IdiomaCambiado += this.CuandoCambiaIdioma;

        this.Recargar();
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

        this.MotivosRechazo = new List<EnumItem> { new(null, Localizacion.Texto("Filtro.SinEspecificar")) }
            .Concat(EnumHelper.Valores<MotivoRechazo>())
            .ToList();

        this.EstadosFiltro = new List<EnumItem> { new(null, Localizacion.Texto("Filtro.TodosLosEstados")) }
            .Concat(this.Estados)
            .ToList();

        // IdiomasDisponibles no se toca aquí a propósito: los tres nombres son fijos
        // (cada uno en su propia lengua), y reconstruir la lista forzaría al ComboBox
        // del selector a re-sincronizar su selección.
    }

    /// <summary>
    /// Al cambiar de idioma: se refrescan los desplegables, la lista, las estadísticas y la
    /// gráfica para que lo que no se repinta solo (combos, celdas del DataGrid) lo haga.
    /// </summary>
    private void CuandoCambiaIdioma(object? sender, Idioma idioma)
    {
        EstadoSolicitud? filtro = this.EstadoFiltroItem?.Valor as EstadoSolicitud?;

        this.CargarTextosLocalizados();

        // El item seleccionado del filtro pertenecía a la lista anterior: se reencuentra por
        // valor en la nueva (los objetos son distintos aunque representen lo mismo).
        this.EstadoFiltroItem = this.EstadosFiltro.FirstOrDefault(i => Equals(i.Valor, filtro))
            ?? this.EstadosFiltro[0];

        // Los botones de la gráfica usan un convertidor: se notifica para que se re-evalúen.
        this.OnPropertyChanged(nameof(this.VerGrafica));

        if (this.VerGrafica)
        {
            this.ActualizarEmbudo();
        }

        this.Recargar();
        this.RefrescarPanelDetalle();
    }

    [ObservableProperty]
    private ObservableCollection<Solicitud> solicitudes = new();

    // ---------------------------------------------------------------- Filtros
    [ObservableProperty]
    private string textoBusqueda = string.Empty;

    partial void OnTextoBusquedaChanged(string value) => Recargar();

    [ObservableProperty]
    private EnumItem? estadoFiltroItem;

    partial void OnEstadoFiltroItemChanged(EnumItem? value) => Recargar();

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

    /// <summary>Filas del embudo (una por mes) con las cuatro series ya escaladas a porcentaje.</summary>
    [ObservableProperty]
    private IReadOnlyList<EmbudoMes> mesesEmbudo = new List<EmbudoMes>();

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

        this.Solicitudes = new ObservableCollection<Solicitud>(lista);
        this.ActualizarEstadisticas();
    }

    private void ActualizarEstadisticas()
    {
        List<Solicitud> todas = this.db.Solicitudes.ToList();

        this.TotalSolicitudes = todas.Count;
        this.ProcesosAbiertos = todas.Count(s => s.EstaAbierta);
        this.OfertasRecibidas = todas.Count(s => s.Estado is EstadoSolicitud.OfertaRecibida
            or EstadoSolicitud.OfertaAceptada
            or EstadoSolicitud.OfertaRechazada);
        this.EnEntrevista = todas.Count(s => s.Estado is EstadoSolicitud.EntrevistaRrhh
            or EstadoSolicitud.EntrevistaTecnica
            or EstadoSolicitud.EntrevistaFinal
            or EstadoSolicitud.PruebaTecnica);
        this.SeguimientosPendientes = todas.Count(s => s.SeguimientoPendiente);

        int conRespuesta = todas.Count(s => s.HuboRespuesta);
        this.TasaRespuesta = todas.Count == 0
            ? "—"
            : ((double)conRespuesta / todas.Count).ToString("P0", Localizacion.CulturaActual);

        List<int> dias = todas
            .Where(s => s.DiasHastaRespuesta.HasValue)
            .Select(s => s.DiasHastaRespuesta!.Value)
            .ToList();

        this.MediaDiasRespuesta = dias.Count == 0
            ? "—"
            : $"{dias.Average().ToString("0.#", Localizacion.CulturaActual)} {Localizacion.Texto("Metrica.Dias")}";
    }

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
    /// Embudo enviadas → respondidas → entrevistas → ofertas de los últimos 12 meses.
    /// Se apoya en el historial (Eventos) para fechar cada hito con precisión.
    /// </summary>
    private void ActualizarEmbudo()
    {
        var meses = new List<DateTime>();
        var inicioMesActual = DateTime.Today.AddDays(1 - DateTime.Today.Day);
        for (int i = 11; i >= 0; i--)
        {
            meses.Add(inicioMesActual.AddMonths(-i));
        }

        List<Solicitud> todas = this.db.Solicitudes.Include(s => s.Eventos).ToList();

        double[] enviadas = new double[meses.Count];
        double[] respondidas = new double[meses.Count];
        double[] entrevistas = new double[meses.Count];
        double[] ofertas = new double[meses.Count];

        for (int m = 0; m < meses.Count; m++)
        {
            DateTime inicio = meses[m];
            DateTime fin = inicio.AddMonths(1);

            enviadas[m] = todas.Count(s => EnviadaEnMes(s, inicio, fin));
            respondidas[m] = todas.Count(s => s.FechaPrimeraRespuesta >= inicio && s.FechaPrimeraRespuesta < fin);
            entrevistas[m] = todas
                .SelectMany(s => s.Eventos)
                .Count(e => e.Tipo == TipoEvento.Entrevista && e.Fecha >= inicio && e.Fecha < fin);
            ofertas[m] = todas
                .SelectMany(s => s.Eventos)
                .Count(e => e.Tipo == TipoEvento.Oferta && e.Fecha >= inicio && e.Fecha < fin);
        }

        this.MesesEmbudo = meses
            .Select((m, i) => new EmbudoMes(
                m.ToString("MMM yyyy", Localizacion.CulturaActual),
                enviadas[i],
                respondidas[i],
                entrevistas[i],
                ofertas[i]))
            .ToList();
    }

    internal static bool EnviadaEnMes(Solicitud s, DateTime inicio, DateTime fin)
    {
        bool tieneHito = s.Eventos.Any(e =>
            e.Tipo == TipoEvento.SolicitudEnviada && e.Fecha >= inicio && e.Fecha < fin);

        bool sinHito = !s.Eventos.Any(e => e.Tipo == TipoEvento.SolicitudEnviada)
            && s.FechaSolicitud >= inicio && s.FechaSolicitud < fin;

        return tieneHito || sinHito;
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
    [RelayCommand]
    private void AdjuntarCv() => this.AdjuntarAdjunto(
        Localizacion.Texto("Dialogo.SeleccionaCv"), "cv",
        obtenerRuta: e => e.RutaCv,
        asignar: (e, ruta, nombre) =>
        {
            e.RutaCv = ruta;
            e.NombreOriginalCv = nombre;
        });

    [RelayCommand]
    private void AdjuntarCarta() => this.AdjuntarAdjunto(
        Localizacion.Texto("Dialogo.SeleccionaCarta"), "carta",
        obtenerRuta: e => e.RutaCarta,
        asignar: (e, ruta, nombre) =>
        {
            e.RutaCarta = ruta;
            e.NombreOriginalCarta = nombre;
        });

    private void AdjuntarAdjunto(
        string titulo, string etiqueta,
        Func<Solicitud, string?> obtenerRuta,
        Action<Solicitud, string, string> asignar)
    {
        Solicitud? editar = this.Edicion;
        if (editar is null)
        {
            return;
        }

        var dialogo = new OpenFileDialog
        {
            Title = titulo,
            Filter = Localizacion.Texto("Dialogo.FiltroAdjuntos"),
        };

        if (dialogo.ShowDialog() != true)
        {
            return;
        }

        try
        {
            string nuevaRuta = AdjuntosHelper.Copiar(dialogo.FileName, etiqueta);
            AdjuntosHelper.Eliminar(obtenerRuta(editar));
            asignar(editar, nuevaRuta, Path.GetFileName(dialogo.FileName));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                string.Format(Localizacion.Texto("Mensaje.NoSePudoAdjuntar"), ex.Message),
                Localizacion.Texto(ClaveError), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // Las entidades son POCOs sin INotifyPropertyChanged: se reasigna Edicion
        // al mismo objeto para que el panel de detalle repinte el nombre del adjunto.
        this.RefrescarPanelDetalle();
    }

    [RelayCommand]
    private void QuitarCv() => this.QuitarAdjunto(
        e => e.RutaCv, e =>
        {
            e.RutaCv = null;
            e.NombreOriginalCv = null;
        });

    [RelayCommand]
    private void QuitarCarta() => this.QuitarAdjunto(
        e => e.RutaCarta, e =>
        {
            e.RutaCarta = null;
            e.NombreOriginalCarta = null;
        });

    private void QuitarAdjunto(Func<Solicitud, string?> obtenerRuta, Action<Solicitud> limpiar)
    {
        if (this.Edicion is null)
        {
            return;
        }

        AdjuntosHelper.Eliminar(obtenerRuta(this.Edicion));
        limpiar(this.Edicion);
        this.RefrescarPanelDetalle();
    }

    private void RefrescarPanelDetalle()
    {
        Solicitud? actual = this.Edicion;
        this.Edicion = null;
        this.Edicion = actual;
    }

    [RelayCommand]
    private void AbrirCv() => AbrirAdjunto(this.Edicion?.RutaCv);

    [RelayCommand]
    private void AbrirCarta() => AbrirAdjunto(this.Edicion?.RutaCarta);

    private static void AbrirAdjunto(string? ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta))
        {
            MessageBox.Show(
                Localizacion.Texto("Mensaje.SinAdjuntoOYaNoExiste"),
                Localizacion.Texto("Titulo.Adjunto"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(ruta.Trim()) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                string.Format(Localizacion.Texto("Mensaje.NoSePudoAbrirFichero"), ex.Message),
                Localizacion.Texto(ClaveError), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ---------------------------------------------------------------- Importación LinkedIn
    [RelayCommand]
    private void ImportarLinkedIn()
    {
        var dialogo = new OpenFileDialog
        {
            Title = Localizacion.Texto("Dialogo.ImportarCsvLinkedIn"),
            Filter = "CSV (*.csv)|*.csv",
        };

        if (dialogo.ShowDialog() != true)
        {
            return;
        }

        List<List<string>> lineas;
        try
        {
            lineas = CsvHelper.LeerCsv(dialogo.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                string.Format(Localizacion.Texto("Mensaje.NoSePudoLeerFichero"), ex.Message),
                Localizacion.Texto(ClaveError), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (lineas.Count < 2)
        {
            MessageBox.Show(
                Localizacion.Texto("Mensaje.CsvSinFilas"),
                Localizacion.Texto("Titulo.Importar"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var columnas = CsvHelper.IdentificarColumnas(lineas[0]);
        if (!columnas.ContainsKey("empresa") || !columnas.ContainsKey("puesto"))
        {
            MessageBox.Show(
                Localizacion.Texto("Mensaje.CsvColumnas"),
                Localizacion.Texto("Titulo.Importar"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        (int importadas, int duplicadas, int omitidas) = this.ImportarLineas(lineas, columnas);

        MessageBox.Show(
            ResumenImportacion(importadas, duplicadas, omitidas),
            Localizacion.Texto("Titulo.ImportacionCompletada"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    internal (int Importadas, int Duplicadas, int Omitidas) ImportarLineas(
        List<List<string>> lineas, Dictionary<string, int> columnas)
    {
        int maximoIndice = columnas.Values.Max();
        var vistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int importadas = 0, duplicadas = 0, omitidas = 0;

        foreach (List<string> campos in lineas.Skip(1))
        {
            (int Imp, int Dup, int Omi) fila = this.ImportarFila(campos, columnas, maximoIndice, vistas);
            importadas += fila.Imp;
            duplicadas += fila.Dup;
            omitidas += fila.Omi;
        }

        if (importadas > 0)
        {
            this.db.SaveChanges();
        }

        this.Recargar();
        this.RecargarEmbudoSiVisible();

        return (importadas, duplicadas, omitidas);
    }

    internal (int Importadas, int Duplicadas, int Omitidas) ImportarFila(
        List<string> campos, Dictionary<string, int> columnas, int maximoIndice, HashSet<string> vistas)
    {
        if (campos.Count <= maximoIndice)
        {
            return (0, 0, 0);
        }

        string empresa = CsvHelper.ObtenerCampo(campos, columnas, "empresa").Trim();
        string puesto = CsvHelper.ObtenerCampo(campos, columnas, "puesto").Trim();
        if (empresa.Length == 0 || puesto.Length == 0)
        {
            return (0, 0, 1);
        }

        DateTime fecha = CsvHelper.ParsearFecha(CsvHelper.ObtenerCampo(campos, columnas, "fecha"));
        string clave = $"{empresa}|{puesto}|{fecha:yyyy-MM-dd}";

        bool existe = vistas.Contains(clave) || this.db.Solicitudes.Any(s =>
            s.Empresa == empresa && s.Puesto == puesto && s.FechaSolicitud == fecha);

        if (existe)
        {
            return (0, 1, 0);
        }

        vistas.Add(clave);

        string evento = CsvHelper.ObtenerCampo(campos, columnas, "evento");
        string uuid = CsvHelper.ObtenerCampo(campos, columnas, "uuid");
        string ubicacion = CsvHelper.ObtenerCampo(campos, columnas, "ubicacion");

        var solicitud = new Solicitud
        {
            Empresa = empresa,
            Puesto = puesto,
            FechaSolicitud = fecha,
            Ubicacion = ubicacion.Length == 0 ? null : ubicacion,
            Portal = "LinkedIn",
            Estado = CsvHelper.MapearEstadoLinkedIn(CsvHelper.ObtenerCampo(campos, columnas, "estado")),
            EnlaceOferta = uuid.Length > 0 ? $"https://www.linkedin.com/jobs/view/{uuid}" : null,
            Notas = evento.Length > 0 ? string.Format(Localizacion.Texto("Importar.EventoLinkedIn"), evento) : null,
        };

        solicitud.Eventos.Add(new Evento
        {
            Fecha = fecha,
            Tipo = TipoEvento.SolicitudEnviada,
            Descripcion = Localizacion.Texto("Evento.ImportadaLinkedIn"),
        });

        this.db.Solicitudes.Add(solicitud);
        return (1, 0, 0);
    }

    internal static string ResumenImportacion(int importadas, int duplicadas, int omitidas)
    {
        string resumen = string.Format(Localizacion.Texto("Importar.ResumenImportadas"), importadas);
        if (duplicadas > 0)
        {
            resumen += "\n" + string.Format(Localizacion.Texto("Importar.ResumenDuplicadas"), duplicadas);
        }

        if (omitidas > 0)
        {
            resumen += "\n" + string.Format(Localizacion.Texto("Importar.ResumenOmitidas"), omitidas);
        }

        return resumen;
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
                origen.Id, origen.Empresa, origen.FechaSolicitud.ToString("dd/MM/yyyy")),
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
        if (this.Edicion is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(this.Edicion.Empresa) || string.IsNullOrWhiteSpace(this.Edicion.Puesto))
        {
            MessageBox.Show(
                Localizacion.Texto("Mensaje.FaltanDatos"),
                Localizacion.Texto("Titulo.FaltanDatos"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Si el proceso sigue abierto y no se ha fijado cuando tocaria volver a
        // escribir, proponemos +7 dias desde el envio. Sin esto, el aviso de
        // "seguimiento pendiente" nunca llega a dispararse salvo que el usuario
        // recuerde rellenarlo a mano cada vez.
        if (this.Edicion.EstaAbierta && this.Edicion.ProximoSeguimiento is null)
        {
            this.Edicion.ProximoSeguimiento = this.Edicion.FechaSolicitud.AddDays(7);
        }

        bool esNueva = this.Edicion.Id == 0;
        if (esNueva)
        {
            this.db.Solicitudes.Add(this.Edicion);

            // Primer hito del historial, para que la línea temporal no empiece vacía.
            if (this.Edicion.Eventos.Count == 0)
            {
                this.Edicion.Eventos.Add(new Evento
                {
                    Fecha = this.Edicion.FechaSolicitud,
                    Tipo = TipoEvento.SolicitudEnviada,
                    Descripcion = Localizacion.Texto("Evento.CandidaturaEnviada"),
                });
            }
        }

        try
        {
            this.db.SaveChanges();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                string.Format(Localizacion.Texto("Mensaje.NoSePudoGuardar"), ex.Message),
                Localizacion.Texto(ClaveError), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        int id = this.Edicion.Id;
        this.Recargar();
        this.RecargarEmbudoSiVisible();
        this.SolicitudSeleccionada = this.Solicitudes.FirstOrDefault(s => s.Id == id);
    }

    [RelayCommand]
    private void Cancelar()
    {
        if (this.Edicion is null)
        {
            return;
        }

        int id = this.Edicion.Id;

        // Si la candidatura nunca se guardó, los adjuntos copiados se quedan
        // huérfanos: los borramos antes de soltar la edición.
        if (id == 0)
        {
            AdjuntosHelper.Eliminar(this.Edicion.RutaCv);
            AdjuntosHelper.Eliminar(this.Edicion.RutaCarta);
        }

        // Desenganchamos lo que EF tenía en seguimiento: los cambios pendientes
        // se pierden y la siguiente consulta vuelve a traer los datos de disco.
        foreach (EntityEntry entrada in this.db.ChangeTracker.Entries().ToList())
        {
            entrada.State = EntityState.Detached;
        }

        this.Edicion = null;
        this.Recargar();

        if (id > 0)
        {
            this.SolicitudSeleccionada = this.Solicitudes.FirstOrDefault(s => s.Id == id);
        }
    }

    [RelayCommand]
    private void Eliminar()
    {
        if (this.Edicion is null || this.Edicion.Id == 0)
        {
            return;
        }

        var confirmacion = MessageBox.Show(
            string.Format(Localizacion.Texto("Mensaje.ConfirmarEliminar"), this.Edicion.Empresa, this.Edicion.Puesto),
            Localizacion.Texto("Titulo.Confirmar"), MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirmacion != MessageBoxResult.Yes)
        {
            return;
        }

        string? cv = this.Edicion.RutaCv;
        string? carta = this.Edicion.RutaCarta;

        this.db.Solicitudes.Remove(this.Edicion);
        this.db.SaveChanges();

        AdjuntosHelper.Eliminar(cv);
        AdjuntosHelper.Eliminar(carta);

        this.Edicion = null;
        this.Recargar();
        this.RecargarEmbudoSiVisible();
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
            Process.Start(new ProcessStartInfo(url.Trim()) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                string.Format(Localizacion.Texto("Mensaje.NoSePudoAbrirEnlace"), ex.Message),
                Localizacion.Texto(ClaveError), MessageBoxButton.OK, MessageBoxImage.Warning);
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
        var dialogo = new SaveFileDialog
        {
            Title = Localizacion.Texto("Dialogo.ExportarCandidaturas"),
            FileName = $"solicitudes-{DateTime.Today:yyyy-MM-dd}.csv",
            Filter = "CSV (*.csv)|*.csv",
        };

        if (dialogo.ShowDialog() != true)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(
                    ';',
                    Localizacion.Texto("Csv.Empresa"),
                    Localizacion.Texto("Csv.Puesto"),
                    Localizacion.Texto("Csv.Estado"),
                    Localizacion.Texto("Csv.Portal"),
                    Localizacion.Texto("Csv.Ubicacion"),
                    Localizacion.Texto("Csv.Modalidad"),
                    Localizacion.Texto("Csv.FechaSolicitud"),
                    Localizacion.Texto("Csv.PrimeraRespuesta"),
                    Localizacion.Texto("Csv.DiasHastaRespuesta"),
                    Localizacion.Texto("Csv.Entrevista"),
                    Localizacion.Texto("Csv.Cierre"),
                    Localizacion.Texto("Csv.ProximoSeguimiento"),
                    Localizacion.Texto("Csv.SalarioMin"),
                    Localizacion.Texto("Csv.SalarioMax"),
                    Localizacion.Texto("Csv.Pretension"),
                    Localizacion.Texto("Csv.Interes"),
                    Localizacion.Texto("Csv.Tecnologias"),
                    Localizacion.Texto("Csv.Contacto"),
                    Localizacion.Texto("Csv.EmailContacto"),
                    Localizacion.Texto("Csv.RespuestaEmpresa"),
                    Localizacion.Texto("Csv.Notas"),
                    Localizacion.Texto("Csv.Enlace")));
        foreach (Solicitud s in this.db.Solicitudes.OrderByDescending(x => x.FechaSolicitud).ToList())
        {
            sb.AppendLine(string.Join(
            ';',
            CsvHelper.Escapar(s.Empresa),
                CsvHelper.Escapar(s.Puesto),
                CsvHelper.Escapar(EnumHelper.Descripcion(s.Estado)),
                CsvHelper.Escapar(s.Portal),
                CsvHelper.Escapar(s.Ubicacion),
                CsvHelper.Escapar(EnumHelper.Descripcion(s.Modalidad)),
                s.FechaSolicitud.ToString(FormatoFecha),
                s.FechaPrimeraRespuesta?.ToString(FormatoFecha) ?? string.Empty,
                s.DiasHastaRespuesta?.ToString() ?? string.Empty,
                s.FechaEntrevista?.ToString(FormatoFecha) ?? string.Empty,
                s.FechaCierre?.ToString(FormatoFecha) ?? string.Empty,
                s.ProximoSeguimiento?.ToString(FormatoFecha) ?? string.Empty,
                s.SalarioMin?.ToString() ?? string.Empty,
                s.SalarioMax?.ToString() ?? string.Empty,
                s.PretensionSalarial?.ToString() ?? string.Empty,
                s.Interes.ToString(),
                CsvHelper.Escapar(s.Tecnologias),
                CsvHelper.Escapar(s.ContactoNombre),
                CsvHelper.Escapar(s.ContactoEmail),
                CsvHelper.Escapar(s.RespuestaEmpresa),
                CsvHelper.Escapar(s.Notas),
                CsvHelper.Escapar(s.EnlaceOferta)));
        }

        // UTF-8 con BOM para que Excel en español no destroce los acentos.
        File.WriteAllText(dialogo.FileName, sb.ToString(), new UTF8Encoding(true));

        MessageBox.Show(
            string.Format(Localizacion.Texto("Csv.ExportadasN"), this.TotalSolicitudes),
            Localizacion.Texto("Titulo.ExportacionCompletada"), MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

/// <summary>
/// Una fila del embudo: un mes con las cuatro series (enviadas, respondidas, entrevistas,
/// ofertas) y la altura de cada barra, ya escalada a porcentaje del valor máximo del mes.
/// </summary>
public record EmbudoMes(string Etiqueta, double Enviadas, double Respondidas, double Entrevistas, double Ofertas)
{
    public double AlturaEnviadas => this.Altura(this.Enviadas);

    public double AlturaRespondidas => this.Altura(this.Respondidas);

    public double AlturaEntrevistas => this.Altura(this.Entrevistas);

    public double AlturaOfertas => this.Altura(this.Ofertas);

    private double Maximo => Math.Max(Math.Max(this.Enviadas, this.Respondidas), Math.Max(this.Entrevistas, this.Ofertas));

    private double Altura(double valor) => this.Maximo == 0 ? 0 : valor / this.Maximo * 100.0;
}
