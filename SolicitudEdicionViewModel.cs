namespace GestorSolicitudes.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using GestorSolicitudes.Models;

/// <summary>
/// Búfer de edición de una solicitud: la interfaz escribe aquí y el modelo solo se toca al guardar.
/// </summary>
public sealed partial class SolicitudEdicionViewModel : ObservableValidator
{
    [ObservableProperty]
    [CustomValidation(typeof(SolicitudEdicionViewModel), nameof(ValidarObligatorio))]
    [MaxLength(Solicitud.LongitudMaximaIdentificacion)]
    private string empresa = string.Empty;

    [ObservableProperty]
    [CustomValidation(typeof(SolicitudEdicionViewModel), nameof(ValidarObligatorio))]
    [MaxLength(Solicitud.LongitudMaximaIdentificacion)]
    private string puesto = string.Empty;

    [ObservableProperty]
    [MaxLength(Solicitud.LongitudMaximaEnlace)]
    private string? enlaceOferta;

    [ObservableProperty]
    [MaxLength(Solicitud.LongitudMaximaPortal)]
    private string? portal;

    [ObservableProperty]
    private Origen origen = Origen.AplicacionDirecta;

    [ObservableProperty]
    [MaxLength(Solicitud.LongitudMaximaUbicacion)]
    private string? ubicacion;

    [ObservableProperty]
    private Modalidad modalidad = Modalidad.SinIndicar;

    [ObservableProperty]
    [MaxLength(Solicitud.LongitudMaximaTecnologias)]
    private string? tecnologias;

    [ObservableProperty]
    [MaxLength(Solicitud.LongitudMaximaRequisitos)]
    private string? requisitos;

    [ObservableProperty]
    private int? salarioMin;

    [ObservableProperty]
    [CustomValidation(typeof(SolicitudEdicionViewModel), nameof(ValidarRangoSalarial))]
    private int? salarioMax;

    [ObservableProperty]
    private int? pretensionSalarial;

    [ObservableProperty]
    private int interes = 3;

    [ObservableProperty]
    private EstadoSolicitud estado = EstadoSolicitud.Enviada;

    [ObservableProperty]
    private MotivoRechazo? motivoRechazo;

    [ObservableProperty]
    private DateTime fechaSolicitud = DateTime.Today;

    [ObservableProperty]
    private DateTime? fechaPrimeraRespuesta;

    [ObservableProperty]
    private DateTime? fechaEntrevista;

    [ObservableProperty]
    private DateTime? proximoSeguimiento;

    [ObservableProperty]
    private DateTime? fechaCierre;

    [ObservableProperty]
    private string? respuestaEmpresa;

    [ObservableProperty]
    [MaxLength(Solicitud.LongitudMaximaContactoNombre)]
    private string? contactoNombre;

    [ObservableProperty]
    [MaxLength(Solicitud.LongitudMaximaContactoEmail)]
    private string? contactoEmail;

    [ObservableProperty]
    [MaxLength(Solicitud.LongitudMaximaContactoTelefono)]
    private string? contactoTelefono;

