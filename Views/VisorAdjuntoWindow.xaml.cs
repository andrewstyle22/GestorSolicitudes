namespace GestorSolicitudes.Views;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using GestorSolicitudes.Helpers;
using GestorSolicitudes.ViewModels;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

// Ventana de solo lectura para ver el CV o la carta adjuntos sin abrir el programa externo.
// PDF se renderiza con la API nativa de Windows, una página a la vez (un CV de 20 páginas no
// necesita 20 mapas de bits en memoria); DOCX y ODT se extraen como texto; DOC no tiene visor
// nativo y se ofrece descargar. Nunca modifica el fichero original.
[ExcludeFromCodeCoverage] // Code-behind de diálogo: la UI no es automatizable.
public partial class VisorAdjuntoWindow : Window
{
    private const string ClaveNoSePuedePrevisualizar = "Mensaje.NoSePuedePrevisualizar";

    private readonly string ruta;
    private readonly IDialogoService dialogo;
    private readonly CancellationTokenSource cancelacion = new();

    private PdfDocument? documento;
    private uint paginaActual;
    private bool renderizando;

    public VisorAdjuntoWindow(string ruta, IDialogoService dialogo)
    {
        this.InitializeComponent();
        this.ruta = ruta;
        this.dialogo = dialogo;
        this.Title = Path.GetFileName(ruta);
        this.Loaded += this.AlCargar;

        // Al cerrar se cancela el render en curso: la ventana puede desaparecer antes de que
        // Windows termine de pintar la página y no hay a quién enseñársela. Después se libera.
        this.Closed += (_, _) =>
        {
            this.cancelacion.Cancel();
            this.cancelacion.Dispose();
        };
    }

    private async void AlCargar(object sender, RoutedEventArgs e)
    {
        // Loaded puede llegar más de una vez (reparentado); el contenido se carga una sola vez.
        this.Loaded -= this.AlCargar;
        await this.CargarContenidoAsync(this.cancelacion.Token);
    }

    private async Task CargarContenidoAsync(CancellationToken token)
    {
        try
        {
            switch (Path.GetExtension(this.ruta).ToLowerInvariant())
            {
                case ".pdf":
                    await this.MostrarPdfAsync(token);
                    break;
                case ".docx":
                    this.MostrarTexto(DocumentoHelper.ExtraerTextoDocx(this.ruta));
                    break;
                case ".odt":
                    this.MostrarTexto(DocumentoHelper.ExtraerTextoOdt(this.ruta));
                    break;
                case ".doc":
                    this.MostrarAviso(Localizacion.Texto(ClaveNoSePuedePrevisualizar));
                    break;
                default:
                    this.MostrarTexto(await File.ReadAllTextAsync(this.ruta, token));
                    break;
            }
        }
        catch (Exception ex)
        {
            // No se pudo previsualizar (o se cerró la ventana a media carga): se avisa en la
            // propia ventana si sigue abierta.
            Debug.WriteLine($"No se pudo previsualizar '{this.ruta}': {ex}");

            if (!token.IsCancellationRequested)
            {
                this.MostrarAviso(Localizacion.Texto(ClaveNoSePuedePrevisualizar));
            }
        }
    }

    private async Task MostrarPdfAsync(CancellationToken token)
    {
        StorageFile archivo = await StorageFile.GetFileFromPathAsync(this.ruta);
        this.documento = await PdfDocument.LoadFromFileAsync(archivo);

        if (this.documento.PageCount == 0)
        {
            this.MostrarAviso(Localizacion.Texto(ClaveNoSePuedePrevisualizar));
            return;
        }

        this.BarraPaginas.Visibility = Visibility.Visible;
        await this.MostrarPaginaAsync(0, token);
    }

