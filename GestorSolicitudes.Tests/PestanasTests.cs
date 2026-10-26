using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Resources;
using GestorSolicitudes;
using GestorSolicitudes.Views;

namespace GestorSolicitudes.Tests;

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
        Assert.Same(plantillaBoton, ((Style)app.Resources["BotonBase"]!).Setters
            .OfType<Setter>().Single(s => s.Property == Control.TemplateProperty).Value);
        Assert.Same(plantillaBoton, estiloBoton.Setters
            .OfType<Setter>().Single(s => s.Property == Control.TemplateProperty).Value);

        // La fila de adjunto es un solo control para CV y carta: la etiqueta y el nombre
        // llegan por propiedades de dependencia y el XAML los resuelve con ElementName.
        var fila = new FilaAdjunto { Etiqueta = "Currículum", Texto = "mi-cv.pdf", Ruta = @"C:\mi-cv.pdf" };
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
        Assert.Equal(app.Resources["Cabecera"], estiloCabecera.Setters
            .OfType<Setter>().Single(s => s.Property == Control.BackgroundProperty).Value);
        Assert.Equal(app.Resources["Texto"], estiloCabecera.Setters
            .OfType<Setter>().Single(s => s.Property == TextBlock.ForegroundProperty).Value);

        var cabecera = new DataGridColumnHeader { Style = estiloCabecera };
        cabecera.Measure(new Size(120, 30));
        cabecera.Arrange(new Rect(0, 0, 120, 30));
        cabecera.UpdateLayout();

        Assert.Equal(app.Resources["Cabecera"], cabecera.Background);
        Assert.Equal(
            FontWeights.SemiBold,
            estiloCabecera.Setters.OfType<Setter>().Single(s => s.Property == TextBlock.FontWeightProperty).Value);

        // El icono de la bandeja se carga del recurso empaquetado, así que se comprueba
        // que existe y trae los tamaños que usa la bandeja, con la misma ruta que usa
        // MainWindow: relativa al ensamblado principal y sin "pack://" absoluto.
        StreamResourceInfo? recurso = Application.GetResourceStream(
            new Uri($"/{typeof(App).Assembly.GetName().Name};component/Resources/appicon.ico", UriKind.Relative));

        Assert.NotNull(recurso);
        using (var icono = new System.Drawing.Icon(recurso!.Stream))
        {
            Assert.True(icono.Width >= 16);
        }
    }

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
