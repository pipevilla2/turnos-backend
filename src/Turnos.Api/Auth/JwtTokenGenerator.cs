using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Turnos.Api.Auth;

public interface IJwtTokenGenerator
{
    string GenerarToken(string subject, string rol, IDictionary<string, string>? claimsAdicionales = null);
}

/// <summary>
/// Emite JWT firmados simétricamente. Se usan dos "flujos" de autenticación:
///  - Cliente: identificado únicamente por número de cédula (simula que el
///    cliente ya se autenticó en el app móvil/web del banco con sus propios
///    factores; aquí solo se emite el token de sesión para consumir la API).
///  - Empleado: usuario/clave de sucursal, con permisos para gestionar
///    cualquier turno (activar, marcar atendido, cancelar).
/// </summary>
public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _settings;

    public JwtTokenGenerator(IOptions<JwtSettings> settings) => _settings = settings.Value;

    public string GenerarToken(string subject, string rol, IDictionary<string, string>? claimsAdicionales = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject),
            new(ClaimTypes.Role, rol),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (claimsAdicionales != null)
            claims.AddRange(claimsAdicionales.Select(kv => new Claim(kv.Key, kv.Value)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpiracionMinutos),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
