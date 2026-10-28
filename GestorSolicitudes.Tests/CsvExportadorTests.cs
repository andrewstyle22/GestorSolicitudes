using GestorSolicitudes.Helpers;
using GestorSolicitudes.Models;

namespace GestorSolicitudes.Tests;

public class CsvExportadorTests
{
    [Fact]
    public void Construir_IncluyeCabeceraTraducidaYLosDatosDeLaFila()
    {
        var solicitud = new Solicitud
        {
            Empresa = "ACME",
            Puesto = "Dev",
            FechaSolicitud = new DateTime(2024, 5, 1),
            Portal = "LinkedIn",
            Estado = EstadoSolicitud.Enviada,
            EnlaceOferta = "https://www.linkedin.com/jobs/view/abc",
        };

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
