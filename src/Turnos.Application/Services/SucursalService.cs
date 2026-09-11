using Turnos.Application.DTOs;
using Turnos.Application.Interfaces;
using Turnos.Domain.Interfaces;

namespace Turnos.Application.Services;

public class SucursalService : ISucursalService
{
    private readonly ISucursalRepository _repository;

    public SucursalService(ISucursalRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<SucursalDto>> ObtenerTodasAsync(CancellationToken ct = default)
    {
        var sucursales = await _repository.GetAllAsync(ct);
        return sucursales.Select(s => new SucursalDto(s.Id, s.Nombre, s.Direccion, s.Ciudad, s.Activa)).ToList();
    }
}
