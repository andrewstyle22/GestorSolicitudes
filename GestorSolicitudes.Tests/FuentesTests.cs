namespace GestorSolicitudes.Tests;

using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using GestorSolicitudes.ViewModels;

/// <summary>
/// Reglas que cruzan los ficheros fuente con el código. Una clave de traducción mal escrita
/// o una columna sin su x:Name no rompen la compilación, pero se ven en crudo en la UI.
/// Se leen los ficheros como texto porque en WPF solo puede existir una Application por
/// AppDomain, así que no se puede abrir la ventana en un test más.
/// </summary>
public partial class FuentesTests
{
    private static readonly string Raiz = RaizDelProyecto();

    [Fact]
    public void LasClavesDeTextoUsadasEstanDefinidas()
    {
        List<string> usadas = ClavesUsadas();

        Assert.NotEmpty(usadas);
        foreach (string clave in usadas)
        {
            Assert.True(Localizacion.Contiene(clave), $"La clave '{clave}' se usa pero no está en los diccionarios.");
        }
    }

    [Fact]
    public void LasColumnasOcultablesSonColumnasDelXamlConSuClave()
    {
        List<string> nombres = NombresDeColumna();

        Assert.Equal(6, MainViewModel.ColumnasOcultables.Length);
        foreach (string columna in MainViewModel.ColumnasOcultables)
        {
            Assert.Contains(columna, nombres);
            Assert.True(Localizacion.Contiene($"Columna.{columna}"), $"Falta la clave Columna.{columna}.");
        }
    }

    [Fact]
    public void NingunaColumnaSustituyeElEstiloDeLaCabecera()
    {
        // Un HeaderStyle propio sustituye al ColumnHeaderStyle del DataGrid en vez de
        // heredarlo, y esa columna se queda sin el fondo de la cabecera (le pasó a "Días").
        List<XElement> conEstilo = Columnas().Where(c => c.Attribute("HeaderStyle") is not null).ToList();

        Assert.Empty(conEstilo);
    }

    [Fact]
    public void LasClavesDeRecursosNoPisanLasDeTraduccion()
    {
        // Un recurso de App.xaml con la misma clave que un texto hace que el
        // {DynamicResource} de un TextBlock reciba el pincel y reviente al pintar.
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        List<string> claves = XDocument.Load(Path.Combine(Raiz, "App.xaml"))
            .Descendants()
            .Select(e => (string?)e.Attribute(x + "Key"))
            .Where(clave => clave is not null)
            .Select(clave => clave!)
            .ToList();

        Assert.NotEmpty(claves);
        foreach (string clave in claves)
        {
            Assert.False(Localizacion.Contiene(clave), $"El recurso '{clave}' pisa la clave de texto del mismo nombre.");
        }
    }

    [Fact]
    public void NingunFicheroDeTextoTieneCaracteresDeReemplazo()
    {
        IEnumerable<string> ficheros = FicherosDeCodigo()
            .Concat(FicherosDeXaml())
            .Concat(FicherosDeTest());

        foreach (string fichero in ficheros)
        {
            Assert.DoesNotContain("\uFFFD", File.ReadAllText(fichero));
        }
    }

    [Fact]
    public void LasColumnasDeTextoCompartenElEstiloDeCelda()
    {
        // Las nueve columnas de texto leen de EstiloCelda (o de un derivado con BasedOn);
        // un estilo incrustado por columna vuelve a desincronizar márgenes y recortes.
        List<XElement> deTexto = Columnas().Where(c => c.Name.LocalName == "DataGridTextColumn").ToList();

        Assert.Equal(9, deTexto.Count);
        Assert.All(deTexto, c => Assert.NotNull(c.Attribute("ElementStyle")));
        Assert.DoesNotContain(
            Columnas(),
            c => c.Descendants().Any(e => e.Name.LocalName == "DataGridTextColumn.ElementStyle"));
    }

    [Fact]
    public void LosViewModelsNoUsanWpfDirectamente()
    {
        string[] prohibidos = { "MessageBox.", "OpenFileDialog", "SaveFileDialog", "Process.Start" };

        foreach (string fichero in FicherosDelModeloDeVista())
        {
            string texto = File.ReadAllText(fichero);
            foreach (string prohibido in prohibidos)
            {
                Assert.False(
                    texto.Contains(prohibido),
                    $"{Path.GetFileName(fichero)} usa '{prohibido}': los diálogos van por IDialogoService.");
            }
        }
    }

    private static List<string> ClavesUsadas()
    {
        List<string> deCodigo = FicherosDeCodigo()
            .SelectMany(f => Claves(File.ReadAllText(f), ClaveEnCodigo()))
            .ToList();

        List<string> deXaml = FicherosDeXaml()
            .SelectMany(f =>
            {
                string texto = File.ReadAllText(f);
                return Claves(texto, ClaveEnXaml()).Concat(Claves(texto, ClaveDeParametro()));
            })
            .ToList();

        return deCodigo
            .Concat(deXaml)
            // "Columna." + nombre se resuelve en ColumnaItem; lo cubre el test de columnas.
            .Where(clave => !clave.EndsWith('.'))
            .Distinct()
            .Order()
            .ToList();
    }

    private static IEnumerable<string> Claves(string texto, Regex patron) =>
        patron.Matches(texto).Select(m => m.Groups[1].Value);

    private static List<XElement> Columnas() =>
        XDocument.Load(Path.Combine(Raiz, "Views", "MainWindow.xaml"))
            .Descendants()
            .Where(e => e.Name.LocalName is "DataGridTextColumn" or "DataGridTemplateColumn")
            .ToList();

    private static List<string> NombresDeColumna()
    {
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        return Columnas()
            .Select(c => (string?)c.Attribute(x + "Name"))
            .Where(nombre => nombre is not null)
            .Select(nombre => nombre!)
            .ToList();
    }

    private static IEnumerable<string> FicherosDeCodigo() =>
        Directory.EnumerateFiles(Raiz, "*.cs", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(Path.Combine(Raiz, "Views"), "*.cs"));

    private static IEnumerable<string> FicherosDelModeloDeVista() =>
        Directory.EnumerateFiles(Raiz, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(f => File.ReadAllText(f).Contains("namespace GestorSolicitudes.ViewModels"));

    private static IEnumerable<string> FicherosDeXaml() =>
        Directory.EnumerateFiles(Raiz, "*.xaml", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(Path.Combine(Raiz, "Views"), "*.xaml"));

    private static IEnumerable<string> FicherosDeTest() =>
        Directory.EnumerateFiles(Path.Combine(Raiz, "GestorSolicitudes.Tests"), "*.cs");

    private static string RaizDelProyecto()
    {
        var carpeta = new DirectoryInfo(AppContext.BaseDirectory);

        while (carpeta is not null && !File.Exists(Path.Combine(carpeta.FullName, "GestorSolicitudes.slnx")))
        {
            carpeta = carpeta.Parent;
        }

        Assert.NotNull(carpeta);
        return carpeta!.FullName;
    }

    [GeneratedRegex(@"Localizacion\.Texto\(""([^""]+)""")]
    private static partial Regex ClaveEnCodigo();

    [GeneratedRegex(@"\{DynamicResource\s+([^\s}]+)\}")]
    private static partial Regex ClaveEnXaml();

    [GeneratedRegex(@"ConverterParameter=([A-Za-z0-9_.]+)")]
    private static partial Regex ClaveDeParametro();
}
