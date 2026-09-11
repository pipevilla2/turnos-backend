using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Turnos.Application.DTOs;
using Turnos.Application.Interfaces;

namespace Turnos.Api.Controllers;

[ApiController]
[Route("api/turnos")]
[Authorize]
public class TurnosController : ControllerBase
{
    private readonly ITurnoService _service;

    public TurnosController(ITurnoService service) => _service = service;

    private bool EsEmpleado => User.IsInRole("Empleado");
    private string? CedulaDelToken => User.FindFirstValue("cedula");

    /// <summary>Crea un nuevo turno. Un cliente solo puede agendar para su propia cédula.</summary>
    [HttpPost]
    public async Task<ActionResult<TurnoDto>> Crear([FromBody] CrearTurnoDto dto, CancellationToken ct)
    {
        if (!EsEmpleado && !string.Equals(CedulaDelToken, dto.Cedula, StringComparison.Ordinal))
            return Forbid();

        var turno = await _service.CrearTurnoAsync(dto, ct);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = turno.Id }, turno);
    }

    /// <summary>
    /// Lista turnos. Un cliente solo ve los suyos (filtra automáticamente por
    /// la cédula del token); un empleado puede ver todos y filtrar libremente.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TurnoDto>>> ObtenerTodos(
        [FromQuery] string? cedula, [FromQuery] int? sucursalId, [FromQuery] string? estado, CancellationToken ct)
    {
        var cedulaFiltro = EsEmpleado ? cedula : CedulaDelToken;
        return Ok(await _service.ObtenerTodosAsync(cedulaFiltro, sucursalId, estado, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TurnoDto>> ObtenerPorId(Guid id, CancellationToken ct)
    {
        var turno = await _service.ObtenerPorIdAsync(id, ct);
        if (!EsEmpleado && !string.Equals(CedulaDelToken, turno.Cedula, StringComparison.Ordinal))
            return Forbid();
        return Ok(turno);
    }

    /// <summary>Activa el turno cuando el cliente llega físicamente a la sucursal.</summary>
    [HttpPost("{id:guid}/activar")]
    public async Task<ActionResult<TurnoDto>> Activar(Guid id, CancellationToken ct)
    {
        var turno = await _service.ObtenerPorIdAsync(id, ct);
        if (!EsEmpleado && !string.Equals(CedulaDelToken, turno.Cedula, StringComparison.Ordinal))
            return Forbid();

        return Ok(await _service.ActivarTurnoAsync(id, ct));
    }

    /// <summary>Marca un turno como Atendido o Cancelado. Restringido a personal de sucursal.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Empleado")]
    public async Task<ActionResult<TurnoDto>> ActualizarEstado(Guid id, [FromBody] ActualizarEstadoTurnoDto dto, CancellationToken ct)
        => Ok(await _service.ActualizarEstadoAsync(id, dto, ct));
}
