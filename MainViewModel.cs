using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestorSolicitudes.Data;
using GestorSolicitudes.Helpers;
using GestorSolicitudes.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Win32;
using SkiaSharp;

namespace GestorSolicitudes.ViewModels;

public partial class MainViewModel : ObservableObject
{
    // Una sola instancia viva durante toda la sesión: es una app monousuario,
    // así que aprovechamos el change tracking de EF Core para editar en sitio.
    private readonly AppDbContext _db = new();

    public MainViewModel()
    {
        Estados = EnumHelper.Valores<EstadoSolicitud>();
        Modalidades = EnumHelper.Valores<Modalidad>();
        TiposEvento = EnumHelper.Valores<TipoEvento>();

        EstadosFiltro = new List<EnumItem> { new(null, "Todos los estados") }
            .Concat(Estados)
            .ToList();

        // Asignación directa al campo para no disparar la recarga dos veces.
        estadoFiltroItem = EstadosFiltro[0];

        Recargar();
    }

    // ---------------------------------------------------------------- Listas

    public IReadOnlyList<EnumItem> Estados { get; }
    public IReadOnlyList<EnumItem> EstadosFiltro { get; }
    public IReadOnlyList<EnumItem> Modalidades { get; }
    public IReadOnlyList<EnumItem> TiposEvento { get; }

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

    [ObservableProperty] private int totalSolicitudes;
    [ObservableProperty] private int procesosAbiertos;
    [ObservableProperty] private int enEntrevista;
    [ObservableProperty] private int ofertasRecibidas;
    [ObservableProperty] private int seguimientosPendientes;
    [ObservableProperty] private string tasaRespuesta = "—";
    [ObservableProperty] private string mediaDiasRespuesta = "—";

    // ---------------------------------------------------------------- Gráfica de embudo

