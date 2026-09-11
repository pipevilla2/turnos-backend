namespace Turnos.Domain.Enums;

/// <summary>
/// Ciclo de vida de un turno.
/// Pendiente -> Activado -> Atendido
/// Pendiente -> Expirado (si no se activa dentro del tiempo límite)
/// Pendiente/Activado -> Cancelado (cancelación manual)
/// </summary>
public enum EstadoTurno
{
    Pendiente = 0,
    Activado = 1,
    Atendido = 2,
    Expirado = 3,
    Cancelado = 4
}
