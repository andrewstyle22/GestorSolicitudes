using Xunit;
using System.Windows;
using System.Windows.Controls;
using GestorSolicitudes;

namespace GestorSolicitudes.Tests;

public class PestanasTests
{
    /// <summary>
    /// Los estilos de las pestañas del panel de detalle viven en App.xaml y solo se
    /// resuelven al aplicarse la plantilla, no al compilar: este test carga los
    /// recursos (sin pasar por OnStartup, que crearía la base real) y fuerza el
    /// dibujo de un TabControl para que se construyan TabPanel, ContentPresenter y
    /// los StaticResource que usan las plantillas.
    /// </summary>
    [StaFact]
    public void Pestanas_EstilosDeDetalleExistenYAplican()
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
    }
}