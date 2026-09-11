using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Turnos.Api.Auth;

namespace Turnos.Api.Controllers;

public record LoginClienteRequest(string Cedula);
public record LoginEmpleadoRequest(string Usuario, string Password);
public record TokenResponse(string Token, string Rol, DateTime ExpiraUtc);

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly JwtSettings _jwtSettings;
    private readonly EmpleadoDemoSettings _empleadoSettings;

    public AuthController(IJwtTokenGenerator tokenGenerator, IOptions<JwtSettings> jwtSettings, IOptions<EmpleadoDemoSettings> empleadoSettings)
    {
        _tokenGenerator = tokenGenerator;
        _jwtSettings = jwtSettings.Value;
        _empleadoSettings = empleadoSettings.Value;
    }

    /// <summary>
    /// Emite el token de sesión para un cliente identificado por su cédula.
    /// En un escenario real, este paso ocurre tras la autenticación fuerte
    /// del cliente en el app móvil/web (PIN, biometría, OTP, etc.).
    /// </summary>
    [HttpPost("token-cliente")]
    public ActionResult<TokenResponse> TokenCliente(LoginClienteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Cedula) || request.Cedula.Length < 6)
            return BadRequest(new { mensaje = "Cédula inválida." });

        var token = _tokenGenerator.GenerarToken(request.Cedula, "Cliente",
            new Dictionary<string, string> { ["cedula"] = request.Cedula });

        return Ok(new TokenResponse(token, "Cliente", DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiracionMinutos)));
    }

    /// <summary>Login de personal de sucursal (credenciales demo en appsettings).</summary>
    [HttpPost("login")]
    public ActionResult<TokenResponse> LoginEmpleado(LoginEmpleadoRequest request)
    {
        if (request.Usuario != _empleadoSettings.Usuario || request.Password != _empleadoSettings.Password)
            return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." });

        var token = _tokenGenerator.GenerarToken(request.Usuario, "Empleado");
        return Ok(new TokenResponse(token, "Empleado", DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiracionMinutos)));
    }
}
