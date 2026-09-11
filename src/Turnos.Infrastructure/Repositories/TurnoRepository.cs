using Microsoft.EntityFrameworkCore;
using Turnos.Domain.Entities;
using Turnos.Domain.Enums;
using Turnos.Domain.Interfaces;
using Turnos.Infrastructure.Data;

namespace Turnos.Infrastructure.Repositories;

public class TurnoRepository : ITurnoRepository
{
    private readonly TurnosDbContext _context;

    public TurnoRepository(TurnosDbContext context) => _context = context;

    public Task<Turno?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Turnos.Include(t => t.Sucursal).FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<Turno>> GetAllAsync(string? cedula, int? sucursalId, EstadoTurno? estado, CancellationToken ct = default)
    {
        var query = _context.Turnos.Include(t => t.Sucursal).AsQueryable();

        if (!string.IsNullOrWhiteSpace(cedula))
            query = query.Where(t => t.Cedula == cedula);
        if (sucursalId.HasValue)
            query = query.Where(t => t.SucursalId == sucursalId.Value);
        if (estado.HasValue)
            query = query.Where(t => t.Estado == estado.Value);

        return await query.OrderByDescending(t => t.FechaHoraCreacion).ToListAsync(ct);
    }

    public async Task AddAsync(Turno turno, CancellationToken ct = default) =>
        await _context.Turnos.AddAsync(turno, ct);

    public Task<int> CountByCedulaOnDateAsync(string cedula, DateTime fechaUtc, CancellationToken ct = default)
    {
        var inicioDia = fechaUtc.Date;
        var finDia = inicioDia.AddDays(1);
        return _context.Turnos.CountAsync(t =>
            t.Cedula == cedula &&
            t.FechaHoraCreacion >= inicioDia &&
            t.FechaHoraCreacion < finDia &&
            t.Estado != EstadoTurno.Cancelado, ct);
    }

    public Task<IReadOnlyList<Turno>> GetPendientesVencidosAsync(DateTime ahoraUtc, CancellationToken ct = default) =>
        _context.Turnos
            .Where(t => t.Estado == EstadoTurno.Pendiente && t.FechaHoraExpiracion < ahoraUtc)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<Turno>)t.Result, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
}
