using Xunit;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using GestorSolicitudes;

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
    }
}
