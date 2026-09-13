using Turnos.Domain.Enums;
using Turnos.Domain.Exceptions;

namespace Turnos.Domain.Entities;

/// <summary>
/// Representa un turno agendado por un cliente para ser atendido en una sucursal.
/// La entidad concentra las reglas de transición de estado (rich domain model)
/// para evitar que la lógica de negocio quede dispersa en los servicios.
/// </summary>
public class Turno
{
    public const int MinutosLimiteActivacion = 15;
    public const int MaxTurnosDiarios = 5;

    public Guid Id { get; private set; }
    public string CodigoTurno { get; private set; } = default!;
    public string Cedula { get; private set; } = default!;
    public int SucursalId { get; private set; }
    public Sucursal? Sucursal { get; private set; }
    public DateTime FechaHoraCreacion { get; private set; }
    public DateTime FechaHoraExpiracion { get; private set; }
    public DateTime? FechaHoraActivacion { get; private set; }
    public EstadoTurno Estado { get; private set; }

    // Requerido por EF Core
    private Turno() { }

    public static Turno Crear(string cedula, int sucursalId, string codigoTurno, DateTime ahoraColombia)
    {
        return new Turno
        {
            Id = Guid.NewGuid(),
            Cedula = cedula,
            SucursalId = sucursalId,
            CodigoTurno = codigoTurno,
            FechaHoraCreacion = ahoraColombia,
            FechaHoraExpiracion = ahoraColombia.AddMinutes(MinutosLimiteActivacion),
            Estado = EstadoTurno.Pendiente
        };
    }

    public void Activar(DateTime ahoraColombia)
    {
        if (Estado == EstadoTurno.Expirado || (Estado == EstadoTurno.Pendiente && ahoraColombia > FechaHoraExpiracion))
        {
            Estado = EstadoTurno.Expirado;
            throw new TurnoExpiradoException(Id);
        }

        if (Estado != EstadoTurno.Pendiente)
            throw new OperacionInvalidaException($"El turno {CodigoTurno} no se puede activar en estado {Estado}.");

        Estado = EstadoTurno.Activado;
        FechaHoraActivacion = ahoraColombia;
    }

    public void MarcarAtendido()
    {
        if (Estado != EstadoTurno.Activado)
            throw new OperacionInvalidaException($"El turno {CodigoTurno} debe estar Activado para marcarse como Atendido.");
        Estado = EstadoTurno.Atendido;
    }

    public void Cancelar()
    {
        if (Estado is EstadoTurno.Atendido or EstadoTurno.Expirado or EstadoTurno.Cancelado)
            throw new OperacionInvalidaException($"El turno {CodigoTurno} no se puede cancelar en estado {Estado}.");
        Estado = EstadoTurno.Cancelado;
    }

    /// <summary>Usado por el proceso de expiración en segundo plano.</summary>
    public bool IntentarExpirar(DateTime ahoraColombia)
    {
        if (Estado == EstadoTurno.Pendiente && ahoraColombia > FechaHoraExpiracion)
        {
            Estado = EstadoTurno.Expirado;
            return true;
        }
        return false;
    }
}
