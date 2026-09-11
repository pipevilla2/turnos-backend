using Turnos.Domain.Entities;

namespace Turnos.Domain.Interfaces;

public interface ISucursalRepository
{
    Task<Sucursal?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Sucursal>> GetAllAsync(CancellationToken ct = default);
}
