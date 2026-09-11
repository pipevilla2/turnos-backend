using Turnos.Application.DTOs;

namespace Turnos.Application.Interfaces;

public interface ITurnoService
{
    Task<TurnoDto> CrearTurnoAsync(CrearTurnoDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<TurnoDto>> ObtenerTodosAsync(string? cedula, int? sucursalId, string? estado, CancellationToken ct = default);
    Task<TurnoDto> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);
    Task<TurnoDto> ActivarTurnoAsync(Guid id, CancellationToken ct = default);
    Task<TurnoDto> ActualizarEstadoAsync(Guid id, ActualizarEstadoTurnoDto dto, CancellationToken ct = default);
}
