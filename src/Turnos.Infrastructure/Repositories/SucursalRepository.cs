using Microsoft.EntityFrameworkCore;
using Turnos.Domain.Entities;
using Turnos.Domain.Interfaces;
using Turnos.Infrastructure.Data;

namespace Turnos.Infrastructure.Repositories;

public class SucursalRepository : ISucursalRepository
{
    private readonly TurnosDbContext _context;

    public SucursalRepository(TurnosDbContext context) => _context = context;

    public Task<Sucursal?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _context.Sucursales.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<IReadOnlyList<Sucursal>> GetAllAsync(CancellationToken ct = default) =>
        _context.Sucursales.OrderBy(s => s.Nombre).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<Sucursal>)t.Result, ct);
}
