using Turnos.Domain.Entities;
using Turnos.Domain.Enums;

namespace Turnos.Domain.Interfaces;

public interface ITurnoRepository
{
    Task<Turno?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Turno>> GetAllAsync(string? cedula, int? sucursalId, EstadoTurno? estado, CancellationToken ct = default);
    Task AddAsync(Turno turno, CancellationToken ct = default);
    Task<int> CountByCedulaBetweenAsync(string cedula, DateTime inicioUtc, DateTime finUtc, CancellationToken ct = default);
    Task<int> CountBySucursalBetweenAsync(int sucursalId, DateTime inicioUtc, DateTime finUtc, CancellationToken ct = default);
    Task<int> GetNextConsecutivoAsync(int sucursalId, CancellationToken ct = default);
    Task<IReadOnlyList<Turno>> GetPendientesVencidosAsync(DateTime ahoraUtc, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
