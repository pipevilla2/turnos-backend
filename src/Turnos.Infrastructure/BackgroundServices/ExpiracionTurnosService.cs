using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Turnos.Domain.Interfaces;
using Turnos.Domain.Time;

namespace Turnos.Infrastructure.BackgroundServices;

/// <summary>
/// Proceso en segundo plano que recorre periódicamente los turnos en estado
/// Pendiente cuya fecha de expiración ya pasó y los marca como Expirado.
/// Esto garantiza la regla de negocio "15 minutos para activar el turno"
/// incluso si el cliente nunca vuelve a consultar el turno.
/// </summary>
public class ExpiracionTurnosService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiracionTurnosService> _logger;
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(30);

    public ExpiracionTurnosService(IServiceScopeFactory scopeFactory, ILogger<ExpiracionTurnosService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<ITurnoRepository>();

                var ahora = ColombiaClock.Ahora;
                var vencidos = await repo.GetPendientesVencidosAsync(ahora, stoppingToken);
                if (vencidos.Count > 0)
                {
                    foreach (var turno in vencidos)
                        turno.IntentarExpirar(ahora);

                    await repo.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Se expiraron {Cantidad} turnos vencidos.", vencidos.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ejecutando el barrido de expiración de turnos.");
            }

            await Task.Delay(Intervalo, stoppingToken);
        }
    }
}
