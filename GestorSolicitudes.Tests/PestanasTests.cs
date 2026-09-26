using Xunit;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Resources;
using GestorSolicitudes;
namespace GestorSolicitudes.Tests;

public class PestanasTests
{
    /// <summary>
    /// Los estilos que dependen de una plantilla (las pestaÃ±as del panel de detalle y el
    /// botÃ³n del desplegable de columnas) viven en App.xaml y solo se resuelven al
    /// aplicarse, no al compilar: este test carga los recursos (sin pasar por OnStartup,
    /// que crearÃ­a la base real) y fuerza el dibujo para que se construyan las plantillas
    /// y se activen los triggers.
    /// Solo puede existir una Application por AppDomain, asÃ­ que todos los estilos con
    /// plantilla se comprueban aquÃ­ y en un Ãºnico test.
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
        // plantillas de los TabItem y activa los triggers con los EstÃ¡ticos de
        // recurso del subrayado de acento.
        pestanas.Measure(new Size(400, 300));
        pestanas.Arrange(new Rect(0, 0, 400, 300));
        pestanas.UpdateLayout();

        Assert.NotNull(pestanas.Template);
        Assert.True(((TabItem)pestanas.Items[1]).IsSelected);

        // El botÃ³n de "Columnas": sin plantilla propia serÃ­a un rectÃ¡ngulo gris, y el
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

        // Un HeaderStyle por columna sustituye al ColumnHeaderStyle del DataGrid en vez de
        // heredarlo, y esa columna se queda sin el fondo gris (le pasÃ³ a "DÃ­as"). Comprobarlo
        // sobre la ventana real no cabe aquÃ­ (WPF solo admite una Application por AppDomain y
        // la crea este mismo test), asÃ­ que se mira el XAML, sin comentarios: si alguna
        // columna declara un HeaderStyle propio, el tooltip va en su HeaderTemplate.
        var xaml = Regex.Replace(XamlDeLaVentana(), "<!--.*?-->", string.Empty, RegexOptions.Singleline);
        Assert.DoesNotContain("HeaderStyle", xaml.Replace("ColumnHeaderStyle", string.Empty));

        // El icono de la bandeja se carga del recurso empaquetado, asÃ­ que se comprueba
        // que existe y trae los tamaÃ±os que usa la bandeja.
        StreamResourceInfo? recurso = Application.GetResourceStream(
            new Uri("pack://application:,,,/GestorSolicitudes;component/Resources/appicon.ico"));

        Assert.NotNull(recurso);
        using (var icono = new System.Drawing.Icon(recurso!.Stream))
        {
            Assert.True(icono.Width >= 16);
        }
    }

    /// <summary>Localiza Views/MainWindow.xaml subiendo desde la carpeta de salida.</summary>
    private static string XamlDeLaVentana()
    {
        var carpeta = new DirectoryInfo(AppContext.BaseDirectory);

        while (carpeta is not null && !File.Exists(Path.Combine(carpeta.FullName, "Views", "MainWindow.xaml")))
        {
            carpeta = carpeta.Parent;
        }

        Assert.NotNull(carpeta);
        return File.ReadAllText(Path.Combine(carpeta!.FullName, "Views", "MainWindow.xaml"));
    }
}

