// <copyright file="AppDbContext.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace GestorSolicitudes.Data;

using System.IO;
using GestorSolicitudes.Models;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<Solicitud> Solicitudes => this.Set<Solicitud>();

    public DbSet<Evento> Eventos => this.Set<Evento>();

    /// <summary>
    /// Gets %APPDATA%\GestorSolicitudes\solicitudes.db — fuera de la carpeta del ejecutable,
    /// para que recompilar o mover la app no se lleve los datos por delante.
    /// </summary>
    public static string RutaBaseDatos { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GestorSolicitudes",
        "solicitudes.db");

    private readonly string rutaBaseDatos;

    public AppDbContext()
        : this(RutaBaseDatos)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AppDbContext"/> class.Permite apuntar a otra base (p. ej. una temporal, en los tests).</summary>
    public AppDbContext(string rutaBaseDatos)
    {
        this.rutaBaseDatos = rutaBaseDatos;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var carpeta = Path.GetDirectoryName(this.rutaBaseDatos);
        if (!string.IsNullOrEmpty(carpeta))
        {
            Directory.CreateDirectory(carpeta);
        }

        optionsBuilder.UseSqlite($"Data Source={this.rutaBaseDatos}");
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
