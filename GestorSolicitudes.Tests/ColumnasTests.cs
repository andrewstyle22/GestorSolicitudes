using System.IO;
using GestorSolicitudes.Helpers;
using GestorSolicitudes.ViewModels;

namespace GestorSolicitudes.Tests;

/// <summary>
/// Selector de columnas de la tabla: la preferencia se guarda en un fichero aparte
/// (nunca en %APPDATA% desde los tests) y las claves son los nombres del XAML, que
/// además son las claves del diccionario Columna.*.
/// </summary>
public class ColumnasTests
{
    private static string RutaTemporal() => Path.Combine(TestDb.NuevaCarpeta(), "columnas.txt");

    // ---------------------------------------------------------------- Helper

    [Fact]
    public void GuardarEn_YCargarDe_DevuelvenLasMismasClaves()
    {
        string ruta = RutaTemporal();

        ColumnasHelper.GuardarEn(ruta, ["Dias", "Portal"]);

        Assert.Equal(["Dias", "Portal"], ColumnasHelper.CargarDe(ruta)!.Order());
    }

    [Fact]
    public void CargarDe_SinFicheroDevuelveNullParaQueSalganTodas()
    {
        Assert.Null(ColumnasHelper.CargarDe(RutaTemporal()));
    }

    [Fact]
    public void CargarDe_IgnoraLineasVaciasYNoDistingueMayusculas()
    {
        string ruta = RutaTemporal();
        File.WriteAllText(ruta, "Dias\r\n  \r\n\r\ndias\r\n");

        HashSet<string>? claves = ColumnasHelper.CargarDe(ruta);

        Assert.NotNull(claves);
        Assert.Equal(["Dias"], claves);
    }

    [Fact]
    public void GuardarEn_CreaLaCarpetaSiNoExiste()
    {
        string ruta = Path.Combine(TestDb.NuevaCarpeta(), "anidada", "columnas.txt");

        ColumnasHelper.GuardarEn(ruta, ["Interes"]);

        Assert.True(File.Exists(ruta));
    }

    [Fact]
    public void GuardarEn_SinClavesDejaElFicheroVacio()
    {
        string ruta = RutaTemporal();

        ColumnasHelper.GuardarEn(ruta, []);

        Assert.Empty(ColumnasHelper.CargarDe(ruta)!);
    }

    // ---------------------------------------------------------------- Item

    [Fact]
    public void ColumnaItem_CambiarVisibleAvisaYNotifica()
    {
        int avisos = 0;
        var columna = new ColumnaItem("Dias", true, () => avisos++);

        columna.Visible = false;

        Assert.Equal(1, avisos);
        Assert.False(columna.Visible);
    }

    [Fact]
    public void ColumnaItem_MarcarLoMismoNoAvisa()
    {
        int avisos = 0;
        var columna = new ColumnaItem("Dias", true, () => avisos++);

        columna.Visible = true;

        Assert.Equal(0, avisos);
    }

    [Fact]
    public void ColumnaItem_NombreSaleDelDiccionarioYSeRefresca()
    {
        var columna = new ColumnaItem("PrimeraRespuesta", true, () => { });
        var notificados = new List<string?>();
        columna.PropertyChanged += (_, e) => notificados.Add(e.PropertyName);

        Assert.Equal("1ª respuesta", columna.Nombre);

        columna.RefrescarNombre();

        Assert.Equal(["Nombre"], notificados);
    }

    // ---------------------------------------------------------------- ViewModel

    [Fact]
    public void Columnas_SinPreferenciaSonLasSeisVisibles()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db, RutaTemporal());

        Assert.Equal(
            ["Dias", "PrimeraRespuesta", "Entrevista", "Seguimiento", "Interes", "Portal"],
            vm.Columnas.Select(c => c.Columna));
        Assert.All(vm.Columnas, c => Assert.True(c.Visible));
    }

    [Fact]
    public void Columnas_ConPreferenciaSoloRespetanLasGuardadas()
    {
        string ruta = RutaTemporal();
        ColumnasHelper.GuardarEn(ruta, ["Dias", "Portal"]);

        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db, ruta);

        Assert.Equal(
            [true, false, false, false, false, true],
            vm.Columnas.Select(c => c.Visible));
    }

    [Fact]
    public void Columnas_CadaClaveTieneSuTextoEnElDiccionario()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db, RutaTemporal());

        // La clave guardada es a la vez el x:Name de la columna y el sufijo de Columna.*:
        // si una se queda atrás, el botón mostraría la clave en crudo.
        Assert.All(vm.Columnas, c => Assert.True(Localizacion.Contiene("Columna." + c.Columna)));
    }

    [Fact]
    public void OcultarUnaColumna_GuardaSoloLasVisiblesYAvisaALaVista()
    {
        string ruta = RutaTemporal();
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db, ruta);
        int avisos = 0;
        vm.ColumnaVisibilidadCambiada += () => avisos++;

        vm.Columnas.First(c => c.Columna == "Entrevista").Visible = false;

        Assert.Equal(1, avisos);

        // El fichero guarda solo las visibles (el orden del HashSet no se garantiza).
        Assert.Equal(
            ["Dias", "Interes", "Portal", "PrimeraRespuesta", "Seguimiento"],
            ColumnasHelper.CargarDe(ruta)!.Order());
    }

    [Fact]
    public void PanelColumnasAbierto_EmpiezaCerradoYAbreAlPedirlo()
    {
        using var db = TestDb.NuevoContexto();
        var vm = new MainViewModel(db, RutaTemporal());

        Assert.False(vm.PanelColumnasAbierto);

        vm.PanelColumnasAbierto = true;

        Assert.True(vm.PanelColumnasAbierto);
    }
}