    private async Task MostrarPaginaAsync(uint indice, CancellationToken token)
    {
        if (this.documento is null || indice >= this.documento.PageCount || token.IsCancellationRequested)
        {
            return;
        }

        using PdfPage pagina = this.documento.GetPage(indice);
        using var memoria = new InMemoryRandomAccessStream();
        await pagina.RenderToStreamAsync(memoria);

        if (token.IsCancellationRequested)
        {
            return;
        }

        // OnLoad copia los píxeles y suelta el flujo: el using lo cierra en cuanto sale el bloque.
        // Freeze deja la imagen inmutable, que es lo que WPF necesita para pintarla sin más.
        var imagen = new BitmapImage();
        using (Stream flujo = memoria.AsStreamForRead())
        {
            imagen.BeginInit();
            imagen.CacheOption = BitmapCacheOption.OnLoad;
            imagen.StreamSource = flujo;
            imagen.EndInit();
        }

        imagen.Freeze();

        this.paginaActual = indice;
        this.ImagenPagina.Source = imagen;
        this.ActualizarBarra();
    }

    private void ActualizarBarra()
    {
        if (this.documento is null)
        {
            return;
        }

        this.TextoPagina.Text = string.Format(
            Localizacion.Texto("Visor.Pagina"),
            this.paginaActual + 1,
            this.documento.PageCount);

        this.AnteriorBoton.IsEnabled = this.paginaActual > 0;
        this.SiguienteBoton.IsEnabled = this.paginaActual + 1 < this.documento.PageCount;
    }

    private async void Anterior_Click(object sender, RoutedEventArgs e) => await this.CambiarPaginaAsync(-1);

    private async void Siguiente_Click(object sender, RoutedEventArgs e) => await this.CambiarPaginaAsync(1);

    private async Task CambiarPaginaAsync(int salto)
    {
        if (this.documento is null || this.renderizando)
        {
            return;
        }

        int destino = (int)this.paginaActual + salto;

        if (destino < 0 || destino >= this.documento.PageCount)
        {
            return;
        }

        // Dos renders a la vez acabarían pintando la página que termine última, no la pedida.
        this.renderizando = true;
        try
        {
            await this.MostrarPaginaAsync((uint)destino, this.cancelacion.Token);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"No se pudo pasar de página en '{this.ruta}': {ex}");
            this.MostrarAviso(Localizacion.Texto(ClaveNoSePuedePrevisualizar));
        }
        finally
        {
            this.renderizando = false;
        }
    }

    private void MostrarTexto(string texto)
    {
        this.ScrollContenido.Visibility = Visibility.Collapsed;
        this.BarraPaginas.Visibility = Visibility.Collapsed;
        this.CajaTexto.Text = texto;
        this.CajaTexto.Visibility = Visibility.Visible;
    }

    private void MostrarAviso(string mensaje)
    {
        this.ScrollContenido.Visibility = Visibility.Collapsed;
        this.CajaTexto.Visibility = Visibility.Collapsed;
        this.BarraPaginas.Visibility = Visibility.Collapsed;
        this.TextoAviso.Text = mensaje;
        this.TextoAviso.Visibility = Visibility.Visible;
    }

    private void Descargar_Click(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(this.ruta))
        {
            this.dialogo.Informar(
                Localizacion.Texto("Mensaje.SinAdjuntoOYaNoExiste"),
                Localizacion.Texto("Titulo.Adjunto"));
            return;
        }

        string? destino = this.dialogo.ElegirDestino(
            Localizacion.Texto("Boton.Descargar"),
            Localizacion.Texto("Dialogo.FiltroAdjuntos"),
            Path.GetFileName(this.ruta));

        if (destino is null)
        {
            return;
        }

        try
        {
            File.Copy(this.ruta, destino, true);
        }
        catch (Exception ex)
        {
            this.dialogo.Error(
                string.Format(Localizacion.Texto("Mensaje.NoSePudoGuardar"), ex.Message),
                Localizacion.Texto(Traducciones.ClaveError));
        }
    }

    private void Cerrar_Click(object sender, RoutedEventArgs e) => this.Close();
}
