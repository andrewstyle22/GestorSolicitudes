namespace GestorSolicitudes.ViewModels;

/// <summary>
/// Lo que el ViewModel necesita pedirle al usuario: avisos, confirmaciones, elegir ficheros y
/// abrir enlaces. La implementación real vive en Views (WPF); los tests pasan un falso, así que
/// los comandos de MainViewModel se pueden probar sin abrir ninguna ventana.
/// </summary>
public interface IDialogoService
{
    /// <summary>Pregunta de sí/no. Devuelve true solo si el usuario confirma.</summary>
    bool Confirmar(string mensaje, string titulo);

    /// <summary>Advertencia: algo no es correcto, pero la app sigue.</summary>
    void Avisar(string mensaje, string titulo);

    /// <summary>Aviso informativo de que todo fue bien.</summary>
    void Informar(string mensaje, string titulo);

    /// <summary>Error: la operación no se pudo completar.</summary>
    void Error(string mensaje, string titulo);

    /// <summary>Ruta del fichero elegido, o null si el usuario cancela.</summary>
    string? ElegirFichero(string titulo, string filtro);

    /// <summary>Ruta donde guardar, o null si el usuario cancela.</summary>
    string? ElegirDestino(string titulo, string filtro, string nombrePropuesto);

    /// <summary>Abre una URL en el navegador. Puede lanzar si el sistema no puede abrirla.</summary>
    void AbrirEnlace(string url);
}
