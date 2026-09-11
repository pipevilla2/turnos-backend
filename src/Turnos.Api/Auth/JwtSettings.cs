namespace Turnos.Api.Auth;

public class JwtSettings
{
    public string Key { get; set; } = default!;
    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public int ExpiracionMinutos { get; set; } = 120;
}

public class EmpleadoDemoSettings
{
    public string Usuario { get; set; } = default!;
    public string Password { get; set; } = default!;
}
