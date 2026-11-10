namespace GestorSolicitudes.Models;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Hito dentro de un proceso de selección: una llamada, una prueba, una entrevista...
/// </summary>
public class Evento
{
    private const string JustificacionEf = "EF Core escribe esta propiedad por campo";

    public Evento(DateTime fecha, TipoEvento tipo, string descripcion)
    {
        this.Fecha = fecha;
        this.Tipo = tipo;
        this.Descripcion = descripcion;
    }

    private Evento()
    {
    }

    [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = JustificacionEf)]
    public int Id { get; private set; }

    [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = JustificacionEf)]
    public int SolicitudId { get; private set; }

    [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = JustificacionEf)]
    public Solicitud? Solicitud { get; private set; }

    public DateTime Fecha { get; private set; }

    public TipoEvento Tipo { get; private set; }

    [MaxLength(500)]
    public string Descripcion { get; private set; } = string.Empty;

    public void Actualizar(DateTime fecha, TipoEvento tipo, string descripcion)
    {
        this.Fecha = fecha;
        this.Tipo = tipo;
        this.Descripcion = descripcion;
    }
}
