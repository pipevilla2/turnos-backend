using Microsoft.EntityFrameworkCore;
using Turnos.Domain.Entities;
using Turnos.Domain.Enums;

namespace Turnos.Infrastructure.Data;

public class TurnosDbContext : DbContext
{
    public TurnosDbContext(DbContextOptions<TurnosDbContext> options) : base(options) { }

    public DbSet<Turno> Turnos => Set<Turno>();
    public DbSet<Sucursal> Sucursales => Set<Sucursal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sucursal>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.Nombre).IsRequired().HasMaxLength(150);
            b.Property(s => s.Direccion).IsRequired().HasMaxLength(250);
            b.Property(s => s.Ciudad).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Turno>(b =>
        {
            b.HasKey(t => t.Id);
            b.Property(t => t.Cedula).IsRequired().HasMaxLength(20);
            b.Property(t => t.CodigoTurno).IsRequired().HasMaxLength(30);
            b.Property(t => t.Estado)
                .HasConversion<string>()
                .HasMaxLength(20);

            b.HasOne(t => t.Sucursal)
                .WithMany(s => s.Turnos)
                .HasForeignKey(t => t.SucursalId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índices que soportan las consultas más frecuentes:
            // validar el límite diario por cédula y el barrido de expiración.
            b.HasIndex(t => new { t.Cedula, t.FechaHoraCreacion });
            b.HasIndex(t => new { t.Estado, t.FechaHoraExpiracion });
        });
    }
}
