namespace GestorSolicitudes.Tests;

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using GestorSolicitudes;
using GestorSolicitudes.ViewModels;
using GestorSolicitudes.Views;

public class PestanasTests
{
    /// <summary>
    /// Los estilos que dependen de una plantilla (las pestañas del panel de detalle y el
    /// botón del desplegable de columnas) viven en App.xaml y solo se resuelven al
    /// aplicarse, no al compilar: este test carga los recursos (sin pasar por OnStartup,
    /// que crearía la base real) y fuerza el dibujo para que se construyan las plantillas
    /// y se activen los triggers.
    /// Solo puede existir una Application por AppDomain, así que todos los estilos con
    /// plantilla se comprueban aquí y en un único test.
    /// </summary>
    [StaFact]
    public void AppXaml_LosEstilosConPlantillaSeComponenAlPintar()
    {
        var app = new App();
        app.InitializeComponent();

        var estiloPestanas = (Style)app.Resources["Pestanas"]!;
        var estiloPestana = (Style)app.Resources["Pestana"]!;

        Assert.NotNull(estiloPestanas);
        Assert.NotNull(estiloPestana);
        Assert.Contains(estiloPestana.Setters, s => s is Setter setter && setter.Property == Control.TemplateProperty);

        var pestanas = new TabControl { Style = estiloPestanas };
        pestanas.Items.Add(new TabItem { Style = estiloPestana, Header = "Oferta" });
        pestanas.Items.Add(new TabItem { Style = estiloPestana, Header = "Seguimiento", IsSelected = true });

        // El layout del TabControl genera el TabPanel (IsItemsHost), aplica las
        // plantillas de los TabItem y activa los triggers con los Estáticos de
        // recurso del subrayado de acento.
        pestanas.Measure(new Size(400, 300));
        pestanas.Arrange(new Rect(0, 0, 400, 300));
        pestanas.UpdateLayout();

        Assert.NotNull(pestanas.Template);
        Assert.True(((TabItem)pestanas.Items[1]).IsSelected);

        // El botón de "Columnas": sin plantilla propia sería un rectángulo gris, y el
        // trigger de IsChecked es lo que pinta el borde de acento al abrirse la lista.
        var estiloBoton = (Style)app.Resources["BotonDesplegable"]!;
        Assert.Equal(typeof(ToggleButton), estiloBoton.TargetType);
        Assert.Contains(estiloBoton.Setters, s => s is Setter setter && setter.Property == Control.TemplateProperty);

        var boton = new ToggleButton { Style = estiloBoton };
        boton.Measure(new Size(120, 30));
        boton.Arrange(new Rect(0, 0, 120, 30));
        boton.UpdateLayout();

        Assert.NotNull(boton.Template);
        Assert.NotNull(boton.Template.FindName("borde", boton));

        boton.IsChecked = true;
        boton.UpdateLayout();

        Assert.Equal(
            app.Resources["Acento"],
            ((Border)boton.Template.FindName("borde", boton)).BorderBrush);

        // La plantilla del botón es una sola: BotonBase y BotonDesplegable la comparten.
        var plantillaBoton = (ControlTemplate)app.Resources["BotonPlano"]!;
        Assert.Equal(typeof(ButtonBase), plantillaBoton.TargetType);
        Assert.Same(plantillaBoton, ValorDeEstilo((Style)app.Resources["BotonBase"]!, Control.TemplateProperty));
        Assert.Same(plantillaBoton, ValorDeEstilo(estiloBoton, Control.TemplateProperty));

        // La fila de adjunto es un solo control para CV y carta: la etiqueta y el adjunto
        // (ruta + nombre) llegan por propiedades de dependencia y el XAML los resuelve con ElementName.
        var fila = new FilaAdjunto
        {
            Etiqueta = "Currículum",
            Adjunto = new Adjunto(@"C:\mi-cv.pdf", "mi-cv.pdf"),
        };
        fila.Measure(new Size(500, 30));
        fila.Arrange(new Rect(0, 0, 500, 30));
        fila.UpdateLayout();

        List<string> textos = Descendientes<TextBlock>(fila).Select(t => t.Text).ToList();
        Assert.Contains("Currículum", textos);
        Assert.Contains("mi-cv.pdf", textos);

        // La cabecera de las columnas: el fondo tiene que ser el gris de la paleta y no el del
        // tema de Windows, que es lo que pone el DataGrid si no se le da estilo.
        var estiloCabecera = (Style)app.Resources["CabeceraColumna"]!;
        Assert.Equal(typeof(DataGridColumnHeader), estiloCabecera.TargetType);
        Assert.Equal(app.Resources["Cabecera"], ValorDeEstilo(estiloCabecera, Control.BackgroundProperty));
        Assert.Equal(app.Resources["Texto"], ValorDeEstilo(estiloCabecera, TextBlock.ForegroundProperty));

        var cabecera = new DataGridColumnHeader { Style = estiloCabecera };
        cabecera.Measure(new Size(120, 30));
        cabecera.Arrange(new Rect(0, 0, 120, 30));
        cabecera.UpdateLayout();

        Assert.Equal(app.Resources["Cabecera"], cabecera.Background);
        Assert.Equal(
            FontWeights.SemiBold,
            estiloCabecera.Setters.OfType<Setter>().Single(s => s.Property == TextBlock.FontWeightProperty).Value);

        // El icono de la ventana sale de una ruta absoluta: BitmapFrame.Create no resuelve
        // la forma relativa y la trataría como una ruta de fichero. El de la bandeja usa
        // la misma ruta, así que se comprueba que ambas formas leen el recurso.
        BitmapFrame iconoVentana = BitmapFrame.Create(MainWindow.RutaIcono);
        Assert.True(iconoVentana.PixelWidth >= 16);

        StreamResourceInfo? recurso = Application.GetResourceStream(MainWindow.RutaIcono);
        Assert.NotNull(recurso);
        using (var icono = new System.Drawing.Icon(recurso!.Stream))
        {
            Assert.True(icono.Width >= 16);
        }

        // Las pestañas y la cabecera de métricas son UserControl: no basta con que existan,
        // sus enlaces tienen que resolver. Un path mal escrito deja el campo vacío y el botón
        // sin comando, sin avisar en ningún sitio.
        using var db = TestDb.NuevoContexto();
        using var vm = new MainViewModel(
            db,
            rutaColumnas: Path.Combine(TestDb.NuevaCarpeta(), "columnas.txt"),
            carpetaAdjuntos: TestDb.NuevaCarpeta());
        vm.Edicion = new SolicitudBuilder()
            .ConEmpresa("ACME")
            .ConPuesto("Programador")
            .ConEnlaceOferta("https://ejemplo.test/oferta")
            .ConNotas("hablar el lunes")
            .ConRutaCv(@"C:\cv.pdf")
            .Construir();

        var detalleOferta = new PestanaOferta { DataContext = vm };
        Dibujar(detalleOferta);
        Assert.Contains(Descendientes<TextBox>(detalleOferta), t => t.Text == "https://ejemplo.test/oferta");
        Assert.Contains(Descendientes<Button>(detalleOferta), b => ReferenceEquals(b.Command, vm.AbrirEnlaceCommand));

        var detalleSeguimiento = new PestanaSeguimiento { DataContext = vm };
        Dibujar(detalleSeguimiento);
        Assert.Contains(Descendientes<TextBox>(detalleSeguimiento), t => t.Text == "hablar el lunes");

        var detalleAdjuntos = new PestanaAdjuntos { DataContext = vm };
        Dibujar(detalleAdjuntos);
        Assert.Contains(
            Descendientes<FilaAdjunto>(detalleAdjuntos),
            f => ReferenceEquals(f.ComandoAdjuntar, vm.AdjuntarCvCommand)
                && ReferenceEquals(f.ComandoAbrir, vm.AbrirAdjuntoCommand));

        var metricas = new CabeceraMetricas { DataContext = vm };
        Dibujar(metricas);
        Assert.Equal(12, Descendientes<TextBlock>(metricas).Count());
    }

    private static void Dibujar(FrameworkElement elemento)
    {
        elemento.Measure(new Size(420, 700));
        elemento.Arrange(new Rect(0, 0, 420, 700));
        elemento.UpdateLayout();
    }

    private static object? ValorDeEstilo(Style estilo, DependencyProperty propiedad) =>
        estilo.Setters.OfType<Setter>().Single(s => s.Property == propiedad).Value;

    private static IEnumerable<T> Descendientes<T>(DependencyObject raiz)
        where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(raiz); i++)
        {
            DependencyObject hijo = VisualTreeHelper.GetChild(raiz, i);
            if (hijo is T encontrado)
            {
                yield return encontrado;
            }

            foreach (T nieto in Descendientes<T>(hijo))
            {
                yield return nieto;
            }
        }
    }
}
