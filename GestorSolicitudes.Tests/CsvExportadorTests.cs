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
    public void Construir_MotivoDeCierreYFechaDeReaperturaVanTrasCierre()
    {
        var solicitud = new SolicitudBuilder()
            .ConEstado(EstadoSolicitud.Rechazada)
            .ConMotivoRechazo(MotivoRechazo.SeleccionaronOtroCandidato)
            .ConFechaCierre(new DateTime(2024, 6, 1))
            .Construir();
        solicitud.Reabrir(EstadoSolicitud.EnRevision, "Retomo el proceso");
        solicitud.CambiarEstado(EstadoSolicitud.Rechazada, MotivoRechazo.CulturalFit, null);

        string[] lineas = CsvExportador.Construir(new[] { solicitud })
            .TrimEnd()
            .Split(Environment.NewLine);
        string[] cabecera = lineas[0].Split(';');
        string[] fila = lineas[1].Split(';');

        Assert.Equal(cabecera.Length, fila.Length);
        int cierre = Array.IndexOf(cabecera, Localizacion.Texto("Csv.Cierre"));
        Assert.Equal(Localizacion.Texto("Csv.MotivoCierre"), cabecera[cierre + 1]);
        Assert.Equal(Localizacion.Texto("Csv.Reapertura"), cabecera[cierre + 2]);
        Assert.Equal(TextoHelper.Escapar(EnumHelper.Descripcion(MotivoRechazo.CulturalFit)), fila[cierre + 1]);
        Assert.Equal(DateTime.Today.ToString("yyyy-MM-dd"), fila[cierre + 2]);
    }

    [Fact]
    public void Construir_SinSolicitudesSoloDevuelveLaCabecera()
    {
        string csv = CsvExportador.Construir(Array.Empty<Solicitud>());

        Assert.Single(csv.TrimEnd().Split(Environment.NewLine));
        Assert.StartsWith(Localizacion.Texto("Csv.Empresa"), csv);
    }

    [Theory]
    [InlineData(';')]
    [InlineData(',')]
    [InlineData('\t')]
    public void Construir_UsaElSeparadorEnCabeceraYFilas(char separador)
    {
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME").Construir();
        int columnas = CsvExportador.Construir(Array.Empty<Solicitud>())
            .TrimEnd()
            .Split(';')
            .Length;

        string[] lineas = CsvExportador.Construir(new[] { solicitud }, separador)
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(columnas, lineas[0].Split(separador).Length);
        Assert.Equal(columnas, lineas[1].Split(separador).Length);
    }

    [Fact]
    public void Construir_UnValorConElSeparadorSigueEntreComillas()
    {
        var solicitud = new SolicitudBuilder().ConEmpresa("ACME, S.L.").Construir();

        string csv = CsvExportador.Construir(new[] { solicitud }, ',');

        Assert.Contains("\"ACME, S.L.\"", csv);
    }

    [Fact]
    public void Construir_TodasLasCabecerasEstanTraducidas()
    {
        string csv = CsvExportador.Construir(Array.Empty<Solicitud>());

        Assert.DoesNotContain("Csv.", csv);
    }
}
