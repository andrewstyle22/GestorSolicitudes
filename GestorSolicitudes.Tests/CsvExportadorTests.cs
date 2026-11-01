namespace GestorSolicitudes.Tests;

using GestorSolicitudes.Helpers;
using GestorSolicitudes.Models;

public class CsvExportadorTests
{
    [Fact]
    public void Construir_IncluyeCabeceraTraducidaYLosDatosDeLaFila()
    {
        var solicitud = new SolicitudBuilder()
            .ConEmpresa("ACME")
            .ConPuesto("Dev")
            .ConFechaSolicitud(new DateTime(2024, 5, 1))
            .ConPortal("LinkedIn")
            .ConEstado(EstadoSolicitud.Enviada)
            .ConEnlaceOferta("https://www.linkedin.com/jobs/view/abc")
            .Construir();

        string csv = CsvExportador.Construir(new[] { solicitud });

        Assert.Contains(Localizacion.Texto("Csv.Empresa"), csv);
        Assert.Contains("ACME", csv);
        Assert.Contains("2024-05-01", csv);
        Assert.Contains("https://www.linkedin.com/jobs/view/abc", csv);
    }

    [Fact]
    public void Construir_SinSolicitudesSoloDevuelveLaCabecera()
    {
        string csv = CsvExportador.Construir(Array.Empty<Solicitud>());

        Assert.Single(csv.TrimEnd().Split(Environment.NewLine));
        Assert.StartsWith(Localizacion.Texto("Csv.Empresa"), csv);
    }

    [Fact]
    public void Construir_TodasLasCabecerasEstanTraducidas()
    {
        string csv = CsvExportador.Construir(Array.Empty<Solicitud>());

        Assert.DoesNotContain("Csv.", csv);
    }
}
