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
        NivelesInteres = new[] { 1, 2, 3, 4, 5 };

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
    public IReadOnlyList<int> NivelesInteres { get; }

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

    // ---------------------------------------------------------------- Carga

    private void Recargar()
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
    private void Nueva()
    {
        SolicitudSeleccionada = null;
        Edicion = new Solicitud
        {
            FechaSolicitud = DateTime.Today,
            Estado = EstadoSolicitud.Enviada
        };
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
        SolicitudSeleccionada = Solicitudes.FirstOrDefault(s => s.Id == id);
    }

    [RelayCommand]
    private void Cancelar()
    {
        if (Edicion is null) return;

        int id = Edicion.Id;

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

        _db.Solicitudes.Remove(Edicion);
        _db.SaveChanges();
        Edicion = null;
        Recargar();
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
