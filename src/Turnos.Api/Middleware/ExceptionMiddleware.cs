using System.Net;
using System.Text.Json;
using Turnos.Domain.Exceptions;

namespace Turnos.Api.Middleware;

/// <summary>
/// Traduce las excepciones de dominio a respuestas HTTP consistentes,
/// evitando bloques try/catch repetidos en cada controlador.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (status, titulo) = ex switch
            {
                NotFoundException => (HttpStatusCode.NotFound, "Recurso no encontrado"),
                LimiteTurnosDiariosException => (HttpStatusCode.Conflict, "Límite diario de turnos alcanzado"),
                TurnoExpiradoException => (HttpStatusCode.Conflict, "El turno expiró"),
                SucursalInactivaException => (HttpStatusCode.BadRequest, "Sucursal inactiva"),
                OperacionInvalidaException => (HttpStatusCode.BadRequest, "Operación inválida"),
                _ => (HttpStatusCode.InternalServerError, "Error interno del servidor")
            };

            if (status == HttpStatusCode.InternalServerError)
                _logger.LogError(ex, "Error no controlado procesando {Path}", context.Request.Path);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)status;

            var body = JsonSerializer.Serialize(new
            {
                status = (int)status,
                titulo,
                detalle = ex.Message,
                path = context.Request.Path.Value
            });

            await context.Response.WriteAsync(body);
        }
    }
}