    [ObservableProperty]
    private string? notas;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdjuntoCv))]
    private string? rutaCv;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdjuntoCv))]
    private string? nombreOriginalCv;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdjuntoCarta))]
    private string? rutaCarta;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdjuntoCarta))]
    private string? nombreOriginalCarta;

    /// <summary>CV de la edición, con el texto de "sin adjuntar" si no hay fichero.</summary>
    public Adjunto AdjuntoCv => new(
        this.RutaCv,
        this.NombreOriginalCv ?? Path.GetFileName(this.RutaCv) ?? Localizacion.Texto("Adjuntos.CurriculumSinAdjuntar"));

    /// <summary>Carta de la edición, con el texto de "sin adjuntar" si no hay fichero.</summary>
    public Adjunto AdjuntoCarta => new(
        this.RutaCarta,
        this.NombreOriginalCarta ?? Path.GetFileName(this.RutaCarta) ?? Localizacion.Texto("Adjuntos.CartaSinAdjuntar"));

    /// <summary>Historial de la solicitud abierta, como copias editables.</summary>
    public ObservableCollection<EventoViewModel> EventosEdicion { get; } = [];

    /// <summary>Eventos guardados que se han quitado del búfer: el guardado los borra del modelo.</summary>
    public List<Evento> EventosEliminados { get; } = [];

    public SolicitudEdicionViewModel() => this.CargarDesde(new Solicitud(string.Empty, string.Empty));

    public SolicitudEdicionViewModel(Solicitud solicitud) => this.CargarDesde(solicitud);

    /// <summary>Mensaje de campo obligatorio, el mismo que ya usaba el aviso al guardar.</summary>
    public static ValidationResult? ValidarObligatorio(string? valor) =>
        string.IsNullOrWhiteSpace(valor)
            ? new ValidationResult(Localizacion.Texto("Mensaje.FaltanDatos"))
            : ValidationResult.Success;

    /// <summary>El salario mínimo no puede superar al máximo, como exige la entidad.</summary>
    public static ValidationResult? ValidarRangoSalarial(int? valor, ValidationContext contexto) =>
        contexto.ObjectInstance is SolicitudEdicionViewModel editor && editor.SalarioMin > valor
            ? new ValidationResult(Localizacion.Texto("Mensaje.SalarioMinimoMayor"))
            : ValidationResult.Success;

    partial void OnEmpresaChanged(string value) => this.ValidateProperty(value, nameof(this.Empresa));

    partial void OnPuestoChanged(string value) => this.ValidateProperty(value, nameof(this.Puesto));

    /// <summary>Revalida todos los campos del búfer, incluidas las longitudes, antes de guardar.</summary>
    public void Validar() => this.ValidateAllProperties();

    public void CargarDesde(Solicitud solicitud)
    {
        this.Empresa = solicitud.Empresa;
        this.Puesto = solicitud.Puesto;
        this.EnlaceOferta = solicitud.EnlaceOferta;
        this.Portal = solicitud.Portal;
        this.Origen = solicitud.Origen;
        this.Ubicacion = solicitud.Ubicacion;
        this.Modalidad = solicitud.Modalidad;
        this.Tecnologias = solicitud.Tecnologias;
        this.Requisitos = solicitud.Requisitos;
        this.SalarioMin = solicitud.SalarioMin;
        this.SalarioMax = solicitud.SalarioMax;
        this.PretensionSalarial = solicitud.PretensionSalarial;
        this.Interes = solicitud.Interes;
        this.Estado = solicitud.Estado;
        this.MotivoRechazo = solicitud.MotivoRechazo;
        this.FechaSolicitud = solicitud.FechaSolicitud;
        this.FechaPrimeraRespuesta = solicitud.FechaPrimeraRespuesta;
        this.FechaEntrevista = solicitud.FechaEntrevista;
        this.ProximoSeguimiento = solicitud.ProximoSeguimiento;
        this.FechaCierre = solicitud.FechaCierre;
        this.RespuestaEmpresa = solicitud.RespuestaEmpresa;
        this.ContactoNombre = solicitud.ContactoNombre;
        this.ContactoEmail = solicitud.ContactoEmail;
        this.ContactoTelefono = solicitud.ContactoTelefono;
        this.Notas = solicitud.Notas;
        this.RutaCv = solicitud.RutaCv;
        this.NombreOriginalCv = solicitud.NombreOriginalCv;
        this.RutaCarta = solicitud.RutaCarta;
        this.NombreOriginalCarta = solicitud.NombreOriginalCarta;
        this.CargarEventos(solicitud);
        this.ValidateAllProperties();
    }

    private void CargarEventos(Solicitud solicitud)
    {
        this.EventosEdicion.Clear();
        this.EventosEliminados.Clear();

        foreach (Evento evento in solicitud.Eventos)
        {
            var edicion = new EventoViewModel();
            edicion.CargarDesde(evento);
            this.EventosEdicion.Add(edicion);
        }
    }

    public void EliminarEvento(EventoViewModel evento)
    {
        this.EventosEdicion.Remove(evento);

        if (evento.Original is Evento original && original.Id != 0)
        {
            this.EventosEliminados.Add(original);
        }
    }

    public void AplicarIdentificacion(Solicitud solicitud) =>
        solicitud.ActualizarIdentificacion(this.Empresa, this.Puesto);

    public void AplicarOferta(Solicitud solicitud) =>
        solicitud.ActualizarOferta(new DatosOferta(
            this.EnlaceOferta,
            this.Portal,
            this.Origen,
            this.Ubicacion,
            this.Modalidad,
            this.Tecnologias,
            this.Requisitos));

    public void AplicarEconomico(Solicitud solicitud) =>
        solicitud.ActualizarCondiciones(
            this.SalarioMin,
            this.SalarioMax,
            this.PretensionSalarial,
            this.Interes);

    public void AplicarEstado(Solicitud solicitud)
    {
        solicitud.Estado = this.Estado;
        solicitud.MotivoRechazo = this.MotivoRechazo;
    }

    public void AplicarFechas(Solicitud solicitud)
    {
        solicitud.FechaSolicitud = this.FechaSolicitud;
        solicitud.FechaPrimeraRespuesta = this.FechaPrimeraRespuesta;
        solicitud.FechaEntrevista = this.FechaEntrevista;
        solicitud.ProximoSeguimiento = this.ProximoSeguimiento;
        solicitud.FechaCierre = this.FechaCierre;
    }

    public void AplicarRespuesta(Solicitud solicitud)
    {
        solicitud.RespuestaEmpresa = this.RespuestaEmpresa;
    }

    public void AplicarContacto(Solicitud solicitud)
    {
        solicitud.ContactoNombre = this.ContactoNombre;
        solicitud.ContactoEmail = this.ContactoEmail;
        solicitud.ContactoTelefono = this.ContactoTelefono;
        solicitud.Notas = this.Notas;
    }

    public void AplicarAdjuntos(Solicitud solicitud)
    {
        solicitud.RutaCv = this.RutaCv;
        solicitud.NombreOriginalCv = this.NombreOriginalCv;
        solicitud.RutaCarta = this.RutaCarta;
        solicitud.NombreOriginalCarta = this.NombreOriginalCarta;
    }

    /// <summary>
    /// Vuelca el historial: los eventos con <see cref="EventoViewModel.Original"/> nulo se crean,
    /// los existentes se actualizan y los borrados salen del modelo.
    /// </summary>
    public void AplicarEventos(Solicitud solicitud)
    {
        foreach (EventoViewModel edicion in this.EventosEdicion)
        {
            if (edicion.Original is Evento original)
            {
                edicion.AplicarA(original);
                continue;
            }

            solicitud.AgregarEvento(new Evento(edicion.Fecha, edicion.Tipo, edicion.Descripcion));
        }

        foreach (Evento eliminado in this.EventosEliminados)
        {
            solicitud.QuitarEvento(eliminado);
        }
    }

    /// <summary>
    /// Vuelca al modelo solo los grupos cuya pestaña ya escribe en el editor. Mientras la UI
    /// siga bindeada a la entidad, ese grupo no se toca para no pisar lo editado.
    /// </summary>
    public void AplicarA(Solicitud solicitud)
    {
        this.AplicarOferta(solicitud);
        this.AplicarEconomico(solicitud);
        this.AplicarEstado(solicitud);
        this.AplicarFechas(solicitud);
        this.AplicarRespuesta(solicitud);
        this.AplicarContacto(solicitud);
        this.AplicarIdentificacion(solicitud);
        this.AplicarAdjuntos(solicitud);
        this.AplicarEventos(solicitud);
    }
}
