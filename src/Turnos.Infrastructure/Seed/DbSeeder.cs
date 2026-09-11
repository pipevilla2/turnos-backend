using Turnos.Domain.Entities;
using Turnos.Infrastructure.Data;

namespace Turnos.Infrastructure.Seed;

public static class DbSeeder
{
    public static void Seed(TurnosDbContext context)
    {
        context.Database.EnsureCreated();

        if (context.Sucursales.Any()) return;

        context.Sucursales.AddRange(
            new Sucursal { Nombre = "Sucursal Centro", Direccion = "Cra 50 # 45-10", Ciudad = "Medellín", Activa = true },
            new Sucursal { Nombre = "Sucursal Poblado", Direccion = "Cl 10 # 40-25", Ciudad = "Medellín", Activa = true },
            new Sucursal { Nombre = "Sucursal Norte", Direccion = "Cra 65 # 90-30", Ciudad = "Medellín", Activa = true },
            new Sucursal { Nombre = "Sucursal Chapinero", Direccion = "Cl 60 # 9-20", Ciudad = "Bogotá", Activa = true }
        );
        context.SaveChanges();
    }
}
