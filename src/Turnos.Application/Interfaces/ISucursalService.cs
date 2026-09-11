using Turnos.Application.DTOs;

namespace Turnos.Application.Interfaces;

public interface ISucursalService
{
    Task<IReadOnlyList<SucursalDto>> ObtenerTodasAsync(CancellationToken ct = default);
}
