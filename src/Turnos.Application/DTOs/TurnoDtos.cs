using System.ComponentModel.DataAnnotations;

namespace Turnos.Application.DTOs;

public record CrearTurnoDto(
    [property: Required, RegularExpression(@"^\d{6,15}$", ErrorMessage = "La cédula debe contener solo dígitos (6 a 15).")]
    string Cedula,
    [property: Range(1, int.MaxValue, ErrorMessage = "Debe indicar una sucursal válida.")]
    int SucursalId
);

public record ActualizarEstadoTurnoDto(
    [property: Required]
    string Estado // "Atendido" | "Cancelado"
);

public record TurnoDto(
    Guid Id,
    string CodigoTurno,
    string Cedula,
    int SucursalId,
    string SucursalNombre,
    DateTime FechaHoraCreacion,
    DateTime FechaHoraExpiracion,
    DateTime? FechaHoraActivacion,
    string Estado,
    int SegundosRestantesActivacion
);
