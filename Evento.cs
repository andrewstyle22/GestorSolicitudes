namespace GestorSolicitudes.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Hito dentro de un proceso de selección: una llamada, una prueba, una entrevista...
/// </summary>
public class Evento
{
    public Evento(DateTime fecha, TipoEvento tipo, string descripcion)
    {
        this.Fecha = fecha;
        this.Tipo = tipo;
        this.Descripcion = descripcion;
    }

    private Evento()
    {
    }

    public int Id { get; private set; }

    public int SolicitudId { get; private set; }

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