    [ObservableProperty] private bool verGrafica;
    [ObservableProperty] private ISeries[] serieEmbudo = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] ejesXEmbudo = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] ejesYEmbudo = Array.Empty<Axis>();

    // ---------------------------------------------------------------- Carga

    /// <summary>
    /// Recarga la lista, los filtros y las estadísticas. Público: la ventana lo
    /// usa para refrescar los contadores tras avisar de seguimientos vencidos.
    /// </summary>
    public void Recargar()
    {
        IQueryable<Solicitud> consulta = _db.Solicitudes.Include(s => s.Eventos);

        if (!string.IsNullOrWhiteSpace(TextoBusqueda))
        {
            string texto = TextoBusqueda.Trim();
            consulta = consulta.Where(s =>
                s.Empresa.Contains(texto) ||
                s.Puesto.Contains(texto) ||
                (s.Tecnologias != null && s.Tecnologias.Contains(texto)) ||
                (s.Ubicacion != null && s.Ubicacion.Contains(texto)) ||
                (s.Portal != null && s.Portal.Contains(texto)));
        }

        if (EstadoFiltroItem?.Valor is EstadoSolicitud estado)
            consulta = consulta.Where(s => s.Estado == estado);

        List<Solicitud> lista = consulta
            .OrderByDescending(s => s.FechaSolicitud)
            .ThenByDescending(s => s.Id)
            .ToList();

        // EstaAbierta y SeguimientoPendiente son [NotMapped]: se filtran en memoria.
        if (SoloAbiertas)
            lista = lista.Where(s => s.EstaAbierta).ToList();

        if (SoloConSeguimientoPendiente)
            lista = lista.Where(s => s.SeguimientoPendiente).ToList();

        Solicitudes = new ObservableCollection<Solicitud>(lista);
        ActualizarEstadisticas();
    }

    private void ActualizarEstadisticas()
    {
        List<Solicitud> todas = _db.Solicitudes.ToList();

        TotalSolicitudes = todas.Count;
        ProcesosAbiertos = todas.Count(s => s.EstaAbierta);
        OfertasRecibidas = todas.Count(s => s.Estado is EstadoSolicitud.OfertaRecibida
            or EstadoSolicitud.OfertaAceptada
            or EstadoSolicitud.OfertaRechazada);
        EnEntrevista = todas.Count(s => s.Estado is EstadoSolicitud.EntrevistaRrhh
            or EstadoSolicitud.EntrevistaTecnica
            or EstadoSolicitud.EntrevistaFinal
            or EstadoSolicitud.PruebaTecnica);
        SeguimientosPendientes = todas.Count(s => s.SeguimientoPendiente);

        int conRespuesta = todas.Count(s => s.HuboRespuesta);
        TasaRespuesta = todas.Count == 0
            ? "—"
            : $"{(double)conRespuesta / todas.Count:P0}";

        List<int> dias = todas
            .Where(s => s.DiasHastaRespuesta.HasValue)
            .Select(s => s.DiasHastaRespuesta!.Value)
            .ToList();

        MediaDiasRespuesta = dias.Count == 0
            ? "—"
            : $"{dias.Average():0.#} días";
    }

    // ---------------------------------------------------------------- Comandos

    [RelayCommand]
    private void ConfigurarGrafica()
    {
        VerGrafica = !VerGrafica;
        if (VerGrafica)
            ActualizarEmbudo();
    }

    /// <summary>Si el panel de la gráfica está abierto, la refresca con los datos actuales.</summary>
    private void RecargarEmbudoSiVisible()
    {
        if (VerGrafica)
            ActualizarEmbudo();
    }

    /// <summary>
    /// Embudo enviadas → respondidas → entrevistas → ofertas de los últimos 12 meses.
    /// Se apoya en el historial (Eventos) para fechar cada hito con precisión.
    /// </summary>
    private void ActualizarEmbudo()
    {
        var meses = new List<DateTime>();
        var inicioMesActual = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        for (int i = 11; i >= 0; i--)
            meses.Add(inicioMesActual.AddMonths(-i));

        List<Solicitud> todas = _db.Solicitudes.Include(s => s.Eventos).ToList();

        double[] enviadas = Repetir(meses.Count, 0.0);
        double[] respondidas = Repetir(meses.Count, 0.0);
        double[] entrevistas = Repetir(meses.Count, 0.0);
        double[] ofertas = Repetir(meses.Count, 0.0);

        for (int m = 0; m < meses.Count; m++)
        {
            DateTime inicio = meses[m];
            DateTime fin = inicio.AddMonths(1);

            foreach (Solicitud s in todas)
            {
                bool tieneHitoEnvio = s.Eventos.Any(e =>
                    e.Tipo == TipoEvento.SolicitudEnviada && e.Fecha >= inicio && e.Fecha < fin);

                bool enviadaSinHito = !s.Eventos.Any(e => e.Tipo == TipoEvento.SolicitudEnviada)
                    && s.FechaSolicitud >= inicio && s.FechaSolicitud < fin;

                if (tieneHitoEnvio || enviadaSinHito) enviadas[m]++;

                if (s.FechaPrimeraRespuesta >= inicio && s.FechaPrimeraRespuesta < fin)
                    respondidas[m]++;
            }

            entrevistas[m] = todas
                .SelectMany(s => s.Eventos)
                .Count(e => e.Tipo == TipoEvento.Entrevista && e.Fecha >= inicio && e.Fecha < fin);

            ofertas[m] = todas
                .SelectMany(s => s.Eventos)
                .Count(e => e.Tipo == TipoEvento.Oferta && e.Fecha >= inicio && e.Fecha < fin);
        }

        string[] etiquetas = meses
            .Select(m => m.ToString("MMM yyyy", CultureInfo.CurrentCulture))
            .ToArray();

        SerieEmbudo = new ISeries[]
        {
            new ColumnSeries<double> { Name = "Enviadas",     Values = enviadas,     Fill = new SolidColorPaint(SKColor.Parse("#94A3B8")) },
            new ColumnSeries<double> { Name = "Respondidas",  Values = respondidas,  Fill = new SolidColorPaint(SKColor.Parse("#2563EB")) },
            new ColumnSeries<double> { Name = "Entrevistas",  Values = entrevistas,  Fill = new SolidColorPaint(SKColor.Parse("#7C3AED")) },
            new ColumnSeries<double> { Name = "Ofertas",      Values = ofertas,      Fill = new SolidColorPaint(SKColor.Parse("#059669")) }
        };

        EjesXEmbudo = new[] { new Axis { Labels = etiquetas, LabelsRotation = 45, TextSize = 11 } };
        EjesYEmbudo = new[] { new Axis { MinLimit = 0, TextSize = 11 } };
    }

    private static T[] Repetir<T>(int cantidad, T valor)
    {
        var resultado = new T[cantidad];
        Array.Fill(resultado, valor);
        return resultado;
    }

    /// <summary>Solicitudes abiertas cuyo siguiente seguimiento ya venció. Lo usa el icono de la bandeja.</summary>
    public List<Solicitud> SeguimientosVencidosAhora()
    {
        DateTime hoy = DateTime.Today;
        return _db.Solicitudes
            .Where(s => s.ProximoSeguimiento != null && s.ProximoSeguimiento.Value.Date <= hoy)
            .ToList()
            .Where(s => s.EstaAbierta)
            .ToList();
    }

    // ---------------------------------------------------------------- Interés (estrellas)

    /// <summary>Fija el nivel de interés (1 a 5) al pulsar una estrella del panel de detalle.</summary>
    [RelayCommand]
    private void EstablecerInteres(string parametro)
    {
        if (Edicion is null || !int.TryParse(parametro, out int valor)) return;

        Edicion.Interes = valor;
        RefrescarPanelDetalle();
    }

    // ---------------------------------------------------------------- Adjuntos

    [RelayCommand]
    private void AdjuntarCv() => AdjuntarAdjunto(
        "Selecciona el CV que enviaste", "cv",
        obtenerRuta: e => e.RutaCv,
        asignar: (e, ruta, nombre) => { e.RutaCv = ruta; e.NombreOriginalCv = nombre; });

    [RelayCommand]
    private void AdjuntarCarta() => AdjuntarAdjunto(
        "Selecciona la carta de presentación", "carta",
        obtenerRuta: e => e.RutaCarta,
        asignar: (e, ruta, nombre) => { e.RutaCarta = ruta; e.NombreOriginalCarta = nombre; });

    private void AdjuntarAdjunto(
        string titulo, string etiqueta,
        Func<Solicitud, string?> obtenerRuta,
        Action<Solicitud, string, string> asignar)
    {
        Solicitud? editar = Edicion;
        if (editar is null) return;

        var dialogo = new OpenFileDialog
        {
            Title = titulo,
            Filter = "Documentos (*.pdf;*.docx;*.doc)|*.pdf;*.docx;*.doc|Currículos (*.pdf;*.docx;*.doc)|*.pdf;*.docx;*.doc|Todos los archivos (*.*)|*.*"
        };

        if (dialogo.ShowDialog() != true) return;

        try
        {
            string nuevaRuta = AdjuntosHelper.Copiar(dialogo.FileName, etiqueta);
            AdjuntosHelper.Eliminar(obtenerRuta(editar));
            asignar(editar, nuevaRuta, Path.GetFileName(dialogo.FileName));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo adjuntar el fichero:\n\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // Las entidades son POCOs sin INotifyPropertyChanged: se reasigna Edicion
        // al mismo objeto para que el panel de detalle repinte el nombre del adjunto.
        RefrescarPanelDetalle();
    }

    [RelayCommand]
    private void QuitarCv() => QuitarAdjunto(
        e => e.RutaCv, e => { e.RutaCv = null; e.NombreOriginalCv = null; });

    [RelayCommand]
    private void QuitarCarta() => QuitarAdjunto(
        e => e.RutaCarta, e => { e.RutaCarta = null; e.NombreOriginalCarta = null; });

    private void QuitarAdjunto(Func<Solicitud, string?> obtenerRuta, Action<Solicitud> limpiar)
    {
        if (Edicion is null) return;

        AdjuntosHelper.Eliminar(obtenerRuta(Edicion));
        limpiar(Edicion);
        RefrescarPanelDetalle();
    }

    private void RefrescarPanelDetalle()
    {
        Solicitud? actual = Edicion;
        Edicion = null;
        Edicion = actual;
    }

    [RelayCommand]
    private void AbrirCv() => AbrirAdjunto(Edicion?.RutaCv);

    [RelayCommand]
    private void AbrirCarta() => AbrirAdjunto(Edicion?.RutaCarta);

    private static void AbrirAdjunto(string? ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta))
        {
            MessageBox.Show("No hay un fichero adjunto, o ya no existe en disco.",
                "Adjunto", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(ruta.Trim()) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo abrir el fichero: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ---------------------------------------------------------------- Importación LinkedIn

    [RelayCommand]
    private void ImportarLinkedIn()
    {
        var dialogo = new OpenFileDialog
        {
            Title = "Importar el CSV de 'Mis candidaturas' de LinkedIn",
            Filter = "CSV (*.csv)|*.csv"
        };

        if (dialogo.ShowDialog() != true) return;

        List<List<string>> lineas;
        try
        {
            lineas = LeerCsv(dialogo.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo leer el fichero:\n\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (lineas.Count < 2)
        {
            MessageBox.Show("El fichero parece no tener filas de datos.",
                "Importar", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var columnas = IdentificarColumnas(lineas[0]);
        if (!columnas.ContainsKey("empresa") || !columnas.ContainsKey("puesto"))
        {
            MessageBox.Show(
                "No se reconocen las columnas de empresa o puesto en la cabecera.\n\n" +
                "Se espera el CSV que exporta LinkedIn en Ajustes → Privacidad de datos → " +
                "'Obtener una copia de tus datos' (fichero Jobs).",
                "Importar", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int maximoIndice = columnas.Values.Max();
        var vistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int importadas = 0, duplicadas = 0, omitidas = 0;

        foreach (List<string> campos in lineas.Skip(1))
        {
            if (campos.Count <= maximoIndice) continue;

            string empresa = ObtenerCampo(campos, columnas, "empresa").Trim();
            string puesto = ObtenerCampo(campos, columnas, "puesto").Trim();
            if (empresa.Length == 0 || puesto.Length == 0)
            {
                omitidas++;
                continue;
            }

            DateTime fecha = ParsearFecha(ObtenerCampo(campos, columnas, "fecha"));
            string clave = $"{empresa}|{puesto}|{fecha:yyyy-MM-dd}";

            bool existe = vistas.Contains(clave) || _db.Solicitudes.Any(s =>
                s.Empresa == empresa && s.Puesto == puesto && s.FechaSolicitud == fecha);

            if (existe)
            {
                duplicadas++;
                continue;
            }

            vistas.Add(clave);

            string evento = ObtenerCampo(campos, columnas, "evento");
            string uuid = ObtenerCampo(campos, columnas, "uuid");

            var solicitud = new Solicitud
            {
                Empresa = empresa,
                Puesto = puesto,
                FechaSolicitud = fecha,
                Ubicacion = Nulo(ObtenerCampo(campos, columnas, "ubicacion")),
                Portal = "LinkedIn",
                Estado = MapearEstadoLinkedIn(ObtenerCampo(campos, columnas, "estado")),
                EnlaceOferta = uuid.Length > 0 ? $"https://www.linkedin.com/jobs/view/{uuid}" : null,
                Notas = evento.Length > 0 ? $"Evento LinkedIn: {evento}" : null
            };

            solicitud.Eventos.Add(new Evento
            {
                Fecha = fecha,
                Tipo = TipoEvento.SolicitudEnviada,
                Descripcion = "Importada desde LinkedIn"
            });

            _db.Solicitudes.Add(solicitud);
            importadas++;
        }

        if (importadas > 0)
            _db.SaveChanges();

        Recargar();
        RecargarEmbudoSiVisible();

        string resumen = $"Se importaron {importadas} candidaturas desde LinkedIn.";
        if (duplicadas > 0) resumen += $"\nSe omitieron {duplicadas} ya existentes.";
        if (omitidas > 0) resumen += $"\nSe saltaron {omitidas} filas sin empresa o puesto.";

        MessageBox.Show(resumen,
            "Importación completada", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static EstadoSolicitud MapearEstadoLinkedIn(string estado)
    {
        string s = estado.Trim().ToLowerInvariant();

        if (s.Contains("applied") || s.Contains("sent") || s.Contains("don't know")) return EstadoSolicitud.Enviada;
        if (s.Contains("progress")) return EstadoSolicitud.EnRevision;
        if (s.Contains("interview")) return EstadoSolicitud.EntrevistaRrhh;
        if (s.Contains("offer")) return EstadoSolicitud.OfertaRecibida;
        if (s.Contains("hired") || s.Contains("accepted")) return EstadoSolicitud.OfertaAceptada;
        if (s.Contains("reject") || s.Contains("not selected") || s.Contains("not moving")) return EstadoSolicitud.Rechazada;
        if (s.Contains("withdrawn") || s.Contains("withdrew") || s.Contains("archived")) return EstadoSolicitud.Retirada;

        return EstadoSolicitud.Enviada;
    }

    private static DateTime ParsearFecha(string valor)
    {
        var formatos = new[]
        {
            "yyyy-MM-dd", "dd/MM/yyyy", "M/d/yyyy", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss"
        };

        if (DateTime.TryParseExact(valor, formatos, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateTime exacta))
            return exacta;

        if (DateTime.TryParse(valor, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime local))
            return local;

        if (DateTime.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime invariante))
            return invariante;

        return DateTime.Today;
    }

    /// <summary>Localiza cada columna útil del CSV de LinkedIn por el nombre de la cabecera.</summary>
    private static Dictionary<string, int> IdentificarColumnas(List<string> cabecera)
    {
        var resultado = new Dictionary<string, int>();

        for (int i = 0; i < cabecera.Count; i++)
        {
            string col = NormalizarCabecera(cabecera[i]);
            if (col.Length == 0) continue;

            if (!resultado.ContainsKey("empresa") && col.Contains("company")) resultado["empresa"] = i;
            if (!resultado.ContainsKey("puesto") && (col.Contains("title") || col == "puesto")) resultado["puesto"] = i;
            if (!resultado.ContainsKey("fecha") && (col.Contains("application") && col.Contains("date") || col == "fecha")) resultado["fecha"] = i;
            if (!resultado.ContainsKey("estado") && (col.Contains("status") || col.Contains("estado"))) resultado["estado"] = i;
            if (!resultado.ContainsKey("ubicacion") && (col.Contains("location") || col.Contains("ubicacion"))) resultado["ubicacion"] = i;
            if (!resultado.ContainsKey("evento") && (col == "event" || col.Contains("evento"))) resultado["evento"] = i;
            if (!resultado.ContainsKey("uuid") && col.Contains("uuid")) resultado["uuid"] = i;
        }

        return resultado;
    }

    /// <summary>
    /// Deja la cabecera en minúsculas y solo con letras/dígitos ASCII, quitando también
    /// las tildes: así "Ubicación" y "Ubicacion" identifican la misma columna.
    /// </summary>
    private static string NormalizarCabecera(string valor)
    {
        string descompuesto = valor.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);

        foreach (char c in descompuesto)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(c);
            if (categoria == UnicodeCategory.NonSpacingMark) continue;

            char minuscula = char.ToLowerInvariant(c);
            if (char.IsLetterOrDigit(minuscula)) sb.Append(minuscula);
        }

        return sb.ToString();
    }

    private static string ObtenerCampo(List<string> campos, Dictionary<string, int> columnas, string nombre) =>
        columnas.TryGetValue(nombre, out int indice) && indice >= 0 && indice < campos.Count
            ? campos[indice]
            : string.Empty;

    private static string? Nulo(string valor) => valor.Length == 0 ? null : valor;

    /// <summary>Lee un CSV respetando comillas y detecta si el separador es ';' o ','.</summary>
    private static List<List<string>> LeerCsv(string ruta)
    {
        var lineas = new List<List<string>>();
        char delimitador = ',';

        using var lector = new StreamReader(ruta, Encoding.UTF8, true);

        string? linea;
        bool primera = true;
        while ((linea = lector.ReadLine()) is not null)
        {
            if (primera)
            {
                delimitador = DelimitadorDe(linea);
                primera = false;
            }

            lineas.Add(DividirLinea(linea, delimitador));
        }

        return lineas;
    }

    private static char DelimitadorDe(string linea)
    {
        int puntoYComa = 0, coma = 0;
        bool dentroDeComillas = false;

        foreach (char c in linea)
        {
            if (c == '"') dentroDeComillas = !dentroDeComillas;
            else if (!dentroDeComillas && c == ';') puntoYComa++;
            else if (!dentroDeComillas && c == ',') coma++;
        }

        return puntoYComa > coma ? ';' : ',';
    }

    private static List<string> DividirLinea(string linea, char delimitador)
    {
        var campos = new List<string>();
        var actual = new StringBuilder();
        bool dentroDeComillas = false;

        for (int i = 0; i < linea.Length; i++)
        {
            char c = linea[i];

            if (c == '"')
            {
                if (dentroDeComillas && i + 1 < linea.Length && linea[i + 1] == '"')
                {
                    // Comillas dobles escapadas dentro de un campo ("" -> ")
                    actual.Append('"');
                    i++;
                }
                else
                {
                    dentroDeComillas = !dentroDeComillas;
                }
            }
            else if (c == delimitador && !dentroDeComillas)
            {
                campos.Add(actual.ToString().Trim());
                actual.Clear();
            }
            else
            {
                actual.Append(c);
            }
        }

        campos.Add(actual.ToString().Trim());
        return campos;
    }

    [RelayCommand]
    private void Nueva()
    {
        SolicitudSeleccionada = null;
        Edicion = new Solicitud
        {
            FechaSolicitud = DateTime.Today,
            Estado = EstadoSolicitud.Enviada
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
        if (Edicion is null || Edicion.Id == 0) return;

        Solicitud origen = Edicion;
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
            Notas = $"Duplicada de la candidatura #{origen.Id} ({origen.Empresa}, {origen.FechaSolicitud:dd/MM/yyyy})."
        };

        SolicitudSeleccionada = null;
        Edicion = copia;
    }

    [RelayCommand]
    private void Guardar()
    {
        if (Edicion is null) return;

        if (string.IsNullOrWhiteSpace(Edicion.Empresa) || string.IsNullOrWhiteSpace(Edicion.Puesto))
        {
            MessageBox.Show("La empresa y el puesto son obligatorios.",
                "Faltan datos", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Si el proceso sigue abierto y no se ha fijado cuando tocaria volver a
        // escribir, proponemos +7 dias desde el envio. Sin esto, el aviso de
        // "seguimiento pendiente" nunca llega a dispararse salvo que el usuario
        // recuerde rellenarlo a mano cada vez.
        if (Edicion.EstaAbierta && Edicion.ProximoSeguimiento is null)
            Edicion.ProximoSeguimiento = Edicion.FechaSolicitud.AddDays(7);

        bool esNueva = Edicion.Id == 0;
        if (esNueva)
        {
            _db.Solicitudes.Add(Edicion);

            // Primer hito del historial, para que la línea temporal no empiece vacía.
            if (Edicion.Eventos.Count == 0)
            {
                Edicion.Eventos.Add(new Evento
                {
                    Fecha = Edicion.FechaSolicitud,
                    Tipo = TipoEvento.SolicitudEnviada,
                    Descripcion = "Candidatura enviada"
                });
            }
        }

        try
        {
            _db.SaveChanges();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo guardar: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        int id = Edicion.Id;
        Recargar();
        RecargarEmbudoSiVisible();
        SolicitudSeleccionada = Solicitudes.FirstOrDefault(s => s.Id == id);
    }

    [RelayCommand]
    private void Cancelar()
    {
        if (Edicion is null) return;

        int id = Edicion.Id;

        // Si la candidatura nunca se guardó, los adjuntos copiados se quedan
        // huérfanos: los borramos antes de soltar la edición.
        if (id == 0)
        {
            AdjuntosHelper.Eliminar(Edicion.RutaCv);
            AdjuntosHelper.Eliminar(Edicion.RutaCarta);
        }

        // Desenganchamos todo lo que EF tenía en seguimiento: los cambios pendientes
        // se pierden y la siguiente consulta vuelve a traer los datos de disco.
        foreach (EntityEntry entrada in _db.ChangeTracker.Entries().ToList())
            entrada.State = EntityState.Detached;

        Edicion = null;
        Recargar();

        if (id > 0)
            SolicitudSeleccionada = Solicitudes.FirstOrDefault(s => s.Id == id);
    }

    [RelayCommand]
    private void Eliminar()
    {
        if (Edicion is null || Edicion.Id == 0) return;

        var confirmacion = MessageBox.Show(
            $"¿Eliminar la candidatura de {Edicion.Empresa} ({Edicion.Puesto})?",
            "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirmacion != MessageBoxResult.Yes) return;

        string? cv = Edicion.RutaCv;
        string? carta = Edicion.RutaCarta;

        _db.Solicitudes.Remove(Edicion);
        _db.SaveChanges();

        AdjuntosHelper.Eliminar(cv);
        AdjuntosHelper.Eliminar(carta);

        Edicion = null;
        Recargar();
        RecargarEmbudoSiVisible();
    }

    [RelayCommand]
    private void AbrirEnlace()
    {
        string? url = Edicion?.EnlaceOferta;
        if (string.IsNullOrWhiteSpace(url)) return;

        try
        {
            Process.Start(new ProcessStartInfo(url.Trim()) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo abrir el enlace: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void AnadirEvento()
    {
        Edicion?.Eventos.Add(new Evento
        {
            Fecha = DateTime.Today,
            Tipo = TipoEvento.Nota,
            Descripcion = string.Empty
        });
    }

    [RelayCommand]
    private void EliminarEvento(Evento? evento)
    {
        if (Edicion is null || evento is null) return;

        Edicion.Eventos.Remove(evento);
        if (evento.Id != 0)
            _db.Eventos.Remove(evento);
    }

    [RelayCommand]
    private void ExportarCsv()
    {
        var dialogo = new SaveFileDialog
        {
            Title = "Exportar candidaturas",
            FileName = $"solicitudes-{DateTime.Today:yyyy-MM-dd}.csv",
            Filter = "CSV (*.csv)|*.csv"
        };

        if (dialogo.ShowDialog() != true) return;

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(';', new[]
        {
            "Empresa", "Puesto", "Estado", "Portal", "Ubicacion", "Modalidad",
            "Fecha solicitud", "Primera respuesta", "Dias hasta respuesta",
            "Entrevista", "Cierre", "Proximo seguimiento",
            "Salario min", "Salario max", "Pretension", "Interes",
            "Tecnologias", "Contacto", "Email contacto", "Respuesta empresa", "Notas", "Enlace"
        }));

        foreach (Solicitud s in _db.Solicitudes.OrderByDescending(x => x.FechaSolicitud).ToList())
        {
            sb.AppendLine(string.Join(';', new[]
            {
                Escapar(s.Empresa),
                Escapar(s.Puesto),
                Escapar(EnumHelper.Descripcion(s.Estado)),
                Escapar(s.Portal),
                Escapar(s.Ubicacion),
                Escapar(EnumHelper.Descripcion(s.Modalidad)),
                s.FechaSolicitud.ToString("yyyy-MM-dd"),
                s.FechaPrimeraRespuesta?.ToString("yyyy-MM-dd") ?? string.Empty,
                s.DiasHastaRespuesta?.ToString() ?? string.Empty,
                s.FechaEntrevista?.ToString("yyyy-MM-dd") ?? string.Empty,
                s.FechaCierre?.ToString("yyyy-MM-dd") ?? string.Empty,
                s.ProximoSeguimiento?.ToString("yyyy-MM-dd") ?? string.Empty,
                s.SalarioMin?.ToString() ?? string.Empty,
                s.SalarioMax?.ToString() ?? string.Empty,
                s.PretensionSalarial?.ToString() ?? string.Empty,
                s.Interes.ToString(),
                Escapar(s.Tecnologias),
                Escapar(s.ContactoNombre),
                Escapar(s.ContactoEmail),
                Escapar(s.RespuestaEmpresa),
                Escapar(s.Notas),
                Escapar(s.EnlaceOferta)
            }));
        }

        // UTF-8 con BOM para que Excel en español no destroce los acentos.
        File.WriteAllText(dialogo.FileName, sb.ToString(), new UTF8Encoding(true));

        MessageBox.Show($"Exportadas {TotalSolicitudes} candidaturas.",
            "Exportación completada", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string Escapar(string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return string.Empty;
        string limpio = valor.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ");
        return $"\"{limpio}\"";
    }
}
