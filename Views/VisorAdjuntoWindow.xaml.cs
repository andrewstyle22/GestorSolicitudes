namespace GestorSolicitudes.Views;

using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

// Ventana de solo lectura para ver el CV o la carta adjuntos sin abrir el programa externo.
// PDF se renderiza con la API nativa de Windows; DOCX se extrae como texto; DOC no tiene
// visor nativo y se ofrece descargar. Nunca modifica el fichero original.
public partial class VisorAdjuntoWindow : Window
{
    private readonly string ruta;

    public VisorAdjuntoWindow(string ruta)
    {
        this.InitializeComponent();
        this.ruta = ruta;
        this.Title = Path.GetFileName(ruta);
        this.Loaded += async (_, _) => await this.CargarContenidoAsync();
    }

    private async System.Threading.Tasks.Task CargarContenidoAsync()
    {
        try
        {
            switch (Path.GetExtension(this.ruta).ToLowerInvariant())
            {
                case ".pdf":
                    await this.MostrarPdfAsync();
                    break;
                case ".docx":
                    this.MostrarTexto(ExtraerTextoDocx(this.ruta));
                    break;
                case ".doc":
                    this.MostrarAviso(Localizacion.Texto("Mensaje.NoSePuedePrevisualizar"));
                    break;
                default:
                    this.MostrarTexto(File.ReadAllText(this.ruta));
                    break;
            }
        }
        catch
        {
            this.MostrarAviso(Localizacion.Texto("Mensaje.NoSePuedePrevisualizar"));
        }
    }

    private async System.Threading.Tasks.Task MostrarPdfAsync()
    {
        StorageFile archivo = await StorageFile.GetFileFromPathAsync(this.ruta);
        PdfDocument documento = await PdfDocument.LoadFromFileAsync(archivo);

        for (uint i = 0; i < documento.PageCount; i++)
        {
            using PdfPage pagina = documento.GetPage(i);
            using var flujo = new InMemoryRandomAccessStream();
            await pagina.RenderToStreamAsync(flujo);

            var imagen = new BitmapImage();
            imagen.BeginInit();
            imagen.CacheOption = BitmapCacheOption.OnLoad;
            imagen.StreamSource = flujo.AsStreamForRead();
            imagen.EndInit();

            this.PanelPaginas.Children.Add(new System.Windows.Controls.Image
            {
                Source = imagen,
                Stretch = System.Windows.Media.Stretch.Uniform,
                Margin = new Thickness(0, 0, 0, 8),
            });
        }
    }

    private void MostrarTexto(string texto)
    {
        this.ScrollContenido.Visibility = Visibility.Collapsed;
        this.CajaTexto.Text = texto;
        this.CajaTexto.Visibility = Visibility.Visible;
    }

    private void MostrarAviso(string mensaje)
    {
        this.ScrollContenido.Visibility = Visibility.Collapsed;
        this.TextoAviso.Text = mensaje;
        this.TextoAviso.Visibility = Visibility.Visible;
    }

    private static string ExtraerTextoDocx(string ruta)
    {
        using ZipArchive zip = ZipFile.OpenRead(ruta);
        ZipArchiveEntry? entrada = zip.GetEntry("word/document.xml");
        if (entrada is null)
        {
            return string.Empty;
        }

        using var lector = new StreamReader(entrada.Open());
        string xml = lector.ReadToEnd();
        xml = Regex.Replace(xml, "</w:p>", "\n");
        xml = Regex.Replace(xml, "<[^>]+>", string.Empty);
        return System.Net.WebUtility.HtmlDecode(xml);
    }

    private void Descargar_Click(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(this.ruta))
        {
            MessageBox.Show(
                Localizacion.Texto("Mensaje.SinAdjuntoOYaNoExiste"),
                Localizacion.Texto("Titulo.Adjunto"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var dialogo = new SaveFileDialog
        {
            Title = Localizacion.Texto("Boton.Descargar"),
            FileName = Path.GetFileName(this.ruta),
            Filter = Localizacion.Texto("Dialogo.FiltroAdjuntos"),
            DefaultExt = Path.GetExtension(this.ruta),
        };

        if (dialogo.ShowDialog(this) == true)
        {
            try
            {
                File.Copy(this.ruta, dialogo.FileName, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(Localizacion.Texto("Mensaje.NoSePudoGuardar"), ex.Message),
                    Localizacion.Texto("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    private void Cerrar_Click(object sender, RoutedEventArgs e) => this.Close();
}
