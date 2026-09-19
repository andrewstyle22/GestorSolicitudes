using System.IO;
using GestorSolicitudes.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorSolicitudes.Data;

public class AppDbContext : DbContext
{
    public DbSet<Solicitud> Solicitudes => Set<Solicitud>();
    public DbSet<Evento> Eventos => Set<Evento>();

    /// <summary>
    /// %APPDATA%\GestorSolicitudes\solicitudes.db — fuera de la carpeta del ejecutable,
    /// para que recompilar o mover la app no se lleve los datos por delante.
    /// </summary>
    public static string RutaBaseDatos { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GestorSolicitudes",
        "solicitudes.db");

    private readonly string _rutaBaseDatos;

    public AppDbContext() : this(RutaBaseDatos) { }

    /// <summary>Permite apuntar a otra base (p. ej. una temporal, en los tests).</summary>
    public AppDbContext(string rutaBaseDatos)
    {
        _rutaBaseDatos = rutaBaseDatos;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var carpeta = Path.GetDirectoryName(_rutaBaseDatos);
        if (!string.IsNullOrEmpty(carpeta))
            Directory.CreateDirectory(carpeta);

        optionsBuilder.UseSqlite($"Data Source={_rutaBaseDatos}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Solicitud>(entidad =>
        {
            entidad.Property(s => s.Empresa).IsRequired();
            entidad.Property(s => s.Puesto).IsRequired();
            entidad.HasIndex(s => s.Empresa);
            entidad.HasIndex(s => s.FechaSolicitud);

            entidad.HasMany(s => s.Eventos)
                   .WithOne(e => e.Solicitud!)
                   .HasForeignKey(e => e.SolicitudId)
                   .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}
