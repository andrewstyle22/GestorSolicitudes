namespace GestorSolicitudes.Helpers;

/// <summary>
/// Detecta si ya hay otra copia de la aplicación abierta y permite avisarle. Los nombres
/// se pueden cambiar para que los tests usen unos propios y no choquen con la app real.
/// </summary>
public sealed class InstanciaUnica : IDisposable
{
    /// <summary>Nombre del mutex de la aplicación; los tests pasan uno único.</summary>
    public const string NombrePredeterminado = "GestorSolicitudes";

    private readonly Mutex mutex;
    private readonly EventWaitHandle aviso;
    private volatile bool cerrada;

    public InstanciaUnica(string nombre = NombrePredeterminado)
    {
        // createdNew es falso cuando otra copia ya tiene el mutex: esta no lo adquiere.
        this.mutex = new Mutex(initiallyOwned: true, nombre, out bool creada);
        this.EsPrimera = creada;
        this.aviso = new EventWaitHandle(initialState: false, EventResetMode.AutoReset, nombre + ".aviso");
    }

    /// <summary>Verdadero si es la única copia abierta (y por tanto la que escucha el aviso).</summary>
    public bool EsPrimera { get; }

    /// <summary>Despierta a la primera copia para que muestre su ventana.</summary>
    public void AvisarALaPrimera() => this.aviso.Set();

    /// <summary>
    /// Espera avisos de otras copias y ejecuta la acción con cada uno. Bloquea, así que se
    /// llama desde un hilo de fondo. Termina al liberar el objeto.
    /// </summary>
    public void EsperarAviso(Action accion)
    {
        try
        {
            while (!this.cerrada && this.aviso.WaitOne())
            {
                if (!this.cerrada)
                {
                    accion();
                }
            }
        }
        catch (ObjectDisposedException)
        {
            // Dispose cerró el manejador mientras se esperaba: la escucha termina.
        }
    }

    public void Dispose()
    {
        // Despierta a EsperarAviso para que vea cerrada y salga del bucle. No se llama a
        // ReleaseMutex: su propiedad es del hilo que lo creó y Dispose puede llegar desde
        // otro; cerrar el último manejador destruye el objeto con nombre.
        this.cerrada = true;
        this.aviso.Set();
        this.aviso.Dispose();
        this.mutex.Dispose();
    }
}
