using Turnos.Application.DTOs;
using Turnos.Application.Interfaces;
using Turnos.Domain.Entities;
using Turnos.Domain.Enums;
using Turnos.Domain.Exceptions;
using Turnos.Domain.Interfaces;
using Turnos.Domain.Time;

namespace Turnos.Application.Services;

/// <summary>
/// Orquesta las reglas de negocio de agendamiento de turnos.
/// Las fechas de negocio se generan y almacenan con la hora local de Colombia.
/// </summary>
public class TurnoService : ITurnoService
{
    private readonly ITurnoRepository _turnoRepository;
    private readonly ISucursalRepository _sucursalRepository;

    public TurnoService(ITurnoRepository turnoRepository, ISucursalRepository sucursalRepository)
    {
        _turnoRepository = turnoRepository;
        _sucursalRepository = sucursalRepository;
    }

    public async Task<TurnoDto> CrearTurnoAsync(CrearTurnoDto dto, CancellationToken ct = default)
    {
        var sucursal = await _sucursalRepository.GetByIdAsync(dto.SucursalId, ct)
            ?? throw new NotFoundException(nameof(Sucursal), dto.SucursalId);

        if (!sucursal.Activa)
            throw new SucursalInactivaException(sucursal.Id);

        var ahora = ColombiaClock.Ahora;
        var inicioDia = ahora.Date;
        var finDia = inicioDia.AddDays(1);

        var turnosCedulaHoy = await _turnoRepository.CountByCedulaBetweenAsync(dto.Cedula, inicioDia, finDia, ct);
        if (turnosCedulaHoy >= Turno.MaxTurnosDiarios)
            throw new LimiteTurnosDiariosException(dto.Cedula);

        var consecutivo = await _turnoRepository.GetNextConsecutivoAsync(sucursal.Id, ct);
        var codigo = GenerarCodigoTurno(sucursal.Id, consecutivo);
        var turno = Turno.Crear(dto.Cedula, sucursal.Id, codigo, ahora);

        await _turnoRepository.AddAsync(turno, ct);
        await _turnoRepository.SaveChangesAsync(ct);

        return MapToDto(turno, sucursal.Nombre);
    }

    public async Task<IReadOnlyList<TurnoDto>> ObtenerTodosAsync(string? cedula, int? sucursalId, string? estado, CancellationToken ct = default)
    {
        EstadoTurno? estadoEnum = null;
        if (!string.IsNullOrWhiteSpace(estado))
        {
            if (!Enum.TryParse<EstadoTurno>(estado, true, out var parsed))
                throw new OperacionInvalidaException($"Estado '{estado}' no es válido.");
            estadoEnum = parsed;
        }

        var turnos = await _turnoRepository.GetAllAsync(cedula, sucursalId, estadoEnum, ct);
        return turnos.Select(t => MapToDto(t, t.Sucursal?.Nombre ?? string.Empty)).ToList();
    }

    public async Task<TurnoDto> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
    {
        var turno = await _turnoRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Turno), id);
        return MapToDto(turno, turno.Sucursal?.Nombre ?? string.Empty);
    }

    public async Task<TurnoDto> ActivarTurnoAsync(Guid id, CancellationToken ct = default)
    {
        var turno = await _turnoRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Turno), id);

        turno.Activar(ColombiaClock.Ahora);
        await _turnoRepository.SaveChangesAsync(ct);

        return MapToDto(turno, turno.Sucursal?.Nombre ?? string.Empty);
    }

    public async Task<TurnoDto> ActualizarEstadoAsync(Guid id, ActualizarEstadoTurnoDto dto, CancellationToken ct = default)
    {
        var turno = await _turnoRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Turno), id);

        if (!Enum.TryParse<EstadoTurno>(dto.Estado, true, out var nuevoEstado))
            throw new OperacionInvalidaException($"Estado '{dto.Estado}' no es válido.");

        switch (nuevoEstado)
        {
            case EstadoTurno.Atendido:
                turno.MarcarAtendido();
                break;
            case EstadoTurno.Cancelado:
                turno.Cancelar();
                break;
            default:
                throw new OperacionInvalidaException("Solo se permite actualizar manualmente a 'Atendido' o 'Cancelado'.");
        }

        await _turnoRepository.SaveChangesAsync(ct);
        return MapToDto(turno, turno.Sucursal?.Nombre ?? string.Empty);
    }

    private static string GenerarCodigoTurno(int sucursalId, int consecutivoDia)
    {
        return $"S{sucursalId:D2}-{consecutivoDia:D3}";
    }

    private static TurnoDto MapToDto(Turno t, string sucursalNombre)
    {
        var segundosRestantes = t.Estado == EstadoTurno.Pendiente
            ? Math.Max(0, (int)(t.FechaHoraExpiracion - ColombiaClock.Ahora).TotalSeconds)
            : 0;

        return new TurnoDto(
            t.Id, t.CodigoTurno, t.Cedula, t.SucursalId, sucursalNombre,
            t.FechaHoraCreacion, t.FechaHoraExpiracion, t.FechaHoraActivacion,
            t.Estado.ToString(), segundosRestantes);
    }
}
