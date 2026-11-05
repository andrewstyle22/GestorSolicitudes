namespace GestorSolicitudes.Tests;

using GestorSolicitudes.Data;
using GestorSolicitudes.Models;

public class NormalizarMotivosTests
{
    [Theory]
    [InlineData(8, null, 6)] // Oferta rechazada por mí, sin motivo
    [InlineData(9, null, 6)] // Descartado por la empresa, sin motivo
    [InlineData(10, null, 6)] // Retirada por mí, sin motivo
    [InlineData(8, 1, 6)] // Motivo de empresa que no vale para un cierre propio
    [InlineData(8, 2, 6)]
    [InlineData(8, 3, 6)]
    [InlineData(8, 4, 6)]
    [InlineData(10, 1, 6)]
    [InlineData(9, 1, 1)] // Descartado conserva los motivos de empresa
    [InlineData(9, 4, 4)]
    [InlineData(8, 0, 0)] // Motivos válidos para el cierre propio no cambian
    [InlineData(8, 5, 5)]
    [InlineData(8, 7, 7)]
    [InlineData(10, 8, 8)]
    [InlineData(10, 9, 9)]
    [InlineData(7, null, null)] // Fuera de los tres cierres negativos no se toca
    [InlineData(7, 1, 1)]
    public void Normalizar_RellenaSoloLosMotivosQueNoValen(int estado, int? motivo, int? esperado)
    {
        string ruta = CrearBaseConFila((EstadoSolicitud)estado, motivo is null ? null : (MotivoRechazo)motivo);

        EsquemaDb.NormalizarMotivosDeCierre(ruta);

        Assert.Equal(esperado, LeerMotivo(ruta));
    }

    [Fact]
    public void Normalizar_EsIdempotente()
    {
        string ruta = CrearBaseConFila(EstadoSolicitud.OfertaRechazada, null);

        EsquemaDb.NormalizarMotivosDeCierre(ruta);
        EsquemaDb.NormalizarMotivosDeCierre(ruta);

        Assert.Equal((int)MotivoRechazo.NoIndicado, LeerMotivo(ruta));
    }

    private static string CrearBaseConFila(EstadoSolicitud estado, MotivoRechazo? motivo)
    {
        using var db = TestDb.NuevoContexto(out string ruta);
        var solicitud = new Solicitud("Empresa", "Puesto");
        solicitud.CambiarEstado(estado, motivo, null);
        db.Solicitudes.Add(solicitud);
        db.SaveChanges();
        return ruta;
    }

    private static int? LeerMotivo(string ruta)
    {
        using var db = new AppDbContext(ruta);
        return (int?)db.Solicitudes.Single().MotivoRechazo;
    }
}
