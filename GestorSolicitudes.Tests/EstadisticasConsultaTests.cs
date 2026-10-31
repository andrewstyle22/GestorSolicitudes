namespace GestorSolicitudes.Tests;

using System.Data.Common;
using GestorSolicitudes.Data;
using GestorSolicitudes.Models;
using GestorSolicitudes.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

/// <summary>
/// La cabecera de métricas se calcula con la lista que la recarga ya ha leído: cambiar de
/// filtro o de página no lanza una segunda consulta de tabla completa, y los totales siguen
/// contando la base entera aunque la tabla esté filtrada.
/// </summary>
public class EstadisticasConsultaTests
{
    [Fact]
    public void Recargar_LeeLaTablaUnaVezAunqueCambienLosFiltros()
    {
        using var db = new ContextoContador();
        db.Database.EnsureCreated();
        db.Solicitudes.Add(new SolicitudBuilder().ConEmpresa("ACME").ConPuesto("Dev").Construir());
        db.Solicitudes.Add(new SolicitudBuilder().ConEmpresa("Globex").ConPuesto("Dev").ConEstado(EstadoSolicitud.Rechazada).Construir());
        db.SaveChanges();

        using var vm = new MainViewModel(db);
        db.ReiniciarContador();

        vm.TextoBusqueda = "acme";
        vm.SoloAbiertas = true;

        Assert.Equal(2, db.LecturasDeLaTabla);
        Assert.Single(vm.Solicitudes);
        Assert.Equal(2, vm.TotalSolicitudes);
        Assert.Equal(1, vm.ProcesosAbiertos);
    }

    private sealed class ContadorDeConsultas : DbCommandInterceptor
    {
        public int LecturasDeLaTabla { get; set; }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            // La consulta de entidades empieza seleccionando la clave; el MIN del embudo no.
            if (command.CommandText.StartsWith($"SELECT \"s\".\"{nameof(Solicitud.Id)}\"", StringComparison.Ordinal))
            {
                this.LecturasDeLaTabla++;
            }

            return base.ReaderExecuting(command, eventData, result);
        }
    }

    private sealed class ContextoContador : AppDbContext
    {
        private readonly ContadorDeConsultas contador = new();

        public ContextoContador()
            : base(TestDb.NuevaRuta())
        {
        }

        public int LecturasDeLaTabla => this.contador.LecturasDeLaTabla;

        public void ReiniciarContador() => this.contador.LecturasDeLaTabla = 0;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.AddInterceptors(this.contador);
        }
    }
}
