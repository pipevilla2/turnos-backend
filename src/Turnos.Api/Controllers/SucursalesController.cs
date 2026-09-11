using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Turnos.Application.Interfaces;

namespace Turnos.Api.Controllers;

[ApiController]
[Route("api/sucursales")]
[AllowAnonymous] // información pública requerida para poder agendar
public class SucursalesController : ControllerBase
{
    private readonly ISucursalService _service;

    public SucursalesController(ISucursalService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> ObtenerTodas(CancellationToken ct)
        => Ok(await _service.ObtenerTodasAsync(ct));
}
