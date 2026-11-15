namespace GestorSolicitudes.Tests;

using GestorSolicitudes.Helpers;

/// <summary>
/// Instancia única: cada test usa un nombre propio para no chocar con la app real ni con
/// los demás tests, que xUnit puede ejecutar en paralelo.
/// </summary>
public class InstanciaUnicaTests
{
    private static string NombreUnico() => "GestorSolicitudes.Tests." + Guid.NewGuid().ToString("N");

    [Fact]
    public void EsPrimera_LaPrimeraCopiaLoEs()
    {
        using var instancia = new InstanciaUnica(NombreUnico());

        Assert.True(instancia.EsPrimera);
    }

    [Fact]
    public void EsPrimera_LaSegundaCopiaConElMismoNombreNoLoEs()
    {
        string nombre = NombreUnico();
        using var primera = new InstanciaUnica(nombre);
        using var segunda = new InstanciaUnica(nombre);

        Assert.True(primera.EsPrimera);
        Assert.False(segunda.EsPrimera);
    }

    [Fact]
    public void EsPrimera_UnNombreDistintoNoCuentaComoSegundaCopia()
    {
        using var primera = new InstanciaUnica(NombreUnico());
        using var otra = new InstanciaUnica(NombreUnico());

        Assert.True(otra.EsPrimera);
    }

    [Fact]
    public async Task AvisarALaPrimera_EjecutaLaAccionQueEspera()
    {
        string nombre = NombreUnico();
        var primera = new InstanciaUnica(nombre);
        using var segunda = new InstanciaUnica(nombre);
        var avisada = new TaskCompletionSource();

        Task espera = Task.Run(() => primera.EsperarAviso(avisada.SetResult));
        try
        {
            segunda.AvisarALaPrimera();

            await avisada.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            primera.Dispose();
        }

        await espera.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task EsperarAviso_TerminaAlLiberarElObjeto()
    {
        var instancia = new InstanciaUnica(NombreUnico());

        Task espera = Task.Run(() => instancia.EsperarAviso(() => { }));

        instancia.Dispose();

        await espera.WaitAsync(TimeSpan.FromSeconds(5));
    }
}
